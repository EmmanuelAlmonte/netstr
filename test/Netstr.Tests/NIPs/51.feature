Feature: NIP-51 Lists
    Tests for NIP-51 Lists implementation

    Background:
        Given a relay at "wss://localhost:5001"
        And a user Alice
        And Alice is connected to the relay

    Scenario: Create and retrieve a public mute list
        When Alice publishes an event with kind 10000 and tags:
            | p | 07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9 |
            | p | a55c15f5e41d5aebd236eca5e0142789c5385703f1a7485aa4b38d94fd18dcc4 |
        Then the relay accepts the event
        When Alice subscribes to events with kind 10000
        Then Alice receives 1 event
        And the event has 2 "p" tags

    Scenario: Create and retrieve a private mute list
        When Alice publishes an event with kind 10000 and encrypted content and tags:
            | p | 07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9 |
        Then the relay accepts the event
        When Alice subscribes to events with kind 10000
        Then Alice receives 1 event
        And the event has encrypted content
        And the event has 1 "p" tag

    Scenario: Create and retrieve a bookmark set
        When Alice publishes an event with kind 30003 and tags:
            | d | my-bookmarks |
            | name | Programming Resources |
            | about | Collection of useful programming articles and tutorials |
            | e | d78ba0d5dce22bfff9db0a9e996c9ef27e2c91051de0c4e1da340e0326b4941e |
            | a | 30023:26dc95542e18b8b7aec2f14610f55c335abebec76f3db9e58c254661d0593a0c:95ODQzw3 |
        Then the relay accepts the event
        When Alice subscribes to events with kind 30003
        Then Alice receives 1 event
        And the event has tag "d" with value "my-bookmarks"
        And the event has tag "name" with value "Programming Resources"

    Scenario: Create and retrieve an emoji set
        When Alice publishes an event with kind 30030 and tags:
            | d | custom-emojis |
            | name | My Custom Emojis |
            | emoji | happy | https://example.com/happy.png |
            | emoji | sad | https://example.com/sad.png |
        Then the relay accepts the event
        When Alice subscribes to events with kind 30030
        Then Alice receives 1 event
        And the event has 2 "emoji" tags

    Scenario: Create and retrieve a relay set
        When Alice publishes an event with kind 30002 and tags:
            | d | my-relays |
            | name | Primary Relays |
            | about | My main relay connections |
            | relay | wss://relay1.example.com |
            | relay | wss://relay2.example.com |
        Then the relay accepts the event
        When Alice subscribes to events with kind 30002
        Then Alice receives 1 event
        And the event has 2 "relay" tags

    Scenario: Create and retrieve a kind mute set
        When Alice publishes an event with kind 30007 and tags:
            | d | 1 |
            | p | 07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9 |
            | p | a55c15f5e41d5aebd236eca5e0142789c5385703f1a7485aa4b38d94fd18dcc4 |
        Then the relay accepts the event
        When Alice subscribes to events with kind 30007
        Then Alice receives 1 event
        And the event has tag "d" with value "1"
        And the event has 2 "p" tags
