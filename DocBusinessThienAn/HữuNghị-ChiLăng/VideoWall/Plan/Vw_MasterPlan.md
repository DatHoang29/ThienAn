# MasterPlan Phân Hệ VideoWall (Tường Màn Hình)

> 🔴 **SINGLE SOURCE OF TRUTH (SSOT) — TÀI LIỆU SỐNG DUY NHẤT CHO VIDEOWALL.**
> ⚠️ **KHÔNG XOÁ theo quy ước Auto-Cleanup.** Theo quy định dự án (Mục 13 & Mục 19.52 của `thienan_rules.md`), mỗi phân hệ **CHỈ CÓ DUY NHẤT 1 FILE MASTERPLAN NÀY**.
> Mọi cập nhật kiến trúc, kết quả review/audit sau triển khai, tiến độ trên nhánh `dev`, và backlog phát sinh BẮT BUỘC phải được cập nhật trực tiếp vào file này (kèm ghi nhận tại bảng *Lịch sử cập nhật* bên dưới). Tuyệt đối **KHÔNG tạo thêm file MasterPlan hay file Review riêng lẻ đính kèm ngày tháng** làm phân mảnh tài liệu.
>
> 📖 **Tài liệu tham chiếu:**
> - Đặc tả UI & Test checklist: [`../DacTa/VideoWall-DacTa-UI_1.md`](../DacTa/VideoWall-DacTa-UI_1.md)
> - Kiến trúc phần cứng Cascade: [`../doc/KienTruc_VideoWall_DS-C66S-Cascade.md`](../doc/KienTruc_VideoWall_DS-C66S-Cascade.md)
> - Bảng CSDL & Audit Write Path: [`../doc/TableSQL/Vw_Tables_Analysis_And_Design.md`](../doc/TableSQL/Vw_Tables_Analysis_And_Design.md)
> - Kịch bản test API Cascade 32 màn: [`../doc/KichBan/KichBan_VideoWall_DS-C66S_4Controller_32Man.md`](../doc/KichBan/KichBan_VideoWall_DS-C66S_4Controller_32Man.md)

---

## 📜 Lịch Sử Cập Nhật & Tiến Hóa Của Tài Liệu (Changelog)

