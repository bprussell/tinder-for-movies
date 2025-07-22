using TinderForMovies.Models;

namespace TinderForMovies.Services;

public interface ITvdbService
{
    Task<List<Movie>> GetPopularMoviesAsync(int page = 1);
    Task<List<Movie>> SearchMoviesAsync(string query);
    Task<Movie?> GetMovieDetailsAsync(int movieId);
}