# Test Stabilization Baseline

## Repro Command

- `dotnet test test/Netstr.Tests/Netstr.Tests.csproj --filter "FullyQualifiedName!~MemoryLeakTest"`

## Snapshot

- Baseline before this run: `82` failed / `140` passed / `222` total (`test/Netstr.Tests` excluding MemoryLeakTest).

## Failure Inventory by Root Cause

- Harness/transforms defects
  - `test/Netstr.Tests/NIPs/Transforms.cs` throws `NotImplementedException` for unhandled message types in `CreateEventIds`.
  - Most visible trigger: spec expectations including `Type=NOTICE` are blocked before relay behavior is evaluated.

- DI/setup defects
  - `test/Netstr.Tests/Events/EventVerificationTests.cs` builds validators with `AddEventValidators()` but does not register `INip05VerificationService`.
  - This causes test construction/service-resolution failures for any scenario that exercises `Nip05Validator`.

- Shared assertion semantics drift
  - Wildcard and strict-shape matching for message tuples is inconsistent across fixtures.
  - Expected/actual drift appears mostly in SpecFlow shared assertions for `Then ... receives messages` and message tuple transforms.

- Feature-specific behavior expectation drift
  - Remaining failures after fixes above are expected to cluster around NIP-01/02/04/05/51/57/65 expectations where relay assertions remain protocol-evolution sensitive.

## Top 3 Blockers by Impact

1. `CreateEventIds` transform exceptions (non-deterministic scenario abort across many NIP specs).
2. Missing `INip05VerificationService` registration in `EventVerificationTests` (hard DI failure path).
3. Inconsistent wildcard/tuple expectation interpretation in shared step assertions.

## Immediate Follow-up

- Task 2: implement transform completion in `NIPs/Transforms.cs`.
- Task 3: provide deterministic `INip05VerificationService` in `Events/EventVerificationTests.cs` and proceed to shared expectation normalization.
