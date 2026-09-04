namespace UI.Models.ViewModels.Group
{
    public record GroupListItemDto(Guid Id, string Name, string? Season, 
                                    int MinBirthYear, int MaxBirthYear, int MaxPlayers, 
                                    int PlayersCount, Guid CoachId, string CoachName, 
                                    string? Color, bool IsArchived, DateTime? ArchivedAt, int UpcomingTrainings);
}
