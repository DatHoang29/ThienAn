## Brief overview
Guidelines for organizing the codebase using a **Feature‑Based Architecture**. The structure separates feature‑specific code from shared utilities and core configuration, enforcing clear boundaries and a predictable public API per feature.

## Feature organization
- **Location**: All code belonging to a single feature lives under `src/features/<feature-name>/`.
- **Public API**: Each feature must expose an `index.ts` that re‑exports its public symbols (components, hooks, services, store, types). Other parts of the app should import from this entry point only.
  ```ts
  // Example
  import { MyComponent, useMyHook } from '@/features/chat';
  ```
- **Self‑contained**: Inside a feature folder, keep its own:
  - `components/` – UI components used only by the feature.
  - `hooks/` – custom React hooks scoped to the feature.
  - `services/` – API calls or business‑logic helpers.
  - `store/` – Zustand/Zustand‑style slices or Redux sub‑reducers.
  - `types/` – TypeScript types/interfaces used by the feature.
- **No deep internal imports**: Features must not import **internal files** of another feature directly. Use the other feature’s public `index.ts` if sharing is needed.

## Shared code
- **Location**: `src/shared/` holds utilities, helpers, UI components, constants, and types that are truly cross‑feature.
- **Export pattern**: Provide an `index.ts` that aggregates shared exports.
- **Import rule**: Only import from `src/shared` when the code is genuinely reusable across multiple features.

## Core configuration
- **Location**: `src/core/` contains application‑wide setup that is not feature‑specific:
  - API client (e.g., Axios instance)
  - Router/Navigation configuration
  - Global store initialization
  - Theme, localization, and other global providers
- **Singleton pattern**: Export a single instance from each core module and import it where needed.
- **Do not place feature logic** in `src/core`; keep it strictly for infrastructure concerns.

## Import guidelines
- Use absolute imports with the `@` alias (configured in `tsconfig.json`):
  - `@/features/<feature-name>` for feature public APIs.
  - `@/shared` for shared utilities.
  - `@/core` for core services.
- Avoid relative imports that traverse multiple directories (`../../../`) as they obscure the module’s intended layer.

## Naming conventions
- **Folders**: kebab‑case (e.g., `chat`, `user-profile`).
- **Files**: 
  - Components – `MyComponent.tsx`
  - Hooks – `useMyHook.ts`
  - Services – `my-service.ts`
  - Types – `my-types.ts`
  - Index – `index.ts`
- **Exports**: Use `export const` or `export function` for named exports; default exports are discouraged to keep the public API explicit.

## Testing placement
- Feature tests reside alongside the feature or in a parallel `__tests__/` folder mirroring the feature structure.
- Shared utilities have their own test files under `src/shared/__tests__/`.

## Trigger cases
- Adding a new screen: create `src/features/<new-screen>/` with its own `components`, `hooks`, etc., and expose it via `index.ts`.
- Refactoring a component used by multiple features: move it to `src/shared/components/` and update imports to `@/shared`.
- Introducing a new global API endpoint: add the call in `src/core/api-client.ts` and expose it through a core service.

## Benefits
- Improves code discoverability and modularity.
- Reduces coupling between unrelated parts of the app.
- Simplifies testing and future feature extraction or reuse.