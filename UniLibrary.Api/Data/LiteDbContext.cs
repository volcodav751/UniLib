using LiteDB;
using UniLibrary.Api.Models;

namespace UniLibrary.Api.Data;

public class LiteDbContext : IDisposable
{
    private readonly LiteDatabase _database;

    public LiteDbContext(string connectionString)
    {
        _database = new LiteDatabase(connectionString);

        Books = _database.GetCollection<Book>("books");
        BookDocuments = _database.GetCollection<BsonDocument>("books");
        Users = _database.GetCollection<AppUser>("users");

        Books.EnsureIndex(x => x.Info.Title);
        Books.EnsureIndex(x => x.Info.Author);
        Books.EnsureIndex(x => x.Info.Category);
        Books.EnsureIndex(x => x.Info.Isbn);
        Books.EnsureIndex(x => x.Info.PublicationYear);
        Books.EnsureIndex(x => x.Inventory.IsDigital);
        Books.EnsureIndex(x => x.Inventory.AvailableCopies);

        Users.EnsureIndex(x => x.Email, unique: true);
        Users.EnsureIndex(x => x.Role);
    }

    public ILiteCollection<Book> Books { get; }

    public ILiteCollection<BsonDocument> BookDocuments { get; }

    public ILiteCollection<AppUser> Users { get; }

    public ILiteStorage<string> FileStorage => _database.FileStorage;

    public void Dispose()
    {
        _database.Dispose();
    }
}
