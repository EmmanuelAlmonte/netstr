Feature: NIP-77
	Negentropy Syncing enables efficient set reconciliation between relay and client.
	Protocol messages: NEG-OPEN, NEG-MSG, NEG-CLOSE, NEG-ERR

Background:
	Given a relay is running
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |
	And Bob is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 | 3551fc7617f76632e4542992c0bc01fecb224de639c4b6a1e0956946e8bb8a29 |

# Basic Protocol Tests
Scenario: Seed events and query via standard subscription
	Negentropy syncs based on events in the database.
	First seed some events, then verify they can be queried.
	When Alice publishes events
	| Id                                                               | Content  | Kind | Tags | CreatedAt  |
	| 1111111111111111111111111111111111111111111111111111111111111111 | Event 1  | 1    |      | 1722337838 |
	| 2222222222222222222222222222222222222222222222222222222222222222 | Event 2  | 1    |      | 1722337848 |
	| 3333333333333333333333333333333333333333333333333333333333333333 | Event 3  | 1    |      | 1722337858 |
	And Bob sends a subscription request events_sub
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 1     |
	Then Bob receives messages
	| Type  | Id         | EventId                                                          |
	| EVENT | events_sub | 3333333333333333333333333333333333333333333333333333333333333333 |
	| EVENT | events_sub | 2222222222222222222222222222222222222222222222222222222222222222 |
	| EVENT | events_sub | 1111111111111111111111111111111111111111111111111111111111111111 |
	| EOSE  | events_sub |                                                                  |
