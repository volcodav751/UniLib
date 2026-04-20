using LiteDB;

namespace UniLibrary.Api.Models
{
    public class Book
    {
        [BsonId]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int Year { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}