# Kịch bản test toàn trình — Công cụ WPF đấu nối VideoWall

> Viết theo đúng UI thật đang có trên đĩa (`Module.VideoWall.WPF/Views/MainWindow.xaml`, xác nhận lúc viết tài liệu này). Tập trung chính vào mục tiêu **kết nối trực tiếp thiết bị phần cứng thật (Direct Mode)** và kiểm thử toàn diện **31 API "Dùng chính"** theo danh mục chuẩn `ISAPI-Videowall-Controller/00B-api-list-full.md`. Không đăng nhập app — vào thẳng `MainWindow`.

---

## Chuẩn bị (3 tiến trình, đúng thứ tự)

```
1. MockServer:  cd scripts/VwMockServerRunner && dotnet run
                → nghe 127.0.0.1:18080-18083, account admin / Password123!
                (Dùng khi giả lập hoặc test các lệnh nguy hiểm; khi cắm thiết bị thật thì bỏ qua MockServer)
2. Backend:     cd src/TAC_WebAPI && dotnet run
                → http://*:5005
3. WPF:         cd .../Module.VideoWall.WPF && dotnet run
```

Mở app → vào thẳng `MainWindow` (không có màn đăng nhập nào) → góc trên trái thấy 2 radio "Gọi API: Trực tiếp thiết bị ● / Qua Service ○", mặc định chọn sẵn **"Trực tiếp thiết bị"** (Direct Mode).

---

## Bước 1 — Tab "1. Kết nối"

**A. Chế độ Trực tiếp thiết bị (Direct Mode — Mặc định khi khởi động, trọng tâm đấu nối phần cứng):**
1. Mặc định app chọn sẵn radio "Trực tiếp thiết bị" (góc trên trái), ComboBox ẩn đi và hiện 4 ô: IP (`127.0.0.1` hoặc IP thật ngoài hiện trường), Port (`18080` hoặc `80`), Tài khoản (`admin`), Mật khẩu (`Password123!`).
2. (Tuỳ chọn) Gõ "Tường số" (ví dụ `1`), để trống thì client tự dò tường đang gắn màn hình.
3. Bấm **"Kết nối (Ping)"** → kết quả trả về trực tiếp từ thiết bị không qua backend service, khung trạng thái báo thành công.
4. Bấm **"Khảo sát (Probe)"** → tự động gọi 4 API ISAPI (`9.7.4.8`, `9.7.5.2`, `9.7.5.5`, `9.7.5.6`) và đổ dữ liệu lên 3 khung bên dưới:
   - "Tường"
   - "Cổng ra / Kênh vào"
   - "Lệch CSDL ↔ thiết bị"
5. Tại GroupBox **"Gửi lệnh ISAPI"** (khối thứ 4 bên dưới) → chọn từ ComboBox 31 API hoặc gõ URL/Method tuỳ ý để gửi lệnh trực tiếp tới phần cứng thật (xem chi tiết danh sách 24 API gửi tay ở phần Phụ lục).

**B. Chế độ Qua Service (Chạy qua WebAPI Backend):**
6. Bấm radio "Qua Service" (góc trên trái) → 4 ô nhập ẩn đi, ComboBox "Bộ điều khiển" hiện ra.
7. Bấm **"Nạp lại"** → danh sách bộ điều khiển khai trong CSDL đổ về. Nếu rỗng, tạo trước 1 `VwController` (IP, Account, Password) qua DB/API rồi nạp lại.
8. Chọn controller vừa nạp → Bấm **"Kết nối (Ping)"** / **"Khảo sát (Probe)"** → Backend service đứng giữa thực hiện kết nối.
9. Tại GroupBox **"Gửi lệnh ISAPI"** → gửi lệnh qua backend (dùng chung credential của controller đã chọn).
10. Bấm chuyển lại radio "Trực tiếp thiết bị" để tiếp tục chu trình test phần cứng trực tiếp.

---

## Bước 2 — Tab "2. Tham số bộ điều khiển / màn hình" (Tiền kiểm tra DB ↔ Hiện trường vật lý)

