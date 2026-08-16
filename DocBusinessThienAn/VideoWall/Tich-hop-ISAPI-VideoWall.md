# Tích hợp Module.VideoWall với thiết bị Video Wall Controller (ISAPI)

> Tài liệu mô tả **implementation thật trong code** — khác với thư mục [`ISAPI-Videowall-Controller/`](ISAPI-Videowall-Controller/) là bản convert nguyên văn tài liệu hãng. Đọc file này để biết code đang làm gì; đọc `ISAPI-Videowall-Controller/09-api-reference.md` khi cần tra cứu spec gốc của 1 API cụ thể.

## 1. Tổng quan kiến trúc

```
Module.VideoWall.Core/
  Abstracts/
    IVwDeviceClient.cs          — interface giao tiếp thiết bị (never-throw)
    VwIsapiResult.cs            — record kết quả Ok()/Fail(), có bản generic <T>
  Dto/Isapi/
    VwIsapiResponse.cs          — DTO response (XML) từ thiết bị
    VwIsapiWindowRequest.cs     — DTO request (XML) gửi lên thiết bị

Module.VideoWall/
  Infrastructure/Services/Device/
    VwIsapiClient.cs            — implement IVwDeviceClient, gọi HTTP thật (XmlSerializer)
    VwIsapiCredentialStore.cs   — build base URI + đăng ký Digest credential/controller
  Infrastructure/Services/Scene/
    VwSceneRegionService.cs     — quy đổi toạ độ tuyệt đối → cục bộ ISAPI
  Controllers/Scene/Commands/
    VwSceneWorkflowCommandHandler.cs   — kích hoạt scene (Activate)
  Controllers/WindowScene/Commands/
    VwWindowSceneCommandHandler.cs     — Add/Update/Delete window (resync toàn bộ)
  Extensions/ServiceCollectionExtensions.cs — đăng ký DI (AddVWInfrastructure)
```

Nguyên tắc xuyên suốt: **`IVwDeviceClient` không bao giờ throw** — mọi lỗi mạng/HTTP/XML đều gói vào `VwIsapiResult.Fail(...)`. Quyết định throw hay không (fail-fast) thuộc về handler gọi nó.

## 2. Điều kiện tiên quyết trước khi chạy trên thiết bị thật

Vì chưa implement API "tạo scene mới" (`POST .../scene`, §9.7.7.2 đánh dấu ⚪ Tùy chọn trong `00-api-catalog.md`), **hệ thống không tự tạo scene trên thiết bị**. Trước khi dùng:

1. Trên chính thiết bị (qua web UI/phần mềm cấu hình gốc của hãng): tạo video wall + tạo sẵn scene, ghi lại SID (ví dụ `1`).
2. Trong DB:
   - `VwController.IP` / `Account` / `PassWord` — 3 field bắt buộc, thiếu `IP` thì `VwIsapiCredentialStore.EnsureRegistered` throw ngay (`InvalidOperationException`).
   - `VwScene.OutputId` = đúng SID vừa tạo ở bước 1. Đây là field ĐÓNG VAI TRÒ ISAPI Scene ID — không phải cột mới, tái sử dụng field có sẵn.
   - Scene rỗng `OutputId` → bị bỏ qua hoàn toàn bước gọi thiết bị (chỉ ghi DB như trước khi có tích hợp).

Quy ước cố định không lưu DB (đổi ở code nếu cần khác cho toàn fleet): scheme `http`, port `80`, `videoWallID = "1"` — xem `VwIsapiCredentialStore.cs` / hằng số `DefaultWallId` trong `VwIsapiClient.cs`.

## 3. Kết nối & xác thực

- **HTTP Digest Auth** (RFC 7616) qua `System.Net.CredentialCache` — không viết `DelegatingHandler` tuỳ biến. `VwIsapiCredentialStore.EnsureRegistered(controller)` remove-rồi-add lại credential cho đúng base URI mỗi lần gọi, để đổi mật khẩu trong DB có hiệu lực ngay không cần restart app.
- **TLS**: `ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator` — bỏ qua xác thực cert self-signed (thiết bị on-prem). Chỉ chấp nhận được vì thiết bị nằm trong mạng nội bộ tin cậy.
- **Timeout**: 8 giây/request (`ServiceCollectionExtensions.AddVWInfrastructure`) — 1 controller bị treo mạng không được làm nghẽn luồng kích hoạt scene quá lâu, nhưng cũng đồng nghĩa API activate của hệ thống có thể "cảm giác treo" tới 8s nếu mạng LAN tới thiết bị chập chờn.
- DI đăng ký qua `AddHttpClient<IVwDeviceClient, VwIsapiClient>` (typed client, đúng khuôn mẫu `AddNxVmsIntegration` đã có trong dự án).

## 4. Luồng kích hoạt scene (`VwSceneWorkflowCommandHandler`)

