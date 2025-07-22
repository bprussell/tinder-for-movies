using Microsoft.EntityFrameworkCore;
using TinderForMovies.Data;
using TinderForMovies.Models;

namespace TinderForMovies.Services;

public class MovieInteractionService : IMovieInteractionService
{
    private readonly MovieDbContext _context;

    public MovieInteractionService(MovieDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasUserInteractedWithMovieAsync(int movieId)
    {
        return await _context.UserMovieInteractions
            .AnyAsync(i => i.MovieId == movieId);
    }

    public async Task SaveMatchAsync(Movie movie)
    {
        var interaction = new UserMovieInteraction
        {
            MovieId = movie.Id,
            MovieTitle = movie.Title,
            MoviePosterUrl = movie.PosterUrl,
            MovieOverview = movie.Overview,
            MovieYear = movie.Year,
            MovieGenres = movie.GenreText,
            MovieRating = movie.Rating,
            InteractionType = InteractionType.Matched,
            InteractionDate = DateTime.UtcNow
        };

        _context.UserMovieInteractions.Add(interaction);
        await _context.SaveChangesAsync();
    }

    public async Task SaveRejectionAsync(Movie movie)
    {
        var interaction = new UserMovieInteraction
        {
            MovieId = movie.Id,
            MovieTitle = movie.Title,
            MoviePosterUrl = movie.PosterUrl,
            MovieOverview = movie.Overview,
            MovieYear = movie.Year,
            MovieGenres = movie.GenreText,
            MovieRating = movie.Rating,
            InteractionType = InteractionType.Rejected,
            InteractionDate = DateTime.UtcNow
        };

        _context.UserMovieInteractions.Add(interaction);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserMovieInteraction>> GetMatchedMoviesAsync()
    {
        return await _context.UserMovieInteractions
            .Where(i => i.InteractionType == InteractionType.Matched)
            .OrderByDescending(i => i.InteractionDate)
            .ToListAsync();
    }

    public async Task<List<UserMovieInteraction>> GetRejectedMoviesAsync()
    {
        return await _context.UserMovieInteractions
            .Where(i => i.InteractionType == InteractionType.Rejected)
            .OrderByDescending(i => i.InteractionDate)
            .ToListAsync();
    }

    public async Task MarkAsWatchedAsync(int interactionId, int? userRating = null, string? userReview = null)
    {
        var interaction = await _context.UserMovieInteractions
            .FirstOrDefaultAsync(i => i.Id == interactionId);

        if (interaction != null)
        {
            interaction.IsWatched = true;
            interaction.WatchedDate = DateTime.UtcNow;
            interaction.UserRating = userRating;
            interaction.UserReview = userReview;
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task UnmarkAsWatchedAsync(int interactionId)
    {
        var interaction = await _context.UserMovieInteractions
            .FirstOrDefaultAsync(i => i.Id == interactionId);

        if (interaction != null)
        {
            interaction.IsWatched = false;
            interaction.WatchedDate = null;
            interaction.UserRating = null;
            interaction.UserReview = null;
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveMatchAsync(int interactionId)
    {
        var interaction = await _context.UserMovieInteractions
            .FirstOrDefaultAsync(i => i.Id == interactionId);

        if (interaction != null)
        {
            _context.UserMovieInteractions.Remove(interaction);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserMovieInteraction?> GetInteractionAsync(int interactionId)
    {
        return await _context.UserMovieInteractions
            .FirstOrDefaultAsync(i => i.Id == interactionId);
    }
}