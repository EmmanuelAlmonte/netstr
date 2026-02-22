# NIP-51 Client Implementation Guide

## Overview

This guide provides comprehensive implementation details for NIP-51 (Nostr Lists) based on the Netstr relay implementation. It covers event structures, query patterns, validation rules, and client implementation examples.

## Architecture Overview

### List Types

**Standard Lists (10000-10999):**

- Single instance per user (replaceable events)
- Unique by `pubkey + kind`
- Examples: Mute lists, bookmarks, relay lists

**Sets (30000-30999):**

- Multiple instances per user with unique 'd' tags (addressable events)
- Unique by `pubkey + kind + d_tag_value`
- Examples: Follow sets, bookmark sets, curation sets

### Event Processing Rules

- **Standard Lists:** Newer events completely replace older ones (same pubkey+kind)
- **Sets:** Newer events replace older ones with same pubkey+kind+d_tag
- **Deletion:** Events marked as deleted prevent older replacements
- **Timestamps:** Replacement only occurs if new event has later `created_at`

## Supported Event Kinds

### Standard Lists (10000-10999)

- `10000` - Mute List
- `10001` - Pinned Notes
- `10002` - Relay List
- `10003` - Bookmarks
- `10004` - Communities
- `10005` - Public Chats
- `10006` - Blocked Relays
- `10007` - Search Relays
- `10009` - Simple Groups
- `10015` - Interests
- `10030` - Emojis
- `10050` - DM Relays
- `10101` - Good Wiki Authors
- `10102` - Good Wiki Relays

### Sets (30000-30999)

- `30000` - Follow Sets
- `30002` - Relay Sets
- `30003` - Bookmark Sets
- `30004` - Article Curation Sets
- `30005` - Video Curation Sets
- `30007` - Kind Mute Sets
- `30015` - Interest Sets
- `30030` - Emoji Sets
- `30063` - Release Artifact Sets
- `30267` - App Curation Sets

## Event Structure Examples

### Standard Mute List (Kind 10000)

```json
{
  "id": "a92a316b75e44cfdc19986c634049158d4206fcc0b7b9c7ccbcdabe28beebcd0",
  "pubkey": "854043ae8f1f97430ca8c1f1a090bdde6488bd5115c7a45307a2a212750ae4cb",
  "created_at": 1699597889,
  "kind": 10000,
  "tags": [
    ["p", "07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9"],
    ["p", "a55c15f5e41d5aebd236eca5e0142789c5385703f1a7485aa4b38d94fd18dcc4"]
  ],
  "content": "encrypted_private_items_base64",
  "sig": "1173822c53261f8cffe7efbf43ba4a97a9198b3e402c2a1df130f42a8985a2d0d3430f4de350db184141e45ca844ab4e5364ea80f11d720e36357e1853dba6ca"
}
```

### Bookmark Set (Kind 30003)

```json
{
  "id": "567b41fc9060c758c4216fe5f8d3df7c57daad7ae757fa4606f0c39d4dd220ef",
  "pubkey": "d6dc95542e18b8b7aec2f14610f55c335abebec76f3db9e58c254661d0593a0c",
  "created_at": 1695327657,
  "kind": 30003,
  "tags": [
    ["d", "programming-resources"],
    ["name", "Programming Resources"],
    ["about", "Collection of programming articles and tutorials"],
    ["e", "d78ba0d5dce22bfff9db0a9e996c9ef27e2c91051de0c4e1da340e0326b4941e"],
    [
      "a",
      "30023:26dc95542e18b8b7aec2f14610f55c335abebec76f3db9e58c254661d0593a0c:95ODQzw3"
    ],
    ["t", "programming"],
    ["r", "https://example.com/resource"]
  ],
  "content": "",
  "sig": "a9a4e2192eede77e6c9d24ddfab95ba3ff7c03fbd07ad011fff245abea431fb4d3787c2d04aad001cb039cb8de91d83ce30e9a94f82ac3c5a2372aa1294a96bd"
}
```

### Follow Set (Kind 30000)

