using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    /// <summary>Тренировка — событие FullCalendar.</summary>
    public class Training : BaseEntity
    {
        public Guid GroupId { get; set; }
        public TrainingGroup Group { get; set; } = null!;

        public Guid VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        public DateTime StartsAt { get; set; }             // UTC
        public DateTime EndsAt { get; set; }               // UTC
        public TrainingStatus Status { get; set; } = TrainingStatus.Planned;
        public string? Note { get; set; }

        public EventKind Kind { get; set; } = EventKind.Training;
        public Guid? OpponentGroupId { get; set; }          // для матча
        public TrainingGroup? OpponentGroup { get; set; }
        public Guid? SeriesId { get; set; }                 // общий id для повторяющихся
        public string? CancelReason { get; set; }

        public string? Summary { get; set; }        // что отрабатывали
        public string? Highlights { get; set; }     // кто отличился
        public DateTime? CompletedAt { get; set; }

        public ICollection<Attendance> Attendances { get; set; } = [];
        public ICollection<AbsenceNotice> AbsenceNotices{ get; set; } = [];
    }
}
