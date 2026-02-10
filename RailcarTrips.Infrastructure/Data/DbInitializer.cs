using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using RailcarTrips.Domain.Models;

namespace RailcarTrips.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext dbContext, IHostEnvironment env, ILogger logger)
    {
        await dbContext.Database.EnsureCreatedAsync();

        if (await dbContext.Cities.AnyAsync())
        {
            // if we have cities, we assume the rest of the reference data is also seeded and skip seeding.
            return;
        }

        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed Data", "canadian_cities.csv");
        if (!File.Exists(dataPath))
        {
            logger.LogWarning("City seed file not found at {Path}", dataPath);
            return;
        }

        var lines = await File.ReadAllLinesAsync(dataPath);
        if (lines.Length <= 1)
        {
            logger.LogWarning("City seed file is empty: {Path}", dataPath);
            return;
        }

        var cities = new List<City>();

        // we are starting at 1 to skip the header row
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                logger.LogWarning("Skipping invalid city row: {Row}", line);
                continue;
            }

            if (!int.TryParse(parts[0], out var id))
            {
                logger.LogWarning("Skipping city row with invalid id: {Row}", line);
                continue;
            }

            var name = parts[1];
            var tz = parts[2];

            logger.LogInformation("Seeding city: {Name} (ID: {Id}, TimeZone: {TimeZone})", name, id, tz);
            cities.Add(new City
            {
                Id = id,
                Name = name,
                TimeZoneId = tz
            });
        }

        if (cities.Count == 0)
        {
            logger.LogWarning("No cities found to seed.");
            return;
        }

        dbContext.Cities.AddRange(cities);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} cities.", cities.Count);
    }
}
