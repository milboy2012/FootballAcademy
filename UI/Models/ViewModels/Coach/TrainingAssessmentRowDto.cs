namespace UI.Models.ViewModels.Coach
{
    public record TrainingAssessmentRowDto(Guid PlayerId, string Name, bool Present, Dictionary<Guid, int>? Scores, string? Comment, Dictionary<Guid, double> Avg5);

    // средние за последние 5 тренировок — тренер видит динамику
}
