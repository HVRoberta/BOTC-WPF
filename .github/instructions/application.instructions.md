---
applyTo: "src/BOTC.Application/**"
---

This layer contains application use cases and orchestration logic.

Responsibilities:

- Coordinate domain operations.
- Execute use cases.
- Interact with persistence through abstractions.
- Translate requests into domain actions.

Architecture rules:

- Depend only on BOTC.Domain and abstractions.
- Do not reference infrastructure implementations directly.
- Do not reference Presentation layers.
- Keep commands, queries, results, and interfaces explicit.
- Prefer one use case per handler or service.
- Keep orchestration logic here, not in controllers or UI.

Domain boundary rules:

- Core business rules belong in BOTC.Domain.
- Do not move domain invariants into application services.
- Application services should orchestrate domain behavior, not replace it.

Testability rules:

- Keep services deterministic and testable.
- Avoid static dependencies.
- Prefer dependency injection via interfaces.

## Design decisions

If a use case could be implemented in multiple ways (for example: command handler, service class, mediator pattern, or direct orchestration), do not immediately generate code.

Instead:

1. List the possible approaches.
2. Explain the trade-offs briefly.
3. Ask which approach should be used.
4. Wait for confirmation before generating code.