# VideoWall Device Proxy & Architecture Memory

> Cập nhật: 2026-09-07
> Nguồn sự thật: `VideoWall/LogsAPI/` (2113 bản ghi thiết bị thật đo từ DS-C30S-S11) và `09-api-reference.md`.
> Tài liệu cấm: `09B-practical-guide-and-tested-responses.md` đã bị xoá hoàn toàn.

---

## 1. Tầng Proxy Thiết Bị (Device Proxy Layer)

- Service `Module.VideoWall` đã có tầng controller proxy thiết bị: `VwDeviceController` (`Controllers/Device/VwDeviceController.cs`).
- GroupName: `VideoWall`, `Order = 495` (nằm giữa `DeviceSetup` 490 và `Controller` 500).
- Chế độ: `[AllowAnonymous]` (có chủ ý cho giai đoạn đấu nối).
- Phơi đủ **23 endpoint HTTP POST** tương ứng 1:1 với các endpoint ISAPI đo thật trên phần cứng:
  - Nhóm 1 (Xác thực & Năng lực): `UserCheck`, `Capabilities`.
  - Nhóm 2 (Tường & Cổng): `Walls`, `Outputs`, `InputChannels`, `OutputChannels`, `StreamChannels` (🔒 CỐ Ý không chứa mật khẩu).
  - Nhóm 3 (Kịch bản): `SceneList`, `SceneRunning`, `SceneInfo` (JSON layout), `SceneCreate`, `SceneRename`, `SceneSave`, `SceneActivate`.
  - Nhóm 4 (Cửa sổ): `WindowList`, `WindowGet`, `WindowAdd`, `WindowUpdate`, `WindowDelete`, `WindowDeleteAll`, `WindowTop`, `WindowBottom`, `WindowStartDecode`.
- Mọi endpoint forward qua Wolverine mediator (`MessBus.InvokeAsync`), có validator FluentValidation độc lập và trả `VwSetupSceneStep` để debug XML/thời gian.

---

## 2. Kết luận Mô hình Controller ↔ Wall (Đối chiếu §5.1)

- **Thiết bị thật**: 1 controller phần cứng = **8 videowall logic** (`VideoWall1`…`VideoWall8`, `maxWallNums = 8`).
- **Web client (hiện tại)**: Giả định ngược — N controller ghép vào 1 videowall duy nhất, hardcode 4 hàng × 8 cột = 32 panel panel (`wallConstants.ts`, `Object.freeze`).
- **Thực tế hiện trường**: Wall 1 có 4 cổng ra (lưới 2×2 ảo 3840×3840). Wall 4 và Wall 7 có 0 cổng ra (unbound).
- **Nguyên tắc**: KHÔNG hardcode bất kỳ kích thước lưới nào (không 4×8, không 4×3, không 2×2). Kích thước và số màn hình phải suy từ `GET .../{wallNo}/outputs` sắp xếp theo toạ độ ảo `Rect.Coordinate`.

---

## 3. Entity Tường: `VwWallTopology` (Không tạo `VwWall`)

- Bảng tường **ĐÃ TỒN TẠI** trong CSDL: `dbo.VwWallTopology` (entity `VwWallTopology.cs`), bản ghi dev `vw-wall-default`.
- **TUYỆT ĐỐI KHÔNG TẠO ENTITY `VwWall`**: Tránh tái lặp lỗi hai nguồn sự thật cho cùng một khái niệm.
- **Kế hoạch Giai đoạn B (khi PO phê duyệt)**:
  - Mở rộng `VwWallTopology` từ singleton thành đa tường: thêm `ControllerId` (`nvarchar(128)`) và `WallNo` (`int`, 1..8).
  - Thêm `WallId` vào `VwScreen` và `VwScene`.
  - Dựng bộ CRUD `vwwalltopology/{page,byid,add,update,delete,current,ImportFromDevice}`.

---

## 4. Khoảng trống dữ liệu cần đo thêm tại hiện trường

1. **Tường 2, 3, 5, 6, 8**: Chưa từng đọc `outputs`, cần đo tại hiện trường xem có màn hình hay không.
2. **12 màn hình của trạm**: Wall 1 hiện mới chỉ có 4 màn (2×2). Cần đo xem 8 màn còn lại rải trên tường nào hay chưa đấu nối.
3. **`GET ISAPI/DisplayDev/Video/outputs/channels`**: Chưa từng có dữ liệu đo thật.
4. **SID giữa các tường**: Cần kiểm tra SID có độc lập theo tường hay chia sẻ toàn bộ controller.
5. **`PUT .../windows/{VWMWID}` với payload tối thiểu** (`id` + `wndOperateMode` + `Rect`): Cần thử nghiệm trên tường unbound (Wall 4 hoặc 7) để kết luận có thể dùng để resize/move cửa sổ hay không.
6. **`GET /ISAPI/Security/capabilities?username=...`**: Cần gọi đo thực tế để lấy `maxIllegalLoginLockTime` và `maxIllegalLoginTimes` thật.
7. **`lockStatus`/`unlockTime`/`retryLoginTime` từ `userCheck`**: Đọc cơ hội chủ nghĩa khi firmware hỗ trợ.
