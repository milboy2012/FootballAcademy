using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Group;
using UI.Models.ViewModels.Player;
using UI.Services.Interfaces;

namespace UI.Services
{
    public class GroupService : IGroupService
    {
        private readonly IUoW _data;
        public GroupService(IUoW data) => _data = data;

        private IQueryable<GroupListItemDto> Project(IQueryable<TrainingGroup> src)
        {
            var now = DateTime.UtcNow;
            return src.Select(g => new GroupListItemDto(
                g.Id, g.Name, g.Season, g.MinBirthYear, g.MaxBirthYear, g.MaxPlayers,
                g.Players.Count(p => p.IsActive),
                g.CoachId, g.Coach.User.LastName + " " + g.Coach.User.FirstName, g.Color,
                g.IsArchived, g.ArchivedAt,
                g.Trainings.Count(t => t.StartsAt >= now && t.Status == TrainingStatus.Planned)));
        }

        public async Task<TabulatorPage<GroupListItemDto>> GetPageAsync(GroupsQuery q, CancellationToken ct)
        {
            var query = _data.Groups.Query().AsNoTracking().Where(g => g.IsArchived == (q.Archived ?? false));
            if (q.CoachId is not null) query = query.Where(g => g.CoachId == q.CoachId);
            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = $"%{q.Search.Trim()}%";
                query = query.Where(g => EF.Functions.ILike(g.Name, s) || EF.Functions.ILike(g.Coach.User.LastName, s));
            }

            var desc = q.SortDir == "desc";
            query = q.SortField switch
            {
                "coachName" => desc ? query.OrderByDescending(g => g.Coach.User.LastName) : query.OrderBy(g => g.Coach.User.LastName),
                "minBirthYear" => desc ? query.OrderByDescending(g => g.MinBirthYear) : query.OrderBy(g => g.MinBirthYear),
                "playersCount" => desc ? query.OrderByDescending(g => g.Players.Count) : query.OrderBy(g => g.Players.Count),
                _ => desc ? query.OrderByDescending(g => g.Name) : query.OrderBy(g => g.Name)
            };

            var size = Math.Clamp(q.Size, 1, 200); var page = Math.Max(q.Page, 1);
            var total = await query.CountAsync(ct);
            var data = await Project(query.Skip((page - 1) * size).Take(size)).ToListAsync(ct);
            return new TabulatorPage<GroupListItemDto>(data, Math.Max(1, (int)Math.Ceiling(total / (double)size)), total);
        }

        public Task<GroupListItemDto?> GetAsync(Guid id, CancellationToken ct)
            => Project(_data.Groups.Query().AsNoTracking().Where(g => g.Id == id)).FirstOrDefaultAsync(ct);

        public async Task<(Guid?, string?)> CreateAsync(GroupEditDto dto, CancellationToken ct)
        {
            if (await ValidateAsync(null, dto, ct) is { } e) return (null, e);
            var g = new TrainingGroup(); Apply(g, dto);
            _data.Groups.AddAsync(g,ct); 
            await _data.SaveChangesAsync(ct);
            return (g.Id, null);
        }

        public async Task<string?> UpdateAsync(Guid id, GroupEditDto dto, CancellationToken ct)
        {
            var g = await _data.Groups.Query().Include(x => x.Players).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (g is null) return "Группа не найдена";
            if (g.IsArchived) return "Архивная группа не редактируется";
            if (await ValidateAsync(id, dto, ct) is { } e) return e;

            // сужение возраста не должно выбросить текущих игроков
            var outOfRange = g.Players.Count(p => p.BirthDate.Year < dto.MinBirthYear || p.BirthDate.Year > dto.MaxBirthYear);
            if (outOfRange > 0) return $"{outOfRange} игрок(ов) не попадают в новый возрастной диапазон. Сначала переведите их";
            if (g.Players.Count(p => p.IsActive) > dto.MaxPlayers) return "Вместимость меньше текущего числа игроков";

            Apply(g, dto); await _data.SaveChangesAsync(ct);
            return null;
        }

        public async Task<string?> AssignCoachAsync(Guid id, Guid coachId, CancellationToken ct)
        {
            var g = await _data.Groups.Query().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (g is null) return "Группа не найдена";
            if (await CheckCoachAsync(coachId, ct) is { } e) return e;
            g.CoachId = coachId; await _data.SaveChangesAsync(ct);
            return null;
        }

