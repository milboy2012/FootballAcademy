using Core.Entity;

namespace UI.Services.Interfaces
{
    public interface INotificationService
    {
        //Уведомить участников события: тренеров групп, родителей и игроков с аккаунтом
        Task NotifyEventAsync(Training ev, string title, string message, CancellationToken ct);
        Task<int> UnreadCountAsync(Guid userId, CancellationToken ct);
    }
}
