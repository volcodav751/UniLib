namespace UniLibrary.Blazor.Models.Requests;

public class CreateBookRequest
{
    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string Publisher { get; set; } = string.Empty;

    public int PublicationYear { get; set; }

    public int PageCount { get; set; }

    public string Isbn { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();


    public bool IsDigital { get; set; } = false;

    public int TotalCopies { get; set; } = 1;

    public string? CoverImageUrl { get; set; }
}