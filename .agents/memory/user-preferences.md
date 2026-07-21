---
type: user_preferences
created: 2026-07-21
updated: 2026-07-21
---

# User Preferences

## Workflow & Execution Rules
- **DO NOT auto-commit Git**: The AI must NOT automatically execute `git commit` after completing code changes unless explicitly requested by the user.
- **DO NOT auto-run dev server / app**: The AI must NOT automatically run `dotnet run`, dev servers, or start the web app in background after building code unless explicitly requested by the user. Only perform `dotnet build` to verify compilation.
