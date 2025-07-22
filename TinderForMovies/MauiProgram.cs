using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TinderForMovies.Configuration;
using TinderForMovies.Services;
using TinderForMovies.Data;

namespace TinderForMovies
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // Add configuration with fallbacks for mobile platforms
            ConfigureAppSettings(builder);

            // Configure settings (local file overrides main settings)
            builder.Services.Configure<TvdbSettings>(
                builder.Configuration.GetSection(TvdbSettings.SectionName));

            // Add database
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "movies.db");
            builder.Services.AddDbContext<MovieDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Add HTTP client and services
            builder.Services.AddHttpClient<ITvdbService, TvdbService>();
            builder.Services.AddScoped<IMovieInteractionService, MovieInteractionService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Initialize database on startup
            Task.Run(async () =>
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
                await DatabaseInitializer.InitializeAsync(context);
            });

            return app;
        }

        private static void ConfigureAppSettings(MauiAppBuilder builder)
        {
            // Try multiple approaches to load configuration
            bool configLoaded = false;

            // Method 1: Try embedded resource
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("TinderForMovies.appsettings.json");
                if (stream != null)
                {
                    builder.Configuration.AddJsonStream(stream);
                    configLoaded = true;
                    System.Diagnostics.Debug.WriteLine("Configuration loaded from embedded resource");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load embedded resource: {ex.Message}");
            }

            // Method 2: Try file system approach
            if (!configLoaded)
            {
                try
                {
                    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    configLoaded = true;
                    System.Diagnostics.Debug.WriteLine("Configuration loaded from file system");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load from file system: {ex.Message}");
                }
            }

            // Method 3: Provide default configuration if all else fails
            if (!configLoaded)
            {
                var defaultConfig = new Dictionary<string, string?>
                {
                    {"Tvdb:BaseUrl", "https://api4.thetvdb.com"},
                    {"Tvdb:ApiKey", ""}  // Will need to be set in appsettings.local.json
                };
                
                builder.Configuration.AddInMemoryCollection(defaultConfig);
                System.Diagnostics.Debug.WriteLine("Using default configuration - API key must be set in appsettings.local.json");
            }

            // Always try to load local settings (optional) - try embedded resource first, then file system
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var localStream = assembly.GetManifestResourceStream("TinderForMovies.appsettings.local.json");
                if (localStream != null)
                {
                    builder.Configuration.AddJsonStream(localStream);
                    System.Diagnostics.Debug.WriteLine("Local settings loaded from embedded resource");
                }
                else
                {
                    // Fallback to file system
                    builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
                    System.Diagnostics.Debug.WriteLine("Local settings loaded from file system");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Local settings not loaded: {ex.Message}");
            }
        }
    }
}
