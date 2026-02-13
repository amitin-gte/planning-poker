namespace PlanningPoker.Api.Models
{
    public class SignInRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class SignInResponse
    {
        public required string Token { get; set; }
        public required string Username { get; set; }
        public UserRole Role { get; set; }
    }

    public class UserListItem
    {
        public required string Username { get; set; }
        public UserRole Role { get; set; }
    }
}
