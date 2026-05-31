using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Services.Results;

namespace UniLibrary.Api.Interfaces;

public interface IBookService
{
    List<Book> GetAll();
    ServiceResult<Book> GetById(int id);
    List<Book> GetBooksWithActiveRentals();
    ServiceResult<Book> Create(CreateBookRequest request);
    ServiceResult<Book> Update(int id, CreateBookRequest request);
    ServiceResult<Book> StaffRentBook(int id, StaffCreateRentalRequest request);
    ServiceResult<Book> StaffReturnBook(int id, int rentalId, StaffReturnRentalRequest? request = null);
    ServiceResult Delete(int id);
}
