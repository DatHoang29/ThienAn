# Plan tổng thể VideoWall

> ⚠️ **KHÔNG xoá theo quy ước Auto-Cleanup Prompt/Plan.** Đây là tài liệu SỐNG — nguồn tham chiếu
> backlog duy nhất cho VideoWall, cập nhật liên tục, không phải prompt dùng 1 lần. Xem thêm
> [`Vw_BE_Review_PostImplementation_2026-09-14.md`](Vw_BE_Review_PostImplementation_2026-09-14.md)
> cho chi tiết đối chiếu từng hạng mục.

> Ngày lập: 16-09-2026. Tổng hợp từ 2 transcript họp gốc
> (`../doc/transcript/09-09-2026-videowall-phan-quyen-va-layout.md`,
> `../doc/transcript/11-09-2026-videowall-script.md`) đối chiếu với trạng thái code thật hiện tại (build,
> test, git diff — không suy đoán). Nguồn tham chiếu backlog duy nhất cho VideoWall kể từ đây.

## 1. Kiến trúc đã chốt (ổn định, khớp cả 2 transcript + đã verify trong code)

- **Luồng**: FE → BE WebAPI → NATS (2 subject: `Control` = `ta.its.data.videowall.control`,
  `Data` = `ta.its.data.videowall`) → VideoWall Worker Service → ISAPI HTTP → Controller Hikvision.
  Lý do tách Service riêng (transcript 09-11): cô lập lỗi phần cứng, chống block/treo WebAPI khi
  thiết bị chập chờn.
- **Cascade DS-C66S**: 1 bộ điều khiển TRUNG TÂM (toàn bộ ISAPI) + tối đa 3 bộ CON (KHÔNG ISAPI,
  chỉ nối vật lý HDMI+GENLOCK) — đã ép đúng ở `LoadController` (chặn gọi ISAPI vào `Role=="sub"`).
- **3 trụ cột** (transcript 09-11): Thiết lập / Điều khiển / Giám sát-Dữ liệu — khớp danh sách
  `EventType` (`SceneActivated`, `HardwareOutOfSync`, `DeviceHeartbeat`, `DeviceProbeCompleted`...)
  trên subject `Data`.
- **Phân quyền** (transcript 09-09, mục trọng tâm nhất buổi họp): bảng `UserId`/`OrgId` + ma trận
  toạ độ ô màn hình, `UserId` ghi đè `OrgId`, mặc định **Full quyền** nếu không khai báo gì. BE đã
  có (`VwWallPermission`, API `GetMy` trả vùng hiệu lực) — **FE hiển thị trực quan vùng rào quyền +
  chặn kéo thả ngoài vùng vẫn chưa làm** (đúng yêu cầu gốc của Anh Sơn).
- **Zone `ToTree()`** (transcript 09-09, mục 1.4): đã xác nhận dùng đúng
  (`Modules.TMS/Controllers/Zone/Queries/ZonesQueryHandler.cs`) — đây KHÔNG phải phạm vi VideoWall
  (Zone là bảng vị trí dùng chung toàn hệ thống), mục này coi như đã đóng, không cần hành động thêm.

## 2. Trạng thái từng mảng (đã verify thật — build/test/git diff, không suy đoán)

