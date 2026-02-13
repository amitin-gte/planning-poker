using PlanningPoker.Api.Models;
using PlanningPoker.Api.Services;
using Xunit;

namespace PlanningPoker.Tests
{
    public class TokenServiceTests
    {
        [Fact]
        public void GenerateToken_ReturnsNonEmptyToken()
        {
            var service = new TokenService();
            var user = new User { Username = "alice", Password = "pass", Role = UserRole.User };
            var token = service.GenerateToken(user);
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void GenerateToken_GeneratesUniqueTokens()
        {
            var service = new TokenService();
            var user1 = new User { Username = "alice", Password = "pass1", Role = UserRole.User };
            var user2 = new User { Username = "bob", Password = "pass2", Role = UserRole.Admin };
            var token1 = service.GenerateToken(user1);
            var token2 = service.GenerateToken(user2);
            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public void ValidateToken_ReturnsUser_WhenTokenIsValid()
        {
            var service = new TokenService();
            var user = new User { Username = "alice", Password = "pass", Role = UserRole.User };
            var token = service.GenerateToken(user);
            var validatedUser = service.ValidateToken(token);
            Assert.NotNull(validatedUser);
            Assert.Equal("alice", validatedUser.Username);
            Assert.Equal(UserRole.User, validatedUser.Role);
        }

        [Fact]
        public void ValidateToken_ReturnsNull_WhenTokenIsInvalid()
        {
            var service = new TokenService();
            var user = service.ValidateToken("invalid-token");
            Assert.Null(user);
        }

        [Fact]
        public void RevokeToken_RemovesToken()
        {
            var service = new TokenService();
            var user = new User { Username = "alice", Password = "pass", Role = UserRole.User };
            var token = service.GenerateToken(user);
            var revoked = service.RevokeToken(token);
            Assert.True(revoked);
            var validatedUser = service.ValidateToken(token);
            Assert.Null(validatedUser);
        }

        [Fact]
        public void RevokeToken_ReturnsFalse_WhenTokenDoesNotExist()
        {
            var service = new TokenService();
            var revoked = service.RevokeToken("nonexistent-token");
            Assert.False(revoked);
        }

        [Fact]
        public void Clear_RemovesAllTokens()
        {
            var service = new TokenService();
            var user1 = new User { Username = "alice", Password = "pass1", Role = UserRole.User };
            var user2 = new User { Username = "bob", Password = "pass2", Role = UserRole.Admin };
            var token1 = service.GenerateToken(user1);
            var token2 = service.GenerateToken(user2);
            
            service.Clear();
            
            Assert.Null(service.ValidateToken(token1));
            Assert.Null(service.ValidateToken(token2));
        }
    }
}
