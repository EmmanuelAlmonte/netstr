Feature: NIP-05
	DNS-based identity verification for user metadata (kind 0) events.
	NIP-05 identifiers follow the format: local-part@domain
	Verification is done asynchronously and never rejects events.

Background:
	Given a relay is running
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |
	And Bob is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 | 3551fc7617f76632e4542992c0bc01fecb224de639c4b6a1e0956946e8bb8a29 |

Scenario: Accept metadata event with NIP-05 identifier
	NIP-05 validation runs asynchronously and never rejects events.
	When Alice publishes an event
	| Id                                                               | Content                                                                  | Kind | Tags | CreatedAt  |
	| 1111111111111111111111111111111111111111111111111111111111111111 | {"name":"alice","nip05":"alice@example.com"}                            | 0    |      | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 1111111111111111111111111111111111111111111111111111111111111111 | true    |

Scenario: Accept metadata event without NIP-05 identifier
	Events without NIP-05 field are valid.
	When Alice publishes an event
	| Id                                                               | Content                         | Kind | Tags | CreatedAt  |
	| 2222222222222222222222222222222222222222222222222222222222222222 | {"name":"alice","about":"test"} | 0    |      | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 2222222222222222222222222222222222222222222222222222222222222222 | true    |

Scenario: Accept metadata event with empty NIP-05 identifier
	Empty NIP-05 field should be accepted.
	When Alice publishes an event
	| Id                                                               | Content                               | Kind | Tags | CreatedAt  |
	| 3333333333333333333333333333333333333333333333333333333333333333 | {"name":"alice","nip05":""}          | 0    |      | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 3333333333333333333333333333333333333333333333333333333333333333 | true    |

Scenario: Accept metadata event with root identifier
	Root identifier uses underscore: _@domain.com
	When Alice publishes an event
	| Id                                                               | Content                                                                  | Kind | Tags | CreatedAt  |
	| 4444444444444444444444444444444444444444444444444444444444444444 | {"name":"example.com","nip05":"_@example.com"}                          | 0    |      | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 4444444444444444444444444444444444444444444444444444444444444444 | true    |

Scenario: Accept metadata event with invalid NIP-05 format
	Invalid NIP-05 format is still accepted, verification just fails silently.
	When Alice publishes an event
	| Id                                                               | Content                                                                  | Kind | Tags | CreatedAt  |
	| 5555555555555555555555555555555555555555555555555555555555555555 | {"name":"alice","nip05":"invalid-no-at-sign"}                           | 0    |      | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 5555555555555555555555555555555555555555555555555555555555555555 | true    |

Scenario: Query metadata by author
	When Alice publishes an event
	| Id                                                               | Content                                                                  | Kind | Tags | CreatedAt  |
	| 6666666666666666666666666666666666666666666666666666666666666666 | {"name":"alice","nip05":"alice@example.com","picture":"https://example.com/pic.jpg"} | 0    |      | 1722337838 |
	And Bob sends a subscription request metadata_sub
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 0     |
	Then Bob receives messages
	| Type  | Id           | EventId                                                          |
	| EVENT | metadata_sub | 6666666666666666666666666666666666666666666666666666666666666666 |
	| EOSE  | metadata_sub |                                                                  |

Scenario: Metadata event is replaceable
	Only the latest metadata event should be stored per author.
	When Alice publishes events
	| Id                                                               | Content                    | Kind | Tags | CreatedAt  |
	| 7777777777777777777777777777777777777777777777777777777777777777 | {"name":"alice_old"}      | 0    |      | 1722337838 |
	| 8888888888888888888888888888888888888888888888888888888888888888 | {"name":"alice_new"}      | 0    |      | 1722337848 |
	And Bob sends a subscription request metadata_sub
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 0     |
	Then Bob receives messages
	| Type  | Id           | EventId                                                          |
	| EVENT | metadata_sub | 8888888888888888888888888888888888888888888888888888888888888888 |
	| EOSE  | metadata_sub |                                                                  |
