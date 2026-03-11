# CasCap.Api.Azure - Copilot Instructions

## Code Style & Conventions

**Enforced by .editorconfig:**
- **Indentation:** 4 spaces (not tabs)
- **Line endings:** LF (Unix-style)
- **Insert final newline:** Yes
- **Naming:** PascalCase for types/methods, interfaces prefixed with `I`
- **Implicit usings:** Enabled
- **Nullable reference types:** Enabled
- **C# Language Version:** 14.0

**Suppressed warnings (Directory.Build.props):**
- IDE1006, IDE0079, IDE0042, CS0162, S125, NETSDK1233

**Key Conventions:**
- Use expression-bodied members for accessors and properties
- Prefer pattern matching over `is`/`as` with null checks
- Always use braces for code blocks (`csharp_prefer_braces = true`)
- Namespace declarations: block-scoped (not file-scoped)
- Using directives: Outside namespace