```json
{
  "kind": 30000,
  "tags": [
    ["d", "bitcoin-developers"],
    ["name", "Bitcoin Developers"],
    ["about", "Core Bitcoin protocol developers"],
    ["p", "dev1_pubkey"],
    ["p", "dev2_pubkey"],
    ["p", "dev3_pubkey"]
  ],
  "content": "",
  "pubkey": "your_pubkey",
  "created_at": 1699597889,
  "id": "event_id",
  "sig": "signature"
}
```

## Query Patterns and Subscription Filters

### Basic List Retrieval

**Get User's Mute List:**

```json
[
  "REQ",
  "mute_list",
  {
    "authors": ["user_pubkey"],
    "kinds": [10000],
    "limit": 1
  }
]
```

**Get All User's Bookmark Sets:**

```json
[
  "REQ",
  "bookmark_sets",
  {
    "authors": ["user_pubkey"],
    "kinds": [30003]
  }
]
```

**Get All User's Lists:**

```json
[
  "REQ",
  "all_lists",
  {
    "authors": ["user_pubkey"],
    "kinds": [10000, 10001, 10003, 30000, 30002, 30003]
  }
]
```

### Specific Set Queries

**Get Specific Bookmark Set by ID:**

```json
[
  "REQ",
  "specific_bookmarks",
  {
    "authors": ["user_pubkey"],
    "kinds": [30003],
    "#d": ["programming-resources"]
  }
]
```

**Get Relay Sets for UI Picker:**

```json
[
  "REQ",
  "relay_picker",
  {
    "authors": ["user_pubkey"],
    "kinds": [30002]
  }
]
```

**Get Multiple Specific Sets:**

```json
[
  "REQ",
  "multiple_sets",
  {
    "authors": ["user_pubkey"],
    "kinds": [30003],
    "#d": ["bookmarks-1", "bookmarks-2", "programming"]
  }
]
```

### Content-Based Queries

**Find Lists Containing Specific User:**

```json
[
  "REQ",
  "lists_with_user",
  {
    "kinds": [10000, 30000, 30007],
    "#p": ["target_user_pubkey"]
  }
]
```

**Find Bookmark Sets Containing Specific Event:**

```json
[
  "REQ",
  "bookmarks_with_event",
  {
    "kinds": [30003],
    "#e": ["event_id"]
  }
]
```

**Find Sets Containing Addressable Events:**

```json
[
  "REQ",
  "sets_with_article",
  {
    "kinds": [30003, 30004],
    "#a": ["30023:author:article_id"]
  }
]
```

**Find Interest Sets by Topic:**

```json
[
  "REQ",
  "bitcoin_interests",
  {
    "kinds": [30015],
    "#t": ["bitcoin"]
  }
]
```

**Find Relay Sets with Specific Relay:**

```json
[
  "REQ",
  "sets_with_relay",
  {
    "kinds": [30002],
    "#relay": ["wss://relay.damus.io"]
  }
]
```

### Multi-User and Discovery Queries

**Get All Public Mute Lists (Moderation):**

```json
[
  "REQ",
  "public_mutes",
  {
    "kinds": [10000],
    "limit": 100
  }
]
```

**Get Community/Interest Lists:**

```json
[
  "REQ",
  "community_lists",
  {
    "kinds": [10004, 10015, 30015],
    "limit": 50
  }
]
```

**Recent List Updates:**

```json
[
  "REQ",
  "recent_lists",
  {
    "authors": ["user_pubkey"],
    "kinds": [10000, 10001, 10003, 30000, 30002, 30003],
    "since": 1699500000
  }
]
```

### Complex Multi-Filter Queries

**Multiple OR Conditions:**

```json
[
  "REQ",
  "various_lists",
  { "authors": ["user1"], "kinds": [10000] },
  { "authors": ["user2"], "kinds": [30003] },
  { "kinds": [30002], "#d": ["primary-relays"] }
]
```

## Subscription Filter Structure

