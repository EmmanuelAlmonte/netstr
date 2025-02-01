Feature: NIP-65 Relay List Metadata
    As a NOSTR client
    I want to publish and retrieve my relay preferences
    So that other clients know which relays I use

    Background:
        Given I am connected to the relay
        And I am authenticated

    Scenario: Publishing valid relay list
        When I publish an event with kind 10002 and tags:
            | r | wss://relay1.com | read  | write |
            | r | wss://relay2.com | read  |       |
            | r | wss://relay3.com | write |       |
        Then I should receive an "OK" message
        And the relay configurations should be stored for my public key

    Scenario: Updating existing relay list
        Given I have published relay configurations
        When I publish an event with kind 10002 and tags:
            | r | wss://relay1.com | read |
            | r | wss://relay4.com | write |
        Then I should receive an "OK" message
        And my old relay configurations should be replaced
        And the new relay configurations should be stored

    Scenario: Publishing empty relay list
        When I publish an event with kind 10002 and no tags
        Then I should receive an error message containing "must contain at least one relay tag"

    Scenario: Publishing invalid relay URL
        When I publish an event with kind 10002 and tags:
            | r | invalid-url | read | write |
        Then I should receive an error message containing "Invalid relay URL format"

    Scenario: Publishing invalid permission marker
        When I publish an event with kind 10002 and tags:
            | r | wss://relay1.com | invalid |
        Then I should receive an error message containing "Invalid relay permission marker"

    Scenario: Retrieving relay configurations
        Given I have published relay configurations
        When I request relay configurations for my public key
        Then I should receive my relay configurations
