var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

// Room CRUD endpoints
app.MapPost("/rooms", (PlanningPoker.Api.Repositories.RoomRepository repo, PlanningPoker.Api.Models.RoomConfig room) => Results.Ok(repo.Create(room)));
app.MapPut("/rooms/{roomId}", (PlanningPoker.Api.Repositories.RoomRepository repo, string roomId, PlanningPoker.Api.Models.RoomConfig room) =>
{
    room.RoomId = roomId;
    return repo.Update(room) ? Results.Ok(room) : Results.NotFound();
});
app.MapGet("/rooms/{roomId}", (PlanningPoker.Api.Repositories.RoomRepository repo, string roomId) =>
{
    var room = repo.Get(roomId);
    return room != null ? Results.Ok(room) : Results.NotFound();
});
app.MapDelete("/rooms/{roomId}", (PlanningPoker.Api.Repositories.RoomRepository repo, string roomId) =>
{
    return repo.Delete(roomId) ? Results.Ok() : Results.NotFound();
});
app.MapGet("/rooms", (PlanningPoker.Api.Repositories.RoomRepository repo) => Results.Ok(repo.GetAll()));

app.Run();
