---
applyTo: "tests/**"
---

This area contains automated tests for the BOTC solution.

Responsibilities:

- Verify domain rules and invariants.
- Validate application use-case orchestration.
- Ensure architectural boundaries are respected.

Testing principles:

- Write focused, readable tests.
- Follow the arrange–act–assert structure.
- Prefer testing behavior over implementation details.
- Avoid brittle tests coupled to internal refactoring details.
- Use explicit test names that describe the expected behavior.

Domain tests:

- Focus on business rules and invariants.
- Prefer simple tests without mocks when possible.
- Test state transitions and rule enforcement.

Application tests:

- Focus on use-case orchestration.
- Mock only external dependencies such as repositories or integrations.
- Verify interactions and outcomes rather than internal steps.

Architecture tests:

- Verify Clean Architecture dependency direction.
- Ensure Domain does not depend on Infrastructure or Presentation.
- Ensure Application does not depend on Presentation.

## Test design decisions

If multiple testing approaches are possible (for example: unit test vs integration test, mock vs fake implementation), 
explain the alternatives and ask before generating code.
