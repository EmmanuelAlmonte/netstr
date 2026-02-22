# Database Retention and Cleanup Policy

This document explains the data retention policies configured for the Netstr relay.

## Automatic Cleanup Service

The cleanup service runs **daily** (configured in `CleanupBackgroundService.cs`) and removes events based on the following rules:

### 1. Soft-Deleted Events
**Retention**: 7 days after deletion
**Configuration**: `DeleteDeletedEventsAfterDays: 7`

When events are deleted via NIP-09 delete events, they are "soft deleted" (marked with `DeletedAt` timestamp). After 7 days, these soft-deleted events are permanently removed from the database.

**Example**: An event deleted on January 1st will be permanently removed on January 8th.

### 2. Expired Events
**Retention**: 7 days after expiration
**Configuration**: `DeleteExpiredEventsAfterDays: 7`

Events with an expiration tag (NIP-40) are automatically removed 7 days after their expiration date.

**Example**: An event with expiration set to February 1st will be permanently removed on February 8th.

### 3. Event Kind-Based Cleanup Rules

#### Kind 17 (Private Direct Messages)
**Retention**: 14 days
**Reason**: Privacy - private messages should not be stored indefinitely

```json
{
  "Kinds": ["17"],
  "DeleteAfterDays": 14
}
```

#### Kind 40000+ (Custom/Experimental Events)
**Retention**: 7 days
**Reason**: These are typically temporary or experimental event types

```json
{
  "Kinds": ["40000-"],
  "DeleteAfterDays": 7
}
```

## Ephemeral Events (Not Stored)

Events with kinds **20000-29999** are **never stored** to the database per NIP-01 specification. These are broadcast to connected clients but immediately discarded.

Examples:
- Kind 20000: Typing indicators
- Kind 20001: Presence updates
- Kind 20002: Live activities

## Adjusting Retention Policies

To modify retention periods, edit `appsettings.json` or `appsettings.local.json`:

```json
{
  "Cleanup": {
    "DeleteDeletedEventsAfterDays": 30,  // Increase to 30 days
    "DeleteExpiredEventsAfterDays": 30,  // Increase to 30 days
    "DeleteEventsRules": [
      {
        "Kinds": ["17"],
        "DeleteAfterDays": 30  // Keep private messages for 30 days
      }
    ]
  }
}
```

### Recommended Settings by Use Case

**Public Relay (High Traffic)**
- DeleteDeletedEventsAfterDays: 7
- DeleteExpiredEventsAfterDays: 7
- Kind 17: 7-14 days

**Private/Community Relay**
- DeleteDeletedEventsAfterDays: 30-90
- DeleteExpiredEventsAfterDays: 30-90
- Kind 17: 30-90 days

**Archive Relay**
- DeleteDeletedEventsAfterDays: 365+
- DeleteExpiredEventsAfterDays: 365+
- Consider removing Kind 17 rule entirely

## Monitoring Cleanup

Cleanup metrics are logged at INFO level. Check your logs for:

```
[INF] Cleanup: removed 42 soft-deleted events older than 7 days
[INF] Cleanup: removed 15 expired events older than 7 days
[INF] Cleanup: removed 8 events matching kind rule (kinds: 17, 14 days old)
[INF] Cleanup completed in 2.5 seconds: deleted 65 total events
```

For slow cleanup operations (>60 seconds), a WARNING is logged:

```
[WRN] Cleanup took 125 seconds to delete 50000 events
```

## Database Storage Considerations

### Supabase Free Tier
- 500MB database storage
- Monitor usage at: https://app.supabase.com/project/_/settings/billing

### Calculating Storage Needs

Average event size: ~1-2KB (depending on tags and content)

| Daily Events | Monthly Storage | Recommended Retention |
|--------------|-----------------|----------------------|
| 100 | ~6MB | 90+ days |
| 1,000 | ~60MB | 30-90 days |
| 10,000 | ~600MB | 7-30 days |
| 100,000 | ~6GB | 1-7 days |

## Best Practices

1. **Monitor cleanup logs daily** to ensure cleanup is running
2. **Adjust retention based on storage limits** and relay purpose
3. **Consider database backups** before reducing retention periods
4. **Test retention changes** on development environment first
5. **Document custom rules** for your specific relay needs

## Related NIPs

- **NIP-09**: Event Deletion
- **NIP-16**: Event Treatment (Ephemeral, Replaceable, etc.)
- **NIP-40**: Event Expiration
- **NIP-62**: Vanish Requests
