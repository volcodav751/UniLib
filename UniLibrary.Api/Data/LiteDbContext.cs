using LiteDB;
using UniLibrary.Api.Models;

namespace UniLibrary.Api.Data
{
    public class LiteDbContext : IDisposable
    {
        private readonly LiteDatabase _database;

        public LiteDbContext(string connectionString)
        {
            _database = new LiteDatabase(connectionString);

            Books = _database.GetCollection<Book>("books");

            Books.EnsureIndex(x => x.Title);
            Books.EnsureIndex(x => x.Author);
            Books.EnsureIndex(x => x.Category);
            Books.EnsureIndex(x => x.Isbn);
            Books.EnsureIndex(x => x.PublicationYear);
        }

        public ILiteCollection<Book> Books { get; }

        public void Dispose()
        {
            _database.Dispose();
        }
    }
}