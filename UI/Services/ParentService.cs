using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Parent;
using UI.Services.Interfaces;

namespace UI.Services
{
    public class ParentService : IParentService
    {
        private readonly IUoW _data;
        private readonly INotificationService _notify;
        public ParentService(IUoW data, INotificationService notify) { _data = data; _notify = notify; }

        public Task<List<ChildBriefDto>> GetChildrenAsync(Guid parentId, CancellationToken ct)
            => _data.Players.Query().AsNoTracking().Where(p => p.ParentId == parentId && p.IsActive).OrderBy(p => p.BirthDate)
                .Select(p => new ChildBriefDto(p.Id, p.FirstName + " " + p.LastName, p.Group != null ? p.Group.Name : null, p.Group != null ? p.Group.Color : null))
                .ToListAsync(ct);

        public Task<bool> OwnsAsync(Guid parentId, Guid playerId, CancellationToken ct)
            => _data.Players.AnyAsync(p => p.Id == playerId && p.ParentId == parentId, ct);

        // ---------- посещаемость ----------

        public async Task<AttendanceHistoryDto> GetAttendanceAsync(Guid playerId, DateOnly? from, DateOnly? to, CancellationToken ct)
        {
            var q = _data.Attendances.Query().AsNoTracking().Where(a => a.PlayerId == playerId && a.Training.Status == TrainingStatus.Completed);
            if (from is not null) q = q.Where(a => a.Training.StartsAt >= from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            if (to is not null) q = q.Where(a => a.Training.StartsAt < to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

            var noticed = await _data.AbsenceNotices.Query().Where(n => n.PlayerId == playerId).Select(n => n.TrainingId).ToHashSetAsync(ct);

            var rows = (await q.OrderByDescending(a => a.Training.StartsAt)
                .Select(a => new { a.TrainingId, a.Training.StartsAt, VenueName = a.Training.Venue.Name, a.Training.Kind, a.Present, a.Reason, a.Comment, a.Training.Highlights })
                .ToListAsync(ct))
                .Select(a => new AttendanceHistoryRowDto(a.TrainingId, a.StartsAt, a.VenueName, a.Kind, a.Present, a.Reason, a.Comment, a.Highlights, noticed.Contains(a.TrainingId)))
                .ToList();

            var total = rows.Count; var present = rows.Count(r => r.Present);
            var stats = new AttendanceStatsDto(total, present, total - present, total == 0 ? 0 : present * 100 / total,
                rows.Count(r => r.Reason == AbsenceReason.Sick), rows.Count(r => r.Reason == AbsenceReason.Excused),
                rows.Count(r => r.Reason == AbsenceReason.Late), rows.Count(r => !r.Present && (r.Reason is null or AbsenceReason.Unknown)));

            var byMonth = rows.GroupBy(r => new { r.StartsAt.Year, r.StartsAt.Month }).OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthStatDto($"{g.Key.Year}-{g.Key.Month:00}", g.Count(), g.Count(r => r.Present))).ToList();

            return new AttendanceHistoryDto(stats, byMonth, rows);
        }

        public async Task<string?> NoticeAbsenceAsync(Guid parentId, AbsenceNoticeDto dto, CancellationToken ct)
        {
            if (!await OwnsAsync(parentId, dto.PlayerId, ct)) return "Ребёнок не найден";
            var t = await _data.Trainings.Query().Include(x => x.Group).FirstOrDefaultAsync(x => x.Id == dto.TrainingId, ct);
            if (t is null) return "Тренировка не найдена";
            if (t.Status != TrainingStatus.Planned) return "Тренировка уже прошла или отменена";
            if (t.StartsAt <= DateTime.UtcNow) return "Тренировка уже началась";
            var player = await _data.Players.Query().FirstAsync(p => p.Id == dto.PlayerId, ct);
            if (player.GroupId != t.GroupId && player.GroupId != t.OpponentGroupId) return "Ребёнок не участвует в этой тренировке";

            var existing = await _data.AbsenceNotices.Query().FirstOrDefaultAsync(n => n.PlayerId == dto.PlayerId && n.TrainingId == dto.TrainingId, ct);
            if (existing is null) { existing = new AbsenceNotice { PlayerId = dto.PlayerId, TrainingId = dto.TrainingId, CreatedByUserId = parentId }; await _data.AbsenceNotices.AddAsync(existing, ct); }
            existing.Reason = dto.Reason == AbsenceReason.Unknown ? AbsenceReason.Excused : dto.Reason;
            existing.Comment = dto.Comment?.Trim();
            await _data.SaveChangesAsync(ct);

            // уведомить тренера
            var coachUserId = await _data.Groups.Query().Where(g => g.Id == t.GroupId).Select(g => g.Coach.UserId).FirstAsync(ct);
            await _data.Notifications.AddAsync(new Notification
            {
                UserId = coachUserId,
                Title = "Предупреждение о пропуске",
                Message = $"{player.LastName} {player.FirstName} пропустит {t.Group.Name} {TimeZoneInfo.ConvertTimeFromUtc(t.StartsAt, Tz):dd.MM HH:mm}. Причина: {ReasonName(existing.Reason)}{(existing.Comment is null ? "" : $" — {existing.Comment}")}",
                Link = $"/Coach/Training/{t.Id}"
            }, ct);
            await _data.SaveChangesAsync(ct);
            return null;
        }

        public async Task<string?> WithdrawNoticeAsync(Guid parentId, Guid playerId, Guid trainingId, CancellationToken ct)
        {
            if (!await OwnsAsync(parentId, playerId, ct)) return "Ребёнок не найден";
            var n = await _data.AbsenceNotices.Query().FirstOrDefaultAsync(x => x.PlayerId == playerId && x.TrainingId == trainingId, ct);
            if (n is null) return "Предупреждение не найдено";
            _data.AbsenceNotices.Delete(n); await _data.SaveChangesAsync(ct);
            return null;
        }

        public Task<List<Guid>> GetNoticedTrainingIdsAsync(Guid playerId, CancellationToken ct)
            => _data.AbsenceNotices.Query().Where(n => n.PlayerId == playerId && n.Training.StartsAt >= DateTime.UtcNow).Select(n => n.TrainingId).ToListAsync(ct);

        // ---------- успеваемость ----------

        public async Task<ProgressDto> GetProgressAsync(Guid playerId, string? season, CancellationToken ct)
        {
            var skills = await _data.Skills.Query().AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.SortOrder)
                .Select(s => new SkillDto(s.Id, s.Name, s.Description)).ToListAsync(ct);

            var q = _data.SkillAssessments.Query().AsNoTracking().Where(a => a.PlayerId == playerId);
            season ??= await _data.Players.Query().Where(p => p.Id == playerId).Select(p => p.Group != null ? p.Group.Season : null).FirstOrDefaultAsync(ct);
            if (season is not null) q = q.Where(a => a.Season == season);

            var list = await q.OrderBy(a => a.Date)
                .Select(a => new AssessmentDto(a.Id, a.Date, a.Coach.User.LastName + " " + a.Coach.User.FirstName, a.Comment,
                    a.Scores.ToDictionary(s => s.SkillId, s => s.Value)))
                .ToListAsync(ct);

            Dictionary<Guid, double>? groupAvg = null;
            var groupId = await _data.Players.Query().Where(p => p.Id == playerId).Select(p => p.GroupId).FirstOrDefaultAsync(ct);
            if (groupId is not null && list.Count > 0)
            {
                // последняя оценка каждого игрока группы → среднее по навыкам
                var latestPerPlayer = await _data.SkillAssessments.Query().AsNoTracking()
                    .Where(a => a.Player.GroupId == groupId && (season == null || a.Season == season))
                    .GroupBy(a => a.PlayerId).Select(g => g.OrderByDescending(a => a.Date).First().Id).ToListAsync(ct);
                groupAvg = await _data.SkillScores.Query().Where(s => latestPerPlayer.Contains(s.AssessmentId))
                    .GroupBy(s => s.SkillId).Select(g => new { g.Key, Avg = g.Average(s => s.Value) })
                    .ToDictionaryAsync(x => x.Key, x => Math.Round(x.Avg, 1), ct);
            }

            return new ProgressDto(skills, list, list.FirstOrDefault(), list.LastOrDefault(), groupAvg, season);
        }

        private static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        private static string ReasonName(AbsenceReason r) => r switch { AbsenceReason.Sick => "болезнь", AbsenceReason.Late => "опоздание", _ => "по семейным обстоятельствам" };
    }
}
