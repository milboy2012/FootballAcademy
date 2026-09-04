namespace UI.Models.ViewModels.Schedule
{
    public record ConflictDto(Guid EventId, string What, DateTime Start, DateTime End, string Where);
}
