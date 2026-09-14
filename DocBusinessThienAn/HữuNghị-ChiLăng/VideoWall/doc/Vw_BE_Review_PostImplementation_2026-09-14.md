# Review sau triển khai: VideoWall Backend — đối chiếu prompt đã giao & kiến trúc DS-C66S

> Ngày lập: 2026-09-14. Review code BE VideoWall (FE chưa tính trong đợt này) sau khi đã sửa theo
> các prompt trước đó và push. Đối chiếu trực tiếp với git log/code thật, không suy đoán, cộng thêm
> build thật + chạy test thật.

## Bối cảnh nhánh git

Code đang ở nhánh `feat/20260909-videowall-device` (đã push lên `origin`, đồng bộ, working tree
sạch) — **chưa merge vào `dev`/`main`**. Toàn bộ 9 hạng mục dưới đây nằm trong đúng 1 commit lớn:
`01f724d3` (2026-09-14) "Hoàn thiện worker service - Khởi tạo service nền ITS.VideoWall và
ITS.VideoWall.Core điều khiển Hikvision qua ISAPI/NATS."

Có 1 nhánh phụ bị bỏ (`feat/20260819/videowall_device`, KHÔNG phải tổ tiên của HEAD hiện tại) từng
đi sai hướng ở 2 điểm (đổi `TargetSceneId` → `SceneId`, thêm comment nhắc `DeviceIntegration.json`
không tồn tại) — nhánh đó không ảnh hưởng gì tới code hiện tại, ghi nhận lại cho đầy đủ lịch sử.

## Kết quả đối chiếu 9 hạng mục prompt đã giao

| # | Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|---|
| 1 | `LoadController` chặn gọi ISAPI vào `Role=="sub"` | ✅ Đúng | `VwISAPIDeviceService.DeviceSetup.cs:1125-1126` — throw rõ ràng, là chokepoint dùng chung cho Ping/Probe/SyncSources/SyncActiveScene/SetupScene/Passthrough |
| 2 | `SyncActiveSceneCore` sửa bug deactivate chéo controller | ✅ Đúng | Dùng đúng pattern HashSet `activeSceneIds` theo từng controller trước khi deactivate, khớp 100% với pattern gốc ở `VwCommandConsumer.HandleActivateSceneAsync:262-294` |
| 3 | `VwEventTriggerLog` giữ tên cột `TargetSceneId` | ✅ Đúng | Entity (dòng 38) + 4 call site (2 Writer, QueryHandler, comment PermissionService) đều nhất quán |
| 4 | Đổi tên CircuitBreaker → DeviceAuthFailure (cache key + class + method) | ✅ Đúng | Đủ 14/14 file, 0 chỗ còn sót "circuitbreaker" trong code nguồn |
| 5 | Thêm `VwController.ParentControllerId` (multi-wall) | ✅ Đúng | Entity (dòng 208-215) + Validator (dòng 127-150) đủ 2 chiều: center bắt buộc rỗng, sub bắt buộc trỏ tới 1 center tồn tại |
| 6 | Chuyển DTO ISAPI ra khỏi `Module.VideoWall.Core` | ✅ Đúng | Project mới `ITS.VideoWall.Core` (tầng Worker) chứa toàn bộ DTO ISAPI, tham chiếu MỘT CHIỀU vào `Module.VideoWall.Core` — đúng hướng phụ thuộc |
| 7 | Sửa comment cũ nhắc `DeviceIntegration.json` | N/A | String này không tồn tại trong code hiện tại (chỉ có ở nhánh phụ bị bỏ) — không có gì để sửa |
| 8 | Gom/bỏ test trùng lặp (theory/InlineData) | ✅ Đúng (Nhóm A+B) | Xem mục "Cập nhật 2026-09-14" bên dưới — đã thực thi qua prompt riêng, 432→382 test case, PASS 100% |
| 9 | Bỏ reference `Shared.*` dư thừa khỏi `Module.VideoWall.Core.csproj` | ❌ Chưa làm | File `.csproj` không đổi từ 2026-08-10, còn đủ `Shared.DTO`/`Shared.Reference`/`Shared.Utility` + 3 HintPath DLL cũ |

## Đối chiếu kiến trúc DS-C66S (không chỉ theo prompt, đọc lại toàn bộ `KienTruc_VideoWall_DS-C66S-Cascade.md`)

Nguyên tắc gốc: 1 bộ điều khiển TRUNG TÂM nói ISAPI, 3 bộ CON không có traffic ISAPI (chỉ nối vật lý
qua HDMI+GENLOCK) — **"Không có master/slave trong ISAPI DS-C66S"**.

