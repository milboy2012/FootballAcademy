using Core.Entity;
using Core.Enums;
using UI.Models.ViewModels.Subscription;

namespace UI.Services.Interfaces
{
    public record PlanDto(Guid Id, string Name, PlanType Type, PeriodUnit? Period, int? Visits, int VisitsValidDays, decimal Price, string? Description, bool IsActive, int SortOrder);
    public record SubscriptionDto(Guid Id, Guid PlayerId, string PlayerName, string PlanName, PlanType Type, SubscriptionStatus Status, DateOnly From, DateOnly To, decimal Price, int? TrainingsLimit, int TrainingsUsed, int? TrainingsLeft, int DaysLeft, string? ParentComment, string? ManagerComment, DateTime CreatedAt, DateTime? ConfirmedAt, decimal Paid);
    
    public class RequestSubscriptionDto { public Guid PlayerId { get; set; } public Guid PlanId { get; set; } public DateOnly? StartFrom { get; set; } public string? Comment { get; set; } }
    public class ConfirmDto { public decimal? Amount { get; set; } public PaymentMethod Method { get; set; } = PaymentMethod.Transfer; public string? Comment { get; set; } }
    public class ConfirmDtoReq
    {
        public decimal? Amount { get; set; }
        public int Method { get; set; }
        public string? Comment { get; set; }
    }

    public interface ISubscriptionService
    {
        Task<List<PlanDto>> GetPlansAsync(bool onlyActive, CancellationToken ct);
        Task<(Guid? Id, string? Error)> RequestAsync(Guid parentId, RequestSubscriptionDto dto, CancellationToken ct);
        Task<string?> CancelRequestAsync(Guid parentId, Guid subscriptionId, CancellationToken ct);
        Task<List<SubscriptionDto>> GetForPlayerAsync(Guid playerId, CancellationToken ct);
        Task<SubscriptionStatusDto> GetStatusAsync(Guid playerId, CancellationToken ct);
        Task<List<SubscriptionDto>> GetForManagerAsync(SubscriptionStatus? status, string? search, CancellationToken ct);
        Task<string?> ConfirmAsync(Guid managerId, Guid subscriptionId, ConfirmDto dto, CancellationToken ct);
        Task<string?> RejectAsync(Guid managerId, Guid subscriptionId, string? reason, CancellationToken ct);
        Task<string?> SetStatusAsync(Guid subscriptionId, SubscriptionStatus status, CancellationToken ct);
        //Вызывается при отметке присутствия. Возвращает ошибку, если отмечать нельзя.
        Task<(Subscription? Sub, string? Error)> TryConsumeAsync(Guid playerId, DateOnly trainingDate, CancellationToken ct);
        Task RefundAsync(Guid? subscriptionId, CancellationToken ct);
        Task ExpireOutdatedAsync(CancellationToken ct);
        Task<string?> ExtendAsync(Guid subscriptionId, DateOnly newTo, string? comment, CancellationToken ct);

    }
}
