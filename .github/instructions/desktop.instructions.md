---
applyTo: "src/BOTC.Presentation.Desktop/**"
---

This area contains the WPF desktop client.

Architecture rules:

- Use MVVM with CommunityToolkit.Mvvm.
- Prefer bindings, commands, and view models over code-behind.
- Keep code-behind UI-only and minimal.
- Do not place business logic in views or view models.
- Prefer reusable UserControls, ResourceDictionaries, DataTemplates, and ControlTemplates.
- Keep styling in XAML and theme files where possible.
- Prefer clean, readable XAML over overly clever markup.

Dependency rules:

- The UI must not reference Infrastructure implementations directly.
- Networking and persistence must go through application-level services.
- ViewModels should remain testable and independent from UI frameworks where possible.

## UI design decisions

If a UI problem can be solved in multiple ways (for example: ViewModel logic, XAML triggers, behaviors, or services), do not immediately generate code.

Instead:

1. Explain the possible approaches.
2. Describe the trade-offs briefly.
3. Ask which approach should be used.
4. Wait for confirmation before generating code.