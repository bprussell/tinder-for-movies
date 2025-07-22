using TinderForMovies.Data;
using Microsoft.EntityFrameworkCore;

namespace TinderForMovies.Services;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(MovieDbContext context)
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            // Run any pending migrations
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                await context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
        }
    }
}