using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// CORS for Vue frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

// Endpoints
app.MapGet("/api/health", () =>
    {
        return Results.Ok(new
        {
            Status = "Healthy",
            Application = "StudyGraph API"
        });
    })
    .WithName("GetHealth")
    .WithTags("Health")
    .WithSummary("Check API health");

app.Run();