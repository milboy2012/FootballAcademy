using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class Payment : BaseEntity
    {
        public Guid SubscriptionId { get; set; }
        public Subscription Subscription { get; set; } = null!;

        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public PaymentMethod Method { get; set; }
        public string? Comment { get; set; }
    }
}
