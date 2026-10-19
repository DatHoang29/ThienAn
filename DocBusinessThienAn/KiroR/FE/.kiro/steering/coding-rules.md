# Coding Rules & Conventions

## View / Page Structure

### Feature Folder Layout
```
views/{module}/{featureName}/
├── index.vue                    # Main page (list/grid view)
└── component/
    ├── edit{Feature}.vue        # Add/Edit dialog component
    ├── detail{Feature}.vue      # Detail view (optional)
    └── {otherComponent}.vue     # Other sub-components
```

### Module Folders
- `views/tms/` — Traffic Management
- `views/vms/` — Variable Message Sign
- `views/fms/` — Fleet Management
- `views/system/` — System administration
- `views/bigScreen/` — Full-screen dashboards
- `views/login/` — Authentication

Feature folders use **camelCase**: `dutySchedule`, `equipmentType`, `trafficData`

## Component Conventions

### Script Setup Pattern
```vue
<script lang="ts" setup name="tmsEquip">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentApi } from '/@/api-services/tms';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
</script>
```

### Component Name Attribute
- Use `name` in `<script setup>` via `vite-plugin-vue-setup-extend`
- Convention: `{module}{Feature}` in camelCase (e.g., `tmsEquip`, `tmsEditEquip`, `vmsImage`)

### Lazy Component Loading
```typescript
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
```

### Props & Emits
```typescript
// Props with TypeScript interface
interface Props {
    operateType: OperateType;
    title: string;
}
const props = defineProps<Props>();

// Props with defaults
const props = withDefaults(defineProps<Props>(), {
    showDelete: false,
    showAdd: true,
});

// Emits with typed interface
interface Emits {
    (e: 'handleQuery'): void;
    (e: 'add'): void;
}
const emit = defineEmits<Emits>();

// Expose methods to parent
defineExpose({ openDialog });
```

### State Pattern
```typescript
const state = reactive({
    isShowDialog: false,
    ruleForm: {} as UpdateEquipmentInput,
    queryParams: { ... },
    loading: false,
    typeList: [] as any,
});
```

## API Usage

### Auto-Generated Clients (DO NOT EDIT, DO NOT CREATE)
- Located in `src/api-services/{module}/` — generated from Swagger/OpenAPI
- Regenerate with: `pnpm build-api` (runs `api_build/build.bat`)
- Each module exports API classes and model types
- **IMPORTANT**: If an API client or model type does not exist yet, DO NOT create or modify files in `api-services/`. Instead, notify the user that the API has not been generated yet and wait for manual regeneration before proceeding with implementation.

### Calling APIs
```typescript
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import { PageEquipmentInput } from '/@/api-services/tms';

// GET request
const res = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();

// POST request
await getAPI(TmsEquipmentApi).apiTmsTmsequipmentUpdatePost(state.ruleForm);

// Paginated query
const params = { page: 1, pageSize: 50, field: 'createTime', order: 'desc' } as PageEquipmentInput;
await getAPI(TmsEquipmentApi).apiTmsTmsequipmentPagePost(params);
```

### API Method Naming (auto-generated)
- Pattern: `api{Module}{Feature}{Action}{Method}` 
- Examples: `apiTmsTmsequipmentPagePost`, `apiTmsTmsequipmentAddPost`, `apiTmsTmsequipmentDeletePost`

## i18n (Internationalization)

### Key Structure
| Category | Pattern | Example |
|----------|---------|---------|
| Entity fields (shared) | `lz.entity.base.{field}` | `lz.entity.base.code`, `lz.entity.base.name` |
| Entity fields (module) | `lz.entity.{module}.{field}` | `lz.entity.tms.equipmentType`, `lz.entity.vms.message` |
| Entity fields (feature) | `lz.entity.{module}{Feature}.{field}` | `lz.entity.tmsEquipment.vendor`, `lz.entity.tmsIncident.source` |
| Labels (shared) | `lz.label.base.{label}` | `lz.label.base.pleaseType`, `lz.label.base.basicInfo` |
| Labels (module) | `lz.label.{module}.{label}` | `lz.label.tms.zoneInfo` |
| Labels (feature) | `lz.label.{module}{Feature}.{label}` | `lz.label.tmsEquipment.locationTab` |
| Validation (shared) | `lz.validation.base.{rule}` | `lz.validation.base.required`, `lz.validation.base.maxLength` |
| Messages (shared) | `lz.message.base.{msg}` | `lz.message.base.saveSuccess`, `lz.message.base.confirmDelete` |
| Buttons (shared) | `lz.button.base.{action}` | `lz.button.base.confirm`, `lz.button.base.cancel` |
| Config params | `lz.param.{typeCode}.{code}` | `lz.param.status_type.1` |

