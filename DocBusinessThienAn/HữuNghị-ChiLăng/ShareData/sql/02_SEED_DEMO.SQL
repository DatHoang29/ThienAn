-- ============================================================================
-- MODULE: SHARE DATA (SHARE DATA V1)
-- FILE: 02_SEED_DEMO.SQL
-- DESCRIPTION: Nạp Dữ Liệu Mẫu Demo Cho 6 Đối Tác & 6 Subscriptions Đầu Ra (Outbound)
-- AUTHOR: Đạt
-- CREATED DATE: 2026-08-05
-- ============================================================================

-- ============================================================================
-- CHỐT CHẶN SERVER
--   File .sql KHÔNG chọn được server — server do kết nối của trình soạn thảo
--   quyết định. Điền tên server bạn MUỐN chạy vào @ServerKyVong để script tự
--   dừng nếu chạy nhầm máy. Để trống thì bỏ qua kiểm.
--   Xem tên server hiện tại: SELECT @@SERVERNAME
-- ============================================================================
SET NOEXEC OFF;

DECLARE @ServerKyVong SYSNAME = N'';   -- ví dụ: N'aa0914ef791d'

IF @ServerKyVong <> N'' AND @@SERVERNAME <> @ServerKyVong
BEGIN
    RAISERROR(N'DUNG LAI: dang o server [%s] nhung ky vong [%s]. Khong thuc thi gi ca.',
              16, 1, @@SERVERNAME, @ServerKyVong);
    SET NOEXEC ON;
END;
GO

-- Ép về đúng database của môi trường local. Đổi tên ở đây nếu dùng database khác.
-- Đặt SAU chốt chặn để khi sai server thì NOEXEC đã bật, câu USE này cũng không chạy.
USE [dev_its10];
GO

