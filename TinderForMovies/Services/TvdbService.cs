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

        var response = await _httpClient.PostAsync("/v4/login", content);
        
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
        // Use a much larger list of specific movie titles to avoid generic search issues
        var movieTitles = new[]
        {
            // Classics
            "The Shawshank Redemption", "The Godfather", "The Dark Knight", "Pulp Fiction",
            "The Lord of the Rings", "Forrest Gump", "Star Wars", "Inception",
            "The Matrix", "Goodfellas", "The Silence of the Lambs", "Saving Private Ryan",
            "Schindler's List", "Terminator 2", "Back to the Future", "Alien",
            "The Lion King", "Gladiator", "Titanic", "Jurassic Park",
            
            // Modern hits
            "Avatar", "Avengers", "Spider-Man", "Batman", "Superman", "Iron Man",
            "The Dark Knight Rises", "Interstellar", "Dunkirk", "Joker",
            "Parasite", "1917", "Once Upon a Time in Hollywood", "Jojo Rabbit",
            "Ford v Ferrari", "The Irishman", "Marriage Story", "Little Women",
            
            // Action movies
            "Mad Max Fury Road", "John Wick", "Mission Impossible", "Fast and Furious",
            "Die Hard", "Lethal Weapon", "Rush Hour", "The Bourne Identity",
            "Casino Royale", "Skyfall", "Top Gun", "Heat", "Speed", "Face/Off",
            
            // Comedies
            "Groundhog Day", "The Big Lebowski", "Anchorman", "Superbad",
            "Pineapple Express", "Step Brothers", "Talladega Nights", "Wedding Crashers",
            "Old School", "Meet the Parents", "There's Something About Mary", "Dumb and Dumber",
            
            // Horror/Thriller
            "Get Out", "A Quiet Place", "Hereditary", "The Conjuring",
            "Insidious", "Saw", "Scream", "Halloween", "Friday the 13th",
            "The Exorcist", "Psycho", "Jaws", "The Thing", "Poltergeist",
            
            // Sci-Fi
            "Blade Runner", "Minority Report", "Total Recall", "The Fifth Element",
            "District 9", "Elysium", "Pacific Rim", "Edge of Tomorrow",
            "Ex Machina", "Her", "Arrival", "Gravity", "Prometheus", "Aliens",
            
            // Romance/Drama
            "Titanic", "The Notebook", "Casablanca", "When Harry Met Sally",
            "Ghost", "Pretty Woman", "Sleepless in Seattle", "You've Got Mail",
            "The Princess Bride", "Dirty Dancing", "Top Gun", "Jerry Maguire",
            
            // Animated
            "Toy Story", "Finding Nemo", "The Incredibles", "Monsters Inc",
            "Up", "WALL-E", "Inside Out", "Coco", "Moana", "Frozen",
            "Shrek", "How to Train Your Dragon", "Kung Fu Panda", "Madagascar",
            
            // More recent hits
            "Black Panther", "Wonder Woman", "Aquaman", "Captain Marvel",
            "Endgame", "Infinity War", "Thor Ragnarok", "Guardians of the Galaxy",
            "Doctor Strange", "Ant-Man", "Spider-Man Homecoming", "Civil War",
            
            // Classic franchises  
            "Raiders of the Lost Ark", "E.T.", "Close Encounters", "Jaws",
            "Rocky", "Rambo", "Terminator", "Predator", "RoboCop", "Total Recall",
            "Demolition Man", "Judge Dredd", "The Running Man", "Eraser"
        };

        var movies = new List<Movie>();
        var moviesPerPage = 10;
        var startIndex = (page - 1) * moviesPerPage;

        // Calculate how many complete cycles through the array we need
        var cycleLength = movieTitles.Length;
        var currentCycle = startIndex / cycleLength;
        var indexInCycle = startIndex % cycleLength;

        System.Diagnostics.Debug.WriteLine($"Page {page}: Starting at cycle {currentCycle}, index {indexInCycle}");

        var moviesAdded = 0;
        var attempts = 0;
        var maxAttempts = moviesPerPage * 3; // Try up to 3x to get enough movies

        while (moviesAdded < moviesPerPage && attempts < maxAttempts)
        {
            var movieIndex = (indexInCycle + attempts) % cycleLength;
            var movieTitle = movieTitles[movieIndex];
            
            // For later cycles, add variation to the search
            var searchTerm = movieTitle;
            if (currentCycle > 0)
            {
                // Add year variations, sequels, etc.
                var variations = new[]
                {
                    movieTitle,
                    $"{movieTitle} 2",
                    $"{movieTitle} II", 
                    $"{movieTitle} Returns",
                    $"{movieTitle} Reloaded",
                    movieTitle.Replace("The ", ""),
                    movieTitle + " movie"
                };
                searchTerm = variations[currentCycle % variations.Length];
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Searching for: '{searchTerm}'");
                var searchResults = await SearchMoviesAsync(searchTerm);
                
                if (searchResults.Any())
                {
                    // Filter to ensure we get actual movies with real titles
                    var validMovies = searchResults.Where(m => 
                        !string.IsNullOrEmpty(m.Title) && 
                        m.Title.Length > 1 &&
                        !m.Title.Equals("action", StringComparison.OrdinalIgnoreCase) &&
                        !m.Title.Equals("comedy", StringComparison.OrdinalIgnoreCase) &&
                        !m.Title.Equals("drama", StringComparison.OrdinalIgnoreCase) &&
                        !m.Title.Equals("horror", StringComparison.OrdinalIgnoreCase) &&
                        !m.Title.Equals("thriller", StringComparison.OrdinalIgnoreCase) &&
                        m.Id > 0
                    ).Take(2).ToList(); // Take up to 2 movies per search
                    
                    foreach (var movie in validMovies)
                    {
                        if (moviesAdded >= moviesPerPage) break;
                        if (!movies.Any(m => m.Id == movie.Id)) // Avoid duplicates
                        {
                            movies.Add(movie);
                            moviesAdded++;
                            System.Diagnostics.Debug.WriteLine($"Added movie: '{movie.Title}' (ID: {movie.Id})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search failed for '{searchTerm}': {ex.Message}");
            }
            
            attempts++;
        }

        System.Diagnostics.Debug.WriteLine($"Page {page}: Found {movies.Count} movies after {attempts} attempts");
        return movies;
    }

    public async Task<List<Movie>> SearchMoviesAsync(string query)
    {
        await EnsureAuthenticatedAsync();

        var response = await _httpClient.GetAsync($"/v4/search?query={Uri.EscapeDataString(query)}&type=movie");
        
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

        var response = await _httpClient.GetAsync($"/v4/movies/{movieId}/extended");
        
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