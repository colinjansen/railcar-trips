# Plan

## Goals
- Build a Blazor WebAssembly app with a “Railcar Trips” page that ingests equipment events CSV, processes trips, and stores trips in a database via Entity Framework.
- Pre-seed the other CSVs (e.g., canadian_cities.csv) via script/seed code.
- Show trips in a grid and optionally show events per trip.
- Document assumptions and open questions.

## Proposed stack and structure
- Blazor WebAssembly hosted by ASP.NET Core (Client/Server/Shared) for server-side EF and file upload.
- EF Core + SQLite for simplicity (file-based DB) unless a different DB is preferred.
- Server endpoints: upload CSV, trigger processing, query trips, query trip events.

## Data model (draft)
- City: Id, Name, TimeZoneId (from canadian_cities.csv).
- EquipmentEvent: Id, EquipmentId, CityId, EventCode, EventLocalTime, EventUtcTime.
- Trip: Id, EquipmentId, OriginCityId, DestinationCityId, StartUtc, EndUtc, TotalHours.
- TripEvent (optional): Id, TripId, EventId, Sequence.

## Processing flow (draft)
1. Parse equipment_events.csv.
2. Map city name to CityId and timezone.
3. Convert local event time to UTC (timezone conversion per event).
4. Group by EquipmentId.
5. Sort events by UTC time per equipment.
6. Build trips: W starts, Z ends; ignore/flag invalid sequences.
7. Persist events + trips (and trip-event linkage if implemented).

## UI flow (draft)
- Railcar Trips page with:
  - CSV upload control + “Process” button.
  - Status/validation messages.
  - Trips grid (equipment id, origin, destination, start, end, total hours).
  - Optional: select trip to show ordered events in a detail panel.

## Implementation steps
1. Create solution with hosted Blazor WASM + Server + Shared.
2. Add EF Core models, DbContext, migrations, SQLite config.
3. Build DB seeding for canadian_cities.csv (and other CSV if needed).
4. Create and confirm BDD tests to make sure all asoects of the application are tested and their behaviour defined
5. Implement CSV parser + time zone conversion utilities.
6. Implement trip processing service.
7. Add API endpoints for upload/process and querying trips/events.
8. Build Railcar Trips UI with upload + grid + details.
9. Add TODOs/notes for edge cases and incomplete items.

## Assumptions
- SQLite is acceptable for local demo.
- City names in events match those in canadian_cities.csv.
- Event codes only W and Z are used for trip boundaries.

These assumptions are all correct.

## Open questions
- Preferred database (SQLite vs. SQL Server)? 
  - for this project, I want to use an adapter pattern and we'll use SQLite
- Desired behavior for missing W/Z pairs or overlapping trips?
  - this would indicate a problem with the input data. log the error to console and try to continue
- Should raw events be stored always, or only derived trips?
  - store raw events for debugging purposes but make the level of storage a variable in appsettings.json
- Expected volume/performance constraints?
  - this is just a PoC so no constraints of that nature yet
- logging should be to the console AND to a separate, rolling file

Please let me know if there are any more questions that need to be answered before moving forward.