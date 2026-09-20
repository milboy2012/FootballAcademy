namespace UI.Models.DataModels
{
    public record ChatMessageDto(Guid Id, Guid GroupId, Guid SenderId, string SenderName, string SenderRole, string? SenderAvatar,
    string Text, DateTime CreatedAt, bool IsEdited, Guid? ReplyToId, string? ReplyToText, string? ReplyToSender);

}
