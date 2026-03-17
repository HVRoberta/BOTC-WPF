---
applyTo: "src/BOTC.Infrastructure/**"
---

This layer contains persistence and external integrations for the BOTC project.

Responsibilities:

- Implement abstractions defined in the Application layer.
- Handle database access and persistence concerns.
- Integrate external services and technical infrastructure.
- Keep framework and provider-specific code isolated from the Domain layer.

Architecture rules:

- Do not place business rules here.
- Do not move domain decisions into repositories or adapters.
- Implement abstractions defined in the Application layer.
- Keep persistence and integration concerns separate from presentation concerns.
- Avoid introducing dependencies from Domain to Infrastructure.

Persistence rules:

- Keep EF Core mappings and persistence concerns isolated from the Domain model where possible.
- Prefer Fluent API configuration over attributes in domain classes.
- Keep repository implementations explicit and use-case friendly.
- Avoid generic repository patterns unless there is a clear benefit.
- Prefer clear persistence boundaries over leaking DbContext usage into higher layers.

Integration rules:

- Treat external systems as technical dependencies, not business rule owners.
- Keep adapters small and focused.
- Prefer explicit interfaces and clear mapping between transport, persistence, and domain models.

## Infrastructure design decisions

If persistence or integration can be implemented in multiple ways (for example: EF Core repository, direct DbContext usage, Dapper, in-memory implementation, separate persistence model, or direct domain mapping), do not immediately generate code.

Instead:

1. List the possible approaches.
2. Explain the trade-offs briefly.
3. Ask which approach should be used.
4. Wait for confirmation before generating code.

## Clean Architecture safety

If a proposed implementation would push business logic into Infrastructure, stop and suggest moving that logic to Domain or Application instead.