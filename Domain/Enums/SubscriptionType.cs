using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum SubscriptionType
    {
        [Description("Ежемесячны абонемент")]
        Monthly,
        [Description("Сезонный абонемент")]
        Seasonal,
        [Description("Разовое занятие")]
        OneTime
    }
}
