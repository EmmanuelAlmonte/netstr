# NIP Implementation Guides

This document provides structural implementation guides for different categories of NIPs based on the patterns used in Netstr.

## Event Kind Ranges Overview

Understanding event kind ranges is crucial for proper NIP implementation:

- **0-999**: Protocol events (metadata, notes, DMs, reactions, follows)
- **1000-9999**: Special protocol events (mute, auth, zaps) 
- **10000-19999**: Replaceable events (lists, settings) - One per pubkey+kind
- **20000-29999**: Ephemeral events (presence, typing) - No storage
- **30000-39999**: Addressable replaceable events (profiles, sets) - One per pubkey+kind+d_tag

## Core Architectural Patterns

### Message Flow Pattern
All client-relay interactions follow the EVENT → OK/NOTICE pattern:
1. Client sends EVENT message
2. Relay validates and processes
3. Relay responds with OK (success) or NOTICE (error)
4. Relay broadcasts to matching subscriptions

### Event Categories
- **Regular Events**: Stored normally, can have duplicates
- **Replaceable Events**: Replace previous event of same kind by same author
- **Ephemeral Events**: Not stored, only broadcast in real-time
- **Addressable Events**: Replace previous event with same kind+pubkey+d_tag

## 1. Basic Protocol NIPs (1, 2, 11)

### NIP-01: Basic Protocol Flow
**Core Components Required:**

1. **Event Handler (`RegularEventHandler`)**
```csharp
public class RegularEventHandler : EventHandlerBase, IEventHandler
{
    public bool CanHandleEvent(Event e) => true; // Fallback handler

    public async Task HandleEventAsync(IWebSocketAdapter sender, Event e)
    {
        // 1. Validate event wasn't deleted
        // 2. Store in database with duplicate prevention
        // 3. Send OK response
        // 4. Broadcast to matching subscriptions
    }
}
```

2. **Message Handlers**
```csharp
// SubscribeMessageHandler - Handle REQ messages
// UnsubscribeMessageHandler - Handle CLOSE messages
// EventParser - Parse EVENT messages
```

3. **Database Schema**
```sql
-- EventEntity: Core event storage
-- TagEntity: Event tags for filtering
-- Proper indexing on pubkey, kind, created_at
```

4. **Key Implementation Steps:**
   - WebSocket message routing via `MessageDispatcher`
   - Event validation through validator chain
   - Database storage with EF Core
   - Real-time broadcasting via `SubscriptionsAdapter`

### NIP-02: Contact Lists / Following
**Uses existing replaceable event infrastructure (kind 3)**

### NIP-11: Relay Information Document
**Implementation in `RelayInformationService`:**
```csharp
// Serves JSON at /.well-known/nostr.json
// Returns relay capabilities, supported NIPs, contact info
```

## 2. List Management NIPs (51)

### NIP-51: Lists Implementation Pattern

**Event Handler Architecture:**
```csharp
public class ListEventHandler : ReplaceableEventHandlerBase
{
    // Standard Lists (10000-10999): One per user per kind
    // Sets (30000-30999): Multiple per user, identified by 'd' tag
}
```

**Key Components:**

1. **EventKind Definitions**
```csharp
// Standard Lists
MuteList = 10000,
PinnedNotes = 10001,
RelayList = 10002,
Bookmarks = 10003,
// ... additional list kinds

// Sets  
FollowSets = 30000,
RelaySets = 30002,
BookmarkSets = 30003,
// ... additional set kinds
```

2. **Storage Pattern**
```csharp
// Standard lists: Replace by pubkey + kind
// Sets: Replace by pubkey + kind + d_tag_value
// Private items: Encrypted in content field (NIP-04)
// Public items: Stored in tags array
```

3. **Implementation Steps:**
   - Extend `ReplaceableEventHandler` or `AddressableEventHandler`
   - Add specific list kinds to `EventKind` enum
   - Implement tag parsing for list items
   - Handle encryption/decryption for private items
   - Support both public and private list items

## 3. Relay Metadata NIP (65)

### NIP-65: Relay List Metadata

**Specialized Handler:**
```csharp
public class RelayListEventHandler : ReplaceableEventHandlerBase
{
    // Kind 10002 - Relay lists
    
    protected override async Task ProcessEventAsync(...)
    {
        // 1. Parse relay tags (read/write markers)
        // 2. Update RelayConfigs table
        // 3. Store event normally
        // 4. Update user's relay configuration
    }
}
```

