# Prompt: FE hiển thị vùng lưới được/không được cấp quyền + chặn kéo thả ngoài vùng

> Ngày lập: 2026-09-13. Đối chiếu transcript `2026-09-09-videowall-phan-quyen-va-layout.md`
> (việc giao cho Kiên: *"vẽ bố cục ma trận lưới, hiển thị trực quan vùng rào quyền của người
> dùng — vùng bị khóa/vùng được phép kéo thả"*) với FE thật (`TA-ITS015-WEBVUE-V1.0`) — xác nhận
> qua Explore agent đọc code: **tính năng này chưa hề tồn tại**. Đã đọc kỹ cả BE (`VwPermissionService.cs`,
> `VwWallPermissionQueryHandler.cs`) và FE (`outputMap/index.vue`, `scenes/component/sceneCanvas.vue`,
> `scenes/index.vue`, `monitor/index.vue`) để soạn prompt chính xác, không suy đoán.
>
> **File này CHỈ còn phần FE.** Phần Backend (API "vùng lưới hiệu lực của tôi") đã tách riêng ra
> file `videowall-wallpermission-my-effective-config-api-prompt.md` — **phải làm file đó TRƯỚC**,
> vì phần FE dưới đây phụ thuộc trực tiếp vào API đó.

## Hiện trạng đã xác nhận

**FE hiện có (nhưng KHÔNG phải thứ transcript yêu cầu):**
- `outputMap/index.vue` (dòng 27-51, 137): vẽ lưới `rows × cols` cho gán cổng output↔màn hình
  vật lý — khoá theo `isFullAccess = accountType === '111'` (hardcode SuperAdmin), KHÔNG liên
  quan tới `VwWallPermission`.
- `scenes/component/sceneCanvas.vue` + `scenes/index.vue`: canvas kéo-thả cửa sổ nguồn vào Scene,
  khoá theo `readonly` — TOÀN BỘ canvas readonly hay không, dựa vào việc `controllerId` của scene
  có nằm trong danh sách controller được gán cho user không (`state.controllers`) — khoá **CẢ
  SCENE**, không phải khoá TỪNG Ô LƯỚI riêng lẻ.
- `monitor/index.vue` (dòng 14): luôn ép `readonly=true` cho `SceneCanvas`, kèm comment **lỗi
  thời** *"BE chưa có endpoint đẩy layout xuống tường"* — BE thực ra ĐÃ có `SetWindowLayer`/
  `SwitchSource` từ lâu, comment này cần sửa lại nếu sau này quyết định bật tương tác trên trang
  Monitor (KHÔNG thuộc phạm vi prompt này — chỉ ghi chú, không tự ý bật).

**KHÔNG tồn tại ở đâu trong FE**: không có field/type/API call nào tên `VwWallPermission`/
`Config`/`allowedCells` — FE hoàn toàn chưa biết tới khái niệm phân quyền theo Ô LƯỚI CỤ THỂ
(`{Col,Row}`) mà chỉ có BE đang tự áp (Tầng 3, `VwPermissionService.EnsureWindowInsideUserAllowedAreaAsync`).

**API cần dùng** (đã có prompt riêng, xem `videowall-wallpermission-my-effective-config-api-prompt.md`):
1 endpoint mới trên `VwWallPermissionController`, không tham số, tự suy user từ token đăng nhập,
trả về:
```csharp
public class VwMyWallPermissionOutput
{
    public bool IsFullAccess { get; set; }
    public List<VwGridCell>? AllowedCells { get; set; } // null khi IsFullAccess = true
}
```

## Frontend: vẽ vùng khoá/mở theo lưới + chặn kéo thả

1. Tạo 1 composable/service gọi API trên khi vào trang có canvas lưới
   (`scenes/index.vue`, và cân nhắc cả `outputMap/index.vue` nếu áp dụng luôn ở đó) — cache kết
   quả trong phiên làm việc (không cần gọi lại liên tục, chỉ refetch khi đổi user/reload trang).
2. `scenes/component/sceneCanvas.vue`: khi vẽ lưới nền (dòng 15-19, 143-150 hiện tại), với MỖI ô
   `(col,row)`, kiểm tra có nằm trong `AllowedCells` hay không (hoặc `IsFullAccess = true` → luôn
   coi là mở) — nếu KHÔNG được phép: hiển thị ô đó ở trạng thái khoá (mờ đi/hoạ tiết gạch chéo/icon
   khoá — tham khảo cách `outputMap/index.vue` đã làm ở dòng 34-48 cho phần hiển thị, tái dùng
   style tương tự cho nhất quán UI).
3. Chặn kéo-thả vào ô bị khoá: trong handler `onDrop`/`@dragover.prevent` (dòng 353-388 hiện tại),
   thêm điều kiện — nếu vị trí thả rơi vào (dù chỉ 1 phần) các ô KHÔNG nằm trong `AllowedCells`,
   từ chối thao tác (không emit `drop-source`, có thể hiện toast/thông báo ngắn "Vùng này không
   thuộc quyền quản lý của bạn"). Đây là khoá bổ sung ở tầng UI — KHÔNG thay thế việc BE vẫn phải
   enforce lại (Tầng 3 đã có), chỉ để UX rõ ràng hơn, tránh user kéo xong mới bị BE từ chối.
4. Không đụng tới cơ chế khoá "cả scene readonly" hiện có (`canEditScene`/`canEditCurrent` theo
   controller ownership) — 2 cơ chế khoá này SONG SONG, không thay thế nhau: khoá theo controller
   (thô, cả scene) vẫn giữ nguyên, khoá theo ô lưới (mục 2-3) là lớp chi tiết hơn, áp dụng THÊM
   vào bên trong 1 scene có thể chỉnh sửa.

## Test cần thêm

- FE: test/manual — user có permission giới hạn thấy đúng ô bị khoá theo `Config`; kéo-thả vào ô
  khoá bị chặn kèm thông báo; SuperAdmin/không có bản ghi permission thấy toàn bộ lưới mở.
- (Test phần API `IsFullAccess`/`AllowedCells` đã nằm trong
  `videowall-wallpermission-my-effective-config-api-prompt.md`, không lặp lại ở đây.)

## Verification

1. API (`videowall-wallpermission-my-effective-config-api-prompt.md`) phải làm xong và build sạch
   TRƯỚC khi test phần FE này.
2. `npm run build`/chạy dev FE, test thủ công trên trang Scenes: đăng nhập user có
   `VwWallPermission.Config` giới hạn, xác nhận đúng ô bị khoá hiển thị + kéo-thả bị chặn.
3. Không commit/push — để người dùng tự soát diff cả 2 phía trước khi quyết định.
