namespace PlanningPoker.Api.Models
{
    public class User
    {
        [LiteDB.BsonId]
        public required string Username { get; set; } // Unique username (also used for sign-in and display)
        public required string Password { get; set; } // Plain text password
        public UserRole Role { get; set; } // User role (Admin or User)
    }

    public enum UserRole
    {
        User,
        Admin
    }
}
