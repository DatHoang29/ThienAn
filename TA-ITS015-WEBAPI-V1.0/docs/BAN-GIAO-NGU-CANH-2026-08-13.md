# Bàn giao ngữ cảnh — 13/08/2026

Tài liệu tóm tắt để tiếp tục công việc ở phiên làm việc mới.

## 1. Hai thư mục đang làm việc

| Thư mục | Vai trò |
|---|---|
| `C:\Users\Admin\Desktop\WORK\Code\TACorp\TA-ITS015-WEBAPI-V1.0` | API chính, chứa phân hệ báo cáo TOLL |
| `C:\Users\Admin\Desktop\WORK\Code\TACorp\TA-ITS015-WebAPI-ShareData-V1.0` | ShareDataWorker — service kết xuất dữ liệu chia sẻ |

---

## 2. ĐÃ HOÀN THÀNH — Hiển thị tên trạm trên header báo cáo

### Vấn đề gốc

Store `ITS015_Proc_ReportRevenue_Monthly` chỉ dùng `@station` để lọc (`WHERE`), không trả về tên trạm. Trong khi đó `DTTTQueryHandler` lại gán thẳng `command.Tram` vào cả hai vai: vừa là khóa lọc xuống store, vừa là text hiển thị trên header báo cáo. Kết quả: header in ra mã trạm thay vì tên trạm.

### Hướng đã chọn

FE gửi `StationId` (value của dropdown), BE tra ngược ra `Name` từ bảng `TollStation` để hiển thị. Không đổi FE, không sửa store.

### File mới

`src/Modules/TOLL/Module.TOLL.Report/Core/Helpers/StationHelper.cs`

Extension method `ISqlSugarClient.GetTenTram(string? stationId)`:
- `null`/rỗng → trả `"Tất cả các trạm"`
- không tìm thấy trong `TollStation` → fallback trả về chính mã trạm

### 14 handler đã sửa

Thay `Tram = command.Tram` thành `Tram = _baseRepo.Context.GetTenTram(station)`, thêm `using Module.TOLL.Report.Core.Helpers;`.

Nhóm Doanh thu (`Controllers/DoanhThu/`):
`DTTNQueryHandler`, `DTTTQueryHandler`, `DTTQQueryHandler`, `DTTNamQueryHandler`, `DTTTGQueryHandler`

Nhóm Lưu lượng (`Controllers/LuuLuong/`):
`LLTCDVQueryHandler`, `LLTCDRQueryHandler`, `LLTNDauVaoQueryHandler`, `LLTNDauRaQueryHandler`, `LLTTDVQueryHandler`, `LLTTDRQueryHandler`, `LLTNamDVQueryHandler`, `LLTNamDRQueryHandler`, `LLTLTQueryHandler`

### Hai việc phát sinh cần lưu ý

1. `DTTTGQueryHandler` dùng tham số `@Station` viết hoa chữ S, khác với 13 file còn lại dùng `@station`. Dễ lọt khi grep.

2. `LLTTParameter` và `LLTNamParameter` **chưa có** property `Tram` — 4 báo cáo LLTT/LLTNam trước giờ không in tên trạm. Đã thêm `public string? Tram` vào hai DTO:
   - `src/Modules/TOLL/Module.TOLL.Core/Dto/LuuLuong/LuuLuongTheoThang/LLTTOutput.cs`
   - `src/Modules/TOLL/Module.TOLL.Core/Dto/LuuLuong/LuuLuongTheoNam/LLTNamOutput.cs`

   **Muốn hiển thị trên báo cáo thì phải bổ sung field vào file `.frx` tương ứng.** Chưa làm. Nếu không muốn đổi 4 báo cáo này thì revert lại được.

### Chưa kiểm chứng

**Chưa chạy `dotnet build`.** Cần build để xác nhận phần `using`/namespace không vướng.

---

## 3. ĐANG TRIỂN KHAI — ShareDataWorker trên server

### Đường dẫn deploy

`E:\IIS_WebPool\ITS\ITS015\Services\TA-ShareData-Service` trên máy `10.10.8.30` (SRV-ITS)

