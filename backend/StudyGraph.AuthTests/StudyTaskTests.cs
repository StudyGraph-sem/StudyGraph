using System.Text.Json;
using System.Text.Json.Serialization;
using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Domain.Entities;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using StudyGraph.Application.DTOs.Responses;
using StudyGraph.Application.Services.Implementations;
using StudyGraph.Infrastructure.Persistence;

namespace StudyGraph.AuthTests;

public sealed class StudyTaskTests : IAsyncLifetime
{
    private readonly string schema = "task_test_" + Guid.NewGuid().ToString("N");
    private readonly string key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
    private string connectionString = "";
    private bool schemaCreated;
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;
    private readonly string email = Guid.NewGuid().ToString("N") + "@example.com";
    private readonly FixedClock clock = new();
    private readonly Guid ownerId = Guid.NewGuid();
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private const string Password = "TestPass123!";

    public async Task InitializeAsync()
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build();
        var connection = Environment.GetEnvironmentVariable("STUDYGRAPH_TEST_CONNECTION")
            ?? config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Set STUDYGRAPH_TEST_CONNECTION or development User Secrets.");
        var builder = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema };
        connectionString = builder.ConnectionString;
        await using (var database = new NpgsqlConnection(connectionString))
        {
            await database.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE SCHEMA {schema}", database);
            await command.ExecuteNonQueryAsync();
            schemaCreated = true;
        }
        await using (var context = Context()) await context.Database.MigrateAsync();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseEnvironment("Development");
            host.ConfigureServices(services => services.AddSingleton<TimeProvider>(clock));
            host.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            host.UseSetting("Jwt:Key", key);
            host.UseSetting("Jwt:Issuer", "StudyGraph");
            host.UseSetting("Jwt:Audience", "StudyGraph.Client");
        });
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        await using (var context = Context())
        {
            context.Users.Add(new User { Id = ownerId, Email = email, DisplayName = "Owner", PasswordHash = "not-used", Role = "User", CreatedAt = clock.UtcNow.UtcDateTime });
            await context.SaveChangesAsync();
        }
        SignIn(ownerId, "User");
        client.DefaultRequestHeaders.Add("Origin", "https://localhost");
        client.DefaultRequestHeaders.Add("X-StudyGraph-CSRF", "1");
    }

    public async Task DisposeAsync()
    {
        client?.Dispose();
        if (factory is not null) await factory.DisposeAsync();
        if (!schemaCreated) return;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {schema} CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }

    private AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
    private void SignIn(Guid id, string role)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = key, ["Jwt:Issuer"] = "StudyGraph", ["Jwt:Audience"] = "StudyGraph.Client"
        }).Build();
        var (token, _) = new JwtTokenService(config).GenerateAccessToken(new User { Id = id, Email = email, Role = role });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
    private async Task<StudyTaskResponse> Create(string title = "Read chapter", DateTimeOffset? due = null, StudyTaskPriority priority = StudyTaskPriority.Normal)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new CreateStudyTaskRequest(title, null, priority, due), json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = (await response.Content.ReadFromJsonAsync<StudyTaskResponse>(json))!;
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location?.ToString());
        return task;
    }
    private Task<HttpResponseMessage> Status(Guid id, string status) =>
        client.PatchAsJsonAsync($"/api/tasks/{id}/status", new { status });
    private async Task<StudyTaskResponse> Read(Guid id) =>
        (await client.GetFromJsonAsync<StudyTaskResponse>($"/api/tasks/{id}", json))!;
    private async Task<StudyTaskPageResponse> List(string query = "") =>
        (await client.GetFromJsonAsync<StudyTaskPageResponse>("/api/tasks" + query, json))!;

    [Fact]
    public async Task CreatesOwnedTaskAndConvertsDeadlineToUtc()
    {
        var due = new DateTimeOffset(2026, 12, 1, 15, 0, 0, TimeSpan.FromHours(3));
        var response = await client.PostAsJsonAsync("/api/tasks", new { title = "  Read  ", description = "  Notes  ", priority = "High", dueAt = due, userId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = (await response.Content.ReadFromJsonAsync<StudyTaskResponse>(json))!;
        Assert.Equal(ownerId, task.UserId);
        Assert.Equal("Read", task.Title);
        Assert.Equal("Notes", task.Description);
        Assert.Equal(StudyTaskStatus.Todo, task.Status);
        Assert.Equal(StudyTaskPriority.High, task.Priority);
        Assert.Equal(due.UtcDateTime, task.DueAt);
        Assert.Equal(DateTimeKind.Utc, task.DueAt!.Value.Kind);
        Assert.Equal(clock.UtcNow.UtcDateTime, task.CreatedAt);
        Assert.Equal(task.CreatedAt, task.UpdatedAt);
        Assert.Null(task.CompletedAt);
        Assert.Equal(task, await Read(task.Id));
        var empty = await Create();
        Assert.Null(empty.Description);
        Assert.Null(empty.DueAt);
        Assert.False(empty.IsOverdue);
    }
    [Fact]
    public async Task UpdatesAndDeletesTask()
    {
        var task = await Create(due: clock.UtcNow.AddDays(1));
        clock.UtcNow = clock.UtcNow.AddHours(1);
        var response = await client.PutAsJsonAsync($"/api/tasks/{task.Id}", new { title = "Updated", description = "  ", priority = "Low", dueAt = (DateTimeOffset?)null });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await Read(task.Id);
        Assert.Equal("Updated", updated.Title);
        Assert.Null(updated.Description);
        Assert.Null(updated.DueAt);
        Assert.Equal(StudyTaskPriority.Low, updated.Priority);
        Assert.Equal(task.CreatedAt, updated.CreatedAt);
        Assert.Equal(clock.UtcNow.UtcDateTime, updated.UpdatedAt);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
    }
    [Fact]
    public async Task CompletesIdempotentlyAndReopens()
    {
        var task = await Create(due: clock.UtcNow.AddDays(-1));
        Assert.Equal(HttpStatusCode.OK, (await Status(task.Id, "InProgress")).StatusCode);
        clock.UtcNow = clock.UtcNow.AddHours(1);
        Assert.Equal(HttpStatusCode.OK, (await Status(task.Id, "Completed")).StatusCode);
        var completed = await Read(task.Id);
        Assert.Equal(clock.UtcNow.UtcDateTime, completed.CompletedAt);
        Assert.False(completed.IsOverdue);
        clock.UtcNow = clock.UtcNow.AddHours(1);
        await Status(task.Id, "Completed");
        Assert.Equal(completed.CompletedAt, (await Read(task.Id)).CompletedAt);
        Assert.Equal(completed.UpdatedAt, (await Read(task.Id)).UpdatedAt);
        await Status(task.Id, "Todo");
        var reopened = await Read(task.Id);
        Assert.Null(reopened.CompletedAt);
        Assert.Equal(StudyTaskStatus.Todo, reopened.Status);
        Assert.True(reopened.IsOverdue);
    }
    [Fact]
    public async Task FiltersSortsAndPaginatesWithNullDeadlinesLast()
    {
        var late = await Create("Late", clock.UtcNow.AddDays(2), StudyTaskPriority.High);
        var early = await Create("Early", clock.UtcNow.AddDays(1), StudyTaskPriority.High);
        var noDate = await Create("No date", null, StudyTaskPriority.Low);
        await Status(late.Id, "Completed");
        var list = await List();
        Assert.Equal(new[] { early.Id, late.Id, noDate.Id }, list.Items.Select(x => x.Id));
        var descending = await List("?dueDescending=true");
        Assert.Equal(new[] { late.Id, early.Id, noDate.Id }, descending.Items.Select(x => x.Id));
        var filtered = await List("?status=Todo&priority=High");
        Assert.Equal(early.Id, Assert.Single(filtered.Items).Id);
        var page = await List("?page=2&pageSize=1");
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(late.Id, Assert.Single(page.Items).Id);
        Assert.Empty((await List("?page=100")).Items);
    }
    [Fact]
    public async Task OverdueRespectsBoundaryAndCompletion()
    {
        var overdue = await Create("Past", clock.UtcNow.AddSeconds(-1));
        await Create("Boundary", clock.UtcNow);
        await Create("Future", clock.UtcNow.AddSeconds(1));
        await Create("No deadline");
        var completed = await Create("Completed", clock.UtcNow.AddDays(-1));
        await Status(completed.Id, "Completed");
        var list = await List("?overdue=true");
        Assert.Equal(overdue.Id, Assert.Single(list.Items).Id);
        Assert.True(list.Items[0].IsOverdue);
        Assert.Equal(4, (await List("?overdue=false")).TotalCount);
    }
    [Fact]
    public async Task HidesForeignTasksIncludingFromAdmins()
    {
        var task = await Create();
        var otherId = Guid.NewGuid();
        await using (var context = Context())
        {
            context.Users.Add(new User { Id = otherId, Email = "other-" + email, DisplayName = "Other", PasswordHash = "unused", Role = "Admin", CreatedAt = clock.UtcNow.UtcDateTime });
            await context.SaveChangesAsync();
        }
        SignIn(otherId, "Admin");
        Assert.Empty((await List()).Items);
        foreach (var id in new[] { task.Id, Guid.NewGuid() })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/tasks/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/tasks/{id}", new { title = "Stolen" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Status(id, "Completed")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/tasks/{id}")).StatusCode);
        }
        SignIn(ownerId, "User");
        Assert.Equal(task, await Read(task.Id));
    }
    [Fact]
    public async Task RejectsInvalidInputWithoutChangingTask()
    {
        foreach (var body in new object[] {
            new { title = "" }, new { title = "  " }, new { description = "No title" },
            new { title = new string('a', 201) }, new { title = "Test", description = new string('a', 5001) },
            new { title = "Test", priority = "Unknown" }, new { title = "Test", priority = 99 },
            new { title = "Test", dueAt = "invalid" }
        }) Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/tasks", body)).StatusCode);
        var task = await Create();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/tasks/{task.Id}", new { title = " " })).StatusCode);
        foreach (var body in new object[] { new { status = "Unknown" }, new { status = 99 }, new { status = (string?)null }, new { other = "Completed" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", body)).StatusCode);
        foreach (var query in new[] { "page=0", "pageSize=101", "pageSize=-1", "status=99", "priority=99", "status=Unknown", "overdue=invalid", "page=2147483647" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/tasks?" + query)).StatusCode);
        Assert.Equal(task, await Read(task.Id));
    }
    [Theory]
    [InlineData("GET", "/api/tasks")]
    [InlineData("POST", "/api/tasks")]
    [InlineData("GET", "/api/tasks/00000000-0000-0000-0000-000000000001")]
    [InlineData("PUT", "/api/tasks/00000000-0000-0000-0000-000000000001")]
    [InlineData("PATCH", "/api/tasks/00000000-0000-0000-0000-000000000001/status")]
    [InlineData("DELETE", "/api/tasks/00000000-0000-0000-0000-000000000001")]
    public async Task RequiresAuthentication(string method, string path)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT" or "PATCH") request.Content = JsonContent.Create(new { title = "Test", status = "Todo" });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }
    [Fact]
    public async Task RejectsTokenWithoutValidUserIdentifier()
    {
        SignIn(Guid.Empty, "User");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tasks")).StatusCode);
    }
    private sealed class FixedClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
