namespace UI.Models.ViewModels.Schedule
{
    public record CreateResult(int Created, List<ConflictDto> Skipped);
}
