# Project Structure

## Repository Layout

```
src/
├── src/                      # Main Vue application source
│   ├── api-services/         # Auto-generated API clients (Swagger)
│   │   ├── system/           # System management APIs
│   │   ├── tms/              # Traffic Management APIs
│   │   ├── vms/              # Variable Message Sign APIs
│   │   ├── fms/              # Fleet Management APIs
│   │   ├── cfgSystem/        # Configuration APIs
│   │   └── report/           # Reporting APIs
│   ├── assets/               # Static assets (images, fonts)
│   ├── components/           # Shared Vue components
│   │   ├── leafletMap/       # Leaflet map components
│   │   ├── table/            # VxeTable wrappers
│   │   ├── editor/           # WangEditor integration
│   │   ├── livecam/          # Camera/streaming players
│   │   └── scEcharts/        # ECharts wrapper
│   ├── directive/            # Vue directives
│   ├── hooks/                # Vue composables
│   ├── i18n/                 # Internationalization (vi-VN, en-US)
│   ├── indexedDb/            # Dexie IndexedDB integration
│   ├── layout/               # App layout components
│   ├── router/               # Vue Router configuration
│   │   ├── backEnd.ts        # Backend-controlled routes (active)
│   │   ├── frontEnd.ts       # Frontend routes (unused)
│   │   └── route.ts          # Static route definitions
│   ├── stores/               # Pinia stores
│   │   ├── themeConfig.ts    # UI theme configuration
│   │   ├── userInfo.ts       # User auth & dictionaries
│   │   ├── routesList.ts     # Dynamic routes from backend
│   │   └── its/              # ITS-specific stores
│   ├── theme/                # SCSS theme files
│   ├── transporter/          # Data transport utilities
│   ├── types/                # TypeScript type definitions
│   ├── utils/                # Utility functions
│   │   ├── axios-utils.ts    # HTTP client with JWT handling
│   │   ├── storage.ts        # Local/Session storage helpers
│   │   └── locale.ts         # i18n helpers
│   ├── views/                # Feature modules
│   │   ├── bigScreen/        # Full-screen dashboards
│   │   ├── tms/              # Traffic Management views
│   │   ├── vms/              # Variable Message Sign views
│   │   ├── fms/              # Fleet Management views
│   │   ├── system/           # System administration
│   │   └── login/            # Authentication views
│   ├── App.vue               # Root component
│   └── main.ts               # Application entry
├── public/                   # Static files (config.js, uploads)
├── api_build/                # Swagger codegen scripts
├── .env                      # Shared environment variables
├── .env.development          # Development environment
└── .env.production           # Production environment
```

## Key Patterns

### Routing
- Routes fetched from backend via `SysMenuApi.apiSystemSysmenuLoginmenutreeGet`
- Backend returns menu tree with `component` paths resolved against `views/**`
- Static routes (login, full-screen) defined in `router/route.ts`

### API Usage
```typescript
import { getAPI } from '/@/utils/axios-utils';
import { TmsMapApi } from '/@/api-services/tms';

const response = await getAPI(TmsMapApi).apiTmsTmsmapListGet();
```

### State Management
- `themeConfig.ts` - Central UI configuration
- `userInfo.ts` - Auth state and dictionaries
- Feature-specific stores in `stores/its/`

### Component Organization
- Shared components in `src/components/`
- Feature-specific components alongside views
- Use `<script setup lang="ts">` for all Vue components
