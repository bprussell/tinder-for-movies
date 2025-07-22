namespace TinderForMovies.Models;

public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public DateTime? FirstAired { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public List<string> Genres { get; set; } = new();
    public double? Rating { get; set; }
    public string? ContentRating { get; set; }
    public int? Runtime { get; set; }
    public string? Status { get; set; }
    public List<string> Companies { get; set; } = new();
    
    // Computed properties for UI
    public string Year => FirstAired?.Year.ToString() ?? "Unknown";
    public string GenreText => string.Join(", ", Genres.Take(2)); // Show first 2 genres
    public string RuntimeText => Runtime.HasValue ? $"{Runtime} min" : "Unknown";
    public string RatingText => Rating.HasValue ? $"{Rating:F1}/10" : "No rating";
}