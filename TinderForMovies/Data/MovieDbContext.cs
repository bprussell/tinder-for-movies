using Microsoft.EntityFrameworkCore;
using TinderForMovies.Models;

namespace TinderForMovies.Data;

public class MovieDbContext : DbContext
{
    public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options)
    {
    }

    public DbSet<UserMovieInteraction> UserMovieInteractions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure UserMovieInteraction
        modelBuilder.Entity<UserMovieInteraction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MovieTitle).IsRequired().HasMaxLength(500);
            entity.Property(e => e.MoviePosterUrl).HasMaxLength(1000);
            entity.Property(e => e.MovieOverview).HasMaxLength(2000);
            entity.Property(e => e.MovieYear).HasMaxLength(10);
            entity.Property(e => e.MovieGenres).HasMaxLength(500);
            entity.Property(e => e.UserReview).HasMaxLength(2000);
            
            // Create index for faster queries
            entity.HasIndex(e => e.MovieId);
            entity.HasIndex(e => e.InteractionType);
            entity.HasIndex(e => e.InteractionDate);
        });
    }
}