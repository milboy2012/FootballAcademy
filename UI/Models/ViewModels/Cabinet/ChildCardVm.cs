namespace UI.Models.ViewModels.Cabinet
{
    public class ChildCardVm
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public DateOnly BirthDate { get; set; }
        public string? GroupName { get; set; }
        public string? CoachName { get; set; }
        public DateOnly? MedicalUntil { get; set; }
        public bool IsActive { get; set; }
        public DateOnly? ActiveSubscriptionUntil { get; set; }
        public DateTime? NextTraining { get; set; }
        public string? Login { get; set; }
        public bool? AccountActive { get; set; }
        public int Age
        {
            get
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var age = today.Year - BirthDate.Year;
                return BirthDate > today.AddYears(-age) ? age - 1 : age;
            }
        }
        public bool MedicalExpired => MedicalUntil is null || MedicalUntil < DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
