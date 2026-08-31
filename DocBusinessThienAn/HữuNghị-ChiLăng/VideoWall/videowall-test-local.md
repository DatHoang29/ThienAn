# Thử công cụ WPF tại máy local (trước khi đi hiện trường)

> Hai cách chạy thử `Module.VideoWall.WPF` tại máy local trước khi sang hiện trường.
> Checklist đi test 2 ngày: [`videowall-test-2ngay.md`](videowall-test-2ngay.md).

| Cách | Cần gì | Thấy được gì | Thời gian |
|---|---|---|---|
| 1. Chạy test tự động | .NET SDK | luồng Direct chạy đúng với thiết bị giả lập | ~1 phút |
| 2. MockServer riêng + app WPF | .NET SDK, Windows | thao tác tay gần giống hiện trường, kiểm tra auto-log | ~10 phút |

---

## Cách 1 — Chạy bộ test tự động (đã drive MockServer sẵn)

```powershell
# Toàn bộ test WPF — cần TFM net10.0-windows
dotnet test tests/test.csproj -f net10.0-windows --filter "FullyQualifiedName~Tests.Modules.VideoWall.Wpf"
```

Xanh = luồng Ping / Probe / SendIsapi / thiết lập scene / guardrail chạy đúng với `VwISAPIMockServerHikvision` (thiết bị Hikvision giả lập, khởi động tự động trong test Host).

---

## Cách 2 — MockServer đứng riêng + mở app WPF thật *(giống hiện trường nhất)*

### 2.1. Bật MockServer

```powershell
dotnet run --project scripts/VwMockServerRunner
```

In ra:

```
[VwMockServerRunner] Giả lập thiết bị Hikvision DS-C66S-H88-CL tại http://127.0.0.1:18080/
[VwMockServerRunner] Port: 18080, 18081, 18082, 18083
[VwMockServerRunner] Account: admin | Password: Password123!
```

Để cửa sổ này chạy. Ctrl+C để dừng.

### 2.2. Mở app WPF, trỏ vào MockServer

```powershell
dotnet run --project src/Modules/VideoWall/Module.VideoWall.WPF
```

Thanh kết nối (Row 0):
- IP `127.0.0.1` · Port `18080` · Account `admin` · Password **bất kỳ** (mock không kiểm tra hash mật khẩu — chỉ cần account `admin` + có header Digest).
- Thanh kết nối gồm: IP, Port, Account, Password, WallNo, nút **🔍 Kết nối & Khảo sát** và thông báo trạng thái.

Thao tác kiểm tra (1 nút bấm duy nhất):
- Khi mới mở app: Toàn bộ 13 Tab bên dưới ở trạng thái **vô hiệu hóa và mờ nhẹ (`Opacity: 0.45`)**.
- Bấm **🔍 Kết nối & Khảo sát** → Hệ thống tự động kiểm tra kết nối mạng + xác thực Digest Auth + đọc đầy đủ thông số phần cứng: 2 wall (`VideoWall1`, `HoangNhu`), 2 output, 2 input channel, `maxWindowNums=512`, `maxSceneNums=128`, `isSupportScene=true`.
- Ngay sau khi thành công: Toàn bộ các Tab lập tức **mở khóa sáng rõ (100%)**, tự động chuyển danh sách Cổng ra và Nguồn tín hiệu sang Tab 1.
- **Tab 3–13** (11 nhóm ISAPI: Board, Decoding, Output...): Khung **Response** rộng toàn màn hình tự động hiển thị bên dưới TabControl (có GridSplitter kéo dãn linh hoạt) → gửi GET/PUT/POST ISAPI bất kỳ và xem ngay response XML/JSON.
- **Tab 1 "Thiết lập Scene" / Tab 2 "Kịch bản"**: Khung Response tự động ẩn gọn (`Height = 0`), nhường 100% diện tích cho bảng thiết lập và kịch bản.
- Mọi thao tác đều tự động ghi log vào file `%LOCALAPPDATA%\Module.VideoWall.WPF\Logs\session_*.jsonl`.

---

## MockServer giả lập được gì

