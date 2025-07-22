# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a cross-platform mobile application called "Tinder for Movies" built with .NET 9 MAUI and Blazor Hybrid. The app allows users to discover movies through a swipe-based interface, creating personalized watchlists of "matched" movies.

## Development Commands

### Building the Project
```bash
# Build the entire solution
dotnet build TinderForMovies.sln

# Build for specific platform
dotnet build TinderForMovies/TinderForMovies.csproj -f net9.0-android
dotnet build TinderForMovies/TinderForMovies.csproj -f net9.0-ios
dotnet build TinderForMovies/TinderForMovies.csproj -f net9.0-windows10.0.19041.0

# Clean build
dotnet clean TinderForMovies.sln
```

### Running the Application
```bash
# Run on Windows
dotnet run --project TinderForMovies/TinderForMovies.csproj -f net9.0-windows10.0.19041.0

# For mobile platforms, use Visual Studio or platform-specific deployment
```

### Testing
```bash
# Currently no test projects exist - tests should be added in future development
# When tests are added, use:
# dotnet test TinderForMovies.sln
```

### Package Management
```bash
# Restore NuGet packages
dotnet restore TinderForMovies.sln

# Add packages
dotnet add TinderForMovies/TinderForMovies.csproj package <PackageName>
```

## Architecture Overview

### Technology Stack
- **Framework**: .NET 9.0 with MAUI
- **UI**: Hybrid approach using MAUI XAML for performance-critical views (swipe interface) and Blazor for data-rich views (matches, reviews)
- **Language**: C# 13
- **Target Platforms**: iOS, Android, Windows, macOS

### Project Structure
```
TinderForMovies/
├── TinderForMovies.sln              # Solution file
├── TinderForMovies/                 # Main application project
│   ├── Components/                  # Blazor components
│   │   ├── Layout/                  # Layout components (MainLayout, NavMenu)
│   │   └── Pages/                   # Razor pages (Home, Counter, Weather)
│   ├── Platforms/                   # Platform-specific implementations
│   │   ├── Android/                 # Android-specific code
│   │   ├── iOS/                     # iOS-specific code  
│   │   ├── Windows/                 # Windows-specific code
│   │   ├── MacCatalyst/             # macOS-specific code
│   │   └── Tizen/                   # Tizen-specific code
│   ├── Resources/                   # Application resources
│   │   ├── AppIcon/                 # App icons
│   │   ├── Fonts/                   # Custom fonts
│   │   ├── Images/                  # Image assets
│   │   └── Splash/                  # Splash screen assets
│   ├── wwwroot/                     # Web assets for Blazor
│   ├── App.xaml                     # Application definition
│   ├── MainPage.xaml                # Main page layout
│   ├── MauiProgram.cs               # Application bootstrapping
│   └── TinderForMovies.csproj       # Project configuration
├── requirements.md                  # MVP requirements document
└── technical-architecture.md        # Detailed technical architecture
```

### Planned Architecture (Clean Architecture)
The application follows Clean Architecture principles with these layers:
- **Presentation Layer**: MAUI views and Blazor components
- **Application Layer**: Business logic orchestration and use cases
- **Domain Layer**: Core business entities and rules
- **Infrastructure Layer**: External APIs, database access, platform services

### Key Features to Implement
- **Movie Swiping Interface**: Swipe right to match, left to reject movies
- **Movie Profile Display**: Rich movie cards with posters, ratings, cast, synopsis
- **Matches Management**: View matched movies, mark as watched, add ratings/reviews
- **External API Integration**: TMDb API for movie data
- **Local Data Storage**: SQLite with Entity Framework Core
- **Cross-Platform UI**: Native MAUI for swipe gestures, Blazor for complex data views

## Development Guidelines

### File Organization
- Keep platform-specific code in respective `Platforms/` folders
- Use Blazor components in `Components/` for complex UI that benefits from web technologies
- Use MAUI XAML for performance-critical native UI like swipe interactions
- Place shared business logic in separate service classes

### Key Dependencies
- Microsoft.Maui.Controls
- Microsoft.AspNetCore.Components.WebView.Maui
- Microsoft.Extensions.Logging.Debug (debug builds)

### Current Status
This is a new project with basic MAUI Blazor template structure. The core movie discovery and swiping functionality has not yet been implemented. Refer to `requirements.md` and `technical-architecture.md` for detailed feature specifications and implementation guidance.