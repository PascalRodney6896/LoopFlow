using System;
using System.Security.Cryptography;
using System.Text;

namespace LoopFlow.Services
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32;  // 256 bits
        private const int Iterations = 10000;

        public static string HashPassword(string password, out string salt)
        {
            byte[] saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            salt = Convert.ToBase64String(saltBytes);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] key = pbkdf2.GetBytes(KeySize);
                return Convert.ToBase64String(key);
            }
        }

        public static string HashPasswordWithSalt(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] key = pbkdf2.GetBytes(KeySize);
                return Convert.ToBase64String(key);
            }
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                return false;

            try
            {
                string computedHash = HashPasswordWithSalt(password, storedSalt);
                if (SlowEquals(Convert.FromBase64String(computedHash), Convert.FromBase64String(storedHash)))
                {
                    return true;
                }

                // Check trimmed password fallback
                string trimmed = password.Trim();
                if (trimmed != password)
                {
                    string computedHashTrimmed = HashPasswordWithSalt(trimmed, storedSalt);
                    if (SlowEquals(Convert.FromBase64String(computedHashTrimmed), Convert.FromBase64String(storedHash)))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
    }
}