Service đã tạo bằng `New-Service` với tên `ShareDataWorker`, khớp `options.ServiceName` trong `Program.cs`.

### Ba điều quan trọng về hành vi runtime

1. **File xuất ghi vào chính thư mục deploy.** `ShareDataWorkerService.cs:330` dùng `AppContext.BaseDirectory`, tạo cấu trúc:
   ```
   <thư mục deploy>\Out\{PartnerCode}\{dd_MM_yyyy}\{DatatypeId}\{HH_mm_ss}.json
   ```
   Trên máy dev khi chạy F5 thì đây chính là `bin\Debug\net10.0\`.

2. **Thư mục `Out` chỉ sinh ra khi có dữ liệu thật.** Nếu query trả 0 bản ghi, code `continue` trước khi tạo thư mục. **Không có `Out` không có nghĩa là service lỗi.**

3. **Bản Release không có log nghiệp vụ nào.** `LogInformationMsg` (dòng 742-746) bọc trong `#if DEBUG`. Project chỉ tham chiếu `Microsoft.Extensions.Logging.Console`, không có Serilog/NLog. `UseWindowsService()` tự thêm EventLog nên chỉ có log start/stop/crash vào Windows Event Viewer.

### Cách kiểm tra đúng thứ tự

Xem bảng `ShareDataActivityLog` **trước**, thư mục `Out` sau. Bảng log luôn có bản ghi mỗi lần worker xử lý một subscription, dù thành công, rỗng hay lỗi.

```sql
SELECT TOP 20 OccurredAt, Status, RecordCount, ByteSize, FilePath, ErrorMessage, Description
FROM ShareDataActivityLog ORDER BY OccurredAt DESC;

SELECT ID, Code, State, PartnerId, DatatypeId, NextTimeRun, LastTimeRun
FROM ShareDataSubscription;
```

Worker chỉ chạy khi `NextTimeRun <= GETDATE()` và `State` Active.

### Vấn đề tồn đọng

- Đã có lúc **2 tiến trình `ShareDataWorker` chạy song song** (console thủ công + service). Cần xác nhận đã kill con thừa và kiểm tra `ShareDataActivityLog` xem có bản ghi trùng không.
- **Code chưa có cơ chế chống chạy song song.** Chưa có mutex toàn cục hay lock ở tầng DB khi lấy subscription. Nên bổ sung.
- Config `NasStorage:BasePath` trong `appsettings.json` **không được code đọc ở đâu cả** — đường dẫn xuất file đang hardcode. Nếu ý định ban đầu là ghi ra NAS thì phần này chưa nối.

---

## 4. ĐANG THIẾT KẾ — Gửi email báo cáo tự động

### Kiến trúc đã chốt

**Hangfire làm đồng hồ, bảng DB là nguồn sự thật.** Một `RecurringJob` duy nhất (`scan-report-email-schedule`), đăng ký trong `Configure()` của `Startup.cs` cạnh `sync-cctv-devices`, chạy mỗi 5 phút, quét bảng lịch.

Lý do không dùng "mỗi lịch một RecurringJob": tránh **hai nguồn sự thật**. Doc `Modules/TMS/docs/THIET-KE-KICH-BAN-XU-LY-SU-CO-TU-DONG.md` mục 5.5 ghi lại chính sai lầm này — team từng phải lưu `ExecuteJobId` và gọi `BackgroundJob.Delete` chỉ để giữ Hangfire khớp với `Status`.

### Hiện trạng Hangfire trong solution

- Package đã có: `Hangfire.AspNetCore` + `Hangfire.SqlServer` 1.8.23 trong `Shared.Reference.csproj`
- `Startup.cs:131` đăng ký có điều kiện theo `Hangfire:Enable`
- `Startup.cs:200` `UseHangfireDashboard("/jobs")` — mặc định `LocalRequestsOnlyAuthorizationFilter`, chỉ truy cập được từ chính máy chủ
- `Permissions.Hangfire.View` đã khai báo nhưng **chưa nối vào đâu**
- **Đang tắt**: `Configuration/Hangfire.json` có `"Enable": false` và connection string là rác cũ (`.\SQLEXPRESS / PROD_ITS / sa / 123`)
- **Nhưng bảng `HangFire.*` đã tồn tại trong `DEV_ITS10`** — nghĩa là đã từng chạy thật với connection string khác cái trong file

