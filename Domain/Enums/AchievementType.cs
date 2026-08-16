using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum AchievementType
    {
        [Description("За посещаемость")]
        Attendance,
        [Description("За результаты")]
        Perfomance,
        [Description("За навыки")]
        Skills,
        [Description("Особые")]
        Special,
        [Description("Сезонные")]
        Season
    }
}
