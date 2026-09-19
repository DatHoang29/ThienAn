---
name: gortex-src-api-services-statusenum
description: "Work in the src/api-services · StatusEnum area — 584 symbols across 27 files (89% cohesion)"
---

# src/api-services · StatusEnum

584 symbols | 27 files | 89% cohesion

## When to Use

Use this skill when working on files in:
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/cctv/apis/template-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/cctv/models/status-enum.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/mail/apis/rpt-email-schedule-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/report/apis/rpt-management-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/system/apis/sys-role-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-equipment-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-equipment-event-rule-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-equipment-type-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-event-type-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-incident-automation-config-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-map-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-modules-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-traffic-plan-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-vehicle-filter-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-work-contact-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-work-detail-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-work-unit-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-zone-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-zone-equipment-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-controller-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-event-rule-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-scene-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-schedule-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-screen-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-slot-port-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-source-api.ts`
- `TA-ITS015-WEBVUE-V1.0/src/src/api-services/vms/apis/vms-template-api.ts`

## Key Files

| File | Symbols |
|------|---------|
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/cctv/apis/template-api.ts` | status, query, localVarHeaderParameter, localVarQueryParameter, apiCctvTemplateTemplatelistGet, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/cctv/models/status-enum.ts` | NUMBER_0, NUMBER_1, StatusEnum |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/mail/apis/rpt-email-schedule-api.ts` | accessToken, localVarPath, status, options, localVarUrlObj, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/report/apis/rpt-management-api.ts` | baseOptions, accessToken, apiReportRptmanagementAddPostForm, type, localVarPath, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/system/apis/sys-role-api.ts` | localVarRequestOptions, accessToken, options, key, key, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-equipment-api.ts` | status, headersFromBaseOptions, equipmentTypeId, localVarUrlObj, apiTmsTmsequipmentListGet, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-equipment-event-rule-api.ts` | eventTypeId, localVarRequestOptions, status, baseOptions, equipmentTypeId, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-equipment-type-api.ts` | ID, moduleId, localVarRequestOptions, headersFromBaseOptions, options, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-event-type-api.ts` | localVarPath, localVarQueryParameter, accessToken, key, localVarHeaderParameter, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-incident-automation-config-api.ts` | options, localVarRequestOptions, localVarUrlObj, localVarHeaderParameter, query, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-map-api.ts` | code, localVarPath, baseOptions, type, localVarRequestOptions, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-modules-api.ts` | localVarQueryParameter, localVarPath, headersFromBaseOptions, localVarHeaderParameter, status, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-traffic-plan-api.ts` | query, name, options, localVarHeaderParameter, localVarQueryParameter, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-vehicle-filter-api.ts` | query, localVarRequestOptions, headersFromBaseOptions, accessToken, status, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-work-contact-api.ts` | localVarPath, key, options, code, type, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-work-detail-api.ts` | key, ID, options, headersFromBaseOptions, status, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-work-unit-api.ts` | accessToken, localVarUrlObj, baseOptions, localVarRequestOptions, name, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-zone-api.ts` | toMetNumber, localVarUrlObj, localVarRequestOptions, apiTmsTmszoneListGet, options, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/tms/apis/tms-zone-equipment-api.ts` | apiTmsTmszoneequipmentListGet, baseOptions, headersFromBaseOptions, accessToken, zoneId, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-controller-api.ts` | options, localVarQueryParameter, headersFromBaseOptions, accessToken, name, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-event-rule-api.ts` | eventSource, localVarHeaderParameter, priority, options, accessToken, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-scene-api.ts` | ID, apiVideowallVwsceneListGet, outputId, localVarPath, key, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-schedule-api.ts` | targetSceneId, name, action, baseOptions, localVarRequestOptions, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-screen-api.ts` | status, ID, query, localVarHeaderParameter, key, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-slot-port-api.ts` | status, ID, options, localVarPath, accessToken, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/videoWall/apis/vw-source-api.ts` | apiVideowallVwsourceListGet, localVarPath, headersFromBaseOptions, options, accessToken, ... |
| `TA-ITS015-WEBVUE-V1.0/src/src/api-services/vms/apis/vms-template-api.ts` | lang, localVarQueryParameter, baseOptions, localVarPath, size, ... |

## Connected Communities

- **cctv/models +4 dirs · apiCctvTemplateAddPost** (2 cross-edges)
- **tms/models +100 dirs** (1 cross-edges)

## How to Explore

```
analyze(operation:"communities", id:"community-868")
explore(operation:"context", task:"understand src/api-services · StatusEnum", format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