> ℹ️ **MỤC ĐÍCH TIỀN KIỂM TRA (Bắt buộc trước khi thao tác Tab 3):**  
> Nút **"Nạp tham số"** ở tab này **chỉ đọc dữ liệu từ CSDL** (`VwScreen`, `VwSlotPort`), **KHÔNG gọi bất kỳ API thiết bị nào**.  
> Mục đích bắt buộc của bước này là để người kiểm thử đối chiếu bảng *Màn hình / cổng ra* và *Cổng khe cắm* với sơ đồ đấu nối vật lý thực tế ngoài hiện trường (cáp HDMI, số thứ tự cổng, ma trận màn hình) trước khi bắt đầu các bước gọi ISAPI ở Tab 3. Điều này giúp loại trừ hoàn toàn việc lẫn lộn giữa lỗi sai lệch cấu hình mapping trong DB với lỗi giao tiếp ISAPI khi debug trên thiết bị phần cứng thật.

1. Bấm **"Nạp tham số"** → khối "Bộ điều khiển (chỉ đọc)" hiện Tên/Model/IP/Khung máy/Gốc-Phủ (Col-Row)/Khe cắm/Trạng thái lấy từ CSDL.
2. Bảng **"Màn hình / cổng ra"** → đối chiếu đúng số dòng = số `VwScreen` đã khai cho controller này; các cột OutPutPort / OutputId / GridCol-Row / Resolution phải khớp chính xác với sơ đồ màn hình vật lý ngoài hiện trường.
3. Bảng **"Cổng khe cắm (VwSlotPort)"** → đối chiếu PortNo / PortType / GlobalIndex khớp với các cổng cắm vật lý thực tế trên thiết bị phần cứng.

---

## Bước 3 — Tab "3. Dựng kịch bản"

1. Nạp màn hình & nguồn (nút đầu tab) → xác nhận danh sách màn hình/nguồn tín hiệu đổ đúng từ DB.
2. Tạo kịch bản mới, đặt tên → thử **cả 2 chế độ**:
   - **Từng cổng ra**: mỗi màn hình 1 cửa sổ phủ kín, gán nguồn cho từng cái (khối "Gán nguồn cho từng màn hình").
   - **Cửa sổ xếp lớp** (radio "Cửa sổ xếp lớp (nhiều cửa sổ, khác ZIndex)"): tự thêm dòng, nhập X/Y/W/H/ZIndex tay.
3. "Đọc thông tin" → xác nhận đúng số cửa sổ vừa tạo.
4. Sửa vị trí 1 cửa sổ đã lưu → Lưu → đọc lại xác nhận đổi đúng. Thử nhập W hoặc H = 0 → xác nhận bị chặn, không lưu (validate client-side).
5. Xoá 1 cửa sổ → xác nhận hỏi lại trước khi xoá, xoá xong mất khỏi danh sách.
6. Khối **"Đẩy xuống thiết bị (SetupScene)"**:
   - Để mặc định **DryRun** → bấm "Đẩy" → xác nhận KHÔNG có lệnh ghi thật xuống thiết bị (đếm log ghi thật = 0).
   - Tắt DryRun, thử đẩy thật → xác nhận có hộp thoại cảnh báo trước khi ghi; khi đồng ý, client tự động gọi các API ISAPI:
     - `9.7.11.2` (GET windows — đọc danh sách cửa sổ hiện có)
     - `9.7.11.7` (DELETE window — xoá từng cửa sổ cũ)
     - `9.7.11.4` (POST window — tạo cửa sổ mới theo bố cục kịch bản)
     - `9.7.7.4` (saveData — lưu dữ liệu Scene trên thiết bị)
7. Nút **"Kích hoạt"** (bên cạnh Đẩy) → kích hoạt scene vừa lưu, tự động gọi API `9.7.7.3` (activate Scene).
8. Xoá kịch bản → xác nhận hỏi lại, xoá xong dropdown mất kịch bản đó.

---

## Bước 4 — Tab "4. Lịch bật/tắt"

> ⚠️ **CẢNH BÁO / LƯU Ý:**  
> Tab **"4. Lịch bật/tắt"** hiện đang bị **comment-out hoàn toàn trong XAML** (`MainWindow.xaml` dòng 669-763, ghi chú *"Tạm ẩn cho demo API thiết bị"*), nên giao diện hiện tại của `TabControl` chỉ hiển thị **3 tab** (1. Kết nối, 2. Tham số bộ điều khiển / màn hình, 3. Dựng kịch bản).  
> Các bước CRUD lịch mô tả dưới đây **KHÔNG THỂ THỰC HIỆN QUA GIAO DIỆN** ở bản build hiện tại (mặc dù ViewModel `ScheduleViewModel.cs` vẫn còn logic xử lý). Các bước dưới đây được giữ lại làm tài liệu tham khảo và tạm thời không áp dụng cho tới khi Tab 4 được bật lại trong XAML.

