USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-chuyen-doi-moc-ShareDataLastSend.sql
-- BẢNG GHI: [dbo].[ShareDataLastSend] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: 1. Gỡ unique index 2 cột cũ [index_ShareDataLastSend_Partner_Packet] trên [dbo].[ShareDataLastSend] nếu còn tồn tại
--                 (nhường chỗ cho unique index 3 cột [index_ShareDataLastSend_Partner_Packet_Key] do CodeFirst tạo).
--              2. Chép không trùng (NOT EXISTS) các dòng nhóm mốc có tên từ bảng cũ [dbo].[ShareDataLastSendKey] (chỉ đọc)
--                 sang [dbo].[ShareDataLastSend] khi bảng cũ còn tồn tại và cột [CursorKey] đã có.
--              3. Đổi mốc 2 cột (LastTime + LastKey) sang một chuỗi ghép (yyyy-MM-dd HH:mm:ss.fff_ID) cho nhóm mặc định ([CursorKey] IS NULL)
--                 của đối tác dùng câu mẫu chung một chuỗi ghép, để không phải gửi lại từ đầu.
-- THỨ TỰ CHẠY: 11 (sau khi CodeFirst đã tạo cột CursorKey và phải sau tệp 2 sharedata-seed-goi-tin-ShareDataSql.sql; tắt Worker trước khi chạy).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 02/11/2026
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

-- 3. ĐỔI MỐC 2 CỘT SANG MỘT CHUỖI GHÉP (IDEMPOTENT)
--    Câu mẫu chung đã đổi sang một chuỗi ghép (tệp sharedata-seed-goi-tin-ShareDataSql.sql) thì mốc cũ LastTime + LastKey không so được với câu mới;
--    chữ ký công thức lại khác nên Worker sẽ bỏ mốc, ghi ESH-1308 và gửi lại từ đầu. Đổi mốc sang yyyy-MM-dd HH:mm:ss.fff_ID để gửi tiếp đúng chỗ cũ,
--    chữ ký về NULL (Worker dùng tiếp, lần ghi mốc kế tiếp tự điền). Chỉ đổi nhóm mặc định của đối tác đang dùng câu mẫu chung: câu riêng của đối tác giữ công thức riêng,
--    gói còn câu 2 cột và câu chỉ khóa thuần (ID AS __lastKey) không ghép thời gian thì không đổi. Dòng đã là chuỗi ghép bị loại bởi TRY_CONVERT nên chạy lại không đổi.
PRINT N'-> 3. Đổi mốc 2 cột sang một chuỗi ghép cho đối tác dùng câu mẫu chung...';

IF COL_LENGTH(N'dbo.ShareDataLastSend', N'CursorKey') IS NULL
BEGIN
    PRINT N'   ⚠️ Cột [CursorKey] chưa có trên [dbo].[ShareDataLastSend] (chạy CodeFirst trước). Bỏ qua bước đổi mốc.';
END
ELSE
BEGIN
    DECLARE @ConvertedCursors INT = 0;
    DECLARE @ConvertCursorSql NVARCHAR(MAX) = N'
    UPDATE ls
    SET ls.[LastKey] = CONVERT(varchar(23), ls.[LastTime], 121) + ''_'' + ls.[LastKey],
        ls.[LastTime] = NULL,
        ls.[CursorSignature] = NULL,
        ls.[UpdateTime] = GETDATE()
    FROM [dbo].[ShareDataLastSend] ls
    JOIN [dbo].[ShareDataPacket] p ON p.[Code] = ls.[PacketCode] AND p.[IsDelete] IS NULL
    JOIN [dbo].[ShareDataSql] common ON common.[PacketId] = p.[ID] AND common.[PartnerId] IS NULL AND common.[IsDelete] IS NULL
    WHERE ls.[CursorKey] IS NULL
      AND ls.[IsDelete] IS NULL
      AND ls.[LastTime] IS NOT NULL
      AND ls.[LastKey] IS NOT NULL
      AND TRY_CONVERT(datetime, LEFT(ls.[LastKey], 23), 121) IS NULL
      AND common.[ExtractSql] LIKE N''%[_][_]lastKey%''
      AND common.[ExtractSql] LIKE N''%CONVERT(varchar(23)%''
      AND common.[ExtractSql] NOT LIKE N''%@lastTime%''
      AND NOT EXISTS (
          SELECT 1
          FROM [dbo].[ShareDataPartner] pa
          JOIN [dbo].[ShareDataSql] own ON own.[PartnerId] = pa.[ID] AND own.[PacketId] = p.[ID] AND own.[IsDelete] IS NULL
          WHERE pa.[Code] = ls.[PartnerCode]
            AND pa.[IsDelete] IS NULL
            AND own.[ExtractSql] IS NOT NULL
            AND LTRIM(RTRIM(own.[ExtractSql])) <> N''''
      );
    SET @Converted = @@ROWCOUNT;';
    EXEC sp_executesql @ConvertCursorSql, N'@Converted INT OUTPUT', @Converted = @ConvertedCursors OUTPUT;
    PRINT N'   - Đã đổi ' + CAST(@ConvertedCursors AS NVARCHAR(10)) + N' dòng mốc sang chuỗi ghép.';
END
GO

PRINT N'=============================================================================';
PRINT N'HOÀN TẤT KỊCH BẢN sharedata-chuyen-doi-moc-ShareDataLastSend.sql';
PRINT N'=============================================================================';
GO
SET NOEXEC OFF;
GO
