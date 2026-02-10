using Microsoft.EntityFrameworkCore;
using RailcarTrips.Domain.Models;
using RailcarTrips.Infrastructure.Data;

namespace RailcarTrips.UnitTests;

public sealed class AppDbContextModelTests
{
    [Fact]
    public void Model_ConfiguresUniqueIndexes_ForEventAndTripNaturalKeys()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var dbContext = new AppDbContext(options);

        var eventEntity = dbContext.Model.FindEntityType(typeof(EquipmentEvent));
        var tripEntity = dbContext.Model.FindEntityType(typeof(Trip));

        Assert.NotNull(eventEntity);
        Assert.NotNull(tripEntity);

        var eventIndex = eventEntity!.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(
                new[] { nameof(EquipmentEvent.EquipmentId), nameof(EquipmentEvent.EventUtcTime), nameof(EquipmentEvent.EventCode), nameof(EquipmentEvent.CityId) }));

        var tripIndex = tripEntity!.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(
                new[] { nameof(Trip.EquipmentId), nameof(Trip.StartUtc), nameof(Trip.EndUtc) }));

        Assert.True(eventIndex.IsUnique);
        Assert.True(tripIndex.IsUnique);
    }
}
