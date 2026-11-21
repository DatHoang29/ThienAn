# Toll API Test Tool — Tài liệu kiến trúc & bàn giao

> Công cụ WPF để **test các API thu phí (ETC)**: gọi API GET (cấu hình động), xem dữ liệu trả về,
> ghi nhật ký (form + file), tự động gọi theo chu kỳ, và **lưu masterdata xuống DB ITS** (tùy chọn).
> Tài liệu này đủ để tiếp tục phát triển ở session khác.

Cập nhật: 24/08/2026

---

## 1. Mục tiêu & phạm vi hiện tại

| Hạng mục | Trạng thái |
|---|---|
| Form test gọi API GET (giống VDS), cấu hình động 4 API | ✅ Xong |
| Truyền `FromDate`/`ToDate` cho API cần khoảng ngày | ✅ Xong |
| Hiển thị JSON trả về (format đẹp) + status + số bản ghi | ✅ Xong |
| Nhật ký trên form + ghi file log theo ngày | ✅ Xong |
| Lưu cấu hình ra file JSON (`Toll.json`) + nút "Lưu cấu hình" | ✅ Xong |
| Tự động gọi tất cả API theo chu kỳ (giây) | ✅ Xong |
| Checkbox "Lưu vào DB sau khi gọi" | ✅ Xong |
| Lưu **masterdata** → DB ITS (TollMasterData/TollRoad/TollPriceList/TollPriceListDetail) | ✅ Xong (TollPriceList bổ sung 06/10/2026) |
| Lưu **TransactionIn** → TollTransactionIn, **TransactionOut** → TollTransactionOut (khóa transaction_ID) | ✅ Xong (cần đối chiếu sample In thật) |
| Lưu **OutSummary** → TollTicketOutSummary (khóa id) | ✅ Xong |
| Lưu **AccessLog** → TollAccessHistory (result.items, khóa EmployeeCode+AccessTime+Status) | ✅ Xong |
| Worker chạy nền định kỳ 24/7 | ⏳ Chưa làm (mới có WPF) |

---

## 2. Vị trí & cấu trúc project

```
src/Services/Toll/
├── ITS.Toll.Core/                      # Class lib (net10.0) — lõi, không UI
│   ├── Configuration/
│   │   ├── TollOptions.cs              # Cấu hình gốc (BaseUrl, Timeout, DateFormat, AutoCallIntervalSeconds, Endpoints[]) + CreateDefault()
│   │   ├── TollApiEndpoint.cs          # 1 API: Key/Name/Url/RequiresDateRange/DateFromParam/DateToParam/DataType/Enabled
│   │   └── TollConfigStore.cs          # Load()/Save() cấu hình ra FILE JSON (System.Text.Json)
│   ├── Models/
│   │   ├── TollApiCallResult.cs        # Kết quả 1 lần gọi API (status, duration, content, recordCount, error)
│   │   └── TollSaveResult.cs           # record (bool Ok, string Message) — kết quả lưu DB
│   ├── Persistence/
│   │   ├── TollDbOptions.cs            # ConnectionString/DbType/TenantId (bind từ section "Database")
│   │   └── TollDb.cs                   # Bọc SqlSugarScope (1 kết nối "Its") + TestConnectionAsync
│   └── Services/
│       ├── TollApiClient.cs            # Gọi HTTP GET, dựng URL + query ngày, format JSON, đếm bản ghi
│       └── TollDataStore.cs            # Parse masterdata JSON → upsert vào DB (Insert nếu mới / Update nếu có)
│
├── ITS.Toll.Wpf/                       # WPF app (net10.0-windows, WinExe)
│   ├── App.xaml / App.xaml.cs          # Composition root: đọc appsettings, dựng DI, mở form
│   ├── MainWindow.xaml / .cs           # Giao diện + DataContext = MainViewModel
│   ├── ViewModels/
│   │   ├── MainViewModel.cs            # Toàn bộ logic UI (gọi API, auto-call, lưu cấu hình, lưu DB, log)
│   │   ├── EndpointItemViewModel.cs    # 1 dòng lưới API (editable + kết quả lần gọi gần nhất)
│   │   └── LogItem.cs                  # 1 dòng nhật ký (Timestamp/Level/Message)
│   ├── Logging/FileLogger.cs           # Ghi file log toll-yyyy-MM-dd.log, tự dọn file cũ
│   ├── appsettings.json                # Trỏ ConfigFilePath + section Database + Logging
│   └── AssemblyInfo.cs
│
└── README.md                           # (file này)
```

