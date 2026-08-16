using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum GroupLevel
    {
        [Description("Начинающие")]
        Beginner,
        [Description("Продвинутые")]
        Intermediate,
        [Description("Профи")]
        Advanced,
        [Description("Элита")]
        Elite
    }
}
