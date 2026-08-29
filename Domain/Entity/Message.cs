using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    // Сообщение в чате между родителем и тренером
    public class Message : BaseEntity
    {
        public string Content { get; private set; }
        public DateTime SentAt { get; private set; }
        public bool IsRead { get; private set; }
        public DateTime? ReadAt { get; private set; }
        // Ссылка на файл
        public string? AttachmentUrl { get; private set; }

        // Внешние ключи
        // Чат идет в рамках игрока
        public Guid SenderId { get; private set; }
        public User Sender { get; private set; } = null!;

        
        public Guid PlayerId { get; private set; } 
        public Player Player { get; private set; } = null!;

        // EF Core
        private Message() { }

        public Message(Guid senderId, Guid playerId, string content, string? attachmentUrl = null)
        {
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrEmpty(attachmentUrl))
                throw new ArgumentException("Текст или вложение обязательно");

            SenderId = senderId;
            PlayerId = playerId;
            Content = content ?? string.Empty;
            AttachmentUrl = attachmentUrl;
            SentAt = DateTime.UtcNow;
            IsRead = false;
        }

        // Методы
        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}
