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

            // Add configuration
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

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
    }
}
