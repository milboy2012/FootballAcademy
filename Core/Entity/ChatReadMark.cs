using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class ChatReadMark : BaseEntity
    {
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ReadUpTo { get; set; }
    }
}
