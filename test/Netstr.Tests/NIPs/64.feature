Feature: NIP-64 Chess (Portable Game Notation)
    Tests for NIP-64 Chess implementation

    Background:
        Given a relay at "wss://localhost:5001"
        And a user Alice
        And Alice is connected to the relay

    Scenario: Publish a simple chess game in progress
        When Alice publishes an event with kind 64 and content "1. e4 *"
        Then the relay accepts the event
        When Alice subscribes to events with kind 64
        Then Alice receives 1 event
        And the event content is "1. e4 *"

    Scenario: Publish a chess game with basic moves
        When Alice publishes an event with kind 64 and content "1. e4 e5 2. Nf3 Nc6 3. Bb5 *"
        Then the relay accepts the event
        When Alice subscribes to events with kind 64
        Then Alice receives 1 event

    Scenario: Publish a complete chess game with PGN headers
        When Alice publishes an event with kind 64 and content:
        """
        [Event "F/S Return Match"]
        [Site "Belgrade, Serbia JUG"]
        [Date "1992.11.04"]
        [Round "29"]
        [White "Fischer, Robert J."]
        [Black "Spassky, Boris V."]
        [Result "1/2-1/2"]

        1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6 5. O-O Be7 6. Re1 b5 7. Bb3 d6 1/2-1/2
        """
        Then the relay accepts the event
        When Alice subscribes to events with kind 64
        Then Alice receives 1 event

    Scenario: Publish chess game with alt tag for non-supporting clients
        When Alice publishes an event with kind 64 and tags:
            | alt | Fischer vs. Spassky in Belgrade on 1992-11-04 |
        And content "1. e4 e5 2. Nf3 Nc6 3. Bb5 1/2-1/2"
        Then the relay accepts the event
        When Alice subscribes to events with kind 64
        Then Alice receives 1 event
        And the event has tag "alt" with value "Fischer vs. Spassky in Belgrade on 1992-11-04"

    Scenario: Publish unknown result game
        When Alice publishes an event with kind 64 and content "*"
        Then the relay accepts the event
        When Alice subscribes to events with kind 64
        Then Alice receives 1 event

    Scenario: Reject empty chess content
        When Alice publishes an event with kind 64 and content ""
        Then the relay rejects the event with "invalid: chess content is empty or malformed"

    Scenario: Reject invalid PGN format
        When Alice publishes an event with kind 64 and content "invalid chess moves here"
        Then the relay rejects the event with "invalid: PGN format is not valid"

    Scenario: Accept castling notation
        When Alice publishes an event with kind 64 and content "1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. O-O O-O *"
        Then the relay accepts the event
        When Alice subscribes to events with kind 64
        Then Alice receives 1 event

    Scenario: Accept game with result
        When Alice publishes an event with kind 64 and content "1. f3 e5 2. g4 Qh4# 0-1"
        Then the relay accepts the event
        When Alice subscribes to events with kind 64
        Then Alice receives 1 event