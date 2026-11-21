# Sơ đồ kiến trúc & luồng xử lý Sự cố (Incident)

> Tài liệu sơ đồ hoá source code và luồng xử lý phân hệ **TMS Incident** (Backend .NET + Frontend Vue). Bổ sung cho `incident-flow.md` (mô tả chi tiết bằng lời). Sơ đồ dùng cú pháp **Mermaid** (xem được trên VS Code/GitHub/Kiro preview).

---

## 1. Bản đồ source code (component map)

```mermaid
flowchart TB
  subgraph FE["FRONTEND (TA-ITS015-WEBVUE)"]
    direction TB
    subgraph FEViews["views/tms/incident"]
      idx["index.vue<br/>(danh sách + lọc + chart)"]
      edit["component/editIncident.vue<br/>(thêm/sửa)"]
      detail["component/detailIncident.vue<br/>(xem chi tiết)"]
      tl["timeline.vue<br/>(dòng thời gian)"]
      lc["component/lineChart.vue"]
      cnt["component/incidentCount.vue"]
    end
    subgraph FEModal["views/modal/incident"]
      popup["incidentPopup.vue<br/>(xử lý realtime, đa tab)"]
    end
    subgraph FEBig["views/bigScreen/trafficInfo"]
      notif["incidentNotification.vue"]
      bigidx["index.vue + controlMap.vue"]
      leaflet["mapSample/LeafletMap.vue<br/>(marker sự cố)"]
    end
    store["stores/its/incidentMonitor.ts"]
    sock["transporter/transporterSocket.ts"]
    api["api-services/tms<br/>(TmsIncidentApi - tự sinh)"]
  end

  subgraph BE["BACKEND (TA-ITS015-WEBAPI / Modules.TMS)"]
    direction TB
    ctrl["Controllers/Incident/<br/>TmsIncidentController.cs"]
    subgraph Cmd["Commands"]
      ch["IncidentCommandHandler<br/>(Add/Update/Delete)"]
      wch["IncidentWorkflowCommandHandler<br/>(UpdateState/BatchUpdateState)"]
    end
    subgraph Qry["Queries"]
      qh["IncidentQueryHandler<br/>(Page/List/Single)"]
      wqh["IncidentWorkflowQueryHandler<br/>(Latest/Check/ByAccount/AllowedAction)"]
      sqh["IncidentStatisticsQueryHandler<br/>(Chart/Count)"]
    end
    subgraph Svc["Infrastructure/Services/Incident"]
      bc["IncidentBroadcaster"]
      perm["IncidentPermissionService"]
      acc["IncidentAccessService"]
      chart["IncidentChartService"]
    end
    repo[("BaseRepository&lt;T&gt;<br/>SqlSugar / SQL Server")]
    transBE["ItsDataTransporter<br/>(NATS / SocketCluster)"]
  end

  idx & edit & detail & tl & lc & cnt & popup & notif & bigidx --> api
  api -->|"HTTP REST"| ctrl
  ctrl -->|"MessBus.InvokeAsync"| Cmd
  ctrl -->|"MessBus.InvokeAsync"| Qry
  ch --> bc
  wch --> bc
  wqh --> perm
  wqh --> acc
  sqh --> chart
  sqh -->|"InvokeAsync(Page)"| qh
  acc --> perm
  Cmd --> repo
  Qry --> repo
  acc --> repo
  bc --> transBE
  transBE -->|"kênh its.incident"| sock
  sock --> store
  store -->|"watch(broadcastCode)"| idx
  store --> popup
  store --> notif
  store --> leaflet
```

---

## 2. Vòng đời trạng thái sự cố (state machine)

```mermaid
stateDiagram-v2
  [*] --> Registration: Add (CRUD)
  Registration --> Skipped: Skip
  Registration --> Verified: Verify
  Verified --> Canceled: Cancel
  Verified --> InProgress: Execute (gán TrafficPlan)
  InProgress --> Canceled: Cancel
  InProgress --> Finished: Finish
  Skipped --> [*]
  Canceled --> [*]
  Finished --> [*]

  note right of InProgress
    Bắt buộc có TrafficPlanId
    Sinh danh sách TmsWorkExecute (Waiting)
    Insert lỗi -> rollback về Verified
  end note
  note right of Finished
    Chuyển TmsWorkExecute
    -> TmsWorkExecuteHistory
  end note
```

Giá trị enum `TmsIncidentStateEnum`: `Skipped=0, Registration=1, Verified=2, InProgress=3, Finished=4, Canceled=5` (và `Updated=99`, `Deleted=100` chỉ dùng cho thông báo realtime).

---

## 3. Điều phối CQRS — message → handler

