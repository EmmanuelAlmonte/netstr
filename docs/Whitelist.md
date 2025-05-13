# Public Key Whitelist

The Netstr relay supports a whitelist feature that allows you to restrict which public keys can interact with your relay. This document explains how to configure and use this feature.

## Overview

The whitelist feature allows you to:

1. Restrict which public keys can publish events to your relay
2. Optionally restrict which public keys can subscribe to events from your relay
3. Enable or disable the whitelist feature without changing your configuration

## Configuration

The whitelist is configured in the `appsettings.json` and `appsettings.Development.json` files under the `Whitelist` section:

```json
"Whitelist": {
  "Enabled": true,
  "AllowedPublicKeys": [
    "854043ae8f1f97430ca8c1f1a090bdde6488bd5115c7a45307a2a212750ae4cb",
    "07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9"
  ],
  "RestrictPublishing": true,
  "RestrictSubscribing": false
}
```

### Configuration Options

- `Enabled`: When set to `true`, the whitelist feature is active. When set to `false`, the whitelist is ignored and all public keys are allowed.
- `AllowedPublicKeys`: An array of public keys that are allowed to interact with the relay.
- `RestrictPublishing`: When set to `true`, only whitelisted public keys can publish events to the relay.
- `RestrictSubscribing`: When set to `true`, only whitelisted public keys can subscribe to events from the relay.

## How It Works

### Publishing Events

When a client attempts to publish an event to the relay:

1. If `Enabled` is `false`, the event is accepted (subject to other validation rules).
2. If `RestrictPublishing` is `false`, the event is accepted (subject to other validation rules).
3. If the event's public key is in the `AllowedPublicKeys` list, the event is accepted (subject to other validation rules).
4. Otherwise, the event is rejected with the message: `restricted: your public key is not in the whitelist`.

### Subscribing to Events

When a client attempts to subscribe to events from the relay:

1. If `Enabled` is `false`, the subscription is accepted (subject to other validation rules).
2. If `RestrictSubscribing` is `false`, the subscription is accepted (subject to other validation rules).
3. If the client is not authenticated, the subscription is rejected with the message: `auth-required: authentication required for subscription`.
4. If the client's public key is in the `AllowedPublicKeys` list, the subscription is accepted (subject to other validation rules).
5. Otherwise, the subscription is rejected with the message: `restricted: your public key is not in the whitelist`.

## Authentication Requirement

For subscription restrictions to work, clients must authenticate using the `AUTH` message as defined in [NIP-42](https://github.com/nostr-protocol/nips/blob/master/42.md). This is because the relay needs to know the client's public key to check against the whitelist.

## Interaction with Auth Mode

The whitelist feature works alongside the existing authentication modes:

- If `Auth.Mode` is set to `Always` or `Publishing`, clients must still authenticate regardless of the whitelist settings.
- If `Auth.Mode` is set to `WhenNeeded` or `Disabled`, clients only need to authenticate if they want to subscribe and `Whitelist.RestrictSubscribing` is `true`.

## Best Practices

1. **Start with a restrictive configuration**: Enable the whitelist with a small set of trusted public keys.
2. **Monitor logs**: The relay logs when events or subscriptions are rejected due to whitelist restrictions.
3. **Consider your use case**: For private relays, you might want to restrict both publishing and subscribing. For public relays that want to limit spam, you might only want to restrict publishing.

## Example Configurations

### Private Relay

```json
"Whitelist": {
  "Enabled": true,
  "AllowedPublicKeys": [
    "pubkey1",
    "pubkey2",
    "pubkey3"
  ],
  "RestrictPublishing": true,
  "RestrictSubscribing": true
}
```

### Anti-Spam Configuration

```json
"Whitelist": {
  "Enabled": true,
  "AllowedPublicKeys": [
    "pubkey1",
    "pubkey2",
    "pubkey3"
  ],
  "RestrictPublishing": true,
  "RestrictSubscribing": false
}
```

### Disabled Whitelist

```json
"Whitelist": {
  "Enabled": false,
  "AllowedPublicKeys": [],
  "RestrictPublishing": true,
  "RestrictSubscribing": false
}
```
