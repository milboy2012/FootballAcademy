using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entity
{
    public class Post : BaseEntity
    {
        public PostType Type { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;            // Markdown или простой текст
        public string? ImagePath { get; set; }
        public DateTime? EventDate { get; set; }             // для праздников и соревнований
        public string? Location { get; set; }
        public PostAudience Audience { get; set; } = PostAudience.Everyone;
        public bool IsPinned { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public Guid AuthorId { get; set; }
        public AppUser Author { get; set; } = null!;
        public Guid? GroupId { get; set; }                   // null = вся академия
        public TrainingGroup? Group { get; set; }
    }
}
