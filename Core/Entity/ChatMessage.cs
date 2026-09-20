using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class ChatMessage : BaseEntity
    {
        public Guid GroupId { get; set; }
        public TrainingGroup Group { get; set; } = null!;
        public Guid SenderId { get; set; }
        public AppUser Sender { get; set; } = null!;
        public string Text { get; set; } = null!;
        public Guid? ReplyToId { get; set; }
        public ChatMessage? ReplyTo { get; set; }
        public bool IsEdited { get; set; }
    }
}
