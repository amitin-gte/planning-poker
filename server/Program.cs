using PlanningPoker.Api.Models;
using PlanningPoker.Api.Repositories;
using PlanningPoker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register repositories and services as singletons for proper lifetime management
builder.Services.AddSingleton<RoomRepository>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<TokenService>();

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

// Helper function to validate and extract user from authorization header
static IResult? ValidateAuth(HttpRequest request, TokenService tokenService, UserRole? requiredRole, out User? user)
{
    user = null;
    
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
    user = tokenService.ValidateToken(token);

    if (user == null)
    {
        return Results.Unauthorized();
    }

    if (requiredRole.HasValue && user.Role != requiredRole.Value)
    {
        return Results.StatusCode(403);
    }
    
    return null; // No error, validation succeeded
}

// Minimal health endpoint to verify the service is running.
// TODO: Replace or extend this with domain-specific endpoints for Planning Poker.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// User authentication endpoints
app.MapPost("/users/signin", (
    UserRepository userRepo,
    TokenService tokenService,
    SignInRequest request) =>
{
    // Try to sign in with existing credentials
    var user = userRepo.SignIn(request.Username, request.Password);
    
    // If user not found, create a new user
    if (user == null)
    {
        // First user gets Admin role, subsequent users get User role
        var role = userRepo.Count() == 0 ? UserRole.Admin : UserRole.User;
        user = userRepo.Create(request.Username, request.Password, role);
        if (user == null)
        {
            // If user creation fails (e.g., username already exists), treat this as invalid credentials
            return Results.Json(new { error = "Invalid username or password" }, statusCode: 401);
        }
    }
    
    // Generate token
    var token = tokenService.GenerateToken(user);
    
    return Results.Ok(new SignInResponse
    {
        Token = token,
        Username = user.Username,
        Role = user.Role
    });
});

app.MapGet("/users/list", (
    UserRepository userRepo,
    TokenService tokenService,
    HttpRequest request) =>
{
    var authResult = ValidateAuth(request, tokenService, UserRole.Admin, out var user);
    if (authResult != null) return authResult;
    
    var users = userRepo.List();
    var userList = users.Select(u => new UserListItem
    {
        Username = u.Username,
        Role = u.Role
    }).ToList();
    
    return Results.Ok(userList);
});

app.MapGet("/users/any", (UserRepository userRepo) =>
{
    var count = userRepo.Count();
    return count == 0 ? Results.NotFound() : Results.Ok();
});

app.MapDelete("/users/{username}", (
    UserRepository userRepo,
    TokenService tokenService,
    HttpRequest request,
    string username) =>
{
    var authResult = ValidateAuth(request, tokenService, UserRole.Admin, out var user);
    if (authResult != null) return authResult;
    
    return userRepo.Delete(username) ? Results.Ok() : Results.NotFound();
});

// Room CRUD endpoints
app.MapPost("/rooms", (
    RoomRepository repo,
    TokenService tokenService,
    HttpRequest request,
    RoomConfig room) =>
{
    var authResult = ValidateAuth(request, tokenService, null, out var user);
    if (authResult != null) return authResult;
    
    var createdRoom = repo.Create(room);
    return Results.Created($"/rooms/{createdRoom.RoomId}", createdRoom);
});

app.MapPut("/rooms/{roomId}", (
    RoomRepository repo,
    TokenService tokenService,
    HttpRequest request,
    string roomId,
    RoomConfig room) =>
{
    var authResult = ValidateAuth(request, tokenService, null, out var user);
    if (authResult != null) return authResult;
    
    room.RoomId = roomId;
    return repo.Update(room) ? Results.Ok(room) : Results.NotFound();
});

app.MapGet("/rooms/{roomId}", (
    RoomRepository repo,
    TokenService tokenService,
    HttpRequest request,
    string roomId) =>
{
    var authResult = ValidateAuth(request, tokenService, null, out var user);
    if (authResult != null) return authResult;
    
    var room = repo.Get(roomId);
    return room != null ? Results.Ok(room) : Results.NotFound();
});

app.MapDelete("/rooms/{roomId}", (
    RoomRepository repo,
    TokenService tokenService,
    HttpRequest request,
    string roomId) =>
{
    var authResult = ValidateAuth(request, tokenService, UserRole.Admin, out var user);
    if (authResult != null) return authResult;
    
    return repo.Delete(roomId) ? Results.Ok() : Results.NotFound();
});

app.MapGet("/rooms", (
    RoomRepository repo,
    TokenService tokenService,
    HttpRequest request) =>
{
    var authResult = ValidateAuth(request, tokenService, UserRole.Admin, out var user);
    if (authResult != null) return authResult;
    
    return Results.Ok(repo.GetAll());
});

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }