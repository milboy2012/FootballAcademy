using Core.Enums;

namespace UI.Models.ViewModels.Coach
{
    public record AttendanceRowDto(Guid PlayerId, string LastName, string FirstName, int Age, bool MedicalValid, 
                    bool HasActiveSubscription, bool? Present, AbsenceReason? Reason, string? Comment, int AttendancePercent, string? ParentNotice);
}
