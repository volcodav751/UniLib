namespace UniLibrary.Blazor.Models;

public class CatalogFilters
{
    public string SearchQuery { get; set; } = string.Empty;

    public List<string> SelectedFormats { get; set; } = new();
    public List<string> SelectedYears { get; set; } = new();
    public List<string> SelectedCategories { get; set; } = new();
    public List<string> SelectedLanguages { get; set; } = new();

    public bool OnlyAvailable { get; set; }
    public bool OnlyDigital { get; set; }
    public bool OnlyPhysical { get; set; }
    public bool OnlyRented { get; set; }
    public bool OnlyWithFile { get; set; }

    public bool HasAnyActiveFilter =>
        !string.IsNullOrWhiteSpace(SearchQuery)
        || SelectedFormats.Count > 0
        || SelectedYears.Count > 0
        || SelectedCategories.Count > 0
        || SelectedLanguages.Count > 0
        || OnlyAvailable
        || OnlyDigital
        || OnlyPhysical
        || OnlyRented
        || OnlyWithFile;
}
