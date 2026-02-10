Feature: Railcar trips processing
  As a user
  I want to upload railcar/equipment events
  So that trips are derived, stored, and visible in the UI

  Background:
    Given the city lookup is seeded

  Scenario: Upload and process a valid events file
    Given a CSV with events:
      | EquipmentId | EventCode | EventTime           | CityId |
      | CAR1        | W         | 2026-01-01 00:00    | 1      |
      | CAR1        | Z         | 2026-01-02 00:00    | 2      |
    When I process the CSV
    Then 1 trips are created
    And 0 errors are reported

  Scenario: Events are ordered per equipment before processing
    Given a CSV with events:
      | EquipmentId | EventCode | EventTime           | CityId |
      | CAR1        | Z         | 2026-01-02 00:00    | 2      |
      | CAR1        | W         | 2026-01-01 00:00    | 1      |
    When I process the CSV
    Then 1 trips are created
    And 0 warnings are reported

  Scenario: Multiple equipment are processed independently
    Given a CSV with events:
      | EquipmentId | EventCode | EventTime           | CityId |
      | CAR1        | W         | 2026-01-01 00:00    | 1      |
      | CAR1        | Z         | 2026-01-02 00:00    | 2      |
      | CAR2        | W         | 2026-01-03 00:00    | 1      |
      | CAR2        | Z         | 2026-01-04 00:00    | 2      |
    When I process the CSV
    Then 2 trips are created

  Scenario: Missing end event is handled deterministically
    Given a CSV with events:
      | EquipmentId | EventCode | EventTime           | CityId |
      | CAR1        | W         | 2026-01-01 00:00    | 1      |
    When I process the CSV
    Then 0 trips are created
    And 1 warnings are reported

  Scenario: Missing start event is handled deterministically
    Given a CSV with events:
      | EquipmentId | EventCode | EventTime           | CityId |
      | CAR1        | Z         | 2026-01-02 00:00    | 2      |
    When I process the CSV
    Then 0 trips are created
    And 1 warnings are reported

  Scenario: Unknown city in events
    Given a CSV with events:
      | EquipmentId | EventCode | EventTime           | CityId |
      | CAR1        | W         | 2026-01-01 00:00    | 99     |
    When I process the CSV
    Then 1 errors are reported

  Scenario: Invalid time workaround
    Given a CSV with events:
      | EquipmentId | EventCode | EventTime           | CityId |
      | CAR1        | W         | 2026-03-08 02:30    | 3      |
    When I process the CSV
    Then 2 warnings are reported
    And the stored local time is adjusted to 2026-03-08 03:30

  Scenario: Duplicate input is idempotent
    Given a CSV with events:
      | EquipmentId | EventCode | EventTime           | CityId |
      | CAR1        | W         | 2026-01-01 00:00    | 1      |
    When I process the CSV
    And I process the CSV again
    Then 2 warnings are reported
    And 1 stored events exist
