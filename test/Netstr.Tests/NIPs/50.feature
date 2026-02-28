Feature: NIP-50
	Search capability.

Background:
	Given a relay is running
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |
	And Bob is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 | 3551fc7617f76632e4542992c0bc01fecb224de639c4b6a1e0956946e8bb8a29 |

Scenario: Search filter matches matching text content
	When Alice publishes events
	| Id                                                               | Content                                        | Kind | Tags | CreatedAt  |
	| 1111111111111111111111111111111111111111111111111111111111111111 | hello relay search query                         | 1    |      | 1722339900 |
	| 2222222222222222222222222222222222222222222222222222222222222222 | this event should not match query                | 1    |      | 1722339901 |
	And Bob sends a subscription request search_basic
	| Authors                                                          | Kinds | Search      | Since      | Until      |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 1     | relay       | 1722339890 | 1722339990 |
	Then Bob receives a message
	| Type  | Id          | EventId |
	| EVENT | search_basic |         |
	| EOSE  | search_basic |         |

Scenario: Unsupported search extensions are ignored without reducing recall
	When Alice publishes events
	| Id                                                               | Content                                     | Kind | Tags | CreatedAt  |
	| 3333333333333333333333333333333333333333333333333333333333333333 | search extension test one                     | 1    |      | 1722340000 |
	| 4444444444444444444444444444444444444444444444444444444444444444 | search extension test two                     | 1    |      | 1722340001 |
	And Bob sends a subscription request search_extensions
	| Authors                                                          | Kinds | Search              | Since      | Until      |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 1     | unsupported:token    | 1722339990 | 1722340100 |
	Then Bob receives a message
	| Type  | Id               | EventId |
	| EVENT | search_extensions |         |
	| EVENT | search_extensions |         |
	| EOSE  | search_extensions |         |