- `SyncActiveSceneCore` (dòng 513-539) và `HandleActivateSceneAsync` (dòng 262-294) hiện **giống hệt
  cấu trúc nhau** — cùng dùng HashSet `activeSceneIds` theo từng controller trước khi deactivate scene
  cũ. Không còn lệch giữa 2 luồng.
- `VwController.ParentControllerId` đúng ngữ nghĩa cascade: center luôn rỗng, sub bắt buộc trỏ tới
  center, tối đa 2 tầng — khớp thiết kế đã chốt ("Bỏ hẳn khái niệm Scene toàn tường, bắt buộc gắn 1
  tường cụ thể").
- `VwScreen.ScreenState` hiện tại: `string?` (đã chốt cuối cùng, không đổi qua enum thật) — khớp
  100% cách `VwDeviceHeartbeatService.cs` đọc/ghi (dùng `BaseEnums.ScreenState.Online/Offline/Warning`
  — hằng string, không phải enum C#).

### Khoảng trống nhỏ, không nằm trong 9 prompt đã giao (chỉ ghi nhận, chưa cần hành động)

`VwControllerValidator.cs` chưa có rule chặn tạo 2 controller cùng `Role=="center"`. Kiến trúc ngầm
định 1 tường = 1 center nhưng chưa bị ép ở tầng validate. Không phải lỗi so với yêu cầu đã giao,
chỉ nêu ra để cân nhắc thêm sau.

## Phát hiện thêm khi đối chiếu chéo với Plan/ và các báo cáo trước

1. **Prompt FE còn tồn đọng `videowall-subject-rename-fe-prompt.md` sai target subject so với code
   thật.** Prompt yêu cầu đổi subject FE thành `ta.its.data.videowall.data`, nhưng subject thật
   trong code (`VwSubjects.cs:15`, `appsettings.json`, `DataTransporter.json`) là
   `ta.its.data.videowall` (không có hậu tố `.data`). **Cần sửa lại target trong prompt trước khi
   giao cho người thực thi**, nếu không FE sẽ lại subscribe sai subject lần nữa.
2. **Comment rác trong `VwSceneController.cs:101`**: còn ghi "kênh NATS
   `ta.its.data.videowallScene`" — không khớp cả tên cũ lẫn tên mới, cần sửa lại thành
   `ta.its.data.videowall`.
3. **Prompt FE thứ 2 (`videowall-fe-grid-permission-visualization-prompt.md`) đã sẵn sàng thực thi.**
   Phần phụ thuộc BE mà nó yêu cầu làm trước đã có thật: endpoint `GET VwWallPermission/GetMy` trả
   `VwMyWallPermissionOutput` (`VwWallPermissionController.cs:44-49`, "Lấy vùng lưới hiệu lực của
   người dùng đang đăng nhập") đã tồn tại trong code đã push.
4. **Dead-link tài liệu (không liên quan code, phát hiện phụ khi đối chiếu):** cả `INDEX.md` (Meeting
   Matrix, dòng 2026-08-28) và `VideoWall/README.md` (3 chỗ) đều trỏ tới
   `doc/transcript/2026-08-28-videowall-chuan-bi.md` — file này **không tồn tại** trên đĩa (thư mục
   `transcript/` chỉ có `00-catalog.md`, `2026-09-09-...md`, `2026-09-11-videowall-script.md`). Bản
   ghi họp 28/08 có vẻ chưa từng được chuyển thể thành `.md`, chỉ có ghi chú "(Lưu trữ nội bộ)" cho
   audio. Đã sửa các link chết này thành ghi chú "chưa có bản transcript .md" thay vì trỏ tới file
   không tồn tại (xem mục cập nhật doc bên dưới).
5. **`VideoWall/README.md` Tier Table thiếu 3 dòng**: 2 báo cáo audit ngày 2026-09-14
   (`Vw_Entities_WritePath_Audit_2026-09-14.md`, `Vw_FE_vs_WPF_Feature_Comparison_2026-09-14.md`),
   báo cáo review này, và file transcript `2026-09-11-videowall-script.md` (đã tồn tại trên đĩa
   nhưng chưa được liệt kê). Đã bổ sung.

## Xác nhận bằng build + test thật (không suy đoán)

```
dotnet build src/Modules/VideoWall/Module.VideoWall.Core/Module.VideoWall.Core.csproj → 0 Error(s)
dotnet build src/Services/VideoWall/ITS.VideoWall/ITS.VideoWall.csproj               → 0 Error(s)
dotnet build tests/test.csproj (net10.0)                                            → 0 Error(s)

dotnet test tests/test.csproj --filter "FullyQualifiedName~VideoWall" (DB local 127.0.0.1,14333)
→ Passed! - Failed: 0, Passed: 432, Skipped: 0, Total: 432, Duration: 1m 29s
```

Không có breaking change nào với các rename/field mới (`TargetSceneId`, `ParentControllerId`,
`DeviceAuthFailure`, `ScreenState` kiểu string) làm vỡ test hiện có. Toàn bộ 432 test VideoWall
(Controllers, Consumer, Infrastructure/Services cascade, Heartbeat, WPF) đều PASS trên code đã push.

## Cập nhật 2026-09-14 (sau khi 2 prompt bổ sung được thực thi)

Đã viết + giao 2 prompt riêng (`Plan/videowall-fix-nats-subject-stale-references-prompt.md`,
`Plan/videowall-tests-consolidate-theory-remove-duplicates-prompt.md`). Đối chiếu thật bằng
`git diff`/`git status`/đọc file trên cả 3 repo sau khi người dùng báo đã thực thi:

**Prompt 1 — sửa subject NATS sai/rác**:
- ✅ Việc 1 XONG: `Plan/videowall-subject-rename-fe-prompt.md` đã sửa đúng — target subject giờ là
  `ta.its.data.videowall` (khớp code thật), phần "phụ thuộc BE" đã sửa thành "KHÔNG CÓ". (Bản thân
  prompt FE này vẫn chưa thực thi lên code FE thật — `Nats.subjects.json`/`transporterEvent.ts` vẫn
  còn subject cũ `.scene`, đúng như dự kiến vì đó là việc của chính prompt đó, chưa nằm trong đợt
  sửa lần này.)
- ❌ Việc 2 CHƯA XONG: `VwSceneController.cs:101` vẫn còn nguyên comment rác
  `ta.its.data.videowallScene` — **prompt vẫn còn hiệu lực, chưa xoá**.

**Prompt 2 — rà soát test trùng lặp**:
- ✅ Nhóm A + Nhóm B đã thực thi đúng — xác nhận qua `git diff --cached` trong repo `tests`: đúng
  8 file (`VwControllerTests`, `VwEventRuleTests`, `VwSceneTests`, `VwScheduleTests`,
  `VwScreenTests`, `VwSlotPortTests`, `VwSourceTests`, `VwWindowSceneTests`), +38/-76 dòng. Đã soát
  nội dung diff — khớp đúng chỉ dẫn (xoá InlineData dư ở Nhóm A; ở Nhóm B còn làm tốt hơn yêu cầu:
  thêm `Code` hợp lệ + `Assert.Contains(...PropertyName == nameof(...))` để cô lập đúng field).
- ⏭️ Nhóm C (`VwCascadeCoordTests.cs`) bỏ qua có chủ đích — đúng như prompt ghi ("ưu tiên thấp,
  không bắt buộc").
- **Test thật sau khi gộp**: `dotnet test --filter VideoWall` (DB local) →
  `Passed! - Failed: 0, Passed: 382, Skipped: 0, Total: 382` (giảm từ 432 → 382 test case, PASS
  100%, không có test nào bị vỡ).
- Thay đổi đang ở trạng thái **staged, chưa commit** trong repo `tests` tại thời điểm review này.

## Việc còn lại chưa làm

- Hạng mục #9: dọn reference `Shared.*` dư thừa khỏi `Module.VideoWall.Core.csproj`.
- Rule validator chặn tạo 2 controller cùng `Role=="center"`.
- Comment rác `VwSceneController.cs:101` (Việc 2 của Prompt 1 ở trên) — prompt còn hiệu lực.

## Kết luận

8/9 hạng mục đã giao đều ĐÚNG (hạng mục #8 đã hoàn tất qua prompt bổ sung), khớp kiến trúc DS-C66S
cascade, không có mismatch nào so với `KienTruc_VideoWall_DS-C66S-Cascade.md`. 1 hạng mục N/A. Còn
1 hạng mục (#9 dọn reference thừa) chưa làm — không ảnh hưởng chức năng. 2 việc nhỏ phát sinh thêm
đã xong 1/2 (subject NATS trong prompt FE đã sửa đúng; comment rác `VwSceneController.cs:101` còn
treo). Build sạch, test VideoWall 382/382 PASS sau khi gộp. Chưa merge vào `dev`/`main`.
