using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class Coach : BaseEntity
    {
        public Guid UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public string? Bio { get; set; }
        public string? Qualification { get; set; }
        public DateOnly? HiredAt { get; set; }

        public ICollection<TrainingGroup> Groups { get; set; } = [];
    }
}
