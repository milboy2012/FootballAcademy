using Core.Entity;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Player;
using UI.Services.Interfaces;

namespace UI.Services
{

    public class PlayerService : IPlayerService
    {
        private readonly IUoW _data;
        public PlayerService(IUoW data)
        {
            _data = data;
        }

        public async Task<Guid> CreateAsync(PlayerEditDto dto, CancellationToken ct)
        {
            var p = new Player();
            Apply(p, dto);
            await _data.Players.AddAsync(p, ct);
            await _data.SaveChangesAsync(ct);
            return p.Id;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            
            var p = await _data.Players.GetByIdAsync(id, ct);
            if (p is null) return false;

            _data.Players.Delete(p);          // SaveChangesAsync превратит в soft delete
            await _data.SaveChangesAsync(ct);
            return true;
        }

        public async Task<PlayerListItemDto?> GetAsync(Guid id, CancellationToken ct)
        {
            //var item = await Project(_data.Players.AsNoTracking().Where(p => p.Id == id)).FirstOrDefaultAsync(ct);
            var item = Project(_data.Players.Query().FirstOrDefault(s=>s.Id == id));
            
            return item is null ? null : item with { Age = CalcAge(item.BirthDate, DateOnly.FromDateTime(DateTime.UtcNow)) };
        }

        public async Task<TabulatorPage<PlayerListItemDto>> GetPageAsync(TabulatorQuery q, Guid? parentOnly, CancellationToken ct)
        {
            var query =  _data.Players.Query();

            if (parentOnly is not null) query = query.Where(p => p.ParentId == parentOnly);
            if (q.GroupId is not null) query = query.Where(p => p.GroupId == q.GroupId);
            if (q.IsActive is not null) query = query.Where(p => p.IsActive == q.IsActive);
            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = $"%{q.Search.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.LastName, s) ||
                    EF.Functions.ILike(p.FirstName, s) ||
                    EF.Functions.ILike(p.Parent.LastName, s));
            }

            var desc = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
            query = q.SortField switch
            {
                "firstName" => desc ? query.OrderByDescending(p => p.FirstName) : query.OrderBy(p => p.FirstName),
                "birthDate" => desc ? query.OrderByDescending(p => p.BirthDate) : query.OrderBy(p => p.BirthDate),
                "groupName" => desc ? query.OrderByDescending(p => p.Group!.Name) : query.OrderBy(p => p.Group!.Name),
                "parentName" => desc ? query.OrderByDescending(p => p.Parent.LastName) : query.OrderBy(p => p.Parent.LastName),
                "medicalCertificateUntil" => desc ? query.OrderByDescending(p => p.MedicalCertificateUntil) : query.OrderBy(p => p.MedicalCertificateUntil),
                _ => desc ? query.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName)
                          : query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            };

            var size = Math.Clamp(q.Size, 1, 200);
            var page = Math.Max(q.Page, 1);
            var total = await query.CountAsync(ct);

            var items = await Project(query.Skip((page - 1) * size).Take(size)).ToListAsync(ct);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            items = items.Select(i => i with { Age = CalcAge(i.BirthDate, today) }).ToList();

            return new TabulatorPage<PlayerListItemDto>(items, Math.Max(1, (int)Math.Ceiling(total / (double)size)), total);
        }

        public async Task<bool> UpdateAsync(Guid id, PlayerEditDto dto, CancellationToken ct)
        {
            var p = await _data.Players.GetByIdAsync(id, ct);
            if (p is null) return false;
            Apply(p, dto);
            await _data.SaveChangesAsync(ct);
            return true;
        }


        private static void Apply(Player p, PlayerEditDto d)
        {
            p.FirstName = d.FirstName.Trim();
            p.LastName = d.LastName.Trim();
            p.BirthDate = d.BirthDate;
            p.MedicalCertificateUntil = d.MedicalCertificateUntil;
            p.ParentId = d.ParentId;
            p.GroupId = d.GroupId;
            p.IsActive = d.IsActive;
            p.Note = d.Note?.Trim();
        }

        //private static IQueryable<PlayerListItemDto> Project(IQueryable<Player> src)
        private static IQueryable<PlayerListItemDto> Project(IQueryable<Player> src)
        {
            
            return src.Select(p => new PlayerListItemDto(
            p.Id, p.FirstName, p.LastName, p.BirthDate,
            0, // возраст досчитаем на клиенте/после materialize
            p.Group != null ? p.Group.Name : null, p.GroupId,
            p.Parent.LastName + " " + p.Parent.FirstName, p.ParentId,
            p.MedicalCertificateUntil, p.IsActive));
        }
        private static PlayerListItemDto Project(Player player)
        {
            if (player == null) return null;

            return new PlayerListItemDto(
                player.Id,
                player.FirstName,
                player.LastName,
                player.BirthDate,
                0,
                player.Group?.Name,
                player.GroupId,
                player.Parent.LastName + " " + player.Parent.FirstName,
                player.ParentId,
                player.MedicalCertificateUntil,
                player.IsActive);
        }


        private static int CalcAge(DateOnly birth, DateOnly today)
        {
            var age = today.Year - birth.Year;
            if (birth > today.AddYears(-age)) age--;
            return age;
        }
    }
}
