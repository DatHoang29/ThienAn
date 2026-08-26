# Hướng Dẫn Sử Dụng Postman Collection — VideoWall ISAPI

Bộ Postman Collection và Environment phục vụ đo kiểm toàn diện giao thức ISAPI trên thiết bị điều khiển Video Wall Hikvision **`DS-C66S-H88-CL`** thông qua API Backend Thiên An (`/api/videowall/vwdevicesetup/*`) hoặc bắn trực tiếp xuống thiết bị.

---

## 📁 1. Danh Sách Tệp Giao Nộp

| Tệp | Đường dẫn tương đối | Chức năng |
|---|---|---|
| **Collection** | `CollectionPostman/VideoWallISAPI.postman_collection.json` | Bộ kịch bản 8 folder (127 requests) phủ toàn bộ nghiệp vụ và ISAPI 09A/09B |
| **Environment** | `EnvPostman/VideoWallISAPI.postman_environment.json` | Bộ biến môi trường mẫu (Backend URL, IP thiết bị, ID tường/cửa sổ/kênh) |
| **Hướng dẫn** | `README.md` | Hướng dẫn import, cấu hình biến, chạy Postman & Newman |

---

## 📥 2. Hướng Dẫn Import Vào Postman

1. Mở ứng dụng **Postman** (khuyến nghị phiên bản Desktop v10+ hoặc v11+).
2. Nhấn nút **Import** (góc trên bên trái).
3. Kéo thả hoặc chọn 2 tệp:
   - `CollectionPostman/VideoWallISAPI.postman_collection.json`
   - `EnvPostman/VideoWallISAPI.postman_environment.json`
4. Ở góc trên bên phải của Postman, chọn Environment là **`VideoWallISAPI`**.

---

## ⚙️ 3. Cấu Hình Biến Môi Trường (Environment Variables)

Mở Environment **`VideoWallISAPI`** và điền các giá trị vào cột **Current Value** (tuyệt đối không commit mật khẩu vào Initial Value):

| Tên biến | Kiểu | Giá trị mặc định | Giải thích |
|---|---|---|---|
| `beBase` | default | `http://localhost:5005` | Địa chỉ gốc của Backend Thiên An API (`Urls` trong `App.json`) |
| `deviceScheme` | default | `http` | Giao thức kết nối tới thiết bị (`http`) |
| `deviceIp` | default | `10.10.9.236` | Địa chỉ IP của bộ điều khiển Hikvision trên mạng LAN |
| `devicePort` | default | `80` | Cổng ISAPI HTTP của thiết bị (mặc định `80`, test mock `18080`) |
| `deviceUser` | default | `admin` | Tên tài khoản quản trị thiết bị |
| `devicePass` | secret | *(Điền tay)* | Mật khẩu thiết bị (Điền vào *Current value*) |
| `controllerId` | default | *(Để trống)* | ID bản ghi CSDL `VwController` (nếu muốn đi đường DB thay vì `device.ip`) |
| `sceneId` | default | *(Để trống)* | ID kịch bản CSDL khi test `setupscene` |
| `sceneSid` | default | `1` | Số hiệu Scene trên thiết bị ISAPI (1..N) |
| `wallSandbox` | default | `1` | Số hiệu tường Sandbox (`unbound` — không cắm màn hình) để thử nghiệm ghi an toàn |
| `wallLive` | default | `2` | Số hiệu tường thật (`bound` — đang chiếu ra 2 màn hình ghép) |
| `outputId` | default | `17235971` | ID cổng Output đang cắm màn trên (Board 7 - Port 3) |
| `inputId` | default | `16842753` | ID kênh Input đang có tín hiệu phát (Board 1 - Port 1) |
| `windowId` | default | `33554433` | ID cửa sổ hiện hành trên Wall 2 |
| `subWindowId` | default | `1` | ID cửa sổ con |
| `allowLive` | default | `no` | **Cờ bảo vệ Wall 2**: Đổi thành `yes` khi muốn chạy Folder 4 tác động màn thật |
| `allowDanger` | default | `no` | **Cờ bảo vệ lệnh cấm**: Đổi thành `yes` khi cho phép chạy Folder 9 |

---

## 🗂️ 4. Cấu Trúc Các Folder Trong Collection

