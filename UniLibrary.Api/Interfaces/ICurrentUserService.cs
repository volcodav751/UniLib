namespace UniLibrary.Api.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    bool IsInRole(string role);
}