File cấu hình động (nguồn chuẩn): **`src/TAC_WebAPI/Configuration/Toll.json`**
→ được **link** vào `ITS.Toll.Wpf` (copy ra `Configuration/Toll.json` ở thư mục chạy). App đọc & ghi file này.

Đã thêm 2 project vào solution `TAC_WebAPI.sln` (bằng `dotnet sln add`).

---

## 3. Sơ đồ phụ thuộc

```
ITS.Toll.Wpf  ──►  ITS.Toll.Core  ──►  Module.TOLL.Core (entity Toll*)
     │                   │                    └► Shared.Core.dll (EntityTenant), Shared.DTO (EntityConst)
     │                   └► SqlSugarCore, Shared.Core.dll, Shared.Infrastructure.dll
     └► CommunityToolkit.Mvvm, Microsoft.Extensions.{Configuration.Json,Configuration.Binder,DependencyInjection}
```

- **Core không phụ thuộc UI**; WPF chỉ nối dây + trạng thái màn hình.
- Tái sử dụng **entity gốc** trong `Module.TOLL.Core` (không khai lại bảng).

---

## 4. Luồng hoạt động

### 4.1. Khởi động (App.xaml.cs)
1. Đọc `appsettings.json` (BasePath = thư mục chạy exe).
2. Lấy `Toll:ConfigFilePath` → `ResolveConfigPath` (tuyệt đối dùng luôn; tương đối tính từ exe; mặc định `Configuration/Toll.json`).
3. `TollConfigStore.Load()` đọc `Toll.json` → `TollOptions` (thiếu file thì tạo mặc định bằng `CreateDefault()`).
4. Bind section `Database` → `TollDbOptions`.
5. Đăng ký DI: `TollConfigStore`, `TollOptions`, `FileLogger`, `TollDbOptions`, `TollDb`, `HttpClient`, `TollApiClient`, `TollDataStore`, `MainViewModel`, `MainWindow` → mở form.

### 4.2. Gọi API (MainViewModel)
- **"Gọi API đang chọn"** → `CallSelectedApiAsync` gọi `SelectedEndpoint`.
- **"Gọi tất cả"** → `CallAllApisAsync` lặp các endpoint `Enabled = true`.
- **"Bật/Tắt tự động"** → `ToggleAutoCall`: bật `DispatcherTimer` theo `AutoCallIntervalSeconds`, mỗi nhịp gọi tất cả (bỏ qua nếu đang bận).
- Lõi gọi: `CallEndpointAsync` → dựng `TollOptions` từ form (`BuildOptions`) → `TollApiClient.CallAsync`:
  - `BuildUrl`: URL tuyệt đối dùng luôn / tương đối ghép `BaseUrl`; nếu `RequiresDateRange` thì thêm `FromDate`/`ToDate` (định dạng theo `DateFormat`).
  - GET có timeout; đọc content; format JSON đẹp; đếm bản ghi (mảng gốc hoặc `data/result/items`).
- Cập nhật panel phản hồi + dòng lưới (status/ms/bản ghi/lúc) + nhật ký.
- Nếu **checkbox "Lưu vào DB sau khi gọi"** bật và gọi thành công → `TollDataStore.SaveAsync(result)`.

### 4.3. Cấu hình theo form (không phụ thuộc file)
- **Nguồn dữ liệu khi gọi = form**: mọi lần gọi (`CallEndpointAsync`) đều dựng `TollOptions` từ trạng thái hiện tại của form qua `BuildOptions()` (BaseUrl/Timeout/DateFormat + danh sách `Endpoints`). KHÔNG cần lưu file trước khi gọi.
- **Nhập liệu trực tiếp**: nút **"Thêm API"** (`AddEndpointCommand`) thêm dòng trống; **"Xóa API đang chọn"** (`RemoveEndpointCommand`) xóa dòng đang chọn. Sửa Tên/Url/khoảng ngày/DataType ngay trên lưới (đã `UpdateSourceTrigger=PropertyChanged`).
- File `Toll.json` chỉ là **seed lúc mở** + nơi lưu tùy chọn.
- **"Lưu cấu hình"** → `SaveConfig` → `BuildOptions()` → `TollConfigStore.Save()` ghi đè `Toll.json` (chỉ khi muốn giữ cho lần sau).
- Lưới dùng `DataGridTemplateColumn` + `UpdateSourceTrigger=PropertyChanged` (checkbox Bật/Cần khoảng ngày, textbox Tên/Url/DataType) để **commit ngay khi sửa** (tránh lỗi 2-click của `DataGridCheckBoxColumn`).

