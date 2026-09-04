namespace UI.Models.ViewModels.My
{
    public class PlayerHomeVm
    {
        public string Name { get; set; } = null!;
        public string? GroupName { get; set; }
        public string? CoachName { get; set; }
        public List<UpcomingVm> Upcoming { get; set; } = [];
        public int TotalTrainings { get; set; }
        public int Visited { get; set; }
        public int AttendancePercent => TotalTrainings == 0 ? 0 : Visited * 100 / TotalTrainings;
    }
}
