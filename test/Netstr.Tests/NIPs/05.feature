Feature: NIP-05 DNS-based Identities
    Tests for NIP-05 DNS-based identity verification implementation

    Background:
        Given a relay at "wss://localhost:5001"
        And a user Alice
        And Alice is connected to the relay

    Scenario: Accept metadata event with valid NIP-05 identifier
        When Alice publishes a metadata event with NIP-05 identifier "alice@example.com"
        Then the relay accepts the event
        And the event is stored in the database

    Scenario: Accept metadata event with invalid NIP-05 identifier
        When Alice publishes a metadata event with NIP-05 identifier "invalid-format"
        Then the relay accepts the event
        And the event is stored in the database
        # Note: NIP-05 validation doesn't reject events, only logs verification results

    Scenario: Accept metadata event without NIP-05 identifier
        When Alice publishes a metadata event without NIP-05 identifier
        Then the relay accepts the event
        And the event is stored in the database

    Scenario: Handle metadata event with empty NIP-05 identifier
        When Alice publishes a metadata event with empty NIP-05 identifier
        Then the relay accepts the event
        And the event is stored in the database