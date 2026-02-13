using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlanningPoker.Api.Repositories;
using PlanningPoker.Api.Services;

namespace PlanningPoker.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private static int _testDbCounter = 0;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing singleton registrations
                services.RemoveAll<UserRepository>();
                services.RemoveAll<RoomRepository>();
                services.RemoveAll<TokenService>();

                // Register with unique in-memory databases for each test run to avoid conflicts
                // Using a counter to ensure each factory instance gets a unique database filename
                var dbId = System.Threading.Interlocked.Increment(ref _testDbCounter);
                services.AddSingleton(new UserRepository($"Filename=:memory:TestUsers{dbId}"));
                services.AddSingleton(new RoomRepository($"Filename=:memory:TestRooms{dbId}"));
                services.AddSingleton<TokenService>();
            });
        }
    }
}
