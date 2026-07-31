# Tóm tắt Khởi chạy và Giải thích Chi tiết Module Dự án WP_Sync

## 1. Overview & Mục Tiêu (Overview)

Dự án **WP_Sync** (`/Users/hoangquydat/ThienAn/WP_Sync`) là hệ thống đồng bộ dữ liệu 2 chiều/1 chiều giữa các CSDL của giải pháp quản lý giao thông thông minh (ITS) và hệ thống TMS. 

Hệ thống được thiết kế theo chuẩn **Clean Architecture** và mô hình **Modular**, phân tách rõ ràng trách nhiệm giữa lớp Nhân (Core), Lớp Nghiệp vụ (Application), Lớp Hạ tầng (Infrastructure) và các Lớp Host khởi chạy (Worker Service & WPF Desktop App).

---

## 2. Hướng Dẫn Tóm Tắt Quy Trình Chạy Dự Án (Execution Guide)

### 2.1 Môi trường yêu cầu (Prerequisites)
- **.NET SDK**: phiên bản 10.0 (`net10.0` / `net10.0-windows`).
- **Database**: SQL Server (Source DB `DEV_ITS10` & Target DB `DEV_ITS015_WP`).
- **Event Broker**: NATS Server (nếu sử dụng cơ chế đồng bộ theo sự kiện Realtime).

### 2.2 Các bước Khởi chạy

#### Chạy dưới dạng Worker Service (Chạy ngầm / Cross-platform):
1. **Kiểm tra cấu hình CSDL**: Đảm bảo chuỗi kết nối trong `src/ITS.Sync.Worker/Configuration/Database.json` hoặc `appsettings.json` hợp lệ.
2. **Kiểm tra file NATS Credentials**: Đảm bảo file `src/ITS.Sync.Worker/Configuration/camera.creds` đã tồn tại.
3. **Thực thi lệnh trong Terminal**:
   ```bash
   cd /Users/hoangquydat/ThienAn/WP_Sync/src/ITS.Sync.Worker
   dotnet run
   ```

#### Chạy dưới dạng Ứng dụng Desktop WPF (Giao diện Giám sát):
1. Mở giải pháp trên môi trường **Windows** (bằng Visual Studio hoặc Rider).
2. Đặt `ITS.Sync.Wpf` làm **Startup Project**.
3. Nhấn `F5` hoặc chạy lệnh:
   ```cmd
   dotnet run --project src/ITS.Sync.Wpf/ITS.Sync.Wpf.csproj
   ```

---

## 3. Giải Thích Chi Tiết Từng Module Con (Sub-Modules Deep Dive)

```
WP_Sync/src/
├── ITS.Sync.Core/
├── ITS.Sync.Application/
├── ITS.Sync.Infrastructure/
├── ITS.Sync.Worker/
└── ITS.Sync.Wpf/
```

### 3.1 `ITS.Sync.Core` - Lớp Nhân & Hợp Đồng (Domain Core)
Lớp độc lập nhất trong hệ thống, không tham chiếu đến thư viện bên ngoài hay các dự án khác.
- **`Abstractions/` (Giao diện hợp đồng)**:
  - `ISyncController.cs`: Định nghĩa API khởi chạy, dừng, kiểm tra trạng thái và trigger đồng bộ toàn bộ hệ thống.
  - `ISyncManager.cs`: Định nghĩa API lập kế hoạch, đăng ký các chiến lược đồng bộ cho từng bảng.
  - `ITableSync.cs`: Interface chuẩn mà mỗi class đồng bộ bảng (`SysConfigDataSync`, `TmsEquipmentSync`...) bắt buộc phải kế thừa.
- **`Models/` (Mô hình dữ liệu)**:
  - `SyncChange.cs`: Chứa thông tin về biến động dữ liệu (ID, thao tác Insert/Update/Delete).
  - `SyncInitResult.cs` & `SyncResult.cs`: Chứa kết quả thực thi đồng bộ (số dòng thành công, số dòng lỗi, thời gian chạy).
  - `SyncRunReport.cs`: Báo cáo tổng hợp sau mỗi lượt đồng bộ.
- **`Enums/`**:
  - `SyncStatus.cs`: Trạng thái đồng bộ (`Idle`, `Running`, `Success`, `Failed`).
  - `MissingRowAction.cs`: Hành vi khi phát hiện dữ liệu thiếu ở CSDL Đích (`Insert`, `Ignore`, `Delete`).

---

### 3.2 `ITS.Sync.Application` - Lớp Nghiệp Vụ & Điều Phối (Application Layer)
Nơi chứa toàn bộ logic kinh doanh và chiến lược đồng bộ từng bảng dữ liệu.
- **`Sync/Orchestration/` (Điều phối tổng thể)**:
  - `SyncController.cs`: Lớp triển khai `ISyncController`, quản lý vòng đời tiến trình đồng bộ, điều phối qua `SyncManager`.
  - `SyncManager.cs`: Quản lý danh sách các chiến lược đồng bộ bảng (`ITableSync`), khởi tạo context kết nối.
  - `SyncPlan.cs`: Sắp xếp thứ tự ưu tiên đồng bộ giữa các bảng để tránh lỗi khóa ngoại (Foreign Key).
  - `SyncSteps.cs`: Thực thi các bước chi tiết: Tải dữ liệu Nguồn/Đích ➡️ So sánh Khóa/Hash ➡️ Thực thi Cập nhật/Thêm mới trong Transaction.
