using UniLibrary.Api.Models;

namespace UniLibrary.Api.Repos;

public interface IBookRepository
{
    List<Book> GetAll();
    Book? GetById(int id);
    int GetNextId();
    void Add(Book book);
    void Update(Book book);
    bool Delete(int id);
}
