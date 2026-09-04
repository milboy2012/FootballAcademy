using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class Venue : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public bool IsIndoor { get; set; }
        public int? Capacity { get; set; }          // макс. игроков одновременно
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;  // временно закрыто на ремонт и т.п.

        public ICollection<Training> Trainings { get; set; } = [];
    }
}