**Database Schema:**
```csharp
public class RelayConfigEntity
{
    public string UserId { get; set; }
    public string RelayUrl { get; set; }
    public bool Read { get; set; }
    public bool Write { get; set; }
    // Additional relay metadata
}
```

**Implementation Pattern:**
1. **Tag Structure:** `["r", "relay_url", "read|write"]`
2. **Storage:** Dual storage in events table + relay configs table
3. **Processing:** Parse tags → Update relay configs → Store event
4. **Usage:** Query relay configs for user publishing/reading preferences

## 4. Authentication NIPs (42, 70)

### NIP-42: Authentication of Clients to Relays

**Key Components:**

1. **Auth Message Handler**
```csharp
public class AuthMessageHandler : IMessageHandler
{
    // Handle AUTH responses from clients
    // Verify signed events for authentication
    // Update ClientContext with authenticated pubkey
}
```

2. **Challenge System**
```csharp
public class ClientContext
{
    public string Challenge { get; } // Random challenge string
    public User? User { get; set; } // Set after successful auth
}
```

3. **Configuration**
```csharp
public class AuthOptions
{
    public AuthMode Mode { get; set; } // Always, Publishing, WhenNeeded, Disabled
    public long[] ProtectedKinds { get; set; } // Event kinds requiring auth
}
```

### NIP-70: Protected Events

**Implementation in Event Validation:**
```csharp
public class ProtectedEventValidator : IEventValidator
{
    public ValidationResult ValidateEvent(Event e, ClientContext context)
    {
        if (IsProtectedKind(e.Kind) && !context.IsAuthenticated)
            return ValidationResult.Fail("auth-required");
        
        return ValidationResult.Success();
    }
}
```

## 5. Event Modification NIPs (9, 40)

### NIP-09: Event Deletion

**Specialized Handler Pattern:**
```csharp
public class DeleteEventHandler : EventHandlerBase
{
    protected override async Task ProcessEventAsync(...)
    {
        // 1. Parse 'e' tags for event IDs to delete
        // 2. Parse 'a' tags for addressable event references
        // 3. Verify user owns events to be deleted
        // 4. Mark events as deleted (soft delete)
        // 5. Store deletion event
    }
}
```