```text
VideoWallISAPI/
├── 0. Thiên An APIs (typed)                     (6 requests - Luồng API nghiệp vụ cấp cao)
├── 1. Vòng 1 — Khám phá & định danh (chỉ GET)   (9 requests - userCheck, deviceInfo, capabilities)
├── 2. Vòng 2 — Khảo sát tường & layout (chỉ GET)(17 requests - VideoWall, outputs, inputs, windows)
├── 3. Vòng 3 — Ghi trên sandbox Wall 1          (9 requests - Đổi tên, tạo window, Rect, Scene)
├── 4. Vòng 4 — Tác động tường thật (Wall 2)     (4 requests - Kéo phủ màn, setupscene thật) [Guard]
├── 5. Mở rộng 09A (chỉ đọc)                     (52 requests - 8 tiểu mục chuyên sâu)
│   ├── 9.7.2 Trạng thái giải mã (Decoding Device Status) (9 requests)
│   ├── 9.7.3 & 9.7.8 Cấu hình âm thanh (Audio Configuration) (1 request)
│   ├── 9.7.4 Nguồn tín hiệu đầu vào (Signal Source) (19 requests)
│   ├── 9.7.6 Kế hoạch & Lịch trình (Plan & Schedule) (3 requests)
│   ├── 9.7.7 Quản lý Scene (Scene Management) (2 requests)
│   ├── 9.7.9 Màn hình ảo Virtual LED (Virtual LED Display) (4 requests)
│   ├── 9.7.10 Ảnh nền BaseMap (BaseMap Background) (5 requests)
│   └── 9.7.11 Quản lý cửa sổ con & PreEdit (Sub-Window & Pre-Edit) (9 requests)
├── 9. ⛔ Lệnh cấm — KHÔNG chạy                  (4 requests - closeAll không ID, factoryReset...) [Guard]
└── D. Gọi trực tiếp thiết bị (không qua backend)(26 requests - Đối chiếu XML thô Digest Auth)
    ├── D.1 Định danh & Năng lực thiết bị (Trực tiếp) (9 requests)
    └── D.2 Khảo sát Layout & Kênh (Trực tiếp) (17 requests)
```

---

## 🧪 5. Cách Chạy Tự Động Với Newman

### 5.1. Chạy Folder 0 với Mock Server cục bộ (Port 18080)
Khi Backend Thiên An đang chạy và Mock Server test đang mở:

```bash
newman run DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/API/CollectionPostman/VideoWallISAPI.postman_collection.json \
  -e DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/API/EnvPostman/VideoWallISAPI.postman_environment.json \
  --folder "0. Thiên An APIs (typed)" \
  --env-var "deviceIp=127.0.0.1" \
  --env-var "devicePort=18080" \
  --env-var "deviceUser=admin" \
  --env-var "devicePass=Password123!"
```

### 5.2. Chạy Vòng 1 & Vòng 2 khảo sát thiết bị thật (10.10.9.236)
```bash
newman run DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/API/CollectionPostman/VideoWallISAPI.postman_collection.json \
  -e DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/API/EnvPostman/VideoWallISAPI.postman_environment.json \
  --folder "1. Vòng 1 — Khám phá & định danh (chỉ GET)" \
  --folder "2. Vòng 2 — Khảo sát tường & layout (chỉ GET)" \
  --env-var "devicePass=MatKhauThatCuaThietBi"
```

---

## 🛡️ 6. Cơ Chế Guard Bảo Vệ An Toàn

1. **Folder 4 (Tác động tường thật)**: Tự động gọi `pm.execution.skipRequest()` nếu `allowLive !== 'yes'`.
2. **Folder 9 (Lệnh cấm phá hủy)**: Tự động chặn toàn bộ request nếu `allowDanger !== 'yes'`.
3. *(Lưu ý: Nếu dùng phiên bản Postman cũ không hỗ trợ `pm.execution.skipRequest`, script sẽ fallback sang `pm.execution.setNextRequest(null)`).*

---

## ⚠️ 7. Cảnh Báo An Toàn Tuyệt Đối (Bẫy #10)

- **KHÓA IP THIẾT BỊ**: Nhập sai mật khẩu 2 lần liên tiếp sẽ khiến thiết bị kích hoạt chế độ khóa IP (`illaccess`). **Không thể mở lại qua mạng LAN**.
- **KHÔNG THỬ MẬT KHẨU SAI TRÊN THIẾT BỊ THẬT**: Nếu muốn kiểm tra tính năng Digest sai hoặc Circuit Breaker, luôn trỏ tới Mock Server (`127.0.0.1:18080`).
