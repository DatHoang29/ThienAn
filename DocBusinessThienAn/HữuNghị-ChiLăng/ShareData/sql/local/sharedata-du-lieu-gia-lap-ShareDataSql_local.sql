USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-du-lieu-gia-lap-ShareDataSql_local.sql
-- BẢNG GHI: [dbo].[ShareDataSql] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Chiều nhận E0: câu SQL mẫu chung của gói E2E_INBOUND_TEST, lấy theo câu mẫu chung của gói 101.
-- THỨ TỰ CHẠY: G2 (sau sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql) (xem ../README.md).
-- GHI CHÚ:     Khi dọn dẹp, chạy khối dọn dẹp của tệp này trước tệp ShareDataPacket_local.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: ⚠️ CHỈ CHẠY TRÊN LOCAL VÀ TEST (hậu tố _local, Rule 19.101).
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 24/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-du-lieu-gia-lap-ShareDataSql_local.sql';
PRINT N'=============================================================================';
GO

-- -----------------------------------------------------------------------------------------
-- 1. CÂU SQL MẪU CHUNG CỦA GÓI KIỂM THỬ E2E_INBOUND_TEST (THEO GÓI 101)
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

IF OBJECT_ID(N'[dbo].[ShareDataSql]', N'U') IS NOT NULL
BEGIN
    DECLARE @Extract101 NVARCHAR(MAX) = (
        SELECT TOP 1 s.[ExtractSql]
        FROM [dbo].[ShareDataSql] s
        INNER JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
        WHERE (p.[Code] = N'101' OR p.[Code] LIKE N'101[_]%') AND p.[IsDelete] IS NULL
          AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL
        ORDER BY CASE WHEN p.[Code] = N'101' THEN 0 ELSE 1 END, p.[ID]
    );

    IF @Extract101 IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[ShareDataSql] ([ID], [TenantId], [PacketId], [PartnerId], [ExtractSql], [CreateTime], [IsDelete], [CreateUId])
        SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), @DefaultTenantId, p.[ID], NULL, @Extract101, GETDATE(), NULL, @SeedUser
        FROM [dbo].[ShareDataPacket] p
        WHERE p.[Code] = N'E2E_INBOUND_TEST' AND p.[IsDelete] IS NULL
          AND NOT EXISTS (
              SELECT 1 FROM [dbo].[ShareDataSql] s
              WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
        PRINT N'  -> Đã nạp câu SQL mẫu chung cho gói E2E_INBOUND_TEST: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
    END
    ELSE
    BEGIN
        PRINT N'⚠️ Gói 101 chưa có câu SQL mẫu chung trong ShareDataSql, bỏ qua.';
    END
END
ELSE
BEGIN
    PRINT N'⚠️ Bảng dbo.ShareDataSql chưa tồn tại trên DEV_ITS10, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- DỌN DẸP DỮ LIỆU GIẢ LẬP [ShareDataSql] (KÍCH HOẠT KHI @XoaDuLieuGiaLap = 1)
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;

DECLARE @XoaDuLieuGiaLap BIT = 0; -- Đặt = 1 khi cần dọn sạch dữ liệu mock của bảng này
DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

IF @XoaDuLieuGiaLap = 1 AND OBJECT_ID(N'[dbo].[ShareDataSql]', N'U') IS NOT NULL
BEGIN
    DELETE FROM [dbo].[ShareDataSql] WHERE [CreateUId] = @SeedUser OR [PacketId] IN (SELECT [ID] FROM [dbo].[ShareDataPacket] WHERE [Code] = N'E2E_INBOUND_TEST');
    PRINT N'  -> Đã xóa câu SQL của gói kiểm thử E2E_INBOUND_TEST trong ShareDataSql: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
END
ELSE
BEGIN
    PRINT N'  -> Biến @XoaDuLieuGiaLap = 0 (mặc định), không kích hoạt khối dọn dẹp.';
END
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-du-lieu-gia-lap-ShareDataSql_local.sql';
PRINT N'=============================================================================';
GO
