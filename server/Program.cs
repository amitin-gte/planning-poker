var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Minimal health endpoint to verify the service is running.
// TODO: Replace or extend this with domain-specific endpoints for Planning Poker.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
