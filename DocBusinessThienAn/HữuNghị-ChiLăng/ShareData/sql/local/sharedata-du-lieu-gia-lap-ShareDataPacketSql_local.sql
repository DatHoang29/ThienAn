USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-du-lieu-gia-lap-ShareDataPacketSql_local.sql
-- BẢNG GHI: [dbo].[ShareDataPacketSql] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Chiều nhận E0: xóa mềm các dòng ShareDataPacketSql cũ của gói 101 do bản cũ của script tạo,
--              nạp câu ghi MERGE chiều nhận cho gói E2E_INBOUND_TEST vào ShareDataPacketSql.
-- THỨ TỰ CHẠY: G3 (xem ../README.md).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: ⚠️ CHỈ CHẠY TRÊN LOCAL VÀ TEST (hậu tố _local, Rule 19.101).
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 24/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-du-lieu-gia-lap-ShareDataPacketSql_local.sql';
PRINT N'=============================================================================';
GO

-- -----------------------------------------------------------------------------------------
-- 1. CÂU GHI CHIỀU NHẬN CỦA GÓI KIỂM THỬ E2E_INBOUND_TEST
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

-- 4.2.a Xóa mềm idempotent các dòng ShareDataPacketSql cũ nếu từng chạy bản cũ
IF OBJECT_ID(N'[dbo].[ShareDataPacketSql]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM [dbo].[ShareDataPacketSql]
        WHERE (
            ([CreateUId] = @SeedUser AND [PacketCode] IN (N'101', N'101_commonData'))
            OR [ID] IN (N'1500000000901', N'1500000000902') -- dòng do bản cũ của script tạo
        ) AND [IsDelete] IS NULL
    )
    BEGIN
        UPDATE [dbo].[ShareDataPacketSql]
        SET [IsDelete] = GETDATE(),
            [Status] = 0,
            [UpdateTime] = GETDATE()
        WHERE (
            ([CreateUId] = @SeedUser AND [PacketCode] IN (N'101', N'101_commonData'))
            OR [ID] IN (N'1500000000901', N'1500000000902')
        ) AND [IsDelete] IS NULL;
        PRINT N'  -> [Khối 4.2] Đã xóa mềm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng ShareDataPacketSql cũ của gói 101/101_commonData.';
    END
    ELSE
    BEGIN
        PRINT N'  -> [Khối 4.2] Không có dòng ShareDataPacketSql cũ cần xóa mềm, bỏ qua.';
    END
END

-- 4.2.c Nạp câu ghi MERGE chiều nhận cho gói E2E_INBOUND_TEST vào ShareDataPacketSql
IF OBJECT_ID(N'[dbo].[ShareDataPacketSql]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[ShareDataPacketSql]
        WHERE [PacketCode] = N'E2E_INBOUND_TEST' AND [TableName] = N'dbo.InboundTrafficFlow' AND [IsDelete] IS NULL
    )
    BEGIN
        INSERT INTO [dbo].[ShareDataPacketSql] (
            [ID], [TenantId], [PacketCode], [Code], [OrderNo], [TableName], [WriteSql],
            [Status], [Remark], [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
        ) VALUES (
            LOWER(CONVERT(NVARCHAR(64), NEWID())),
            @DefaultTenantId,
            N'E2E_INBOUND_TEST',
            N'E2E_INBOUND_TEST',
            1,
            N'dbo.InboundTrafficFlow',
            N'MERGE dbo.InboundTrafficFlow AS target
USING (
    SELECT id, code, recorded_time, val
    FROM OPENJSON(@records)
    WITH (
        id NVARCHAR(64) ''$.id'',
        code NVARCHAR(64) ''$.code'',
        recorded_time DATETIME2 ''$.time'',
        val INT ''$.val''
    )
) AS src
ON target.id = src.id
WHEN MATCHED THEN
    UPDATE SET target.code = src.code, target.recorded_time = src.recorded_time, target.val = src.val
WHEN NOT MATCHED THEN
    INSERT (id, code, recorded_time, val) VALUES (src.id, src.code, src.recorded_time, src.val)
OUTPUT $action;',
            1,
            N'Chỉ dùng kiểm thử chiều nhận E0 trên local/test cho gói E2E_INBOUND_TEST',
            GETDATE(),
            GETDATE(),
            NULL,
            @SeedUser
        );
        PRINT N'  -> [Khối 4.2] Đã nạp câu ghi MERGE chiều nhận cho gói E2E_INBOUND_TEST vào ShareDataPacketSql.';
    END
    ELSE
    BEGIN
        PRINT N'  -> [Khối 4.2] Câu ghi MERGE chiều nhận cho gói E2E_INBOUND_TEST đã tồn tại trong ShareDataPacketSql, bỏ qua.';
    END
END
ELSE
BEGIN
    PRINT N'⚠️ [Khối 4.2] Bảng dbo.ShareDataPacketSql chưa tồn tại trên DEV_ITS10, bỏ qua nạp câu ghi chiều nhận.';
END
GO

-- -----------------------------------------------------------------------------------------
-- DỌN DẸP DỮ LIỆU GIẢ LẬP [ShareDataPacketSql] (KÍCH HOẠT KHI @XoaDuLieuGiaLap = 1)
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;

DECLARE @XoaDuLieuGiaLap BIT = 0; -- Đặt = 1 khi cần dọn sạch dữ liệu mock của bảng này
DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

IF @XoaDuLieuGiaLap = 1 AND OBJECT_ID(N'[dbo].[ShareDataPacketSql]', N'U') IS NOT NULL
BEGIN
    DELETE FROM [dbo].[ShareDataPacketSql] WHERE [PacketCode] = N'E2E_INBOUND_TEST';
    PRINT N'  -> Đã xóa câu ghi E2E_INBOUND_TEST trong ShareDataPacketSql: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
END
ELSE
BEGIN
    PRINT N'  -> Biến @XoaDuLieuGiaLap = 0 (mặc định), không kích hoạt khối dọn dẹp.';
END
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-du-lieu-gia-lap-ShareDataPacketSql_local.sql';
PRINT N'=============================================================================';
GO
