USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-du-lieu-ShareDataPacket.sql
-- BẢNG GHI: [dbo].[ShareDataPacket] — tệp chỉ XÓA dữ liệu trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Xóa dữ liệu danh mục gói tin ShareDataPacket (bao gồm 11 gói Catalog 101–111 khi @XoaCaGoiCatalog = 1,
--              hoặc chỉ xóa gói tạo thêm / E2E_INBOUND_TEST khi @XoaCaGoiCatalog = 0).
--              ⚠️ LƯU Ý QUAN TRỌNG: Tệp seed 1 (sharedata-seed-goi-tin-ShareDataPacket.sql) đã bị gỡ khỏi thư mục sql/
--                 từ ngày 06/11/2026 (commit a9cf99ec). Nếu xóa sạch cả 11 gói Catalog (@XoaCaGoiCatalog = 1), trước khi
--                 chạy lại tệp 2 (sharedata-seed-goi-tin-ShareDataSql.sql) bắt buộc khôi phục & chạy lại tệp 1 từ git:
--                   git show a9cf99ec^:"DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/sql/sharedata-seed-goi-tin-ShareDataPacket.sql"
--                 Nếu muốn chạy thẳng bước 2 → 16 mà không lấy tệp 1 từ git, đặt @XoaCaGoiCatalog = 0.
-- THỨ TỰ CHẠY: X11 (sau X5, X6, X7). Tắt ShareDataWorker và WebAPI trước khi chạy.
--              Đặt @ChiXem = 1 để xem trước số dòng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL chính chạy lại từ đầu (dev / staging / test). ⛔ Cẩn trọng trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 17/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-du-lieu-ShareDataPacket.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataPacket]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataPacket] chưa tồn tại trên DB ' + DB_NAME() + N' — bỏ qua.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0;            -- 1 = chỉ thống kê số dòng, không xóa dữ liệu
DECLARE @XoaCaGoiCatalog BIT = 1;   -- 1 = xóa sạch mọi gói (nhớ chạy lại bước 1 từ git a9cf99ec^ trước bước 2); 0 = giữ 11 gói Catalog 101–111

DECLARE @SoDong INT = (
    SELECT COUNT(1)
    FROM [dbo].[ShareDataPacket] p
    WHERE @XoaCaGoiCatalog = 1
       OR NOT (
              p.[Code] IN (N'101', N'102', N'103', N'104', N'105', N'106', N'107', N'108', N'109', N'110', N'111')
           OR p.[Code] LIKE N'10[1-9][_]%'
           OR p.[Code] LIKE N'11[01][_]%'
       )
);

PRINT N'-> Số dòng sẽ xóa trong [dbo].[ShareDataPacket] (@XoaCaGoiCatalog = '
    + CAST(@XoaCaGoiCatalog AS NVARCHAR(1)) + N'): ' + CAST(@SoDong AS NVARCHAR(12));

IF @SoDong = 0
    PRINT N'ℹ️ Không có dòng nào cần xóa trong [dbo].[ShareDataPacket].';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ xem trước, không xóa dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        IF @XoaCaGoiCatalog = 1
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [referenced_object_id] = OBJECT_ID(N'[dbo].[ShareDataPacket]'))
                DELETE FROM [dbo].[ShareDataPacket];
            ELSE
                TRUNCATE TABLE [dbo].[ShareDataPacket];
        END
        ELSE
        BEGIN
            DELETE p
            FROM [dbo].[ShareDataPacket] p
            WHERE NOT (
                   p.[Code] IN (N'101', N'102', N'103', N'104', N'105', N'106', N'107', N'108', N'109', N'110', N'111')
                OR p.[Code] LIKE N'10[1-9][_]%'
                OR p.[Code] LIKE N'11[01][_]%'
            );
        END;
        COMMIT TRAN;
        PRINT N'✅ Đã xóa ' + CAST(@SoDong AS NVARCHAR(12)) + N' dòng khỏi [dbo].[ShareDataPacket].';
        IF @XoaCaGoiCatalog = 1
            PRINT N'⚠️ Lưu ý: đã xóa cả gói Catalog 101–111. Hãy lấy & chạy tệp 1 từ git show a9cf99ec^ trước khi chạy tệp 2 (sharedata-seed-goi-tin-ShareDataSql.sql).';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'❌ Lỗi xóa [dbo].[ShareDataPacket]: %s', 16, 1, @ErrMsg);
    END CATCH;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-du-lieu-ShareDataPacket.sql';
PRINT N'=============================================================================';
GO
