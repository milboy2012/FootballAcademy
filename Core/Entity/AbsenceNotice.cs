using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class AbsenceNotice : BaseEntity
    {
        public Guid PlayerId { get; set; }
        public Player Player { get; set; } = null!;
        public Guid TrainingId { get; set; }
        public Training Training { get; set; } = null!;
        public AbsenceReason Reason { get; set; }
        public string? Comment { get; set; }
        public Guid CreatedByUserId { get; set; }       // родитель
    }
}
