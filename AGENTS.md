# Repository Guidelines

## Project Structure & Module Organization
`src/Netstr/` contains the ASP.NET Core relay implementation (messaging, events, subscriptions, middleware, options, controllers, and EF Core data/migrations).  
`test/Netstr.Tests/` contains test code: unit/integration tests plus SpecFlow NIP scenarios in `NIPs/*.feature` and step definitions in `NIPs/Steps/`.  
`scripts/` contains operational and utility scripts (deployment, relay probe, secret checks).  
`docs/` stores architecture and NIP notes; `art/` stores branding assets.

## Build, Test, and Development Commands
- `dotnet restore Netstr.sln` - restore dependencies.
- `dotnet build Netstr.sln` - compile app and tests.
- `dotnet run --project src/Netstr/Netstr.csproj` - run relay locally.
- `dotnet test test/Netstr.Tests/Netstr.Tests.csproj` - run full test suite.
- `dotnet test test/Netstr.Tests/Netstr.Tests.csproj --filter "FullyQualifiedName!~MemoryLeakTest"` - run suite excluding memory leak test.
- `dotnet test test/Netstr.Tests/Netstr.Tests.csproj --collect:"XPlat Code Coverage"` - generate coverage via Coverlet collector.
- `pwsh -File scripts/check-no-connection-secrets.ps1` - fail if appsettings contain hardcoded DB passwords.

## Coding Style & Naming Conventions
Use C# (`net9.0`) with nullable enabled. Follow `.editorconfig` and keep member access explicitly qualified where required (for example, `this.field`).  
Use 4-space indentation, PascalCase for types/methods/properties, and camelCase for locals/parameters.  
Keep naming and folder placement consistent with existing patterns (for example, validators under `Messaging/Events/Validators`).

## Testing Guidelines
Primary frameworks: xUnit, SpecFlow.xUnit, FluentAssertions, and Moq.  
Name tests `*Tests.cs` and keep behavior-specific assertions near corresponding NIP feature files when applicable.  
When changing relay behavior, update both unit/integration tests and any impacted SpecFlow scenarios.

## Task Tracking (Software Planning MCP)
For multi-step work, track execution in the Software Planning MCP server instead of ad-hoc notes.  
Create a goal first, then add scoped todos with priority, complexity, and dependencies.  
Keep exactly one todo in `in_progress`, and update statuses as work moves (`pending` -> `in_progress` -> `completed`/`blocked`).  
Save an implementation plan for larger efforts and keep it aligned with actual execution.  
Before handoff, ensure goal/todo state reflects reality and includes any remaining follow-up tasks.

## Commit & Pull Request Guidelines
Use Conventional Commit style as seen in history (`feat:`, `fix:`, `chore:`, `refactor:`, `test:`), e.g. `fix: align REQ/COUNT filtering with NIPs`.  
PRs should complete the template: clear description, related issue, motivation/context, test evidence, and updated docs when needed.

## Security & Configuration Tips
Do not commit secrets. Put local credentials in `src/Netstr/appsettings.local.json` (gitignored) or environment variables such as `ConnectionStrings__NetstrDatabase`.  
Start from `src/Netstr/appsettings.example.json` or `src/Netstr/appsettings.local.json.example` for safe configuration templates.
