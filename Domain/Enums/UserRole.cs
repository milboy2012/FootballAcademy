using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum UserRole
    {
        
        [Description("Администратор")]
        Administrator,
        [Description("Тренер")]
        Coach,
        [Description("Родитель")]
        Parent,
        [Description("Игрок")]
        Player
    }
}