PRINT N'▶ Server: ' + @@SERVERNAME
    + N' | Database: ' + DB_NAME()
    + N' | Login: ' + SUSER_SNAME()
    + N' | Phiên bản: ' + CONVERT(NVARCHAR(100), SERVERPROPERTY('Edition'));
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- 0. KIỂM TRA BẢNG CSDL TỒN TẠI CHƯA
    IF OBJECT_ID('dbo.ShareDataPartner', 'U') IS NULL
    BEGIN
        RAISERROR(N'⚠️ Chưa tìm thấy các bảng CSDL ShareData! Vui lòng chạy file 01_CREATE_TABLES.SQL trước khi nạp Seed Data.', 16, 1);
        RETURN;
    END;

    -- 1. XOÁ DỮ LIỆU CŨ TRÁNH XUNG ĐỘT (NẾU BẢNG ĐÃ TỒN TẠI)
    IF OBJECT_ID('dbo.ShareDataSubscription', 'U') IS NOT NULL
        DELETE FROM [dbo].[ShareDataSubscription] WHERE [Code] LIKE 'SUB-DEMO-%' OR [ID] LIKE 'SUB-D-%';
    IF OBJECT_ID('dbo.ShareDataMappingProfile', 'U') IS NOT NULL
        DELETE FROM [dbo].[ShareDataMappingProfile] WHERE [Code] LIKE 'MP-DEMO-%' OR [ID] LIKE 'MP-DEMO-%';
    IF OBJECT_ID('dbo.ShareDataDataSource', 'U') IS NOT NULL
        DELETE FROM [dbo].[ShareDataDataSource] WHERE [Code] LIKE 'DS-DEMO-%' OR [ID] LIKE 'DS-DEMO-%';
    IF OBJECT_ID('dbo.ShareDataPartner', 'U') IS NOT NULL
        DELETE FROM [dbo].[ShareDataPartner] WHERE [Code] LIKE 'PTN-DEMO-%' OR [ID] LIKE 'P-DEMO-%';

    -- 2. THÊM 6 ĐỐI TÁC DEMO (PTN-DEMO-001 ➔ PTN-DEMO-006)
    INSERT INTO [dbo].[ShareDataPartner] ([ID], [Code], [Name], [Status], [SessionState], [TenantId], [CreateTime], [IsDelete])
    VALUES
    ('P-DEMO-001', 'PTN-DEMO-001', N'Sở Giao thông Vận tải Hà Nội',   1, 1, '1', GETDATE(), NULL),
    ('P-DEMO-002', 'PTN-DEMO-002', N'Cục Cảnh sát Giao thông (C08)', 1, 1, '1', GETDATE(), NULL),
    ('P-DEMO-003', 'PTN-DEMO-003', N'Cục Đường bộ Việt Nam',          1, 1, '1', GETDATE(), NULL),
    ('P-DEMO-004', 'PTN-DEMO-004', N'Trung tâm Khí tượng Thủy văn',   1, 1, '1', GETDATE(), NULL),
    ('P-DEMO-005', 'PTN-DEMO-005', N'Công ty Đầu tư Thu phí VETC',     1, 1, '1', GETDATE(), NULL),
    ('P-DEMO-006', 'PTN-DEMO-006', N'Công ty Thu phí Tự động VDTC',    1, 1, '1', GETDATE(), NULL);

    -- 3. THÊM 6 NGUỒN DỮ LIỆU (DS-DEMO-001 ➔ DS-DEMO-006)
    INSERT INTO [dbo].[ShareDataDataSource] ([ID], [Code], [Name], [Kind], [TableOrView], [QueryText], [TopN], [TenantId], [CreateTime], [IsDelete])
    VALUES
    ('DS-DEMO-001', 'DS-DEMO-001', N'Nguồn Dữ liệu Giao thông (FIELD_PICKER)', 'FIELD_PICKER', 'TmsTrafficData', NULL, 100, '1', GETDATE(), NULL),
    ('DS-DEMO-002', 'DS-DEMO-002', N'Nguồn Dữ liệu Đăng kiểm Xe (SAVED_QUERY)', 'SAVED_QUERY', NULL, N'SELECT Location AS VehicleNo, Speed AS Speed FROM TmsTrafficData', 50, '1', GETDATE(), NULL),
    ('DS-DEMO-003', 'DS-DEMO-003', N'Nguồn Dữ liệu Sự cố Giao thông (FIELD_PICKER)', 'FIELD_PICKER', 'TmsTrafficData', NULL, 100, '1', GETDATE(), NULL),
    ('DS-DEMO-004', 'DS-DEMO-004', N'Nguồn Dữ liệu Khí tượng Thủy văn (FIELD_PICKER)', 'FIELD_PICKER', 'TmsTrafficData', NULL, 100, '1', GETDATE(), NULL),
    ('DS-DEMO-005', 'DS-DEMO-005', N'Nguồn Dữ liệu Thu phí Không dừng (SAVED_QUERY)', 'SAVED_QUERY', NULL, N'SELECT Location AS StationCode, Speed AS FeeAmount FROM TmsTrafficData', 100, '1', GETDATE(), NULL),
    ('DS-DEMO-006', 'DS-DEMO-006', N'Nguồn Dữ liệu Sự kiện Xe (SAVED_QUERY)', 'SAVED_QUERY', NULL, N'SELECT LicensePlate AS PlateNo, DetectTime AS EventTime FROM TmsTrafficData', 100, '1', GETDATE(), NULL);

    -- 4. THÊM 6 HỒ SƠ ÁNH XẠ VỚI DATATYPE_ID THEO CHUẨN MÃ GÓI TIN (101 -> 109)
    INSERT INTO [dbo].[ShareDataMappingProfile] ([ID], [Code], [Name], [VendorId], [DatatypeId], [Direction], [DataSourceId], [IsActive], [TenantId], [CreateTime], [IsDelete])
    VALUES
    ('MP-DEMO-001', 'MP-DEMO-001', N'Ánh xạ Lưu lượng GT (Gói 101)', 'P-DEMO-001', '101', 'OUT', 'DS-DEMO-001', 1, '1', GETDATE(), NULL),
    ('MP-DEMO-002', 'MP-DEMO-002', N'Ánh xạ Hình ảnh CCTV (Gói 102)', 'P-DEMO-002', '102', 'OUT', 'DS-DEMO-002', 1, '1', GETDATE(), NULL),
    ('MP-DEMO-003', 'MP-DEMO-003', N'Ánh xạ Dò xe VDS (Gói 103)',     'P-DEMO-003', '103', 'OUT', 'DS-DEMO-003', 1, '1', GETDATE(), NULL),
    ('MP-DEMO-004', 'MP-DEMO-004', N'Ánh xạ Định danh xe (Gói 105)', 'P-DEMO-004', '105', 'OUT', 'DS-DEMO-004', 1, '1', GETDATE(), NULL),
    ('MP-DEMO-005', 'MP-DEMO-005', N'Ánh xạ Sự cố GT (Gói 107)',    'P-DEMO-005', '107', 'OUT', 'DS-DEMO-005', 1, '1', GETDATE(), NULL),
    ('MP-DEMO-006', 'MP-DEMO-006', N'Ánh xạ Thu phí ETC (Gói 109)',  'P-DEMO-006', '109', 'OUT', 'DS-DEMO-006', 1, '1', GETDATE(), NULL);

    -- 5. THÊM 6 SUBSCRIPTIONS ĐẦU RA HOẠT ĐỘNG VỚI MÃ GÓI TIN CHUẨN (101, 102, 103, 105, 107, 109)
    INSERT INTO [dbo].[ShareDataSubscription] 
    ([ID], [Code], [SerialNbr], [PartnerId], [DatatypeId], [MappingProfileId], [DataSourceId], [Direction], [Mode], [State], [NextTimeRun], [TenantId], [CreateTime], [IsDelete])
    VALUES
    ('SUB-D-001', 'SUB-DEMO-001', 1, 'P-DEMO-001', '101', 'MP-DEMO-001', 'DS-DEMO-001', 'OUTBOUND', 'PERIODIC', 1, GETDATE(), '1', GETDATE(), NULL),
    ('SUB-D-002', 'SUB-DEMO-002', 2, 'P-DEMO-002', '102', 'MP-DEMO-002', 'DS-DEMO-002', 'OUTBOUND', 'PERIODIC', 1, GETDATE(), '1', GETDATE(), NULL),
    ('SUB-D-003', 'SUB-DEMO-003', 3, 'P-DEMO-003', '103', 'MP-DEMO-003', 'DS-DEMO-003', 'OUTBOUND', 'PERIODIC', 1, GETDATE(), '1', GETDATE(), NULL),
    ('SUB-D-004', 'SUB-DEMO-004', 4, 'P-DEMO-004', '105', 'MP-DEMO-004', 'DS-DEMO-004', 'OUTBOUND', 'PERIODIC', 1, GETDATE(), '1', GETDATE(), NULL),
    ('SUB-D-005', 'SUB-DEMO-005', 5, 'P-DEMO-005', '107', 'MP-DEMO-005', 'DS-DEMO-005', 'OUTBOUND', 'PERIODIC', 1, GETDATE(), '1', GETDATE(), NULL),
    ('SUB-D-006', 'SUB-DEMO-006', 6, 'P-DEMO-006', '109', 'MP-DEMO-006', 'DS-DEMO-006', 'OUTBOUND', 'PERIODIC', 1, GETDATE(), '1', GETDATE(), NULL);

    COMMIT TRANSACTION;
    PRINT N'✅ Nạp dữ liệu mẫu ShareData cho 6 Subscriptions thành công 100%!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
GO

SET NOEXEC OFF;
GO
