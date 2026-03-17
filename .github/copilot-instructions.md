# Project overview

This repository contains a .NET 10 solution for an online Blood on the Clocktower companion app.

## Solution structure

- BOTC.Domain contains core business rules and domain models.
- BOTC.Application contains use cases, orchestration, and abstractions.
- BOTC.Contracts contains API and realtime contracts shared between client and server.
- BOTC.Infrastructure contains persistence and external integrations.
- BOTC.Presentation.Api contains the ASP.NET Core API and SignalR host.
- BOTC.Presentation.Desktop contains the WPF desktop client.

## Architecture rules

- Follow Clean Architecture principles.
- Dependencies must point inward.
- Do not put business logic in presentation or infrastructure code.
- Keep the domain model framework-independent.
- Prefer feature-oriented organization over technical dumping grounds.

## Domain rules

- Keep business rules in the Domain layer.
- Prefer explicit domain language in method and type names.
- Use value objects where they improve clarity and safety.
- Avoid primitive obsession where reasonable.
- Protect invariants inside domain entities.

## Application rules

- Model use cases explicitly.
- Prefer command/query separation in a lightweight way.
- Keep handlers focused on a single use case.
- Depend on abstractions, not infrastructure implementations.

## Infrastructure rules

- Infrastructure implements persistence and integrations only.
- Do not move business decisions into repositories or adapters.
- Prefer EF Core configuration via Fluent API, not data annotations in domain classes.

## API rules

- Keep endpoints and SignalR hubs thin.
- Delegate orchestration to the Application layer.
- Never expose private game information through public contracts accidentally.

## WPF rules

- Use MVVM with CommunityToolkit.Mvvm.
- Prefer bindings and commands over code-behind.
- Keep code-behind minimal and UI-only.
- Use pure WPF with a custom style layer.
- Put reusable styles into ResourceDictionaries.

## Code quality rules

- Prefer clear names over abbreviations in code, except for the BOTC solution name.
- Avoid god classes, generic helpers, and manager-style classes.
- Prefer small methods with a single clear responsibility.
- Do not introduce unnecessary frameworks or abstractions.
- When proposing code, preserve the current architectural boundaries.

## Preferred working style

- Favor incremental vertical slices.
- Start with simple implementations before introducing more complexity.
- Prefer practical, maintainable solutions over over-engineered patterns.

## Decision policy

When a task has multiple valid architectural or implementation options, do not immediately generate code.

Instead:

1. Explain the possible options briefly.
2. Describe the trade-offs.
3. Ask the developer which direction should be chosen.
4. Wait for confirmation before generating code.

Examples of situations that require confirmation:

- introducing a new architectural pattern
- choosing between multiple persistence strategies
- creating new services or abstractions
- deciding where logic should live (Domain, Application, Infrastructure)
- introducing new dependencies or NuGet packages
- adding caching or background processing
- introducing complex patterns such as event sourcing or messaging

Default behavior:

If uncertain about architectural placement or design decisions, ask first.

## Type safety rules

Avoid using weak or untyped constructs.

- Do not use "any"-style patterns.
- Prefer strong typing and explicit domain models.
- Use dedicated types and value objects instead of primitive types when they represent domain concepts.
- Avoid `Dictionary<string, object>` unless there is a clear and justified need.

If a type is unclear or multiple options exist, ask for clarification instead of using a generic or weakly typed solution.

## Dependency introduction policy

Do not introduce new NuGet packages, frameworks, or external libraries without asking first.

Instead:

1. Explain why a new dependency might help.
2. List the alternatives using the current stack.
3. Ask for confirmation before adding the dependency.

## Class design rules

Avoid god classes and vague abstractions.

- Do not create classes named Manager, Helper, Utility, or Service unless the responsibility is explicit and justified.
- Prefer small, focused classes with one clear purpose.
- Use explicit names that reflect the business or technical responsibility.

## State management rules

Avoid static mutable state.

- Do not store application or domain state in static fields.
- Prefer explicit state ownership through domain models, application services, or scoped dependencies.
- If shared state is needed, explain the options and ask before implementing it.

## State management rules

Avoid static mutable state.

- Do not store application or domain state in static fields.
- Prefer explicit state ownership through domain models, application services, or scoped dependencies.
- If shared state is needed, explain the options and ask before implementing it.

## Explicit constants and types

Avoid magic strings and magic numbers.

- Prefer explicit constants, enums, or value objects.
- Do not hardcode protocol values, statuses, or domain concepts directly in methods.
- Use named types for important concepts.

## Refactoring rules

When refactoring existing code:

- preserve architectural boundaries
- do not silently move business logic across layers
- explain structural changes before applying them
- prefer incremental refactoring over large rewrites unless explicitly requested

## Simplicity rule

Prefer the simplest solution that fits the current stage of the project.

- Do not introduce advanced patterns prematurely.
- Avoid over-engineering.
- Prefer straightforward implementations before adding abstractions.
- If a more complex design is proposed, explain why it is needed and ask first.

## Async rules

Use async APIs where appropriate, especially for I/O operations.

- Do not fake async with unnecessary Task.Run.
- Do not block on async code with .Result or .Wait().
- Keep async flows explicit and readable.

## Mapping rules

Keep mapping responsibilities explicit.

- Do not return Domain entities directly from API contracts.
- Do not mix persistence models, transport models, and domain models.
- Keep conversions clear and localized.

## Comment rules

Do not add redundant comments that restate the code.

- Prefer self-explanatory naming.
- Add comments only when explaining intent, trade-offs, or non-obvious decisions.

## Interaction style

Prefer asking clarification questions instead of guessing developer intent.