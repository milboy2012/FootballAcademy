namespace UI.Models.ViewModels.Profile
{
    public class CoachProfileDto
    {
        public string? Qualification { get; set; }
        public int? ExperienceYears { get; set; }
        public string? Bio { get; set; }
        public string? Achievements { get; set; }
        public DateOnly? HiredAt { get; set; }         // только чтение
        public List<string> Groups { get; set; } = []; // только чтение
    }
}
