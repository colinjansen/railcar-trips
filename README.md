# RailcarTrips

RailcarTrips is a hosted ASP.NET Core + Blazor WebAssembly app for processing railcar equipment event CSV files and deriving trips.

## Features

- Upload an equipment events CSV file from the UI.
- Process events into persisted equipment events and derived trips.
- Show processing summary counts: parsed, stored, trips created, warnings, and errors.
- Toggle warning details on/off after processing.
- View trips in a grid (including origin, destination, UTC start/end, and total hours).
- Select a trip to view its detailed trip events.
- Idempotent processing behavior for duplicate input.

## Run the Application

### Prerequisites

- .NET 10 SDK

### Start the app

```bash
dotnet restore RailcarTrips.sln
dotnet build RailcarTrips.sln
dotnet run --project RailcarTrips.Server
```

Notes:

- Run the `RailcarTrips.Server` project; it hosts the API and serves the Blazor client.
- The launch profile uses dynamic ports (`:0`), so use the URL printed in console output.
- The app uses SQLite (`railcartrips.db`) and seeds city reference data on first run.

## Tests and Code Coverage

Coverage tooling is already configured in this repo:

- `coverlet.msbuild` + `coverlet.collector` in test projects
- `reportgenerator` local tool (`.config/dotnet-tools.json`)
- automatic HTML report generation via `Directory.Build.targets`

### Run tests with coverage

```bash
dotnet tool restore
dotnet test RailcarTrips.UnitTests/RailcarTrips.UnitTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=TestResults/Coverage/
dotnet test RailcarTrips.BddTests/RailcarTrips.BddTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=TestResults/Coverage/
```

### Coverage report locations

- Unit tests HTML report: `RailcarTrips.UnitTests/TestResults/coverage-report/index.html`
- BDD tests HTML report: `RailcarTrips.BddTests/TestResults/coverage-report/index.html`
- Cobertura XML:
  - `RailcarTrips.UnitTests/TestResults/Coverage/coverage.cobertura.xml`
  - `RailcarTrips.BddTests/TestResults/Coverage/coverage.cobertura.xml`

### Current coverage snapshot (from local run on 2026-02-10)

Unit tests (`RailcarTrips.UnitTests`):

- Total line coverage: `77.01%`
- Total branch coverage: `78.12%`
- Total method coverage: `79.03%`

BDD tests (`RailcarTrips.BddTests`):

- Total line coverage: `51.58%`
- Total branch coverage: `50.78%`
- Total method coverage: `66.93%`

These values are per test project run and are not a merged combined report.
