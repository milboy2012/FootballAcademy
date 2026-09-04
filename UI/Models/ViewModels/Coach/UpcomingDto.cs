using Core.Enums;

namespace UI.Models.ViewModels.Coach
{
    public record UpcomingDto(Guid Id, DateTime StartsAt, DateTime EndsAt, string GroupName, 
                    string VenueName, EventKind Kind, TrainingStatus Status, bool HasAttendance);
}
