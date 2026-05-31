using UniLibrary.Api.Data;
using UniLibrary.Api.Models;
using UniLibrary.Api.Interfaces;

namespace UniLibrary.Api.Repos;

public class UserRepository : IUserRepository
{
    private readonly LiteDbContext _context;

    public UserRepository(LiteDbContext context)
    {
        _context = context;
    }

    public List<AppUser> GetAll()
    {
        return _context.Users.FindAll().ToList();
    }

    public AppUser? GetById(int id)
    {
        return _context.Users.FindById(id);
    }

    public AppUser? GetByEmail(string email)
    {
        return _context.Users.FindOne(user => user.Email == email);
    }

    public bool EmailExists(string email)
    {
        return _context.Users.Exists(user => user.Email == email);
    }

    public int CountAdmins()
    {
        return _context.Users.Count(user => user.Role == UserRoles.Admin);
    }

    public void Add(AppUser user)
    {
        _context.Users.Insert(user);
    }

    public void Update(AppUser user)
    {
        _context.Users.Update(user);
    }

    public bool Delete(int id)
    {
        return _context.Users.Delete(id);
    }
}
