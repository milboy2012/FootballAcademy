using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum PaymentStatus
    {
        [Description("1-")]
        Pending,
        [Description("2-")]
        Paid,
        [Description("3-")]
        Failed,
        [Description("4-")]
        Refunded,
        [Description("5-")]
        Cancelled
    }
}
