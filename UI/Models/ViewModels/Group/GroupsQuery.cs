using UI.Models.ViewModels.Player;

namespace UI.Models.ViewModels.Group
{
    public class GroupsQuery : TabulatorQuery
    {
        public bool? Archived { get; set; }
        public Guid? CoachId { get; set; }
    }
}
