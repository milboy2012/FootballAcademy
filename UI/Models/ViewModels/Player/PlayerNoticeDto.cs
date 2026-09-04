using Core.Enums;

namespace UI.Models.ViewModels.Player
{
    public class PlayerNoticeDto
    {
        public Guid TrainingId { get; set; }
        public AbsenceReason Reason { get; set; } = AbsenceReason.Excused;
        public string? Comment { get; set; }
    }
}
