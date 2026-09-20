namespace UI.Models.ViewModels.Profile
{
    public class ProfileDto
    {
        // общие
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string? Phone { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? City { get; set; }
        public string? About { get; set; }
        public string? AvatarUrl { get; set; }
        public bool NotifyByEmail { get; set; }
        public DateTime CreatedAt { get; set; }

        // ролевые
        public CoachProfileDto? Coach { get; set; }
        public ParentProfileDto? Parent { get; set; }
        public PlayerProfileDto? Player { get; set; }
    }
}
