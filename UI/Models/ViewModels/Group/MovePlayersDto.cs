namespace UI.Models.ViewModels.Group
{
    public record MovePlayersDto(Guid[] PlayerIds, Guid? TargetGroupId); // null = отчислить (без группы)

}
