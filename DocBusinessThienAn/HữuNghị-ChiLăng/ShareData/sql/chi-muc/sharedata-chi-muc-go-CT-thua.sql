USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-chi-muc-go-CT-thua.sql
-- BẢNG GHI: chỉ đổi CẤU TRÚC (gỡ chỉ mục), không ghi dữ liệu, trên 5 bảng:
--             [dbo].[ShareDataActivityLog]   index_ShareDataActivityLog_CT
--             [dbo].[ShareDataAlertLog]      index_ShareDataAlertLog_CT
--             [dbo].[ShareDataPartner]       index_ShareDataPartner_CT
--             [dbo].[ShareDataSession]       index_ShareDataSession_CT
--             [dbo].[ShareDataSubscription]  index_ShareDataSubscription_CT
--           Ngoại lệ Rule 19.103 (một tệp nhiều bảng): cùng một việc gỡ chỉ mục sót, chạy một lần.
-- DESCRIPTION: 5 chỉ mục (CreateTime) sót lại từ bản Entity cũ: Entity hiện tại không khai [SugarIndex] này nên CodeFirst
--              không tạo lại, và không câu truy vấn nào đang chạy cần chúng (rà staging 12/11/2026):
--                - ActivityLog: trùng cột CreateTime với index_ShareDataActivityLog_OA (Entity khai _OA trên CreateTime);
--                  câu dọn nhật ký (CreateTime < cutoff) vẫn dùng _OA.
--                  CHẶN AN TOÀN: chỉ gỡ _CT khi _OA còn nằm trên CreateTime; nếu _OA đã bị chuyển sang OccurredAt
--                  (lệch Entity, ví dụ do script cũ) thì GIỮ _CT để câu dọn không quét cả bảng.
--                - AlertLog: màn hình lọc OccurredAt (đã có _OA); câu dọn theo CreateTime đang để chú thích, không chạy.
--                - Partner, Session, Subscription: không câu nào lọc / sắp theo CreateTime trên chỉ mục; bảng vài chục đến
--                  vài trăm dòng.
-- THỨ TỰ CHẠY: Chạy độc lập. HOÀN TÁC: đặt @HoanTac = 1 rồi chạy lại (dựng lại 5 chỉ mục trên CreateTime).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi CSDL vận hành có các bảng ShareData (staging: DEV_ITS015).
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): không có chỉ mục thì bỏ qua; bảng chưa tồn tại thì bỏ qua.
--           Mỗi lệnh chờ khóa tối đa @ChoKhoaMs rồi báo lỗi 1222; khi đó chạy lại tệp (bảng đã xong được bỏ qua).
-- CẬP NHẬT LẦN CUỐI: 12112026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-chi-muc-go-CT-thua.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @HoanTac BIT = 0;                             -- 1 = dựng lại 5 chỉ mục đã gỡ
DECLARE @ChoKhoaMs INT = 5000;                        -- chờ khóa tối đa (mili giây)

DECLARE @TrucTuyen BIT = CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS INT) IN (3, 5, 8) THEN 1 ELSE 0 END;
DECLARE @DanhSach TABLE ([Bang] SYSNAME, [ChiMuc] SYSNAME);
INSERT INTO @DanhSach ([Bang], [ChiMuc]) VALUES
    (N'ShareDataActivityLog',  N'index_ShareDataActivityLog_CT'),
    (N'ShareDataAlertLog',     N'index_ShareDataAlertLog_CT'),
    (N'ShareDataPartner',      N'index_ShareDataPartner_CT'),
    (N'ShareDataSession',      N'index_ShareDataSession_CT'),
    (N'ShareDataSubscription', N'index_ShareDataSubscription_CT');

-- _OA của ActivityLog còn nằm trên CreateTime thì câu dọn nhật ký vẫn có chỉ mục khi gỡ _CT
DECLARE @OaTrenCreateTime BIT = CASE WHEN EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.index_columns ic ON ic.[object_id] = i.[object_id] AND ic.[index_id] = i.[index_id] AND ic.[key_ordinal] = 1
    JOIN sys.columns c ON c.[object_id] = ic.[object_id] AND c.[column_id] = ic.[column_id]
    WHERE i.[object_id] = OBJECT_ID(N'[dbo].[ShareDataActivityLog]')
      AND i.[name] = N'index_ShareDataActivityLog_OA'
      AND c.[name] = N'CreateTime'
) THEN 1 ELSE 0 END;