### 4.4. Lưu DB — masterdata (TollDataStore.SaveAsync)
- `switch (DataType)`: `"Master"` → `ImportMasterAsync`; còn lại → trả "chưa hỗ trợ".
- `ImportMasterAsync`: parse `result` (tự bóc envelope `result`), mở transaction, upsert theo khóa (Insert mới / Update cũ), commit; lỗi → rollback + trả message.
- **Stamp bản ghi mới**: `ID = Guid.NewGuid().ToString()` (GUID ngẫu nhiên v4 — cùng quy ước CCTV/TMS/VDS), `TenantId = options.TenantId` ("Default"), `CreateTime = now`.

---

## 5. Ánh xạ masterdata → bảng ITS

Response `result` (xem `docs/reponesAPI.txt`) gồm các mảng:

| Mảng JSON | Bảng đích | Khóa upsert | Ghi chú |
|---|---|---|---|
| `vehicleTypes` | `TollMasterData` (DataType=`VehicleType`) | DataType+SourceId | id→SourceId, name→Name, group→GroupCode, note→Remark, RawJson=item |
| `ticketTypes` | `TollMasterData` (`TicketType`) | " | note→Remark (JSON có `price` nhưng bảng không có cột) |
| `priceTypes` | `TollMasterData` (`PriceType`) | " | note→Remark |
| `reasons` | `TollMasterData` (`Reason`) | " | description→Remark |
| `payments` | `TollMasterData` (`Payment`) | " | description→Remark |
| `statuses` | `TollMasterData` (`Status`) | " | note→Remark |
| `transactionStatuses` | `TollMasterData` (`TransactionStatus`) | " | note→Remark |
| `tcsRoads` | `TollRoad` | SourceId | station_ID_In/Out, km→Km(string), note→Remark, active→Active |
| `tcsPriceListDetails` | `TollPriceListDetail` | SourceId | code/name/price/vehicleType_ID/ticketType_ID/priceType_ID/priceList_ID/road_ID/active/note |
| `tcsPriceLists` (header) | `TollPriceList` | SourceId | id→SourceId, name/startDate/endDate/active/note, create_Date→SourceCreateDate, write_Date→SourceWriteDate. Upsert **trước** detail |

Quyết định đã chốt với khách:
1. Bảng đã tồn tại (không tạo bảng). 2. Được tham chiếu `Module.TOLL.Core`. 3. ~~Bỏ qua `tcsPriceLists`~~ → từ 06/10/2026 đồng bộ vào `TollPriceList` (tạo bảng bằng `Scripts/20261006_TollPriceList.sql`).
4. Dùng `TollPriceListDetail` (không dùng class cũ `PriceListDetail`). 5. `GroupCode = group` (type nằm trong RawJson).
6. Không quản lý `Active` cho masterdata (chỉ hiển thị, không sửa dữ liệu TCS) → để null.
7. Insert nếu mới / Update nếu có (không xóa). 8. ID = GUID tự động + giữ SourceId (ID bên TCS).
9. Chỉ làm masterdata trước. 10. 1 checkbox chung "Lưu vào DB sau khi gọi".

### 5.1. Ánh xạ giao dịch (transaction) → bảng ITS

Sample: `docs/reponesAPITransaction.txt` (`result` là **mảng** giao dịch). Khóa upsert: `transaction_ID`.

| Trường JSON | TollTransactionIn | TollTransactionOut |
|---|---|---|
| transaction_ID | TransactionId (khóa) | TransactionId (khóa) |
| station_ID / lane_ID | StationId / LaneId | StationId / LaneId |
| station_ID_In / lane_ID_In | — | StationIdIn / LaneIdIn |
| transaction_Datetime | TransactionDateTime | TransactionDateTime |
| transaction_Datetime_In | TransactionDateTimeIn | TransactionDateTimeIn |
| price | — (In không có cột giá) | Price |
| vehicleType_ID / ticketType_ID | VehicleTypeId / TicketTypeId | VehicleTypeId / TicketTypeId |
| reason_ID / ticket_ID / tagID | ReasonId / TicketId / TagId | ReasonId / TicketId / TagId |
| plate / plate_LPR / plate_Edit | Plate / PlateLpr / PlateEdit | Plate / PlateLpr / PlateEdit |
| status_ID / status_Checked | StatusId / StatusChecked | StatusId / StatusChecked |
| imageLane / imagePlate | ImageLane / ImagePlate | ImageLane / ImagePlate |
| imageLane_In / imagePlate_In | — | ImageLaneIn / ImagePlateIn |
| user_ID / shift_ID / proccess_Date / serial | UserId / ShiftCode / ProcessDate / Serial | UserId / ShiftCode / ProcessDate / Serial |
| (thêm khi ghép SysUser) | OperatorName | OperatorName |

