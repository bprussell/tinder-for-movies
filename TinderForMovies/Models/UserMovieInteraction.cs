using System.ComponentModel.DataAnnotations;

namespace TinderForMovies.Models;

public class UserMovieInteraction
{
    [Key]
    public int Id { get; set; }
    
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string? MoviePosterUrl { get; set; }
    public string? MovieOverview { get; set; }
    public string? MovieYear { get; set; }
    public string? MovieGenres { get; set; }
    public double? MovieRating { get; set; }
    
    public InteractionType InteractionType { get; set; }
    public DateTime InteractionDate { get; set; } = DateTime.UtcNow;
    
    // For matched movies
    public bool IsWatched { get; set; }
    public int? UserRating { get; set; } // 1-5 stars
    public string? UserReview { get; set; }
    public DateTime? WatchedDate { get; set; }
}

public enum InteractionType
{
    Rejected = 0,
    Matched = 1
}