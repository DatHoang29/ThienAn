---
name: gortex-src-utils-35-dirs
description: "Work in the src/utils +35 dirs area — 753 symbols across 76 files (79% cohesion)"
---

# src/utils +35 dirs

753 symbols | 76 files | 79% cohesion

## When to Use

Use this skill when working on files in:
- ``
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/system/apis/sys-file-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/toll/models/ca-lam-viec-output.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/toll/models/tram-output.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/components/cctv/CctvPtzPanel.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/components/cctv/cctvComponent.service.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/composables/useVmsContent.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/composables/useVmsControlDisplay.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/composables/useVmsIncident.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/composables/useVmsManual.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/composables/useVmsValidation.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/index.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/directive/authDirective.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/hooks/useTollCatalog.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/hooks/useVideoWallScope.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/i18n/i18n.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/router/backEnd.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/router/frontEnd.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/ISourceService.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/apiUtils.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/controllerServiceApi.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/eventRuleServiceApi.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/mappers.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/sceneServiceApi.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/scheduleServiceApi.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/screenServiceApi.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/sourceServiceApi.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/source.types.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/stores/baseConfig.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/stores/its/commonVarsAndFuncs.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/stores/its/incidentAutomation.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/stores/its/mapStore.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/stores/its/vehicleFilterAlert.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/stores/requestOldRoutes.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/stores/userInfo.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/transporter/hooks/useTransporterVms.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/arrayOperation.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/authFunction.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/axios-utils.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/constHelper.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/download.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/exportExcel.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/locale.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/other.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/randomHelper.js`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/videoWall/controllerQuery.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/videoWall/sceneQuery.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/videoWall/screenQuery.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/videoWall/sourcePrepare.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/utils/videoWall/sourceQuery.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/composables/useMapEditing.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/composables/useMapFilters.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/composables/useMapLoader.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/composables/useZoneStatus.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/helpers/mapViewUtils.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/helpers/markerIcons.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/transformDataHelper.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/component/CameraGrid.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/composables/useCameraDashboardEditor.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/services/cctvDashboardConfig.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/services/cctvDashboardService.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/services/cctvNxSession.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/types.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/utils/cameraControlUtils.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/templates/utils/templateLayout.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/templates/utils/templatePresentation.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/fms/telemetry/telemetryMetrics.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/toll/lichGuiMail/component/editLichGuiMail.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/toll/lichGuiMail/index.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/controllers/component/controllerDetail.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/controllers/index.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/itsIntegration/index.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/scenes/index.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/schedule/index.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/sources/component/editSource.vue`
- `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/sources/index.vue`

## Key Files

| File | Symbols |
|------|---------|
| `` | includes, map, querySelectorAll, toLowerCase, sort, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/system/apis/sys-file-api.ts` | headersFromBaseOptions, query, baseOptions, options, apiSystemSysfileUploadfilesPostForm, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/toll/models/ca-lam-viec-output.ts` | CaLamViecOutput |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/toll/models/tram-output.ts` | TramOutput |
| `TA-ITS015-WEBVUE-V1.0/src/src/components/cctv/CctvPtzPanel.vue` | status, getCameraStatusLabel |
| `TA-ITS015-WEBVUE-V1.0/src/src/components/cctv/cctvComponent.service.ts` | input, equipmentTypeCode, equipmentTypeId, isCctvCameraEquipmentTypeCode, error, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/composables/useVmsContent.ts` | current, formatRowCase, mode, beforeUpload, deps, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/composables/useVmsControlDisplay.ts` | onWordList1SelectChange, temp, toggleControlDisplay, onVmsActionlTypeChange, ctrlType, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/composables/useVmsIncident.ts` | replaceDynamicTokens, state, getConfigDataByCode, applyEventDefaultContent, $t, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/composables/useVmsManual.ts` | error, sourceList, ctrlType, displayInformationProcess, key, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/composables/useVmsValidation.ts` | size, isTextControl, list, row2, getTemplateTextContents, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/components/vms/controlVms/index.vue` | index |
| `TA-ITS015-WEBVUE-V1.0/src/src/directive/authDirective.ts` | binding, stores, stores, binding, mounted, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/hooks/useTollCatalog.ts` | loadTollStations, x, toCatalogItem, x, toShiftItem, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/hooks/useVideoWallScope.ts` | isFullAccess, VideoWallDataScope, userStore, useVideoWallScope, controllers, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/i18n/i18n.ts` | data, loadLocaleMessages, locale, i18n |
| `TA-ITS015-WEBVUE-V1.0/src/src/router/backEnd.ts` | matchKeys, matchKey, dynamicImport, component, keys, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/router/frontEnd.ts` | setFilterRoute, storesRoutesList, chil, setAddRoute, hasRoles, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/ISourceService.ts` | ListResponse, ISourceService |
| `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/apiUtils.ts` | all, fetchAllPaged, fetchPage, res, result, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/controllerServiceApi.ts` | list, getAll, action |
| `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/eventRuleServiceApi.ts` | action, list, getAll |
| `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/mappers.ts` | seed, id, fromSource, makeCode, prefix, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/sceneServiceApi.ts` | list, getAll, beStatus, action, getByStatus, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/scheduleServiceApi.ts` | action, getAll, list |
| `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/screenServiceApi.ts` | action, getByOutputType, list, getAll, panelType, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/api/sourceServiceApi.ts` | list, be, action, res, id, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/services/videoWall/source.types.ts` | SignalStatus, SignalType, SourceStatus, AspectRatio, Source, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/stores/baseConfig.ts` | configListTemp, configList, data, setConfigDataLanguage, useBaseConfig.setConfigDataList, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/stores/its/commonVarsAndFuncs.ts` | equipmentTypeCode, response, response, url, response, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/stores/its/incidentAutomation.ts` | serverTime, useIncidentAutomation.syncFromIncidentList, incidentList, getters.pendingRuns, list, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/stores/its/mapStore.ts` | changedEquipmentIds, updates, payload, useMapStore.updateEquipmentStatusFromNats |
| `TA-ITS015-WEBVUE-V1.0/src/src/stores/its/vehicleFilterAlert.ts` | records, useVehicleFilterAlert.dismissAlert, payload, useVehicleFilterAlert.processVdsData, id, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/stores/requestOldRoutes.ts` | matchedRoutes, searchRoutes, routes, defaultMenu |
| `TA-ITS015-WEBVUE-V1.0/src/src/stores/userInfo.ts` | dictTypeCode, useUserInfo.getDictIntDatasByCode, dictList, ds, useUserInfo |
| `TA-ITS015-WEBVUE-V1.0/src/src/transporter/hooks/useTransporterVms.ts` | data, key, out, camel, normalizeVmsRecord |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/arrayOperation.ts` | clone, value, T |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/authFunction.ts` | stores, auths, stores, auth, value, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/axios-utils.ts` | token, decryptJWT, json |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/constHelper.ts` | getConstType, constType, type, userStore |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/download.ts` | headers, fileNameUnicode, fileName, getFileName |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/exportExcel.ts` | traverse, obj |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/locale.ts` | term, getTerminologyList, result, lang |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/other.ts` | lazyImg, arr, other.lazyImg, el, io, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/randomHelper.js` | generateRandomNumber |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/videoWall/controllerQuery.ts` | numeric, order, filtered, page, size, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/videoWall/sceneQuery.ts` | all, filters, compareValues, size, sort, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/videoWall/screenQuery.ts` | a, records, start, all, value, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/videoWall/sourcePrepare.ts` | id, prepareForCreate, updatedAt, row, rest, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/utils/videoWall/sourceQuery.ts` | total, sort, size, all, source, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/composables/useMapEditing.ts` | name, coords, layer, item, handleStartDrawingPolygon, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/composables/useMapFilters.ts` | isHaveAnyPolygonInFeatures, filters, listFeaturesPolygon, listIdFilter, applyFilters, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/composables/useMapLoader.ts` | center, centerImageMap, bounds, map |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/composables/useZoneStatus.ts` | STATUS_SEVERITY, getFlowDurationByStatus, latestNatsZoneStatuses, updateZoneStatusFromNats, n, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/helpers/mapViewUtils.ts` | hex, hexToRgb, h |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/helpers/markerIcons.ts` | parsePositiveSize, stateValue, resolveEquipmentIconSize, resolveEquipBorderColorByStatus, equipmentType, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/mapSample/transformDataHelper.ts` | groupedData, response, isIdInGeoJson, zonesData, equipmentTypeList, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/component/CameraGrid.vue` | index, second, _camera, cellSpan, items, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/composables/useCameraDashboardEditor.ts` | getNearestCameraPosition, score, x, canPlaceCameraAt, updateCameraLayout, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/services/cctvDashboardConfig.ts` | readBooleanConfig, items, getCctvVendor, value, value, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/services/cctvDashboardService.ts` | dashboardId, tags, devices, items, normalizeTags, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/services/cctvNxSession.ts` | hasNxRuntimeGuidCookie |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/types.ts` | VideoMonitoringItem |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/dashboard/utils/cameraControlUtils.ts` | value, hasDeviceId, cameras, hasUsableData, status, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/templates/utils/templateLayout.ts` | slots, renumberByReadingOrder |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/cctv/templates/utils/templatePresentation.ts` | TemplateWithCreateTime, sortTemplatesByCreateTimeDesc, timestamp, templates, TemplateCreateTime, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/fms/telemetry/telemetryMetrics.ts` | parseColumnJson, raw, raw, def, MetricColumn, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/toll/lichGuiMail/component/editLichGuiMail.vue` | row, openDialog |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/toll/lichGuiMail/index.vue` | handleAdd |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/controllers/component/controllerDetail.vue` | row, open |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/controllers/index.vue` | all, sort, scoped, error, page, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/itsIntegration/index.vue` | scenes, ajax.query, records, total, page, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/scenes/index.vue` | loadControllers |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/schedule/index.vue` | records, sort, ajax.query, total, handleQueryApi, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/sources/component/editSource.vue` | closeDialog, submit |
| `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/sources/index.vue` | resetQuery |

## Connected Communities

- **controlVms/composables +6 dirs** (16 cross-edges)
- **. +9 dirs** (13 cross-edges)
- **tms/models +100 dirs** (12 cross-edges)
- **. +6 dirs** (10 cross-edges)
- **tms/apis +12 dirs** (7 cross-edges)
- **videoWall/models +6 dirs** (7 cross-edges)
- **cctv/dashboard · normalizeCameraGridItem** (6 cross-edges)
- **tms/apis +20 dirs** (4 cross-edges)
- **src/utils +3 dirs · ensureStatusRibbonGradient** (4 cross-edges)
- **src/router +2 dirs** (3 cross-edges)
- **. +18 dirs** (3 cross-edges)
- **bigScreen/mapSample · renderGeoJson** (2 cross-edges)
- **src/utils +2 dirs · safeString** (2 cross-edges)
- **. +3 dirs · prioritizeTemplateById** (2 cross-edges)
- **cfgSystem/apis +4 dirs** (2 cross-edges)
- **components/cctv +1 dirs · safeString** (2 cross-edges)
- **videoWall/models +2 dirs · apiVideowallVwsourceDeletePost** (2 cross-edges)
- **mapSample/composables +1 dirs** (2 cross-edges)
- **videoWall/models +2 dirs · apiVideowallVwscreenDeletePost** (2 cross-edges)
- **videoWall/models +3 dirs** (2 cross-edges)
- **api-services/toll · apiTollCalamviecListGet** (1 cross-edges)
- **src/stores +2 dirs · initBackEndControlRoutes** (1 cross-edges)
- **. +5 dirs** (1 cross-edges)
- **api-services/videoWall · apiVideowallVwsourceByidGet** (1 cross-edges)
- **cfgSystem/models +2 dirs · SysConfigTypeApiFp** (1 cross-edges)
- **cfgSystem/apis** (1 cross-edges)
- **cfgSystem/apis +2 dirs · apiCfgsystemSysconfigtypeListGet** (1 cross-edges)
- **src/stores +1 dirs · globalComponentSize** (1 cross-edges)
- **. +3 dirs · parseFloat** (1 cross-edges)
- **services/videoWall +7 dirs** (1 cross-edges)
- **cfgSystem/apis +2 dirs · apiCfgsystemSysopconfigListGet** (1 cross-edges)
- **api-services/cctv · apiCctvDashboarditemUpdatePost** (1 cross-edges)
- **videoWall/models +2 dirs · apiVideowallVwcontrollerDeleteP…** (1 cross-edges)
- **api-services/toll · apiTollTramListGet** (1 cross-edges)
- **. +2 dirs · toStr** (1 cross-edges)

## How to Explore

```
analyze(operation:"communities", id:"community-1366")
explore(operation:"context", task:"understand src/utils +35 dirs", format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
