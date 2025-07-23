# tinder-for-movies
Swipe-based movie discovery app built with .NET MAUI and Blazor Hybrid

## API Key

This app requires an API key from thetvdb.com. Go to thetvdb.com/api-information and create a key, then add an `appsettings.local.json` file with the following contents:

```
{
  "Tvdb": {
    "ApiKey": "YOUR_KEY_HERE"
  }
}
```