<!--
=== SYNC IMPACT REPORT (temporary — remove before committing) ===

Version change: N/A (no prior version) → 1.0.0
Basis: Initial constitution. The existing file contained only an unpopulated
       template skeleton; all placeholder tokens have been replaced.

Added sections:
  - I.  Target Platform & Scope
  - II. Architecture & Patterns
  - III. Data Access & Persistence
  - IV.  Android-Specific UI & Integrations
  - V.   Code Quality & Governance
  - Android-Specific Technical Constraints
  - Development Workflow
  - Governance

Removed sections: None (skeleton had no authored content).

Modified principles: N/A — first populated version.

Follow-up TODOs:
  - RATIFICATION_DATE is set to today (2026-09-15) as the constitution's first
    adoption date. If a different historical date applies, amend manually.

=== END SYNC IMPACT REPORT ===
-->

# YoutubeShortsEditorMobile Constitution

## Core Principles

### I. Target Platform & Scope

This project is an **Android-only** .NET MAUI application. All implementation
decisions MUST be scoped exclusively to the Android platform.

- The `.csproj` MUST list only the Android target framework (e.g.,
  `net9.0-android` or the latest stable equivalent). `net9.0-ios`,
  `net9.0-maccatalyst`, and `net9.0-windows*` target frameworks MUST NOT be
  present.
- Android SDK versions (`SupportedOSPlatformVersion`, `TargetPlatformVersion`)
  MUST track the latest stable .NET release channel. Agents MUST verify and
  update SDK pins whenever a new stable .NET version ships.
- Cross-platform abstractions that exist solely to support non-Android targets
  MUST NOT be added. Code that will never run on Android is dead weight and is
  forbidden.

**Rationale**: Eliminating unused target frameworks removes compilation overhead,
reduces CI build times, and prevents accidental regressions on platforms the
app does not ship to.

### II. Architecture & Patterns

The application MUST follow the **Model-View-ViewModel (MVVM)** pattern,
enforced via **CommunityToolkit.Mvvm**.

- ViewModels MUST inherit from `ObservableObject` (or `CommunityToolkit.Mvvm`
  base classes) and MUST use source-generator attributes such as
  `[ObservableProperty]` and `[RelayCommand]` to eliminate boilerplate.
- Manual `INotifyPropertyChanged` implementations are forbidden when a
  source-generator attribute covers the same use case.
- All ViewModels, services, repositories, and navigation/routing dependencies
  MUST be registered with **Microsoft.Extensions.DependencyInjection** in
  `MauiProgram.cs` (or a dedicated service-registration module). Constructor
  injection is the ONLY permitted injection strategy; service-locator patterns
  (e.g., static `App.Current` casts) are forbidden.
- Complex UI event streams, background tasks, and multi-step state transitions
  MUST be modelled using **System.Reactive (Rx.NET)** observables or
  **`IAsyncEnumerable<T>`** async streams. `Task`-based polling loops that
  could be replaced with reactive pipelines MUST be refactored.

**Rationale**: MVVM with source generators keeps Views thin and testable.
DI enforces loose coupling. Rx.NET / async streams prevent callback hell and
make concurrency intent explicit.

### III. Data Access & Persistence

All local data persistence MUST use **Entity Framework Core** with the
**SQLite provider** (`Microsoft.EntityFrameworkCore.Sqlite`).

- Every database query MUST be asynchronous: `ToListAsync`, `FirstOrDefaultAsync`,
  `SaveChangesAsync`, etc. Synchronous EF Core methods (`ToList`, `FirstOrDefault`,
  `SaveChanges`) on the main thread are **forbidden**.
- `DbContext` instances MUST be resolved from DI with an appropriate lifetime
  (typically `Transient` or scoped per ViewModel/screen) to avoid threading issues.
- Raw SQL strings are discouraged; use strongly-typed LINQ expressions. Raw SQL
  is permitted only for performance-critical queries, and MUST be accompanied by
  a comment justifying the deviation.

**Rationale**: Async queries keep the Android UI thread unblocked. EF Core with
LINQ provides compile-time safety and migration support.

### IV. Android-Specific UI & Integrations

All XAML layouts and visual controls MUST adhere to **Material 3 Design**
guidelines as exposed by the MAUI Material theme / `Material3` control styles.

- Color tokens, typography scale, shape tokens, and elevation/state-layer
  patterns from Material 3 MUST be used consistently. Custom ad-hoc colors or
  fonts that duplicate existing Material tokens are forbidden.
