namespace UI.Models.ViewModels.Player
{
    public record PlayerHomeDto(string FirstName, string LastName, int Age, 
                                string? GroupName, string? GroupColor, string? CoachName, 
                                int Total, int Present, int Percent, int Streak, DateTime? NextTraining, 
                                string? NextVenue, string? LastHighlights, int SeasonAssessments);
}
