Feature: NIP-57
	Lightning Zaps enable Bitcoin payments on nostr.
	Zap Request (kind 9734) is sent to initiate a zap.
	Zap Receipt (kind 9735) is published after payment confirmation.

Background:
	Given a relay is running
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |
	And Bob is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 | 3551fc7617f76632e4542992c0bc01fecb224de639c4b6a1e0956946e8bb8a29 |

# Zap Request (9734)
Scenario: Create valid zap request with required tags
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                                                                                       | CreatedAt  |
	| 1111111111111111111111111111111111111111111111111111111111111111 | *       | 9734 | [["p","04c915daefee38317fa734444acee390a8269fe5810b2241e5e6dd343dfbecc9"],["relays","wss://relay1.example.com","wss://relay2.example.com"]]                                               | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 1111111111111111111111111111111111111111111111111111111111111111 | true    |

Scenario: Create zap request with amount and lnurl
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                                                                                                                           | CreatedAt  |
	| 2222222222222222222222222222222222222222222222222222222222222222 | *       | 9734 | [["p","04c915daefee38317fa734444acee390a8269fe5810b2241e5e6dd343dfbecc9"],["relays","wss://relay1.example.com"],["amount","21000"],["lnurl","lnurl1dp68gurn8ghj7um5v93kketj9ehx2amn9uh8wetvdskkkmn0wahz7mrww4excup0dajx2mrv92x9xp"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 2222222222222222222222222222222222222222222222222222222222222222 | true    |

Scenario: Create zap request with e tag for specific event
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                                                                                                                                                  | CreatedAt  |
	| 3333333333333333333333333333333333333333333333333333333333333333 | *       | 9734 | [["p","04c915daefee38317fa734444acee390a8269fe5810b2241e5e6dd343dfbecc9"],["relays","wss://relay1.example.com"],["e","3624762a1274dd9636e0c552b53086d70bc88c165bc4dc0f9e836a1eaf86c3b8"]]                                                              | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 3333333333333333333333333333333333333333333333333333333333333333 | true    |

Scenario: Reject zap request without p tag
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                           | CreatedAt  |
	| 4444444444444444444444444444444444444444444444444444444444444444 | *       | 9734 | [["relays","wss://relay1.example.com"]]       | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message |
	| OK   | 4444444444444444444444444444444444444444444444444444444444444444 | false   | *       |

Scenario: Reject zap request without relays tag
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                       | CreatedAt  |
	| 5555555555555555555555555555555555555555555555555555555555555555 | *       | 9734 | [["p","04c915daefee38317fa734444acee390a8269fe5810b2241e5e6dd343dfbecc9"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message |
	| OK   | 5555555555555555555555555555555555555555555555555555555555555555 | false   | *       |

# Zap Receipt (9735)
Scenario: Create valid zap receipt with required tags
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 | CreatedAt  |
	| 6666666666666666666666666666666666666666666666666666666666666666 | *       | 9735 | [["p","32e1827635450ebb3c5a7d12c1f8e7b2b514439ac10a67eef3d9fd9c5c68e245"],["bolt11","lnbc10u1p3unwfusp5t9r3yymhpfqculx78u027lxspgxcr2n2987mx2j55nnfs95nxnzqpp5jmrh92pfld78spqs78v9euf2385t83uvpwk9ldrlvf6ch7tpascqhp5zvkrmemgth3tufcvflmzjzfvjt023nazlhljz2n9hattj4f8jq8qxqyjw5qcqpjrzjq"],["description","{\"pubkey\":\"test\",\"kind\":9734}"]]                                                                                                                                                                                                                                                                                                                                                                                                          | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 6666666666666666666666666666666666666666666666666666666666666666 | true    |

Scenario: Create zap receipt with preimage
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                                                                                                                                                                                                                 | CreatedAt  |
	| 7777777777777777777777777777777777777777777777777777777777777777 | *       | 9735 | [["p","32e1827635450ebb3c5a7d12c1f8e7b2b514439ac10a67eef3d9fd9c5c68e245"],["bolt11","lnbc10u1"],["description","{\"pubkey\":\"test\",\"kind\":9734}"],["preimage","5d006d2cf1e73c7148e7519a4c68adc81642ce0e25a432b2434c99f97344c15f"]]                                                                               | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 7777777777777777777777777777777777777777777777777777777777777777 | true    |

Scenario: Reject zap receipt without p tag
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                      | CreatedAt  |
	| 8888888888888888888888888888888888888888888888888888888888888888 | *       | 9735 | [["bolt11","lnbc10u1"],["description","{\"pubkey\":\"test\",\"kind\":9734}"]]                            | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message |
	| OK   | 8888888888888888888888888888888888888888888888888888888888888888 | false   | *       |

Scenario: Reject zap receipt without bolt11 tag
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                       | CreatedAt  |
	| 9999999999999999999999999999999999999999999999999999999999999999 | *       | 9735 | [["p","32e1827635450ebb3c5a7d12c1f8e7b2b514439ac10a67eef3d9fd9c5c68e245"],["description","{\"pubkey\":\"test\",\"kind\":9734}"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message |
	| OK   | 9999999999999999999999999999999999999999999999999999999999999999 | false   | *       |

Scenario: Reject zap receipt without description tag
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                         | CreatedAt  |
	| aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa | *       | 9735 | [["p","32e1827635450ebb3c5a7d12c1f8e7b2b514439ac10a67eef3d9fd9c5c68e245"],["bolt11","lnbc10u1"]]             | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message |
	| OK   | aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa | false   | *       |

# Query Zaps
Scenario: Query zap requests by kind
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                                                 | CreatedAt  |
	| bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb | *       | 9734 | [["p","04c915daefee38317fa734444acee390a8269fe5810b2241e5e6dd343dfbecc9"],["relays","wss://relay1.example.com"]]                                     | 1722337838 |
	And Bob sends a subscription request zap_sub
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 9734  |
	Then Bob receives messages
	| Type  | Id      | EventId                                                          |
	| EVENT | zap_sub | bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb |
	| EOSE  | zap_sub |                                                                  |

Scenario: Query zap receipts by kind
	When Alice publishes an event
	| Id                                                               | Content | Kind | Tags                                                                                                                                                                                                                | CreatedAt  |
	| cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc | *       | 9735 | [["p","32e1827635450ebb3c5a7d12c1f8e7b2b514439ac10a67eef3d9fd9c5c68e245"],["bolt11","lnbc10u1"],["description","{\"pubkey\":\"test\",\"kind\":9734}"]]                                                             | 1722337838 |
	And Bob sends a subscription request zap_sub
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 9735  |
	Then Bob receives messages
	| Type  | Id      | EventId                                                          |
	| EVENT | zap_sub | cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc |
	| EOSE  | zap_sub |                                                                  |
