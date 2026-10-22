# VideoWall (BE) — Decoupling Thiết Bị Equipment, Phân Quyền Multi-User & Mở Rộng Đa Cụm Màn Hình

**Tệp prompt:** `DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/Prompt/videowall-decoupling-equipment-va-phan-quyen-multi-cluster-be-prompt.md`  
**Phân hệ:** VideoWall (`TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall`, `ShareDataWorker/ITS.VideoWall`)  
**Ngày lập:** 22/10/2026  
**Căn cứ kỹ thuật:** Biên bản cuộc họp kỹ thuật toàn diện ngày 22/10/2026 giữa Anh Sơn (Tech Lead) và Đạt (Dev) từ tệp ghi âm gốc `videowall.m4a` (~45:10).  
**Trạng thái:** ⏳ Chờ thực thi (Chỉ chạy khi có yêu cầu).

---

## 1. Bối Cảnh & Vấn Đề Kỹ Thuật (Từ Cuộc Họp 22/10/2026)

Trong cuộc họp review kỹ thuật ngày 22/10/2026, Anh Sơn và Đạt đã phân tích các lỗ hổng kiến trúc và điểm nghẽn backend của VideoWall:

1. **Trùng lặp cấu hình kết nối thiết bị (Duplication of Hardware Config):**
   - Thực trạng: Bảng `VwController` đang lưu trực tiếp `IpAddress`, `Port`, `Username`, `Password`.
   - Bất cập: Phân hệ **Equipment (`Module.Equipment`)** đã là Single Source of Truth (SSOT) quản lý toàn bộ thiết bị phần cứng mạng, camera, máy chủ, controller. Việc VideoWall tự lưu riêng IP/User/Pass dẫn đến tình trạng khi quản trị viên đổi mật khẩu hoặc đổi dải IP trên phân hệ Thiết bị, phân hệ VideoWall bị lệch cấu hình, mất kết nối thiết bị và phát sinh lỗi đồng bộ.
   - **Chỉ đạo của Anh Sơn:** VideoWall liên kết với thiết bị thông qua khóa ngoại (`EquipmentId` trỏ tới bảng thiết bị). Tầng Worker nạp thông tin kết nối trực tiếp từ Equipment. Bỏ việc bắt buộc nhập tay IP/Mật khẩu dư thừa trên VideoWall, chỉ giữ lại trường dự phòng tương thích ngược.

2. **Cơ chế phân quyền đơn lẻ từng người dùng (Single-User Bottleneck):**
   - Thực trạng: Entity `VwWallPermission` chỉ lưu đơn lẻ 1 `UserId` hoặc 1 `OrgId` trên mỗi bản ghi.
   - Bất cập: Khi một trung tâm điều hành có 10, 20 hay 100 nhân sự vận hành cần được cấp chung một quyền điều khiển ma trận màn hình, người quản trị phải tạo thủ công hàng chục đến hàng trăm bản ghi. Việc bảo trì và cập nhật quyền ma trận khi có thay đổi nhân sự trở nên bất khả thi.
   - **Chỉ đạo của Anh Sơn:** Nâng cấp API phân quyền hỗ trợ gán quyền hàng loạt cho danh sách nhiều người dùng (`UserIds` dạng mảng) hoặc theo Tổ chức (`OrgId`). Lưu toàn bộ cấu hình ma trận ô cho nhóm người dùng trong 1 transaction duy nhất.

3. **Giả định cứng hệ thống chỉ có một bức tường màn hình (Single-Wall Constraint):**
   - Thực trạng: Toàn bộ bảng `VwScreen`, `VwWallTopology` và `VwWallPermission` đều giả định toàn hệ thống chỉ có duy nhất 1 bức tường màn hình 8x4 (32 màn hình).
   - Bất cập: Thực tế trung tâm điều hành bao gồm nhiều cụm màn hình:
     - Cụm 1: Bức tường chính (8x4, 32 màn hình LCD).
     - Cụm 2: Cụm màn hình phụ bên cạnh (2x2) hiển thị SCADA / Giám sát chuyên dụng.
     - Cụm mở rộng tương lai (Giai đoạn 2, Giai đoạn 3).
   - **Chỉ đạo của Anh Sơn:** Bổ sung trường định danh cụm màn hình `ClusterId` (hoặc `WallId`) vào thực thể màn hình và bố cục để phân tách độc lập các cụm hiển thị.

