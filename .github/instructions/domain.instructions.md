---
applyTo: "src/BOTC.Domain/**"
---

This layer contains the core game rules and domain concepts of the BOTC project.

Responsibilities:

- Model the core business concepts of the game.
- Protect invariants and valid state transitions.
- Express business behavior through domain methods and value objects.
- Remain independent from presentation, persistence, and infrastructure concerns.

Architecture rules:

- Do not reference WPF, ASP.NET Core, EF Core, SignalR, or infrastructure concerns here.
- Keep the domain model framework-independent.
- Prefer rich domain models over anemic data containers.
- Keep invariants inside entities and value objects.
- Use explicit business language in APIs.
- Avoid generic utility classes.
- Prefer deterministic logic that is easy to unit test.
- Do not add serialization, persistence, or UI attributes here unless explicitly required.

DDD rules:

- Prefer behavior over setters and mutable data bags.
- Use value objects where they improve clarity and type safety.
- Protect aggregate boundaries.
- Keep domain logic inside domain types whenever possible.
- Do not move business invariants into the Application layer unless there is a strong reason.

## Domain design decisions

If a domain concept could be modeled in multiple valid ways (for example: entity vs value object, separate aggregate vs child entity, or domain service vs entity behavior), do not immediately generate code.

Instead:

1. List the possible approaches.
2. Explain the trade-offs briefly.
3. Ask which approach should be used.
4. Wait for confirmation before generating code.

## Domain safety

If logic could reasonably belong in the Domain layer instead of Application or Infrastructure, prefer the Domain solution and explain why.