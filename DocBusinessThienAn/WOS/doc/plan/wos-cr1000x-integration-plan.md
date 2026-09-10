# Tích hợp WOS (CR1000X) — nhiệt độ, độ ẩm, lượng mưa, hướng gió

## Context

Đã đối chiếu bộ tài liệu `DocBusinessThienAn/HữuNghị-ChiLăng/WOS/doc/` với mục tiêu "Nhiệt độ, độ ẩm, lượng mưa, hướng gió":

- **Phần cứng/thiết kế: đạt.** WOS gồm 3 cảm biến (RM Young/Campbell `05103` tốc độ+hướng gió, Campbell `TB4` vũ lượng kế, Campbell `HygroVUE5` nhiệt độ+độ ẩm qua SDI-12) nối vào datalogger **Campbell CR1000X**, đã nghiệm thu vật tư "Đạt" 3/3 bộ mỗi loại.
- **Phần mềm backend TA-ITS015: chưa có.** Repo hiện không có driver/entity/worker nào đọc dữ liệu từ CR1000X. Chỉ có bảng `TmsWeather` + `WeatherService` gọi OpenWeatherMap (hard-code toạ độ TP.HCM, chưa từng chạy), với một nhánh code rỗng `if (!string.IsNullOrEmpty(equiApi)) { }` rõ ràng để dành cho thiết bị hiện trường (WOS) nhưng chưa cài gì. Gói tin ShareData/C2C 104 "Dữ liệu thời tiết" mới có spec + test, chưa có service publish thật.

Plan này xây phần tích hợp phần mềm còn thiếu: một service độc lập đọc dữ liệu Modbus TCP từ CR1000X, ghi vào `TmsWeather` (mở rộng thêm cột), theo đúng pattern device-driver đã có trong repo (`Services/Sample`, `Services/VDS`). Việc xây `DataPublicationService` cho gói 104 KHÔNG nằm trong scope này (chỉ để lại 1 TODO comment).

## Quyết định kỹ thuật (đã chốt, có lý do)

| # | Quyết định | Chọn | Vì sao |
|---|---|---|---|
| 1 | Thư viện Modbus TCP client | **FluentModbus** — **đã kiểm tra kỹ, có hỗ trợ C#/.NET đầy đủ** (xem mục xác minh bên dưới) | Có sẵn `ReadHoldingRegisters<float>()` + `ModbusEndianness.BigEndian` khớp thẳng với layout big-endian 32-bit float của CR1000X, khỏi tự ghép byte thủ công như NModbus. |
| 2 | Register map | JSON lưu trong cột `WosStation.RegisterMapJson`, schema ở dưới | CRBasic gán register khác nhau theo từng trạm — cấu hình theo dữ liệu, sửa qua CRUD API, không cần đụng file trên máy chủ worker. |
| 3 | `CanonicalDeviceMessage` (single-scalar hiện tại) | Thêm 1 property `Channel` (nullable, additive) — gọi `Decode` 6 lần/lần poll (1 lần/kênh) | Không phá vỡ contract `IDeviceCodec` hiện có (`DeviceACodec`/`DeviceBCodec` không đổi gì); tái dùng đúng pattern "acquire → decode từng item → aggregate" đã có ở `DeviceAggregationService`. |
| 4 | Quy đổi đơn vị/scale (vd 0.254mm/tip) | Làm ở tầng aggregation service, **không** làm trong codec, lấy scale/offset từ register-map JSON | Giữ `IDeviceCodec` thuần protocol (byte↔canonical); xử lý được mâu thuẫn 0.254mm vs 0.2mm bằng cấu hình thay vì hard-code. |
| 5 | Chu kỳ polling mặc định | 60s (`Service:Wos:PollIntervalSeconds`), có cột `PollIntervalSeconds` per-station để dành (chưa enforce ở v1) | Khớp chu kỳ bảng output 1 phút của CR1000X, tránh đọc trùng khi bảng ring-buffer chưa cập nhật. |
| 6 | Nguồn dữ liệu giả lập khi dev không có phần cứng thật | `FakeWosModbusClient : IWosModbusClient` | Test được toàn bộ pipeline mà không cần CR1000X thật, theo đúng mẫu `FakeEventSource` của VDS. |
| 7 | Typo `Hudmidity` trong `TmsWeather` | **Giữ nguyên**, không rename | Spec mapping ShareData gói 104 đã tham chiếu đúng tên cột này; đổi tên sẽ phá hợp đồng đã tài liệu hoá. Có thể thêm property alias `Humidity` chỉ đọc, không đổi schema. |
| 8 | Nối vào nhánh rỗng `equiApi` của `WeatherService` hay tách worker riêng | **Tách `Services.Wos.Worker` riêng**, tự ghi DB qua SqlSugar | `WeatherService` là hosted service chết (chưa từng được `AddHostedService`); mọi tích hợp thiết bị khác trong repo (`Services/Sample`, `Services/VDS`) đều là process Worker độc lập, không phải in-process hosted service. |

