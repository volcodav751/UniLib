using LiteDB;

namespace UniLibrary.Api.Models
{
    public class Book
    {
        [BsonId]
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

        public bool IsAvailable { get; set; } = true;

        public bool IsDigital { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Метадані файлу
        public string? FileId { get; set; }

        public string? OriginalFileName { get; set; }

        public string? StoredFileName { get; set; }

        public string? ContentType { get; set; }

        public long? FileSizeBytes { get; set; }

        public DateTime? FileUploadedAt { get; set; }

        // Наприклад для прев’ю/обкладинки в майбутньому
        public string? CoverImageUrl { get; set; }
    }
}