*(Tạm không áp dụng trên UI hiện tại — chỉ tham khảo khi tab được mở lại):*
1. "Nạp danh sách" → xem lịch hiện có.
2. "Tạo mới" → điền giờ/ngày trong tuần/hành động → Lưu → nạp lại xác nhận có mặt.
3. Sửa giờ 1 lịch đã lưu → Lưu → đọc lại đúng.
4. "Xoá" 1 lịch → xác nhận mất khỏi danh sách (xoá mềm — kiểm DB nếu cần chắc chắn).

---

## Bước 5 — Khung "Logs" (Cố định ở đáy cửa sổ, có GridSplitter kéo dãn)

1. Khung Logs nằm cố định ở đáy cửa sổ, luôn hiển thị khi đứng ở bất kỳ tab nào (dùng GridSplitter để kéo dãn độ cao nếu cần).
2. Sau các bước 1-3 (hoặc 1-4 khi mở lại Tab 4), mỗi thao tác Ping/Probe/Scene/Schedule/ISAPI đều để lại đúng dòng log tương ứng (cột **Thời gian / Nhóm / Mức / Chi tiết**).
3. Bấm đúp vào 1 dòng (hoặc bấm nút "Xem chi tiết") → cửa sổ chi tiết (`ActivityDetailWindow`) hiện đủ Request/Response XML, mã HTTP, thời lượng.
4. Đối chiếu: log của thao tác THÀNH CÔNG và THẤT BẠI (thử 1 lần Probe khi tắt kết nối thiết bị) đều phải có mặt, không bị bỏ sót lỗi.

---

## Phụ lục — Bảng kiểm thử chi tiết 31 API "Dùng chính" (`00B-api-list-full.md`)

Trong chế độ Direct Mode (`VwDirectDeviceConnectionClient.cs`), các nút tự động trên UI (Ping, Khảo sát/Probe, Đẩy xuống thiết bị, Kích hoạt) **chỉ tự động gọi 7 trong số 31 API "Dùng chính"**: `9.7.4.8`, `9.7.5.2`, `9.7.5.5`, `9.7.5.6`, `9.7.11.2`, `9.7.11.4`, `9.7.11.7`.

**24 API "Dùng chính" còn lại** không có nút tự động riêng, bắt buộc phải kiểm thử bằng cách gửi tay từng API qua GroupBox **"Gửi lệnh ISAPI"** ở Tab 1 (chọn nhanh qua ComboBox 31 preset hoặc gõ URL/Method trực tiếp).

Dưới đây là bảng phân loại theo 8 nhóm nghiệp vụ chuẩn:

