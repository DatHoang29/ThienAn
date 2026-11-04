USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-chuyen-doi-moc-ShareDataLastSend.sql
-- BẢNG GHI: [dbo].[ShareDataLastSend] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: 1. Gỡ unique index 2 cột cũ [index_ShareDataLastSend_Partner_Packet] trên [dbo].[ShareDataLastSend] nếu còn tồn tại
--                 (nhường chỗ cho unique index 3 cột [index_ShareDataLastSend_Partner_Packet_Key] do CodeFirst tạo).
--              2. Chép không trùng (NOT EXISTS) các dòng nhóm mốc có tên từ bảng cũ [dbo].[ShareDataLastSendKey] (chỉ đọc)
--                 sang [dbo].[ShareDataLastSend] khi bảng cũ còn tồn tại và cột [CursorKey] đã có.
-- THỨ TỰ CHẠY: 11 (sau khi CodeFirst đã tạo cột CursorKey; tắt Worker trước khi chạy).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 03/11/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-chuyen-doi-moc-ShareDataLastSend.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataLastSend]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataLastSend] chưa tồn tại. Dừng thực thi kịch bản.';
    SET NOEXEC ON;
END
GO

-- 1. Gỡ unique index 2 cột cũ (PartnerCode, PacketCode) nếu còn tồn tại trên ShareDataLastSend
PRINT N'-> 1. Kiểm tra và gỡ unique index 2 cột cũ trên ShareDataLastSend...';

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[dbo].[ShareDataLastSend]')
      AND [name] = N'index_ShareDataLastSend_Partner_Packet'
)
BEGIN
    DROP INDEX [index_ShareDataLastSend_Partner_Packet] ON [dbo].[ShareDataLastSend];
    PRINT N'   - Đã gỡ unique index 2 cột cũ [index_ShareDataLastSend_Partner_Packet].';
END
ELSE
BEGIN
    PRINT N'   - Unique index 2 cột cũ [index_ShareDataLastSend_Partner_Packet] không còn tồn tại (bỏ qua).';
END
GO

-- 2. Chép không trùng các dòng nhóm mốc có tên từ bảng cũ ShareDataLastSendKey (chỉ đọc) sang ShareDataLastSend
PRINT N'-> 2. Chuyển nhóm mốc có tên từ ShareDataLastSendKey (nếu còn) sang ShareDataLastSend...';

IF OBJECT_ID(N'[dbo].[ShareDataLastSendKey]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ShareDataLastSend', N'CursorKey') IS NULL
    BEGIN
        PRINT N'   ⚠️ Cột [CursorKey] chưa có trên [dbo].[ShareDataLastSend] (chạy CodeFirst trước). Bỏ qua bước chép nhóm mốc.';
    END
    ELSE
    BEGIN
        DECLARE @MovedKeys INT = 0;
        DECLARE @MoveKeysSql NVARCHAR(MAX) = N'
        INSERT INTO [dbo].[ShareDataLastSend] (
            [ID], [PartnerCode], [PacketCode], [CursorKey],
            [LastTime], [LastKey], [CursorSignature],
            [TenantId], [CreateTime], [CreateUId], [UpdateTime], [UpdateUId]
        )
        SELECT
            LOWER(CONVERT(NVARCHAR(64), NEWID())),
            k.[PartnerCode], k.[PacketCode], LTRIM(RTRIM(k.[CursorKey])),
            k.[LastTime], k.[LastKey], k.[CursorSignature],
            k.[TenantId], ISNULL(k.[CreateTime], GETDATE()), ISNULL(k.[CreateUId], N''SHAREDATA-MIGRATE-11''),
            ISNULL(k.[UpdateTime], GETDATE()), k.[UpdateUId]
        FROM [dbo].[ShareDataLastSendKey] k
        WHERE k.[IsDelete] IS NULL
          AND k.[CursorKey] IS NOT NULL
          AND LTRIM(RTRIM(k.[CursorKey])) <> N''''
          AND NOT EXISTS (
              SELECT 1
              FROM [dbo].[ShareDataLastSend] ls
              WHERE ls.[IsDelete] IS NULL
                AND ISNULL(ls.[PartnerCode], N'''') = ISNULL(k.[PartnerCode], N'''')
                AND ISNULL(ls.[PacketCode], N'''') = ISNULL(k.[PacketCode], N'''')
                AND ls.[CursorKey] = LTRIM(RTRIM(k.[CursorKey]))
          );';
        EXEC sp_executesql @MoveKeysSql;
        SET @MovedKeys = @@ROWCOUNT;
        PRINT N'   - Đã chép ' + CAST(@MovedKeys AS NVARCHAR(10)) + N' dòng nhóm mốc có tên sang [ShareDataLastSend].';
    END
END
ELSE
BEGIN
    PRINT N'   - Bảng cũ [dbo].[ShareDataLastSendKey] không tồn tại (bỏ qua).';
END
GO

PRINT N'=============================================================================';
PRINT N'HOÀN TẤT KỊCH BẢN sharedata-chuyen-doi-moc-ShareDataLastSend.sql';
PRINT N'=============================================================================';
GO
SET NOEXEC OFF;
GO
