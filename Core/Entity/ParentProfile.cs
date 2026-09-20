using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class ParentProfile : BaseEntity
    {
        public Guid UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public string? SecondPhone { get; set; }
        public string? EmergencyContactName { get; set; }   // "Бабушка, Мария Ивановна"
        public string? EmergencyContactPhone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }                   // особенности, пожелания
    }
}
