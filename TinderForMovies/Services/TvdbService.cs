using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TinderForMovies.Configuration;
using TinderForMovies.Models;

namespace TinderForMovies.Services;

public class TvdbService : ITvdbService
{
    private readonly HttpClient _httpClient;
    private readonly TvdbSettings _settings;
    private string? _authToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public TvdbService(HttpClient httpClient, IOptions<TvdbSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (!string.IsNullOrEmpty(_authToken) && DateTime.UtcNow < _tokenExpiry)
            return;

        // Create auth request - only include PIN if it's not empty
        object authRequest;
        if (string.IsNullOrEmpty(_settings.Pin))
        {
            authRequest = new { apikey = _settings.ApiKey };
        }
        else
        {
            authRequest = new { apikey = _settings.ApiKey, pin = _settings.Pin };
        }

        var json = JsonSerializer.Serialize(authRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/login", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"TVDB authentication failed: {response.StatusCode} - {errorContent}. URL: {_httpClient.BaseAddress}/login");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<TvdbResponse<TvdbAuthResponse>>(responseContent);

        if (authResponse?.Data?.Token == null)
        {
            throw new InvalidOperationException("Failed to get authentication token from TVDB");
        }

        _authToken = authResponse.Data.Token;
        _tokenExpiry = DateTime.UtcNow.AddHours(23); // Tokens typically last 24 hours

        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _authToken);
    }

    public async Task<List<Movie>> GetPopularMoviesAsync(int page = 1)
    {
        // TVDB doesn't have a "popular movies" endpoint, so we'll search for popular movie titles
        var popularMovies = new[]
        {
            "The Shawshank Redemption", "The Godfather", "The Dark Knight", "Pulp Fiction",
            "The Lord of the Rings", "Forrest Gump", "Star Wars", "Inception",
            "The Matrix", "Goodfellas", "The Silence of the Lambs", "Saving Private Ryan",
            "Schindler's List", "Terminator 2", "Back to the Future", "Alien",
            "The Lion King", "Gladiator", "Titanic", "Jurassic Park",
            "Avatar", "Avengers", "Spider-Man", "Batman", "Superman"
        };

        var movies = new List<Movie>();
        var startIndex = (page - 1) * 10;
        var endIndex = Math.Min(startIndex + 10, popularMovies.Length);

        for (int i = startIndex; i < endIndex; i++)
        {
            try
            {
                var searchResults = await SearchMoviesAsync(popularMovies[i]);
                if (searchResults.Any())
                {
                    movies.Add(searchResults.First());
                }
            }
            catch
            {
                // Continue with next movie if one fails
            }
        }

        return movies;
    }

    public async Task<List<Movie>> SearchMoviesAsync(string query)
    {
        await EnsureAuthenticatedAsync();

        var response = await _httpClient.GetAsync($"/search?query={Uri.EscapeDataString(query)}&type=movie");
        
        if (!response.IsSuccessStatusCode)
        {
            return new List<Movie>();
        }

        var content = await response.Content.ReadAsStringAsync();
        var searchResponse = JsonSerializer.Deserialize<TvdbResponse<List<TvdbMovieSearchResult>>>(content);

        if (searchResponse?.Data == null)
        {
            return new List<Movie>();
        }

        return searchResponse.Data
            .Where(m => m.Type.Equals("movie", StringComparison.OrdinalIgnoreCase))
            .Select(ConvertToMovie)
            .ToList();
    }

    public async Task<Movie?> GetMovieDetailsAsync(int movieId)
    {
        await EnsureAuthenticatedAsync();

        var response = await _httpClient.GetAsync($"/movies/{movieId}/extended");
        
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var detailResponse = JsonSerializer.Deserialize<TvdbResponse<TvdbMovieDetails>>(content);

        if (detailResponse?.Data == null)
        {
            return null;
        }

        return ConvertToMovie(detailResponse.Data);
    }

    private static Movie ConvertToMovie(TvdbMovieSearchResult tvdbMovie)
    {
        DateTime? firstAired = null;
        if (!string.IsNullOrEmpty(tvdbMovie.FirstAirTime))
        {
            DateTime.TryParse(tvdbMovie.FirstAirTime, out var parsed);
            firstAired = parsed == default ? null : parsed;
        }

        return new Movie
        {
            Id = int.TryParse(tvdbMovie.TvdbId, out var id) ? id : 0,
            Title = tvdbMovie.Name,
            Overview = tvdbMovie.Overview,
            FirstAired = firstAired,
            PosterUrl = tvdbMovie.ImageUrl,
            Genres = new List<string>(), // Search results don't include genres
            Rating = null, // Search results don't include ratings
        };
    }

    private static Movie ConvertToMovie(TvdbMovieDetails tvdbMovie)
    {
        DateTime? firstAired = null;
        if (!string.IsNullOrEmpty(tvdbMovie.FirstAirTime))
        {
            DateTime.TryParse(tvdbMovie.FirstAirTime, out var parsed);
            firstAired = parsed == default ? null : parsed;
        }

        return new Movie
        {
            Id = tvdbMovie.Id,
            Title = tvdbMovie.Name,
            Overview = tvdbMovie.Overview,
            FirstAired = firstAired,
            PosterUrl = tvdbMovie.Image,
            Genres = tvdbMovie.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
            Rating = tvdbMovie.Score,
            Runtime = tvdbMovie.Runtime,
            Status = tvdbMovie.Status?.Name,
            Companies = tvdbMovie.Companies?.Select(c => c.Name).ToList() ?? new List<string>()
        };
    }
}