Trường JSON **bị bỏ** (entity chưa có cột): `plateType_ID`, `priceType_ID`, `payment_ID`, `f0`, `note`, `dataJson`, `create_Date/Uid`, `write_Date/Uid`, `invoice_Code`, `parent_ID`, `incident_ID`.

> Lưu ý: `ImportTransactionInAsync` đang giả định endpoint In dùng cùng tên trường như Out — đối chiếu lại khi có sample In thật.

### 5.2. Ánh xạ tổng hợp vé ra (OutSummary) → TollTicketOutSummary

Sample: `docs/reponesAPISummary.json` (`result` là **mảng**). Khóa upsert: `id`.

| Trường JSON | TollTicketOutSummary |
|---|---|
| id | SourceId (khóa) |
| processDate / startDate / endDate | ProcessDate / StartTime / EndTime |
| station_ID / station_ID_In | StationId / EntryStationId |
| direction | Direction (chuỗi "1"/"2") |
| vehicleType_ID / ticketType_ID / priceType_ID / priceList_ID | VehicleTypeId / TicketTypeId / PriceTypeId / PriceListId |
| price | Price |
| amount_Total / total | AmountTotal / TotalAmount |
| amount_Edit / total_Edit | AmountEdit / TotalEdit |
| reason_ID / ticket_Confirmed_ID | ReasonId / ConfirmedId |

Không có trong sample: `Amount_Sum`/`Total_Sum` (để null). Bị bỏ: `note`, `dataJson`, `create/write_*` (entity chưa có cột).

### 5.3. Ánh xạ lịch sử truy cập nhân viên (AccessLog) → TollAccessHistory

API: `GET appaccesslog/accesshistorytcs?FromDate=...&ToDate=...` (đuôi tương đối theo BaseUrl `.../tcs23quanly`; cần khoảng ngày).
Điểm khác biệt: mảng nằm ở **`result.items`** (object bọc mảng), không phải `result` là mảng trực tiếp
→ dùng helper riêng `TryGetResultItemsArray`. TCS **không trả ID** nên khóa upsert là bộ nghiệp vụ
`EmployeeCode + AccessTime + Status` (dựng qua `BuildAccessLogKey`). Danh sách sắp theo `accessTime` giảm dần.

| Trường JSON | TollAccessHistory |
|---|---|
| employeeCode | EmployeeCode (thành phần khóa) |
| employeeName | EmployeeName |
| accessTime | AccessTime (thành phần khóa) |
| status | Status (thành phần khóa) |
| (ITS gán) | LastSyncTime |

Quy tắc: Insert nếu mới / Update `LastSyncTime` nếu đã có. Bản ghi rỗng hoàn toàn (thiếu cả 3 trường khóa) sẽ bị bỏ qua.
`result.items = []` → thành công, 0 bản ghi.

---

## 6. Cấu hình

> **Nguồn cấu hình runtime là bảng `SysOpConfig` (nhóm `toll_config`)** — form đọc qua `TollOpConfigReader.Load()`
> (xem `App.xaml.cs`). File `Toll.json` chỉ còn là **mẫu/seed tham chiếu**, KHÔNG phải nguồn form đang chạy.

### Quy ước BaseUrl + đuôi endpoint
`BaseUrl = http://10.10.9.223:9089/api/tcs23quanly` (đã gồm segment module ETC). Endpoint dưới cùng module này
chỉ cần **đuôi tương đối** (vd access log = `appaccesslog/accesshistorytcs`). Endpoint ở module khác (vd ca làm việc
thuộc `acs13quanly`) để **URL tuyệt đối** vì không cùng nhánh với BaseUrl. `TollApiClient.BuildUrl`: URL tuyệt đối
dùng luôn, URL tương đối ghép `BaseUrl + "/" + Url`.