### Ba việc phải làm trước khi bật

1. Sửa `Hangfire.json` trỏ đúng `10.10.8.30 / DEV_ITS10` để khớp schema đã tồn tại. Đừng tạo bộ bảng thứ hai ở DB khác.
2. Kiểm tra tàn dư job cũ:
   ```sql
   USE DEV_ITS10;
   SELECT * FROM HangFire.[Set] WHERE [Key] = 'recurring-jobs';
   SELECT * FROM HangFire.Server;
   SELECT TOP 50 * FROM HangFire.Job ORDER BY CreatedAt DESC;
   ```
3. Bật `Enable: true` là **kích hoạt cả `sync-cctv-devices`** (`Startup.cs:209`, đọc cron từ DB qua `DeviceService.GetSyncDeviceAsync()`, fallback 5 phút). Xác nhận với team CCTV.

### Tiền lệ trong code — và lỗi đừng chép lại

`Modules.VMS/.../TimerCommandHandler.cs` đang dùng Hangfire thật. Nhưng dòng 155 và 162:

```csharp
var jobStartName = $"StartScheduleVms";   // interpolation nhưng không có biến
```

Tên job cố định, không gắn với thiết bị. Toàn hệ thống chỉ có đúng **một** lịch timer — nhóm thiết bị sau đè mất lịch nhóm trước.

Ngoài ra hai kiểu đăng ký job đang tồn tại song song:
- `sync-cctv-devices` đăng ký trong `Configure()` → tự đăng ký lại mỗi lần khởi động, xóa DB Hangfire vẫn hồi phục
- VMS timer đăng ký khi user gọi API → xóa DB Hangfire là lịch biến mất vĩnh viễn

Với gửi email báo cáo, **bắt buộc dùng kiểu thứ nhất**.

### Điểm móc để lấy file đính kèm

Render báo cáo nằm trong DLL đóng `src/DLLs/Modules.Report.Base.dll`, gọi qua Wolverine bằng message `ExportReportInput`, nhận về `FileStreamResult`. **Chỉ cần gọi message đó rồi đọc stream ra `byte[]`** — không đụng FastReport.

Lưu ý: `ReportDataId` trong luồng hiện tại là do FE sinh ra để đọc data đã cache. Luồng nền không có nó, phải đi thẳng đường `QueryData`.

### Hai bảng cần tạo

`RptEmailSchedule`: `ReportCode`, kiểu kỳ báo cáo (ngày/tháng/quý/năm), tham số cố định (`StationId`, `Direction`), `Format`, `CronExpression`, `NextTimeRun`, `LastTimeRun`, `State`, `Recipients`

`RptEmailLog`: mô phỏng `ShareDataActivityLog` — `ScheduleId`, `OccurredAt`, `Status`, `Recipients`, `FileName`, `ByteSize`, `ErrorMessage`

**Không lưu `Thang`/`Nam` cứng.** Lưu quy ước tương đối ("kỳ trước liền kề"), lúc chạy mới suy ra. Đây là chỗ dễ sai nhất của toàn tính năng: lịch chạy ngày 01/09 phải gửi báo cáo tháng 08.

### Thiết kế giao diện

Ba nguyên tắc đã thống nhất:

1. **Không cho người dùng gõ cron.** Cho chọn tần suất → ngày → giờ, backend tự dựng biểu thức cron.

2. **Phải có dòng xác nhận động về kỳ báo cáo**, ví dụ: "Chạy lúc 08:00 ngày 01/09/2026 sẽ gửi báo cáo của tháng 08/2026". Đây là phần quan trọng nhất màn hình — người dùng đặt lịch ngày 1 tháng 9 rất dễ tưởng sẽ nhận báo cáo tháng 9.

3. **Nút "Gửi thử ngay" là bắt buộc.** Không có nó thì mỗi lần sửa cấu hình phải chờ tới kỳ mới biết đúng sai — với báo cáo tháng là chờ 30 ngày.

