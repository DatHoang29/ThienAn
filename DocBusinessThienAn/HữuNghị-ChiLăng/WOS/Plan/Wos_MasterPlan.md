# Wos_MasterPlan — Kế hoạch Tổng thể & Báo cáo Khả thi Tích hợp Trạm Quan Trắc Thời Tiết (WOS) Campbell CR1000X

> 🔴 **Single Source of Truth (SSOT)**: File này là **Sổ theo dõi MasterPlan DUY NHẤT** của phân hệ Trạm Quan trắc Thời tiết (WOS) dự án Cao tốc Hữu Nghị – Chi Lăng.  
> Tích hợp toàn bộ **Báo cáo khả thi kỹ thuật** (Rev. 2, 10/09/2026), **Quyết định kiến trúc**, **Đặc tả Register Map JSON** và **Kế hoạch triển khai 6 Phase** cho thiết bị Datalogger Campbell CR1000X.  
> Tuân thủ tuyệt đối quy tắc Rule 13 & 19.52 (`.agents/rules/thienan_rules.md`): Mỗi phân hệ chỉ có DUY NHẤT 1 file MasterPlan sống, cấm đẻ file rời rạc theo ngày tháng.

---

## 🧭 Bảng Theo Dõi Tiến Độ Tổng Thể (Master Checklist)

| Phase | Hạng mục | Trạng thái | Ghi chú & Chi tiết |
|:---:|---|:---:|---|
| **Phase 1** | Entity CSDL `WosStation` + Mở rộng `TmsWeather` + CRUD API | ⏳ Sẵn sàng | Đã có schema SQL DDL và thiết kế Entity SqlSugar |
| **Phase 2** | Thư viện Modbus TCP Client + Codec `Cr1000XCodec` | ⏳ Sẵn sàng | Sử dụng thư viện `FluentModbus` 5.3.2 (đã xác minh) |
| **Phase 3** | Worker thu thập định kỳ (`Services.Wos.Worker`) + Pipeline | ⏳ Sẵn sàng | Pattern Device Acquisition Runner + Wolverine message queue |
| **Phase 4** | Cấu hình Host `Database.json`, `Logging.json` + WPF Monitor | ⏳ Sẵn sàng | Đồng bộ DB với `TAC_WebAPI` |
| **Phase 5** | Bộ giả lập kiểm thử `FakeWosModbusClient` | ⏳ Sẵn sàng | Cho phép test luồng hoàn chỉnh không cần thiết bị thật |
| **Phase 6** | Tích hợp giao diện Frontend Vue 3 (`weatherInfo.vue`, `wosInfo.vue`) | ⏳ Chờ BE | Thay mock `Math.random()` bằng API thật |

---

## 1. Mục tiêu Tích Hợp & Thông Tin Thiết Bị

Hệ thống trạm quan trắc thời tiết khí tượng WOS gồm 3 cảm biến gắn trên cột trạm cao 10m nối vào Datalogger **Campbell Scientific CR1000X / CR1000Xe**:

| Thông số thu thập | Đơn vị | Cảm biến hiện trường | Cơ chế đo & Giao thức |
|---|:---:|---|---|
| **Nhiệt độ không khí** | °C | Campbell HygroVUE5 | Đo qua chuẩn số SDI-12 (CR1000X map vào Holding Register) |
| **Độ ẩm tương đối** | %RH | Campbell HygroVUE5 | Đo qua chuẩn số SDI-12 (CR1000X map vào Holding Register) |
| **Lượng mưa tích lũy / tức thời** | mm | Campbell TB4 | Tiếp điểm thùng lật switch-closure (P1/P2 xung) |
| **Tốc độ gió** | m/s | R. M. Young 05103 | Phát xung xoay chiều AC (chân đếm xung P1/P2) |
| **Hướng gió** | 0–360° | R. M. Young 05103 | Biến trở góc quay ratiometric (kênh tương tự SE / VX) |
| **Điện áp ắc quy trạm** | V | Nguồn nội bộ CR1000X | Bảng đo pin nội bộ (BatteryVoltage) |