4. **Bản chất trường `Model` của Controller:**
   - Trường `Model` trong `VwController` là thông tin định danh chủng loại/phiên bản thiết bị (do người dùng khai báo hoặc đọc về từ thiết bị).
   - Tầng Worker Service đọc trường `Model` này để kích hoạt đúng Adapter/Driver ISAPI tương ứng (ví dụ: Hikvision v1 hay v2, DS-C66S hay DS-C30S), không hardcode một nhánh xử lý duy nhất.

5. **API kiểm tra trạng thái Nguồn tín hiệu đang sử dụng (Signal In-Use Status):**
   - Bổ sung trường tính toán `IsUsed` / `ActiveWindowCount` khi truy vấn danh sách `VwSource` để cung cấp cho Frontend rào lại (disable/gray out), ngăn chặn việc người dùng kéo trùng một nguồn tín hiệu vào nhiều cửa sổ gây xung đột phần cứng.

---

## 2. Phạm Vi Tệp Mã Nguồn Cần Can Thiệp

### 2.1. Tầng CSDL (Script SQL Migration Idempotent)
- `DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/sql/20261022-decoupling-equipment-multi-user-cluster.sql`

### 2.2. Backend WebAPI (`TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall`)
- **Entities:**
  - `Modules/VideoWall/Entities/VwController.cs`: Thêm `EquipmentId` (nullable `string?`), giữ nguyên `IpAddress`, `Port`, `Username`, `Password` để tương thích ngược.
  - `Modules/VideoWall/Entities/VwScreen.cs`: Thêm `ClusterId` (nullable `string?`, default `'main'`).
  - `Modules/VideoWall/Entities/VwWallTopology.cs`: Thêm `ClusterId`.
  - `Modules/VideoWall/Entities/VwWallPermission.cs`: Tối ưu hóa cấu trúc gán quyền.
- **DTOs & Inputs:**
  - `Modules/VideoWall/Dto/VwControllerInput.cs`: Thêm `EquipmentId`, cho phép `IpAddress`, `Username`, `Password` nullable khi đã chọn `EquipmentId`.
  - `Modules/VideoWall/Dto/VwWallPermissionBatchInput.cs`: DTO nhận mảng `UserIds: string[]` kèm `Config` ô lưới ma trận.
  - `Modules/VideoWall/Dto/VwSourceOutput.cs`: Thêm `IsUsed: bool` và `ActiveWindowCount: int`.
- **Validators:**
  - `Modules/VideoWall/Validators/VwControllerValidator.cs`: Kiểm tra nếu `EquipmentId` rỗng thì mới bắt buộc nhập IP/Port/Credential; nếu đã có `EquipmentId` thì cho phép rỗng thông tin kết nối mạng.
- **Command / Query Handlers:**
  - `Modules/VideoWall/Handlers/VwWallPermission/BatchAssignPermissionCommandHandler.cs`: Xử lý lưu quyền hàng loạt trong SqlSugar transaction.
  - `Modules/VideoWall/Handlers/VwSource/GetSourceListQueryHandler.cs`: Tính toán trạng thái nguồn đang phát trên các cửa sổ kịch bản active.
  - `Modules/VideoWall/Services/VwPermissionService.cs`: Hỗ trợ nạp quyền theo `ClusterId` và kiểm tra quyền theo nhóm người dùng.
- **Controllers:**
  - `Modules/VideoWall/Controllers/VwWallPermissionController.cs`: Bổ sung endpoint `POST /api/VwWallPermission/BatchAssign`.
  - `Modules/VideoWall/Controllers/VwSourceController.cs`: Trả về trường `IsUsed`.

### 2.3. VideoWall Worker Service (`ShareDataWorker/ITS.VideoWall`)
- `ITS.VideoWall/Services/VwISAPIDeviceService.cs` hoặc `DeviceSetup.cs`: Khi khởi tạo kết nối tới Controller, nếu `EquipmentId` có giá trị $\rightarrow$ truy vấn thông tin IP/Port/Auth từ Equipment (hoặc fallback thông tin trên `VwController` nếu Equipment chưa có).
- `ITS.VideoWall/Services/VwDeviceDriverResolver.cs`: Phân giải Driver/ISAPI API version dựa trên `Model` của Controller.

---

## 3. Đặc Tả Chi Tiết Thay Đổi Kỹ Thuật

