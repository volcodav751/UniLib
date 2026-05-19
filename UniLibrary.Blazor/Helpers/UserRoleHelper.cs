namespace UniLibrary.Blazor.Helpers;

public static class UserRoleHelper
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
    public const string Admin = "Admin";

    public static bool CanManageBooks(string? role)
    {
        return role == Teacher || role == Admin;
    }

    public static bool IsAdmin(string? role)
    {
        return role == Admin;
    }

    public static string GetDisplayName(string? role)
    {
        return role switch
        {
            Student => "Учень / студент",
            Teacher => "Викладач",
            Admin => "Адміністратор",
            _ => string.IsNullOrWhiteSpace(role) ? "Гість" : role
        };
    }
}