| Mảng | Trạng thái | Chi tiết |
|---|---|---|
| BE (`Module.VideoWall`) | 8/9 hạng mục ĐÚNG | Còn treo #9 (dọn `Shared.*` reference thừa trong `Module.VideoWall.Core.csproj`) — chưa có prompt |
| Worker (`ITS.VideoWall`) | ĐÚNG kiến trúc cascade | `LoadController` chặn sub, `SyncActiveSceneCore` đã sửa bug cross-controller deactivate |
| NATS/FE subject | ĐÃ THỰC THI xong | Subject `Data` chuẩn hoá, FE xử lý `EventType` đúng, đối chiếu khớp 2 transcript |
| Test (`tests/VideoWall`) | ĐÃ XONG, ĐÃ COMMIT | 432→382 test case sau gộp trùng lặp InlineData, commit `f3485fa refactor test`, 0 fail |
| **Comment rác `VwSceneController.cs:101`** | ❌ **VẪN CHƯA SỬA** | Prompt gốc đã bị xoá theo Auto-Cleanup nhưng phần sửa comment chưa thực thi — mất dấu, cần viết lại prompt riêng |
| FE — permission visualization | ⏳ Sẵn sàng, chưa giao | `../Prompt/videowall-fe-grid-permission-visualization-prompt.md` còn tồn tại, BE prerequisite (API `GetMy`) đã có |
| FE — DeviceSetup actions (Ping/Probe/SyncSources/ResetCircuitBreaker) | ❌ Chưa có UI, chưa có prompt | Gap từ `Vw_FE_vs_WPF_Feature_Comparison_2026-09-14.md` |
| `VwSchedule` (lịch phát Scene tự động) | ❌ Ghi/đọc được nhưng KHÔNG có scheduler nào thực thi | Gap có thật từ `TableSQL/Vw_Entities_WritePath_Audit_2026-09-14.md`, chưa có prompt, cần chốt phạm vi trước |
| Validator chặn 2 controller `Role=center` | ⏳ Quan sát, chưa có prompt | Không bắt buộc theo transcript, chỉ là khoảng trống ngầm định |
| Docs (README/INDEX) | ✅ Đã cập nhật | Tier Table đủ, dead link transcript 28/08 đã sửa |

## 3. Backlog đề xuất theo thứ tự ưu tiên

1. **[Nhỏ, khẩn]** Viết lại prompt sửa comment rác `VwSceneController.cs:101` (`ta.its.data.videowallScene`
   → `ta.its.data.videowall`) — việc bị "rớt" khi prompt cũ bị Auto-Cleanup xoá trước khi thực thi hết.
2. **[Sẵn sàng, trọng tâm cả 2 cuộc họp]** Giao thực thi
   `videowall-fe-grid-permission-visualization-prompt.md` — đây là mục Anh Sơn nhấn mạnh nhiều nhất
   trong transcript 09-09 (phân quyền ma trận toạ độ + chặn kéo thả ngoài vùng), BE đã sẵn sàng.
3. **[Trung bình]** Viết prompt: FE bổ sung UI cho nhóm DeviceSetup (Ping/Probe/SyncSources/
   ResetCircuitBreaker) — gap thật so với WPF, nhưng không nằm trong yêu cầu gốc của 2 cuộc họp này.
4. **[Nhỏ]** Viết prompt: dọn `Shared.*` reference thừa khỏi `Module.VideoWall.Core.csproj` (#9).
5. **[Cần chốt phạm vi trước]** `VwSchedule` — cần quyết định: xây scheduler engine thật hay bỏ hẳn
   field này (chưa từng nằm trong 2 cuộc họp đã đọc, nên chưa rõ mức độ ưu tiên thật).
6. **[Tuỳ chọn, thấp]** Rule validator chặn tạo 2 controller cùng `Role=center`.

## Tài liệu liên quan

- Kiến trúc: `../doc/KienTruc_VideoWall_DS-C66S-Cascade.md`
- Audit entity: `../doc/TableSQL/Vw_Entities_WritePath_Audit_2026-09-14.md`
- So sánh FE/WPF: `../doc/Vw_FE_vs_WPF_Feature_Comparison_2026-09-14.md`
- Review sau triển khai BE: [`Vw_BE_Review_PostImplementation_2026-09-14.md`](Vw_BE_Review_PostImplementation_2026-09-14.md) (cùng thư mục `Plan/`)
- Transcript gốc: `../doc/transcript/09-09-2026-videowall-phan-quyen-va-layout.md`,
  `../doc/transcript/11-09-2026-videowall-script.md`