- When a cross-platform MAUI abstraction demonstrably degrades performance or
  prevents access to a required Android capability, agents MAY use direct
  Android platform API calls (via `#if ANDROID` guards or `[SupportedOSPlatform]`
  annotated methods). Such deviations MUST be documented with a comment explaining
  why the cross-platform layer was insufficient.
- Android-specific permissions, intents, and lifecycle events MUST be handled in
  platform-native code paths rather than emulated through indirect workarounds.

**Rationale**: Material 3 ensures a cohesive, modern Android experience. Allowing
targeted platform API calls prevents the cross-platform abstraction from becoming
a ceiling on quality or performance.

### V. Code Quality & Governance

High code quality is non-negotiable. The following rules apply to all source
files in this repository.

- **Nullable reference types** MUST be enabled (`<Nullable>enable</Nullable>`
  in the `.csproj`) and nullable warnings MUST be treated as errors
  (`<WarningsAsErrors>Nullable</WarningsAsErrors>`). No `null!` suppressions
  without a justifying comment.
- Code MUST adhere to **SOLID principles**:
  - Single Responsibility: each class has exactly one reason to change.
  - Open/Closed: extend via abstraction, not modification.
  - Liskov Substitution: subtypes are substitutable for base types.
  - Interface Segregation: small, focused interfaces over large monoliths.
  - Dependency Inversion: depend on abstractions, not concrete implementations.
- Prefer **early returns** and **guard clauses** to deeply nested `if` trees.
- Use **concise LINQ** operations over imperative loops where readability and
  performance are equivalent. Avoid LINQ chains exceeding 5-6 operations; extract
  into named methods for clarity.
- All public APIs (types, members) MUST have XML documentation comments.
- Classes MUST be kept small and focused. A class exceeding ~300 lines of code
  SHOULD be reviewed for decomposition.

**Rationale**: Strict null-safety catches a class of runtime crashes at
compile time. SOLID, guard clauses, and concise LINQ keep the codebase
maintainable as the feature set grows.

## Android-Specific Technical Constraints

The following technology choices are binding for all feature implementations:

| Concern | Required Technology |
|---|---|
| UI Framework | .NET MAUI (Android target only) |
| MVVM Toolkit | CommunityToolkit.Mvvm (source generators enabled) |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |
| Reactive Streams | System.Reactive (Rx.NET) + `IAsyncEnumerable<T>` |
| Local Database | Entity Framework Core + SQLite provider |
| Design System | Material 3 (via MAUI Material theme) |
| Nullable Safety | `<Nullable>enable</Nullable>` + warnings as errors |
| Platform API Access | Direct Android API calls under `#if ANDROID` guards |

No alternative libraries that duplicate these responsibilities may be introduced
without a formal constitution amendment.

## Development Workflow

- **Branch strategy**: Feature branches off `main`; PRs require at least one
  review before merge.
- **Build gate**: The Android project MUST build without warnings on every PR.
  Nullable warnings that are elevated to errors MUST be resolved, not suppressed.
- **EF Core migrations**: Any `DbContext` schema change MUST include a matching
  EF Core migration script committed alongside the model change.
- **Reactive pipelines**: New background or event-driven workflows MUST be
  reviewed for correctness of subscription lifecycle (no subscription leaks;
  `CompositeDisposable` or `CancellationToken` MUST be used appropriately).
- **Material 3 review**: UI PRs MUST include a screenshot or recording showing
  the change on an Android device or emulator demonstrating correct Material 3
  styling.

## Governance

This constitution supersedes all other conventions, READMEs, or informal
agreements within this repository. Where a conflict arises, the constitution
prevails.

**Amendment procedure**:
1. Propose a change via a dedicated PR updating `.specify/memory/constitution.md`.
2. The PR description MUST include a rationale for the change and its impact on
   existing implementations.
3. At least one additional team member MUST approve the PR before merge.
4. The `CONSTITUTION_VERSION` MUST be incremented following semantic versioning:
   - **MAJOR**: Removal or incompatible redefinition of an existing principle.
   - **MINOR**: Addition of a new principle or material expansion of guidance.
   - **PATCH**: Clarifications, wording improvements, typo fixes.
5. `LAST_AMENDED_DATE` MUST be updated to the merge date (ISO 8601).

**Compliance review**: All implementation PRs MUST be checked for adherence to
the principles herein. Reviewers are empowered to block merges that violate the
constitution. Agents performing automated implementation MUST read and follow
this constitution before generating any code.

**Version**: 1.0.0 | **Ratified**: 2026-09-15 | **Last Amended**: 2026-09-15
