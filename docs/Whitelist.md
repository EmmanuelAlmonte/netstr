# Public Key Whitelist

Netstr can declaratively restrict which public keys publish events or create subscriptions. Whitelist settings come from the relay's normal configuration sources; the relay does not expose an HTTP surface for reading or changing them.

## Configuration

Configure the `Whitelist` section in `appsettings.json`, a deployment-specific settings file, or equivalent environment variables:

```json
"Whitelist": {
  "Enabled": true,
  "AllowedPublicKeys": [
    "854043ae8f1f97430ca8c1f1a090bdde6488bd5115c7a45307a2a212750ae4cb",
    "07caba282f76441955b695551c3c5c742e5b9202a3784780f8086fdcdc1da3a9"
  ],
  "RestrictPublishing": true,
  "RestrictSubscribing": false,
  "OwnerPublicKey": "854043ae8f1f97430ca8c1f1a090bdde6488bd5115c7a45307a2a212750ae4cb",
  "ExemptKinds": [375, 9735, 17375]
}
```

Environment-variable keys use the standard ASP.NET Core double-underscore separator, such as `Whitelist__Enabled` and `Whitelist__AllowedPublicKeys__0`.

### Options

- `Enabled`: Activates whitelist enforcement when `true`.
- `AllowedPublicKeys`: Public keys allowed through enabled publishing or subscription restrictions.
- `RestrictPublishing`: Restricts event publishing to allowed public keys when `true`.
- `RestrictSubscribing`: Restricts subscriptions to allowed authenticated public keys when `true`.
- `OwnerPublicKey`: Relay-owner metadata. It does not independently grant access; include the owner in `AllowedPublicKeys` when the owner must pass whitelist checks.
- `ExemptKinds`: Event kinds allowed to bypass the publishing whitelist. The generic configuration exempts kinds `375`, `9735`, and `17375`.
- `RateLimitExemptPublicKeys`: Public keys exempt from event rate limiting.

Apply whitelist changes through the configuration source managed by your deployment. Keep secrets and deployment-specific values in untracked local settings or environment variables.

## Publishing Behavior

When a client publishes an event:

1. If the whitelist is disabled or publishing is unrestricted, whitelist validation allows the event.
2. If the event kind appears in `ExemptKinds`, whitelist validation allows the event.
3. If the event public key appears in `AllowedPublicKeys`, whitelist validation allows the event.
4. Otherwise, the relay rejects the event as restricted.

Other relay validation still applies to events allowed by the whitelist.

## Subscription Behavior

When a client creates a subscription:

1. If the whitelist is disabled or subscriptions are unrestricted, whitelist validation allows the subscription.
2. Restricted subscriptions require a client identity established through NIP-42 authentication.
3. An authenticated public key in `AllowedPublicKeys` is allowed.
4. Other clients are rejected as restricted.

Other relay limits and validation still apply to subscriptions allowed by the whitelist.

## Examples

### Private Relay

```json
"Whitelist": {
  "Enabled": true,
  "AllowedPublicKeys": [ "pubkey1", "pubkey2" ],
  "RestrictPublishing": true,
  "RestrictSubscribing": true,
  "OwnerPublicKey": "pubkey1",
  "ExemptKinds": [375, 9735, 17375]
}
```

### Publishing Restriction Only

```json
"Whitelist": {
  "Enabled": true,
  "AllowedPublicKeys": [ "pubkey1", "pubkey2" ],
  "RestrictPublishing": true,
  "RestrictSubscribing": false,
  "OwnerPublicKey": "",
  "ExemptKinds": [375, 9735, 17375]
}
```

### Disabled

```json
"Whitelist": {
  "Enabled": false,
  "AllowedPublicKeys": [],
  "RestrictPublishing": true,
  "RestrictSubscribing": false,
  "OwnerPublicKey": "",
  "ExemptKinds": [375, 9735, 17375]
}
```
