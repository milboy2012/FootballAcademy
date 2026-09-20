namespace UI.Models.DataModels
{
    public class AppTime
    {
        public static TimeZoneInfo Tz()
        {
            TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Belarus Standard Time" : "Europe/Minsk");
            return Tz;
        }
    }
}
