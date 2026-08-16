using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum AttendanceStatus
    {
        [Description("Пришел")]
        Present,
        [Description("Не явился")]
        Absent,
        [Description("Опоздал")]
        Failed,
        [Description("Болеет")]
        Refuned,
        [Description("Уважительная причина")]
        Cancelled
    }
}
