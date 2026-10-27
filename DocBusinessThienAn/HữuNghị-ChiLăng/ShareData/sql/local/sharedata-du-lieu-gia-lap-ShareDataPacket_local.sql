USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql
-- BẢNG GHI: [dbo].[ShareDataPacket] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Chiều nhận E0: gói kiểm thử riêng E2E_INBOUND_TEST (OrderNo = 101). Câu SQL của gói (nếu cần) nằm ở ShareDataSql.
-- THỨ TỰ CHẠY: G1 (sau các tệp seed gói tin) (xem ../README.md).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: ⛔ CHỈ CHẠY TRÊN DB LOCAL (hậu tố _local, Rule 19.101); khối 0 tự dừng nếu không phải DB local.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 27/10/2026 (bỏ TemplateSql)
-- =========================================================================================

-- -----------------------------------------------------------------------------------------
-- 0. CHỐT DB LOCAL (Rule 19.101): tệp _local chỉ chạy trên DB local trên máy lập trình viên.
--    Máy chủ đang kết nối không phải DB local thì báo lỗi và bỏ qua toàn bộ tệp (SET NOEXEC ON tới cuối tệp;
--    RETURN chỉ thoát batch hiện tại nên không dùng được).
--    Được coi là DB local khi:
--      - kết nối cục bộ không qua mạng (shared memory / named pipe: địa chỉ NULL);
--      - SQL Server cài thẳng trên máy (127.0.0.1 / ::1);
--      - SQL Server chạy trong Docker trên máy (địa chỉ máy chủ thuộc dải Docker 172.16.0.0/12, vd 172.18.0.6).
-- -----------------------------------------------------------------------------------------
DECLARE @DiaChiMayChu NVARCHAR(48) = CAST(CONNECTIONPROPERTY('local_net_address') AS NVARCHAR(48));
DECLARE @DiaChiMayKhach NVARCHAR(48) = CAST(CONNECTIONPROPERTY('client_net_address') AS NVARCHAR(48));

PRINT N'Đang kết nối: máy chủ ' + ISNULL(@@SERVERNAME, N'?')
    + N' | địa chỉ máy chủ ' + ISNULL(@DiaChiMayChu, N'(cục bộ)')
    + N' | địa chỉ máy khách ' + ISNULL(@DiaChiMayKhach, N'(cục bộ)')
    + N' | CSDL ' + DB_NAME();

IF NOT (
       @DiaChiMayChu IS NULL
    OR @DiaChiMayChu IN (N'127.0.0.1', N'::1')
    OR @DiaChiMayKhach IN (N'<local machine>', N'127.0.0.1', N'::1')
    OR @DiaChiMayChu LIKE N'172.1[6-9].%'
    OR @DiaChiMayChu LIKE N'172.2[0-9].%'
    OR @DiaChiMayChu LIKE N'172.3[01].%'
)
BEGIN
    RAISERROR(N'⛔ Tệp _local chỉ chạy trên DB local (Rule 19.101). Máy chủ đang kết nối (%s) không phải DB local — dừng, không ghi dữ liệu.', 16, 1, @DiaChiMayChu);
    SET NOEXEC ON;
END;
GO

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql';
PRINT N'=============================================================================';
GO

-- -----------------------------------------------------------------------------------------
-- 1. KHỞI TẠO GÓI KIỂM THỬ E2E_INBOUND_TEST
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';
DECLARE @DefaultTenantId NVARCHAR(64) = (
    SELECT TOP 1 [TenantId]
    FROM [dbo].[ShareDataPacket]
    WHERE [TenantId] IS NOT NULL
);
IF @DefaultTenantId IS NULL
    SET @DefaultTenantId = N'Default';

-- 4.2.b Khởi tạo/cập nhật gói kiểm thử chiều nhận E2E_INBOUND_TEST trong ShareDataPacket
IF OBJECT_ID(N'[dbo].[ShareDataPacket]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[ShareDataPacket]
        WHERE [Code] = N'E2E_INBOUND_TEST' AND [IsDelete] IS NULL
    )
    BEGIN
        INSERT INTO [dbo].[ShareDataPacket] (
            [ID], [TenantId], [Code], [Name], [PacketVersion], [Description],
            [OrderNo], [Status],
            [Remark], [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
        ) VALUES (
            LOWER(CONVERT(NVARCHAR(64), NEWID())),
            @DefaultTenantId,
            N'E2E_INBOUND_TEST',
            N'Gói kiểm thử chiều nhận E0 (chỉ local)',
            N'1.0',
            N'Gói kiểm thử chiều nhận độc lập cho kịch bản E0 trên môi trường local/test',
            101,
            1,
            N'Chỉ dùng kiểm thử chiều nhận E0 trên local/test, tách biệt với gói 101_commonData',
            GETDATE(),
            GETDATE(),
            NULL,
            @SeedUser
        );
        PRINT N'  -> [Khối 4.2] Đã thêm gói kiểm thử E2E_INBOUND_TEST vào ShareDataPacket (OrderNo = 101).';
    END
    ELSE
    BEGIN
        UPDATE [dbo].[ShareDataPacket]
        SET [OrderNo] = 101,
            [UpdateTime] = GETDATE()
        WHERE [Code] = N'E2E_INBOUND_TEST'
          AND [IsDelete] IS NULL
          AND [OrderNo] <> 101;

        PRINT N'  -> [Khối 4.2] Gói kiểm thử E2E_INBOUND_TEST đã tồn tại trong ShareDataPacket (đồng bộ OrderNo = 101).';
    END

END
ELSE
BEGIN
    PRINT N'⚠️ [Khối 4.2] Bảng dbo.ShareDataPacket chưa tồn tại trên DEV_ITS10, bỏ qua nạp gói E2E_INBOUND_TEST.';
END
GO

-- -----------------------------------------------------------------------------------------
-- DỌN DẸP DỮ LIỆU GIẢ LẬP [ShareDataPacket] (KÍCH HOẠT KHI @XoaDuLieuGiaLap = 1)
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;

DECLARE @XoaDuLieuGiaLap BIT = 0; -- Đặt = 1 khi cần dọn sạch dữ liệu mock của bảng này
DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

IF @XoaDuLieuGiaLap = 1 AND OBJECT_ID(N'[dbo].[ShareDataPacket]', N'U') IS NOT NULL
BEGIN
    DELETE FROM [dbo].[ShareDataPacket] WHERE [Code] = N'E2E_INBOUND_TEST';
    PRINT N'  -> Đã xóa gói kiểm thử E2E_INBOUND_TEST trong ShareDataPacket: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
END
ELSE
BEGIN
    PRINT N'  -> Biến @XoaDuLieuGiaLap = 0 (mặc định), không kích hoạt khối dọn dẹp.';
END
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql';
PRINT N'=============================================================================';
GO