DECLARE @Bang SYSNAME, @ChiMuc SYSNAME, @CoChiMuc BIT, @Sql NVARCHAR(MAX);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT [Bang], [ChiMuc] FROM @DanhSach;
OPEN cur;
FETCH NEXT FROM cur INTO @Bang, @ChiMuc;
WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        IF OBJECT_ID(N'[dbo].' + QUOTENAME(@Bang), N'U') IS NULL
            PRINT N'⚠️ ' + @Bang + N': bảng chưa tồn tại, bỏ qua.';
        ELSE
        BEGIN
            SET @CoChiMuc = CASE WHEN EXISTS (SELECT 1 FROM sys.indexes
                                              WHERE [object_id] = OBJECT_ID(N'[dbo].' + QUOTENAME(@Bang)) AND [name] = @ChiMuc)
                                 THEN 1 ELSE 0 END;

            IF @HoanTac = 0
            BEGIN
                IF @CoChiMuc = 0
                    PRINT N'-> ' + @Bang + N': không có ' + @ChiMuc + N', bỏ qua.';
                ELSE IF @Bang = N'ShareDataActivityLog' AND @OaTrenCreateTime = 0
                    PRINT N'⚠️ ' + @Bang + N': GIỮ ' + @ChiMuc + N' vì index_ShareDataActivityLog_OA không còn nằm trên CreateTime (câu dọn nhật ký cần chỉ mục CreateTime).';
                ELSE
                BEGIN
                    -- Gỡ: chỉ mục sót, Entity không khai, không câu nào dùng
                    SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
DROP INDEX ' + QUOTENAME(@ChiMuc) + N' ON [dbo].' + QUOTENAME(@Bang) + N';';
                    EXEC sys.sp_executesql @Sql;
                    PRINT N'-> ' + @Bang + N': đã gỡ ' + @ChiMuc + N'.';
                END;
            END
            ELSE
            BEGIN
                IF @CoChiMuc = 1
                    PRINT N'-> ' + @Bang + N': đã có ' + @ChiMuc + N', bỏ qua.';
                ELSE
                BEGIN
                    -- Hoàn tác: dựng lại chỉ mục trên CreateTime như trước khi gỡ
                    SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
CREATE NONCLUSTERED INDEX ' + QUOTENAME(@ChiMuc) + N' ON [dbo].' + QUOTENAME(@Bang) + N' ([CreateTime] ASC)'
                        + CASE WHEN @TrucTuyen = 1 THEN N' WITH (ONLINE = ON)' ELSE N'' END + N';';
                    EXEC sys.sp_executesql @Sql;
                    PRINT N'-> ' + @Bang + N': đã dựng lại ' + @ChiMuc + N'.';
                END;
            END;
        END;
    END TRY
    BEGIN CATCH
        PRINT N'❌ ' + @Bang + N': lỗi ' + CAST(ERROR_NUMBER() AS NVARCHAR(12)) + N' — ' + ERROR_MESSAGE();
        IF ERROR_NUMBER() = 1222
            PRINT N'   Bảng đang bị câu khác giữ khóa quá ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N' ms. Chạy lại tệp.';
    END CATCH;

    FETCH NEXT FROM cur INTO @Bang, @ChiMuc;
END;
CLOSE cur;
DEALLOCATE cur;

-- -----------------------------------------------------------------------------------------
-- KẾT QUẢ: chỉ mục còn lại của 5 bảng
-- -----------------------------------------------------------------------------------------
SELECT OBJECT_NAME(i.[object_id]) AS [Bang], i.[name] AS [ChiMuc], i.[type_desc] AS [Loai]
FROM sys.indexes i
WHERE i.[object_id] IN (OBJECT_ID(N'[dbo].[ShareDataActivityLog]'), OBJECT_ID(N'[dbo].[ShareDataAlertLog]'),
                        OBJECT_ID(N'[dbo].[ShareDataPartner]'), OBJECT_ID(N'[dbo].[ShareDataSession]'),
                        OBJECT_ID(N'[dbo].[ShareDataSubscription]'))
  AND i.[index_id] > 0
ORDER BY [Bang], i.[index_id];
GO

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-chi-muc-go-CT-thua.sql';
PRINT N'=============================================================================';
GO
