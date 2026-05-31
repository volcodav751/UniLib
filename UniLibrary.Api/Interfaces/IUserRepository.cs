using UniLibrary.Api.Models;

namespace UniLibrary.Api.Interfaces;

public interface IUserRepository
{
    List<AppUser> GetAll();
    AppUser? GetById(int id);
    AppUser? GetByEmail(string email);
    bool EmailExists(string email);
    int CountAdmins();
    void Add(AppUser user);
    void Update(AppUser user);
    bool Delete(int id);
}
