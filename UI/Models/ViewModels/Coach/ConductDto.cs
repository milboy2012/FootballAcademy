namespace UI.Models.ViewModels.Coach
{
    public class ConductDto
    {
        public List<AttendanceItemDto> Attendance { get; set; } = [];
        public string? Summary { get; set; }
        public string? Highlights { get; set; }
        public bool Complete { get; set; }   // true — завершить тренировку, false — сохранить черновик
    }
}