```mermaid
flowchart LR
  subgraph Controller["TmsIncidentController"]
    e1["/Add /Update /Delete"]
    e2["/UpdateState /BatchUpdateState"]
    e3["/Page /List /Single"]
    e4["/LatestIncident /CheckIncident<br/>/ListByAccount /ListAllowedAction"]
    e5["/IncidentChart /CountIncident"]
  end

  e1 -->|"Add/Update/DeleteTmsIncidentInput"| H1["IncidentCommandHandler"]
  e2 -->|"TmsIncidentStateInput<br/>List&lt;TmsIncidentStateInput&gt;"| H2["IncidentWorkflowCommandHandler"]
  e3 -->|"Page/Tms/SingleInput"| H3["IncidentQueryHandler"]
  e4 -->|"Latest/Check/ByAccount/AllowedActionInput"| H4["IncidentWorkflowQueryHandler"]
  e5 -->|"IncidentChart/CountInput"| H5["IncidentStatisticsQueryHandler"]

  H5 -.->|"InvokeAsync&lt;PagedList&gt;(Page)"| H3
```

> Mỗi message type được Wolverine định tuyến tới **đúng một** `HandleAsync`. `IncidentStatisticsQueryHandler` tái sử dụng truy vấn Page bằng cách dispatch lại `PageTmsIncidentInput` qua `IMessageBus`.

---

## 4. Luồng tạo sự cố (Add) — sequence

```mermaid
sequenceDiagram
  autonumber
  actor U as Người dùng
  participant FE as editIncident.vue
  participant API as TmsIncidentApi
  participant C as TmsIncidentController
  participant H as IncidentCommandHandler
  participant R as BaseRepository
  participant B as IncidentBroadcaster
  participant T as ItsDataTransporter

  U->>FE: Nhập thông tin + Submit
  FE->>FE: generateCodeAndName()<br/>dò Zone/Camera theo vị trí
  FE->>API: apiTmsTmsincidentAddPost(form)
  API->>C: POST /Add
  C->>H: MessBus.InvokeAsync(AddInput)
  H->>R: IsAnyAsync(Code trùng?)
  alt Trùng mã
    H-->>C: Oops.Oh(Exist)
  else Hợp lệ
    H->>H: State=Registration, CreatedBy/Date
    H->>R: InsertWithDiffLogAsync(incident)
    H->>R: cache.RemoveByPrefixKey(tms_incident)
    H->>B: Notify(incident, Registration)
    B->>T: BroadcastData(its.incident)
    H-->>C: true
  end
  C-->>FE: result
  T-->>FE: (realtime) cập nhật các màn hình
```

---

## 5. Luồng xử lý realtime trong Popup — sequence

```mermaid
sequenceDiagram
  autonumber
  actor S as Giám sát/Trưởng ca
  participant P as incidentPopup.vue
  participant API as TmsIncidentApi
  participant C as TmsIncidentController
  participant WQ as IncidentWorkflowQueryHandler
  participant WC as IncidentWorkflowCommandHandler
  participant ACC as IncidentAccessService
  participant PERM as IncidentPermissionService

  Note over P: loadIncidentsByAccount()
  P->>API: CheckIncident / ListAllowedAction / ListByAccount
  API->>C: POST ...
  C->>WQ: InvokeAsync(...)
  WQ->>PERM: IsInGroupAsync(Leader/Supervisor)
  WQ->>ACC: GetByAccountAsync()
  ACC->>PERM: IsInGroupAsync(...)
  ACC-->>WQ: {registration, verified, inProgress, allProcessing}
  WQ-->>P: danh sách theo phân quyền

  S->>P: Verify / Skip / Authorize / Cancel / Finish
  P->>API: apiTmsTmsincidentUpdatestatePost(...)
  API->>C: POST /UpdateState
  C->>WC: InvokeAsync(TmsIncidentStateInput)
  WC->>WC: UpdateIncidentState(...)
  alt state = InProgress
    WC->>WC: sinh TmsWorkExecute (Waiting) theo TrafficPlan
  else state = Finished
    WC->>WC: chuyển WorkExecute -> History
  end
  WC-->>P: true → loadIncidentsByAccount() lại
```

---

## 6. Luồng realtime broadcast (BE → FE)

```mermaid
flowchart LR
  subgraph BEflow["Backend"]
    H["CommandHandler /<br/>WorkflowCommandHandler"] --> B["IncidentBroadcaster.Notify(incident, state)"]
    B --> T["ItsDataTransporter.BroadcastData<br/>(ItsDataTransporterChannel.Incident)"]
  end
  T -->|"kênh 'its.incident'<br/>{broadcastCode, broadcastObject,<br/>broadcastMessage, broadcastDateTime}"| SOCK["transporterSocket.ts<br/>case 'its.incident'"]
  SOCK --> MON["incidentMonitor<br/>(broadcastCode, ...)"]
  MON -->|"watch(broadcastCode)"| V1["tms/incident/index.vue → handleQuery()"]
  MON --> V2["modal/incident/incidentPopup.vue → loadIncidentsByAccount(true)"]
  MON --> V3["bigScreen/.../incidentNotification.vue → getLatestIncident()"]
  MON --> V4["bigScreen/.../centerInfo.vue → handleQuery()"]
```

---

## 7. Sơ đồ phụ thuộc lớp BE (class dependency)

