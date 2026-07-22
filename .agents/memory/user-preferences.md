---
type: user_preferences
created: 2026-07-21
updated: 2026-07-22
---

# User Preferences

## Workflow & Execution Rules
- **DO NOT auto-run `dotnet build`**: The AI must NOT automatically execute `dotnet build` or compilation commands after code changes unless explicitly requested by the user.
- **DO NOT auto-commit or push Git**: The AI must NOT automatically execute `git add`, `git commit`, or `git push` after completing code changes under any circumstances unless explicitly requested by the user.
- **DO NOT auto-run dev server / app**: The AI must NOT automatically run `dotnet run`, dev servers, or start the web app in background after building code unless explicitly requested by the user.
