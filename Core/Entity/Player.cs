using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class Player : AppUser
    {
        public DateTime BirthDate { get; set; }           // Дата рождения
        public int Age => DateTime.UtcNow.Year - BirthDate.Year; // Вычисляемое поле (не хранить в БД)

        // Медицинские данные (шифруются!)
        public string? MedicalNotes { get; set; }          // Аллергии, травмы
        public string? EmergencyContact { get; set; }      // Телефон родителя (дубль)

        // Внешние ключи
        public Guid? GroupId { get; set; }                 // Может быть без группы (на пробе)
        public Guid? ParentId { get; set; }                 // Ссылка на родителя (User)
        public Guid? CoachId { get; set; }                 // Персональный тренер (если есть)

        // Навигационные свойства
        public Group Group { get; set; }
        public AppUser Parent { get; set; }
        public AppUser Coach { get; set; }

        // Аудит
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }        
    }
}