| | Nội dung |
|---|---|
| ✅ Có | `Security/userCheck`, `DisplayDev/capabilities` + `.../VideoWall/capabilities`, `DisplayDev/VideoWall` (2 wall), `{id}/outputs` (2), `Video/inputs/channels` (2), windows CRUD (GET trả 2 cửa sổ), `scene/{id}/activate` · `/saveData` · `/isRunning`, `ScreenCtrl/closeAll`, Serial transparent, Digest 401→auth, + 116 route preset |
| ✅ Case lỗi (chỉ bật được **trong code test**, không qua HTTP) | `SimulateBadParameters`, `SimulateInvalidOperation`, `SimulateDeviceFailure`, `SimulateUnreachable`, `SimulateSaveDataFailure`, `SimulateNonceExpiry`, `FailedAuthLockoutThreshold` (khoá IP), hạ `MaxSceneNums` để test chặn SID |
| ⚠️ Khác thiết bị thật DS-C30S-S11 | `maxWindowNums = 512` (thật: **16**) · realm `DS-C66S-H88-CL` · chỉ **2** wall / **2** output / **2** input (thật: 8 wall, 12 output) · số liệu là ground-truth của **DS-C66S-H88-CL**, không phải DS-C30S-S11 |
| ❌ Không có | hình thật trên tường, độ trễ giải mã, băng thông, genlock |

---

## 💡 GIẢI THÍCH TỪ KHÓA CỐT LÕI (ĐỌC 2 PHÚT TRƯỚC KHI TEST)

Để dễ hình dung khi thao tác trên ứng dụng, hãy liên tưởng Video Wall như một **bức tường máy tính khổng lồ** trong phòng trung tâm chỉ huy:

| Thuật ngữ | Tên tiếng Anh | Giải thích dễ hiểu & Ví dụ thực tế |
|---|---|---|
| **Tường màn hình** | `Video Wall` | Bức tường hiển thị lớn được ghép từ nhiều màn hình LCD 55" (ví dụ: ghép 2 hàng × 3 cột = 6 màn hình). |
| **Cổng ra** | `Output Port` | Cổng cắm dây cáp (HDMI/DVI) từ bộ điều khiển vào từng chiếc màn hình. Cổng 1 cắm Màn hình 1, Cổng 2 cắm Màn hình 2. |
| **Nguồn tín hiệu** | `Input Source` | Các luồng hình ảnh đầu vào đưa vào bộ điều khiển (ví dụ: Nguồn 1 = Camera Nút giao A, Nguồn 2 = Camera Nút giao B, Nguồn 3 = Màn hình máy tính bản đồ giao thông). |
| **Cửa sổ** | `Window` | Một khung hình chữ nhật hiển thị video của 1 camera trên bức tường. Cửa sổ có thể nằm gọn trong 1 màn hình, hoặc phóng to kéo dài phủ qua 2, 4 màn hình cùng lúc (như phóng to cửa sổ trên Windows). |
| **Scene (Kịch bản hiển thị)** | `Scene / Layout` | Là **"một bức tranh toàn cảnh"** lưu lại trạng thái của toàn bộ bức tường tại một thời điểm: Có bao nhiêu cửa sổ, vị trí ở đâu, và mỗi cửa sổ đang phát camera nào. <br>• *Ví dụ*: `Scene Giờ cao điểm` (mở 16 camera nút giao), `Scene Ban đêm` (chiếu bản đồ ở giữa và 4 camera trạm thu phí), `Scene Khẩn cấp` (phóng to camera sự cố ra giữa tường). Giúp người trực đổi giao diện tường chỉ bằng 1 nút bấm trong 1 giây mà không phải kéo tay từng camera. |
| **Scenario (Kịch bản kiểm thử / Chuỗi API)** | `Scenario / Automation Sequence` | Là **"một chuỗi hành động động gồm nhiều bước API nối tiếp nhau theo thời gian"** (ví dụ: Bước 1 khảo sát cổng ra $\rightarrow$ Chờ 400ms $\rightarrow$ Bước 2 mở cửa sổ camera $\rightarrow$ Chờ 1000ms $\rightarrow$ Bước 3 chuyển nguồn). Giúp kỹ sư tự động hoá kiểm thử phần cứng, đo độ trễ và bắt mã lỗi mà không cần click tay từng bước. |
| **Bố cục cửa sổ trực quan** | `Visual Window Layout` | Công cụ tự động tính toán toạ độ pixel (X, Y, Rộng, Cao) trên giao diện thay vì người dùng phải tự tính toán số liệu thủ công. Chỉ cần tick chọn màn hình và nguồn, phần mềm tự tạo cửa sổ vừa khít màn hình. |
| **Đẩy cấu hình xuống thiết bị** | `Push to Device` | Khi bạn dựng Scene trên phần mềm, cấu hình đó mới chỉ lưu tạm trong máy tính bạn. Bấm **"🚀 ĐẨY XUỐNG THIẾT BỊ"** sẽ gửi chuỗi lệnh ISAPI xuống bộ điều khiển thật: Xoá cửa sổ cũ, mở các cửa sổ mới đúng vị trí và kích hoạt bức tranh Scene mới lên tường màn hình thật. |
| **Chạy thử** | `DryRun` | Chế độ "diễn tập an toàn": Phần mềm chạy thử quy trình đẩy cấu hình, kiểm tra toạ độ và in ra màn hình log, nhưng **chưa gửi lệnh ghi thật** xuống thiết bị, giúp bạn kiểm tra trước mà không sợ làm loạn màn hình thật. |
| **Lớp xếp chồng** | `Z-Index` | Thứ tự đè lên nhau của các cửa sổ: `Z-Index = 1` nằm ở dưới làm nền, `Z-Index = 2` nằm đè lên trên (như cửa sổ bản đồ to ở dưới, ô camera nhỏ đè lên góc). |