Khi gọi `VwActiveSceneInput` (theo `SceneCode` hoặc `EventTypeId`), với mỗi controller bị scene đó chiếm (`targetControllerIds` — 1 controller nếu scene vùng, tất cả nếu scene toàn tường):

1. `GET /ISAPI/DisplayDev/VideoWall/capabilities` — nếu lỗi hoặc `isSupportScene=false` → ghi `VwEventTriggerLog` Fail, throw `DeviceCapabilityUnsupported`, **không** cập nhật DB.
2. `PUT /ISAPI/DisplayDev/VideoWall/1/scene/{OutputId}/activate` — nếu thiết bị trả lỗi (kể cả HTTP 200 nhưng `statusCode≠0/1` trong body) → ghi Fail log, throw `DeviceActivationFailed`, không cập nhật DB.
3. Chỉ khi **toàn bộ** controller trong vòng lặp activate thành công, mới `BEGIN TRAN` ghi `VwController.ActiveSceneId` + `VwScene.ActiveScene`, `COMMIT`, ghi Success log, rồi broadcast NATS cho FE.

Chính sách: **fail-fast toàn phần** — 1 thiết bị lỗi giữa vòng lặp nhiều controller thì dừng ngay, không rollback thiết bị đã activate thành công trước đó (giới hạn đã biết, chấp nhận được vì tần suất scene toàn tường nhiều controller thấp).

## 5. Luồng Add/Update/Delete Window (`VwWindowSceneCommandHandler`)

**Vấn đề gốc**: `VwWindowScene` không có cột lưu VWMWID (ID window trên thiết bị) → không thể sửa/xoá đúng 1 window theo ID. **Giải pháp đã chốt**: chiến lược *resync toàn bộ* — mỗi lần Add/Update/Delete 1 window trong 1 scene có `ControllerId`, hàm `SyncSceneWindowsToDeviceAsync` sẽ:

1. `GET .../VideoWall/capabilities` — fail-fast nếu không hỗ trợ scene (giống mục 4).
2. `DELETE /ISAPI/DisplayDev/VideoWall/1/windows` — xoá **TẤT CẢ** window đang hiển thị trên controller đó (không riêng window đang sửa). ⚠️ Người dùng sẽ thấy canvas nháy/trắng ngắn mỗi lần Add/Update/Delete 1 window.
3. Với từng window còn lại + window mới (nếu Add/Update) — quy đổi toạ độ (mục 6) rồi `POST .../VideoWall/1/windows` từng cái một.
4. **Nếu còn ít nhất 1 window HOẶC vừa xoá về 0** → `PUT /ISAPI/DisplayDev/VideoWall/1/scene/{OutputId}/saveData` để chốt canvas vừa dựng thành snapshot chính thức của scene. Bỏ qua nếu `OutputId` rỗng.
5. Chỉ khi mọi bước trên OK mới ghi/xoá DB (`VwWindowScene`).

**Vì sao bắt buộc bước `saveData`** (§8.1.2 `ISAPI-Videowall-Controller/08-decoding-and-video-wall.md`): `activate` không hiển thị canvas hiện tại của thiết bị — nó **khôi phục bản đã `saveData` gần nhất** của đúng SID đó. Thiếu bước 4 thì mọi chỉnh sửa window qua app chỉ tồn tại tạm trên canvas, biến mất ngay khi ai đó activate lại scene đó lần sau (kể cả xoá window cuối cùng — nếu không save, thiết bị sẽ "hồi sinh" lại window đã xoá ở lần activate kế tiếp).

Bỏ qua toàn bộ đồng bộ thiết bị (chỉ ghi DB như trước tích hợp) nếu: scene toàn tường (`ControllerId` rỗng — không xác định đúng 1 thiết bị), hoặc controller chưa cấu hình `IP`.

`IVwDeviceClient.UpdateWindowAsync`/`DeleteWindowAsync` (theo VWMWID) đã implement sẵn trong `VwIsapiClient` nhưng **hiện không handler nào gọi** — dự phòng cho sau này nếu `VwWindowScene` được bổ sung cột lưu VWMWID và đổi sang chiến lược sửa-đúng-1-window.

## 6. Quy đổi toạ độ (`VwSceneRegionService.ToIsapiLocalRect`)

`VwWindowScene.X/Y/W/H` là pixel tuyệt đối trên toàn bức tường (panel offset = `GridCol/GridRow × PanelWidthPx(3840)/PanelHeightPx(2160)`). ISAPI `uniformCoordinate` coi mỗi panel là 1 ô ảo **vuông** `baseOutputSize×baseOutputSize` (1920×1920) — đã xác nhận bằng đo thật (§D.3 tài liệu đo, không phải suy đoán), kể cả khi panel thật không vuông (1920×1080).

