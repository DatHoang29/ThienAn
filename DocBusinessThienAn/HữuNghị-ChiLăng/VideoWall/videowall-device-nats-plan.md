# Đối chiếu nhánh `feat/20260909-videowall-device` với chốt họp 2026-09-09 — Plan họp trao đổi

## Context

Buổi họp 2026-09-09 (`doc/transcript/2026-09-09-videowall-phan-quyen-va-layout.md`) chốt 4 định hướng kiến trúc cho VideoWall: (1) nguyên tắc mở rộng Entity, (2) phân quyền 3 tầng theo khu vực hiển thị, (3) kiến trúc 2 tầng Control/Service qua NATS, (4) SqlSugar `ToTree` + trang Service Management Hub. Transcript đã được bổ sung phần kỹ thuật chi tiết (mục 2 trong transcript): tên topic NATS cụ thể, cấu trúc gói tin, schema BoundingBox theo user, spec cột `ScreenConfig`, và phạm vi thật của Service Hub — nhiều câu hỏi mở trước đó nay đã có đáp án rõ, plan này cập nhật lại theo đó.

Nhánh làm việc thực tế là repo lồng `TA-ITS015-WEBAPI-V1.0` (chưa track trong repo cha), branch `feat/20260909-videowall-device`. **Lịch sử commit đã được dọn lại (10/09/2026)**: HEAD hiện quay về `ad961409` (commit nền từ `dev`, trước khi có việc VideoWall) — toàn bộ khối lượng việc cascade DS-C66S + NATS + phân quyền tầng 3 mô tả trong plan này **đang ở dạng thay đổi chưa commit** (staged + working tree), không nằm trong lịch sử chính thức nào. Đây là chủ đích dọn dẹp để commit lại 1 lần sạch, không phải mất dữ liệu.

Mục tiêu tài liệu này: liệt kê rõ **cái gì đã có / cái gì chưa có / câu hỏi cần chốt** so với 4 định hướng trên, để dùng làm agenda họp — không phải để code ngay.

---

## 1. Đối chiếu nhanh