### Scope Rules
1. **`base`** — Nội dung dùng chung cho TẤT CẢ module (entity, label, validation, message, button). Nếu key đã có trong `base`, dùng trực tiếp, KHÔNG tạo lại.
2. **`{module}`** (e.g., `tms`, `vms`, `fms`) — Nội dung dùng chung trong toàn module (nhiều feature cùng dùng).
3. **`{module}{Feature}`** (e.g., `tmsEquipment`, `tmsIncident`, `vmsImage`) — Nội dung chỉ riêng cho 1 chức năng cụ thể.

### Usage in Templates
```vue
<!-- Simple translation -->
{{ $t('lz.entity.base.code') }}

<!-- With parameters -->
{{ $t('lz.label.base.pleaseType', {input: $t('lz.entity.base.code')}) }}
{{ $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.name')}) }}
{{ $t('lz.message.base.confirmDelete', { name: row.name }) }}

<!-- Dialog titles -->
$t('lz.label.base.titleAdd', {name: $t('lz.entity.tms.equipment')})
$t('lz.label.base.titleEdit', {name: $t('lz.entity.tms.equipment')})
```

### Usage in Script
```typescript
const { t: $t } = useI18n();
ElMessage.success($t('lz.message.base.saveSuccess'));
```

## VxeTable Grid Pattern

### Standard Grid Setup
```typescript
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

const xGrid = ref<VxeGridInstance>();

const options = useVxeTable<PageEquipmentOutput>(
    {
        id: 'tmsEquip',          // Unique ID for column state persistence
        name: 'Equipment',       // Export filename prefix
        columns: [
            { type: 'checkbox', fixed: 'left', width: 45 },
            { field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, sortable: true },
            { field: 'status', title: $t('lz.entity.base.status'), width: 110, slots: { default: 'row_status' } },
            { title: $t('lz.label.base.action'), fixed: 'right', width: 100, slots: { default: 'row_buttons' } },
        ],
    },
    {
        proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
        sortConfig: { defaultSort: { field: 'createTime', order: 'desc' } },
        pagerConfig: { pageSize: 50 },
    }
);
```

### Proxy Response Mapping
The `useVxeTable` hook maps API responses:
- Paginated: `data.result.records` (items) + `data.result.total` (count)
- Non-paginated: `data.result` (full list)

## Stores (Pinia)

### Style
Use **Options API** style (not Setup API):
```typescript
export const useBaseConfig = defineStore('baseConfig', {
    state: () => ({ ... }),
    actions: { ... },
});
```

### Key Stores
- `themeConfig.ts` — UI theme, layout, language
- `userInfo.ts` — Auth, user details, permissions
- `baseConfig.ts` — Config data (enum lookups via `getConfigDataByCode()`)
- `routesList.ts` — Dynamic routes from backend

### Config Data Lookup
```typescript
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

// Get list of options (for el-select, el-radio-group)
getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentState)

// Get single item by code (for display/formatter)
getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, row.directionId)
```

### Config Usage Patterns

**In templates (el-select options):**
```vue
<el-select v-model="state.ruleForm.status">
    <el-option 
        v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" 
        :key="item.code" 
        :label="item.name" 
        :value="parseInt(item.code)" 
    />
</el-select>
```

**In VxeTable column formatter:**
```typescript
{ 
    field: 'directionId', 
    title: $t('lz.entity.tms.direction'),
    formatter: ({ cellValue }: any) => getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, cellValue)?.name 
}
```

**In template slots (with TagInfo):**
```vue
<template #row_status="{ row }">
    <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
</template>
```

### Rules
1. **Always use `BaseConfigTypeEnum`** — Never hardcode config type strings like `'status_type'` directly
2. **Use `getConfigDataByCode()`** for lists (dropdowns, radio groups)
3. **Use `getConfigItemByCode()`** for single-item lookups (formatters, tags, display)
4. **Use `getOpConfigListByGroupCode()`** for operation configs (SysOpConfig)
5. **parseInt for numeric values** — Config `code` is always string; use `parseInt(item.code)` when binding to numeric model fields
6. **Add new enums** to `BaseConfigTypeEnum` when new config types are created on the backend

## Types & Enums

### Location
- `types/enums/baseEnum.ts` — All enums (`BaseConfigTypeEnum`, `StatusTypeEnum`, `DateTimeFormatEnum`, etc.)
- `types/constants/operate.ts` — `OperateType = 'add' | 'edit' | 'copy' | 'view' | 'other'`
- `types/constants/natsEvent.ts` — NATS event constants

### Enum Usage
```typescript
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums/baseEnum';
import { OperateType } from '/@/types/constants/operate';
```

## Real-time Events (NATS/mittBus)

