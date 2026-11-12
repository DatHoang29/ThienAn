USE [DEV_ITS015_Inbound];
GO

-- =========================================================================================
-- FILE: sharedata-chi-muc-ShareDataInboundPacket.sql
-- BẢNG GHI: [dbo].[ShareDataInboundPacket] (trên CSDL nhận *_Inbound, ConfigId = ShareDataDB) — chỉ đổi CẤU TRÚC
--           (thêm / gỡ chỉ mục), không ghi dữ liệu (Rule 19.103).
-- DESCRIPTION: Khi WebAPI nhận gói tin (InboundCommandHandler), hệ thống kiểm tra trùng số thứ tự gói bằng câu:
--                  SELECT TOP 1 1 FROM [ShareDataInboundPacket]
--                  WHERE [PartnerCode] = @p0 AND [PacketCode] = @p1 AND [SerialNbr] = @p2 AND [IsDelete] IS NULL
--              Chỉ mục cũ [index_ShareDataInboundPacket_State_Partner_Packet] đặt cột [ProcessState] ở đầu nên câu
--              kiểm tra trùng SerialNbr không Seek được mà phải quét toàn bộ bảng (staging 12/11/2026: 381 lượt/phút,
--              đọc 3.552 trang/lượt, trung bình 294 ms/lượt).
--              Tệp này thêm chỉ mục [index_ShareDataInboundPacket_Partner_Packet_Serial] (cùng tên với [SugarIndex]
--              mới trên Entity ShareDataInboundPacket):
--                  khóa ([PartnerCode], [PacketCode], [SerialNbr]), INCLUDE ([IsDelete])
--              để câu kiểm tra trùng SerialNbr đi thẳng vào Index Seek (đọc 3 trang thay vì 3.552 trang).
-- THỨ TỰ CHẠY: Chạy độc lập trên CSDL nhận chứa bảng ShareDataInboundPacket (staging: DEV_ITS015_Inbound).
--              GỠ: đặt @GoChiMuc = 1 rồi chạy lại.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL nhận *_Inbound (ConfigId = ShareDataDB) chứa bảng ShareDataInboundPacket.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): đã có chỉ mục thì bỏ qua; gỡ khi không có thì bỏ qua.
-- CẬP NHẬT LẦN CUỐI: 12112026 (tạo mới)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-chi-muc-ShareDataInboundPacket.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataInboundPacket]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataInboundPacket] chưa tồn tại trên DB ' + DB_NAME() + N'!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @GoChiMuc BIT = 0;                            -- 1 = gỡ chỉ mục
DECLARE @ChoKhoaMs INT = 5000;                        -- chờ khóa tối đa (mili giây)

DECLARE @TenChiMuc SYSNAME = N'index_ShareDataInboundPacket_Partner_Packet_Serial';
DECLARE @TrucTuyen BIT = CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS INT) IN (3, 5, 8) THEN 1 ELSE 0 END;
DECLARE @CoChiMuc BIT = CASE WHEN EXISTS (SELECT 1 FROM sys.indexes
                                          WHERE [object_id] = OBJECT_ID(N'[dbo].[ShareDataInboundPacket]') AND [name] = @TenChiMuc)
                             THEN 1 ELSE 0 END;
DECLARE @Sql NVARCHAR(MAX);

BEGIN TRY
    IF @GoChiMuc = 1
    BEGIN
        IF @CoChiMuc = 1
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
DROP INDEX ' + QUOTENAME(@TenChiMuc) + N' ON [dbo].[ShareDataInboundPacket];';
            EXEC sys.sp_executesql @Sql;
            PRINT N'-> Đã gỡ chỉ mục ' + @TenChiMuc + N'.';
        END
        ELSE
            PRINT N'-> Không có chỉ mục ' + @TenChiMuc + N', bỏ qua.';
    END
    ELSE IF @CoChiMuc = 1
        PRINT N'-> Chỉ mục ' + @TenChiMuc + N' đã có, bỏ qua.';
    ELSE
    BEGIN
        IF @TrucTuyen = 0
            PRINT N'⚠️ Bản SQL Server này không dựng chỉ mục trực tuyến: bảng bị chặn GHI trong lúc dựng chỉ mục.';

        SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
CREATE NONCLUSTERED INDEX ' + QUOTENAME(@TenChiMuc) + N'
    ON [dbo].[ShareDataInboundPacket] ([PartnerCode] ASC, [PacketCode] ASC, [SerialNbr] ASC)
    INCLUDE ([IsDelete])'
            + CASE WHEN @TrucTuyen = 1 THEN N' WITH (ONLINE = ON)' ELSE N'' END + N';';
        EXEC sys.sp_executesql @Sql;
        PRINT N'-> Đã tạo chỉ mục ' + @TenChiMuc + N'.';
    END;
END TRY
BEGIN CATCH
    PRINT N'❌ Lỗi ' + CAST(ERROR_NUMBER() AS NVARCHAR(12)) + N' — ' + ERROR_MESSAGE();
    IF ERROR_NUMBER() = 1222
        PRINT N'   Bảng đang bị câu khác giữ khóa quá ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N' ms. Dừng ShareDataWorker rồi chạy lại tệp.';
END CATCH;

-- -----------------------------------------------------------------------------------------
-- KẾT QUẢ
-- -----------------------------------------------------------------------------------------
SELECT i.[name] AS [ChiMuc], i.[type_desc] AS [Loai],
       (SELECT SUM(p.[rows]) FROM sys.partitions p WHERE p.[object_id] = i.[object_id] AND p.[index_id] = i.[index_id]) AS [SoDong]
FROM sys.indexes i
WHERE i.[object_id] = OBJECT_ID(N'[dbo].[ShareDataInboundPacket]')
ORDER BY i.[index_id];
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-chi-muc-ShareDataInboundPacket.sql';
PRINT N'=============================================================================';
GO