| Hạng mục | Trạng thái | Chốt ở họp |
|---|---|---|
| **Entity** | `VwWallTopology` có sẵn từ trước, không phải mới — nhánh này chỉ thêm CRUD API quanh nó, chưa mở rộng đa tường (ControllerId/WallNo). `VwController` thêm đúng 2 field mới (`Role`, `IntegrationMode`) — đúng nguyên tắc. `VwScreen` đã bỏ `PosX/PosY` (ngoại lệ, đã có script drop-column) và vẫn thiếu `ConfigJson`/`ScreenConfig`. | Duyệt ngoại lệ xoá `PosX/PosY`; có cần `ConfigJson` ngay không. |
| **Phân quyền 3 tầng** | Tầng 1 (Org, `VwOrgAccessService`/`VwOrgScope`) đã có nhưng đang bypass toàn bộ (`AllowAnonymous` + `BypassPermission`). Tầng 3 nay có spec cụ thể từ transcript: `allowedArea` (colStart/colEnd/rowStart/rowEnd) lưu theo `userId` + hàm kiểm tra hình học — field/bảng MỚI. Đối chiếu với `KienTruc_VideoWall_DS-C66S-Cascade.md`: 4 field `OriginCol/OriginRow/CoverCols/CoverRows` trên `VwController` khớp đúng bảng vùng phủ 3 bộ con ở mục 3 tài liệu đó (Con1 cột1-4, Con2 cột5-6, Con3 cột7-8) — tức đây là **3 vùng CỐ ĐỊNH theo ranh giới phần cứng**, khác hẳn Tầng 3 cần BoundingBox **tuỳ ý theo từng user** (không bị ràng buộc ranh giới bộ con). Lưới toạ độ đúng để dùng cho Tầng 3 là **8 cột × 4 hàng** (tài liệu kiến trúc xác nhận, transcript trước đó ghi mập mờ "8x5 hoặc 8x4"). **Không phụ thuộc KB-00/WallNo** (chốt 10/09/2026, xem mục 2.1): `allowedArea` dùng chung hệ toạ độ toàn cục với `VwWindowScene.X/Y/W/H` (pixel tuyệt đối, không có `wallNo`) — kiểm tra quyền ở cùng tầng với `EnsureWindowInsideSceneRegionAsync`, TRƯỚC bước dịch `ToCenterUniformRect` sang toạ độ riêng thiết bị. **Chốt schema lưu trữ, đã code xong (10/09/2026)**: bảng mới `VwUserAreaPermission` (`UserId`, `ColStart/ColEnd/RowStart/RowEnd`, `Description`, `OrgId`, `EntityTenant` chuẩn) — entity + `VwUserAreaAccessService` đã có, đã wire vào `VwWindowSceneCommandHandler` (xem mục 4). Tầng 2 (User override) vẫn chưa có thiết kế cụ thể, transcript chỉ nêu ví dụ use-case (Trưởng ca vs nhân viên trực ca). | Xác nhận lưới 8×4; duyệt schema `VwUserAreaPermission`; schema/actor cho tầng 2; mốc gỡ bypass. |
| **Kiến trúc NATS Control/Service** | **Cập nhật tiến độ thật (10/09/2026, code đã ngưng đổi)** — đã code khá nhiều, xem chi tiết mục 2.2 bên dưới. Tóm tắt: chiều publish lệnh (WebAPI→NATS) **đã xong và đã wire vào command handler thật**; khung project Worker riêng (`ITS.VideoWall` + `ITS.VideoWall.WPF`) **đã tạo**; nhưng **chưa có ai subscribe/tiêu thụ** `ta.its.data.videowall.cmd` — lệnh phát ra hiện đi vào khoảng không, thiết bị thật CHƯA đổi. Cầu nối Backend→Web Vue (SocketCluster) đã có sẵn từ trước, không phải xây. | Tách Worker riêng hay tạm publish trong WebAPI; làm ngay hay sau khi xong đấu nối hiện trường. |
| **`ToTree` + Service Hub** | `ToTree` ví dụ trong transcript dùng bảng `Zone` — đây là entity của module TMS (trạm/tuyến giao thông), không liên quan VideoWall; VideoWall chưa có cây phân cấp nào để áp dụng. Service Hub transcript xác nhận là hạ tầng **dùng chung toàn hệ thống** (VideoWall, Toll, ShareData, VDS, VMS...), không phải việc riêng của VideoWall — chưa có gì, phụ thuộc NATS ở trên. | VideoWall có cây phân cấp nào cần `ToTree` không (nhiều khả năng không); Service Hub module/đội nào chịu trách nhiệm xây — VideoWall chỉ là 1 consumer publish heartbeat vào đó? |

---

## 2. Việc dở dang trước đó — đã chốt hướng làm, người dùng tự code

1. **KB-00 probe** thực địa trên 1 khung DS-C66S thật để đo `centerCanvas`, `SignalNo`, model, số wall-logic → điền seed. Chưa chốt hướng.
   - **Đã gỡ phụ thuộc ngược với Tầng 3 phân quyền** (chốt 10/09/2026): ban đầu tưởng phải mở rộng `VwWallTopology` thêm `WallNo` ("Giai đoạn B") TRƯỚC khi build BoundingBox tầng 3, vì lo bộ trung tâm có thể phải khai nhiều wall-logic (ngoại lệ mục 6 `KienTruc_VideoWall_DS-C66S-Cascade.md`). Thực ra không cần chờ: `VwWindowScene.X/Y/W/H` hiện đã lưu **pixel tuyệt đối trên canvas toàn tường** (không có `wallNo`), quy đổi sang toạ độ riêng từng wall-logic chỉ xảy ra muộn nhất có thể, ngay trước khi gọi ISAPI, trong `VwSceneRegionService.ToCenterUniformRect`.
   - **Thiết kế**: `allowedArea` của tầng 3 định nghĩa theo ĐÚNG hệ toạ độ toàn cục đó (col/row 8×4 hoặc pixel tuyệt đối, không có trường `wallNo`), và kiểm tra quyền chạy ở CÙNG chỗ/cùng thời điểm với `EnsureWindowInsideSceneRegionAsync` hiện tại — tức là trước bước `ToCenterUniformRect`. Nhờ vậy build tầng 3 ngay được, không cần chờ KB-00; KB-00 xong dù ra 1 hay N wall-logic thì chỉ sửa `ToCenterUniformRect` + `VwWallTopology.WallNo` ("Giai đoạn B"), tầng phân quyền không đổi gì (nó chưa từng biết khái niệm `wallNo`).