> 📌 **Phân biệt quan trọng giữa Tab 1 và Tab 2**:
> - **Tab 1: "Thiết lập Scene"** = Quản lý **Bố cục màn hình tĩnh (Scene)** (dựng cách xếp các ô camera hiển thị trên tường, lưu thành Scene và đẩy xuống thiết bị).
> - **Tab 2: "Kịch bản (Scenario)"** = Quản lý **Chuỗi kiểm thử tự động (Scenario / Automation Sequence)** (chạy một chuỗi nhiều bước gọi API liên tiếp nhau để kiểm tra phần cứng hoặc đo đạc).

---

## Kịch bản test FULL tính năng WPF với MockServer (nắm rõ toàn bộ công cụ)

> Phạm vi Direct Mode only ([`videowall-kien-truc-van-hanh.md` §A3](videowall-kien-truc-van-hanh.md#a3-ràng-buộc-phạm-vi--chỉ-2-tầng-wpf--thiết-bị)): **bỏ Backend mode, tab Lịch, "Bắn 2 trigger" / "So khớp ưu tiên"**.
> Ký hiệu: ✅ chạy thật, kỳ vọng đúng · ⚠️ biết trước là đang lỗi, cứ thử để thấy đúng hiện tượng, đừng tìm nguyên nhân ở cấu hình của bạn · 🚫 đừng test, ngoài phạm vi.

### Bước 0 — Bật MockServer + mở app
1. [ ] Terminal 1, tại `TA-ITS015-WEBAPI-V1.0`: `dotnet run --project scripts/VwMockServerRunner` → để chạy nền.
2. [ ] Terminal 2, cùng thư mục: `dotnet run --project src/Modules/VideoWall/Module.VideoWall.WPF` → mở app.
3. [ ] Nhập **IP** `127.0.0.1` · **Port** `18080` · **Account** `admin` · **Password** bất kỳ (mock không kiểm tra mật khẩu).

### Bước 1 — Kết nối & Khảo sát thiết bị
1. [ ] Rê chuột vào nút **Ping** và **Probe** → kỳ vọng ToolTip hiển thị giải thích rõ ràng, tự động ngắt dòng và nằm gọn trong cửa sổ app.
2. [ ] Bấm **Ping** → kỳ vọng OK (xác nhận kết nối mạng + xác thực Digest Auth thành công).
3. [ ] Bấm **Probe** (bắt buộc bấm để lấy thông số phần cứng) → kỳ vọng: Đọc được 2 wall (`VideoWall1`, `HoangNhu`), 2 output, 2 input, `maxWindowNums=512`, `maxSceneNums=128`. Sau khi Probe xong, app tự động chuyển danh sách Cổng ra và Nguồn tín hiệu sang Tab 1.

---

### Bước 2 — Tab 1 "Thiết lập Scene" (Dựng bố cục các ô camera và đẩy xuống màn hình) ✅

Tab này được thiết kế thành **4 Khung chức năng** từ trên xuống dưới. Bạn có thể kéo rê 2 thanh ngang xám mỏng (`GridSplitter`) giữa Khung 1-2 và Khung 2-3 để điều chỉnh độ cao theo ý muốn.

#### 2.1. Khung 1 — Thiết lập Kịch bản (Scene)
1. [ ] **Nạp 3 Scene mẫu thực tế**: Bấm nút **"📋 Nạp 3 Scene mẫu"** trên thanh công cụ Khung 1. Hệ thống sẽ tự động tạo sẵn 3 kịch bản thực tế chuẩn mẫu:
   - **Scene 1 (Giờ cao điểm: 12 Cam nút giao)**: Mở 12 camera giám sát nút giao trọng điểm, chia lưới đều 4×3 (kích thước 480×360 mỗi ô, phủ kín toàn bộ tường 1920×1080).
   - **Scene 2 (Ban đêm: Bản đồ sự cố & 4 Trạm thu phí)**: Mở Bản đồ sự cố toàn tuyến chiếm 2/3 màn hình bên trái (1280×1080) và 4 ô camera trạm thu phí bên phải (640×270 mỗi ô).
   - **Scene 3 (Khẩn cấp: Phóng to camera tai nạn)**: Phóng to 1 camera duy nhất chiếm 100% diện tích toàn màn hình (1920×1080) tại điểm xảy ra sự cố.
   - *Kiểm tra*: Bấm vào ô chọn kịch bản ("Kịch bản:"), chọn lần lượt từng Scene và quan sát Khung 3 ("Cửa sổ đã lưu") tự động tải danh sách các cửa sổ tương ứng.
2. [ ] Tạo thêm Scene thủ công theo ý muốn: Nhập tên `SCENE_TUY_CHINH` vào ô Tên kịch bản → bấm **"➕ Tạo mới"** → Tên kịch bản vừa tạo sẽ xuất hiện ở ô chọn kịch bản hiện tại.

#### 2.2. Khung 2 — Dựng cửa sổ (Windows)
Chọn 1 trong 2 chế độ dựng bố cục:

* **Chế độ A — Màn KHÔNG chồng (Từng cổng ra riêng biệt)**:
  - Đây là chế độ cơ bản: Mỗi chiếc màn hình LCD sẽ chiếu 1 camera độc lập, vừa khít 100% màn hình, không đè lên nhau.
  - Thao tác: Tick chọn 2 màn hình trong bảng → chọn Nguồn camera cho từng màn → bấm **"▶ Dựng cửa sổ phủ kín"**.
  - Kết quả: Tại Khung 3 ("Cửa sổ đã lưu"), bạn sẽ thấy 2 cửa sổ được sinh ra tương ứng với 2 màn hình.

* **Chế độ B — Màn CHỒNG (Cửa sổ xếp lớp / Picture-in-Picture)**:
  - Dùng khi muốn 1 cửa sổ to làm nền (ví dụ bản đồ giao thông), và 1 cửa sổ camera nhỏ đè lên một góc.
  - Thao tác: Bấm chuyển sang tab con **"Màn CHỒNG (Cửa sổ xếp lớp)"** → bấm **"➕ Thêm dòng cửa sổ"** → đặt ô thứ nhất ZIndex 1 (làm nền), ô thứ hai ZIndex 2 (đè lên) → bấm **"▶ Dựng cửa sổ xếp lớp"**.
  - Kết quả: Các cửa sổ với toạ độ xếp lớp được lưu vào Khung 3.

#### 2.3. Khung 3 — Cửa sổ đã lưu của Kịch bản
1. [ ] Xem danh sách các cửa sổ vừa được dựng cho Scene hiện tại (gồm STT, Tên, Nguồn, toạ độ X, Y, W, H, Z).
2. [ ] Thử sửa trực tiếp toạ độ hoặc tên trên một dòng → bấm biểu tượng **💾** ở cuối dòng để lưu.
3. [ ] **Thao tác chọn nhiều & xoá hàng loạt**:
   - Tick vào CheckBox ở đầu từng dòng muốn xoá (hoặc tick vào ô CheckBox trên cùng của tiêu đề cột để **Chọn tất cả / Bỏ chọn tất cả**).
   - Bấm nút **"🗑 Xoá các mục đã chọn"** → Hộp thoại xác nhận hiện ra → bấm **Có** để xoá các dòng đã chọn khỏi Scene.
   - Thử tick chọn lại, bấm xoá và bấm **Không** → kiểm tra các cửa sổ vẫn còn nguyên vẹn.

#### 2.4. Khung 4 — Đẩy xuống thiết bị & Dòng trạng thái
1. [ ] Để checkbox **"Chạy thử (DryRun)"** được bật (mặc định) → bấm nút **"🚀 ĐẨY XUỐNG THIẾT BỊ"**:
   - Phần mềm sẽ tính toán và mô phỏng từng bước (xoá cửa sổ cũ, mở cửa sổ mới, lưu Scene) và in ra log màu xanh báo thành công, nhưng **chưa gửi lệnh ghi thật** xuống thiết bị.
2. [ ] Bỏ tick **"Chạy thử (DryRun)"** → hộp thoại cảnh báo hiện ra nhắc nhở sắp ghi thật xuống thiết bị → bấm **Có** → bấm **"🚀 ĐẨY XUỐNG THIẾT BỊ"**:
   - Thiết bị MockServer nhận lệnh thật và trả về `statusCode 1 (OK)` cho từng bước. Toàn bộ bố cục Scene đã được ghi đè và kích hoạt thật sự trên thiết bị!

---

### Bước 3 — Tab 2 "Kịch bản (Scenario)" (Tự động hoá chuỗi API & Kiểm thử phần cứng)

> 💡 **Giải thích Keyword: Scenario (Kịch bản kiểm thử / Chuỗi API tự động) là gì?**
> - **Định nghĩa:** Khác với `Scene` (chỉ là ảnh chụp bố cục tĩnh các ô camera trên màn hình lớn ở Tab 1), **Scenario** là một **kịch bản chuỗi hành động động** gồm nhiều bước gọi API ISAPI liên tiếp nhau theo thời gian, có thời gian chờ (delay mili-giây) giữa các bước.
> - **Ví dụ thực tế**:
>   * *Bước 1:* Gọi API đọc danh sách cổng ra màn hình (`GET /ISAPI/DisplayDev/ScreenCtrl/channels`).
>   * *Chờ 400ms.*
>   * *Bước 2:* Gọi API mở cửa sổ camera số 1 (`PUT /ISAPI/DisplayDev/ScreenCtrl/openWindow`).
>   * *Chờ 1000ms.*
>   * *Bước 3:* Gọi API chuyển sang nguồn camera số 2 (`PUT /ISAPI/DisplayDev/ScreenCtrl/switchSource`).
> - **Mục đích của Tab 2 (Scenario):** Giúp kỹ sư tự động hoá kiểm thử phần cứng, đo độ trễ của thiết bị Hikvision và diễn tập các kịch bản vận hành mà không cần click chuột gọi từng API thủ công.
> - **Tính năng Resume (Chạy tiếp từ bước lỗi):** Nếu kịch bản đang chạy 10 bước mà bước 4 bị rớt mạng hoặc timeout, nút chạy sẽ tự động đổi thành **"▶ Chạy tiếp từ bước #4"** để tiếp tục mà không cần chạy lại từ đầu.

**3a. Danh sách Scenario gộp (cột trái "1. Danh sách kịch bản (Scenario)")** ✅
1. [ ] Nhìn cột trái → thấy **3 mục 📦 built-in ở đầu danh sách** ("📦 1. Thiết lập scene (không chụp hình)", "📦 2. ... (có chụp hình)", "📦 3. Active scene"), tiếp theo là các Scene bạn vừa tạo ở Tab 1 (`SCENE_BAN_NGAY`, v.v.).
2. [ ] Chọn 1 mục 📦 built-in → nút **"Xoá"** tự mờ đi (bảo vệ kịch bản mặc định của hệ thống).
3. [ ] Chọn 1 mục do bạn tự tạo → nút **"Xoá"** sáng lên để có thể xoá.

**3b. Tự soạn chuỗi API chạy tự động (Builder)** ✅
1. [ ] Bấm **"+ Tạo mới"** → chọn 2-3 API ở bảng danh sách bên phải (nhóm Board, Scene...) → bấm **"➕ Thêm bước"** cho mỗi API.
2. [ ] Đặt thời gian chờ giữa các bước tại ô **"Chờ (ms)"** (ví dụ: 500ms) → bấm **"💾 Lưu"** với tên `TEST_CHUOI_API`.
3. [ ] Bấm **"▶ CHẠY KỊCH BẢN"** → log sẽ chạy tuần tự từng bước và đếm ngược thời gian chờ.
4. [ ] Nếu có một bước bị lỗi giữa chừng → nút sẽ tự đổi thành **"▶ Chạy tiếp từ bước #N"** để bạn tiếp tục chạy từ bước lỗi sau khi khắc phục mạng, không cần chạy lại từ đầu.

**3c. Kịch bản dựng sẵn "Thiết lập scene" (📦 mục 1 và 2)** ⚠️ ĐANG LỖI
1. [ ] Chọn mục 📦 "1. Thiết lập scene (không chụp hình)" → bấm **"▶ CHẠY KỊCH BẢN"**.
2. [ ] Kỳ vọng thực tế: Nhánh Direct Mode này hiện tại chỉ gọi lệnh đọc (GET danh sách cửa sổ) chứ **chưa tạo mới cửa sổ trên thiết bị**. Vì vậy, muốn dựng và đẩy Scene thật, **hãy luôn dùng Tab 1**.

**3d. "📐 2 nguồn tranh vùng — canh kích thước"** ✅
1. [ ] Nhập thông số kích thước và ID kịch bản thử → bấm **"▶ Chạy kiểm thử tranh vùng"**.
2. [ ] Kỳ vọng: Phần mềm gửi lệnh thật xuống thiết bị và log báo số lượng cửa sổ thực tế được tạo trên thiết bị.

**3e. "🧪 Bộ kiểm thử lỗi (3 trường hợp biên)"** ✅
1. [ ] Quan sát ô checkbox **"Gửi thật để xem mã lỗi thiết bị"**:
   - Mặc định **TẮT** (an toàn): Phần mềm tự dựa vào thông số từ nút Probe để chặn sớm các lệnh sai (ví dụ tạo 999 cửa sổ vượt quá giới hạn), không gửi rác xuống thiết bị.
   - Bật **BẬT**: Cho phép request sai được gửi thật xuống thiết bị / MockServer để ghi nhận mã lỗi chính xác mà hãng Hikvision trả về.
2. [ ] Bấm **"Chạy bộ kiểm thử lỗi"** → kỳ vọng cả 3 dòng log báo **[PASS]**:
   - Case A: Tạo 999 cửa sổ (vượt quá 512) → Thiết bị từ chối.
   - Case B: 1 nguồn camera gán cho 3 cửa sổ khác nhau → Thiết bị chấp nhận (cho phép 1 camera chiếu nhiều ô).
   - Case C: Scene ID 99999 (vượt quá 128) → Thiết bị từ chối.

---

### Bước 4 — Kiểm thử các trường hợp lỗi phần cứng sâu (Qua mã nguồn Test)
1. [ ] Tại terminal thư mục `c:\ThienAn`, chạy lệnh:
   ```powershell
   dotnet test tests/test.csproj -f net10.0-windows --filter "FullyQualifiedName~Tests.Modules.VideoWall.Wpf"
   ```
2. [ ] Kỳ vọng: **Toàn bộ 110/110 tests đều PASS (màu xanh)** — bao gồm các tình huống mất kết nối mạng, sai mật khẩu làm khoá IP, token hết hạn, kiểm tra xoá cửa sổ hàng loạt và các ràng buộc bố cục giao diện tự động.

---

### Bước 5 — Kiểm tra File Log ghi lại phiên làm việc
1. [ ] Log được tự động ghi liên tục vào file: `%LOCALAPPDATA%\Module.VideoWall.WPF\Logs\session_{yyyyMMdd_HHmmss}.jsonl`.
2. [ ] Bấm nút **"Xuất log"** ở góc trên bảng Logs của app nếu muốn xuất file `.json` gộp để gửi báo cáo.

---

### Bảng Tổng Kết Trạng Thái Các Chức Năng
| Chức năng | Trạng thái | Hướng dẫn thao tác |
|---|---|---|
| **Tab 1 — Thiết lập Scene** | ✅ Hoàn chỉnh | Dựng bố cục camera trực quan, hỗ trợ chọn nhiều & xoá hàng loạt, kéo dãn khung linh hoạt |
| **Tab 2 — Kịch bản tự động hoá** | ✅ Hoàn chỉnh | Chạy chuỗi API tự động, có tính năng tiếp tục chạy từ bước lỗi (Resume) |
| **Tab 2 — Bộ kiểm thử lỗi** | ✅ Hoàn chỉnh | Có công tắc gửi thật xem mã lỗi Hikvision |
| **Tab 3..13 — 11 nhóm ISAPI** | ✅ Hoàn chỉnh | Tra cứu và gửi lệnh ISAPI trực tiếp, màn hình phản hồi XML/JSON toàn diện |
| **Ghi Log tự động** | ✅ Hoàn chỉnh | Tự động ghi file JSONL ngầm liên tục, có nút xuất file nhanh |
| **Kiểm thử tự động** | ✅ 110/110 PASS | Bao phủ đầy đủ các kịch bản biên, giao diện và phần cứng |

