---
applyTo: "src/BOTC.Presentation.Api/**"
---

This area contains the ASP.NET Core API and SignalR realtime host.

Responsibilities of this layer:

- Define HTTP endpoints and SignalR hubs.
- Accept and validate incoming requests.
- Delegate use-case execution to the Application layer.
- Return clear responses to clients.

Architecture rules:

- Keep endpoints and hubs thin.
- Do not place business rules in controllers, endpoints, filters, or hubs.
- All business logic must live in BOTC.Domain or BOTC.Application.
- Prefer feature-based endpoint organization instead of large controllers.
- Avoid introducing infrastructure dependencies directly in endpoints.
- Be careful not to leak private player or storyteller information.

SignalR rules:

- Hubs should coordinate realtime communication only.
- Do not implement game rules inside hubs.
- Delegate orchestration to Application services.

## Endpoint design policy

If an endpoint could be implemented using different architectural patterns (for example: controller, minimal endpoint, mediator pattern, or service-based endpoint), do not immediately generate code.

Instead:

1. List the possible approaches.
2. Explain the trade-offs briefly.
3. Ask which approach should be used.
4. Wait for confirmation before generating code.