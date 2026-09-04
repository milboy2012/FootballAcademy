using Core.Enums;

namespace UI.Models.ViewModels.Parent
{
    public record ChildBriefDto(Guid Id, string Name, string? GroupName, string? Color);

    public record AttendanceStatsDto(int Total, int Present, int Absent, int Percent,
        int Sick, int Excused, int Late, int Unknown);

    public record AttendanceHistoryRowDto(Guid TrainingId, DateTime StartsAt, string VenueName, EventKind Kind,
        bool Present, AbsenceReason? Reason, string? CoachComment, string? Highlights, bool NoticedInAdvance);

    public record AttendanceHistoryDto(AttendanceStatsDto Stats, List<MonthStatDto> ByMonth, List<AttendanceHistoryRowDto> Rows);
    public record MonthStatDto(string Month, int Total, int Present);

    public class AbsenceNoticeDto
    {
        public Guid PlayerId { get; set; }
        public Guid TrainingId { get; set; }
        public AbsenceReason Reason { get; set; } = AbsenceReason.Excused;
        public string? Comment { get; set; }
    }

    // --- успеваемость ---
    public record SkillDto(Guid Id, string Name, string? Description);
    public record AssessmentDto(Guid Id, DateOnly Date, string CoachName, string? Comment, Dictionary<Guid, int> Scores);
    public record ProgressDto(
        List<SkillDto> Skills,
        List<AssessmentDto> Assessments,          // по возрастанию даты
        AssessmentDto? SeasonStart,
        AssessmentDto? Latest,
        Dictionary<Guid, double>? GroupAverage,   // средние по группе на последней оценке — для сравнения
        string? Season);

    public class ParentDtos
    {
    }
}
