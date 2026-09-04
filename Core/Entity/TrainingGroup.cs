using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class TrainingGroup : BaseEntity
    {
        public string Name { get; set; } = null!;          // "U-8", "U-10"
        public int MinBirthYear { get; set; }
        public int MaxBirthYear { get; set; }
        public int MaxPlayers { get; set; } = 16;
        public string? Color { get; set; }                 // цвет событий в FullCalendar, "#3788d8"

        public Guid CoachId { get; set; }
        public Coach Coach { get; set; } = null!;

        public string? Season { get; set; }          // "2026/2027"
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string? Description { get; set; }

        public ICollection<Player> Players { get; set; } = [];
        [InverseProperty(nameof(Training.Group))]
        public ICollection<Training> Trainings { get; set; } = [];
    }
}
