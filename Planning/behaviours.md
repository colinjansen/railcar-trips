# Behaviours / BDD Acceptance Criteria

## Feature: Railcar trips processing
As a user
I want to upload railcar/equipment events
So that trips are derived, stored, and visible in the UI

### Background
Given the database is initialized
And canadian_cities.csv is seeded into the City table with valid time zone ids

### Scenario: Upload and process a valid events file
Given I am on the Railcar Trips page
When I upload a valid equipment_events.csv file
And I click "Process"
Then the system parses all events
And converts each event's local timestamp to UTC using the city's time zone
And stores the parsed events in the database
And derives trips using W as start and Z as end
And stores trips in the database with start UTC, end UTC, and total trip hours
And I see the newly created trips in the grid

### Scenario: Events are ordered per equipment before processing
Given events for the same equipment are not ordered in the file
When I process the file
Then the system sorts events by UTC time per equipment
And trip boundaries are determined in that order

### Scenario: Multiple equipment are processed independently
Given the file contains events for multiple equipment ids
When I process the file
Then trips are derived independently per equipment id
And trips for each equipment id appear in the grid

### Scenario: Trip duration is computed correctly
Given a trip with a W event at 2024-01-01 08:00 local time in Vancouver
And a Z event at 2024-01-02 08:00 local time in Toronto
When the trip is processed
Then the stored start and end times are in UTC
And the total trip hours equals the difference between end UTC and start UTC

### Scenario: Trip events are visible in order (nice-to-have)
Given a trip exists with multiple underlying events
When I select the trip in the grid
Then I see its events ordered by UTC time

### Scenario: Missing end event is handled deterministically
Given a W event exists for an equipment id without a subsequent Z event
When I process the file
Then no trip is created for that incomplete pair
And the system logs a warning with the equipment id and event timestamp

### Scenario: Missing start event is handled deterministically
Given a Z event exists for an equipment id without a prior W event
When I process the file
Then no trip is created for that incomplete pair
And the system logs a warning with the equipment id and event timestamp

### Scenario: Overlapping trips are handled deterministically
Given an equipment id has a W event before a prior trip has ended
When I process the file
Then the system logs a warning about overlapping trips
And the overlapping segment is ignored (no trip created) until a valid W→Z pair is found

### Scenario: Unknown city in events
Given an event references a city not present in the City table
When I process the file
Then the system logs an error with the event row data
And that event is skipped
And processing continues for other events

### Scenario: Upload validation
Given I upload a file with missing required columns
When I click "Process"
Then I see a validation error
And no events or trips are stored

### Scenario: Idempotent processing (optional)
Given I process the same file twice
When I re-run processing
Then the system either rejects the duplicate input or prevents duplicate trips and events
And the behavior is clearly logged

### Scenario: Logging is produced during processing
When I process a file
Then the system logs the number of events parsed
And logs the number of trips created
And logs any warnings or errors encountered

## Feature: Trips grid
As a user
I want to view trips in a grid
So that I can review origins, destinations, and durations

### Scenario: Trips grid shows required columns
Given trips exist in the database
When I open the Railcar Trips page
Then I see a grid with equipment id, origin city, destination city, start date/time, end date/time, and total trip hours

### Scenario: Trips grid is sorted by start time (default)
Given trips exist in the database
When I open the Railcar Trips page
Then the grid is sorted by start UTC descending by default

## Feature: Data seeding
As a developer
I want city data seeded
So that time zone conversion works during processing

### Scenario: Seed cities from canadian_cities.csv
Given the application starts with an empty City table
When the seed routine runs
Then all cities from canadian_cities.csv are inserted
And each city has a valid time zone id

## Feature: Testing and BDD
As a developer
I want BDD-style tests
So that behaviors are well defined and verified

### Scenario: Core processing behaviors are covered
Given the test suite runs
Then there are BDD-style tests that cover:
  - W→Z trip creation
  - out-of-order events
  - missing W or Z
  - unknown city
  - time zone conversion
  - duplicate/idempotent processing behavior
