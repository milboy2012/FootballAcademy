using Core.Entity;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using UI.Services.Interfaces;

namespace UI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUoW _data;
        public NotificationService(IUoW data) => _data = data;

        public async Task NotifyEventAsync(Training ev, string title, string message, CancellationToken ct)
        {
            var groupIds = new List<Guid> { ev.GroupId };
            if (ev.OpponentGroupId is Guid og) groupIds.Add(og);

            var coachUsers = await _data.Groups.Query().Where(g => groupIds.Contains(g.Id)).Select(g => g.Coach.UserId).ToListAsync(ct);
            var players = await _data.Players.Query().Where(p => p.GroupId != null && groupIds.Contains(p.GroupId.Value) && p.IsActive)
                .Select(p => new { p.ParentId, p.UserId }).ToListAsync(ct);

            var recipients = coachUsers
                .Concat(players.Select(p => p.ParentId))
                .Concat(players.Where(p => p.UserId != null).Select(p => p.UserId!.Value))
                .Distinct();

            var link = $"/Schedule?date={ev.StartsAt:yyyy-MM-dd}";
            await _data.Notifications.AddRangeAsync(recipients.Select(uid => new Notification { UserId = uid, Title = title, Message = message, Link = link }), ct);
            await _data.SaveChangesAsync(ct);
            // Точка расширения: здесь же можно отправить e-mail через IEmailSender.
        }

        public Task<int> UnreadCountAsync(Guid userId, CancellationToken ct)
            => _data.Notifications.Query().CountAsync(n => n.UserId == userId && n.ReadAt == null, ct);
    }
}
