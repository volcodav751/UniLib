using System.Security.Cryptography;
using System.Text;

namespace UniLibrary.Api.Services
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            using SHA256 sha256 = SHA256.Create();

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = sha256.ComputeHash(passwordBytes);

            return Convert.ToBase64String(hashBytes);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            string newHash = HashPassword(password);

            return newHash == storedHash;
        }
    }
}