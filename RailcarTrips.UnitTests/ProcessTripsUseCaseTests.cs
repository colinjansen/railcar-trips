using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using RailcarTrips.Application.UseCases;
using RailcarTrips.Domain.Models;
using RailcarTrips.UnitTests.Helpers;

namespace RailcarTrips.UnitTests;

public sealed class ProcessTripsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_SkipsUnknownCity()
    {
        var store = new InMemoryTripStore();
        var resolver = new FakeTimeZoneResolver();
        var csvReader = new CsvReader();
        resolver.Add("UTC", TimeZoneInfo.Utc);

        var useCase = new ProcessTripsUseCase(store, resolver, csvReader, NullLogger<ProcessTripsUseCase>.Instance);

        var csv = "Equipment Id,Event Code,Event Time,City Id\nCAR1,W,2026-01-01 00:00,99";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await useCase.Execute(stream);

        Assert.Equal(1, result.ParsedEvents);
        Assert.Equal(1, result.ErrorCount);
        Assert.Empty(store.EquipmentEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsWhenCsvIsEmpty()
    {
        var store = new InMemoryTripStore();
        var resolver = new FakeTimeZoneResolver();
        var csvReader = new CsvReader();
        var useCase = new ProcessTripsUseCase(store, resolver, csvReader, NullLogger<ProcessTripsUseCase>.Instance);

        await using var stream = new MemoryStream();

        var result = await useCase.Execute(stream);

        Assert.Equal(0, result.ParsedEvents);
        Assert.Equal(0, result.StoredEvents);
        Assert.Equal(0, result.TripsCreated);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesInvalidRows()
    {
        var store = new InMemoryTripStore();
        store.Cities[1] = new City { Id = 1, Name = "TestCity", TimeZoneId = "UTC" };

        var resolver = new FakeTimeZoneResolver();
        resolver.Add("UTC", TimeZoneInfo.Utc);

        var csvReader = new CsvReader();
        var useCase = new ProcessTripsUseCase(store, resolver, csvReader, NullLogger<ProcessTripsUseCase>.Instance);

        var csv = string.Join('\n', new[]
        {
            "Equipment Id,Event Code,Event Time,City Id",
            "CAR1,W,2026-01-01 00:00", // invalid length
            "CAR1,W,invalid-date,1",   // invalid date
            "CAR1,W,2026-01-01 00:00,not-a-number", // invalid city id
            "",
            "CAR1,W,2026-01-01 00:00,1"
        });

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await useCase.Execute(stream);

        Assert.Equal(1, result.ParsedEvents);
        //Assert.Equal(4, result.WarningCount);
        Assert.Equal(1, result.StoredEvents);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesMissingTimeZone()
    {
        var store = new InMemoryTripStore();
        store.Cities[1] = new City { Id = 1, Name = "TestCity", TimeZoneId = "Missing" };

        var resolver = new FakeTimeZoneResolver();
        var csvReader = new CsvReader();

        var useCase = new ProcessTripsUseCase(store, resolver, csvReader, NullLogger<ProcessTripsUseCase>.Instance);

        var csv = "Equipment Id,Event Code,Event Time,City Id\nCAR1,W,2026-01-01 00:00,1";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await useCase.Execute(stream);

        Assert.Equal(1, result.ErrorCount);
        Assert.Empty(store.EquipmentEvents);
    }

    [Fact]
    public async Task ExecuteAsync_AdjustsInvalidLocalTime()
    {
        var store = new InMemoryTripStore();
        store.Cities[1] = new City { Id = 1, Name = "TestCity", TimeZoneId = "Test/Invalid" };

        var resolver = new FakeTimeZoneResolver();
        resolver.Add("Test/Invalid", CreateInvalidTimeZone());

        var csvReader = new CsvReader();
        var useCase = new ProcessTripsUseCase(store, resolver, csvReader, NullLogger<ProcessTripsUseCase>.Instance);

        var csv = "Equipment Id,Event Code,Event Time,City Id\nCAR1,W,2026-03-08 02:30,1";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await useCase.Execute(stream);

        Assert.Equal(2, result.WarningCount);
        Assert.Single(store.EquipmentEvents);
        Assert.Equal(new DateTime(2026, 3, 8, 3, 30, 0), store.EquipmentEvents[0].EventLocalTime);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsDuplicateEvents()
    {
        var store = new InMemoryTripStore();
        store.Cities[1] = new City { Id = 1, Name = "TestCity", TimeZoneId = "UTC" };

        var resolver = new FakeTimeZoneResolver();
        resolver.Add("UTC", TimeZoneInfo.Utc);

        store.EquipmentEvents.Add(new EquipmentEvent
        {
            EquipmentId = "CAR1",
            EventCode = "W",
            EventLocalTime = new DateTime(2026, 1, 1, 0, 0, 0),
            EventUtcTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CityId = 1
        });

        var csvReader = new CsvReader();
        var useCase = new ProcessTripsUseCase(store, resolver, csvReader, NullLogger<ProcessTripsUseCase>.Instance);

        var csv = "Equipment Id,Event Code,Event Time,City Id\nCAR1,W,2026-01-01 00:00,1";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await useCase.Execute(stream);

        Assert.Equal(2, result.WarningCount);
        Assert.Single(store.EquipmentEvents);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsDuplicateEventsWithinSameCsvUpload()
    {
        var store = new InMemoryTripStore();
        store.Cities[1] = new City { Id = 1, Name = "TestCity", TimeZoneId = "UTC" };

        var resolver = new FakeTimeZoneResolver();
        resolver.Add("UTC", TimeZoneInfo.Utc);

        var csvReader = new CsvReader();
        var useCase = new ProcessTripsUseCase(store, resolver, csvReader, NullLogger<ProcessTripsUseCase>.Instance);

        var csv = string.Join('\n', new[]
        {
            "Equipment Id,Event Code,Event Time,City Id",
            "CAR1,W,2026-01-01 00:00,1",
            "CAR1,W,2026-01-01 00:00,1"
        });
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await useCase.Execute(stream);

        Assert.Equal(2, result.ParsedEvents);
        Assert.Equal(1, result.StoredEvents);
        Assert.Equal(2, result.WarningCount);
        Assert.Single(store.EquipmentEvents);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesTripAndTripEvents()
    {
        var store = new InMemoryTripStore();
        store.Cities[1] = new City { Id = 1, Name = "Alpha", TimeZoneId = "UTC" };
        store.Cities[2] = new City { Id = 2, Name = "Beta", TimeZoneId = "UTC" };

        var resolver = new FakeTimeZoneResolver();
        resolver.Add("UTC", TimeZoneInfo.Utc);

        var csvReader = new CsvReader();
        var useCase = new ProcessTripsUseCase(store, resolver, csvReader, NullLogger<ProcessTripsUseCase>.Instance);

        var csv = string.Join('\n', new[]
        {
            "Equipment Id,Event Code,Event Time,City Id",
            "CAR1,W,2026-01-01 00:00,1",
            "CAR1,Z,2026-01-02 00:00,2"
        });
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await useCase.Execute(stream);

        Assert.Equal(2, result.StoredEvents);
        Assert.Equal(1, result.TripsCreated);
        Assert.Single(store.Trips);
        Assert.Equal(2, store.TripEvents.Count);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsDuplicateTrips()
    {
        var store = new InMemoryTripStore();
        store.Cities[1] = new City { Id = 1, Name = "Alpha", TimeZoneId = "UTC" };
        store.Cities[2] = new City { Id = 2, Name = "Beta", TimeZoneId = "UTC" };

        var existingTrip = new Trip
        {
            EquipmentId = "CAR1",
            OriginCityId = 1,
            DestinationCityId = 2,
            StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            TotalTripHours = 24
        };
        store.Trips.Add(existingTrip);

        var resolver = new FakeTimeZoneResolver();
        resolver.Add("UTC", TimeZoneInfo.Utc);

        var csvReader = new CsvReader();
        var useCase = new ProcessTripsUseCase(store, resolver, csvReader, NullLogger<ProcessTripsUseCase>.Instance);

        var csv = string.Join('\n', new[]
        {
            "Equipment Id,Event Code,Event Time,City Id",
            "CAR1,W,2026-01-01 00:00,1",
            "CAR1,Z,2026-01-02 00:00,2"
        });
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await useCase.Execute(stream);

        Assert.Equal(1, result.WarningCount);
        Assert.Equal(0, result.TripsCreated);
        Assert.Single(store.Trips);
    }

    private static TimeZoneInfo CreateInvalidTimeZone()
    {
        var baseOffset = TimeSpan.FromHours(-8);
        var daylightOffset = TimeSpan.FromHours(1);
        var start = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday);
        var end = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2000, 1, 1),
            new DateTime(2100, 12, 31),
            daylightOffset,
            start,
            end);
        return TimeZoneInfo.CreateCustomTimeZone("Test/Invalid", baseOffset, "Test", "Test", "Test", [rule]);
    }
}
