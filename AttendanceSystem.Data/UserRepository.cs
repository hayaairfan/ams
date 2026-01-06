using AttendanceSystem.Core.Models;
using System.Security.Cryptography;


using AttendanceSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AttendanceSystem.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public User? GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return _context.Users.FirstOrDefault(u => u.Email == email.Trim());
        }

        public void AddUser(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public void UpdateUser(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public bool ValidatePassword(string email, string password)
        {
            var user = GetUserByEmail(email);
            if (user == null) return false;

            return VerifyPassword(password, user.PasswordHash);
        }

        public bool UpdatePassword(string email, string newPassword)
        {
            var user = GetUserByEmail(email);
            if (user == null) return false;

            user.PasswordHash = HashPassword(newPassword);
            user.MustChangePassword = false;

            UpdateUser(user);
            return true;
        }

        public bool UserExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return _context.Users.Any(u => u.Email == email.Trim());
        }

        public List<User> GetAllUsers()
        {
            return _context.Users.ToList();
        }

        // --- Helpers: secure PBKDF2 hashing and verification ---
        private static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            const int iterations = 10000;
            const int saltSize = 16;
            const int hashSize = 32;

            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[saltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(hashSize);

            // Format: iterations.salt.hash (all base64)
            return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrWhiteSpace(storedHash)) return false;

            var parts = storedHash.Split('.', 3);
            if (parts.Length != 3) return false;

            if (!int.TryParse(parts[0], out int iterations)) return false;
            var salt = Convert.FromBase64String(parts[1]);
            var hash = Convert.FromBase64String(parts[2]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(hash.Length);

            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }
    }
}