**Key Features:**
- Soft deletion (mark as deleted, don't remove)
- Reference parsing: `e` tags for IDs, `a` tags for addressable events
- Ownership verification
- Transaction-based consistency

### NIP-40: Expiration Timestamp

**Implementation in Event Processing:**
```csharp
public class ExpiredEventValidator : IEventValidator
{
    public ValidationResult ValidateEvent(Event e, ClientContext context)
    {
        var expirationTag = e.Tags.FirstOrDefault(t => t.Name == "expiration");
        if (expirationTag != null && IsExpired(expirationTag.Value))
            return ValidationResult.Fail("event expired");
    }
}
```

**Cleanup Service:**
```csharp
public class CleanupBackgroundService : BackgroundService
{
    // Periodically remove expired events based on 'expiration' tags
    // Configurable cleanup intervals and retention policies
}
```

## 6. Messaging NIPs (4, 17, 59)

### NIP-04: Encrypted Direct Messages (Deprecated)
**Standard event handling with encrypted content**

### NIP-17: Private Direct Messages
**Uses replaceable events with specific kind ranges and validation**

### NIP-59: Gift Wrapping

**Event Kind Definition:**
```csharp
GiftWrap = 1059, // In EventKind enum
```

**Configuration:**
```csharp
// Add to ProtectedKinds - requires authentication
"ProtectedKinds": [1059]
```

**Processing:**
- Standard event handling with authentication requirement
- Content remains encrypted (relay doesn't decrypt)
- Proper routing based on recipient information

## 7. Special Feature NIPs (13, 45, 57, 77, 119)

### NIP-13: Proof of Work

**Validator Implementation:**
```csharp
public class EventPowValidator : IEventValidator
{
    public ValidationResult ValidateEvent(Event e, ClientContext context)
    {
        var nonceTag = e.Tags.FirstOrDefault(t => t.Name == "nonce");
        if (nonceTag != null)
        {
            var difficulty = CalculateDifficulty(e.Id);
            if (difficulty < requiredDifficulty)
                return ValidationResult.Fail("insufficient pow");
        }
    }
}
```

### NIP-45: Counting Results

**Message Handler:**
```csharp
public class CountMessageHandler : FilterMessageHandlerBase
{
    // Handle COUNT messages
    // Return count of matching events instead of events themselves
    // Use same filter logic as subscription system
}
```

### NIP-57: Lightning Zaps

**Specialized Handler:**
```csharp
public class ZapEventHandler : EventHandlerBase
{
    // Handle kinds 9734 (ZapRequest) and 9735 (ZapReceipt)
    // Enhanced duplicate detection
    // Standard storage and broadcasting
}
```

### NIP-77: Negentropy Sync

**Complex Multi-Component Implementation:**
```csharp
// NegentropyAdapter - Manages sync state
// NegentropyMessageHandler - Handles NEG-MSG, NEG-OPEN, NEG-CLOSE
// Background processing for efficient set reconciliation
```

### NIP-119: AND Operator for Filters
**Implementation in subscription filter matching logic**

## General Implementation Checklist

### For Any New NIP:

1. **Define Event Kinds** (if applicable)
   - Add to `EventKind` enum
   - Document expected tag structure

2. **Create Event Handler** (if new event types)
   - Inherit from appropriate base class
   - Implement `CanHandleEvent()` and `HandleEventAsync()`
   - Handle storage, validation, and broadcasting

3. **Add Validators** (if special validation needed)
   - Implement `IEventValidator`
   - Add to validation chain in DI

4. **Update Configuration**
   - Add to `SupportedNips` array
   - Add any NIP-specific options

5. **Create Tests**
   - Write SpecFlow scenarios in `.feature` files
   - Implement step definitions
   - Test both success and failure cases

6. **Database Changes** (if needed)
   - Create new entities/tables
   - Add migrations
   - Update indexes for performance

7. **Message Handlers** (if new message types)
   - Implement `IMessageHandler`
   - Add to DI container
   - Handle JSON parsing and response

## 8. Commonly Requested NIPs (Not Yet Implemented)

### NIP-50: Search Capability

**Implementation Requirements:**
```csharp
public class SearchMessageHandler : IMessageHandler
{
    public bool CanHandleMessage(string type) => type == "REQ";
    
    public async Task HandleMessageAsync(IWebSocketAdapter sender, JsonDocument[] parts)
    {
        // Parse REQ message for 'search' field
        // Implement full-text search against event content
        // Return matching events sorted by relevance
    }
}
```

**Key Features:**
- Add `search` field to subscription filters
- Implement full-text search against event content
- Support search extensions: `include:spam`, `domain:`, `language:`
- Sort results by relevance rather than chronological order

### NIP-96: HTTP File Storage

**Implementation Architecture:**
```csharp
[ApiController]
[Route("/.well-known/nostr/nip96.json")]
public class FileStorageController : ControllerBase
{
    // Server configuration endpoint
    
    [HttpPost("/upload")]
    public async Task<IActionResult> UploadFile([FromForm] FileUploadRequest request)
    {
        // Validate NIP-98 authorization header
        // Store file with SHA-256 hash as identifier
        // Return file URL and metadata
    }
    
    [HttpGet("/{hash}")]
    public async Task<IActionResult> DownloadFile(string hash)
    {
        // Serve file by hash
        // Support optional transformations
    }
}
```

**Dependencies:**
- **NIP-98**: HTTP Authorization for uploads
- File storage backend (local/cloud)
- Image processing for transformations

### NIP-05: DNS-based Identities

**Implementation Pattern:**
```csharp
public class Nip05Validator : IEventValidator
{
    public async Task<ValidationResult> ValidateEventAsync(Event e, ClientContext context)
    {
        // Check for NIP-05 identifier in metadata events (kind 0)
        // Validate against /.well-known/nostr.json
        // Cache verification results
    }
}
```

**Key Components:**
- HTTP client for DNS verification
- Caching layer for verification results
- Integration with user profiles (kind 0 events)

### NIP-78: Application-specific Data

**Storage Pattern:**
```csharp
// Use addressable events (kind 30078) with 'd' tag for app identifier
// Store app preferences and settings
// Support encrypted content for private settings
```

## 9. Advanced Implementation Patterns

### Multi-NIP Integration
Some features require combining multiple NIPs:

**Example: Private Groups with File Sharing**
- NIP-17: Private messaging
- NIP-59: Gift wrapping  
- NIP-96: File storage
- NIP-98: HTTP authorization

### Performance Optimizations
```csharp
// Database indexing strategy for large-scale deployments
// Event caching patterns
// Subscription optimization for high-throughput scenarios
```

### Backwards Compatibility
- Maintain support for deprecated NIPs during transition periods
- Implement feature flags for experimental NIPs
- Version negotiation for client compatibility

This guide provides the foundational patterns used in Netstr for implementing NIPs systematically and consistently.