namespace UI.Models.ViewModels.Chat
{
    public record ChatGroupDto(Guid Id, string Name, string? Color, string CoachName, int Members, int Unread, string? LastText, DateTime? LastAt);

}
