# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TAC Web Core (tacwebcore) - An ITS (Intelligent Transport System) web application built with Vue 3, Vite, TypeScript, Element Plus, and PrimeVue. The application is a traffic management platform with modules for TMS (Traffic Management System), VMS (Variable Message Sign), FMS (Fleet Management System), and big-screen map displays. Uses pnpm as the package manager.

All source code lives under the `src/` directory (the project root is one level up for git purposes). The actual Vue app source is at `src/src/`.

## Build & Development Commands

All commands must be run from the `src/` directory:

```bash
cd src
pnpm install          # Install dependencies
pnpm dev              # Start dev server (port from .env, default 8888)
pnpm build            # Production build (8GB heap, terser minification, drops console/debugger)
pnpm lint-fix         # ESLint fix for .js/.jsx/.vue files
```

There are no test commands configured. The `build-api` script runs a Windows batch file for API code generation.

## Architecture

### Routing (Backend-controlled)

Routes are fetched from the backend API (`SysMenuApi.apiSystemSysmenuLoginmenutreeGet`) at runtime. The backend returns a menu tree with `component` paths that are dynamically resolved against `src/views/**` via `import.meta.glob`. The route system is configured in `src/src/router/`:
- `backEnd.ts` - Backend route control (active, `isRequestRoutes: true`)
- `frontEnd.ts` - Frontend route control (unused)
- `route.ts` - Static routes (login pages, fullscreen big-screen) and dynamic route definitions
- `index.ts` - Router setup, guards, auth/token checks

Static fullscreen routes (like `/screen/trafficInfo`) are defined directly in `staticRoutes` in `route.ts`. Layout-wrapped routes are defined in `dynamicRoutes`.

### API Services (Swagger-generated)

API clients in `src/src/api-services/` are auto-generated from Swagger/OpenAPI specs using typescript-axios. Each domain module (system, tms, vms, fms, cfgSystem, report, etc.) has its own subdirectory with `apis/`, `models/`, `api.ts`, and `base.ts`. **Do not manually edit generated API files** - they should be regenerated via the `build-api` script.

The axios instance is configured in `src/src/utils/axios-utils.ts` with JWT token management (access + refresh tokens), request/response interceptors, and automatic token refresh. Use `getAPI(ApiClass)` to instantiate API clients.

### State Management (Pinia)

Stores in `src/src/stores/`:
- `themeConfig.ts` - Central UI configuration (layout, theme colors, i18n language, routing mode)
- `userInfo.ts` - User auth state, constants, and dictionaries
- `routesList.ts` - Dynamic route list from backend
- `its/` - ITS-specific stores (incident monitor, VMS, theme, map)

### Internationalization (i18n)

Vue I18n with Vietnamese (vi-VN, default) and English (en-US). Translation files at `src/src/i18n/lang/`. Language switching is handled in the router guard (`router/index.ts`). Terminology translations are loaded from the backend via `getTerminologyList()` in `src/src/utils/locale.ts`.

### Path Alias

`/@/` maps to `src/src/` (configured in both `vite.config.ts` and `tsconfig.json`). Import example: `import { Session } from '/@/utils/storage'`.

### Key Shared Components

- `src/src/components/leafletMap/` - Leaflet map component
- `src/src/components/table/` - VxeTable wrappers
- `src/src/components/editor/` - WangEditor integration
- `src/src/components/livecam/` - Camera/streaming player (ezuikit, JSMpeg)

### Environment Configuration

Environment variables are loaded from `src/.env` (shared), `src/.env.development`, and `src/.env.production`. At startup, Vite writes all env vars to `public/config.js` as `window.__env__`. The API base URL is `window.__env__.VITE_API_URL`. Upload requests are proxied to the API URL.

### Build Notes

- SCSS style blocks: Vite's CSS preprocessor requires `/* */` block comments, not `//` single-line comments
- Double semicolons (`;;`) in SCSS will cause build failures
- The build uses terser with `drop_console` and `drop_debugger` enabled
- CDN mode is available via `VITE_OPEN_CDN` (disabled by default)

### Code Style

Prettier is configured with tabs (width 2), print width 200, single quotes, trailing commas (es5). ESLint uses `vue-eslint-parser` with `@typescript-eslint` plugin.