### 3.1. Kịch Bản CSDL (SQL Idempotent)

Tạo tệp migration: `DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/sql/20261022-decoupling-equipment-multi-user-cluster.sql`

```sql
-- ============================================================================
-- Phân hệ: VideoWall
-- Nội dung: Bổ sung EquipmentId cho Controller, ClusterId cho Screen/Topology
-- Ngày tạo: 22/10/2026
-- Quy chuẩn: Idempotent, IF NOT EXISTS an toàn tuyệt đối
-- ============================================================================

-- 1. Bổ sung EquipmentId cho bảng VwController
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[VwController]') 
    AND name = 'EquipmentId'
)
BEGIN
    ALTER TABLE [dbo].[VwController]
    ADD [EquipmentId] NVARCHAR(64) NULL;
END
GO

-- 2. Cho phép IpAddress, Username, Password nullable trong VwController
IF EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[VwController]') 
    AND name = 'IpAddress' AND is_nullable = 0
)
BEGIN
    ALTER TABLE [dbo].[VwController] ALTER COLUMN [IpAddress] NVARCHAR(64) NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[VwController]') 
    AND name = 'Username' AND is_nullable = 0
)
BEGIN
    ALTER TABLE [dbo].[VwController] ALTER COLUMN [Username] NVARCHAR(64) NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[VwController]') 
    AND name = 'Password' AND is_nullable = 0
)
BEGIN
    ALTER TABLE [dbo].[VwController] ALTER COLUMN [Password] NVARCHAR(128) NULL;
END
GO

-- 3. Bổ sung ClusterId cho bảng VwScreen (mặc định 'main' cho cụm tường chính)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[VwScreen]') 
    AND name = 'ClusterId'
)
BEGIN
    ALTER TABLE [dbo].[VwScreen]
    ADD [ClusterId] NVARCHAR(64) NOT NULL CONSTRAINT [DF_VwScreen_ClusterId] DEFAULT 'main';
END
GO

-- 4. Bổ sung ClusterId cho bảng VwWallTopology
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[VwWallTopology]') 
    AND name = 'ClusterId'
)
BEGIN
    ALTER TABLE [dbo].[VwWallTopology]
    ADD [ClusterId] NVARCHAR(64) NOT NULL CONSTRAINT [DF_VwWallTopology_ClusterId] DEFAULT 'main';
END
GO

-- 5. Bổ sung ClusterId cho bảng VwWallPermission
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[VwWallPermission]') 
    AND name = 'ClusterId'
)
BEGIN
    ALTER TABLE [dbo].[VwWallPermission]
    ADD [ClusterId] NVARCHAR(64) NOT NULL CONSTRAINT [DF_VwWallPermission_ClusterId] DEFAULT 'main';
END
GO

-- 6. Index tối ưu hóa truy vấn theo cụm màn hình
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_VwScreen_ClusterId' 
    AND object_id = OBJECT_ID(N'[dbo].[VwScreen]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_VwScreen_ClusterId] ON [dbo].[VwScreen]([ClusterId]);
END
GO
```

### 3.2. Cập Nhật Entity `VwController.cs`

```csharp
// Thêm trường EquipmentId liên kết sang module Thiết bị (Equipment)
/// <summary>
/// Khóa ngoại liên kết thiết bị từ module Equipment (Single Source of Truth)
/// </summary>
[SugarColumn(ColumnDescription = "ID thiết bị trong module Equipment", Length = 64, IsNullable = true)]
public string? EquipmentId { get; set; }
```

### 3.3. DTO & Handler Gán Quyền Hàng Loạt (`BatchAssignPermission`)

Tạo DTO `VwWallPermissionBatchInput`:

```csharp
namespace Module.VideoWall.Dto;

public class VwWallPermissionBatchInput
{
    /// <summary>
    /// Danh sách người dùng được gán quyền
    /// </summary>
    public List<string> UserIds { get; set; } = new();

    /// <summary>
    /// ID Tổ chức / Phòng ban (nếu gán theo tổ chức)
    /// </summary>
    public string? OrgId { get; set; }

    /// <summary>
    /// Định danh cụm màn hình áp dụng (mặc định 'main')
    /// </summary>
    public string ClusterId { get; set; } = "main";

    /// <summary>
    /// Có toàn quyền truy cập hay không
    /// </summary>
    public bool IsFullAccess { get; set; }

    /// <summary>
    /// Cấu hình tọa độ ma trận ô được cấp quyền (JSON string hoặc danh sách cell)
    /// </summary>
    public string? Config { get; set; }

    /// <summary>
    /// Trạng thái hiệu lực
    /// </summary>
    public bool Status { get; set; } = true;
}
```