Màn hình danh sách: cột "Lần cuối" hiển thị luôn trạng thái thất bại, vì lỗi gửi mail là loại lỗi âm thầm.

Form: 4 nhóm — Báo cáo và tham số / Lịch chạy / Người nhận và nội dung / Trạng thái. Tiêu đề email hỗ trợ biến `{Thang} {Nam} {Quy} {Ngay} {TenTram} {TenBaoCao}`.

### Thứ tự triển khai

1. Sửa `Hangfire.json`, bật `Enable`, kiểm tra job cũ
2. Viết `IReportMailSender` độc lập, test bằng mail rỗng — chưa ghép gì
3. Hai bảng + CRUD API + màn hình
4. Job quét + xử lý cho riêng DTTT (Doanh thu theo tháng) làm mẫu
5. Nhân bản mapping cho 13 báo cáo còn lại

Bước 2 và 3 làm song song được.

### ĐANG BỊ CHẶN

**Chưa có thông số máy chủ SMTP.** Scan toàn bộ solution không tìm thấy cấu hình mail nào. Hằng số duy nhất là `ConfigConst.SysErrorMail = "sys_error_mail"` — đã kiểm tra, **không được tham chiếu ở bất kỳ đâu**, là hằng số chết.

Cần hỏi bên hạ tầng: host, port, SSL/TLS, tài khoản gửi, giới hạn dung lượng đính kèm.

Chưa quyết thư viện: MailKit (khuyến nghị) hay `System.Net.Mail.SmtpClient` (đã obsolete cho code mới).

---

## 5. Cấu hình kết nối MCP tới CSDL

File `mcp.json` ở thư mục gốc đã được sửa trỏ đúng:
- `its10` → `10.10.8.30 / DEV_ITS10`
- `its10_log` → `10.10.8.30 / DEV_ITS10_Log`

**Cần đổi tên thành `.mcp.json`** (có dấu chấm đầu) rồi mở phiên mới:

```powershell
Rename-Item mcp.json .mcp.json
```

Đã thêm `.mcp.json` và `mcp.json` vào `.gitignore` — file chứa mật khẩu `sa` dạng plaintext.

**Nên chuyển sang login chỉ đọc thay vì `sa`:**

```sql
USE master;
CREATE LOGIN mcp_readonly WITH PASSWORD = '<đặt mật khẩu mạnh>';
GO
USE DEV_ITS10;
CREATE USER mcp_readonly FOR LOGIN mcp_readonly;
ALTER ROLE db_datareader ADD MEMBER mcp_readonly;
GO
USE DEV_ITS10_Log;
CREATE USER mcp_readonly FOR LOGIN mcp_readonly;
ALTER ROLE db_datareader ADD MEMBER mcp_readonly;
GO
```

Nếu sau khi đổi tên mà công cụ `mcp__its10__*` không xuất hiện, nghĩa là Cowork không đọc `.mcp.json` cấp thư mục — khi đó phải đóng gói thành plugin như plugin `thienan-mssql` của anh Đạt (nằm trong `.remote-plugins`, hiện trỏ `localhost,14333` là container local không chạy).

---

## 6. Thông tin hạ tầng

| Mục | Giá trị |
|---|---|
| SQL Server | `10.10.8.30` |
| DB nghiệp vụ | `DEV_ITS10` |
| DB log | `DEV_ITS10_Log` |
| Server ứng dụng | `10.10.8.30` (SRV-ITS), truy cập qua RealVNC |
| Thư mục service | `E:\IIS_WebPool\ITS\ITS015\Services\` |
| Carbone server | `http://192.168.1.30:4000` |
| Template báo cáo | `Report/Template` |

---

## 7. Việc cần làm tiếp — theo thứ tự ưu tiên

1. Chạy `dotnet build` xác nhận 16 file đã sửa ở mục 2 không lỗi biên dịch
2. Quyết định về `Tram` trong 4 báo cáo LLTT/LLTNam: bổ sung field vào `.frx` hay revert
3. Xác nhận chỉ còn 1 tiến trình `ShareDataWorker`, kiểm tra log trùng
4. Lấy thông số SMTP từ bên hạ tầng
5. Bắt đầu bước 1 và 3 của luồng gửi email
