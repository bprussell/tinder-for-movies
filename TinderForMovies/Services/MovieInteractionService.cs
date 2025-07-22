using Microsoft.EntityFrameworkCore;
using TinderForMovies.Data;
using TinderForMovies.Models;

namespace TinderForMovies.Services;

public class MovieInteractionService : IMovieInteractionService
{
    private readonly IServiceProvider _serviceProvider;

    public MovieInteractionService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> HasUserInteractedWithMovieAsync(int movieId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
        return await context.UserMovieInteractions
            .AnyAsync(i => i.MovieId == movieId);
    }

    public async Task SaveMatchAsync(Movie movie)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
            
            // Check if already exists to prevent duplicates
            var existing = await context.UserMovieInteractions
                .FirstOrDefaultAsync(i => i.MovieId == movie.Id);
            
            if (existing != null)
            {
                // Update existing instead of creating duplicate
                existing.InteractionType = InteractionType.Matched;
                existing.InteractionDate = DateTime.UtcNow;
            }
            else
            {
                var interaction = new UserMovieInteraction
                {
                    MovieId = movie.Id,
                    MovieTitle = movie.Title?.Trim() ?? "",
                    MoviePosterUrl = movie.PosterUrl?.Trim(),
                    MovieOverview = movie.Overview?.Trim(),
                    MovieYear = movie.Year?.Trim(),
                    MovieGenres = movie.GenreText?.Trim(),
                    MovieRating = movie.Rating,
                    InteractionType = InteractionType.Matched,
                    InteractionDate = DateTime.UtcNow
                };

                context.UserMovieInteractions.Add(interaction);
            }
            
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveMatchAsync error: {ex}");
            throw new InvalidOperationException($"Failed to save movie match: {ex.Message}", ex);
        }
    }

    public async Task SaveRejectionAsync(Movie movie)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
            
            // Check if already exists to prevent duplicates
            var existing = await context.UserMovieInteractions
                .FirstOrDefaultAsync(i => i.MovieId == movie.Id);
            
            if (existing != null)
            {
                // Update existing instead of creating duplicate
                existing.InteractionType = InteractionType.Rejected;
                existing.InteractionDate = DateTime.UtcNow;
            }
            else
            {
                var interaction = new UserMovieInteraction
                {
                    MovieId = movie.Id,
                    MovieTitle = movie.Title?.Trim() ?? "",
                    MoviePosterUrl = movie.PosterUrl?.Trim(),
                    MovieOverview = movie.Overview?.Trim(),
                    MovieYear = movie.Year?.Trim(),
                    MovieGenres = movie.GenreText?.Trim(),
                    MovieRating = movie.Rating,
                    InteractionType = InteractionType.Rejected,
                    InteractionDate = DateTime.UtcNow
                };

                context.UserMovieInteractions.Add(interaction);
            }
            
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveRejectionAsync error: {ex}");
            throw new InvalidOperationException($"Failed to save movie rejection: {ex.Message}", ex);
        }
    }

    public async Task<List<UserMovieInteraction>> GetMatchedMoviesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
        return await context.UserMovieInteractions
            .Where(i => i.InteractionType == InteractionType.Matched)
            .OrderByDescending(i => i.InteractionDate)
            .ToListAsync();
    }

    public async Task<List<UserMovieInteraction>> GetRejectedMoviesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
        return await context.UserMovieInteractions
            .Where(i => i.InteractionType == InteractionType.Rejected)
            .OrderByDescending(i => i.InteractionDate)
            .ToListAsync();
    }

    public async Task MarkAsWatchedAsync(int interactionId, int? userRating = null, string? userReview = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
        
        var interaction = await context.UserMovieInteractions
            .FirstOrDefaultAsync(i => i.Id == interactionId);

        if (interaction != null)
        {
            interaction.IsWatched = true;
            interaction.WatchedDate = DateTime.UtcNow;
            interaction.UserRating = userRating;
            interaction.UserReview = userReview;
            
            await context.SaveChangesAsync();
        }
    }

    public async Task UnmarkAsWatchedAsync(int interactionId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
        
        var interaction = await context.UserMovieInteractions
            .FirstOrDefaultAsync(i => i.Id == interactionId);

        if (interaction != null)
        {
            interaction.IsWatched = false;
            interaction.WatchedDate = null;
            interaction.UserRating = null;
            interaction.UserReview = null;
            
            await context.SaveChangesAsync();
        }
    }

    public async Task RemoveMatchAsync(int interactionId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
        
        var interaction = await context.UserMovieInteractions
            .FirstOrDefaultAsync(i => i.Id == interactionId);

        if (interaction != null)
        {
            context.UserMovieInteractions.Remove(interaction);
            await context.SaveChangesAsync();
        }
    }

    public async Task<UserMovieInteraction?> GetInteractionAsync(int interactionId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
        return await context.UserMovieInteractions
            .FirstOrDefaultAsync(i => i.Id == interactionId);
    }
}