```mermaid
classDiagram
  class TmsIncidentController
  class IncidentCommandHandler
  class IncidentWorkflowCommandHandler
  class IncidentQueryHandler
  class IncidentWorkflowQueryHandler
  class IncidentStatisticsQueryHandler
  class IncidentBroadcaster
  class IncidentPermissionService
  class IncidentAccessService
  class IncidentChartService

  TmsIncidentController ..> IncidentCommandHandler : MessBus
  TmsIncidentController ..> IncidentWorkflowCommandHandler : MessBus
  TmsIncidentController ..> IncidentQueryHandler : MessBus
  TmsIncidentController ..> IncidentWorkflowQueryHandler : MessBus
  TmsIncidentController ..> IncidentStatisticsQueryHandler : MessBus

  IncidentCommandHandler --> IncidentBroadcaster
  IncidentWorkflowCommandHandler --> IncidentBroadcaster
  IncidentWorkflowQueryHandler --> IncidentPermissionService
  IncidentWorkflowQueryHandler --> IncidentAccessService
  IncidentStatisticsQueryHandler --> IncidentChartService
  IncidentStatisticsQueryHandler ..> IncidentQueryHandler : InvokeAsync(Page)
  IncidentAccessService --> IncidentPermissionService
```

---

## 8. Phân quyền nhóm → dữ liệu hiển thị (ByAccount)

```mermaid
flowchart TD
  A["IncidentWorkflowQueryHandler.ByAccount"] --> ACC["IncidentAccessService.GetByAccountAsync()"]
  ACC --> Q{"OrgCode thuộc nhóm?"}
  Q -->|"GroupLeader (TcVhh)"| L["GetForLeaderAsync()<br/>→ allProcessingIncident (InProgress)"]
  Q -->|"GroupSupervisor (NvGsgt)"| S["GetForSupervisorAsync()<br/>→ registration + verified + inProgress"]
  Q -->|"Nhân viên (NvHtd...)"| E["GetForEmployeeAsync()<br/>→ inProgress lọc theo ExecuteOrg"]
  L --> WD["BuildWorkDetailViewAsync()"]
  S --> WD
  E --> WD
  WD --> OUT["Dictionary&lt;string, List&lt;TmsIncidentView&gt;&gt;"]
```

---

## 9. Sự cố trên bản đồ (BigScreen) — luồng hiển thị & tương tác

```mermaid
flowchart TD
  ME["mapStore.fetchIncidentZoneDataExistInTrafficInfo()"] --> APIm["apiTmsTrafficinfoCheckincidentexistsPost"]
  APIm --> BEm["TmsTrafficInfoController.CheckIncidentExists<br/>→ List&lt;ListZoneIncidentOutput&gt;"]
  BEm --> STORE["mapStore.incidentZoneDataExistInTrafficInfoList"]
  STORE --> RENDER["LeafletMap.vue: renderPolygon<br/>vẽ marker (itemGroup='incident')"]
  RENDER --> HOVER["index.vue: handleMouseOver<br/>→ popover thông tin sự cố"]
  RENDER --> CLICK["index.vue: handleItemClick('incident')"]
  CLICK --> PERMCHK{"userCanViewData()?"}
  PERMCHK -->|"có"| OPEN["incidentMonitor.isShowIncidentPopup = true<br/>selectedIncidentId = id"]
  PERMCHK -->|"không"| STOP["bỏ qua"]
  STORE --> FILTER["controlMap.vue: nhóm filter 'event'<br/>chỉ giữ eventType đang có sự cố"]
```

---

## 10. Bảng tham chiếu file ↔ chức năng

| Chức năng | Backend | Frontend |
|-----------|---------|----------|
| CRUD sự cố | `IncidentCommandHandler`, `IncidentQueryHandler` | `index.vue`, `editIncident.vue`, `detailIncident.vue` |
| Chuyển trạng thái | `IncidentWorkflowCommandHandler` | `index.vue` (skip/cancel), `incidentPopup.vue` |
| Theo account/phân quyền | `IncidentWorkflowQueryHandler`, `IncidentAccessService`, `IncidentPermissionService` | `incidentPopup.vue`, `incidentMonitor.ts` |
| Biểu đồ/thống kê | `IncidentStatisticsQueryHandler`, `IncidentChartService` | `lineChart.vue`, `incidentCount.vue` |
| Thông báo realtime | `IncidentBroadcaster` → `ItsDataTransporter` | `transporterSocket.ts`, `incidentMonitor.ts` |
| Hiển thị trên bản đồ | `TmsTrafficInfoController` (zone-incident) | `LeafletMap.vue`, `controlMap.vue`, `incidentNotification.vue` |
| Dòng thời gian | (đọc WorkExecute/History) | `timeline.vue` |

---

> Ghi chú: tên handler đã chuẩn hoá theo refactor mới (`Incident*Handler`, bỏ tiền tố `Tms`). Route/endpoint và DTO giữ nguyên (`api/tms/TmsIncident/...`). Chi tiết logic xem `incident-flow.md`; đề xuất tối ưu xem `incident-improvements.md`/`incident-refactor.md`.