### Hiện trạng vật tư & Phần mềm:
- **Phần cứng hiện trường**: Nghiệm thu vật tư "Đạt" 3/3 bộ mỗi loại (theo hồ sơ NTĐV `C2.TAP III.Q2.1.3` và `Q2.2.3`).
- **Phần mềm backend TA-ITS015**: Hiện **chưa có code** đọc Modbus từ CR1000X. Bảng `TmsWeather` hiện tại chỉ có các cột cơ bản phục vụ API thời tiết OpenWeatherMap (chưa từng chạy thật trên tuyến).
- **Hướng giải quyết**: Xây dựng service độc lập `Services.Wos` đọc Modbus TCP từ CR1000X, ghi nhận vào `TmsWeather` (được mở rộng thêm cột `StationId`, `BatteryVoltage`, `Source`), theo đúng pattern device-driver của repo (`Services/Sample`, `Services/VDS`).

---

## 2. Báo Cáo Khả Thi Kỹ Thuật (Feasibility Report)

| Tiêu chí | Đánh giá | Chi tiết giải pháp |
|---|:---:|---|
| **Giao thức kết nối** | ✅ Khả thi | **Modbus TCP** qua cổng RJ45 Ethernet. CR1000X đóng vai trò Server (Slave), Backend TA-ITS015 đóng vai trò Client (Master). |
| **Thư viện C#/.NET** | ✅ Khả thi | **`FluentModbus` 5.3.2** (MIT License, hỗ trợ .NET Standard 2.0/2.1 & .NET 10). Hỗ trợ đọc trực tiếp `ReadHoldingRegistersAsync<float>()` big-endian 32-bit. |
| **Khớp nối dữ liệu** | ✅ Khả thi | Cảm biến HygroVUE5 (SDI-12) được CR1000X đọc nội bộ và nạp vào cùng bảng Holding Registers; backend chỉ cần đọc qua 1 kết nối Modbus TCP duy nhất. |
| **Linh hoạt cấu hình** | ✅ Đạt | Bảng thanh ghi (`RegisterMapJson`) lưu theo cấu hình CSDL `WosStation`, không hard-code địa chỉ thanh ghi trong source code. |

---

## 3. Quyết Định Kỹ Thuật Cốt Lõi (Architecture Decisions)

1. **Thư viện Modbus TCP Client — Chọn `FluentModbus`:**
   - CR1000X trả số đo dạng số thực 32-bit (`float`), chiếm 2 thanh ghi liên tiếp (32-bit float = 2 × 16-bit register).
   - `FluentModbus` hỗ trợ generic `ReadHoldingRegistersAsync<float>(unitId, startAddress, count, cancellationToken)` và cấu hình `ModbusEndianness`.
   - Function Code sử dụng: **FC03 (Read Holding Registers)**.
2. **Xử lý Byte Order động (`byteOrder`):**
   - Theo tài liệu chính thức Campbell Scientific, thứ tự byte của số thực float (`ABCD`, `BADC`, `CDAB`, `DCBA`) là tham số cấu hình trong `ModbusServer()` của chương trình CRBasic từng trạm, không cố định trên mọi thiết bị.
   - Do đó, field `byteOrder` được đưa vào cấu hình JSON của từng trạm (`WosStation.RegisterMapJson`), mặc định `"ABCD"` (`ModbusEndianness.BigEndian`).
3. **Mở rộng `CanonicalDeviceMessage` (Kế thừa Pattern Repo):**
   - Thêm thuộc tính `Channel` (nullable, additive) vào `CanonicalDeviceMessage.cs`.
   - Codec gọi `Decode` cho từng kênh thu thập, không phá vỡ contract của `IDeviceCodec` hiện có.
4. **Tách biệt Quy đổi Đơn vị / Tỷ lệ Scale:**
   - Thực hiện tại tầng `WosStationAcquisitionService`, **không** làm trong Codec. Lấy `scale` và `offset` từ JSON cấu hình (ví dụ 0.254 mm/tip hoặc 0.2 mm/tip).
