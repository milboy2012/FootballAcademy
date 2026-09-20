using UI.Models.ViewModels.Parent;

namespace UI.Models.ViewModels.Coach
{
    public record TrainingAssessmentsDto(List<SkillDto> Skills, List<TrainingAssessmentRowDto> Players, bool IsCompleted);

}
