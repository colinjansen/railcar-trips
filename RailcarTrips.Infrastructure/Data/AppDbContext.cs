using Microsoft.EntityFrameworkCore;
using RailcarTrips.Domain.Models;

namespace RailcarTrips.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<City> Cities => Set<City>();
    public DbSet<EquipmentEvent> EquipmentEvents => Set<EquipmentEvent>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripEvent> TripEvents => Set<TripEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>()
            .HasIndex(c => c.Name);

        modelBuilder.Entity<EquipmentEvent>()
            .HasIndex(e => new { e.EquipmentId, e.EventUtcTime, e.EventCode, e.CityId });

        modelBuilder.Entity<Trip>()
            .HasIndex(t => new { t.EquipmentId, t.StartUtc, t.EndUtc });

        modelBuilder.Entity<TripEvent>()
            .HasIndex(te => new { te.TripId, te.Sequence });
    }
}
