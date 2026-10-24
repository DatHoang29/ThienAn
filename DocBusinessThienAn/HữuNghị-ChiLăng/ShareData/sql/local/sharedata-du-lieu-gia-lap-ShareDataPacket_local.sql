USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql
-- BẢNG GHI: [dbo].[ShareDataPacket] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Chiều nhận E0: gói kiểm thử riêng E2E_INBOUND_TEST (OrderNo = 101), TemplateSql lấy theo gói 101.
-- THỨ TỰ CHẠY: G1 (sau các tệp seed gói tin) (xem ../README.md).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: ⚠️ CHỈ CHẠY TRÊN LOCAL VÀ TEST (hậu tố _local, Rule 19.101).
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 24/10/2026
-- =========================================================================================

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
    DECLARE @Template101 NVARCHAR(MAX);
    SELECT TOP 1
        @Template101 = [TemplateSql]
    FROM [dbo].[ShareDataPacket]
    WHERE ([Code] = N'101' OR [Code] LIKE N'101[_]%')
      AND [IsDelete] IS NULL
    ORDER BY CASE WHEN [Code] = N'101' THEN 0 ELSE 1 END, [ID];

    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[ShareDataPacket]
        WHERE [Code] = N'E2E_INBOUND_TEST' AND [IsDelete] IS NULL
    )
    BEGIN
        INSERT INTO [dbo].[ShareDataPacket] (
            [ID], [TenantId], [Code], [Name], [PacketVersion], [Description],
            [OrderNo], [Status], [TemplateSql],
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
            @Template101,
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
            [TemplateSql] = COALESCE([TemplateSql], @Template101),
            [UpdateTime] = GETDATE()
        WHERE [Code] = N'E2E_INBOUND_TEST'
          AND [IsDelete] IS NULL
          AND ([OrderNo] <> 101 OR [TemplateSql] IS NULL);

        PRINT N'  -> [Khối 4.2] Gói kiểm thử E2E_INBOUND_TEST đã tồn tại trong ShareDataPacket (đồng bộ OrderNo = 101 và TemplateSql).';
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
