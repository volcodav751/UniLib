namespace UniLibrary.Blazor.Models;

public class Book
{
    public int Id { get; set; }
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
    public bool IsAvailable { get; set; }
    public bool IsDigital { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? FileId { get; set; }
    public string? OriginalFileName { get; set; }
    public string? StoredFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime? FileUploadedAt { get; set; }

    public string? PreviewFileId { get; set; }
    public string? PreviewFileName { get; set; }
    public string? PreviewContentType { get; set; }
    public DateTime? PreviewGeneratedAt { get; set; }
    public string? PreviewStatus { get; set; }
    public string? PreviewError { get; set; }

    public string? CoverImageUrl { get; set; }
}
