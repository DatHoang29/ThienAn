## Brief overview
Guidelines for breaking down large React Native component files into smaller, reusable sub‑components. This rule is inspired by the structure of `ContactPanelSheet.tsx` and aims to improve readability, maintainability, and testability across the AICC Mobile codebase.

## General principles
- **One responsibility per file**: Each file should contain a single UI component or a closely related group of components.
- **Depth limit**: Avoid nesting more than two levels of sub‑components inside a file. If a component contains its own sub‑components (e.g., `HistoryRow`, `HistoryRowDetail`, `avatarColor`), move them to separate files.
- **Folder organization**: Place sub‑components in a folder named after the parent component (e.g., `src/components/ContactPanelSheet/`) and name the file after the component (`HistoryRow.tsx`, `AvatarUtils.ts`).

## Naming conventions
- Component files: `PascalCase` matching the exported component name (e.g., `HistoryRow.tsx`).
- Utility files (functions, helpers): `camelCase` with a descriptive suffix (e.g., `avatarUtils.ts`).
- Export default the main component; named exports for pure helper functions.

## File structure example (based on `ContactPanelSheet.tsx`)
```
src/
└─ components/
   └─ ContactPanelSheet/
      ├─ index.tsx                // Main ContactPanelSheet component (imports sub‑components)
      ├─ HistoryRow.tsx           // <HistoryRow> component
      ├─ HistoryRowDetail.tsx     // <HistoryRowDetail> component
      ├─ AvatarUtils.ts           // avatarColor, initials helpers
      └─ styles.ts                // StyleSheet objects (optional, can be split further)
```

## Migration steps
1. **Identify sub‑components**: Look for functions that return JSX and are used only within the parent component (e.g., `HistoryRow`, `HistoryRowDetail`).
2. **Create a dedicated folder**: `mkdir -p src/components/ContactPanelSheet`.
3. **Move the component code**: Cut the sub‑component definition and paste it into a new file with the appropriate name.
4. **Adjust imports**: In `ContactPanelSheet/index.tsx`, import the moved components (`import HistoryRow from './HistoryRow';`).
5. **Export utilities**: If helper functions like `avatarColor` are used elsewhere, export them from `AvatarUtils.ts` and import where needed.
6. **Run lint/tests**: Ensure the project still passes ESLint and unit tests.

## Testing considerations
- Create unit tests for each new sub‑component in the existing `__tests__/unit/components/` folder.
- Keep test file names aligned with component names (e.g., `HistoryRow.test.tsx`).

## When NOT to split
- Tiny components (≤ 20 LOC) that are unlikely to be reused.
- Pure utility functions that are already in a shared utilities directory.

## Benefits
- Faster code navigation and IDE indexing.
- Clear separation of concerns.
- Easier to write focused tests for each UI piece.
- Reduces merge conflicts when multiple developers work on different parts of the same UI.

## Enforcement
- Code reviews must verify that new large components follow this modularization rule.
- Automated lint rule (optional) can flag component files exceeding 300 lines or containing more than two nested component definitions.