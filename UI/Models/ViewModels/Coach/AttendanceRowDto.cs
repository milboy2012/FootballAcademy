using Core.Enums;
using UI.Models.ViewModels.Subscription;
using UI.Services.Interfaces;

namespace UI.Models.ViewModels.Coach
{
    //public record AttendanceRowDto(Guid PlayerId, string LastName, string FirstName, int Age, bool MedicalValid, 
    //                bool HasActiveSubscription, bool? Present, AbsenceReason? Reason, string? Comment, int AttendancePercent, string? ParentNotice);
    public record AttendanceRowDto(Guid PlayerId, string LastName, string FirstName, int Age, bool MedicalValid,
                    SubscriptionStatusDto Subscription, bool? Present, AbsenceReason? Reason, string? Comment, int AttendancePercent, string? ParentNotice)
    {
        
    }
}