Tạo Handler `BatchAssignPermissionCommandHandler`:
- Mở transaction `UseTranAsync()`.
- Xóa các cấu hình quyền cũ của các `UserIds` thuộc `ClusterId` tương ứng (nếu người dùng chọn chế độ ghi đè).
- Tạo mới các bản ghi `VwWallPermission` tương ứng cho từng `UserId` với cùng một `Config` ma trận ô đã thiết lập.
- Tuân thủ quy tắc Rule 19.44: Entity `ID` do AOP tự sinh khi Insert (⛔ CẤM gán tay `entity.ID`).

### 3.4. Nguồn Tín Hiệu: Tính Toán `IsUsed` & `ActiveWindowCount`

Trong `VwSourceService` hoặc Query Handler lấy danh sách nguồn:
- Truy vấn kịch bản đang kích hoạt (`VwScene.IsActive == true`).
- Đếm số lượng cửa sổ (`VwWindowScene`) đang sử dụng `SourceId` của từng nguồn tín hiệu.
- Gán `IsUsed = activeWindowCount > 0`.
- Trả về DTO `VwSourceOutput` với 2 trường `IsUsed` và `ActiveWindowCount` để Frontend có dữ liệu rào lại (disable/gray out) trên giao diện kéo thả.

### 3.5. Worker: Phân Giải Kết Nối Thiết Bị & Model Adapter

Trong `ITS.VideoWall`:
- Khi worker gọi lệnh tới Controller:
  ```csharp
  if (!string.IsNullOrEmpty(controller.EquipmentId))
  {
      // Nạp IP, Port, Username, Password từ Equipment
      var equipment = await _equipmentRepo.GetByIdAsync(controller.EquipmentId);
      if (equipment != null)
      {
          ipAddress = equipment.IpAddress;
          port = equipment.Port;
          username = equipment.Username;
          password = equipment.Password;
      }
  }
  ```
- Nếu `EquipmentId` rỗng, giữ fallback dùng `controller.IpAddress`, `controller.Username`, `controller.Password` cũ.
- Dựa trên `controller.Model`, Worker chọn adapter phù hợp (ví dụ Hikvision v1 cho DS-C30S, Hikvision v2 cho DS-C66S cascade).

---

## 4. Tiêu Chí Nghiệm Thu (Acceptance Criteria)

1. **CSDL & Entity:**
   - [ ] Script migration SQL chạy thành công trên SQL Server local, không làm mất mát hoặc hỏng dữ liệu cũ.
   - [ ] Bảng `VwController` chứa cột `EquipmentId`, cho phép lưu controller mà không cần nhập IP/Password nếu đã chọn `EquipmentId`.
   - [ ] Các bảng `VwScreen`, `VwWallTopology`, `VwWallPermission` có trường `ClusterId` với giá trị mặc định `'main'`.
2. **API Phân Quyền Hàng Loạt:**
   - [ ] Endpoint `POST /api/VwWallPermission/BatchAssign` nhận mảng `UserIds` và lưu thành công toàn bộ trong một transaction.
   - [ ] Người dùng trong danh sách được cấp quyền chính xác theo ma trận ô đã cấu hình.
3. **Nguồn Tín Hiệu (Source):**
   - [ ] API lấy danh sách nguồn trả về đúng giá trị `IsUsed: true` nếu nguồn đang được chiếu trên một cửa sổ của kịch bản đang active; trả về `false` nếu đang rảnh.
4. **Bảo Toàn Quy Chuẩn Dự Án (P0 Safeguards):**
   - [ ] ⛔ CẤM tự gán `entity.ID` trong WebAPI (AOP tự sinh - Rule 19.44).
   - [ ] ⛔ CẤM hardcode thông điệp lỗi trong `Oops.Oh`, dùng `BaseMsg` hoặc `SysTerminology` (Rule 19.69).
   - [ ] Tuân thủ Minimal Diff, giữ nguyên 100% logic không liên quan xung quanh (Rule 19.6, Rule 19.89).
   - [ ] Ưu tiên sửa code trước, chỉ chạy test theo yêu cầu (Rule 19.67).

