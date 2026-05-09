using System.Security.Cryptography;
using System.Text;

namespace UniLibrary.Api.Services
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;
        private const string Prefix = "PBKDF2";

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (storedHash.StartsWith($"{Prefix}$", StringComparison.Ordinal))
            {
                return VerifyPbkdf2Password(password, storedHash);
            }

            return VerifyLegacySha256Password(password, storedHash);
        }

        private static bool VerifyPbkdf2Password(string password, string storedHash)
        {
            string[] parts = storedHash.Split('$');

            if (parts.Length != 4 || parts[0] != Prefix)
            {
                return false;
            }

            if (!int.TryParse(parts[1], out int iterations))
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expectedHash = Convert.FromBase64String(parts[3]);
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length
            );

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        private static bool VerifyLegacySha256Password(string password, string storedHash)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = sha256.ComputeHash(passwordBytes);
            string newHash = Convert.ToBase64String(hashBytes);
            return newHash == storedHash;
        }
    }
}
