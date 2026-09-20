namespace UI.Models.ViewModels.Coach
{
    public class PlayerAssessmentDto
    {
        public Guid PlayerId { get; set; }
        public Dictionary<Guid, int> Scores { get; set; } = [];   // skillId -> 1..10; пусто = не оценивать
        public string? Comment { get; set; }
    }
}
