namespace UI.Models.ViewModels.Group
{
    public record GroupPlayerDto(Guid Id, string LastName, string FirstName, 
                                    DateOnly BirthDate, int Age, string ParentName, 
                                    string? ParentPhone, DateOnly? MedicalUntil, bool MedicalValid, 
                                    bool HasActiveSubscription, int AttendancePercent);
}
