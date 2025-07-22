using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TinderForMovies.Configuration;
using TinderForMovies.Models;
using System.Reflection;

namespace TinderForMovies.Services;

public class TvdbService : ITvdbService
{
    private readonly HttpClient _httpClient;
    private readonly TvdbSettings _settings;
    private string? _authToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private static List<string>? _movieTitles;
    private static readonly object _lockObject = new object();

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

    private List<string> LoadMovieTitles()
    {
        if (_movieTitles != null)
            return _movieTitles;

        lock (_lockObject)
        {
            if (_movieTitles != null)
                return _movieTitles;

            try
            {
                // Try to load from CSV file first
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "TinderForMovies.Resources.Raw.movies.csv";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    var csvContent = reader.ReadToEnd(); // Use synchronous ReadToEnd()
                    
                    _movieTitles = ParseCsvMovieTitles(csvContent);
                    System.Diagnostics.Debug.WriteLine($"Loaded {_movieTitles.Count} movies from CSV file");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CSV file not found as embedded resource, using fallback movie list");
                    _movieTitles = GetFallbackMovieTitles();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading CSV file: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("Using fallback movie list");
                _movieTitles = GetFallbackMovieTitles();
            }

            return _movieTitles;
        }
    }

    private List<string> ParseCsvMovieTitles(string csvContent)
    {
        var titles = new List<string>();
        var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length <= 1)
        {
            System.Diagnostics.Debug.WriteLine("CSV file appears empty or only has header");
            return GetFallbackMovieTitles();
        }

        // Skip header row (first line contains "movieId,title,genres")
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                // Parse CSV line - handle quoted fields properly
                var title = ExtractTitleFromCsvLine(line);
                if (!string.IsNullOrEmpty(title))
                {
                    // Clean up title - remove year suffix like "(1995)" for search purposes
                    var cleanTitle = CleanMovieTitle(title);
                    if (!string.IsNullOrEmpty(cleanTitle) && cleanTitle.Length > 1)
                    {
                        titles.Add(cleanTitle);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing CSV line {i}: {ex.Message}");
                // Continue with next line
            }
        }

        return titles.Count > 0 ? titles : GetFallbackMovieTitles();
    }

    private string ExtractTitleFromCsvLine(string csvLine)
    {
        // Simple CSV parsing for format: movieId,title,genres
        // Handle quoted fields that may contain commas
        
        var inQuotes = false;
        var fieldIndex = 0;
        var currentField = new StringBuilder();
        
        for (int i = 0; i < csvLine.Length; i++)
        {
            var ch = csvLine[i];
            
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                if (fieldIndex == 1) // Title is field index 1
                {
                    return currentField.ToString().Trim();
                }
                fieldIndex++;
                currentField.Clear();
            }
            else
            {
                currentField.Append(ch);
            }
        }
        
        // If we ended on field 1 (title), return it
        if (fieldIndex == 1)
        {
            return currentField.ToString().Trim();
        }
        
        return string.Empty;
    }

    private string CleanMovieTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return string.Empty;
            
        // Remove year suffix like "(1995)" or "(2023)"
        var yearPattern = @"\s*\(\d{4}\)\s*$";
        var cleanTitle = System.Text.RegularExpressions.Regex.Replace(title, yearPattern, "").Trim();
        
        // Remove alternate title patterns like "(a.k.a. Something)"
        var akaPattern = @"\s*\(a\.k\.a\..+?\)\s*";
        cleanTitle = System.Text.RegularExpressions.Regex.Replace(cleanTitle, akaPattern, "").Trim();
        
        return cleanTitle;
    }

    private List<string> GetFallbackMovieTitles()
    {
        // Fallback to hardcoded list if CSV loading fails
        return new List<string>
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
            "Ford v Ferrari", "The Irishman", "Marriage Story", "Little Women"
        };
    }

    public async Task<List<Movie>> GetPopularMoviesAsync(int page = 1)
    {
        // Load movie titles from CSV file
        var movieTitles = LoadMovieTitles();

        var movies = new List<Movie>();
        var moviesPerPage = 10;
        var startIndex = (page - 1) * moviesPerPage;

        // Calculate how many complete cycles through the list we need
        var cycleLength = movieTitles.Count;
        var currentCycle = startIndex / cycleLength;
        var indexInCycle = startIndex % cycleLength;

        System.Diagnostics.Debug.WriteLine($"Page {page}: Starting at cycle {currentCycle}, index {indexInCycle}, total movies: {cycleLength}");

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