```json
{
  "ids": ["event_id_1", "event_id_2"], // Optional: specific event IDs
  "authors": ["pubkey_1", "pubkey_2"], // Optional: author pubkeys
  "kinds": [10000, 30003], // Optional: event kinds
  "since": 1699500000, // Optional: timestamp filter
  "until": 1699600000, // Optional: timestamp filter
  "limit": 100, // Optional: result limit
  "search": "bitcoin", // Optional: content search
  "#d": ["set-id-1", "set-id-2"], // Optional: d tag values
  "#p": ["pubkey"], // Optional: p tag values
  "#e": ["event_id"], // Optional: e tag values
  "#a": ["30023:author:article"], // Optional: a tag values
  "#t": ["hashtag"], // Optional: t tag values
  "#relay": ["wss://relay.example.com"] // Optional: relay tag values
}
```

## Validation Rules

### Standard List Validations

- **Mute List (10000):** p, t, word, e tags allowed
- **Pinned Notes (10001):** e tags only
- **Bookmarks (10003):** e, a, t, r tags allowed
- **Communities (10004):** a tags only
- **Public Chats (10005):** e tags only
- **Relay Lists (10006, 10007, 10050, 10102):** relay tags only
- **Simple Groups (10009):** group, r tags allowed
- **Interests (10015):** t, a tags allowed
- **Emojis (10030):** emoji, a tags allowed
- **Wiki Authors (10101):** p tags only

### Set Validations

All sets require a 'd' tag for identification:

- **Follow Sets (30000):** d, p tags allowed
- **Relay Sets (30002):** d, relay tags allowed
- **Bookmark Sets (30003):** d, e, a, t, r tags allowed
- **Curation Sets (30004, 30005):** d, a, e tags allowed
- **Kind Mute Sets (30007):** d, p tags allowed
- **Interest Sets (30015):** d, t tags allowed
- **Emoji Sets (30030):** d, emoji tags allowed
- **Release Artifact Sets (30063):** d, e, a tags allowed
- **App Curation Sets (30267):** d, a tags allowed

## Private List Items

Private items are encrypted using NIP-04 encryption and stored in the `content` field:

```javascript
// Encryption pseudocode
const private_items = [
  ["p", "private_pubkey_1"],
  ["a", "private_addressable_event"],
];
const encrypted_content = nip04.encrypt(
  JSON.stringify(private_items),
  user_private_key,
  user_public_key
);
event.content = encrypted_content;
```

## Client Implementation Examples

### JavaScript WebSocket Helper Class

```javascript
class NostrListClient {
  constructor(websocket, userPubkey) {
    this.ws = websocket;
    this.userPubkey = userPubkey;
    this.subscriptions = new Map();
  }

  // Subscribe to user's lists
  subscribeToUserLists(userPubkey, kinds = [10000, 10001, 10003]) {
    const subId = `user_lists_${Date.now()}`;
    const filter = {
      authors: [userPubkey],
      kinds: kinds,
    };

    this.ws.send(JSON.stringify(["REQ", subId, filter]));
    return subId;
  }

  // Subscribe to specific set
  subscribeToSet(userPubkey, kind, setId) {
    const subId = `set_${kind}_${setId}_${Date.now()}`;
    const filter = {
      authors: [userPubkey],
      kinds: [kind],
      "#d": [setId],
    };

    this.ws.send(JSON.stringify(["REQ", subId, filter]));
    return subId;
  }

  // Find sets containing specific content
  findSetsContaining(tagType, tagValue, kinds = [30000, 30002, 30003]) {
    const subId = `find_sets_${Date.now()}`;
    const filter = {
      kinds: kinds,
      [`#${tagType}`]: [tagValue],
    };

    this.ws.send(JSON.stringify(["REQ", subId, filter]));
    return subId;
  }

  // Get all lists of a specific type
  getListsByKind(kind, limit = 50) {
    const subId = `lists_${kind}_${Date.now()}`;
    const filter = {
      kinds: [kind],
      limit: limit,
    };

    this.ws.send(JSON.stringify(["REQ", subId, filter]));
    return subId;
  }

  // Publish a new standard list
  publishList(kind, items, content = "") {
    const event = {
      kind: kind,
      tags: items,
      content: content,
      created_at: Math.floor(Date.now() / 1000),
      pubkey: this.userPubkey,
    };

    const signedEvent = this.signEvent(event);
    this.ws.send(JSON.stringify(["EVENT", signedEvent]));
    return signedEvent;
  }

  // Publish a new set
  publishSet(kind, setId, name, items, description = "") {
    const tags = [
      ["d", setId],
      ["name", name],
    ];

    if (description) {
      tags.push(["about", description]);
    }

    tags.push(...items);

    const event = {
      kind: kind,
      tags: tags,
      content: "",
      created_at: Math.floor(Date.now() / 1000),
      pubkey: this.userPubkey,
    };

    const signedEvent = this.signEvent(event);
    this.ws.send(JSON.stringify(["EVENT", signedEvent]));
    return signedEvent;
  }

  // Close subscription
  closeSubscription(subId) {
    this.ws.send(JSON.stringify(["CLOSE", subId]));
    this.subscriptions.delete(subId);
  }

  // Helper method for signing events (implement with your preferred signing library)
  signEvent(event) {
    // Implement event signing logic here
    // This would typically use a library like nostr-tools
    throw new Error("Implement event signing");
  }
}
```

### Usage Examples

```javascript
const client = new NostrListClient(websocket, userPubkey);