### Xác minh: FluentModbus có hỗ trợ C# không?

**Có — đã kiểm tra trực tiếp qua NuGet + GitHub (10/09/2026), không chỉ suy đoán:**

| Tiêu chí | Kết quả kiểm tra |
|---|---|
| Ngôn ngữ / nền tảng | Thư viện **C#/.NET Standard 2.0 và 2.1** thuần — tương thích .NET Framework 4.6.1+, .NET Core 2.0+, và cả .NET 10 mà `Services.Wos.*` dự định dùng (project netstandard2.0 tham chiếu bình thường từ project net10.0). |
| Phiên bản mới nhất | `5.3.2` trên NuGet ([nuget.org/packages/FluentModbus](https://www.nuget.org/packages/FluentModbus)). |
| Duy trì tích cực | Repo GitHub `Apollo3zehn/FluentModbus`: lần push gần nhất **19/05/2026**, cập nhật gần nhất **01/09/2026** (tuần trước), chưa archive, 307 sao, 64 issue đang mở — vẫn đang được bảo trì. |
| License | **MIT** — dùng tự do cho phần mềm nội bộ/thương mại, không ràng buộc gì thêm. |
| API cần dùng | `new ModbusTcpClient().Connect(ip, port, ModbusEndianness.BigEndian)` rồi `client.ReadHoldingRegisters<float>(unitId, startAddress, count)` — đúng như thiết kế register-map JSON ở dưới, xác nhận qua tài liệu chính thức [apollo3zehn.github.io/FluentModbus](https://apollo3zehn.github.io/FluentModbus/) và mẫu code [Modbus TCP sample](https://apollo3zehn.github.io/FluentModbus/samples/modbus_tcp.html). |
| ⚠️ Lưu ý khi implement | API của `ModbusTcpClient` là **đồng bộ (sync)**, không có `ReadHoldingRegistersAsync` sẵn — mẫu chính thức của thư viện cũng bọc lệnh gọi trong `Task.Run(...)` khi cần async. Vậy `WosModbusClient.ReadChannelsAsync(...)` (Phase 2) phải tự bọc `Task.Run(() => client.ReadHoldingRegisters<float>(...))`, không giả định có sẵn phương thức async — không đổi thiết kế interface `IWosModbusClient`, chỉ đổi cách implement bên trong. |

**Phát hiện thêm cần đưa vào schema**: theo bài viết chính thức của Campbell Scientific ["How to Access Live Measurement Data Using Modbus"](https://www.campbellsci.com/blog/access-live-measurement-data-using-modbus), thứ tự byte của float (`ABCD`/`BADC`/`CDAB`/`DCBA`) là **tham số cấu hình trong `ModbusServer()` của chương trình CRBasic từng trạm**, không cố định phần cứng — cùng loại rủi ro "khác nhau theo từng trạm" như địa chỉ register. Vì vậy bổ sung field `byteOrder` vào register-map JSON thay vì hard-code `ModbusEndianness.BigEndian`.

### Schema register-map JSON (trên `WosStation.RegisterMapJson`)

```json
{
  "unitId": 1,
  "tableIntervalSeconds": 60,
  "accumulationMode": "delta",
  "byteOrder": "ABCD",
  "channels": [
    { "code": "TEMP", "parameter": "temperature",  "campbellRegister": 30001, "unit": "C",   "scale": 1.0,   "offset": 0.0 },
    { "code": "RH",   "parameter": "humidity",      "campbellRegister": 30003, "unit": "%",   "scale": 1.0,   "offset": 0.0 },
    { "code": "RAIN", "parameter": "rainfall",       "campbellRegister": 30005, "unit": "mm",  "scale": 0.254, "offset": 0.0, "note": "TB4 tip resolution — xác nhận lại theo chương trình CRBasic thực tế của từng trạm" },
    { "code": "WSPD", "parameter": "windSpeed",      "campbellRegister": 30007, "unit": "m/s", "scale": 1.0,   "offset": 0.0 },
    { "code": "WDIR", "parameter": "windDirection",  "campbellRegister": 30009, "unit": "deg", "scale": 1.0,   "offset": 0.0 },
    { "code": "VBAT", "parameter": "batteryVoltage", "campbellRegister": 30011, "unit": "V",   "scale": 1.0,   "offset": 0.0 }
  ]
}
```
`campbellRegister` lưu số Campbell công bố (1-based, offset 30000/40000); có helper strip offset → địa chỉ Modbus 0-based cho FluentModbus. `byteOrder` mặc định `"ABCD"` (= `ModbusEndianness.BigEndian` chuẩn), nhưng phải xác nhận lại theo `ModbusOption` thực tế trong chương trình CRBasic của từng trạm trước khi đưa vào vận hành — không giả định giống nhau cho mọi trạm.

## Các file sẽ tạo/sửa (theo pha)

Đường dẫn gốc: `C:\ThienAn\TA-ITS015-WEBAPI-V1.0\src\`

### Phase 1 — Entity + config (registry, chưa chạy)
- **Sửa** `Modules\TMS\Modules.TMS.Core\Entities\TmsWeather.cs`: thêm `StationId` (FK), `BatteryVoltage` (float?), `Source` (string?, "wos"|"openweathermap"); giữ `Hudmidity`.
- **Mới** `Modules\Wos\Module.Wos.Core\Entities\WosStation.cs`: registry trạm (`Code`, `Name`, `KmNumber`, `ModbusHost`, `ModbusPort`, `ModbusUnitId`, `PollIntervalSeconds`, `RegisterMapJson`, `Enabled`, `LastPolledAt`, `LastStatus`) — theo mẫu [VwController.cs](TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Entities/VwController.cs).
- **Mới** `Modules\Wos\Module.Wos.Core\Module.Wos.Core.csproj` — theo mẫu `Modules.TMS.Core.csproj`.
- **Mới** `Modules\Wos\Module.Wos\Controllers\Station\WosStationController.cs` + `Commands\WosStationCommandHandler.cs` + `Queries\WosStationQueryHandler.cs` + `Dto\WosStationInput.cs`/`Output.cs` + `Validators\WosStationValidator.cs` — CRUD chuẩn, theo mẫu [TmsWeatherController.cs](TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/TmsWeatherController.cs) và handlers cùng thư mục.
- **Mới** `Modules\Wos\Module.Wos\Extensions\{ServiceCollectionExtensions,ModuleExtensions,MvcBuilderExtensions}.cs` + `Infrastructure\BaseRepository.cs` — theo mẫu `Module.ShareData`.
- **Sửa** `TAC_WebAPI\Startup.cs` (`RegisterModules()`) — thêm `.AddWosModule(_config)` vào chain.
- **Mới** `SQL\Scripts\WOS\001_create_wos_station.sql`, `002_alter_tmsweather_add_wos_columns.sql` — DDL, theo convention `.sql` thủ công của repo (không auto-execute).

### Phase 2 — Modbus client + codec CR1000X (tầng giao thức)
- **Mới** `Services\Wos\Services.Wos.CR1000X\Services.Wos.CR1000X.csproj` — netstandard2.0, ref `Services.Shared` + gói `FluentModbus`.
- **Mới** `Cr1000XCodec.cs` (`IDeviceCodec`): decode/encode float big-endian 4-byte ↔ `CanonicalDeviceMessage` (có `Channel`) — theo mẫu [DeviceACodec.cs](TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample.DeviceA/DeviceACodec.cs).
- **Mới** `WosChannelDefinition.cs`, `WosRegisterMap.cs` — parse schema JSON ở trên.
- **Mới** `WosRegisterAddress.cs` — strip offset 30000/40000 → địa chỉ wire 0-based.
- **Mới** `IWosModbusClient.cs` / `WosModbusClient.cs` (FluentModbus thật) / (Phase 5) `FakeWosModbusClient.cs`.

### Phase 3 — Worker thu thập + pipeline xử lý
- **Mới** `Services\Wos\Services.Wos\Services.Wos.csproj` — ref `Services.Shared.Runtime`, `Services.Wos.CR1000X`, `Modules.TMS.Core`, `Module.Wos.Core`.
- **Mới** `Configuration\*` (options/loader/reload) — theo mẫu `Services.Sample\Configuration\*`.
- **Mới** `Features\Catalog\WosStationCatalogManager.cs` + `WosStationRepository.cs` — cache trạm trong RAM từ DB, theo mẫu [CameraManager.cs](TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Catalog/CameraManager.cs).
- **Mới** `Features\Devices\WosStationReading.cs`, `WosStationAcquisitionService.cs` (1 lần đọc Modbus/trạm → 6 `DeviceFrame` → decode → áp scale/offset), `WosAcquisitionRunner.cs` — theo mẫu [DeviceAcquisitionRunner.cs](TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Features/Devices/DeviceAcquisitionRunner.cs) (start/stop loop, publish `ProcessWosReadingCommand` qua Wolverine local queue).
- **Mới** `Features\DataFlow\Messages\ProcessWosReadingCommand.cs` + Handler, `Processing\WosDataProcessor.cs`, `Persistence\WosWeatherRepository.cs` (ghi vào `TmsWeather` có sẵn, idempotent theo `StationId+TimeDetect`).
- **Mới** `Extensions\{ServiceCollectionExtensions,HostProfiles}.cs`, `Manager\WosServiceManager.cs` (implement `IServiceManager`).

### Phase 4 — Host + API
- **Mới** `Services\Wos\Services.Wos.Worker\Program.cs` + `appsettings.json` + `Configuration\{Database,DataTransporter,Logging}.json` — **`Database.json` phải trỏ cùng DB với `TAC_WebAPI`** (ghi chung bảng `TmsWeather`/`WosStation`), không copy nguyên config demo-DB của Sample.
- **Mới** `Services\Wos\Services.Wos.Wpf\*` — UI vận hành Start/Stop + bảng last-reading/trạm, theo mẫu `Services.Sample.Wpf`.
- **Sửa** `TmsWeatherInput.cs`/`TmsWeatherQueryHandler.cs` — thêm filter `StationId` optional.

### Phase 5 — Nguồn giả lập cho dev/test
- `FakeWosModbusClient.cs`, toggle `Service:Wos:Simulated` trong DI, nút "Simulate" trên WPF (theo mẫu `SampleServiceManager.Simulation.cs`).

### Phase 6 (tuỳ chọn) — Frontend
- **Sửa** `TA-ITS015-WEBVUE-V1.0/src/src/views/bigScreen/trafficInfo/component/weatherInfo.vue` — bỏ comment khối UI đã sẵn API call.
- **Sửa** `.../component/unit/wosInfo.vue` — thay data giả `Math.random()` bằng gọi API thật lọc theo `StationId`.

### Ngoài scope
- `DataPublicationService` cho gói ShareData 104: chưa tồn tại trong source (chỉ có test spec). Chỉ để lại `// TODO: publish to ShareData packet 104 once DataPublicationService exists` trong `WosDataProcessor`, không dựng interface/class nào cho nó.

## Vấn đề nhất quán repo đã phát hiện (không sửa trong plan này, chỉ ghi nhận)
1. File `Modules.TMS\Core\Entities\TmsWeather.cs` là bản trùng, đã bị loại khỏi build (`<Compile Remove="Core\**" />` trong `.csproj`) — nên có ticket dọn riêng.
2. `WeatherService.cs` là dead code (chưa từng `AddHostedService`), nhánh `equiApi` rỗng — plan này cố tình không nối vào đó.
3. `TmsWeather.Type` hiện dùng cho "loại thời tiết" (clear/rain/cloud...) từ OpenWeatherMap — không tái dùng cho nguồn dữ liệu (wos/openweathermap), dùng cột `Source` mới thay vì overload `Type`.
4. `Services.Wos` → `Modules.TMS.Core` là lần đầu tiên một project `Services/*` reference vào `Modules/*` trong repo — an toàn (Modules.TMS.Core không có dependency ASP.NET) nhưng cần reviewer chú ý.
5. Ngữ nghĩa tích luỹ mưa (delta vs cumulative tip count) và mâu thuẫn độ phân giải 0.254mm/0.2mm là câu hỏi cấu hình phần cứng chưa chốt — đã đưa vào field bắt buộc `accumulationMode`/`scale` trong register-map thay vì tự chọn ngầm.

## Kiểm thử

**Không cần phần cứng thật:**
1. Bật `Service:Wos:Simulated=true` → `FakeWosModbusClient` sinh dữ liệu giả → chạy `Services.Wos.Worker` (hoặc nút Simulate trên WPF) → xác nhận có row mới trong `TmsWeather` với `Source="wos"`, `StationId` có giá trị, đủ 6 field, và `WosStation.LastPolledAt`/`LastStatus` được cập nhật.
2. Unit test (không DB, không network): `Cr1000XCodec.Decode/Encode` round-trip với chuỗi byte big-endian đã biết trước; `WosRegisterAddress` strip offset; `WosRegisterMap.Parse` với schema ở trên (kể cả case JSON hỏng).
3. Unit test `WosStationAcquisitionService` với fake `IWosModbusClient` (dictionary in-memory) để verify build `DeviceFrame` + áp scale/offset đúng, không cần Modbus I/O thật.

**`dotnet build`** phải pass cho: `Services.Wos.CR1000X`, `Services.Wos`, `Services.Wos.Worker`, `Services.Wos.Wpf`, `Module.Wos.Core`, `Module.Wos`, `Modules.TMS.Core` (đã sửa), `TAC_WebAPI` (thêm `.AddWosModule`), và `tests\test.csproj` (cả hai TargetFramework `net10.0`/`net10.0-windows`).

**`dotnet test`** — tuân thủ nghiêm ngặt an toàn của repo: mọi test project/connection string chạm DB thật (vd test tích hợp `WosWeatherRepository`) chỉ được trỏ `localhost`/`127.0.0.1`/`(localdb)`/`.`; **không bao giờ IP remote** (vd `10.10.8.30`) — nếu phát hiện phải huỷ test ngay và báo user, theo mục "Strict Local Database" trong `CLAUDE.md`. Ưu tiên giữ test `Cr1000XCodec`/`WosRegisterMap`/`WosStationAcquisitionService` thuần unit (dùng fake, không DB); test repository-level phải gate theo đúng pattern `Exists()`-guard + `#if HAS_WOS` đã có sẵn trong `tests\test.csproj` (giống cách `ShareDataWorker`/`Module.VideoWall` được include có điều kiện hiện nay).

## File then chốt để bắt đầu implement
- [CanonicalDeviceMessage.cs](TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared/Devices/CanonicalDeviceMessage.cs) — cần thêm property `Channel` additive.
- [DeviceAcquisitionRunner.cs](TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Features/Devices/DeviceAcquisitionRunner.cs) — khuôn mẫu trực tiếp cho `WosAcquisitionRunner`.
- [TmsWeather.cs](TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWeather.cs) — entity thật (không phải bản trùng) cần mở rộng.
- [VwController.cs](TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Entities/VwController.cs) — mẫu cho entity `WosStation`.
- `Services\VDS\ITS.VDS.Core\Features\Catalog\CameraManager.cs` — mẫu cho `WosStationCatalogManager`.
- `TAC_WebAPI\Startup.cs` (`RegisterModules()`) — cần thêm `.AddWosModule(_config)`.
