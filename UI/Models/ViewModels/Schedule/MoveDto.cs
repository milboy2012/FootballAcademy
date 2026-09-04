namespace UI.Models.ViewModels.Schedule
{
    public record MoveDto(DateTimeOffset Start, DateTimeOffset End, bool ApplyToSeries);
}
