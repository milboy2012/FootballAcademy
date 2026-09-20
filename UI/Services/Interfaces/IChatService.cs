using System.Security.Claims;
using UI.Models.DataModels;
using UI.Models.ViewModels.Chat;

namespace UI.Services.Interfaces
{
    public interface IChatService
    {
        Task<List<ChatGroupDto>> MyGroupsAsync(ClaimsPrincipal user, CancellationToken ct);
        Task<List<ChatMessageDto>> HistoryAsync(ClaimsPrincipal user, Guid groupId, DateTime? before, int take, CancellationToken ct);
        Task<(ChatMessageDto? Msg, string? Error)> SendAsync(ClaimsPrincipal user, Guid groupId, string text, Guid? replyToId, CancellationToken ct);
        Task<(ChatMessageDto? Msg, string? Error)> EditAsync(Guid userId, Guid messageId, string text, CancellationToken ct);
        Task<(Guid GroupId, string? Error)> DeleteAsync(ClaimsPrincipal user, Guid messageId, CancellationToken ct);
        Task MarkReadAsync(Guid userId, Guid groupId, CancellationToken ct);
        Task<List<object>> MembersAsync(ClaimsPrincipal user, Guid groupId, CancellationToken ct);
    }
}
