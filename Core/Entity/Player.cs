using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class Player : BaseEntity
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateOnly BirthDate { get; set; }
        public DateOnly? MedicalCertificateUntil { get; set; }
        //public int Age => DateTime.UtcNow.Year - BirthDate.Year; // Вычисляемое поле (не хранить в БД)

        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }

        public Guid ParentId { get; set; }
        public AppUser Parent { get; set; } = null!;

        public Guid? GroupId { get; set; }
        public TrainingGroup? Group { get; set; }

        // Учётная запись ребёнка 
        public Guid? UserId { get; set; }
        public AppUser? User { get; set; }

        public ICollection<Attendance> Attendances { get; set; } = [];
        public ICollection<Subscription> Subscriptions { get; set; } = [];
        public ICollection<AbsenceNotice> AbsenceNotices{ get; set; } = [];
    }
}
