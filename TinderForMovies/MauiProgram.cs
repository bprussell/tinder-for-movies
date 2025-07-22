using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TinderForMovies.Configuration;
using TinderForMovies.Services;

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

            // Add HTTP client and services
            builder.Services.AddHttpClient<ITvdbService, TvdbService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
