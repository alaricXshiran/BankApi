using BankApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankApi.Data;

// Prepares the database when the app starts:
// applies any pending migrations, and creates the first admin user if there are no users yet.
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        // Services like the DbContext normally live "per request".
        // There's no request at startup, so we create a temporary scope to borrow them from.
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<BankDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        // 1. Create the database, or bring it up to date, by applying any pending migrations.
        await context.Database.MigrateAsync();

        // 2. If users already exist, there's nothing more to do.
        if (await context.Users.AnyAsync())
        {
            return;
        }

        // Read the first admin's details from configuration (User Secrets or environment variables).
        var configuration = services.GetRequiredService<IConfiguration>();
        var username = configuration["Admin:Username"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No users exist and Admin:Username / Admin:Password are not set, so nobody can log in.");
            return;
        }

        // Create the admin with a hashed password, exactly like the register endpoint does.
        var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();

        var admin = new User
        {
            Username = username.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, password);

        context.Users.Add(admin);
        await context.SaveChangesAsync();

        logger.LogInformation("Created the first admin user '{Username}'.", admin.Username);
    }
}