namespace UI.Models.ViewModels.Venue
{
    public class VenueDto
    {
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public bool IsIndoor { get; set; }
        public int? Capacity { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
