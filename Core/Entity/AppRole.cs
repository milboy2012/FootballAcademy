using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class AppRole : IdentityRole<Guid>
    {
        public string Description { get; set; }
        public bool IsAdministration{ get; set; }
        public DateTime CreatedAt { get; set; }

        //public virtual ICollection<IdentityUserRole<Guid>> UserRoles { get; set; }
        
        
    }
}
