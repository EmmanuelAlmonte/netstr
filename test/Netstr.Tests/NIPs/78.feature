Feature: NIP-78
	Application-specific data sets via addressable event kind 30078.

Background:
	Given a relay is running
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |

Scenario: Reject NIP-78 app data without d tag
	When Alice publishes events
	| Id                                                               | Content   | Kind  | Tags                                     | CreatedAt  |
	| 1111111111111111111111111111111111111111111111111111111111111111 | app data  | 30078 | [["foo","bar"]]                           | 1722340800 |
	Then Alice receives a message
	| Type   | Id                                                               | Success | Message                                       |
	| OK     | 1111111111111111111111111111111111111111111111111111111111111111 | false   | invalid: set event missing 'd' tag identifier |

Scenario: Accept NIP-78 app data with d tag
	When Alice publishes events
	| Id                                                               | Content   | Kind  | Tags                                             | CreatedAt  |
	| 2222222222222222222222222222222222222222222222222222222222222222 | app data  | 30078 | [["d","my-app"],["foo","bar"]]                    | 1722340801 |
	Then Alice receives a message
	| Type   | Id                                                               | Success |
	| OK     | 2222222222222222222222222222222222222222222222222222222222222222 | true    |
