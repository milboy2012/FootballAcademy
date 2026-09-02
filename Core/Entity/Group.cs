using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; }                   // "U-9", "U-11"
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public Guid? CoachId { get; set; }                // Главный тренер группы
        public AppUser Coach { get; set; }
        public ICollection<Player> Players{ get; set; }  // Игроки группы
    }
}