| Phiên bản | Mốc ngày | Người cập nhật | Nội dung cập nhật chính |
|---|---|---|---|
| **v1.0** | 16-09-2026 | Antigravity AI / Team | Khởi tạo MasterPlan tổng thể sau khi đối chiếu 2 transcript họp gốc (09-09 & 11-09) và review code BE đợt 1 (commit `01f724d3`). Chốt kiến trúc 3 tầng qua NATS, cascade DS-C66S, phân quyền ma trận toạ độ. |
| **v1.1** | 30-09-2026 | Antigravity AI / Team | Ghi nhận hoàn tất đợt refactor bộ test VideoWall: mirror thư mục `WebApi/`, `Worker/`, `Wpf/`, chuẩn hóa mock server 14 partials, dọn cleanup trong test theo `thienan_rules.md`. |
| **v2.0** | 06-10-2026 | Antigravity AI / Team | **Nâng cấp toàn diện & Gom về 1 file SSOT duy nhất**: Đối chiếu toàn bộ commit đã merge vào `dev` (PR #63 BE & PR #86 FE - phân quyền VideoWall, worker service, Canvas rào quyền). Tích hợp đối chiếu với Đặc tả UI mới (`VideoWall-DacTa-UI_1.md`), phân tích 10 màn hình, rà soát lỗi runtime NATS trên Monitor, thiếu worker scheduler, và tổng hợp danh sách Gaps L-01 $\rightarrow$ L-16 thành Action Plan 3 giai đoạn. |
| **v2.1** | 06-10-2026 | Antigravity AI / Team | **Hợp nhất hoàn chỉnh báo cáo Review Backend 14-09-2026 vào MasterPlan**: Tích hợp toàn bộ nội dung audit 9 hạng mục prompt, đối chiếu kiến trúc DS-C66S, log đo kiểm thực tế và giải quyết dứt điểm các file review rời rạc. Thư mục `Plan/` đạt trạng thái 100% duy nhất 1 tệp SSOT. |

---

## 1. Bối Cảnh Nguồn Dữ Liệu & Căn Cứ Đối Chiếu

1. **Hai bản ghi họp gốc (Tier A transcript):**
   - [`09-09-2026-videowall-phan-quyen-va-layout.md`](../doc/transcript/09-09-2026-videowall-phan-quyen-va-layout.md): Phân quyền khu vực theo ma trận lưới màn hình, quy tắc ưu tiên `UserId` > `OrgId`, ghép nối NATS thay cho Mock data, tối ưu cây danh mục Zone với SqlSugar `ToTree()`.
   - [`11-09-2026-videowall-script.md`](../doc/transcript/11-09-2026-videowall-script.md): Kiến trúc phân tầng loose-coupling qua NATS Message Broker (FE ↔ BE ↔ NATS ↔ Worker Service ↔ ISAPI), lý do tách riêng Worker Service chống blocking WebAPI, 3 trụ cột (Thiết lập / Điều khiển / Giám sát), cơ chế rào quyền theo Screen ID/tọa độ ô.
2. **Các commit đã build và merge vào nhánh `dev`:**
   - **Backend WebAPI (`TA-ITS015-WEBAPI-V1.0`)**: PR #63 (`feat/20260924-videowall-hoan-thien-chuc-nang`) bao gồm commit `bbd178fe` (Service Worker `ITS.VideoWall` giao tiếp ISAPI/NATS) và commit `3acc1f42` (`VwWallPermission`, `VwPermissionService`, `VwEventTriggerLog`, `VwWallTopology`, `VwDeviceSetup`).
   - **Frontend WebVue (`TA-ITS015-WEBVUE-V1.0`)**: PR #86 (`feat/20260924-videowall-hoan-thien-chuc-nang`) bao gồm commit `4ee94cc1` (màn hình Phân quyền vùng `wallPermission`, lưới chọn ô `wallMiniGrid`, tích hợp rào quyền Canvas) và commit `a7d61c8e` (Refactor & Clean code FE 10 màn hình VideoWall).
   - **Test Suite (`tests/BE/ITS/VideoWall`)**: Đợt refactor 30/09/2026 (mirror thư mục `WebApi/`, `Worker/`, `Wpf/`, chuẩn hóa `VwISAPIServerHikvisionMock` 14 partials, dọn cleanup trong test).
3. **Tài liệu Đặc tả UI mới nhất:** [`../DacTa/VideoWall-DacTa-UI_1.md`](../DacTa/VideoWall-DacTa-UI_1.md) (ban hành ngày 02/10/2026 - cập nhật 06/10/2026).

---

## 2. Kiến Trúc Tổng Thể & Luồng Điều Khiển Đã Chốt

```
[Web Frontend (Vue 3)]
        │
        ▼ HTTP REST / JWT
[Backend WebAPI (Module.VideoWall)]
        │
        ├── Pub/Sub (NATS Broker) ──► Kênh Lệnh: ta.its.data.videowall.control
        │                             Kênh Dữ Liệu/Trạng Thái: ta.its.data.videowall
        ▼
[VideoWall Worker Service (ITS.VideoWall)]
        │
        ▼ HTTP ISAPI Digest (Center Controller ONLY)
[Phần Cứng Controller Hikvision (DS-C66S-S12 / DS-C30S)]
        │
        ├── HDMI Out / GENLOCK nối vật lý sang Sub Controllers
        ▼
[Tường Màn Hình 8 Cột × 4 Hàng (32 Panel LCD 55" 4K logic)]
```

### 2.1. Nguyên Tắc Cốt Lõi (Khớp Chỉ Đạo Của Anh Sơn)
- **Loose-Coupling qua NATS:** WebAPI chỉ ghi nhận DB và bắn lệnh qua NATS, trả response ngay cho FE (non-blocking). VideoWall Background Service độc lập đảm nhiệm việc gọi HTTP ISAPI xuống thiết bị trạm.
- **Cô lập lỗi phần cứng (Fault Isolation):** Thiết bị ở phòng máy mất mạng, tắt nguồn hoặc chập chờn chỉ làm worker retry/ghi log, tuyệt đối không gây treo luồng HTTP của WebAPI hoặc làm sập hệ thống Web.
- **Kiến trúc Cascade DS-C66S (1 Trung tâm + tối đa 3 Con):**
  - Chỉ bộ điều khiển **Trung tâm (`Role == "center"`)** mới kết nối mạng và nhận lệnh ISAPI.
  - Các bộ điều khiển **Con (`Role == "sub"`)** chỉ liên kết vật lý (HDMI + GENLOCK), KHÔNG có IP/ISAPI traffic. Đã được ép cứng tại chokepoint `LoadController` trong worker service.
- **3 Trụ Cột Chức Năng:**
  1. *Thiết lập (Setup/Config):* Khai báo Controller, Screen, Port In/Out, Source, Scene/Window.
  2. *Điều khiển (Control):* Bật/tắt kịch bản (Switch Scene), tạo/đóng/di chuyển/phóng to cửa sổ (Roam/Zoom), đổi nguồn tín hiệu.
  3. *Giám sát & Dữ liệu (Telemetry/Status):* Đo trạng thái kết nối Online/Warning/Offline, heartbeat thiết bị, đồng bộ nguồn, gửi real-time qua NATS về FE.
- **Phân Quyền Ma Trận Lưới Màn Hình (Matrix Layout Bounding):**
  - Quản lý hoàn toàn ở tầng phần mềm (thiết bị không có khái niệm user/org).
  - Tường kích thước chuẩn: **8 cột × 4 hàng = 32 màn hình** (mỗi panel logic 3840 × 2160, toạ độ logic 30 720 × 8 640 px).
  - Bảng `VwWallPermission` lưu cấu hình: `UserId`, `OrgId`, `IsFullAccess`, `Config` (danh sách ô `[{"Col": x, "Row": y}]`).
  - **Quy tắc ưu tiên (Priority Rule):**
    1. SuperAdmin / Background worker $\rightarrow$ Full quyền.
    2. Cấu hình theo `UserId` có độ ưu tiên cao nhất, ghi đè `OrgId`.
    3. Không có `UserId` $\rightarrow$ Thừa hưởng quyền của `OrgId`.
    4. Không có bất kỳ cấu hình nào $\rightarrow$ **Không có quyền (None / Bị chặn)** để đảm bảo an toàn vận hành (khác đề xuất ban đầu mở toang Full quyền, đã chuẩn hoá an toàn trong `VwPermissionService`).
- **Danh Mục Vị Trí Zone:** Sử dụng SqlSugar `ToTree()` / `ToTreeAsync()`, cấm đệ quy thủ công.

---

## 3. Đối Chiếu Quyết Định Transcript ↔ Hiện Trạng Mã Nguồn Trên `dev`

| Yêu cầu trong Transcript | Người chỉ đạo | Trạng thái trên `dev` | Bằng chứng mã nguồn |
|---|---|---|---|
| **Bỏ mock service, ghép NATS thật** | Anh Sơn (09-09) | ✅ **HOÀN THÀNH** | Worker `ITS.VideoWall` subscribe NATS, WebAPI publish qua `VwItsNatsPublisher` |
| **Chuẩn hóa cấu trúc gói tin NATS + Metadata** | Anh Sơn (09-09) | ✅ **HOÀN THÀNH** | DTO `VwCommandEnvelope`, `VwCommandResponseEnvelope`, gắn `ControllerId`, `ServiceCode`, `CorrelationId` |
| **Tách VideoWall Background Service riêng** | Anh Sơn (11-09) | ✅ **HOÀN THÀNH** | Project `ITS.VideoWall` (Worker) độc lập với `Module.VideoWall` (WebAPI) |
| **Phân quyền ma trận lưới màn hình (8x4)** | Anh Sơn (09-09) | ✅ **HOÀN THÀNH** | Entity `VwWallPermission`, `VwPermissionService`, FE `wallPermission/index.vue`, `wallMiniGrid.vue` |
| **Quy tắc ưu tiên UserId > OrgId** | Anh Sơn (09-09, 11-09) | ✅ **HOÀN THÀNH** | `VwPermissionService.GetEffectivePermissionAsync`: tìm `UserId` trước, fallback sang `OrgId` |
| **Chặn kéo thả kịch bản ngoài vùng rào quyền** | Anh Sơn (09-09) | ✅ **HOÀN THÀNH** | `VwGeometryHelper`, `VwPermissionService.IsWithinAllowedCells`, FE `sceneCanvas.vue` làm mờ và chặn drag |
| **Dựng cây danh mục Zone bằng SqlSugar `ToTree`** | Anh Sơn (09-09) | ✅ **HOÀN THÀNH** | `Modules.TMS/Controllers/Zone/Queries/ZonesQueryHandler.cs` dùng `ToTreeAsync()` |
| **Kiến trúc Cascade DS-C66S (chỉ gọi ISAPI vào center)** | Anh Sơn (11-09) | ✅ **HOÀN THÀNH** | `VwISAPIDeviceService.DeviceSetup.cs:1125`: `LoadController` throw chặn mọi gọi lệnh vào Sub |

---

## 4. Trạng Thái Chi Tiết 10 Màn Hình Giao Diện (FE WebVue)

Căn cứ theo đối chiếu giữa mã nguồn `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall` và Đặc tả UI [`../DacTa/VideoWall-DacTa-UI_1.md`](../DacTa/VideoWall-DacTa-UI_1.md):

| # | Màn hình | Route / Thư mục | Trạng thái Code | Ghi chú & Điểm cần hoàn thiện |
|---|---|---|---|---|
| 6.1 | **Dashboard** | `views/videoWall/dashboard` | ✅ Đã có UI | Hiển thị tổng số màn (32), bộ điều khiển, trạng thái kết nối |
| 6.2 | **Bộ điều khiển (Controllers)** | `views/videoWall/controllers` | ✅ Đã có UI | Khai báo Controller, Cascade cha-con (tối đa 2 tầng), vùng phủ. ⚠️ Chưa có UI cho nhóm nút DeviceSetup (Ping/Probe/SyncSources) |
| 6.3 | **Nguồn tín hiệu (Sources)** | `views/videoWall/sources` | ✅ Đã có UI | Phân loại: Tín hiệu cục bộ (HDMI/DVI), Camera IP (RTSP), Liên kết cascade. Lọc form động theo loại nguồn |
| 6.4 | **Màn hình đầu ra (Outputs)** | `views/videoWall/outputs` | ✅ Đã có UI | Panel 55" 4K logic (3840×2160), vị trí lưới (Cột 0..7, Hàng 0..3), nối cổng P1..P8 |
| 6.5 | **Sơ đồ đấu nối (OutputMap)** | `views/videoWall/outputMap` | ✅ Đã có UI | Bố cục trực quan gán cổng ra của bộ điều khiển sang panel hiển thị |
| 6.6 | **Kịch bản (Scenes)** | `views/videoWall/scenes` | ✅ Đã có UI | Canvas kéo thả cửa sổ 4K, snap lưới, z-index, gán nguồn, rào quyền vùng mờ. ⚠️ Lưu kịch bản chưa nguyên tử (L-13) |
| 6.7 | **Giám sát tường (Monitor)** | `views/videoWall/monitor` | ⚠️ Có UI, **LỖI REAL-TIME** | Xem trước (preview) và Áp dụng (apply) kịch bản đang phát. 🔴 Ghi nhận lỗi: Worker phát tin NATS sau kích hoạt nhưng các máy trạm chưa tự cập nhật đồng bộ |
| 6.8 | **Lập lịch (Schedule)** | `views/videoWall/schedule` | ⚠️ Có UI, **CHƯA CÓ WORKER** | CRUD cấu hình lịch (Hàng ngày, Hàng tuần, Một lần, Cron). 🔴 BE chưa có Background Scheduler Engine thực thi kích hoạt tự động |
| 6.9 | **Tích hợp ITS (ITS Integration)** | `views/videoWall/itsIntegration` | ⚠️ Có UI, **ĐANG SỬA** | Quy tắc sự kiện TMS (ACCIDENT, CONGESTION...). ⚠️ Nút chạy thử đang phụ thuộc quy tắc chọn của BE (chọn theo ngày tạo thay vì trường Priority); FE sort theo chữ cái |
| 6.10 | **Phân quyền vùng (Wall Permission)** | `views/videoWall/wallPermission` | ✅ Đã có UI | Bảng danh sách kèm `wallMiniGrid.vue`, modal `editWallPermission.vue` kéo chuột chọn khối chữ nhật, chọn cột/hàng, gán User/Org |

---

## 5. Hiện Trạng Tầng Backend, Worker Service & Kiểm Thử

### 5.1. Backend WebAPI (`Module.VideoWall`)
- **Entities & Write Path:** 13 entities (`VwController`, `VwControllerSlot`, `VwScreen`, `VwSlotPort`, `VwSource`, `VwScene`, `VwWindowScene`, `VwSchedule`, `VwEventRule`, `VwWallPermission`, `VwWallTopology`, `VwEventTriggerLog`).
- **Dịch vụ phân quyền `VwPermissionService`:**
  - `GetCoveredCells`: Tính toán chính xác các ô lưới phủ bởi cửa sổ (toạ độ chia cho 3840 và 2160).
  - `IsWithinAllowedCells`: Kiểm tra mọi ô phủ có nằm trong tập ô được cấp không (lấn 1 px cũng bị chặn).
  - `GetEffectivePermissionAsync` & `GetMyAccessAsync`: Trả quyền hiệu lực chuẩn xác cho user đang đăng nhập.
  - Hỗ trợ cờ bypass `VideoWall:BypassPermission` (chỉ cho phép ở Non-Production).
- **Controller & API Endpoints:** Đầy đủ CRUD + các endpoint nghiệp vụ nâng cao (`VwScene/Active`, `VwScene/SetDefault`, `VwWallPermission/GetMy`, `VwDeviceSetup/*`).

### 5.2. VideoWall Worker Service (`ITS.VideoWall` & `ITS.VideoWall.Core`)
- **Kết nối Hikvision ISAPI:** Giao tiếp qua `HttpClient` với Digest Authentication, xử lý retry theo cơ chế Exponential Backoff, phát hiện lỗi xác thực thiết bị và khóa tạm thời (`DeviceAuthFailure`).
- **NATS Messaging Consumer:** Lắng nghe kênh `ta.its.data.videowall.control` để xử lý các lệnh:
  - `ActivateScene`: Kích hoạt kịch bản, gọi ISAPI sang bộ điều khiển trung tâm, deactivate các window không còn thuộc scene mới.
  - `SetupScene`: Đồng bộ layout cửa sổ.
  - `SyncSources`: Quét cổng input vật lý và camera IP.
  - `Probe`: Kiểm tra cấu hình và trạng thái thiết bị.
- **Heartbeat & Telemetry:** Định kỳ gửi heartbeat telemetry lên kênh `ta.its.data.videowall` cập nhật trạng thái online/offline của Controller và Screen.

### 5.3. Bộ Kiểm Thử Tự Động (`tests/BE/ITS/VideoWall`)
- Đã được tái cấu trúc hoàn chỉnh vào ngày 30/09/2026:
  - Phân chia 3 nhóm: `WebApi/` (Controllers, Services), `Worker/` (ISAPIDevice, Heartbeat, Messaging, Scene), `Wpf/` (Local Stores, Scenario, Cascade).
  - Sử dụng Mock Server Hikvision chuẩn hóa `VwISAPIServerHikvisionMock` (14 tệp partial phụ trách Auth, Routes, Scene, Window, Signaling, Decoding, Board...).
  - Không dùng try-finally trong test, không tự xóa DB trong từng test method (tuân thủ mục 6 và 19 của `thienan_rules.md`).
  - Toàn bộ các project của VideoWall (`Module.VideoWall`, `ITS.VideoWall`, `test.dll`) build hoàn toàn sạch lỗi.

---

## 6. Review & Audit Chi Tiết Đợt Triển Khai Backend (Hợp Nhất Từ Review 14-09-2026)

> 📌 Phần này lưu trữ nguyên vẹn các kết quả đối chiếu kỹ thuật chuyên sâu từ đợt triển khai lớn ban đầu (commit `01f724d3` và 2 prompt bổ sung) để phục vụ tra cứu lịch sử và kiểm chứng mã nguồn mà không cần duy trì file review riêng lẻ.

### 6.1. Kết Quả Đối Chiếu 9 Hạng Mục Prompt Đã Giao

| # | Hạng mục | Trạng thái | Bằng chứng mã nguồn thực tế |
|---|---|---|---|
| 1 | `LoadController` chặn gọi ISAPI vào `Role=="sub"` | ✅ **Đúng** | `VwISAPIDeviceService.DeviceSetup.cs:1125-1126` — throw ngoại lệ rõ ràng, là chokepoint dùng chung cho Ping/Probe/SyncSources/SyncActiveScene/SetupScene/Passthrough. |
| 2 | `SyncActiveSceneCore` sửa bug deactivate chéo controller | ✅ **Đúng** | Dùng đúng pattern HashSet `activeSceneIds` theo từng controller trước khi deactivate, khớp 100% với pattern gốc ở `VwCommandConsumer.HandleActivateSceneAsync:262-294`. |
| 3 | `VwEventTriggerLog` giữ tên cột `TargetSceneId` | ✅ **Đúng** | Entity (dòng 38) + 4 call site (2 Writer, QueryHandler, comment PermissionService) đều nhất quán. |
| 4 | Đổi tên CircuitBreaker → DeviceAuthFailure | ✅ **Đúng** | Đủ 14/14 file (cache key + class + method), 0 chỗ còn sót chuỗi "circuitbreaker" trong code nguồn. |
| 5 | Thêm `VwController.ParentControllerId` (multi-wall) | ✅ **Đúng** | Entity (dòng 208-215) + Validator (dòng 127-150) đủ 2 chiều: center bắt buộc rỗng, sub bắt buộc trỏ tới 1 center tồn tại. |
| 6 | Chuyển DTO ISAPI ra khỏi `Module.VideoWall.Core` | ✅ **Đúng** | Project mới `ITS.VideoWall.Core` (tầng Worker) chứa toàn bộ DTO ISAPI, tham chiếu MỘT CHIỀU vào `Module.VideoWall.Core` — đúng hướng phụ thuộc. |
| 7 | Sửa comment cũ nhắc `DeviceIntegration.json` | ⏭️ **N/A** | Chuỗi này không tồn tại trong code hiện tại (chỉ có ở nhánh phụ bị bỏ `feat/20260819/videowall_device`). |
| 8 | Gom/bỏ test trùng lặp (theory/InlineData) | ✅ **Đúng** | Đã thực thi qua prompt riêng, giảm từ 432 → 382 test case, PASS 100%. |
| 9 | Bỏ reference `Shared.*` dư thừa khỏi `Module.VideoWall.Core.csproj` | ❌ **Chưa làm** | File `.csproj` còn giữ `Shared.DTO`/`Shared.Reference`/`Shared.Utility` + 3 HintPath DLL cũ (chuyển vào GAP-08). |

### 6.2. Đối Chiếu Kiến Trúc Phần Cứng Cascade DS-C66S
- **Bất biến trung tâm/con:** 1 bộ điều khiển TRUNG TÂM nói ISAPI, 3 bộ CON không có traffic ISAPI (chỉ nối vật lý qua HDMI + GENLOCK). Không tồn tại khái niệm master/slave trong giao thức ISAPI Hikvision DS-C66S.
- **Không còn lệch luồng:** `SyncActiveSceneCore` và `HandleActivateSceneAsync` cùng dùng cấu trúc HashSet `activeSceneIds` theo từng controller trước khi deactivate scene cũ.
- **Phân cấp tối đa 2 tầng:** `VwController.ParentControllerId` ép cứng: center luôn rỗng, sub bắt buộc trỏ tới center, không có tầng 3.
- **Trạng thái màn hình:** `VwScreen.ScreenState` là kiểu `string?` (`Online`/`Offline`/`Warning` hằng số theo `BaseEnums.ScreenState`).
- **Khoảng trống:** `VwControllerValidator.cs` chưa có rule chặn tạo 2 controller cùng `Role=="center"` (chuyển vào GAP-07).

### 6.3. Tiến Trình Thực Thi 2 Prompt Bổ Sung (14/09 & 16/09/2026)
1. **Prompt 1 — Chuẩn hóa NATS Subject:**
   - FE đã hoàn tất đổi subject sang `ta.its.data.videowall` tại `Nats.subjects.json:44` và `transporterEvent.ts:43`.
   - `transporterNats.ts` đã phân nhánh switch `EventType`: `SceneActivated`, `HardwareOutOfSync`, `DeviceHeartbeat`, `DeviceProbeCompleted`.
   - Comment rác `VwSceneController.cs:112` còn sót lại đã được ghi nhận vào GAP-02.
2. **Prompt 2 — Rà soát & Tối ưu hóa Bộ Kiểm Thử:**
   - Đã gộp và xóa các InlineData trùng lặp tại 8 test class (`VwControllerTests`, `VwEventRuleTests`, `VwSceneTests`, `VwScheduleTests`, `VwScreenTests`, `VwSlotPortTests`, `VwSourceTests`, `VwWindowSceneTests`).
   - Đưa bộ test về 382 test case chạy xanh 100% trước khi thực hiện tiếp đợt refactor cấu trúc thư mục ngày 30/09/2026.

---

## 7. Danh Sách Tồn Đọng, Lỗi Runtime & Khoảng Trống (Gaps L-01 $\rightarrow$ L-16)

| Mã Gap | Mô tả chi tiết | Tầng bị ảnh hưởng | Mức độ ưu tiên |
|---|---|---|---|
| **GAP-01** | **Lỗi đồng bộ Real-time trên Monitor:** Worker sau khi gọi ISAPI kích hoạt scene thành công, phát thông báo NATS nhưng các client đang mở màn Giám sát không tự nhảy sang kịch bản đang phát (được ghi nhận trong Đặc tả UI). | Worker / NATS / FE | 🔴 **P0 (Khẩn)** |
| **GAP-02** | **Comment rác `VwSceneController.cs:112`:** Vẫn còn comment mô tả kênh NATS là `ta.its.data.videowallScene` thay vì `ta.its.data.videowall`. | BE WebAPI | 🟡 **P1 (Nhanh)** |
| **GAP-03** | **Thiếu Background Scheduler Engine cho `VwSchedule`:** Màn hình và bảng `VwSchedule` đã có nhưng chưa có Hangfire job / Worker service quét các lịch Bật để tự động kích hoạt scene theo giờ/ngày/tuần/cron. | BE Worker | 🔴 **P1 (Cốt lõi)** |
| **GAP-04** | **Chuẩn hoá cơ chế ưu tiên kích hoạt ITS EventRule:** Khi nhiều quy tắc cùng khớp loại sự kiện TMS, BE đang chọn theo bản ghi tạo sớm hơn thay vì trường `Priority`. FE cần sắp xếp cột Ưu tiên theo cấp bậc số thay vì chữ cái. | BE WebAPI & FE | 🟡 **P1 (Nghiệp vụ)** |
| **GAP-05** | **Thiếu UI cho nhóm chức năng `DeviceSetup`:** BE đã có API Ping, Probe, SyncSources, ResetDeviceAuthFailure nhưng trang Controllers trên FE chưa có nút bấm tương ứng để kỹ thuật viên thao tác. | FE WebVue | 🟡 **P2 (Tính năng)** |
| **GAP-06** | **Lưu kịch bản chưa nguyên tử (L-13):** FE đang gọi tuần tự API update Scene rồi lặp qua các Window để thêm/sửa/xóa. Nếu rớt mạng giữa chừng sẽ lưu dở dang. Cần bổ sung API Batch Upsert Scene kèm Windows trong 1 Transaction. | BE WebAPI & FE | 🟡 **P2 (Kiến trúc)** |
| **GAP-07** | **Validator hạn chế 2 bộ điều khiển `Role == "center"`:** Chưa có rule kiểm tra chặn người dùng cấu hình 2 bộ điều khiển cùng là Trung tâm trên một hệ thống tường. | BE WebAPI | 🟢 **P3 (Validation)** |
| **GAP-08** | **Dọn reference thừa `Shared.*` trong `Module.VideoWall.Core.csproj`:** Project `.csproj` còn giữ reference tới `Shared.DTO`, `Shared.Reference`, `Shared.Utility` và HintPath DLL cũ chưa dọn dẹp. | BE WebAPI (.csproj) | 🟢 **P3 (Dọn dẹp)** |

---

## 8. Backlog & Kế Hoạch Triển Khai (Action Plan)

### Giai đoạn 1: Sửa Lỗi Vận Hành & Khắc Phục Điểm Nghẽn (P0 / P1)
1. **[P0] Điều tra và khắc phục lỗi đồng bộ Real-time trên Monitor (`GAP-01`):**
   - Rà soát sự kiện NATS do `VwCommandConsumer` / Worker phát sau khi `ActivateScene` hoàn tất.
   - Kiểm tra `useTransporterVideoWallScene.ts` và `transporterNats.ts` trên FE xem có nhận được payload `SceneActivated` và emit sang `mittBus` để cập nhật Store hay không.
2. **[P1] Sửa comment rác trong `VwSceneController.cs:112` (`GAP-02`):**
   - Đổi `ta.its.data.videowallScene` $\rightarrow$ `ta.its.data.videowall`.
3. **[P1] Xây dựng Background Scheduler Engine cho VideoWall (`GAP-03`):**
   - Bổ sung Background Service định kỳ (hoặc Hangfire Job) trong `ITS.VideoWall` để quét bảng `VwSchedule`.
   - Tính toán `NextRunTime` dựa trên cấu hình (Daily, Weekly, Once, Cron) và gọi lệnh kích hoạt scene khi đến hạn.
4. **[P1] Chuẩn hoá bộ quy tắc ưu tiên sự kiện ITS (`GAP-04`):**
   - Cập nhật truy vấn `VwEventRule` ở BE ưu tiên theo thứ bậc: `CRITICAL` > `HIGH` > `NORMAL` > `LOW`.
   - Điều chỉnh giao diện FE để hiển thị và sắp xếp đúng cấp độ ưu tiên.

### Giai đoạn 2: Bổ Sung Tính Năng & Hoàn Thiện Trải Nghiệm (P2)
5. **[P2] Bổ sung nút thao tác DeviceSetup trên màn hình Controllers (`GAP-05`):**
   - Thêm dropdown/nút hành động: *Kiểm tra kết nối (Ping)*, *Thăm dò thiết bị (Probe)*, *Đồng bộ nguồn tín hiệu (Sync Sources)*, *Mở khóa xác thực (Reset Auth Failure)*.
6. **[P2] API Giao dịch nguyên tử cho Kịch bản & Cửa sổ (`GAP-06`):**
   - Tạo endpoint `POST /api/VwScene/BatchSave` nhận toàn bộ thông tin kịch bản + danh sách cửa sổ trong 1 payload.
   - Thực thi lưu trong một SqlSugar transaction `UseTran` duy nhất, đảm bảo tính toàn vẹn dữ liệu.

### Giai đoạn 3: Tối Ưu Hóa & Chuẩn Hóa Cấu Trúc (P3)
7. **[P3] Bổ sung Validator chặn tạo 2 Controller `Role == "center"` (`GAP-07`):**
   - Thêm rule trong `VwControllerValidator.cs`.
8. **[P3] Dọn dẹp reference thừa trong `Module.VideoWall.Core.csproj` (`GAP-08`):**
   - Gỡ bỏ các dòng `<ProjectReference>` và `<Reference>` không cần thiết.

---

## 9. Danh Mục Tài Liệu Liên Quan

- **Kiến trúc thiết bị:** [`../doc/KienTruc_VideoWall_DS-C66S-Cascade.md`](../doc/KienTruc_VideoWall_DS-C66S-Cascade.md)
- **Đặc tả UI & Kiểm thử:** [`../DacTa/VideoWall-DacTa-UI_1.md`](../DacTa/VideoWall-DacTa-UI_1.md)
- **Kịch bản test API 1 controller / 12 màn:** [`../doc/KichBan/KichBan_VideoWall_DS-C30S-S11_12Man.md`](../doc/KichBan/KichBan_VideoWall_DS-C30S-S11_12Man.md)
- **Kịch bản test API cascade / 32 màn:** [`../doc/KichBan/KichBan_VideoWall_DS-C66S_4Controller_32Man.md`](../doc/KichBan/KichBan_VideoWall_DS-C66S_4Controller_32Man.md)
- **Transcript cuộc họp:**
  - [`../doc/transcript/09-09-2026-videowall-phan-quyen-va-layout.md`](../doc/transcript/09-09-2026-videowall-phan-quyen-va-layout.md)
  - [`../doc/transcript/11-09-2026-videowall-script.md`](../doc/transcript/11-09-2026-videowall-script.md)
- **Bảng CSDL & Audit Write Path:**
  - [`../doc/TableSQL/Vw_Tables_Analysis_And_Design.md`](../doc/TableSQL/Vw_Tables_Analysis_And_Design.md)
  - [`../doc/TableSQL/Vw_Entities_WritePath_Audit_2026-09-14.md`](../doc/TableSQL/Vw_Entities_WritePath_Audit_2026-09-14.md)
