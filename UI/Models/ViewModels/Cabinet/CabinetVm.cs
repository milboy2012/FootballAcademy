namespace UI.Models.ViewModels.Cabinet
{
    public class CabinetVm
    {
        public string ParentName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public bool ShowWelcome { get; set; }
        public List<ChildCardVm> Children { get; set; } = [];
    }
}
