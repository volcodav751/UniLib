namespace UniLibrary.Api.Models.Requests
{
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

        public bool IsAvailable { get; set; } = true;

        public bool IsDigital { get; set; } = false;

        public string? CoverImageUrl { get; set; }
    }
}