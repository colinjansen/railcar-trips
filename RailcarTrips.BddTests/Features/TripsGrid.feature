Feature: Trips grid
  As a user
  I want to view trips in a grid
  So that I can review origins, destinations, and durations

  Scenario: Trips grid is sorted by start time descending
    Given the city lookup is seeded
    And a CSV with events:
      | EquipmentId | EventCode | EventTime           | CityId |
      | CAR1        | W         | 2026-01-01 00:00    | 1      |
      | CAR1        | Z         | 2026-01-02 00:00    | 2      |
      | CAR2        | W         | 2026-02-01 00:00    | 1      |
      | CAR2        | Z         | 2026-02-02 00:00    | 2      |
    When I process the CSV
    Then trips are returned sorted by start time descending
