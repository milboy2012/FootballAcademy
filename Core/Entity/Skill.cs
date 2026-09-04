using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class Skill : BaseEntity
    {
        public string Name { get; set; } = null!;        // "Дриблинг", "Пас", "Удар", "Скорость", "Выносливость", "Игровое мышление"
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
