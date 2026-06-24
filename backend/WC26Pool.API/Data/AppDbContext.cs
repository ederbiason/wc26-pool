using Microsoft.EntityFrameworkCore;
using WC26Pool.API.Models;

namespace WC26Pool.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<DayPredictionOrder> DayPredictionOrders => Set<DayPredictionOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Prediction>()
            .HasIndex(p => new { p.ParticipantId, p.MatchId })
            .IsUnique();

        modelBuilder.Entity<DayPredictionOrder>()
            .HasIndex(d => new { d.Date, d.ParticipantId })
            .IsUnique();

        modelBuilder.Entity<Participant>().HasData(
            new Participant { Id = 1, Name = "Pedro", IsAdmin = false, TotalPoints = 0 },
            new Participant { Id = 2, Name = "Gabriel", IsAdmin = false, TotalPoints = 0 },
            new Participant { Id = 3, Name = "Vinícius", IsAdmin = false, TotalPoints = 0 },
            new Participant { Id = 4, Name = "João", IsAdmin = false, TotalPoints = 0 },
            new Participant { Id = 5, Name = "Guilherme", IsAdmin = false, TotalPoints = 0 },
            new Participant { Id = 6, Name = "Eder", IsAdmin = true, TotalPoints = 0 }
        );
    }
}
