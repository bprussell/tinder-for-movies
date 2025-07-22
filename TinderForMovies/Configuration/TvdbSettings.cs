namespace TinderForMovies.Configuration;

public class TvdbSettings
{
    public const string SectionName = "Tvdb";
    
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api4.thetvdb.com";
    public string Pin { get; set; } = string.Empty; // Optional PIN for enhanced features
}