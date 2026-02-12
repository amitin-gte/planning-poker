var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register repositories and services as singletons for proper lifetime management
builder.Services.AddSingleton<PlanningPoker.Api.Repositories.RoomRepository>();
builder.Services.AddSingleton<PlanningPoker.Api.Repositories.UserRepository>();
builder.Services.AddSingleton<PlanningPoker.Api.Services.TokenService>();

// Configure JSON serialization to convert enums to strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Add CORS policy for local development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use CORS before other middleware (only in development)
if (app.Environment.IsDevelopment())
{
    app.UseCors();
}

app.UseHttpsRedirection();

// Minimal health endpoint to verify the service is running.
// TODO: Replace or extend this with domain-specific endpoints for Planning Poker.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// User authentication endpoints
app.MapPost("/users/signin", (
    PlanningPoker.Api.Repositories.UserRepository userRepo,
    PlanningPoker.Api.Services.TokenService tokenService,
    PlanningPoker.Api.Models.SignInRequest request) =>
{
    // Try to sign in with existing credentials
    var user = userRepo.SignIn(request.Username, request.Password);
    
    // If user not found, create a new user
    if (user == null)
    {
        // First user gets Admin role, subsequent users get User role
        var role = userRepo.Count() == 0 ? PlanningPoker.Api.Models.UserRole.Admin : PlanningPoker.Api.Models.UserRole.User;
        user = userRepo.Create(request.Username, request.Password, role);
        if (user == null)
        {
            // If user creation fails (e.g., username already exists), treat this as invalid credentials
            return Results.Json(new { error = "Invalid username or password" }, statusCode: 401);
        }
    }
    
    // Generate token
    var token = tokenService.GenerateToken(user);
    
    return Results.Ok(new PlanningPoker.Api.Models.SignInResponse
    {
        Token = token,
        Username = user.Username,
        Role = user.Role
    });
});

app.MapGet("/users/list", (
    PlanningPoker.Api.Repositories.UserRepository userRepo,
    PlanningPoker.Api.Services.TokenService tokenService,
    HttpRequest request) =>
{
    // Validate token and check admin role
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        return Results.Unauthorized();
    }

    var authHeaderValue = authHeader.ToString().Trim();
    const string bearerPrefix = "Bearer ";
    if (!authHeaderValue.StartsWith(bearerPrefix, System.StringComparison.OrdinalIgnoreCase))
    {
        return Results.Unauthorized();
    }

    var token = authHeaderValue.Substring(bearerPrefix.Length);
    var user = tokenService.ValidateToken(token);

    if (user == null)
    {
        return Results.Unauthorized();
    }

    if (user.Role != PlanningPoker.Api.Models.UserRole.Admin)
    {
        return Results.StatusCode(403);
    }
    
    var users = userRepo.List();
    var userList = users.Select(u => new PlanningPoker.Api.Models.UserListItem
    {
        Username = u.Username,
        Role = u.Role
    }).ToList();
    
    return Results.Ok(userList);
});

app.MapGet("/users/any", (PlanningPoker.Api.Repositories.UserRepository userRepo) =>
{
    var count = userRepo.Count();
    return count == 0 ? Results.NotFound() : Results.Ok();
});

app.MapDelete("/users/{username}", (
    PlanningPoker.Api.Repositories.UserRepository userRepo,
    PlanningPoker.Api.Services.TokenService tokenService,
    HttpRequest request,
    string username) =>
{
    // Validate token and check admin role
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        return Results.Unauthorized();
    }
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    var user = tokenService.ValidateToken(token);
    
    if (user == null || user.Role != PlanningPoker.Api.Models.UserRole.Admin)
    {
        return Results.StatusCode(403);
    }
    
    return userRepo.Delete(username) ? Results.Ok() : Results.NotFound();
});

// Room CRUD endpoints
app.MapPost("/rooms", (
    PlanningPoker.Api.Repositories.RoomRepository repo,
    PlanningPoker.Api.Services.TokenService tokenService,
    HttpRequest request,
    PlanningPoker.Api.Models.RoomConfig room) =>
{
    // Validate token
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        return Results.Unauthorized();
    }
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    var user = tokenService.ValidateToken(token);
    
    if (user == null)
    {
        return Results.Unauthorized();
    }
    
    var createdRoom = repo.Create(room);
    return Results.Created($"/rooms/{createdRoom.RoomId}", createdRoom);
});

app.MapPut("/rooms/{roomId}", (
    PlanningPoker.Api.Repositories.RoomRepository repo,
    PlanningPoker.Api.Services.TokenService tokenService,
    HttpRequest request,
    string roomId,
    PlanningPoker.Api.Models.RoomConfig room) =>
{
    // Validate token
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        return Results.Unauthorized();
    }
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    var user = tokenService.ValidateToken(token);
    
    if (user == null)
    {
        return Results.Unauthorized();
    }
    
    room.RoomId = roomId;
    return repo.Update(room) ? Results.Ok(room) : Results.NotFound();
});

app.MapGet("/rooms/{roomId}", (
    PlanningPoker.Api.Repositories.RoomRepository repo,
    PlanningPoker.Api.Services.TokenService tokenService,
    HttpRequest request,
    string roomId) =>
{
    // Validate token
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        return Results.Unauthorized();
    }
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    var user = tokenService.ValidateToken(token);
    
    if (user == null)
    {
        return Results.Unauthorized();
    }
    
    var room = repo.Get(roomId);
    return room != null ? Results.Ok(room) : Results.NotFound();
});

app.MapDelete("/rooms/{roomId}", (
    PlanningPoker.Api.Repositories.RoomRepository repo,
    PlanningPoker.Api.Services.TokenService tokenService,
    HttpRequest request,
    string roomId) =>
{
    // Validate token and check admin role
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        return Results.Unauthorized();
    }
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    var user = tokenService.ValidateToken(token);
    
    if (user == null || user.Role != PlanningPoker.Api.Models.UserRole.Admin)
    {
        return Results.StatusCode(403);
    }
    
    return repo.Delete(roomId) ? Results.Ok() : Results.NotFound();
});

app.MapGet("/rooms", (
    PlanningPoker.Api.Repositories.RoomRepository repo,
    PlanningPoker.Api.Services.TokenService tokenService,
    HttpRequest request) =>
{
    // Validate token and check admin role
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        return Results.Unauthorized();
    }
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    var user = tokenService.ValidateToken(token);
    
    if (user == null || user.Role != PlanningPoker.Api.Models.UserRole.Admin)
    {
        return Results.StatusCode(403);
    }
    
    return Results.Ok(repo.GetAll());
});

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }