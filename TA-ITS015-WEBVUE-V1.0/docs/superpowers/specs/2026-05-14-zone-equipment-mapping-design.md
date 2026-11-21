# ZoneEquipment Mapping Feature Design

**Date:** 2026-05-14
**Status:** Draft
**Projects:** TA-ITS015-WEBAPI-V1.0 + TA-ITS015-WEBVUE-V1.0

## Overview

Manually and automatically mapping Equipment to Zones using the `TmsZoneEquipment` table. The mapping is based on km+met position and Lane/Direction matching. Users can auto-map, review, adjust, and confirm mappings.

## Context

- `TmsZone` has `fromKmNumber/fromMetNumber` and `toKmNumber/toMetNumber` defining a zone's spatial range
- `TmsEquipment` has `kmNumber/metNumber` defining its position
- `TmsZoneEquipment` entity exists (ZoneId, EquipmentId, Type, Status, LaneId, Remark) but has no API/UI yet
- Position calculation pattern: `km * 1000 + met` (supports bidirectional roads)
- Both projects follow consistent patterns: Wolverine CQRS (BE), vxe-grid + auto-generated APIs (FE)

## Architecture

**Approach:** Integrated into Zone Management (tab-based)
- Add a second tab "Thiết bị" to the existing Zone management page
- When a zone is selected, tab 2 shows the equipment mapping grid for that zone
- Reuses the existing zone tree grid instead of a separate panel

## Backend (WEBAPI)

### File Structure

```
Controllers/ZoneEquipment/
├── TmsZoneEquipmentController.cs
├── Commands/
│   ├── ZoneEquipmentCommandHandler.cs
│   └── Dto/
│       ├── AddZoneEquipmentInput.cs
│       ├── UpdateZoneEquipmentInput.cs
│       ├── DeleteZoneEquipmentInput.cs
│       ├── AutoMapZoneEquipmentInput.cs
│       ├── AutoMapAllZoneEquipmentInput.cs
│       └── ConfirmZoneEquipmentMappingInput.cs
├── Queries/
│   ├── ZoneEquipmentQueryHandler.cs
│   └── Dto/
│       ├── ZoneEquipmentInput.cs
│       ├── ZoneEquipmentOutput.cs
│       ├── AutoMapPreviewOutput.cs
│       └── PageZoneEquipmentInput.cs
```

### API Endpoints

| Method | Route | Handler Input | Description |
|--------|-------|---------------|-------------|
| GET | `/api/tms/TmsZoneEquipment/GetList` | `ZoneEquipmentInput` | List mappings by ZoneId filter |
| GET | `/api/tms/TmsZoneEquipment/Page` | `PageZoneEquipmentInput` | Paged mapping list |
| POST | `/api/tms/TmsZoneEquipment/Add` | `AddZoneEquipmentInput` | Add single mapping |
| POST | `/api/tms/TmsZoneEquipment/Update` | `UpdateZoneEquipmentInput` | Update single mapping |
| POST | `/api/tms/TmsZoneEquipment/Delete` | `DeleteZoneEquipmentInput` | Delete single mapping |
| POST | `/api/tms/TmsZoneEquipment/AutoMap` | `AutoMapZoneEquipmentInput` | Auto-map preview for one zone |
| POST | `/api/tms/TmsZoneEquipment/AutoMapAll` | `AutoMapAllZoneEquipmentInput` | Auto-map preview for all zones |
| POST | `/api/tms/TmsZoneEquipment/ConfirmMapping` | `ConfirmZoneEquipmentMappingInput` | Confirm and save mapping list |

### DTO Definitions