5. **Chu kỳ Polling:**
   - Mặc định 60 giây (`Service:Wos:PollIntervalSeconds`), phù hợp với chu kỳ lưu bảng trung bình 1 phút của CR1000X.
6. **Bảo toàn CSDL `TmsWeather`:**
   - Giữ nguyên tên cột có sẵn trong CSDL kể cả trường hợp có typo lịch sử (`Hudmidity`), thêm alias/wrapper nếu cần để không phá vỡ hợp đồng dữ liệu với các module khác.
7. **Kiến trúc Worker:**
   - Tách thành **`Services.Wos.Worker`** riêng biệt (chạy nền độc lập), ghi CSDL thông qua SqlSugar ORM.

---

## 4. Đặc Tả Schema Register Map JSON (`WosStation.RegisterMapJson`)

```json
{
  "unitId": 1,
  "tableIntervalSeconds": 60,
  "accumulationMode": "delta",
  "byteOrder": "ABCD",
  "channels": [
    { "code": "TEMP", "parameter": "temperature",  "campbellRegister": 30001, "unit": "C",   "scale": 1.0,   "offset": 0.0 },
    { "code": "RH",   "parameter": "humidity",      "campbellRegister": 30003, "unit": "%",   "scale": 1.0,   "offset": 0.0 },
    { "code": "RAIN", "parameter": "rainfall",       "campbellRegister": 30005, "unit": "mm",  "scale": 0.254, "offset": 0.0, "note": "TB4 tip resolution — kiểm tra CRBasic thực tế" },
    { "code": "WSPD", "parameter": "windSpeed",      "campbellRegister": 30007, "unit": "m/s", "scale": 1.0,   "offset": 0.0 },
    { "code": "WDIR", "parameter": "windDirection",  "campbellRegister": 30009, "unit": "deg", "scale": 1.0,   "offset": 0.0 },
    { "code": "VBAT", "parameter": "batteryVoltage", "campbellRegister": 30011, "unit": "V",   "scale": 1.0,   "offset": 0.0 }
  ]
}
```
*Ghi chú:* `campbellRegister` lưu địa chỉ 1-based theo chuẩn Campbell (offset 30000/40000); tầng helper sẽ strip offset để ra địa chỉ wire 0-based khi truyền cho FluentModbus.

---

## 5. Kế Hoạch Triển Khai 6 Giai Đoạn (Implementation Breakdown)

### Phase 1 — Entity & CRUD Trạm Khí Tượng (WebAPI)
- [ ] Mở rộng Entity `TmsWeather`: Bổ sung `StationId` (long?), `BatteryVoltage` (float?), `Source` (string?: "wos"|"openweathermap").
- [ ] Tạo Entity `WosStation` (`Module.Wos.Core/Entities/WosStation.cs`):
  - Thuộc tính: `Id`, `Code`, `Name`, `KmNumber`, `ModbusHost`, `ModbusPort`, `ModbusUnitId`, `PollIntervalSeconds`, `RegisterMapJson`, `Enabled`, `LastPolledAt`, `LastStatus`.
- [ ] Xây dựng WebAPI CRUD cho trạm WOS: `WosStationController`, Command/Query Handlers, DTO Input/Output, Validators.
- [ ] Soạn script migration SQL: `SQL/Scripts/WOS/001_create_wos_station.sql`, `002_alter_tmsweather_add_wos_columns.sql`.

### Phase 2 — Modbus Client & Codec CR1000X (Protocol Layer)
- [ ] Tạo project `Services.Wos.CR1000X` (target `netstandard2.0`, ref `FluentModbus`).
- [ ] Hiện thực `Cr1000XCodec` kế thừa `IDeviceCodec`: Decode byte float big-endian ↔ `CanonicalDeviceMessage`.
- [ ] Hiện thực `WosRegisterMap` & `WosRegisterAddress`: Parser schema JSON và strip offset địa chỉ thanh ghi.
- [ ] Hiện thực `IWosModbusClient` & `WosModbusClient` (bọc `ModbusTcpClient`).