2. **Tiến độ NATS thật, đối chiếu code (10/09/2026, code đã ngưng đổi)**:
   - **Đã xong**: `IVwPublisher`/`VwPublisher` (`Infrastructure/Services/Messaging/`), `VwCommandEnvelope`/`VwCommandActions`/payload DTOs (`Dto/Command/`) — publish qua `ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.VideoWallCommand, ...)`, subject `ta.its.data.videowall.cmd` (+ `ta.its.data.videowall.telemetry` đã khai sẵn trong `DataTransporter.json`, chưa có ai publish/subscribe). Đã **wire xong và bỏ hẳn** các lệnh gọi `IVwISAPIDeviceService` trực tiếp trong `VwWindowSceneCommandHandler` + `VwSceneWorkflowCommandHandler` — thay bằng `_publisher.PublishCommand(...)` fire-and-forget SAU khi ghi DB, đúng "Luồng A" đã bàn.
   - **Đã tạo khung Worker riêng**: `src/Services/VideoWall/ITS.VideoWall` (class library, copy `VwISAPIDeviceService*`/`VwISAPIDigestHandler` sang) + `src/Services/VideoWall/ITS.VideoWall.WPF` (WPF app đầy đủ, mirror từ `Module.VideoWall.WPF`, cấu trúc giống `ITS.VDS.Core`+`ITS.VDS.Wpf`). Lưu ý: `ITS.VideoWall` reference `Modules.DataTransporter.ITS` (cùng `ItsDataTransporter` dùng chung toàn hệ thống) — **khác** gợi ý trước đó theo mẫu VDS dùng `Modules.DataTransporter.Natsio`/`TransportManager` nhẹ hơn; đây là lựa chọn hợp lý để tái dùng đúng 1 cơ chế broadcast cho cả NATS+SocketCluster, nên nêu ở họp để xác nhận đây là chủ đích chứ không phải nhầm lẫn.
   - **⚠️ Gap quan trọng nhất — chưa hoàn chỉnh, đừng tưởng đã chạy được**: `ITS.VideoWall` **CHƯA có chỗ nào gọi `ItsDataTransporter.ConsumeData(VideoWallCommand, ...)`** — tức là lệnh publish ra `ta.its.data.videowall.cmd` hiện **không có ai nhận và xử lý**, thiết bị thật CHƯA đổi khi bấm nút trên UI. Cần thêm 1 entry point (BackgroundService/Program.cs) trong `ITS.VideoWall` subscribe subject này rồi gọi `VwISAPIDeviceService` tương ứng theo `VwCommandActions`.
   - **⚠️ Bug tiềm ẩn phát hiện khi rà code**: `VwSceneWorkflowCommandHandler.BroadcastSceneActivated` (báo FE "scene đã đổi, tự fetch lại") vẫn được gọi ngay sau khi publish NATS **thành công** (`success: published`), KHÔNG phải sau khi thiết bị xác nhận thật — nghĩa là FE sẽ được báo "xong" ngay cả khi Worker chưa tồn tại/chưa xử lý lệnh. Cần dời việc gọi broadcast này sang thời điểm Worker báo kết quả thật qua NATS (Luồng B), không gọi ngay tại chỗ publish nữa.
