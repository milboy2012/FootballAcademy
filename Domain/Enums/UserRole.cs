using Domain.Entity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public class UserRole : BaseEntity
    {

        //[Description("Администратор")]
        //Administrator,
        //[Description("Тренер")]
        //Coach,
        //[Description("Родитель")]
        //Parent,
        //[Description("Игрок")]
        //Player
        public string Name { get; set; }
        public string NormalizedName { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Навигационное свойство
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        public UserRole() : base() { }

        public UserRole(string name, string description = null) 
        {
            Description = description;
            
        }

    }
}