- **`Sync/Tables/` (Chiến lược đồng bộ cho 13 bảng dữ liệu)**:
  - **Nhóm Cấu hình System**:
    - `SysConfigData/`, `SysConfigType/`, `SysOpConfig/`: Đồng bộ danh mục và thông số vận hành hệ thống.
  - **Nhóm Vận hành TMS (Traffic Management)**:
    - `TmsEquipment/` & `TmsEquipmentType/`: Đồng bộ danh sách thiết bị giao thông (camera, biển báo VMS, cảm biến) và loại thiết bị.
    - `TmsEventType/` & `TmsIncident/`: Đồng bộ loại sự kiện và các sự cố giao thông realtime.
    - `TmsMap/` & `TmsMapDetail/`: Đồng bộ dữ liệu bản đồ giao thông và thông tin chi tiết các lớp bản đồ.
    - `TmsWorkUnit/`: Đồng bộ đơn vị quản lý vận hành.
    - `TmsZone/`, `TmsZoneEquipment/`, `TmsZoneStatus/`: Đồng bộ phân vùng giao thông, gán thiết bị theo vùng và trạng thái hoạt động của vùng.

---

### 3.3 `ITS.Sync.Infrastructure` - Lớp Hạ Tầng (Infrastructure Layer)
Chịu trách nhiệm kết nối CSDL và Message Broker bên ngoài.
- **`Persistence/` (Tương tác CSDL - SqlSugar ORM)**:
  - `SqlSugarFactory.cs`: Tạo và quản lý client SqlSugar kết nối đồng thời 2 CSDL: `Source` (`DEV_ITS10`) và `Target` (`DEV_ITS015_WP`).
  - `SyncDbAccessor.cs`: Cung cấp truy cập nhanh đến các kết nối CSDL Nguồn/Đích.
  - `BaseRepository.cs`: Repository cơ sở cho thao tác CRUD.
  - `DbConnectionOptions.cs` & `DbConstants.cs`: Lưu cấu hình và hằng số kết nối.
- **`Messaging/` (Tương tác Event Broker - NATS JetStream)**:
  - `NatsSyncListener.cs`: Lắng nghe PubSub / Stream từ NATS broker. Khi có tin nhắn báo biến động dữ liệu ở Nguồn (VD: subject `ta.its.data.incident`), listener sẽ lập tức trigger tiến trình đồng bộ realtime cho bảng `TmsIncident`.
  - `NatsSyncOptions.cs`: Cấu hình URL NATS, JWT Authentication (`camera.creds`), danh sách Subscriptions.

---

### 3.4 `ITS.Sync.Worker` - Dịch Vụ Chạy Ngầm (Worker Host)
Dự án Host thực thi dưới dạng Console Service / Windows Service:
- `Program.cs`: Cấu hình Dependency Injection (`AddApplication`, `AddInfrastructure`), đọc cấu hình `appsettings.json`.
- `SyncBackgroundService.cs`: Background Service kế thừa `BackgroundService` của .NET. Chạy vòng lặp định kỳ theo `IntervalSeconds` để tự động kích hoạt đồng bộ, đồng thời khởi chạy `NatsSyncListener` để chờ sự kiện realtime.

---

### 3.5 `ITS.Sync.Wpf` - Ứng Dụng Desktop (WPF Presentation Host)
Dự án Host giao diện người dùng trên Windows:
- **`ViewModels/`**:
  - `MainViewModel.cs`: ViewModel chính, liên kết dữ liệu giao diện với `SyncController`, cung cấp các lệnh (Commands) bấm nút **Start Sync**, **Stop Sync**, hiển thị danh sách bảng và phần trăm tiến độ.
  - `StrategyItemViewModel.cs`: Quản lý trạng thái bật/tắt đồng bộ cho từng bảng dữ liệu riêng lẻ.
- **`Views/`**:
  - `MainWindow.xaml`: XAML UI hiển thị dashboard quản trị tiến trình đồng bộ.

---

## 4. Kế Hoạch Xác Minh & Kiểm Thử (Verification Plan)

### Automated Checks / CLI Commands
```bash
# 1. Kiểm tra build thành công toàn bộ giải pháp
dotnet build /Users/hoangquydat/ThienAn/WP_Sync/src/ITS.Sync.Worker/ITS.Sync.Worker.csproj

# 2. Kiểm tra khởi chạy thử Worker Service
dotnet run --project /Users/hoangquydat/ThienAn/WP_Sync/src/ITS.Sync.Worker/ITS.Sync.Worker.csproj
```

### Manual Verification Checklist
- [ ] Kiểm tra file `camera.creds` có trong thư mục `Configuration/`.
- [ ] Xác nhận kết nối thành công tới SQL Server `10.10.8.30`.
- [ ] Kiểm tra log hiển thị tiến trình đồng bộ bảng `TmsEquipment` trong Worker console output.
