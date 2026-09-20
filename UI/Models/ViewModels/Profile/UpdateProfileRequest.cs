namespace UI.Models.ViewModels.Profile
{
    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string? Phone { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? City { get; set; }
        public string? About { get; set; }
        public bool NotifyByEmail { get; set; }
        public CoachProfileDto? Coach { get; set; }
        public ParentProfileDto? Parent { get; set; }
    }
}