```csharp
// Query DTOs
public class ZoneEquipmentInput
{
    public string? ZoneId { get; set; }
    public string? EquipmentId { get; set; }
    public string? Type { get; set; }
    public StatusEnum? Status { get; set; }
    public string? LaneId { get; set; }
}

public class PageZoneEquipmentInput : ZoneEquipmentInput
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public class ZoneEquipmentOutput : TmsZoneEquipment
{
    public string? ZoneName { get; set; }
    public string? EquipmentName { get; set; }
    public string? EquipmentCode { get; set; }
    public int? EquipmentKmNumber { get; set; }
    public int? EquipmentMetNumber { get; set; }
    public string? EquipmentTypeName { get; set; }
}

public class AutoMapPreviewOutput
{
    public string? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public string? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public string? EquipmentCode { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public string? LaneId { get; set; }
    public string? MatchReason { get; set; } // "Position + Lane match" etc.
}

// Command DTOs
public class AddZoneEquipmentInput : TmsZoneEquipment { }
public class UpdateZoneEquipmentInput : AddZoneEquipmentInput { }
public class DeleteZoneEquipmentInput : BaseIdInput { }

public class AutoMapZoneEquipmentInput
{
    public string? ZoneId { get; set; }
}

public class AutoMapAllZoneEquipmentInput
{
    public bool ClearExistingMappings { get; set; } = false;
}

public class ConfirmZoneEquipmentMappingInput
{
    public List<AddZoneEquipmentInput> Mappings { get; set; } = new();
    public string? ZoneId { get; set; } // optional: clear existing for this zone first
    public bool ClearExistingMappings { get; set; } = false;
}
```

### Auto-map Algorithm

```
AutoMap(zoneId):
  1. Load zone by zoneId (get fromKm, fromMet, toKm, toMet, laneId)
  2. Calculate: zoneFromPos = fromKm * 1000 + fromMet
                zoneToPos = toKm * 1000 + toMet
                isGreater = zoneToPos >= zoneFromPos
  3. Load all equipment with status = Enabled
  4. Load existing mappings for this zone (to exclude already mapped)
  5. For each equipment NOT already mapped:
     equipPos = equip.KmNumber * 1000 + equip.MetNumber

     positionMatch =
       isGreater ? (equipPos >= zoneFromPos && equipPos < zoneToPos)
                 : (equipPos <= zoneFromPos && equipPos > zoneToPos)

     laneMatch =
       zone.LaneId is null OR
       equip.LaneId is null OR
       equip.LaneId == zone.LaneId

     if (positionMatch && laneMatch):
       add to preview list with matchReason

  6. Return preview list (NOT saved to DB)

AutoMapAll():
  1. Load all zones
  2. For each zone, run AutoMap logic
  3. Return aggregated preview list grouped by zone
```

### Confirm Mapping Logic

```
ConfirmMapping(input):
  1. If input.ClearExistingMappings and input.ZoneId provided:
     soft-delete all existing TmsZoneEquipment where ZoneId = input.ZoneId
  2. Insert all items from input.Mappings using InsertWithDiffLogAsync
  3. Clear cache: RemoveByPrefixKey(CacheConst.Tms.TmsZoneEquipment)
```

### Controller Pattern

Follow `TmsEquipmentController` pattern:
- Inherit `BaseController`
- `[ApiDescriptionSettings(GroupName, Order = 470)]`
- All operations via `MessBus.InvokeAsync<T>(input)`
- Separate Query/Command handlers implementing Wolverine pattern

### Cache Key

Add to `CacheConst.Tms`:
```csharp
public const string TmsZoneEquipment = "tms_zone_equipment";
```

## Frontend (WEBVUE)

### Integration Strategy

Instead of a separate page, the mapping feature is **integrated into the existing Zone management page** (`views/tms/zone/index.vue`) via a tab panel. This avoids duplicating the zone tree, reduces navigation, and provides a more seamless UX.

### File Structure

**Modified files:**
```
views/tms/zone/index.vue          ← Add tab panel wrapping existing tree + new mapping tab
views/tms/zone/component/
├── editZone.vue                  ← Existing, no changes
├── equipmentMappingGrid.vue      ← NEW: Mapping grid for selected zone
├── addMappingDialog.vue          ← NEW: Dialog to manually add mapping
├── autoMapPreviewDialog.vue      ← NEW: Preview auto-map results before confirm
```

**No new top-level view needed.** All components live under the existing `zone/component/` folder.

### Modified Layout (zone/index.vue)

The existing zone/index.vue is restructured to add tabs below the toolbar:

