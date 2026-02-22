Feature: NIP-65
	Relay List Metadata events (kind 10002) advertise the relays users prefer for reading and writing.
	These are replaceable events.

Background:
	Given a relay is running
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |
	And Bob is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 | 3551fc7617f76632e4542992c0bc01fecb224de639c4b6a1e0956946e8bb8a29 |

Scenario: Publish valid relay list with read/write markers
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                                                                            | CreatedAt  |
	| 1111111111111111111111111111111111111111111111111111111111111111 | *       | 10002 | [["r","wss://relay1.example.com","read"],["r","wss://relay2.example.com","write"],["r","wss://relay3.example.com"]]             | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 1111111111111111111111111111111111111111111111111111111111111111 | true    |

Scenario: Query relay list by author
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                               | CreatedAt  |
	| 2222222222222222222222222222222222222222222222222222222222222222 | *       | 10002 | [["r","wss://relay1.example.com","read"],["r","wss://relay2.example.com","write"]] | 1722337838 |
	And Bob sends a subscription request relays
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 10002 |
	Then Bob receives messages
	| Type  | Id     | EventId                                                          |
	| EVENT | relays | 2222222222222222222222222222222222222222222222222222222222222222 |
	| EOSE  | relays |                                                                  |

Scenario: Update existing relay list replaces previous
	When Alice publishes events
	| Id                                                               | Content | Kind  | Tags                                                  | CreatedAt  |
	| 3333333333333333333333333333333333333333333333333333333333333333 | *       | 10002 | [["r","wss://relay1.example.com"]]                   | 1722337838 |
	| 4444444444444444444444444444444444444444444444444444444444444444 | *       | 10002 | [["r","wss://relay2.example.com"]]                   | 1722337848 |
	And Bob sends a subscription request relays
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 10002 |
	Then Bob receives messages
	| Type  | Id     | EventId                                                          |
	| EVENT | relays | 4444444444444444444444444444444444444444444444444444444444444444 |
	| EOSE  | relays |                                                                  |

Scenario: Reject relay list with no r tags
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags | CreatedAt  |
	| 5555555555555555555555555555555555555555555555555555555555555555 | *       | 10002 |      | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message |
	| OK   | 5555555555555555555555555555555555555555555555555555555555555555 | false   | *       |

Scenario: Reject relay list with invalid URL
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                         | CreatedAt  |
	| 6666666666666666666666666666666666666666666666666666666666666666 | *       | 10002 | [["r","not-a-valid-url"]]   | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message |
	| OK   | 6666666666666666666666666666666666666666666666666666666666666666 | false   | *       |

Scenario: Reject relay list with invalid marker
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                     | CreatedAt  |
	| 7777777777777777777777777777777777777777777777777777777777777777 | *       | 10002 | [["r","wss://relay1.example.com","invalid_marker"]]     | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message |
	| OK   | 7777777777777777777777777777777777777777777777777777777777777777 | false   | *       |

Scenario: Valid relay list with no markers means both read and write
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                 | CreatedAt  |
	| 8888888888888888888888888888888888888888888888888888888888888888 | *       | 10002 | [["r","wss://relay1.example.com"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 8888888888888888888888888888888888888888888888888888888888888888 | true    |
