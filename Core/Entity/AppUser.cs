using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class AppUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        // Временный пароль: при входе требуется смена.
        public bool MustChangePassword { get; set; }

        public string? MiddleName { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? AvatarPath { get; set; }          // /uploads/avatars/{id}.jpg
        public string? About { get; set; }
        public string? City { get; set; }
        public bool NotifyByEmail { get; set; } = true;

        //обратная навигация
        public Coach? Coach { get; set; }
        public ParentProfile? ParentProfile { get; set; }
        public Player? Player { get; set; }

        // Навигационные свойства
        public virtual ICollection<Post> Posts{ get; set; }
        //public virtual ICollection<Player> Players { get; set; }
        //public virtual ICollection<AppRole> UserRoles { get; set; }
    }
}
