using Core.Enums;

namespace UI.Models.ViewModels.Post
{
    public class PostEditDto
    {
        public PostType Type { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public DateTime? EventDate { get; set; }
        public string? Location { get; set; }
        public PostAudience Audience { get; set; }
        public bool IsPinned { get; set; }
        public bool IsPublished { get; set; }
        public Guid? GroupId { get; set; }
        public bool Notify { get; set; }
    }
}
