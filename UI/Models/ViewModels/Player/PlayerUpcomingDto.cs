using Core.Enums;

namespace UI.Models.ViewModels.Player
{
    public record PlayerUpcomingDto(Guid Id, EventKind Kind, DateTime StartsAt, 
                                        DateTime EndsAt, string VenueName,string? VenueAddress, 
                                        string? OpponentName, TrainingStatus Status, string? CancelReason, 
                                        bool Noticed, string? NoticedBy, AbsenceReason? NoticeReason);
}
