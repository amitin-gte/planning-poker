namespace PlanningPoker.Api.Models
{
    public class User
    {
        [LiteDB.BsonId]
        public required string Username { get; set; } // Unique username (also used for sign-in and display)
        
        // Stores a hashed password using BCrypt. Never store or log plain text passwords.
        public required string Password { get; set; } // Hashed password
        
        public UserRole Role { get; set; } // User role (Admin or User)

        /// <summary>
        /// Hashes a plain text password using BCrypt with a secure, salted algorithm.
        /// </summary>
        /// <param name="password">The plain text password to hash.</param>
        /// <returns>The hashed password string suitable for storage.</returns>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifies a plain text password against a previously stored hash.
        /// </summary>
        /// <param name="password">The plain text password supplied by the user.</param>
        /// <param name="passwordHash">The stored password hash.</param>
        /// <returns><c>true</c> if the password matches the hash; otherwise, <c>false</c>.</returns>
        public static bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }

    public enum UserRole
    {
        User,
        Admin
    }
}