3. **Quy ước panel px** — Chốt: sửa backend trước, FE để sau. **Đã xong sẵn**: `VwWallProfile.PanelWidthPx/PanelHeightPx` đã là `1920×1080` (đổi 09/09/2026, comment ghi rõ giá trị 3840×2160 cũ đã bỏ) — không có việc gì thêm ở backend. `wallConstants.ts` (FE) vẫn `3840×2160`, cố ý chưa động theo mục 7.
4. **Seed `docs/sql/2026-09-08-seed-videowall-cascade.sql`** — Chốt: KHÔNG chạy tay script, thay bằng viết test case kiểm chứng đúng các bất biến mà BƯỚC 7 của script định kiểm tra tay. Đề xuất cụ thể để tự code theo pattern có sẵn:
   - `tests/Modules/VideoWall/Controllers/VwSourceTests.cs`: 2 test mới cho `VwSourceCommandHandler.EnsureLocalSourceOnCenterAsync` (09/09/2026) — nguồn `hdmi_in` bỏ trống `ControllerId` tự gán bộ trung tâm; trỏ thẳng vào bộ con (`Role=sub`) bị `VwAddSourceValidator` từ chối. Theo đúng pattern `new VwAddSourceValidator(_localizer)` + `_bus.InvokeAsync(input)` đã dùng trong file.
   - `tests/Modules/VideoWall/Infrastructure/Services/VwCascadeIntegrationTests.cs`: 1 test kiểu `D13_...` dựng đúng `VwWallTopology` (8×4, 1920×1080) + `VwScene` toàn tường, lặp qua toạ độ 21 cửa sổ y hệt seed (1 ITS + 20 camera viền) gọi `VwSceneRegionService.EnsureWindowInsideSceneRegionAsync` — không cửa sổ nào bị ném ngoại lệ tràn canvas 15360×4320 (mirror check thứ 2 của BƯỚC 7).
5. Commit thay đổi backend (hiện đang staged) — **người dùng tự làm**, AI không đụng git.
6. **WPF `SceneSetupViewModel.cs`** — Chốt: fix cho đồng bộ, vẫn test được khi chưa có thiết bị thật. **Đã xong** (logic đã kiểm chứng lúc còn ở `Module.VideoWall.WPF`, nay đã chuyển hẳn sang `ITS.VideoWall.WPF`): mọi nơi thật sự gọi lệnh thiết bị đều đã dùng `WallNo ?? _connection.WallNo ?? _connection.ProbeResult?.BoundWallNo` kèm guard báo lỗi thay vì im lặng mặc định 1. `BuildFallbackScreens(cols, rows, tile)` cũng đã có sẵn, thay hết danh sách "Màn 1..12" cứng cũ. Chỉ còn 1 chỗ dùng `_connection.WallNo ?? 1` (hàm `SeedSampleScenes`, tạo dữ liệu mẫu cục bộ) — đúng ý đồ, không phải lỗi. **✅ Hết trùng lặp (10/09/2026)**: `Module.VideoWall.WPF` (project cũ) đã bị **xoá hẳn khỏi ổ đĩa** — chỉ còn duy nhất `ITS.VideoWall.WPF`, không còn 2 project song song nữa, không cần chốt gì thêm ở mục này.
7. Frontend `TA-ITS015-WEBVUE-V1.0` — Chốt: khoan động vào, giữ nguyên `wallConstants.ts` hardcode 8×4/3840×2160 cho tới khi có quyết định khác.

---

## 3. Đề xuất Agenda họp (thứ tự đề xuất)

