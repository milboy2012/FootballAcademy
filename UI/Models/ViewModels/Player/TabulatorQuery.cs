namespace UI.Models.ViewModels.Player
{
    /// <summary>Параметры запроса Tabulator (ajaxURL + pagination=remote).</summary>
    public class TabulatorQuery
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 20;
        public string? Search { get; set; }
        public Guid? GroupId { get; set; }
        public bool? IsActive { get; set; }
        public string? SortField { get; set; }
        public string? SortDir { get; set; }   // "asc" | "desc"
    }
}
