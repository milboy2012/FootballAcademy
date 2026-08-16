using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class Notification : BaseEntity
    {
        public string Title { get; private set; }
        public string Content { get; private set; }
        public NotificationType Type { get; private set; }
        public bool IsRead { get; private set; }
        public DateTime? ReadAt { get; private set; }
        // Ссылка для перехода
        public string? LinkUrl { get; private set; } 
        public DateTime SentAt { get; private set; }

        // Связи
        public Guid UserId { get; private set; }
        public User User { get; private set; } = null!;

        // EF Core
        private Notification() { } 

        public Notification(Guid userId, string title, string content, NotificationType type, string? linkUrl = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Заголовок уведомления обязателен");
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Содержимое уведомления обязательно");

            UserId = userId;
            Title = title;
            Content = content;
            Type = type;
            LinkUrl = linkUrl;
            SentAt = DateTime.UtcNow;
            IsRead = false;
        }

        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}
