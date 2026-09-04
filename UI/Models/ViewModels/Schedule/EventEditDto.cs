using Core.Enums;

namespace UI.Models.ViewModels.Schedule
{
    public class EventEditDto
    {
        public EventKind Kind { get; set; } = EventKind.Training;
        public Guid GroupId { get; set; }
        public Guid? OpponentGroupId { get; set; }
        public Guid VenueId { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public string? Note { get; set; }
        public RecurrenceDto? Recurrence { get; set; }
        public bool SkipConflicts { get; set; }        // при серии: пропустить занятые слоты
        public bool NotifyParticipants { get; set; } = true;
    }
}
