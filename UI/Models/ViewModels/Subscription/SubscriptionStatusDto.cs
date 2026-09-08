using Core.Enums;

namespace UI.Models.ViewModels.Subscription
{
    public record SubscriptionStatusDto(bool IsValid, string Text, SubscriptionStatus? Status, DateOnly? To, int? TrainingsLeft, bool HasPending);
}
