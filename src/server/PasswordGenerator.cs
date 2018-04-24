using System;
using System.Security.Cryptography;

namespace Domain0.Service
{
    public interface IPasswordGenerator
    {
        string GeneratePassword();

        bool CheckPassword(string password, string passwordHash);

        string HashPassword(string password);
    }

    public class PasswordGenerator : IPasswordGenerator
    {
        private readonly Random _random = new Random();

        public string GeneratePassword()
            => _random.Next(100000, 999999).ToString();

        public string GenerateSalt()
        {
            var salt = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        public string HashPassword(string password)
        {
            var salt = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(salt);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            var hash = pbkdf2.GetBytes(20);

            var result = new byte[36];
            Array.Copy(salt, 0, result, 0, 16);
            Array.Copy(hash, 0, result, 16, 20);
            return Convert.ToBase64String(result);
        }

        public bool CheckPassword(string password, string passwordHash)
        {
            var hashBytes = Convert.FromBase64String(passwordHash);
            var salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            var hash = pbkdf2.GetBytes(20);
            for (var i = 0; i < 20; i++)
                if (hashBytes[i + 16] != hash[i])
                    return false;

            return true;
        }
    }
}