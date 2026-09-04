using Core.Enums;

namespace UI.Models.ViewModels.Coach
{
    public record TrainingDetailsDto(Guid Id, EventKind Kind, DateTime StartsAt, 
                                        DateTime EndsAt, TrainingStatus Status, Guid GroupId, 
                                        string GroupName, string? OpponentName, string VenueName, 
                                        string? Note, string? Summary, string? Highlights, 
                                        DateTime? CompletedAt, List<AttendanceRowDto> Players);
}