1. Chốt **sequencing tổng thể**: hoàn tất đấu nối hiện trường dở dang (mục 2) trước, rồi mới tách kiến trúc NATS — hay làm song song? **(chưa ai trả lời)**
2. **Đã chốt & đã code xong (10/09/2026)** nơi lưu `allowedArea` tầng 3: bảng mới `VwUserAreaPermission` (không dùng field trên `VwController` — 4 field đó là vùng cố định của 3 bộ con, khác BoundingBox tuỳ ý theo user) — họp chỉ cần duyệt lại schema, không còn là quyết định mở.
3. Chốt **schema/actor cho tầng 2** (User override) — vai trò nào cần override, lưu ở đâu? **(chưa ai trả lời)**
4. Chốt **mốc gỡ `AllowAnonymous` + `BypassPermission`** — điều kiện nào coi là xong đấu nối? **(chưa ai trả lời)**
5. **Đã code, không còn là câu hỏi (10/09/2026)**: kiến trúc Service Worker — **đã tách project riêng** `ITS.VideoWall`, publish lệnh qua NATS đã wire (Luồng A), nhưng Worker CHƯA subscribe/xử lý lệnh (gap lớn nhất, xem mục 4). Họp chỉ cần biết tiến độ này, không cần quyết định hướng nữa.
6. **Đã có đáp án, đã dùng thật trong code (10/09/2026)**: topic NATS theo `ta.its.data.videowall.*` — không chỉ là quyết định, đã triển khai: `ta.its.data.videowall.scene` (đã có từ trước), `ta.its.data.videowall.cmd`/`ta.its.data.videowall.telemetry` (mới thêm 10/09/2026) — xem chi tiết mục 2.2 và 4.
7. **Không còn là câu hỏi (10/09/2026)**: cầu nối Backend → Web Vue **đã có sẵn** qua `SocketClusterEventHandler` (FE đã có client `transporterSocket.ts`) — dùng chung cơ chế `ItsDataTransporter.BroadcastData` với NATS, không cần chọn SSE hay WebSocket, không cần xây gì mới.
8. Chốt **phạm vi & chủ sở hữu Service Management Hub**: hạ tầng dùng chung toàn hệ thống, không riêng VideoWall — module/đội nào xây, VideoWall chỉ publish heartbeat vào đó? **(đã hoãn làm, nhưng câu hỏi "ai" vẫn treo)**
9. Xác nhận **`ToTree`** áp dụng cho cây nào trong VideoWall, nếu có (transcript chỉ ví dụ bằng `Zone` của TMS, không phải entity VideoWall). **(chưa ai trả lời, nhiều khả năng là "không cần")**
10. Xác nhận **ngoại lệ xoá `PosX/PosY`** trên `VwScreen` (đã có sẵn `docs/sql/2026-08-26-drop-vwscreen-posx-posy.sql`, chỉ chờ duyệt chạy tay) — chấp nhận được so với nguyên tắc "chỉ thêm field mới"? **(chưa ai trả lời)**
11. Xác nhận **`ConfigJson`/`ScreenConfig`** trên `VwScreen`: có cần ngay không, hay chờ yêu cầu cụ thể (tránh lặp bài học `PosX/PosY` — field tạo ra nhưng chưa từng dùng)? **(chưa ai trả lời)**

---

## 4. Tiến độ code thật (10/09/2026 — code đã ngưng đổi, đối chiếu lần cuối)

**Đã xong:**
1. Worker project riêng: `ITS.VideoWall` (class library, copy `VwISAPIDeviceService*`/`VwISAPIDigestHandler` sang) + `ITS.VideoWall.WPF` (WPF app đầy đủ) — đúng cấu trúc mẫu VDS (`ITS.VDS.Core`+`ITS.VDS.Wpf`).
2. Kênh NATS: `IVwPublisher`/`VwPublisher`, `VwCommandEnvelope`, `VwCommandActions` — publish qua `ItsDataTransporter.BroadcastData`, subject `ta.its.data.videowall.cmd`/`ta.its.data.videowall.telemetry` đã khai trong `DataTransporter.json`, đúng convention `ta.its.data.<domain>.<subtype>` sẵn có (KHÔNG dùng `vw.*` như transcript §2.2/2.3 đề xuất — transcript sai so với thực tế).
3. **Luồng A (gửi lệnh, fire-and-forget)**: `VwWindowSceneCommandHandler` + `VwSceneWorkflowCommandHandler` đã bỏ hẳn gọi `IVwISAPIDeviceService` trực tiếp, thay bằng `_publisher.PublishCommand(...)` ngay SAU khi ghi DB — đúng thiết kế đã bàn.
4. **Tầng 3 phân quyền đã tích hợp vào luồng** (sớm hơn dự kiến ban đầu — không phải "để sau cùng" nữa): `VwUserAreaAccessService.EnsureWindowInsideUserAllowedAreaAsync` đã gọi ngay trong `VwWindowSceneCommandHandler`, đúng chỗ/đúng thứ tự đã thiết kế (mục 2.1).

