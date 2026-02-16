Feature: NIP-04
	A special event with kind 4, meaning "encrypted direct message".

Background: 
	Given a relay is running with AUTH enabled
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |
	And Bob is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 | 3551fc7617f76632e4542992c0bc01fecb224de639c4b6a1e0956946e8bb8a29 |
	
Scenario: Not authenticated client tries to fetch kind 4 events
	Alice can't fetch kind 4 events when she isn't authenticated
	This should be true even when multiple filters are used
	When Alice sends a subscription request abcd
	| Authors                                                          | Kinds |
	|                                                                  | 4,1   |
	| 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 |       |
	Then Alice receives messages
	| Type   | Id   |
	| AUTH   | *    |
	| CLOSED | abcd |

Scenario: Authenticated client tries to fetch kind 4 events
	Once Alice authenticates she can fetch their kind 4 events, but no one else's
	When Alice publishes an AUTH event for the challenge sent by relay
	And Bob publishes events
	| Id | Content           | Kind | Tags                                                                       | CreatedAt  |
	| *  | Secret?iv=AAAA    | 4    | [["p","5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75"]] | 1722337838 |
	| *  | Charlie?iv=BBBB   | 4    | [["p","fe8d7a5726ea97ce6140f9fb06b1fe7d3259bcbf8de42c2a5d2ec9f8f0e2f614"]] | 1722337838 |
	When Alice sends a subscription request abcd
	| Kinds |
	| 4     |
	And Bob publishes events
	| Id | Content               | Kind | Tags                                                                       | CreatedAt  |
	| *  | Secret2?iv=CCCC       | 4    | [["p","5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75"]] | 1722337838 |
	| *  | Charlie2?iv=DDDD      | 4    | [["p","fe8d7a5726ea97ce6140f9fb06b1fe7d3259bcbf8de42c2a5d2ec9f8f0e2f614"]] | 1722337838 |
	Then Alice receives messages
	| Type  | Id   | EventId                                                          | Success |
	| AUTH  | *    |                                                                  |         |
	| OK    | *    |                                                                  | true    |
	| EVENT | abcd | *                                                               |         |
	| EOSE  | abcd |                                                                  |         |
	| EVENT | abcd | *                                                               |         |

Scenario: Authenticated client tries to fetch kind 4 events through other filters
	Even when using complex filters, authenticated client should still not receive someone else's kind 4 events
	When Alice publishes an AUTH event for the challenge sent by relay
	And Bob publishes events
	| Id | Content             | Kind | Tags                                                                       | CreatedAt  |
	| *  | Secret3?iv=EEEE    | 4    | [["p","5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75"]] | 1722337838 |
	| *  | Charlie3?iv=FFFF   | 4    | [["p","fe8d7a5726ea97ce6140f9fb06b1fe7d3259bcbf8de42c2a5d2ec9f8f0e2f614"]] | 1722337838 |
	When Alice sends a subscription request abcd
	| Ids                                                              | Authors                                                          | Kinds |
	|                                                                  |                                                                  | 4     |
	|                                                                  | fe8d7a5726ea97ce6140f9fb06b1fe7d3259bcbf8de42c2a5d2ec9f8f0e2f614 | 4     |
	|                                                                  | 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 | 4     |
	Then Alice receives messages
	| Type  | Id   | EventId                                                          | Success |
	| AUTH  | *    |                                                                  |         |
	| OK    | *    |                                                                  | true    |
	| EVENT | abcd | *                                                               |         |
	| EOSE  | abcd |                                                                  |         |
