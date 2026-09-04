using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    /// <summary>Посещаемость. Составной уникальный индекс (TrainingId, PlayerId).</summary>
    public class Attendance : BaseEntity
    {
        public Guid TrainingId { get; set; }
        public Training Training { get; set; } = null!;

        public Guid PlayerId { get; set; }
        public Player Player { get; set; } = null!;

        public bool Present { get; set; }
        public string? Comment { get; set; }
        public AbsenceReason? Reason { get; set; }  // null если присутствовал

    }
}
