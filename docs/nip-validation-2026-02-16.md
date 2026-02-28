# NIP Validation Audit (2026-02-16)

## Scope

Validation source:
- Local specs under `nips/`.
- Current relay implementation under `src/Netstr/`.
- Test coverage under `test/Netstr.Tests/`.

Validation command:
- `dotnet test test/Netstr.Tests/Netstr.Tests.csproj --filter "FullyQualifiedName!~MemoryLeakTest"`

Observed status:
- Baseline before this conformance refresh: `221` passed / `1` failed / `222` total.
- Current non-memory-leak run (`dotnet test test/Netstr.Tests/Netstr.Tests.csproj --filter "FullyQualifiedName!~MemoryLeakTest"`): `240` passed / `0` failed / `240` total.
- Remaining failure: none.

## Supported NIP Coverage Snapshot

Declared support (`src/Netstr/appsettings.json:92`):
- `1, 2, 4, 5, 9, 11, 13, 17, 40, 42, 45, 50, 51, 57, 59, 62, 64, 65, 70, 77, 78, 119`

Feature-level SpecFlow coverage currently present:
- `1, 2, 4, 5, 9, 11, 13, 17, 40, 42, 45, 51, 57, 62, 64, 65, 70, 77, 119`

## Confirmed Alignments

1. NIP-01 subscription replacement and filter semantics are implemented in core request flow.
   - Spec reference: `nips/01.md:135`, `nips/01.md:145`, `nips/01.md:147`.
   - Implementation reference: `src/Netstr/Messaging/Subscriptions/SubscriptionsAdapter.cs:36`, `src/Netstr/Messaging/Subscriptions/MatchingExtensions.cs:72`.

2. NIP-45 COUNT OR-aggregation behavior is implemented and returns a single count.
   - Spec reference: `nips/45.md:17`, `nips/45.md:30`.
  - Implementation reference: `src/Netstr/Messaging/MessageHandlers/CountMessageHandler.cs:42`, `src/Netstr/Messaging/Subscriptions/MatchingExtensions.cs:22`.

3. NIP-50 extension parsing and unsupported-extension non-reduction are implemented.
   - Spec reference: `nips/50.md:31`, `nips/50.md:32`.
   - Implementation reference: `src/Netstr/Messaging/Subscriptions/SearchQueryParser.cs:27`, `src/Netstr/Messaging/Events/DbExtensions.cs:42`.
   - Conformance coverage: `test/Netstr.Tests/SearchSemanticsIntegrationTests.cs:102`, `test/Netstr.Tests/SearchSemanticsIntegrationTests.cs:135`.

4. NIP-65 relay-list structural validation is implemented.
   - Spec reference: `nips/65.md:11`.
   - Implementation reference: `src/Netstr/Messaging/Events/Validators/RelayListValidator.cs:26`, `src/Netstr/Messaging/Events/Validators/RelayListValidator.cs:41`, `src/Netstr/Messaging/Events/Validators/RelayListValidator.cs:58`.

5. NIP-70 protected-event publication enforcement is implemented.
   - Spec reference: `nips/70.md:15`.
   - Implementation reference: `src/Netstr/Messaging/Events/Validators/ProtectedEventValidator.cs:19`.

6. NIP-42 multi-pubkey AUTH support is implemented.
   - Spec reference: `nips/42.md:35`.
   - Implementation reference: `src/Netstr/Messaging/Models/ClientContext.cs:17`, `src/Netstr/Messaging/Models/ClientContext.cs:35`.
   - Conformance coverage: `test/Netstr.Tests/Events/ClientContextTests.cs:12`.

7. NIP-42 AUTH timestamp strictness now uses AUTH-specific checks.
   - Spec reference: `nips/42.md:106`.
   - Implementation reference: `src/Netstr/Messaging/Events/Validators/AuthCreatedAtValidator.cs:16`, `src/Netstr/Messaging/Events/Validators/AuthCreatedAtValidator.cs:37`, `src/Netstr/Extensions/MessagingExtensions.cs:74`.
   - Conformance coverage: `test/Netstr.Tests/Events/AuthCreatedAtValidatorTests.cs:8`.

8. NIP-59 kind `13` requires empty tags.
   - Spec reference: `nips/59.md:56`.
   - Implementation reference: `src/Netstr/Messaging/Events/Validators/SealEventValidator.cs:1`, `src/Netstr/Extensions/MessagingExtensions.cs:78`.
   - Conformance coverage: `test/Netstr.Tests/Events/SealEventValidatorTests.cs:8`, `test/Netstr.Tests/Nip59And78ConformanceTests.cs:16`, `test/Netstr.Tests/Nip59And78ConformanceTests.cs:39`.

9. NIP-78 kind `30078` `d`-tag requirement is enforced.
   - Spec reference: `nips/78.md:15`.
   - Implementation reference: `src/Netstr/Messaging/Events/Validators/ListEventValidator.cs:40`, `src/Netstr/Messaging/Events/Validators/ListEventValidator.cs:55`.
   - Conformance coverage: `test/Netstr.Tests/Events/ListEventValidatorTests.cs:46`, `test/Netstr.Tests/Nip59And78ConformanceTests.cs:58`, `test/Netstr.Tests/Nip59And78ConformanceTests.cs:75`.

10. NIP-119 source/spec consistency is restored locally.
   - Spec reference: `nips/119.md`.
   - Implementation/reference: `src/Netstr/appsettings.json:92`, `test/Netstr.Tests/NIPs/119.feature:1`.

## Remaining Gaps

1. No dedicated SpecFlow feature files exist for NIP-50, NIP-59, and NIP-78.
   - No `test/Netstr.Tests/NIPs/50.feature`, `test/Netstr.Tests/NIPs/59.feature`, or `test/Netstr.Tests/NIPs/78.feature`.
   - Conformance coverage is covered by integration/unit tests in `test/Netstr.Tests/SearchSemanticsIntegrationTests.cs` and `test/Netstr.Tests/Nip59And78ConformanceTests.cs`.

## Residual Test Failure (Non-Memory-Leak Suite)

Fixed in this refresh:
- `Netstr.Tests.RateLimitingTests.SubscriptionsRateLimitedTest` now uses unique subscription ids for each request (`test/Netstr.Tests/RateLimitingTests.cs:79`), matching relay-replacement semantics and passing consistently.

Residual test failures:
- none
