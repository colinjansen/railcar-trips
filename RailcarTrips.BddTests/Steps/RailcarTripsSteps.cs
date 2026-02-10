using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using RailcarTrips.Application.UseCases;
using RailcarTrips.Domain.Models;
using RailcarTrips.Infrastructure.Services;
using RailcarTrips.Shared.Dtos;
using TechTalk.SpecFlow;
using Xunit;

namespace RailcarTrips.BddTests.Steps;

[Binding]
public sealed class RailcarTripsSteps
{
    private readonly InMemoryTripStore _store = new();
    private readonly FakeTimeZoneResolver _resolver = new();
    private string _csv = string.Empty;
    private ProcessResultDto? _result;

    public RailcarTripsSteps()
    {
        _resolver.Add("UTC", TimeZoneInfo.Utc);
        _resolver.Add("Test/Invalid", CreateInvalidTimeZone());
    }

    [Given("the city lookup is seeded")]
    public void GivenTheCityLookupIsSeeded()
    {
        _store.Cities[1] = new City { Id = 1, Name = "Alpha", TimeZoneId = "UTC" };
        _store.Cities[2] = new City { Id = 2, Name = "Beta", TimeZoneId = "UTC" };
        _store.Cities[3] = new City { Id = 3, Name = "Gamma", TimeZoneId = "Test/Invalid" };
    }

    [Given("a CSV with events:")]
    public void GivenACsvWithEvents(Table table)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Equipment Id,Event Code,Event Time,City Id");

        foreach (var row in table.Rows)
        {
            builder.AppendLine(string.Join(',',
                row["EquipmentId"],
                row["EventCode"],
                row["EventTime"],
                row["CityId"]));
        }

        _csv = builder.ToString();
    }

    [When("I process the CSV")]
    public async Task WhenIProcessTheCsv()
    {
        _result = await ExecuteUseCaseAsync();
    }

    [When("I process the CSV again")]
    public async Task WhenIProcessTheCsvAgain()
    {
        _result = await ExecuteUseCaseAsync();
    }

    [Then("(\\d+) trips are created")]
    public void ThenTripsAreCreated(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result!.TripsCreated);
    }

    [Then("(\\d+) errors are reported")]
    public void ThenErrorsAreReported(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result!.ErrorCount);
    }

    [Then("(\\d+) warnings are reported")]
    public void ThenWarningsAreReported(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result!.WarningCount);
    }

    [Then("(\\d+) stored events exist")]
    public void ThenStoredEventsExist(int count)
    {
        Assert.Equal(count, _store.EquipmentEvents.Count);
    }

    [Then("the stored local time is adjusted to (.*)")]
    public void ThenStoredLocalTimeAdjustedTo(string timestamp)
    {
        Assert.Single(_store.EquipmentEvents);
        var expected = DateTime.ParseExact(timestamp, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        Assert.Equal(expected, _store.EquipmentEvents[0].EventLocalTime);
    }

    [Then("trips are returned sorted by start time descending")]
    public async Task ThenTripsAreReturnedSorted()
    {
        var query = new TripQueryService(_store);
        var trips = await query.GetTrips(CancellationToken.None);

        Assert.Equal(2, trips.Count);
        Assert.True(trips[0].StartUtc >= trips[1].StartUtc);
    }

    private async Task<ProcessResultDto> ExecuteUseCaseAsync()
    {
        var useCase = new ProcessTripsUseCase(
            _store,
            _resolver,
            new CsvReader(),
            NullLogger<ProcessTripsUseCase>.Instance);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(_csv));
        return await useCase.Execute(stream);
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
