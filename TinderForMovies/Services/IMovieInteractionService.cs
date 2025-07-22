using TinderForMovies.Models;

namespace TinderForMovies.Services;

public interface IMovieInteractionService
{
    Task<bool> HasUserInteractedWithMovieAsync(int movieId);
    Task SaveMatchAsync(Movie movie);
    Task SaveRejectionAsync(Movie movie);
    Task<List<UserMovieInteraction>> GetMatchedMoviesAsync();
    Task<List<UserMovieInteraction>> GetRejectedMoviesAsync();
    Task MarkAsWatchedAsync(int interactionId, int? userRating = null, string? userReview = null);
    Task UnmarkAsWatchedAsync(int interactionId);
    Task RemoveMatchAsync(int interactionId);
    Task<UserMovieInteraction?> GetInteractionAsync(int interactionId);
}