**Chưa xong — gap thật cần làm tiếp, theo thứ tự ưu tiên:**
1. **Worker chưa subscribe** — `ITS.VideoWall` chưa có bất kỳ `ItsDataTransporter.ConsumeData(VideoWallCommand, ...)` nào. Đây là mảnh ghép còn thiếu để lệnh thật sự chạm tới thiết bị — thiếu nó thì mọi lệnh publish ở trên đi vào khoảng không, **thiết bị không đổi gì**.
2. **Bug cần sửa**: `BroadcastSceneActivated` đang báo FE "xong" ngay sau khi publish NATS thành công, chưa đợi thiết bị xác nhận thật (chi tiết mục 2.2) — dễ gây hiểu lầm "đã chạy được" trong lúc demo/test.
3. **Luồng B (heartbeat/telemetry)** — chưa có publisher (Worker chưa tự gọi ISAPI định kỳ 10-30s) lẫn subscriber (WebAPI chưa cache trạng thái từ `ta.its.data.videowall.telemetry`).
4. **Luồng C (Request-Reply)** — chưa làm, không gấp (theo transcript chỉ cần khi hỏi trạng thái tức thời, không phải cho gửi lệnh).

~~5. WPF trùng lặp~~ — **đã tự giải quyết (10/09/2026)**: `Module.VideoWall.WPF` (cũ) đã bị xoá, chỉ còn `ITS.VideoWall.WPF` (mục 2.6).

---

## 5. Tài liệu/tệp tham chiếu dùng khi họp

- Transcript gốc: `doc/transcript/2026-09-09-videowall-phan-quyen-va-layout.md` (cùng thư mục module này)
- Kiến trúc cascade DS-C66S (nguồn sự thật cho toạ độ lưới + ràng buộc "chỉ nói với bộ trung tâm"): `doc/KienTruc_VideoWall_DS-C66S-Cascade.md` (cùng thư mục module này)
- Phân quyền hiện tại: `VwOrgAccessService.cs`, `VwOrgScope.cs` (đường dẫn ở mục 1.2)
- Entity liên quan: `VwController.cs`, `VwScreen.cs`, `VwWindowScene.cs`
- ISAPI: `Module.VideoWall/Infrastructure/Services/ISAPIDevice/*` (bản gốc, không còn được gọi trực tiếp từ command handler) — đã copy sang `src/Services/VideoWall/ITS.VideoWall/Services/ISAPIDevice/*` (Worker, chưa có consumer gọi tới)
- Kênh NATS VideoWall đang dùng thật: `Module.VideoWall/Infrastructure/Services/Messaging/VwPublisher.cs`, `Module.VideoWall.Core/Dto/Command/{VwCommandEnvelope,VwCommandPayloads}.cs`, `Module.VideoWall.Core/Constants/VwCommandActions.cs`
- Mẫu Control/Service tham khảo: `src/Services/VDS/ITS.VDS.Core` (mẫu kiến trúc, không phải cơ chế NATS đang dùng thật — VideoWall dùng `ItsDataTransporter`, không dùng `TransportManager`/`Modules.DataTransporter.Natsio` như VDS)
- Mẫu `ToTree` có sẵn: `Modules.TMS/.../ZonesQueryHandler.cs`, `Modules.WP/.../MenuQueryHandler.cs`
- Cầu nối Backend→Web Vue (đã có sẵn, KHÔNG phải xây mới): `ItsDataTransporter.BroadcastData` (`Modules.DataTransporter.ITS/Infrastructure/Services/ItsDataTransporter.cs`) fan-out qua mọi `IBaseEventHandler` đăng ký, gồm `NatsEventHandler` và `SocketClusterEventHandler` (`Modules.DataTransporter.Base/Infrastructure/Handlers/`). Ví dụ dùng thật cho VideoWall: `VwSceneWorkflowCommandHandler.BroadcastSceneActivated` (dòng ~339-356), `VwISAPIDeviceService.BroadcastHardwareOutOfSync`. FE Vue client: `TA-ITS015-WEBVUE-V1.0/src/src/transporter/transporterSocket.ts`.
