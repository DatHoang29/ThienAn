USE [DEV_ITS10];
GO

-- =========================================================================================
-- Bổ sung cột ExtractSql vào bảng ShareDataPacket để lưu câu truy vấn trích xuất động
-- (chiều Gửi - Outbound) cho SuperAdmin, tách biệt hoàn toàn khỏi ShareDataPacketSql
-- (chuyên biệt cho câu ghi chiều Nhận - Inbound WriteSql).
-- Ngày tạo: 13/10/2026
-- =========================================================================================

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[ShareDataPacket]') 
      AND name = N'ExtractSql'
)
BEGIN
    ALTER TABLE [dbo].[ShareDataPacket]
    ADD [ExtractSql] NVARCHAR(MAX) NULL;

    PRINT N'Đã thêm cột ExtractSql vào bảng ShareDataPacket thành công.';
END
ELSE
BEGIN
    PRINT N'Cột ExtractSql đã tồn tại trong bảng ShareDataPacket, bỏ qua.';
END
GO

