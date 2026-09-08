---
name: videowall-cascade-status
description: Tiến độ chuyển Module.VideoWall sang cascade DS-C66S và việc còn lại (tính từ 2026-09-08)
metadata: 
  node_type: memory
  type: project
  originSessionId: c445df9a-ca93-4a19-9ec1-f9a7fa8adc40
  modified: 2026-09-08T15:17:28.268Z
---

Tính đến hết ngày **2026-09-08**. Kiến trúc: [[videowall-cascade-architecture]].

**ĐÃ XONG (backend, nhánh `feat/20260819/videowall_device` trong nested repo `d:\ThienAn\TA-ITS015-WEBAPI-V1.0`):**
- Thu tầng điều phối `foreach 4 controllers` → **1 bộ trung tâm**. `VwISAPIDeviceService.ActivateScene` loop các `wallNo` của CÙNG bộ trung tâm; `SyncSceneWindowsToDevice` gửi window theo `VwSceneRegionService.ToCenterUniformRect` (quy đổi tỉ lệ `wallCanvas`↔`centerCanvas`, canvas từ `VwWallTopology` + `GET .../outputs`), bỏ slicing.
- Thêm `VwController.Role` (`center`/`sub`) + `IntegrationMode`; `GetCenterController` throw nếu 0/>1; `VwSceneValidator` chặn `VwScene.ControllerId` trỏ `Role="sub"`.
- `DeviceIntegration.json`: `UseMockDevice:false`, bỏ block IP/Account cứng.
- Cleanup đã áp: hợp nhất chọn-center về `RequireCenterController`/`TryGetCenterController`, bỏ nhánh chết `IntegrationMode == "active"`, bỏ `?? controllers.FirstOrDefault()` nguy hiểm, sửa `BroadcastHardwareOutOfSync` (đừng truyền cùng list cho succeeded+failed).
- Test cascade: `tests/Modules/VideoWall/Infrastructure/Services/VwCascade{Coord,ControllerSelection,RoutingCredential,Integration,SeedData}Tests.cs` + `Wpf/VwWpfCascadeWallTests.cs` + MockServer sửa (`.cs`, `.Router.cs`). `dotnet test tests/test.csproj` **PASS**.
- Seed: `d:\ThienAn\TA-ITS015-WEBAPI-V1.0\docs\sql\2026-09-08-seed-videowall-cascade.sql` (1 VwWallTopology + 4 VwController + 32 VwScreen + 21 VwSource + 1 VwScene + 21 VwWindowScene). **Giá trị `centerCanvas`/`SignalNo`/`Model` còn là PLACEHOLDER. Chưa chạy** (safeguard #3 — chỉ ghi file, chạy tay).

**Trạng thái git:** `TA-ITS015-WEBAPI-V1.0` — mọi thay đổi đang **staged, CHƯA commit**.

**VIỆC CÒN LẠI (2026-09-09 trở đi):**
1. **KB-00 probe** trên 1 khung DS-C66S thật (đóng vai bộ trung tâm) → đo thật `centerCanvas` (từ `GET .../{wall}/outputs`), map `VwSource.SignalNo` (từ `Video/inputs/channels`), `deviceInfo` model, số wall-logic `bound` → điền vào seed `.sql`.
2. **Chốt quy ước panel px:** `.jpg` = 1920×1080; code (`VwWallProfile.PanelWidthPx/HeightPx`) + FE (`TA-ITS015-WEBVUE-V1.0/.../services/videoWall/api/wallConstants.ts`) hiện = 3840×2160. Phải bằng nhau ở 3 chỗ (seed / VwWallProfile / FE). Khuyến nghị: đổi cả 3 về 1920×1080.
3. Chạy seed `.sql` tay.
4. Commit `TA-ITS015-WEBAPI-V1.0`.
5. **WPF** (`Module.VideoWall.WPF`): áp prompt `videowall-wpf-cascade-fix-prompt-20260908.md` nếu chưa xong hết — chủ yếu `wallNo ?? _connection.WallNo ?? 1` (6 chỗ trong `SceneSetupViewModel.cs`) → `?? ProbeResult.BoundWallNo` + guard; gộp 2 danh sách "Màn 1..12" cứng 4×3 (dòng ~744, ~835) thành `BuildFallbackScreens(cols,rows,tile)`. Tab 1 đã ~90% data-driven qua `ProbeResult.Outputs`.
6. **Frontend `TA-ITS015-WEBVUE-V1.0`** chưa động — `wallConstants.ts` hardcode 8×4/3840×2160, `getWallTopology()` trả hằng số. Cần backend resource `vwwalltopology/current` + canh panel px. Ngoài phạm vi tới giờ.

Các prompt fix (`videowall-*-prompt-*.md` trong thư mục VideoWall) tự xoá sau khi áp xong (CLAUDE.md safeguard #2); một số đã xoá.
