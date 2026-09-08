using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class Subscription : BaseEntity
    {
        public Guid PlayerId { get; set; }
        public Player Player { get; set; } = null!;
        public Guid PlanId { get; set; }
        public TrainingPlan Plan { get; set; } = null!;

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.PendingPayment;
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public decimal Price { get; set; }
        public int? TrainingsLimit { get; set; }           // null = безлимит
        public int TrainingsUsed { get; set; }

        public Guid RequestedByUserId { get; set; }       // родитель
        public string? ParentComment { get; set; }        // "оплатил переводом 05.09"
        public Guid? ConfirmedByUserId { get; set; }      // менеджер
        public DateTime? ConfirmedAt { get; set; }
        public string? ManagerComment { get; set; }


        //public enum PlanType { Period = 0, Visits = 1 }
        //public enum PeriodUnit { Month = 1, Quarter = 3, Year = 12 }

        public ICollection<Payment> Payments { get; set; } = [];
        public ICollection<Attendance> Attendances { get; set; } = [];

    }
}
