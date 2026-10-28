USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-chuyen-doi-moc-ShareDataLastSend.sql
-- BẢNG GHI: [dbo].[ShareDataLastSend] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Chuyển đổi mốc nối đuôi cũ dạng chuỗi ghép (yyyy-MM-dd HH:mm:ss.fff_GUID) sang mốc kép chuẩn
--              (LastTime datetime + LastKey nvarchar) cho gói tin 101; đảm bảo không bị gửi lặp lại toàn bộ dữ liệu.
-- THỨ TỰ CHẠY: 7 (sau khi cập nhật câu truy vấn mẫu gói 101).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 28/10/2026
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

PRINT N'-> Bắt đầu chuẩn hóa mốc LastTime và LastKey cho gói 101 trong ShareDataLastSend...';

-- 1. Với các dòng mốc cũ có chuỗi dạng 'YYYY-MM-DD HH:MM:SS.FFF_GUID' mà LastTime đang NULL:
--    Tách phần ngày giờ đưa vào cột LastTime và phần GUID đưa vào cột LastKey.
UPDATE [dbo].[ShareDataLastSend]
SET [LastTime] = TRY_CONVERT(DATETIME, LEFT([LastKey], 23), 121),
    [LastKey] = SUBSTRING([LastKey], 25, 64),
    [UpdateTime] = GETDATE()
WHERE ([PacketCode] = '101' OR [PacketCode] LIKE '101[_]%')
  AND [LastTime] IS NULL
  AND [LastKey] IS NOT NULL
  AND CHARINDEX('_', [LastKey]) = 24
  AND TRY_CONVERT(DATETIME, LEFT([LastKey], 23), 121) IS NOT NULL;

DECLARE @MigratedCount INT = @@ROWCOUNT;
PRINT N'   - Đã tách chuỗi ghép cũ thành LastTime + LastKey cho ' + CAST(@MigratedCount AS NVARCHAR(10)) + N' bản ghi.';

-- 2. Báo cáo trạng thái mốc hiện hành của gói 101
SELECT [PartnerCode], [PacketCode], [LastTime], [LastKey], [UpdateTime]
FROM [dbo].[ShareDataLastSend]
WHERE ([PacketCode] = '101' OR [PacketCode] LIKE '101[_]%')
  AND [IsDelete] IS NULL;

PRINT N'=============================================================================';
PRINT N'HOÀN TẤT KỊCH BẢN sharedata-chuyen-doi-moc-ShareDataLastSend.sql';
PRINT N'=============================================================================';
GO
SET NOEXEC OFF;
GO

