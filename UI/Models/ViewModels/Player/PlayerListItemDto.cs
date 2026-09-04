namespace UI.Models.ViewModels.Player
{
    public record PlayerListItemDto(
        Guid Id,
        string FirstName,
        string LastName,
        DateOnly BirthDate,
        int Age,
        string? GroupName,
        Guid? GroupId,
        string ParentName,
        Guid ParentId,
        DateOnly? MedicalCertificateUntil,
        bool IsActive);
}
