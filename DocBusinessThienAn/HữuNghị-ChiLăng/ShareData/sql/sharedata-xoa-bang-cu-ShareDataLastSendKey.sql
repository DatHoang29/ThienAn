USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-bang-cu-ShareDataLastSendKey.sql
-- BẢNG GHI: [dbo].[ShareDataLastSendKey] — tệp chỉ xóa đúng bảng cũ này; bảng [dbo].[ShareDataLastSend] chỉ đọc để đối soát an toàn (Rule 19.103).
-- DESCRIPTION: Xóa bảng cũ [dbo].[ShareDataLastSendKey] sau khi đã gộp cột [CursorKey] vào [dbo].[ShareDataLastSend]
--              và chuyển đủ 100% dòng nhóm mốc có tên qua kịch bản 11 (sharedata-chuyen-doi-moc-ShareDataLastSend.sql).
--              Chặn thực thi bằng SET NOEXEC ON nếu bảng [dbo].[ShareDataLastSend] / cột [CursorKey] chưa tồn tại
--              hoặc còn dòng hiệu lực (IsDelete IS NULL) chưa chuyển sang [dbo].[ShareDataLastSend].
-- THỨ TỰ CHẠY: 12 (chạy sau sharedata-chuyen-doi-moc-ShareDataLastSend.sql)
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Thao tác xóa bảng cũ một lần, chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 02/11/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-bang-cu-ShareDataLastSendKey.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataLastSendKey]', N'U') IS NULL
BEGIN
    PRINT N'✅ Bảng cũ [dbo].[ShareDataLastSendKey] không tồn tại (đã được dọn trước đó). Kết thúc an toàn.';
    SET NOEXEC ON;
END
GO

-- 1. Kiểm tra điều kiện tiên quyết trước khi xóa bảng cũ ShareDataLastSendKey
PRINT N'-> 1. Kiểm tra điều kiện an toàn trên [dbo].[ShareDataLastSend] trước khi xóa bảng cũ...';

IF OBJECT_ID(N'[dbo].[ShareDataLastSend]', N'U') IS NULL
   OR COL_LENGTH(N'dbo.ShareDataLastSend', N'CursorKey') IS NULL
BEGIN
    PRINT N'⛔ LỖI CHẶN XÓA BẢNG: Bảng [dbo].[ShareDataLastSend] hoặc cột [CursorKey] chưa tồn tại. Vui lòng chạy CodeFirst và sharedata-chuyen-doi-moc-ShareDataLastSend.sql trước.';
    SET NOEXEC ON;
END
ELSE
BEGIN
    DECLARE @UnmigratedCount INT = 0;
    DECLARE @CheckSql NVARCHAR(MAX) = N'
    SELECT @Cnt = COUNT(1)
    FROM [dbo].[ShareDataLastSendKey] k
    WHERE k.[IsDelete] IS NULL
      AND NOT EXISTS (
          SELECT 1
          FROM [dbo].[ShareDataLastSend] ls
          WHERE ls.[IsDelete] IS NULL
            AND ISNULL(ls.[PartnerCode], N'''') = ISNULL(k.[PartnerCode], N'''')
            AND ISNULL(ls.[PacketCode], N'''') = ISNULL(k.[PacketCode], N'''')
            AND ISNULL(ls.[CursorKey], N'''') = ISNULL(LTRIM(RTRIM(k.[CursorKey])), N'''')
      );';

    EXEC sp_executesql @CheckSql, N'@Cnt INT OUTPUT', @Cnt = @UnmigratedCount OUTPUT;

    IF @UnmigratedCount > 0
    BEGIN
        PRINT N'⛔ LỖI CHẶN XÓA BẢNG: Còn ' + CAST(@UnmigratedCount AS NVARCHAR(10))
            + N' dòng hiệu lực (IsDelete IS NULL) trong [dbo].[ShareDataLastSendKey] chưa có mặt trong [dbo].[ShareDataLastSend] theo (PartnerCode, PacketCode, CursorKey). Vui lòng chạy sharedata-chuyen-doi-moc-ShareDataLastSend.sql trước.';
        SET NOEXEC ON;
    END
    ELSE
    BEGIN
        PRINT N'   - Đã đối soát an toàn: 100% dòng hiệu lực đã có mặt trong [dbo].[ShareDataLastSend].';
    END
END
GO

-- 2. Xóa bảng cũ ShareDataLastSendKey khi đã đủ điều kiện an toàn
PRINT N'-> 2. Thực hiện xóa bảng cũ [dbo].[ShareDataLastSendKey]...';

IF OBJECT_ID(N'[dbo].[ShareDataLastSendKey]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[ShareDataLastSendKey];
    PRINT N'   - ✅ Đã xóa bảng cũ [dbo].[ShareDataLastSendKey] thành công.';
END
GO

PRINT N'=============================================================================';
PRINT N'HOÀN TẤT KỊCH BẢN sharedata-xoa-bang-cu-ShareDataLastSendKey.sql';
PRINT N'=============================================================================';
GO
SET NOEXEC OFF;
GO