```
┌─────────────────────────────────────────────────────────────┐
│  Toolbar: [Thêm Zone] [Mở rộng] [Thu gọn] [Xóa]           │
│           [Auto-map Zone này] [Auto-map Tất cả]             │
├─────────────────────────────────────────────────────────────┤
│  Tabs: [Danh sách Zone] | [Thiết bị (mapping)] ◄── new tab │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Tab 1: "Danh sách Zone"                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Existing zone tree grid (unchanged)                 │   │
│  │  - Search, filter, CRUD operations                   │   │
│  │  - On row click: select zone, switch to Tab 2       │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Tab 2: "Thiết bị" (shown when zone selected)              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Zone info bar: "Zone A1 | Km 12+000 → 15+500"       │   │
│  │ [Thêm thiết bị] [Xóa đã chọn]                        │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │ vxe-grid: mapped equipment list                      │   │
│  │ - Checkbox | Equipment Code | Name | Km+Met         │   │
│  │ - Type | Lane | Status | Remark | Actions           │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │ Pagination                                           │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Components

#### equipmentMappingGrid.vue
- `vxe-grid` with proxy config, pagination
- Load data via `TmsZoneEquipmentApi.apiTmsTmsZoneEquipmentGetlistGet({ zoneId })`
- Checkbox selection for batch operations
- Columns: Equipment Code, Name, Km+Met, Type, Lane, Status, Remark
- Row actions: Edit (inline or dialog), Delete
- Batch: Delete selected
- Props: `zoneId`, `zoneData` (for displaying zone info bar)

#### addMappingDialog.vue
- Dialog with equipment selector (filterable select or grid picker)
- Load available equipment via `TmsEquipmentApi.apiTmsTmsEquipmentGetlistGet()`
- Exclude equipment already mapped to selected zone
- Fields: Equipment (select), Type, LaneId, Status, Remark
- Submit via `TmsZoneEquipmentApi.apiTmsTmsZoneEquipmentAddPost()`

#### autoMapPreviewDialog.vue
- Full-screen or large dialog
- Display preview results from AutoMap API
- Grouped by zone (when AutoMapAll)
- Checkbox per mapping row (user can uncheck unwanted mappings)
- "Match Reason" column showing why each was matched
- Edit Remark per row
- Confirm button → calls ConfirmMapping API
- Cancel → discard preview

### API Service (auto-generated from Swagger after BE is built)

```typescript
// api-services/tms/apis/tms-zone-equipment-api.ts
// Will be generated automatically when Swagger is updated
```

### State Management

No dedicated Pinia store needed. Data fetched directly via API services, matching the pattern used in zone and equipment pages.

### Config Enums Used

- `BaseConfigTypeEnum.TmsLaneType` - Lane/Direction dropdown
- `BaseConfigTypeEnum.SysStatusType` - Status dropdown
- `BaseConfigTypeEnum.TmsZoneType` - Zone type display

## Data Flow

### Manual Add Flow
```
Tab 1: Click zone row → auto-switch to Tab 2
Tab 2: Click "Thêm thiết bị" → addMappingDialog opens
→ Select equipment → Fill fields → Submit
→ POST /Add → Refresh mapping grid
```

### Auto-map Single Zone Flow
```
Tab 1: Click zone row → auto-switch to Tab 2
Tab 2: Click "Auto-map Zone này"
→ POST /AutoMap { zoneId } → Returns preview list
→ autoMapPreviewDialog opens with results
→ User reviews, unchecks unwanted, edits remarks
→ Click Confirm → POST /ConfirmMapping { mappings, zoneId, clearExisting: true }
→ Refresh mapping grid
```

### Auto-map All Zones Flow
```
Click "Auto-map Tất cả" (toolbar, visible on any tab)
→ POST /AutoMapAll {} → Returns preview list grouped by zone
→ autoMapPreviewDialog opens with results (grouped)
→ User reviews, unchecks unwanted, edits remarks
→ Click Confirm → POST /ConfirmMapping { mappings, clearExisting: true }
→ Refresh mapping grid (auto-switch to Tab 2 if needed)
```

## Implementation Order

1. **Backend DTOs** - Define all Input/Output DTOs
2. **Backend Query Handler** - GetList, Page queries
3. **Backend Command Handler** - Add, Update, Delete, ConfirmMapping
4. **Backend Auto-map Logic** - AutoMap, AutoMapAll
5. **Backend Controller** - Wire up endpoints
6. **Build & Swagger** - Generate API, verify endpoints
7. **Frontend API Services** - Auto-generated from Swagger
8. **Frontend equipmentMappingGrid** - Mapping grid component (in zone/component/)
9. **Frontend addMappingDialog** - Manual add dialog (in zone/component/)
10. **Frontend autoMapPreviewDialog** - Preview + confirm dialog (in zone/component/)
11. **Frontend zone/index.vue** - Modify to add tab panel, integrate mapping components
12. **Testing** - End-to-end testing of all flows
