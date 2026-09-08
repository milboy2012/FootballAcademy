using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Core.Entity.Subscription;

namespace Core.Entity
{
    public class TrainingPlan : BaseEntity
    {
        public string Name { get; set; } = null!;        // "Месяц", "Квартал", "8 занятий"
        public PlanType Type { get; set; }
        public PeriodUnit? Period { get; set; }          // для Period
        public int? Visits { get; set; }                 // для Visits
        public int VisitsValidDays { get; set; } = 60;   // срок «сгорания» занятий
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
