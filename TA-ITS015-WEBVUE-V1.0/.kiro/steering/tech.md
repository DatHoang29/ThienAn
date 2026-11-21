# Technology Stack

## Build System

**Vite 5.x** - Primary build tool with TypeScript support

### Commands (run from `src/` directory)
```bash
pnpm install          # Install dependencies
pnpm dev              # Start dev server (port 8888 by default)
pnpm build            # Production build (8GB heap, terser minification)
pnpm lint-fix         # ESLint auto-fix for .js/.jsx/.vue files
```

## Core Framework

- **Vue 3.5** with Composition API (`<script setup>`)
- **TypeScript 6.x** with strict mode enabled
- **Pinia 3.x** for state management
- **Vue Router 5.x** for navigation

## UI Libraries

- **Element Plus 2.x** - Primary UI component library
- **PrimeVue 4.x** - Supplementary UI components
- **VxeTable 4.x** - Advanced data tables with Excel export
- **WangEditor 5.x** - Rich text editor

## Maps & Visualization

- **Leaflet 1.9** with Leaflet-Geoman for drawing
- **MapLibre GL 5.x** for vector maps
- **OpenLayers 10.x** for advanced GIS
- **ECharts 6.x** for charts (including 3D with echarts-gl)

## Real-time Communication

- **SignalR** (.NET WebSocket)
- **MQTT 5.x** for IoT messaging
- **NATS WebSocket** for streaming

## HTTP Client

- **Axios** with auto-generated TypeScript clients from Swagger/OpenAPI
- API clients in `src/src/api-services/` are auto-generated - do not edit manually

## Path Alias

`/@/` maps to `src/src/`

Example: `import { Session } from '/@/utils/storage'`

## Code Style

- **Prettier**: Tabs (width 2), print width 200, single quotes, trailing commas (es5)
- **ESLint**: vue-eslint-parser with @typescript-eslint plugin
- SCSS: Use `/* */` block comments, not `//` single-line (Vite requirement)
- Avoid double semicolons (`;;`) in SCSS - causes build failures

## Build Notes

- Terser drops console and debugger statements in production
- Node.js 16+ required (18+ recommended)
- pnpm is the preferred package manager
