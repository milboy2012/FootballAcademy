using Core.Entity;
using Core.Enums;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using UI.Models.ViewModels.Player;

namespace UI.Services
{
    public class PlayerCabinetService : IPlayerCabinetService
    {
        private static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        private readonly IUoW _data;
        public PlayerCabinetService(IUoW data) => _data = data;

        public Task<Guid?> GetPlayerIdAsync(Guid userId, CancellationToken ct)
            => _data.Players.Query().Where(p => p.UserId == userId && p.IsActive).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);

        public async Task<PlayerHomeDto?> GetHomeAsync(Guid playerId, CancellationToken ct)
        {
            var now = DateTime.UtcNow; var today = DateOnly.FromDateTime(now);
            var p = await _data.Players.Query().AsNoTracking().Where(x => x.Id == playerId).Select(x => new
            {
                x.FirstName,
                x.LastName,
                x.BirthDate,
                x.GroupId,
                GroupName = x.Group != null ? x.Group.Name : null,
                GroupColor = x.Group != null ? x.Group.Color : null,
                GroupSeason = x.Group != null ? x.Group.Season : null,
                CoachName = x.Group != null ? x.Group.Coach.User.FirstName + " " + x.Group.Coach.User.LastName : null
            }).FirstOrDefaultAsync(ct);
            if (p is null) return null;

            // посещаемость по завершённым тренировкам, в хронологическом порядке (для серии)
            var marks = await _data.Attendances.Query().AsNoTracking()
                .Where(a => a.PlayerId == playerId && a.Training.Status == TrainingStatus.Completed)
                .OrderByDescending(a => a.Training.StartsAt)
                .Select(a => new { a.Present, a.Training.Highlights }).ToListAsync(ct);
            var total = marks.Count; var present = marks.Count(m => m.Present);
            var streak = marks.TakeWhile(m => m.Present).Count();
            var lastHighlights = marks.FirstOrDefault(m => m.Present && m.Highlights != null)?.Highlights;

            var next = p.GroupId is null ? null : await _data.Trainings.Query().AsNoTracking()
                .Where(t => (t.GroupId == p.GroupId || t.OpponentGroupId == p.GroupId) && t.Status == TrainingStatus.Planned && t.StartsAt >= now)
                .OrderBy(t => t.StartsAt).Select(t => new { t.StartsAt, Venue = t.Venue.Name }).FirstOrDefaultAsync(ct);

            var assessments = await _data.SkillAssessments.Query().CountAsync(a => a.PlayerId == playerId && (p.GroupSeason == null || a.Season == p.GroupSeason), ct);

            var age = today.Year - p.BirthDate.Year; if (p.BirthDate > today.AddYears(-age)) age--;
            return new PlayerHomeDto(p.FirstName, p.LastName, age, p.GroupName, p.GroupColor, p.CoachName,
                total, present, total == 0 ? 0 : present * 100 / total, streak, next?.StartsAt, next?.Venue, lastHighlights, assessments);
        }

        public async Task<List<PlayerUpcomingDto>> GetUpcomingAsync(Guid playerId, int days, CancellationToken ct)
        {
            var groupId = await _data.Players.Query().Where(p => p.Id == playerId).Select(p => p.GroupId).FirstOrDefaultAsync(ct);
            if (groupId is null) return [];
            var from = DateTime.UtcNow; var to = from.AddDays(days);

            var notices = await _data.AbsenceNotices.Query().AsNoTracking()
                .Where(n => n.PlayerId == playerId && n.Training.StartsAt >= from)
                .Select(n => new { n.TrainingId, n.Reason, ByPlayer = n.Player.UserId == n.CreatedByUserId })
                .ToDictionaryAsync(n => n.TrainingId, ct);

            var rows = await _data.Trainings.Query().AsNoTracking()
                .Where(t => (t.GroupId == groupId || t.OpponentGroupId == groupId) && t.StartsAt >= from && t.StartsAt <= to && t.Status != TrainingStatus.Completed)
                .OrderBy(t => t.StartsAt)
                .Select(t => new
                {
                    t.Id,
                    t.Kind,
                    t.StartsAt,
                    t.EndsAt,
                    VenueName = t.Venue.Name,
                    t.Venue.Address,
                    t.Status,
                    t.CancelReason,
                    OpponentName = t.OpponentGroupId == groupId ? t.Group.Name : (t.OpponentGroup != null ? t.OpponentGroup.Name : null)
                }).ToListAsync(ct);

            return rows.Select(r =>
            {
                notices.TryGetValue(r.Id, out var n);
                return new PlayerUpcomingDto(r.Id, r.Kind, r.StartsAt, r.EndsAt, r.VenueName, r.Address, r.OpponentName, r.Status, r.CancelReason,
                    n is not null, n is null ? null : n.ByPlayer ? "ты" : "родитель", n?.Reason);
            }).ToList();
        }

