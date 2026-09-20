namespace UI.Models.ViewModels.Profile
{
    public class PlayerProfileDto
    {
        public string? GroupName { get; set; }
        public string? CoachName { get; set; }
        public string ParentName { get; set; } = null!;
        public DateOnly? MedicalUntil { get; set; }
    }
}
