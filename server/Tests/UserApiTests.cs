using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using PlanningPoker.Api.Models;
using Xunit;

namespace PlanningPoker.Tests
{
    public class UserApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public UserApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UsersAny_WhenNoUsers_Returns404()
        {
            var response = await _client.GetAsync("/users/any");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SignIn_FirstUser_GetsAdminRole()
        {
            var request = new SignInRequest
            {
                Username = "admin",
                Password = "adminpass"
            };

            var response = await _client.PostAsJsonAsync("/users/signin", request);
            Assert.True(response.IsSuccessStatusCode);

            var result = await response.Content.ReadFromJsonAsync<SignInResponse>();
            Assert.NotNull(result);
            Assert.Equal("admin", result.Username);
            Assert.Equal(UserRole.Admin, result.Role);
            Assert.NotNull(result.Token);
        }

        [Fact]
        public async Task SignIn_SecondUser_GetsUserRole()
        {
            // Create first user (admin)
            var adminRequest = new SignInRequest { Username = "admin1", Password = "pass1" };
            await _client.PostAsJsonAsync("/users/signin", adminRequest);

            // Create second user
            var userRequest = new SignInRequest { Username = "user1", Password = "pass2" };
            var response = await _client.PostAsJsonAsync("/users/signin", userRequest);
            
            Assert.True(response.IsSuccessStatusCode);
            var result = await response.Content.ReadFromJsonAsync<SignInResponse>();
            Assert.NotNull(result);
            Assert.Equal(UserRole.User, result.Role);
        }

        [Fact]
        public async Task UsersList_WithoutAuth_Returns401()
        {
            var response = await _client.GetAsync("/users/list");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UsersList_WithUserRole_Returns403()
        {
            // Create admin first
            var adminRequest = new SignInRequest { Username = "admin2", Password = "pass" };
            await _client.PostAsJsonAsync("/users/signin", adminRequest);

            // Create regular user
            var userRequest = new SignInRequest { Username = "user2", Password = "pass" };
            var userResponse = await _client.PostAsJsonAsync("/users/signin", userRequest);
            var userData = await userResponse.Content.ReadFromJsonAsync<SignInResponse>();

            // Try to access users list with user token
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userData.Token}");
            var response = await _client.GetAsync("/users/list");
            Assert.Equal((HttpStatusCode)403, response.StatusCode);
        }

        [Fact]
        public async Task UsersList_WithAdminRole_ReturnsUserList()
        {
            // Create admin user
            var adminRequest = new SignInRequest { Username = "admin3", Password = "pass" };
            var adminResponse = await _client.PostAsJsonAsync("/users/signin", adminRequest);
            var adminData = await adminResponse.Content.ReadFromJsonAsync<SignInResponse>();

            // Create another user
            var userRequest = new SignInRequest { Username = "user3", Password = "pass" };
            await _client.PostAsJsonAsync("/users/signin", userRequest);

            // Get users list with admin token
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminData.Token}");
            var response = await client.GetAsync("/users/list");
            
            Assert.True(response.IsSuccessStatusCode);
            var users = await response.Content.ReadFromJsonAsync<List<UserListItem>>();
            Assert.NotNull(users);
            Assert.True(users.Count >= 2);
            Assert.Contains(users, u => u.Username == "admin3" && u.Role == UserRole.Admin);
            Assert.Contains(users, u => u.Username == "user3" && u.Role == UserRole.User);
        }

        [Fact]
        public async Task DeleteUser_WithoutAuth_Returns401()
        {
            var response = await _client.DeleteAsync("/users/someuser");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_WithUserRole_Returns403()
        {
            // Create admin and user
            var adminRequest = new SignInRequest { Username = "admin4", Password = "pass" };
            await _client.PostAsJsonAsync("/users/signin", adminRequest);

            var userRequest = new SignInRequest { Username = "user4", Password = "pass" };
            var userResponse = await _client.PostAsJsonAsync("/users/signin", userRequest);
            var userData = await userResponse.Content.ReadFromJsonAsync<SignInResponse>();

            // Try to delete with user token
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userData.Token}");
            var response = await client.DeleteAsync("/users/admin4");
            Assert.Equal((HttpStatusCode)403, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_WithAdminRole_DeletesUser()
        {
            // Create admin
            var adminRequest = new SignInRequest { Username = "admin5", Password = "pass" };
            var adminResponse = await _client.PostAsJsonAsync("/users/signin", adminRequest);
            var adminData = await adminResponse.Content.ReadFromJsonAsync<SignInResponse>();

            // Create user to delete
            var userRequest = new SignInRequest { Username = "user5", Password = "pass" };
            await _client.PostAsJsonAsync("/users/signin", userRequest);

            // Delete user with admin token
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminData.Token}");
            var response = await client.DeleteAsync("/users/user5");
            Assert.True(response.IsSuccessStatusCode);
        }
    }
}
