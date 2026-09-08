using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enums
{
    public enum SubscriptionStatus
    {
        PendingPayment = 0,   // заявка создана родителем
        Active = 1,           // оплата подтверждена
        Expired = 2,          // срок вышел / занятия закончились
        Frozen = 3,
        Cancelled = 4         // отклонена / отменена
    }
}