| Cấu hình | Giá trị |
|---|---|
| `base_url` | `http://10.10.9.223:9089/api/tcs23quanly` |
| `api_master` | `mdsmasterdatatcs/masterdata` |
| `api_station` | `mdstram/allstation` |
| `api_lane` | `mdslane/alllane` |
| `api_shift` | `http://10.10.9.223:9089/api/acs13quanly/mdsca/list` (tuyệt đối — khác module) |
| `api_transaction_in` | `mdsmasterdatatcs/alltransactionin` |
| `api_transaction_out` | `mdsmasterdatatcs/alltransactionout` |
| `api_out_summary` | `mdsmasterdatatcs/alloutsummaryticket` |
| `api_access_log` | `appaccesslog/accesshistorytcs` |

### `src/TAC_WebAPI/Configuration/Toll.json` (mẫu/seed — cùng quy ước trên)
```json
{
  "BaseUrl": "http://10.10.9.223:9089/api/tcs23quanly",
  "TimeoutSeconds": 30,
  "DateFormat": "yyyy-MM-dd HH:mm:ss",
  "AutoCallIntervalSeconds": 60,
  "Endpoints": [
    { "Key": "master", "Name": "Danh mục (Master Data)", "Url": "mdsmasterdatatcs/masterdata", "RequiresDateRange": false, "DataType": "Master", "Enabled": true },
    { "Key": "shift", "Name": "Danh mục ca làm việc", "Url": "http://10.10.9.223:9089/api/acs13quanly/mdsca/list", "RequiresDateRange": false, "DataType": "Shift", "Enabled": true },
    { "Key": "accessLog", "Name": "API lịch sử truy cập", "Url": "appaccesslog/accesshistorytcs", "RequiresDateRange": true, "DataType": "AccessLog", "Enabled": true }
  ]
}
```
> Lưu ý: chỉ URL `alloutsummaryticket` là chắc; các URL còn lại cần chỉnh cho khớp API ETC thật.
> URL masterdata thực tế đang test là `mdsmasterdatatcs/allmasterdatatcs` (chỉnh trên màn hình "Cấu hình hoạt động").

### `ITS.Toll.Wpf/appsettings.json`
- `Toll:ConfigFilePath` = `Configuration/Toll.json`
- `Database:ConnectionString` = DB ITS chứa bảng Toll* (mặc định trỏ `DEV_ITS10` — **sửa theo môi trường**), `DbType`, `TenantId`.
- `Logging:FileDirectory` / `RetainDays`.

---

## 7. Build & chạy

```bash
# Từ thư mục src
dotnet build Services/Toll/ITS.Toll.Wpf/ITS.Toll.Wpf.csproj
# Chạy (Windows)
dotnet run --project Services/Toll/ITS.Toll.Wpf
```
Yêu cầu: .NET 10 SDK, Windows (WPF).

---

## 8. Việc tiếp theo (TODO cho session sau)

1. **Đối chiếu sample TransactionIn thật**: hiện `ImportTransactionInAsync` giả định endpoint `alltransactionin`
   trả cùng tên trường như `alltransactionout` (sample tại `docs/reponesAPITransaction.txt`). Khi có sample In
   riêng, kiểm tra lại tên trường trong `FillIn` (`TollDataStore.cs`).
2. (Đã xong) Lưu OutSummary → TollTicketOutSummary. Nếu API bổ sung `Amount_Sum`/`Total_Sum`, map thêm trong `FillSummary`.
2. **Ghi TollSyncLog** cho mỗi lần lưu (DataType/ApiEndpoint/From-ToDate/Received/Inserted/Updated/Failed/Status) — bảng đã có sẵn.
3. **Worker chạy nền 24/7** (nếu cần): tách phần gọi + lưu sang `BackgroundService` theo mẫu WP_Sync
   (`ITS.Sync.Worker`), dùng chung `TollApiClient` + `TollDataStore`.
4. (Tùy chọn) Chuyển ID sang **sequential GUID** nếu muốn giống các bảng cũ và tối ưu clustered index.
5. (Tùy chọn) Xử lý phân trang/cursor nếu API transaction trả nhiều trang (entity `TollSyncLog` đã có `CursorFrom/CursorTo`).

---

## 9. Tham chiếu

- Mẫu kiến trúc service định kỳ: `src/Services/WP_Sync` (Worker + WPF dùng chung controller).
- Mẫu form test: `src/Services/VDS/ITS.VDS.Wpf`.
- Entity Toll: `src/Modules/TOLL/Module.TOLL.Core/Entities/`.
- Response mẫu masterdata: `docs/reponesAPI.txt`.
