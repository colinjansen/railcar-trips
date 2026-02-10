using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RailcarTrips.Domain.Models;
using RailcarTrips.Infrastructure.Data;
using RailcarTrips.Infrastructure.Stores;

namespace RailcarTrips.UnitTests;

public sealed class TripProcessingStoreTests
{
    [Fact]
    public async Task AddEquipmentEventsAsync_WhenUniqueConstraintHits_ReturnsDuplicateWarning_AndPersistsRemaining()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Cities.AddRange(
                new City { Id = 1, Name = "Alpha", TimeZoneId = "UTC" },
                new City { Id = 2, Name = "Beta", TimeZoneId = "UTC" });
            seedContext.EquipmentEvents.Add(new EquipmentEvent
            {
                EquipmentId = "CAR1",
                EventCode = "W",
                EventLocalTime = new DateTime(2026, 1, 1, 0, 0, 0),
                EventUtcTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CityId = 1
            });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var store = new TripProcessingStore(dbContext);
        var result = await store.AddEquipmentEventsAsync(
            new[]
            {
                new EquipmentEvent
                {
                    EquipmentId = "CAR1",
                    EventCode = "W",
                    EventLocalTime = new DateTime(2026, 1, 1, 0, 0, 0),
                    EventUtcTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CityId = 1
                },
                new EquipmentEvent
                {
                    EquipmentId = "CAR1",
                    EventCode = "Z",
                    EventLocalTime = new DateTime(2026, 1, 2, 0, 0, 0),
                    EventUtcTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    CityId = 2
                }
            },
            CancellationToken.None);

        Assert.Equal(1, result.PersistedCount);
        Assert.Contains(result.Warnings, warning => warning.Code == "DuplicateEvent");
        Assert.Equal(2, await dbContext.EquipmentEvents.CountAsync());
    }

    [Fact]
    public async Task AddTripsAsync_WhenUniqueConstraintHits_ReturnsDuplicateWarning_AndPersistsRemaining()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Cities.AddRange(
                new City { Id = 1, Name = "Alpha", TimeZoneId = "UTC" },
                new City { Id = 2, Name = "Beta", TimeZoneId = "UTC" },
                new City { Id = 3, Name = "Gamma", TimeZoneId = "UTC" });
            seedContext.Trips.Add(new Trip
            {
                EquipmentId = "CAR1",
                OriginCityId = 1,
                DestinationCityId = 2,
                StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                TotalTripHours = 24
            });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var store = new TripProcessingStore(dbContext);
        var result = await store.AddTripsAsync(
            new[]
            {
                new Trip
                {
                    EquipmentId = "CAR1",
                    OriginCityId = 1,
                    DestinationCityId = 2,
                    StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    TotalTripHours = 24
                },
                new Trip
                {
                    EquipmentId = "CAR1",
                    OriginCityId = 2,
                    DestinationCityId = 3,
                    StartUtc = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    EndUtc = new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc),
                    TotalTripHours = 24
                }
            },
            [],
            CancellationToken.None);

        Assert.Equal(1, result.PersistedCount);
        Assert.Contains(result.Warnings, warning => warning.Code == "DuplicateTrip");
        Assert.Equal(2, await dbContext.Trips.CountAsync());
    }
}
