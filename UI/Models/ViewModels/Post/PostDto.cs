using Core.Enums;

namespace UI.Models.ViewModels.Post
{
    public record PostDto(Guid Id, PostType Type, string Title, 
                            string Body, string? ImagePath, DateTime? EventDate, 
                            string? Location, PostAudience Audience, bool IsPinned, 
                            bool IsPublished, DateTime? PublishedAt, string AuthorName, 
                            Guid? GroupId, string? GroupName, DateTime CreatedAt);

}