        public async Task<string?> NoticeAsync(Guid playerId, Guid userId, PlayerNoticeDto dto, CancellationToken ct)
        {
            var player = await _data.Players.Query().FirstAsync(p => p.Id == playerId, ct);
            var t = await _data.Trainings.Query().Include(x => x.Group).FirstOrDefaultAsync(x => x.Id == dto.TrainingId, ct);
            if (t is null) return "Занятие не найдено";
            if (t.Status != TrainingStatus.Planned) return "Занятие отменено или уже прошло";
            if (t.StartsAt <= DateTime.UtcNow) return "Занятие уже началось";
            if (player.GroupId != t.GroupId && player.GroupId != t.OpponentGroupId) return "Это занятие не твоей группы";

            var n = await _data.AbsenceNotices.Query().FirstOrDefaultAsync(x => x.PlayerId == playerId && x.TrainingId == dto.TrainingId, ct);
            if (n is null) { n = new AbsenceNotice { PlayerId = playerId, TrainingId = dto.TrainingId, CreatedByUserId = userId }; await _data.AbsenceNotices.AddAsync(n); }
            n.Reason = dto.Reason == AbsenceReason.Unknown ? AbsenceReason.Excused : dto.Reason;
            n.Comment = dto.Comment?.Trim();

            // уведомляем тренера и родителя
            var coachUserId = await _data.Groups.Query().Where(g => g.Id == t.GroupId).Select(g => g.Coach.UserId).FirstAsync(ct);
            var when = TimeZoneInfo.ConvertTimeFromUtc(t.StartsAt, Tz).ToString("dd.MM HH:mm");
            var text = $"{player.FirstName} {player.LastName} сообщил, что не придёт на {t.Group.Name} {when}. Причина: {ReasonName(n.Reason)}{(n.Comment is null ? "" : $" — {n.Comment}")}";
            await _data.Notifications.AddAsync(new Notification { UserId = coachUserId, Title = "Игрок предупредил о пропуске", Message = text, Link = $"/Coach/Training/{t.Id}" }, ct);
            await _data.Notifications.AddAsync(new Notification { UserId = player.ParentId, Title = "Ребёнок предупредил о пропуске", Message = text, Link = "/Parent/Schedule" }, ct);
            await _data.SaveChangesAsync(ct);
            return null;
        }

        public async Task<string?> WithdrawAsync(Guid playerId, Guid userId, Guid trainingId, CancellationToken ct)
        {
            var n = await _data.AbsenceNotices.Query().FirstOrDefaultAsync(x => x.PlayerId == playerId && x.TrainingId == trainingId, ct);
            if (n is null) return "Предупреждения нет";
            if (n.CreatedByUserId != userId) return "Это предупреждение отправил родитель — отменить его может только он";
            _data.AbsenceNotices.Delete(n); await _data.SaveChangesAsync(ct);
            return null;
        }

        private static string ReasonName(AbsenceReason r) => r switch { AbsenceReason.Sick => "болею", AbsenceReason.Late => "опоздаю", _ => "не смогу прийти" };
    }
}
