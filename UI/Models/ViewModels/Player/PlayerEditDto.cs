namespace UI.Models.ViewModels.Player
{
    public class PlayerEditDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateOnly BirthDate { get; set; }
        public DateOnly? MedicalCertificateUntil { get; set; }
        public Guid ParentId { get; set; }
        public Guid? GroupId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }
    }
}
