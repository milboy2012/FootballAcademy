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

        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public decimal Price { get; set; }
        public int? TrainingsLimit { get; set; }           // null = безлимит
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        public ICollection<Payment> Payments { get; set; } = [];
    }
}
