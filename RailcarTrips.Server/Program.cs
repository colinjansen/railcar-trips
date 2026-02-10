using Microsoft.EntityFrameworkCore;
using RailcarTrips.Application.Abstractions;
using RailcarTrips.Application.UseCases;
using RailcarTrips.Infrastructure.Data;
using RailcarTrips.Infrastructure.Services;
using RailcarTrips.Infrastructure.Stores;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=railcartrips.db"));
builder.Services.AddScoped<ITripProcessingStore, TripProcessingStore>();
builder.Services.AddScoped<ITripReadStore, TripReadStore>();
builder.Services.AddScoped<ICsvReader, CsvReader>();
builder.Services.AddScoped<ProcessTripsUseCase>();
builder.Services.AddScoped<TripQueryService>();
builder.Services.AddSingleton<ITimeZoneResolver, TimeZoneResolver>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
    await DbInitializer.SeedAsync(dbContext, env, logger);
}

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
