namespace UI.Models.ViewModels.Schedule
{
    public class RecurrenceDto
    {
        public int[] Weekdays { get; set; } = [];       // 0=Вс … 6=Сб (как в JS)
        public DateOnly Until { get; set; }
    }
}
