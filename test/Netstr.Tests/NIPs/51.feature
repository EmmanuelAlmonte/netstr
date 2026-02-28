Feature: NIP-51
	Standard lists (kinds 10000-10999) are replaceable per author.
	Sets (kinds 30000-30999) are addressable and require a "d" tag identifier.

Background:
	Given a relay is running
	And Alice is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02 |
	And Bob is connected to relay
	| PublicKey                                                        | PrivateKey                                                       |
	| 5bc683a5d12133a96ac5502c15fe1c2287986cff7baf6283600360e6bb01f627 | 3551fc7617f76632e4542992c0bc01fecb224de639c4b6a1e0956946e8bb8a29 |

# Mute List (10000)
Scenario: Create public mute list with p tags
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                                                                                                                         | CreatedAt  |
	| 1111111111111111111111111111111111111111111111111111111111111111 | *       | 10000 | [["p","07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9"],["p","a55c15f5e41d5aebd236eca5e0142789c5385703f1a7485aa4b38d94fd18dcc4"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 1111111111111111111111111111111111111111111111111111111111111111 | true    |

Scenario: Create mute list with hashtag and word tags
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                   | CreatedAt  |
	| 2222222222222222222222222222222222222222222222222222222222222222 | *       | 10000 | [["t","spam"],["word","scam"],["word","rugpull"]]     | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 2222222222222222222222222222222222222222222222222222222222222222 | true    |

Scenario: Query mute list by author
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                           | CreatedAt  |
	| 3333333333333333333333333333333333333333333333333333333333333333 | *       | 10000 | [["p","07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9"]]     | 1722337838 |
	And Bob sends a subscription request mute_sub
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 10000 |
	Then Bob receives messages
	| Type  | Id       | EventId                                                          |
	| EVENT | mute_sub | 3333333333333333333333333333333333333333333333333333333333333333 |
	| EOSE  | mute_sub |                                                                  |

# Bookmarks (10003)
Scenario: Create bookmarks with event and article references
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                                                                                                                                         | CreatedAt  |
	| 4444444444444444444444444444444444444444444444444444444444444444 | *       | 10003 | [["e","d78ba0d5dce22bfff9db0a9e996c9ef27e2c91051de0c4e1da340e0326b4941e"],["a","30023:26dc95542e18b8b7aec2f14610f55c335abebec76f3db9e58c254661d0593a0c:95ODQzw3"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 4444444444444444444444444444444444444444444444444444444444444444 | true    |

# Blocked Relays (10006)
Scenario: Create blocked relays list
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                             | CreatedAt  |
	| 5555555555555555555555555555555555555555555555555555555555555555 | *       | 10006 | [["relay","wss://badrelay1.com"],["relay","wss://badrelay2.com"]]               | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 5555555555555555555555555555555555555555555555555555555555555555 | true    |

# Interests (10015)
Scenario: Create interests list
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                             | CreatedAt  |
	| 6666666666666666666666666666666666666666666666666666666666666666 | *       | 10015 | [["t","bitcoin"],["t","nostr"],["t","programming"]]                             | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 6666666666666666666666666666666666666666666666666666666666666666 | true    |

# Emoji list (10030)
Scenario: Create emoji list with emoji tags
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                                                                             | CreatedAt  |
	| 7777777777777777777777777777777777777777777777777777777777777777 | *       | 10030 | [["emoji","happy","https://example.com/happy.png"],["emoji","sad","https://example.com/sad.png"]]                               | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 7777777777777777777777777777777777777777777777777777777777777777 | true    |

# Follow Sets (30000) - Addressable, requires d tag
Scenario: Create follow set with d tag
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                                                                                                                                                               | CreatedAt  |
	| 8888888888888888888888888888888888888888888888888888888888888888 | *       | 30000 | [["d","friends"],["p","07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9"],["p","a55c15f5e41d5aebd236eca5e0142789c5385703f1a7485aa4b38d94fd18dcc4"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | 8888888888888888888888888888888888888888888888888888888888888888 | true    |

Scenario: Reject follow set without d tag
	Sets require a d tag identifier.
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                           | CreatedAt  |
	| 9999999999999999999999999999999999999999999999999999999999999999 | *       | 30000 | [["p","07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9"]]     | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success | Message |
	| OK   | 9999999999999999999999999999999999999999999999999999999999999999 | false   | *       |

# Relay Sets (30002)
Scenario: Create relay set with d tag
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                                                              | CreatedAt  |
	| aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa | *       | 30002 | [["d","my-relays"],["relay","wss://relay1.example.com"],["relay","wss://relay2.example.com"]]                    | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa | true    |

# Bookmark Sets (30003)
Scenario: Create bookmark set with d tag
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                                                                                                                       | CreatedAt  |
	| bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb | *       | 30003 | [["d","programming"],["e","d78ba0d5dce22bfff9db0a9e996c9ef27e2c91051de0c4e1da340e0326b4941e"],["a","30023:26dc95542e18b8b7aec2f14610f55c335abebec76f3db9e58c254661d0593a0c:95ODQzw3"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb | true    |

# Kind Mute Sets (30007)
Scenario: Create kind mute set with d tag as kind number
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                                                                                                                         | CreatedAt  |
	| cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc | *       | 30007 | [["d","1"],["p","07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9"],["p","a55c15f5e41d5aebd236eca5e0142789c5385703f1a7485aa4b38d94fd18dcc4"]] | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc | true    |

# Interest Sets (30015)
Scenario: Create interest set with d tag
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                             | CreatedAt  |
	| dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd | *       | 30015 | [["d","tech"],["t","bitcoin"],["t","programming"]]                              | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd | true    |

# Emoji Sets (30030)
Scenario: Create emoji set with d tag
	When Alice publishes an event
	| Id                                                               | Content | Kind  | Tags                                                                                                                                         | CreatedAt  |
	| eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee | *       | 30030 | [["d","reactions"],["emoji","thumbsup","https://example.com/thumbsup.png"],["emoji","fire","https://example.com/fire.png"]]                 | 1722337838 |
	Then Alice receives a message
	| Type | Id                                                               | Success |
	| OK   | eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee | true    |

# Addressable events are replaced by d tag
Scenario: Update addressable list replaces previous with same d tag
	When Alice publishes events
	| Id                                                               | Content | Kind  | Tags                                                                                             | CreatedAt  |
	| *                                                                | *       | 30000 | [["d","friends"],["p","07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9"]]       | 1722337838 |
	| *                                                                | *       | 30000 | [["d","friends"],["p","a55c15f5e41d5aebd236eca5e0142789c5385703f1a7485aa4b38d94fd18dcc4"]]       | 1722337848 |
	And Bob sends a subscription request set_sub
	| Authors                                                          | Kinds |
	| 5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75 | 30000 |
	Then Bob receives messages
	| Type  | Id      | EventId                                                          |
	| EVENT | set_sub | *                                                               |
	| EOSE  | set_sub |                                                                  |
