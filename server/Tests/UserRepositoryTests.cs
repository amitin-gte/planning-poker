using PlanningPoker.Api.Models;
using PlanningPoker.Api.Repositories;
using Xunit;

namespace PlanningPoker.Tests
{
    public class UserRepositoryTests
    {
        private UserRepository GetRepo()
        {
            // Use in-memory LiteDB for tests
            return new UserRepository("Filename=:memory:");
        }

        [Fact]
        public void Create_CreatesNewUser()
        {
            using var repo = GetRepo();
            var user = repo.Create("alice", "password123", UserRole.User);
            Assert.NotNull(user);
            Assert.Equal("alice", user.Username);
            // Password should be hashed, not plain text
            Assert.NotEqual("password123", user.Password);
            Assert.True(User.VerifyPassword("password123", user.Password));
            Assert.Equal(UserRole.User, user.Role);
        }

        [Fact]
        public void Create_ReturnsNull_WhenUsernameExists()
        {
            using var repo = GetRepo();
            repo.Create("alice", "password123", UserRole.User);
            var duplicate = repo.Create("alice", "different", UserRole.Admin);
            Assert.Null(duplicate);
        }

        [Fact]
        public void SignIn_ReturnsUser_WhenCredentialsMatch()
        {
            using var repo = GetRepo();
            repo.Create("alice", "password123", UserRole.User);
            var user = repo.SignIn("alice", "password123");
            Assert.NotNull(user);
            Assert.Equal("alice", user.Username);
            Assert.Equal(UserRole.User, user.Role);
        }

        [Fact]
        public void SignIn_ReturnsNull_WhenPasswordIncorrect()
        {
            using var repo = GetRepo();
            repo.Create("alice", "password123", UserRole.User);
            var user = repo.SignIn("alice", "wrongpassword");
            Assert.Null(user);
        }

        [Fact]
        public void SignIn_ReturnsNull_WhenUserNotFound()
        {
            using var repo = GetRepo();
            var user = repo.SignIn("nonexistent", "password");
            Assert.Null(user);
        }

        [Fact]
        public void List_ReturnsAllUsers()
        {
            using var repo = GetRepo();
            repo.Create("alice", "pass1", UserRole.User);
            repo.Create("bob", "pass2", UserRole.Admin);
            repo.Create("charlie", "pass3", UserRole.User);

            var users = repo.List();
            Assert.Equal(3, users.Count);
            Assert.Contains(users, u => u.Username == "alice" && u.Role == UserRole.User);
            Assert.Contains(users, u => u.Username == "bob" && u.Role == UserRole.Admin);
            Assert.Contains(users, u => u.Username == "charlie" && u.Role == UserRole.User);
        }

        [Fact]
        public void List_ReturnsEmptyList_WhenNoUsers()
        {
            using var repo = GetRepo();
            var users = repo.List();
            Assert.Empty(users);
        }

        [Fact]
        public void Delete_RemovesUser_WhenUserExists()
        {
            using var repo = GetRepo();
            repo.Create("alice", "password123", UserRole.User);
            var deleted = repo.Delete("alice");
            Assert.True(deleted);
            var user = repo.SignIn("alice", "password123");
            Assert.Null(user);
        }

        [Fact]
        public void Delete_ReturnsFalse_WhenUserNotFound()
        {
            using var repo = GetRepo();
            var deleted = repo.Delete("nonexistent");
            Assert.False(deleted);
        }

        [Fact]
        public void Count_ReturnsCorrectCount()
        {
            using var repo = GetRepo();
            Assert.Equal(0, repo.Count());
            repo.Create("alice", "pass1", UserRole.User);
            Assert.Equal(1, repo.Count());
            repo.Create("bob", "pass2", UserRole.Admin);
            Assert.Equal(2, repo.Count());
            repo.Delete("alice");
            Assert.Equal(1, repo.Count());
        }
    }
}
