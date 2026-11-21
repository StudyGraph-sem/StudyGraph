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
using StudyGraph.Domain.Entities;
using StudyGraph.Infrastructure.Persistence;

namespace StudyGraph.AuthTests;

public sealed class AuthTests : IAsyncLifetime
{
    private readonly string schema = "auth_test_" + Guid.NewGuid().ToString("N");
    private readonly string key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
    private string connectionString = "";
    private bool schemaCreated;
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;
    private readonly string email = Guid.NewGuid().ToString("N") + "@example.com";
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
            host.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            host.UseSetting("Jwt:Key", key);
            host.UseSetting("Jwt:Issuer", "StudyGraph");
            host.UseSetting("Jwt:Audience", "StudyGraph.Client");
        });
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
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
    private Task<HttpResponseMessage> Register(string? address = null) => client.PostAsJsonAsync("/api/auth/register", new { email = address ?? email, password = Password, displayName = "Tester" });
    private Task<HttpResponseMessage> Login(string password = Password) => client.PostAsJsonAsync("/api/auth/login", new { email, password });
    private static string Cookie(HttpResponseMessage response) => response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    private Task<HttpResponseMessage> Refresh(string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }

    [Fact]
    public async Task RegistrationLoginAndAuthorization()
    {
        Assert.Equal(HttpStatusCode.Created, (await Register("  " + email.ToUpperInvariant() + "  ")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Register()).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login("wrong")).StatusCode);
        var login = await Login();
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(email, auth.User.Email);
        Assert.Equal("User", auth.User.Role);
        Assert.Contains("httponly", login.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant());
        Assert.Contains("secure", login.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant());
        Assert.Contains("samesite=strict", login.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant());
        await using (var context = Context())
        {
            var user = await context.Users.SingleAsync();
            Assert.NotEqual(Password, user.PasswordHash);
            var token = await context.Set<RefreshToken>().SingleAsync();
            Assert.DoesNotContain(token.TokenHash, Cookie(login));
            Assert.Equal(64, token.TokenHash.Length);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/auth/admin")).StatusCode);
        await using (var context = Context()) await context.Users.ExecuteUpdateAsync(s => s.SetProperty(x => x.Role, "Admin"));
        var admin = (await (await Login()).Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/admin")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken + "invalid");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task RotationReplayAndLogout()
    {
        await Register();
        var login = await Login();
        var oldCookie = Cookie(login);
        var rotated = await Refresh(oldCookie);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var newCookie = Cookie(rotated);
        Assert.NotEqual(oldCookie, newCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(oldCookie)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(newCookie)).StatusCode);
        var another = Cookie(await Login());
        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add("Cookie", another);
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(logout)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(another)).StatusCode);
    }

    [Fact]
    public async Task ConcurrentRefreshRevokesFamily()
    {
        await Register();
        var cookie = Cookie(await Login());
        var results = await Task.WhenAll(Refresh(cookie), Refresh(cookie));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Unauthorized);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(Cookie(results.Single(x => x.IsSuccessStatusCode)))).StatusCode);
    }

    [Fact]
    public async Task ExpirationValidationCsrfAndRateLimit()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new { password = Password, displayName = "Test" })).StatusCode);
        await Register();
        var cookie = Cookie(await Login());
        await using (var context = Context()) await context.Set<RefreshToken>().ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(cookie)).StatusCode);
        client.DefaultRequestHeaders.Remove("Origin");
        Assert.Equal(HttpStatusCode.Forbidden, (await Refresh(cookie)).StatusCode);
        client.DefaultRequestHeaders.Add("Origin", "https://evil.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await Login()).StatusCode);
        HttpResponseMessage response = null!;
        for (var i = 0; i < 11; i++) response = await Login();
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task DatabaseRejectsDuplicateEmail()
    {
        await Register();
        await using var context = Context();
        context.Users.Add(new User { Email = email, DisplayName = "Duplicate", PasswordHash = "unused", Role = "User", CreatedAt = DateTime.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    [InlineData("expired")]
    public async Task RejectsInvalidJwt(string failure)
    {
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = failure == "issuer" ? "invalid" : "StudyGraph",
            Audience = failure == "audience" ? "invalid" : "StudyGraph.Client",
            NotBefore = DateTime.UtcNow.AddHours(-2),
            Expires = failure == "expired" ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(failure == "signature" ? new string('x', 64) : key)), SecurityAlgorithms.HmacSha256)
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
}
