using System.Drawing;

namespace UI.Models.ViewModels.Dashboard
{
    public record DashboardDto(
    // ключевые цифры
    int ActivePlayers, int NewPlayersMonth, int Groups, int Coaches, int Parents,
    int PendingSubscriptions, int ActiveSubscriptions, int ExpiringWeek, int PlayersWithoutSubscription, int MedicalExpired,
    decimal RevenueMonth, decimal RevenuePrevMonth,
    int TrainingsWeek, int TrainingsCompletedMonth, int TrainingsCancelledMonth, int AttendancePercentMonth,
    // ряды для графиков
    List<Point> RevenueByMonth, List<Point> AttendanceByWeek, List<NamedPoint> GroupFill, List<NamedPoint> AttendanceByGroup,
    List<NamedPoint> AbsenceReasons, List<NamedPoint> VenueLoad, List<NamedPoint> SkillAverages,
    // списки внимания
    List<AlertDto> Alerts);
}
