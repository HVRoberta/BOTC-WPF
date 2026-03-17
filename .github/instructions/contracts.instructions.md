---
applyTo: "src/BOTC.Contracts/**"
---

This layer contains shared transport contracts between the client and server.

Responsibilities:

- Define request and response models.
- Define SignalR message contracts.
- Provide stable data structures for communication between components.

Design rules:

- Keep contracts simple, explicit, and serialization-friendly.
- Do not place business logic here.
- Do not reference Domain entities directly.
- Do not expose internal domain behavior through transport models.
- Prefer immutable or simple data structures where possible.

Compatibility rules:

- Contracts should remain stable once published.
- Avoid breaking changes when evolving request or response models.

## Design decisions

If multiple contract shapes are possible (for example: flattened DTO vs nested structure), explain the alternatives and ask before generating code.