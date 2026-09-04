using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Coach;
using UI.Services.Interfaces;

namespace UI.Services
{
    public class CoachTrainingService : ICoachTrainingService
    {
        private readonly IUoW _data;
        public CoachTrainingService(IUoW data) => _data = data;

        public Task<Guid?> GetCoachIdAsync(Guid userId, CancellationToken ct)
            => _data.Coaches.Query().Where(c => c.UserId == userId).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);

        public Task<List<CoachGroupDto>> GetGroupsAsync(Guid coachId, CancellationToken ct)
            => _data.Groups.Query().AsNoTracking().Where(g => g.CoachId == coachId && !g.IsArchived).OrderBy(g => g.Name)
                .Select(g => new CoachGroupDto(g.Id, g.Name, g.Players.Count(p => p.IsActive), g.Color)).ToListAsync(ct);

        /// <summary>Ближайшие занятия + недавние без отметки посещаемости (чтобы не забыть заполнить).</summary>
        public async Task<List<UpcomingDto>> GetUpcomingAsync(Guid coachId, int days, CancellationToken ct)
        {
            var from = DateTime.UtcNow.AddDays(-7); var to = DateTime.UtcNow.AddDays(days);
            return await MyTrainings(coachId)
                .Where(t => t.StartsAt >= from && t.StartsAt <= to && t.Status != TrainingStatus.Cancelled)
                .OrderBy(t => t.StartsAt)
                .Select(t => new UpcomingDto(t.Id, t.StartsAt, t.EndsAt, t.Group.Name, t.Venue.Name, t.Kind, t.Status, t.Attendances.Any()))
                .ToListAsync(ct);
        }

        public async Task<(TrainingDetailsDto?, string?)> GetTrainingAsync(Guid trainingId, Guid coachId, CancellationToken ct)
        {
            var t = await MyTrainings(coachId).AsNoTracking()
                .Where(x => x.Id == trainingId)
                .Select(x => new
                {
                    x.Id,
                    x.Kind,
                    x.StartsAt,
                    x.EndsAt,
                    x.Status,
                    x.GroupId,
                    GroupName = x.Group.Name,
                    OpponentName = x.OpponentGroup != null ? x.OpponentGroup.Name : null,
                    VenueName = x.Venue.Name,
                    x.Note,
                    x.Summary,
                    x.Highlights,
                    x.CompletedAt
                }).FirstOrDefaultAsync(ct);
            if (t is null) return (null, "Тренировка не найдена или принадлежит другой группе");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var marks = await _data.Attendances.Query().AsNoTracking().Where(a => a.TrainingId == trainingId).ToDictionaryAsync(a => a.PlayerId, ct);

            var players = await _data.Players.Query().AsNoTracking()
                .Where(p => p.GroupId == t.GroupId && p.IsActive)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new
                {
                    p.Id,
                    p.LastName,
                    p.FirstName,
                    p.BirthDate,
                    p.MedicalCertificateUntil,
                    HasSub = p.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active && s.To >= today),
                    Total = p.Attendances.Count(a => a.Training.GroupId == t.GroupId && a.TrainingId != trainingId),
                    Present = p.Attendances.Count(a => a.Training.GroupId == t.GroupId && a.TrainingId != trainingId && a.Present)
                }).ToListAsync(ct);

            var rows = players.Select(p =>
            {
                marks.TryGetValue(p.Id, out var m);
                var age = today.Year - p.BirthDate.Year; if (p.BirthDate > today.AddYears(-age)) age--;
                return new AttendanceRowDto(p.Id, p.LastName, p.FirstName, age,
                    p.MedicalCertificateUntil is not null && p.MedicalCertificateUntil >= today, p.HasSub,
                    m?.Present, m?.Reason, m?.Comment, p.Total == 0 ? 0 : p.Present * 100 / p.Total);
            }).ToList();

            // игроки, которые были отмечены, но уже покинули группу — тоже показываем
            var gone = marks.Keys.Except(players.Select(p => p.Id)).ToList();
            if (gone.Count > 0)
            {
                var extra = await _data.Players.Query().AsNoTracking().Where(p => gone.Contains(p.Id))
                    .Select(p => new { p.Id, p.LastName, p.FirstName }).ToListAsync(ct);
                rows.AddRange(extra.Select(p => { var m = marks[p.Id]; return new AttendanceRowDto(p.Id, p.LastName, p.FirstName + " (выбыл)", 0, true, false, m.Present, m.Reason, m.Comment, 0); }));
            }

            return (new TrainingDetailsDto(t.Id, t.Kind, t.StartsAt, t.EndsAt, t.Status, t.GroupId, t.GroupName, t.OpponentName,
                t.VenueName, t.Note, t.Summary, t.Highlights, t.CompletedAt, rows), null);
        }

        public async Task<string?> ConductAsync(Guid trainingId, Guid coachId, ConductDto dto, CancellationToken ct)
        {
            var t = await MyTrainings(coachId).Include(x => x.Attendances).FirstOrDefaultAsync(x => x.Id == trainingId, ct);
            if (t is null) return "Тренировка не найдена или принадлежит другой группе";
            if (t.Status == TrainingStatus.Cancelled) return "Тренировка отменена";
            if (t.StartsAt > DateTime.UtcNow.AddMinutes(30)) return "Тренировка ещё не началась — отметить посещаемость можно за 30 минут до начала";

            var allowed = await _data.Players.Query().Where(p => p.GroupId == t.GroupId).Select(p => p.Id).ToHashSetAsync(ct);
            foreach (var id in t.Attendances.Select(a => a.PlayerId)) allowed.Add(id);
            if (dto.Attendance.Any(a => !allowed.Contains(a.PlayerId))) return "В списке есть игрок не из этой группы";
            if (dto.Attendance.GroupBy(a => a.PlayerId).Any(g => g.Count() > 1)) return "Игрок указан дважды";

            foreach (var item in dto.Attendance)
            {
                var a = t.Attendances.FirstOrDefault(x => x.PlayerId == item.PlayerId);
                if (a is null) { a = new Attendance { TrainingId = t.Id, PlayerId = item.PlayerId }; t.Attendances.Add(a); }
                a.Present = item.Present;
                a.Reason = item.Present ? null : item.Reason ?? AbsenceReason.Unknown;
                a.Comment = string.IsNullOrWhiteSpace(item.Comment) ? null : item.Comment.Trim();
            }

            t.Summary = dto.Summary?.Trim();
            t.Highlights = dto.Highlights?.Trim();
            if (dto.Complete)
            {
                var unmarked = allowed.Count(id => _data.Players.Query().Any(p => p.Id == id && p.IsActive && p.GroupId == t.GroupId) && dto.Attendance.All(a => a.PlayerId != id));
                if (unmarked > 0) return $"Не отмечено игроков: {unmarked}. Отметьте всех, чтобы завершить тренировку";
                t.Status = TrainingStatus.Completed;
                t.CompletedAt = DateTime.UtcNow;
            }
            await _data.SaveChangesAsync(ct);
            return null;
        }

        private IQueryable<Training> MyTrainings(Guid coachId) => _data.Trainings.Query().Where(t => t.Group.CoachId == coachId || (t.OpponentGroup != null && t.OpponentGroup.CoachId == coachId));
    }
}