        public async Task<List<GroupPlayerDto>> GetPlayersAsync(Guid id, CancellationToken ct)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var rows = await _data.Players.Query().AsNoTracking()
                .Where(p => p.GroupId == id)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new
                {
                    p.Id,
                    p.LastName,
                    p.FirstName,
                    p.BirthDate,
                    p.MedicalCertificateUntil,
                    ParentName = p.Parent.LastName + " " + p.Parent.FirstName,
                    ParentPhone = p.Parent.PhoneNumber,
                    HasSub = p.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active && s.To >= today),
                    Total = p.Attendances.Count(a => a.Training.GroupId == id),
                    Present = p.Attendances.Count(a => a.Training.GroupId == id && a.Present)
                }).ToListAsync(ct);

            return rows.Select(r => new GroupPlayerDto(r.Id, r.LastName, r.FirstName, r.BirthDate, Age(r.BirthDate, today),
                r.ParentName, r.ParentPhone, r.MedicalCertificateUntil,
                r.MedicalCertificateUntil is not null && r.MedicalCertificateUntil >= today,
                r.HasSub, r.Total == 0 ? 0 : r.Present * 100 / r.Total)).ToList();
        }

        /// <summary>Перевод (TargetGroupId задан) или отчисление (null). Работает для нескольких игроков.</summary>
        public async Task<string?> MovePlayersAsync(Guid id, MovePlayersDto dto, CancellationToken ct)
        {
            if (dto.PlayerIds.Length == 0) return "Не выбраны игроки";
            var players = await _data.Players.Query().Where(p => p.GroupId == id && dto.PlayerIds.Contains(p.Id)).ToListAsync(ct);
            if (players.Count != dto.PlayerIds.Length) return "Некоторые игроки не найдены в этой группе";

            if (dto.TargetGroupId is Guid target)
            {
                if (target == id) return "Это та же группа";
                var tg = await _data.Groups.Query().Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == target, ct);
                if (tg is null || tg.IsArchived) return "Целевая группа не найдена или в архиве";
                var free = tg.MaxPlayers - tg.Players.Count(p => p.IsActive);
                if (players.Count > free) return $"В группе «{tg.Name}» свободно мест: {free}";
                var bad = players.Where(p => p.BirthDate.Year < tg.MinBirthYear || p.BirthDate.Year > tg.MaxBirthYear).ToList();
                if (bad.Count > 0) return $"Не подходят по возрасту для «{tg.Name}»: {string.Join(", ", bad.Select(b => b.LastName))}";
            }

            foreach (var p in players) p.GroupId = dto.TargetGroupId;
            await _data.SaveChangesAsync(ct);
            return null;
        }

        /// <summary>Архивация: будущие тренировки отменяются, игроки переводятся в moveTo или освобождаются.</summary>
        public async Task<string?> ArchiveAsync(Guid id, bool archive, Guid? moveTo, CancellationToken ct)
        {
            var g = await _data.Groups.Query().Include(x => x.Players).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (g is null) return "Группа не найдена";
            if (g.IsArchived == archive) return null;

            if (archive)
            {
                if (moveTo is not null)
                {
                    var err = await MovePlayersAsync(id, new MovePlayersDto(g.Players.Select(p => p.Id).ToArray(), moveTo), ct);
                    if (err is not null) return err;
                }
                else foreach (var p in g.Players) p.GroupId = null;

                var future = await _data.Trainings.Query()
                    .Where(t => t.GroupId == id && t.StartsAt >= DateTime.UtcNow && t.Status == TrainingStatus.Planned)
                    .ToListAsync(ct);
                foreach (var t in future) t.Status = TrainingStatus.Cancelled;

                g.IsArchived = true; g.ArchivedAt = DateTime.UtcNow;
            }
            else
            {
                if (await _data.Groups.AnyAsync(x => x.Id != id && !x.IsArchived && x.Name == g.Name, ct))
                    return $"Активная группа «{g.Name}» уже существует — переименуйте её перед восстановлением";
                g.IsArchived = false; g.ArchivedAt = null;
            }
            await _data.SaveChangesAsync(ct);
            return null;
        }

        public Task<List<CoachLookupDto>> GetCoachesAsync(CancellationToken ct) =>
            _data.Coaches.Query().AsNoTracking()
                .OrderBy(c => c.User.LastName)
                .Select(c => new CoachLookupDto(c.Id, c.User.LastName + " " + c.User.FirstName,
                    c.Groups.Count(g => !g.IsArchived), c.User.IsActive))
                .ToListAsync(ct);

        // ---------- helpers ----------

        private async Task<string?> ValidateAsync(Guid? id, GroupEditDto d, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(d.Name)) return "Название обязательно";
            var y = DateTime.UtcNow.Year;
            if (d.MinBirthYear < y - 18 || d.MaxBirthYear > y || d.MinBirthYear > d.MaxBirthYear) return "Некорректный диапазон годов рождения";
            if (d.MaxPlayers is < 1 or > 50) return "Вместимость: от 1 до 50";
            if (await _data.Groups.AnyAsync(g => g.Id != id && !g.IsArchived && g.Name == d.Name.Trim(), ct)) return "Активная группа с таким названием уже есть";
            return await CheckCoachAsync(d.CoachId, ct);
        }

        private async Task<string?> CheckCoachAsync(Guid coachId, CancellationToken ct)
        {
            var c = await _data.Coaches.Query().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == coachId, ct);
            if (c is null) return "Тренер не найден";
            if (!c.User.IsActive) return "Тренер заблокирован";
            return null;
        }

        private static void Apply(TrainingGroup g, GroupEditDto d)
        {
            g.Name = d.Name.Trim(); g.Season = d.Season?.Trim(); g.MinBirthYear = d.MinBirthYear; g.MaxBirthYear = d.MaxBirthYear;
            g.MaxPlayers = d.MaxPlayers; g.CoachId = d.CoachId; g.Color = d.Color; g.Description = d.Description?.Trim();
        }

        private static int Age(DateOnly b, DateOnly t) { var a = t.Year - b.Year; return b > t.AddYears(-a) ? a - 1 : a; }
    }
}