### Pattern
```typescript
import mittBus from '/@/utils/mitt';
import { NatsEvent } from '/@/types/constants/natsEvent';

onMounted(() => {
    mittBus.on(NatsEvent.StatusFms as any, handleNatsDeviceStatus);
});

onUnmounted(() => {
    mittBus.off(NatsEvent.StatusFms as any, handleNatsDeviceStatus);
});
```

## CRUD Page Pattern (Standard)

### List Page (`index.vue`)
1. Search form with `el-card` + `el-form` + `el-row`/`el-col`
2. `vxe-grid` with toolbar (add/delete/refresh/custom-column)
3. Action buttons per row (copy/edit/delete) with `v-auth` directive
4. `EditComponent ref` for dialog
5. `handleQuery`, `resetQuery`, `handleAdd`, `handleEdit`, `handleDelete`, `handleBatchDelete`

### Edit Dialog (`component/edit{Feature}.vue`)
1. `el-dialog` with `v-model` visibility
2. `el-form` with validation rules (i18n messages)
3. `openDialog(row)` exposed to parent
4. `submit()` dispatches add/update via `getAPI(...)`
5. Emits `handleQuery` to refresh parent grid

### Standard Operations
```typescript
// Add
state.operateType = 'add';
editRef.value?.openDialog({ status: 1, orderNo: 100 });

// Edit
state.operateType = 'edit';
editRef.value?.openDialog(row);

// Delete with confirmation
ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), ...)
    .then(async () => {
        await getAPI(Api).apiXxxDeletePost({ id: row.id });
        handleQuery();
        ElMessage.success($t('lz.message.base.deleteSuccess'));
    });
```

## Storage

### Utilities
```typescript
import { Local, Session } from '/@/utils/storage';

// Permanent (localStorage)
Local.set('key', value);
Local.get('key');

// Temporary (sessionStorage), token uses cookies
Session.set('token', tokenValue);
Session.get('token');
```

## Path Alias

`/@/` maps to `src/src/`

```typescript
import { getAPI } from '/@/utils/axios-utils';
import { Session } from '/@/utils/storage';
import { TmsEquipmentApi } from '/@/api-services/tms';
```

## Authorization

### Route-level
Routes include `roles: ['admin', 'common']` in meta.

### Button-level
```vue
<el-button v-auth="'tmsEquip:add'" @click="handleAdd">Add</el-button>
<el-button v-auth="'tmsEquip:update'" @click="handleEdit(row)">Edit</el-button>
<el-button v-auth="'tmsEquip:delete'" @click="handleDelete(row)">Delete</el-button>
```

Pattern: `{featureId}:{action}` (e.g., `tmsEquip:page`, `tmsEquip:add`, `tmsEquip:update`, `tmsEquip:delete`)

## CSS/SCSS Rules

- Use Element Plus utility classes: `w100`, `mb5`, `mb20`
- Responsive via `el-col` breakpoints: `:xs="24" :sm="12" :md="8" :lg="6" :xl="6"`
- SCSS comments: Use `/* */` only (not `//` — causes Vite build failure)
- Avoid double semicolons (`;;`) in SCSS
- Scoped styles preferred for page-specific styling

### Color Convention
Ưu tiên sử dụng các CSS variables của Element Plus thay vì hardcode màu:

| Semantic | CSS Variable | Use Case |
|----------|-------------|----------|
| Primary | `var(--el-color-primary)` | Action chính, link, highlight |
| Success | `var(--el-color-success)` | Trạng thái hoạt động, thành công |
| Warning | `var(--el-color-warning)` | Cảnh báo, chờ xử lý |
| Danger | `var(--el-color-danger)` | Lỗi, xóa, ngừng hoạt động |
| Info | `var(--el-color-info)` | Thông tin phụ, trạng thái trung tính |

Các biến thể sáng/tối:
- `var(--el-color-primary-light-3)` đến `var(--el-color-primary-light-9)` — nhạt dần
- `var(--el-color-primary-dark-2)` — đậm hơn

**Rules:**
1. **Không hardcode hex/rgb** — Dùng `var(--el-color-*)` để tương thích dark/light theme
2. **Element component type** — Dùng prop `type="primary|success|warning|danger|info"` cho `el-button`, `el-tag`, `el-alert`, etc.
3. **Text color** — Dùng `var(--el-text-color-primary)`, `var(--el-text-color-regular)`, `var(--el-text-color-secondary)`
4. **Background** — Dùng `var(--el-bg-color)`, `var(--el-fill-color-light)` thay vì `#fff`, `#f5f5f5`
5. **Border** — Dùng `var(--el-border-color)`, `var(--el-border-color-light)`

```vue
<!-- Correct -->
<el-button type="primary">Save</el-button>
<el-tag type="success">Active</el-tag>
<span :style="{ color: 'var(--el-color-danger)' }">Error</span>

<!-- Avoid -->
<el-button style="background: #1578a3">Save</el-button>
<span style="color: red">Error</span>
```
