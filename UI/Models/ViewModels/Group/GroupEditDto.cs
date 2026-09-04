namespace UI.Models.ViewModels.Group
{
    public class GroupEditDto
    {
        public string Name { get; set; } = null!;
        public string? Season { get; set; }
        public int MinBirthYear { get; set; }
        public int MaxBirthYear { get; set; }
        public int MaxPlayers { get; set; } = 16;
        public Guid CoachId { get; set; }
        public string? Color { get; set; }
        public string? Description { get; set; }
    }
}
