# NIP-57: Lightning Zaps Implementation

This document describes the implementation of [NIP-57 Lightning Zaps](https://github.com/nostr-protocol/nips/blob/master/57.md) in the Netstr relay.

## Overview

NIP-57 defines two new event types for recording lightning payments between users:
- **Zap Request (Kind 9734)**: Represents a payer's request to a recipient's lightning wallet for an invoice
- **Zap Receipt (Kind 9735)**: Represents confirmation that an invoice has been paid

## Implementation Details

### Event Kinds

Two new event kinds have been added to the `EventKind` enum:
```csharp
// NIP-57 Lightning Zaps
ZapRequest = 9734,
ZapReceipt = 9735,
```

### Event Tags

New tag constants have been added to the `EventTag` class:
```csharp
// NIP-57 Zap tags
public const string Amount = "amount";
public const string Bolt11 = "bolt11";
public const string Description = "description";
public const string Preimage = "preimage";
public const string Lnurl = "lnurl";
public const string Relays = "relays";
```

### Validation

A new `ZapEventValidator` class has been created to validate Zap events:
- For Zap Requests (9734), it validates the presence of required tags: `p` (recipient) and `relays`
- For Zap Receipts (9735), it validates the presence of required tags: `p` (recipient), `bolt11`, and `description`

### Event Handling

A new `ZapEventHandler` class has been created to handle Zap events. Unlike NIP-51 list events, Zap events are not replaceable or addressable, so they are handled as regular events with the following flow:
1. Check if the event has been deleted
2. Check for duplicates
3. Save the event to the database
4. Send OK response to the client
5. Broadcast the event to other clients

### Extension Methods

A set of extension methods have been added in the `ZapEventExtensions` class to make working with Zap events easier:
- `IsZapRequest(this Event e)`: Determines if the event is a Zap Request
- `IsZapReceipt(this Event e)`: Determines if the event is a Zap Receipt
- `GetRecipientPubkey(this Event e)`: Gets the recipient's public key
- `GetBolt11(this Event e)`: Gets the bolt11 invoice
- `GetAmount(this Event e)`: Gets the amount in millisats
- `GetRelayUrls(this Event e)`: Gets the relay URLs from a Zap Request

## Testing

Tests for NIP-57 have been added in `test/Netstr.Tests/NIPs/57.feature` to verify:
1. Creating and retrieving Zap Requests
2. Creating and retrieving Zap Receipts

## Protocol Flow

The complete protocol flow for NIP-57 is as follows:

1. Client calculates a recipient's lnurl pay request url from the zap tag on the event being zapped, or from the recipient's profile.
2. Client sends a GET request to this url and parses the response.
3. When a user wants to send a zap, the client creates a zap request event (kind 9734).
4. Instead of publishing the zap request, it's sent to the recipient's lnurl pay callback url.
5. The recipient's lnurl server validates the zap request.
6. If valid, the server returns an invoice where the description is the zap request note.
7. The client pays the invoice.
8. Once paid, the recipient's lnurl server generates a zap receipt (kind 9735) and publishes it to the relays specified in the zap request.
9. Clients can fetch zap receipts on posts and profiles, and validate them.

## Comparison with NIP-51 Implementation

While NIP-51 and NIP-57 serve different purposes, the implementation approach is similar:

1. **Event Validation**: Both require specific validators to check for required tags
2. **Event Handling**: 
   - NIP-51 uses replaceable/addressable event handlers
   - NIP-57 uses a regular event handler (not replaceable)
3. **Database Storage**: Both store events with their tags in the same database structure
4. **Tag Handling**: Both require specific tag validation and processing

## Key Differences from NIP-51

1. **Event Types**: 
   - NIP-51: Replaceable (10000-10999) or Addressable (30000-30999)
   - NIP-57: Regular events (9734, 9735)

2. **Replacement Logic**:
   - NIP-51: Events can be replaced based on pubkey+kind or pubkey+kind+d-tag
   - NIP-57: Events are not replaceable, each zap request/receipt is unique

3. **Tag Requirements**:
   - NIP-51: Various tag requirements based on list type
   - NIP-57: Specific tag requirements for zap requests and receipts
