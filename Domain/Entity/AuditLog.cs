using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    // Лог действий пользователей в системе
    public class AuditLog : BaseEntity
    {
        // Create, Update, Delete, Login и т.д.
        public string Action { get; private set; }
        // Player, Group, Payment и т.д.
        public string EntityName { get; private set; }
        // ID изменяемой сущности
        public string? EntityId { get; private set; }
        // Старое значение (JSON)
        public string? OldValue { get; private set; }
        // Новое значение (JSON)
        public string? NewValue { get; private set; }    
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // Внешние ключи
        public Guid? UserId { get; private set; }
        public User? User { get; private set; }

        private AuditLog() { } // Для EF Core

        public AuditLog(string action, string entityName, string? entityId = null, string? oldValue = null, string? newValue = null, Guid? userId = null, string? ipAddress = null, string? userAgent = null)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Действие обязательно");
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Имя сущности обязательно");

            Action = action;
            EntityName = entityName;
            EntityId = entityId;
            OldValue = oldValue;
            NewValue = newValue;
            UserId = userId;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