### Phase 3 — Worker Thu Thập & Pipeline Xử Lý (Background Processing)
- [ ] Tạo project `Services.Wos` (ref `Services.Shared.Runtime`, `Services.Wos.CR1000X`, `Module.Wos.Core`, `Modules.TMS.Core`).
- [ ] Xây dựng `WosStationCatalogManager`: Quản lý & cache danh sách trạm từ DB trong RAM.
- [ ] Xây dựng `WosStationAcquisitionService` & `WosAcquisitionRunner`: Vòng lặp định kỳ đọc Modbus TCP → giải mã qua Codec → áp scale/offset → phát lệnh xử lý qua Wolverine.
- [ ] Xây dựng `WosWeatherRepository`: Lưu bản ghi thời tiết vào bảng `TmsWeather` bằng SqlSugar ORM (idempotent theo `StationId` + `TimeDetect`).

### Phase 4 — Cấu Hình Host & Vận Hành (Worker Host)
- [ ] Tạo project `Services.Wos.Worker`: `Program.cs`, cấu hình `Database.json` (kết nối CSDL chung với `TAC_WebAPI`), `Logging.json`.
- [ ] Xây dựng giao diện WPF giám sát cục bộ `Services.Wos.Wpf` (Start/Stop worker, xem bảng trạng thái đọc trạm gần nhất).

### Phase 5 — Bộ Giả Lập Kiểm Thử (Simulation & Test)
- [ ] Hiện thực `FakeWosModbusClient`: Giả lập dữ liệu nhiệt độ, độ ẩm, gió, mưa mà không cần kết nối phần cứng thật.
- [ ] Viết test suite toàn diện tại `tests/BE/ITS/Wos/`:
  - Unit test `Cr1000XCodec`: Round-trip encode/decode float big-endian.
  - Unit test `WosRegisterMap`: Parse schema JSON, kiểm tra fallback khi JSON lỗi.
  - Integration test `WosStationAcquisitionService`: Đọc giả lập qua Fake Client, kiểm tra ghi nhận vào DB test local `127.0.0.1`.

### Phase 6 — Tích Hợp Giao Diện Frontend (WebVue)
- [ ] Cập nhật component `weatherInfo.vue` và `wosInfo.vue`: Kết nối WebAPI thật thay cho hàm random mock.

---

## 6. Chiến Lược Kiểm Thử An Toàn (Testing & Verification)

- **Nguyên tắc CSDL**: Tuyệt đối tuân thủ quy tắc kết nối CSDL Local `127.0.0.1` khi chạy test (mục 19.18 & Rule 11).
- **Chế độ Giả lập**: Bật cấu hình `Service:Wos:Simulated=true` để chạy `FakeWosModbusClient` khi phát triển và kiểm thử tự động.
- **Biên dịch & Test Commands**:
  - `dotnet build TA-ITS015-WEBAPI-V1.0/src/TAC_WebAPI/TAC_WebAPI.csproj`
  - `dotnet test tests/BE/test.csproj --filter "FullyQualifiedName~Wos"` (không dùng cờ `--no-build`).

---
*Tài liệu tham chiếu gốc:*  
- Cẩm nang sản phẩm Campbell CR1000X (334 trang): [`../doc/cr1000x-product-manual/00-catalog.md`](../doc/cr1000x-product-manual/00-catalog.md)  
- Thông số kỹ thuật CR1000X: [`../doc/cr1000x-specifications.md`](../doc/cr1000x-specifications.md)  
- Hồ sơ nghiệm thu vật tư WOS: [`../doc/ho-so-nghiem-thu-wos.md`](../doc/ho-so-nghiem-thu-wos.md)  
- Hồ sơ bản vẽ thi công BVTKTC: [`../doc/bvtktc-wos.md`](../doc/bvtktc-wos.md)  

