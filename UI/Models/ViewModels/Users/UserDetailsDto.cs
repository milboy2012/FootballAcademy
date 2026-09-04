namespace UI.Models.ViewModels.Users
{
    public record UserDetailsDto(Guid Id, string LastName, string FirstName, string Email, string? Phone,
    string Role, bool IsActive, bool MustChangePassword, DateTime CreatedAt,
    int ChildrenCount, int GroupsCount, string? LinkedPlayer);
}