// Get user's mute list
const muteSubId = client.subscribeToUserLists(userPubkey, [10000]);

// Get all bookmark sets
const bookmarkSetsSubId = client.subscribeToUserLists(userPubkey, [30003]);

// Find sets containing a specific user
const setsWithUserSubId = client.findSetsContaining(
  "p",
  "target_pubkey",
  [30000, 30007]
);

// Create a new follow set
client.publishSet(
  30000,
  "bitcoin-devs",
  "Bitcoin Developers",
  [
    ["p", "dev1_pubkey"],
    ["p", "dev2_pubkey"],
    ["p", "dev3_pubkey"],
  ],
  "Core Bitcoin protocol developers"
);

// Create a mute list
client.publishList(10000, [
  ["p", "spammer_pubkey"],
  ["t", "spam"],
  ["word", "badword"],
]);
```

## Common Use Cases

### 1. User Profile Enhancement

- Display pinned notes on profile
- Show user's interests and communities
- List preferred relays for communication

### 2. Content Curation

- Create and share article collections
- Organize bookmarks by topic
- Curate video playlists

### 3. Social Graph Management

- Organize follows into categories
- Manage mute lists for content filtering
- Create topic-specific follow lists

### 4. Relay Management

- Set up relay groups for different purposes
- Share relay recommendations
- Manage blocked relays

### 5. Community Building

- Share community lists
- Create interest-based groups
- Organize member lists

## Best Practices

### 1. Event Publishing

- Always include proper timestamps
- Use descriptive names for sets
- Include helpful descriptions in 'about' tags
- Validate tag formats before publishing

### 2. Query Efficiency

- Use specific filters to reduce bandwidth
- Implement proper pagination with 'limit'
- Close subscriptions when no longer needed
- Use time-based filters for recent updates

### 3. User Experience

- Cache frequently accessed lists locally
- Implement real-time updates for list changes
- Provide UI for easy list management
- Show loading states during queries

### 4. Privacy Considerations

- Encrypt sensitive list items in content field
- Consider public vs private list implications
- Respect user privacy preferences
- Implement proper key management

## Error Handling

### Common Error Scenarios

- Invalid event signatures
- Missing required tags (especially 'd' tags for sets)
- Invalid tag formats
- Timestamp validation failures
- Rate limiting by relays

### Implementation Considerations

- Implement retry logic for failed publishes
- Validate events before sending
- Handle relay disconnections gracefully
- Provide user feedback for errors

This guide provides comprehensive implementation details for NIP-51 based on the Netstr relay codebase. Use these patterns and examples to build robust list functionality in your Nostr clients.
