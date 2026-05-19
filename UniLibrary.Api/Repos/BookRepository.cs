using UniLibrary.Api.Data;
using UniLibrary.Api.Models;

namespace UniLibrary.Api.Repos;

public class BookRepository : IBookRepository
{
    private readonly LiteDbContext _context;

    public BookRepository(LiteDbContext context)
    {
        _context = context;
    }

    public List<Book> GetAll()
    {
        return _context.Books.FindAll().ToList();
    }

    public Book? GetById(int id)
    {
        return _context.Books.FindById(id);
    }

    public int GetNextId()
    {
        return _context.Books.Count() == 0
            ? 1
            : _context.Books.Max(book => book.Id) + 1;
    }

    public void Add(Book book)
    {
        _context.Books.Insert(book);
    }

    public void Update(Book book)
    {
        _context.Books.Update(book);
    }

    public bool Delete(int id)
    {
        return _context.Books.Delete(id);
    }
}