| STT | Nhóm & Mục | Method | URL / Path | Trạng thái trên UI | Hướng dẫn kiểm thử & Ghi chú |
|:---:|---|:---:|---|:---:|---|
| **I** | **Board (9.7.1)** | | | | *(1/1 gửi tay)* |
| 1 | **9.7.1.4** | `GET` | `/ISAPI/System/Board/status/capabilities` | 🖐️ Gửi tay | Kiểm tra capability trạng thái của tất cả sub-boards trong khung máy. |
| **II** | **Decoding (9.7.2)** | | | | *(5/5 gửi tay)* |
| 2 | **9.7.2.1** | `GET` | `/ISAPI/DisplayDev/decoingDevice/status?format=json` | 🖐️ Gửi tay | Lấy trạng thái giải mã và health status tổng thể của controller. |
| 3 | **9.7.2.5** | `PUT` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/start` | 🖐️ Gửi tay | Bắt đầu giải mã động — phát luồng nguồn tín hiệu vào 1 sub-window cụ thể. |
| 4 | **9.7.2.6** | `GET` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/status` | 🖐️ Gửi tay | Kiểm tra trạng thái decode của các sub-window trong 1 window (dùng drill-down lỗi). |
| 5 | **9.7.2.7** | `PUT` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/sub/<VWSWID>/stop` | 🖐️ Gửi tay | Dừng giải mã trên 1 sub-window (giữ khung window nhưng ngừng phát hình). |
| 6 | **9.7.2.8** | `GET` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/status` | 🖐️ Gửi tay | Trạng thái decode TẤT CẢ window — dùng poll chu kỳ 3–5s để giám sát lỗi hiển thị. |
| **III** | **Output Channel (9.7.3)** | | | | *(4/4 gửi tay)* |
| 7 | **9.7.3.4** | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels` | 🖐️ Gửi tay | Đọc thông số cơ bản của toàn bộ các cổng ra video (output channels). |
| 8 | **9.7.3.5** | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels/<channelID>` | 🖐️ Gửi tay | Đọc thông số chi tiết của một cổng ra video cụ thể theo ID. |
| 9 | **9.7.3.6** | `PUT` | `/ISAPI/DisplayDev/Video/outputs/channels/<channelID>` | 🖐️ Gửi tay | Cấu hình cài đặt tham số cho một cổng ra video cụ thể. |
| 10 | **9.7.3.7** | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels/<channelID>/capabilities` | 🖐️ Gửi tay | Đọc khả năng (capability: độ phân giải, tần số quét) của cổng ra video. |
| **IV** | **Signal Source (9.7.4)** | | | | *(4 gửi tay, 1 tự động)* |
| 11 | **9.7.4.8** | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels` | ⚡ Tự động | *Đã tự động phủ qua nút "Khảo sát (Probe)" ở Tab 1 — không bắt buộc gửi tay lại.* |
| 12 | **9.7.4.10** | `PUT` | `/ISAPI/DisplayDev/Video/inputs/channels/<channelID>` | 🖐️ Gửi tay | Cài đặt thông số của nguồn tín hiệu vào (signal source) chỉ định. |
| 13 | **9.7.4.15** | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels/<channelID>/cutOff` | 🖐️ Gửi tay | Đọc thông số cắt ảnh (picture cropping) của nguồn tín hiệu vào chỉ định. |
| 14 | **9.7.4.16** | `PUT` | `/ISAPI/DisplayDev/Video/inputs/channels/<channelID>/cutOff` | 🖐️ Gửi tay | Cài đặt thông số cắt ảnh của nguồn tín hiệu vào chỉ định. |
| 15 | **9.7.4.18** | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels/<channelID>/picture` | 🖐️ Gửi tay | Lấy ảnh snapshot trực tiếp từ cổng nguồn vào chỉ định. |
| **V** | **Video Wall (9.7.5)** | | | | *(3 gửi tay, 3 tự động)* |
| 16 | **9.7.5.1** | `GET` | `/ISAPI/DisplayDev/capabilities` | 🖐️ Gửi tay | Đọc khả năng tổng thể của thiết bị điều khiển Video Wall. |
| 17 | **9.7.5.2** | `GET` | `/ISAPI/DisplayDev/VideoWall` | ⚡ Tự động | *Đã tự động phủ qua nút "Khảo sát (Probe)" ở Tab 1 — không bắt buộc gửi tay lại.* |
| 18 | **9.7.5.3** | `PUT` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>` | 🖐️ Gửi tay | Cài đặt tham số của một Video Wall cụ thể theo ID. |
| 19 | **9.7.5.4** | `GET` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>` | 🖐️ Gửi tay | Đọc thông số chi tiết của một Video Wall cụ thể theo ID. |
| 20 | **9.7.5.5** | `GET` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/outputs` | ⚡ Tự động | *Đã tự động phủ qua nút "Khảo sát (Probe)" ở Tab 1 — không bắt buộc gửi tay lại.* |
| 21 | **9.7.5.6** | `GET` | `/ISAPI/DisplayDev/VideoWall/capabilities` | ⚡ Tự động | *Đã tự động phủ qua nút "Khảo sát (Probe)" ở Tab 1 — không bắt buộc gửi tay lại.* |
| **VI** | **Screen (9.7.8)** | | | | *(1/1 gửi tay — Cấm thiết bị thật)* |
| 22 | **9.7.8.1** | `PUT` | `/ISAPI/DisplayDev/ScreenCtrl/closeAll` | 🖐️ Gửi tay ⚠️ | Đóng tất cả màn hình (Tắt màn hình).<br>⛔ **CẢNH BÁO:** Nằm trong 3 lệnh cấm ngoài chủ đích — **CHỈ TEST TRÊN MOCKSERVER**, tuyệt đối không gửi lên thiết bị thật đang chiếu. |
| **VII** | **Window (9.7.11)** | | | | *(6 gửi tay, 3 tự động)* |
| 23 | **9.7.11.2** | `GET` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows` | ⚡ Tự động | *Đã tự động phủ qua Probe (Tab 1) và Đẩy xuống thiết bị (Tab 3) — không bắt buộc gửi tay lại.* |
| 24 | **9.7.11.3** | `DELETE` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows` | 🖐️ Gửi tay | Xoá toàn bộ tất cả cửa sổ trên Video Wall.<br>*Ghi chú:* UI Direct Mode hiện tại xoá cửa sổ theo từng ID đơn lẻ (dùng 9.7.11.7), không gọi API xoá tất cả này tự động, nên cần test tay riêng. |
| 25 | **9.7.11.4** | `POST` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows` | ⚡ Tự động | *Đã tự động phủ qua thao tác "Đẩy xuống thiết bị" ở Tab 3 (thêm window mới).* |
| 26 | **9.7.11.5** | `GET` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>` | 🖐️ Gửi tay | Đọc cấu hình toạ độ/kích thước chi tiết của 1 cửa sổ cụ thể. |
| 27 | **9.7.11.6** | `PUT` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>` | 🖐️ Gửi tay | Di chuyển / thay đổi kích thước (move/resize) cửa sổ chỉ định. |
| 28 | **9.7.11.7** | `DELETE` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>` | ⚡ Tự động | *Đã tự động phủ qua thao tác "Đẩy xuống thiết bị" ở Tab 3 (dọn sạch window cũ).* |
| 29 | **9.7.11.8** | `PUT` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/bottom` | 🖐️ Gửi tay | Đưa cửa sổ chỉ định xuống lớp hiển thị dưới cùng (Bottom). |
| 30 | **9.7.11.13** | `PUT` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/<VWMWID>/top` | 🖐️ Gửi tay | Đưa cửa sổ chỉ định lên lớp hiển thị trên cùng (Top). |
| 31 | **9.7.11.14** | `GET` | `/ISAPI/DisplayDev/VideoWall/<videoWallID>/windows/capabilities` | 🖐️ Gửi tay | Đọc capability cấu hình cửa sổ của Video Wall. |

### Nhóm Scene (9.7.7) — Nhóm Tùy chọn mở rộng (không nằm trong 31 "Dùng chính")
- `9.7.7.4` `PUT /ISAPI/DisplayDev/VideoWall/<videoWallID>/scene/<sceneID>/saveData` (Lưu Scene) và `9.7.7.3` `PUT /ISAPI/DisplayDev/VideoWall/<videoWallID>/scene/<sceneID>/activate` (Kích hoạt Scene) đã được tự động gọi khi bấm nút **"Đẩy xuống thiết bị"** / **"Kích hoạt"** ở Tab 3. Không cần gửi tay lại, chỉ cần kiểm tra xác nhận 2 dòng log tương ứng xuất hiện trong khung Logs.

### ⛔ 3 lệnh cấm tuyệt đối gửi lên thiết bị thật ngoài chủ đích (chỉ thử trên MockServer)
1. `PUT /ISAPI/DisplayDev/ScreenCtrl/closeAll` không kèm `OutputID` (9.7.8.1).
2. `DELETE /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows` không lọc theo ID (9.7.11.3).
3. `PUT /ISAPI/System/reboot` hoặc `/ISAPI/System/factoryReset`.

---

## Checklist tổng hợp

- [ ] Vào app không có màn đăng nhập
- [ ] Tab 1 Trực tiếp (Direct Mode): Ping/Probe/Gửi lệnh ISAPI kết nối thẳng IP thiết bị
- [ ] Tab 1 Qua Service: Ping/Probe/Gửi lệnh ISAPI chạy đúng
- [ ] Tab 1: ComboBox chọn nhanh 31 API ISAPI "Dùng chính" tự điền Method & Path
- [ ] Tab 2: Tiền kiểm tra dữ liệu DB ↔ hiện trường thật (không gọi API thiết bị)
- [ ] Tab 3: tạo/sửa/xoá scene + cửa sổ cả 2 chế độ, DryRun không ghi thật
- [ ] Tab 4: CRUD lịch đầy đủ (tạm ẩn trong XAML, không test được qua UI hiện tại)
- [ ] Tab 4 hiện bị ẩn trong XAML — xác nhận lại với dev khi nào bật lại để test đầy đủ
- [ ] Khung Logs ở đáy: luôn thấy ở mọi tab, kéo dãn bằng splitter được, xem chi tiết XML đầy đủ (cột Thời gian/Nhóm/Mức/Chi tiết)
- [ ] 7 API tự động (Probe/SetupScene/Kích hoạt) đã xác nhận qua log Tab 1/3
- [ ] 24 API còn lại đã gửi tay đủ qua GroupBox ISAPI theo bảng Phụ lục (theo 8 nhóm), riêng 9.7.8.1 chỉ test trên MockServer
