namespace UI.Models.ViewModels.Player
{
    public record TabulatorPage<T>(IEnumerable<T> Data, int Last_Page, int Total);
}
