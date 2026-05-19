namespace UniLibrary.Api.Services.Common;

public interface ICurrentUserService
{
    int? UserId { get; }
    bool IsInRole(string role);
}
