using Core.Enums;

namespace UI.Models.ViewModels.Coach
{
    public class AttendanceItemDto
    {
        public Guid PlayerId { get; set; }
        public bool Present { get; set; }
        public AbsenceReason? Reason { get; set; }
        public string? Comment { get; set; }
    }
}
