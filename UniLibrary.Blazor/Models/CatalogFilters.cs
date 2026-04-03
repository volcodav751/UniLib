namespace UniLibrary.Blazor.Models;

public class CatalogFilters
{
    public string SearchQuery { get; set; } = string.Empty;

    public List<string> SelectedTypes { get; set; } = new();
    public List<string> SelectedYears { get; set; } = new();
    public List<string> SelectedCategories { get; set; } = new();
}