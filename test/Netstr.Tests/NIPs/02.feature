Feature: NIP-02
	Follow list events (kind 3) contain public keys of users the author is following.
	Follow list is a replaceable event (only the latest version per author is kept).

Background:
	Given a relay is running
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |
	And Bob is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 | 3551fc7617f76632e4542992c0bc01fecb224de639c4b6a1e0956946e8bb8a29 |
	And Charlie is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| fe8d7a5726ea97ce6140f9fb06b1fe7d3259bcbf8de42c2a5d2ec9f8f0e2f614 | f77f81a6a223eb15f81fee569161a4f729401a9cbc31bb69fef6a949b9d3c23a |

Scenario: Publish valid follow list with multiple p tags
	Alice publishes a follow list with multiple public keys and can query it back.
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                                                                         | CreatedAt  |
	| 1c5d9f9b8c3e4d6a7f8e9d0c1b2a3948576a5d4c3b2e1f0a9d8c7b6e5a4f3d2c | *       | 3    | [["p","5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627"],["p","fe8d7a5726ea97ce6140f9fb06b1fe7d3259bcbf8de42c2a5d2ec9f8f0e2f614"]] | 1722337838 |
	And Bob sends a subscription request abcd
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 3     |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 1c5d9f9b8c3e4d6a7f8e9d0c1b2a3948576a5d4c3b2e1f0a9d8c7b6e5a4f3d2c | true    |
	And Bob receives messages
	| Type  | Id   | EventId                                                          |
	| EVENT | abcd | 1c5d9f9b8c3e4d6a7f8e9d0c1b2a3948576a5d4c3b2e1f0a9d8c7b6e5a4f3d2c |
	| EOSE  | abcd |                                                                  |

Scenario: Replace existing follow list with newer timestamp
	Follow list is a replaceable event, so only the latest version should be stored.
	When Alice publishes events
	| Id                                                               | Content | Kind | Tags                                                                           | CreatedAt  |
	| a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd | *       | 3    | [["p","5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627"]]     | 1722337838 |
	| b2c3d4e5f6789012345678901234567890123456789012345678901234abcdef | *       | 3    | [["p","fe8d7a5726ea97ce6140f9fb06b1fe7d3259bcbf8de42c2a5d2ec9f8f0e2f614"]]     | 1722337848 |
	And Bob sends a subscription request abcd
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 3     |
	Then Bob receives messages
	| Type  | Id   | EventId                                                          |
	| EVENT | abcd | b2c3d4e5f6789012345678901234567890123456789012345678901234abcdef |
	| EOSE  | abcd |                                                                  |

Scenario: Follow list with relay hints and petnames
	Follow list p tags can include optional relay URL and petname.
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                                                                         | CreatedAt  |
	| c3d4e5f6789012345678901234567890123456789012345678901234abcdef01 | *       | 3    | [["p","5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627","wss://relay.example.com","bob"],["p","fe8d7a5726ea97ce6140f9fb06b1fe7d3259bcbf8de42c2a5d2ec9f8f0e2f614","wss://nostr.example.com","charlie"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | c3d4e5f6789012345678901234567890123456789012345678901234abcdef01 | true    |

Scenario: Empty follow list with no p tags is valid
	A follow list with no contacts is valid.
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags | CreatedAt  |
	| d4e5f6789012345678901234567890123456789012345678901234abcdef0123 | *       | 3    |      | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | d4e5f6789012345678901234567890123456789012345678901234abcdef0123 | true    |

Scenario: Follow list with content is valid for backwards compatibility
	NIP-02 says content is not used but some clients store relay info there.
	When Alice publishes an event
	| Id                                                               | Content                                    | Kind | Tags                                                                           | CreatedAt  |
	| e5f6789012345678901234567890123456789012345678901234abcdef012345 | {"wss://relay.example.com":{"write":true}} | 3    | [["p","5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627"]]     | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | e5f6789012345678901234567890123456789012345678901234abcdef012345 | true    |

Scenario: Reject follow list with invalid pubkey format - wrong length
	Public keys must be 64-character hex strings.
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                             | CreatedAt  |
	| f6789012345678901234567890123456789012345678901234abcdef01234567 | *       | 3    | [["p","abc123"]]                 | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message                                            |
	| OK   | f6789012345678901234567890123456789012345678901234abcdef01234567 | false   | invalid: follow list contains invalid pubkey format |

Scenario: Reject follow list with invalid pubkey format - non-hex characters
	Public keys must only contain hexadecimal characters.
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                           | CreatedAt  |
	| 0789012345678901234567890123456789012345678901234abcdef0123456789 | *       | 3    | [["p","zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz"]]     | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message                                            |
	| OK   | 0789012345678901234567890123456789012345678901234abcdef0123456789 | false   | invalid: follow list contains invalid pubkey format |

Scenario: Reject follow list with invalid relay URL
	Relay URLs must be valid absolute URIs.
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                       | CreatedAt  |
	| 1890123456789012345678901234567890123456789012345abcdef012345678a | *       | 3    | [["p","5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627","not-a-valid-url"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message                                         |
	| OK   | 1890123456789012345678901234567890123456789012345abcdef012345678a | false   | invalid: follow list contains invalid relay URL |

Scenario: Reject follow list with non-p tags
	Follow list should only contain p tags.
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                                                        | CreatedAt  |
	| 2901234567890123456789012345678901234567890123456abcdef012345678b | *       | 3    | [["p","5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627"],["e","aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]]         | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message                                           |
	| OK   | 2901234567890123456789012345678901234567890123456abcdef012345678b | false   | invalid: follow list must only contain 'p' tags   |

Scenario: Query follow list by author pubkey
	Bob and Charlie both have follow lists, Alice can query them by author.
	When Bob publishes an event
	| Id                                                               | Content | Kind | Tags                                                                           | CreatedAt  |
	| 3012345678901234567890123456789012345678901234567abcdef012345678c | *       | 3    | [["p","5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75"]]     | 1722337838 |
	And Charlie publishes an event
	| Id                                                               | Content | Kind | Tags                                                                           | CreatedAt  |
	| 4123456789012345678901234567890123456789012345678abcdef012345678d | *       | 3    | [["p","5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627"]]     | 1722337838 |
	And Alice sends a subscription request follow_sub
	| Authors                                                          | Kinds |
	| 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 | 3     |
	Then Alice receives messages
	| Type  | Id         | EventId                                                          |
	| EVENT | follow_sub | 3012345678901234567890123456789012345678901234567abcdef012345678c |
	| EOSE  | follow_sub |                                                                  |
