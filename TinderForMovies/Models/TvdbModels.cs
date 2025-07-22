using System.Text.Json.Serialization;

namespace TinderForMovies.Models;

// TVDB API Response Models
public class TvdbResponse<T>
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class TvdbAuthResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

public class TvdbMovieSearchResult
{
    [JsonPropertyName("tvdb_id")]
    public string TvdbId { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;
    
    [JsonPropertyName("first_air_time")]
    public string? FirstAirTime { get; set; }
    
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("year")]
    public string? Year { get; set; }
}

public class TvdbMovieDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;
    
    [JsonPropertyName("first_air_time")]
    public string? FirstAirTime { get; set; }
    
    [JsonPropertyName("image")]
    public string? Image { get; set; }
    
    [JsonPropertyName("genres")]
    public List<TvdbGenre>? Genres { get; set; }
    
    [JsonPropertyName("score")]
    public double? Score { get; set; }
    
    [JsonPropertyName("status")]
    public TvdbStatus? Status { get; set; }
    
    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }
    
    [JsonPropertyName("companies")]
    public List<TvdbCompany>? Companies { get; set; }
}

public class TvdbGenre
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TvdbStatus
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TvdbCompany
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}