# [netstr - a nostr relay](https://relay.netstr.io/)
[![release](https://img.shields.io/github/v/release/EmmanuelAlmonte/netstr)](https://github.com/EmmanuelAlmonte/netstr/releases)
[![build](https://github.com/EmmanuelAlmonte/netstr/actions/workflows/build-deploy.yml/badge.svg)](https://github.com/EmmanuelAlmonte/netstr/actions/workflows/build-deploy.yml)

![netstr logo](art/logo.jpg)

Netstr is a modern relay for the [nostr protocol](https://github.com/nostr-protocol/nostr) written in C#. 

Upstream acknowledgment: this repository is forked from [bezysoftware/netstr](https://github.com/bezysoftware/netstr), with gratitude to its original maintainers and contributors.

 * **Prod** instance: https://relay.netstr.io/
 * **Dev** instance: https://relay-dev.netstr.io/ (feel free to play with it / try to break it, just report if you find anything that needs fixing)

## Features

NIPs with a relay-specific implementation are listed here.

- [x] NIP-01: [Basic protocol flow description](https://github.com/nostr-protocol/nips/blob/master/01.md)
- [x] NIP-02: [Follow list](https://github.com/nostr-protocol/nips/blob/master/02.md)
- [x] NIP-04: [Encrypted Direct Message](https://github.com/nostr-protocol/nips/blob/master/04.md) (deprecated in favor of NIP-17)
- [x] NIP-05: [Mapping Nostr keys to DNS-based internet identifiers](https://github.com/nostr-protocol/nips/blob/master/05.md)
- [x] NIP-09: [Event deletion](https://github.com/nostr-protocol/nips/blob/master/09.md)
- [x] NIP-11: [Relay information document](https://github.com/nostr-protocol/nips/blob/master/11.md)
- [x] NIP-13: [Proof of Work](https://github.com/nostr-protocol/nips/blob/master/13.md)
- [x] NIP-17: [Private Direct Messages](https://github.com/nostr-protocol/nips/blob/master/17.md)
- [x] NIP-40: [Expiration Timestamp](https://github.com/nostr-protocol/nips/blob/master/40.md)
- [x] NIP-42: [Authentication of clients to relays](https://github.com/nostr-protocol/nips/blob/master/42.md)
- [x] NIP-45: [Counting results](https://github.com/nostr-protocol/nips/blob/master/45.md)
- [x] NIP-50: [Search Capability](https://github.com/nostr-protocol/nips/blob/master/50.md)
- [x] NIP-51: [Lists](https://github.com/nostr-protocol/nips/blob/master/51.md)
- [x] NIP-57: [Lightning Zaps](https://github.com/nostr-protocol/nips/blob/master/57.md)
- [x] NIP-59: [Gift Wrap](https://github.com/nostr-protocol/nips/blob/master/59.md)
- [x] NIP-60: [Cashu Wallet and Token](https://github.com/nostr-protocol/nips/blob/master/60.md)
- [x] NIP-62: [Request to Vanish](https://github.com/vitorpamplona/nips/blob/right-to-vanish/62.md)
- [x] NIP-64: [Chess (Portable Game Notation)](https://github.com/nostr-protocol/nips/blob/master/64.md)
- [x] NIP-65: [Relay List Metadata](https://github.com/nostr-protocol/nips/blob/master/65.md)
- [x] NIP-70: [Protected events](https://github.com/nostr-protocol/nips/blob/master/70.md)
- [x] NIP-77: [Negentropy syncing](https://github.com/nostr-protocol/nips/pull/1494)
- [x] NIP-78: [Application-specific Data](https://github.com/nostr-protocol/nips/blob/master/78.md)
- [x] NIP-119: [AND operator for filters](https://github.com/nostr-protocol/nips/pull/1365)

## Additional Features

- [x] **Public Key Whitelist**: Restrict which public keys can publish events and/or subscribe to your relay. [Learn more](docs/Whitelist.md)

## Tests

Supported NIPs are covered by automated tests using [Specflow / Gherkin language](https://docs.specflow.org/projects/specflow/en/latest/Gherkin/Gherkin-Reference.html) and xUnit integration/unit tests.
The scenarios are described in plain English which lets anyone read them and even contribute with new ones without any programming skills. See sample (simplified):

```gherkin
Scenario: Newly subscribed client receives matching events, EOSE and future events
    Given a relay is running
    And Alice is connected to relay
    And Bob is connected to relay
    When Bob publishes events
    | Id                                                               | Content  | Kind  | CreatedAt  |
    | a6d166e834e78827af0770f31f15b13a772f281ad880f43ce12c24d4e3d0e346 | Hello 1  | 1     | 1722337838 |
    | cb952d0ab727c3fcaf94e6809a64d1a27ff87cae5be583398ee7f0f1381d6b66 | Hello MD | 30023 | 1722337839 |
    And Alice sends a subscription request abcd
    | Kinds |
    | 1     |
    And Bob publishes an event
    | Id                                                               | Content | Kind | CreatedAt  |
    | 8013e4630a69528007355f65e01936c9b761a4bbd9340b60a4bd0222b15b7cf3 | Hello 2 | 1    | 1722337840 |
    Then Alice receives messages
    | Type  | Id   | EventId                                                          |
    | EVENT | abcd | a6d166e834e78827af0770f31f15b13a772f281ad880f43ce12c24d4e3d0e346 |
    | EOSE  | abcd |                                                                  |
    | EVENT | abcd | 8013e4630a69528007355f65e01936c9b761a4bbd9340b60a4bd0222b15b7cf3 |
    And Bob receives messages
    | Type | Id                                                               | Success | 
    | OK   | a6d166e834e78827af0770f31f15b13a772f281ad880f43ce12c24d4e3d0e346 | true    |
    | OK   | cb952d0ab727c3fcaf94e6809a64d1a27ff87cae5be583398ee7f0f1381d6b66 | true    |
    | OK   | 8013e4630a69528007355f65e01936c9b761a4bbd9340b60a4bd0222b15b7cf3 | true    |
```

Above scenario simulates that `Bob` publishes events to a relay, `Alice` creates a subscription and `Bob` publishes more events. The scenario then asserts that `Alice` and `Bob`
both received their expected messages in correct order.

## Setup

Netstr is a C# app backed by PostgreSQL. You have several options to get up and running:

* Using `dotnet run`
* Using `docker run`
* Using `docker compose`
* Using host-native `systemd + nginx` (Relay Wizard installer contract)
* Deploying to Azure

### Dotnet run

* Install .NET: https://dotnet.microsoft.com/en-us/download
* Install Postgres: https://www.postgresql.org/download/
* Copy `src/Netstr/appsettings.local.json.example` to `src/Netstr/appsettings.local.json`
* Set `ConnectionStrings:NetstrDatabase` in `src/Netstr/appsettings.local.json` (or set env var `ConnectionStrings__NetstrDatabase`)
* Use `src/Netstr/appsettings.example.json` as a safe baseline if you need a full config template
* Run `dotnet run --project .\src\Netstr\Netstr.csproj`
* Preflight config check: `dotnet run --project .\src\Netstr\Netstr.csproj -- --check-config`
* Print version: `dotnet run --project .\src\Netstr\Netstr.csproj -- --version`

### Host-native (systemd + nginx)

* Build release artifact: `bash scripts/package-linux-release.sh <version>`
  * Output archive: `artifacts/release/netstr-<version>-x86_64-linux-gnu.tar.gz`
  * Archive contains one binary named exactly `netstr`
  * Sidecar checksum: `artifacts/release/netstr-<version>-x86_64-linux-gnu.tar.gz.sha256`
* Install binary to `/usr/local/bin/netstr`
* Copy `scripts/systemd/netstr.service` to `/etc/systemd/system/netstr.service`
* Copy `scripts/systemd/netstr.env.example` to `/etc/netstr/netstr.env` and set real values
* Ensure runtime directories exist and are owned by the `netstr` user:
  * `/etc/netstr`
  * `/var/lib/netstr`
  * `/var/log/netstr`
* Default internal listen is `http://127.0.0.1:8086` (set with `ASPNETCORE_URLS`)
* For nginx reverse proxy mode, keep `Connection__UseHttpsRedirection=false` and `Connection__UseForwardedHeaders=true`
* Health check endpoint: `curl -sS http://127.0.0.1:8086/healthz`
* NIP-11 check through proxy:
  * `curl -H "Accept: application/nostr+json" https://<domain>/`

Relay metadata can be fully env-driven via:
* `RelayInformation__Name`
* `RelayInformation__Description`
* `RelayInformation__PublicKey`
* `RelayInformation__Contact` (optional)
* `RelayInformation__Version`

### PostgreSQL ops scripts

* Backup: `bash scripts/db-backup.sh [output.dump]`
* Restore: `bash scripts/db-restore.sh <backup.dump>`
* Connectivity + migration status: `bash scripts/db-check.sh`
* Strict migration check mode: `bash scripts/db-check.sh --strict-migrations`
  * In default mode, a fresh database without `__EFMigrationsHistory` is reported as pending and not treated as failure.
* Optional env file override for all scripts: `NETSTR_ENV_FILE=/path/to/netstr.env`

### Docker run

* Install Docker: https://docs.docker.com/engine/install/
* Install Postgres: https://www.postgresql.org/download/
* Run `docker run -e ConnectionStrings__NetstrDatabase=YOUR_CONNECTION_STRING bezysoftware/netstr:latest`
  * Set your connection string to point to your Postgres instance
* Note: Docker examples default to the upstream image. You can override with `NETSTR_IMAGE` (for example, a locally built image) when using Compose.

### Docker compose

Docker compose contains a Postgres DB service so no need to install it manually. You will however need to set the following environment variable:
 * NETSTR_DB_PASSWORD - password for Postgres DB
 
Optionally you can also set following variables:
 * NETSTR_IMAGE - docker image (default `bezysoftware/netstr:latest`)
 * NETSTR_PORT - port on which the relay will be accessible (default 8080)
 * NETSTR_ENVIRONMENT - will be used to name the compose instance (default 'prod')
 * NETSTR_ENVIRONMENT_LONG - will be used inside the application to load specific configuration (default 'Production')
 * NETSTR_RELAY_NAME - relay metadata name
 * NETSTR_RELAY_DESCRIPTION - relay metadata description
 * NETSTR_RELAY_PUBLIC_KEY - relay metadata public key
 * NETSTR_RELAY_CONTACT - relay metadata contact
 * NETSTR_USE_HTTPS_REDIRECTION - `true|false` (recommended `false` behind nginx)
 * NETSTR_USE_FORWARDED_HEADERS - `true|false` (recommended `true` behind nginx)

### Deploying to Azure

The `scripts` folder contains scripts to setup a VM in Azure with everything you'll need to run a Netstr instance:
 * Separate VM with an attached data disk
 * Docker with Compose to run the `compose.yaml`
 * Nginx with certbot which generates an SSL certificate for your domain
