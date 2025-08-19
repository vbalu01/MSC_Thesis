using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace SmartHomeWeb.Lib
{
    public static class PasswordManager
    {
        public static string GenerateSalt(int size = 16)
        {
            var saltBytes = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 32
            ));
            return hashed;
        }

        public static string CombineSaltHash(string hash, string salt) => $"{salt}:{hash}";

        public static (string salt, string hash) SplitSaltHash(string combined)
        {
            var parts = combined.Split(':', 2);
            return (parts[0], parts[1]);
        }

        public static bool VerifyPassword(string password, string combined)
        {
            var (salt, hash) = SplitSaltHash(combined);
            var checkHash = HashPassword(password, salt);
            return checkHash == hash;
        }
    }
}