```
originCol = min(GridCol) của các VwScreen thuộc controller
originRow = min(GridRow) của các VwScreen thuộc controller
localX = X - originCol × 3840        localY = Y - originRow × 2160
isapiX = localX × 1920 / 3840        isapiY = localY × 1920 / 2160
isapiW = W × 1920 / 3840             isapiH = H × 1920 / 2160
```

Test thuần (không cần Host/DB): [`VwSceneRegionServiceCoordinateTests.cs`](../../TA-ITS015-WEBAPI-V1.0/tests/Modules/VideoWall/VwSceneRegionServiceCoordinateTests.cs).

## 7. Chính sách xử lý lỗi (fail-fast, thống nhất toàn module)

Mọi thao tác ghi thiết bị đều theo đúng 1 khuôn: **gọi thiết bị TRƯỚC → thành công mới ghi DB**. Không có trường hợp nào ghi DB trước rồi mới gọi thiết bị (tránh trạng thái DB nói có nhưng thiết bị không phản ánh đúng). Danh sách message lỗi (`Module.VideoWall.Core/Exceptions/BaseMsg.cs`):

| Constant | Khi nào |
| --- | --- |
| `DeviceCapabilityUnsupported` | `GetCapabilitiesAsync` lỗi hoặc `isSupportScene=false` |
| `DeviceActivationFailed` | `ActivateSceneAsync` lỗi |
| `DeviceWindowSyncFailed` | `DeleteAllWindowsAsync`/`AddWindowAsync` lỗi trong lúc resync |
| `DeviceSaveSceneFailed` | `SaveSceneDataAsync` lỗi sau khi resync xong |

## 8. Những điểm CHƯA xác nhận trên thiết bị thật (rủi ro cần lưu ý khi go-live)

- **`AddWindow` response shape** — suy đoán theo tài liệu vendor (`<ID>` trong `ResponseStatus`), tài liệu đo thật (`API_Postman_Videowall.md`) liệt kê rõ API này "CHƯA CHẠY" trên thiết bị đo.
- **`videoInputChannelID`** — dùng `VwSource.SignalNo`, suy luận từ tên field, chưa ai xác nhận đúng ID kênh input thật trên thiết bị.
- **Quy đổi toạ độ khi có ≥2 controller ghép tường** — công thức mục 6 chỉ được xác nhận với 1 controller/1 wall trong dữ liệu đo thật; hành vi khi nhiều controller cùng lúc chưa kiểm chứng.
- **`GetCapabilitiesAsync` response shape** (`VideoWallCap`, §9.7.5.6) — lấy theo `09-api-reference.md`, chưa có bản đo thật.
- **Phân biệt lỗi tạm thời vs lỗi cứng** — thiết bị có thể trả `inSceneSwitchingPleaseDoNotOperate` (Device Busy, tạm thời) nhưng code hiện coi mọi lỗi là fail-fast như nhau, chưa có retry.

## 9. Test coverage liên quan tích hợp thiết bị

Xem chi tiết đầy đủ ở [`tests/README.MD`](../../TA-ITS015-WEBAPI-V1.0/tests/README.MD). Tóm tắt riêng phần thiết bị:

- `VwIsapiClientTests.cs` — mock XML response ở tầng HTTP (`FakeIsapiHttpMessageHandler`), dùng nguyên văn response đo thật khi có, có chú thích rõ chỗ nào là suy đoán.
- `VwSceneTests.cs` / `VwWindowSceneTests.cs` — test handler-level qua `FakeVwDeviceClient` (fake toàn bộ `IVwDeviceClient`, điều khiển kết quả qua field static), kiểm cả case thành công lẫn fail-fast không ghi DB.
- `VwIsapiClientLiveSmokeTests.cs` — chạy thật với thiết bị vật lý, mặc định **Skipped** (`Xunit.SkippableFact`) nếu chưa điền IP/Account/Password vào 3 hằng số đầu file. Chỉ gọi 2 API đọc an toàn (`GetCapabilities`/`GetActiveScene`), tuyệt đối không gọi Activate/AddWindow thật để tránh đổi hình đang chiếu trên tường khi test.

## 10. Tham chiếu

- `ISAPI-Videowall-Controller/00-api-catalog.md` — khoanh vùng API đã chốt dùng cho dự án (🟢/⚪).
- `ISAPI-Videowall-Controller/08-decoding-and-video-wall.md` §8.1.2 — lý do bắt buộc `saveData`.
- `ISAPI-Videowall-Controller/09-api-reference.md` §9.7.5.6, §9.7.11.3, §9.7.11.4 — spec chi tiết từng endpoint.
- `API/API_Postman_Videowall.md` — dữ liệu đo THẬT trên thiết bị Hikvision DS-C66S-H88-CL (IP 10.10.9.236), nguồn tin cậy cao nhất khi có mâu thuẫn với tài liệu vendor.
- `TableSQL/Vw_Tables_Analysis_And_Design.md` — thiết kế bảng gốc, lý do không thêm cột mới cho tích hợp này.
