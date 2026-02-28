Feature: NIP-59
	Gift wrapping.

Background:
	Given a relay is running
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |

Scenario: Reject kind 13 events with tags
	When Alice publishes events
	| Id                                                               | Content       | Kind | Tags                                           | CreatedAt  |
	| 1111111111111111111111111111111111111111111111111111111111111111 | sealed rumor  | 13   | [["p","5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75"]] | 1722340500 |
	Then Alice receives a message
	| Type   | Id                                                               | Success | Message                                                  |
	| OK     | 1111111111111111111111111111111111111111111111111111111111111111 | false   | invalid: kind 13 events must not contain tags               |

Scenario: Accept kind 13 events with empty tags
	When Alice publishes events
	| Id                                                               | Content       | Kind | Tags | CreatedAt  |
	| 2222222222222222222222222222222222222222222222222222222222222222 | sealed rumor  | 13   |      | 1722340501 |
	Then Alice receives a message
	| Type   | Id                                                               | Success |
	| OK     | 2222222222222222222222222222222222222222222222222222222222222222 | true    |
