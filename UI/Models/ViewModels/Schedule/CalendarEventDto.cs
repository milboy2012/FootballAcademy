namespace UI.Models.ViewModels.Schedule
{
    // Событие в формате FullCalendar
    public record CalendarEventDto(Guid Id, string Title, DateTime Start, DateTime End, string? Color, string? TextColor, object ExtendedProps);
}
