USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-kiem-tai-ShareDataSql.sql
-- BẢNG GHI: [dbo].[ShareDataSql] — tệp chỉ thêm / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Kiểm tải ShareData: đối tác kiểm tải từ số @TuSoDoiTacRieng trở đi (mặc định KT026..KT050) có câu SQL
--              riêng cho mỗi gói tin mà đối tác đó có đăng ký gửi; các đối tác số nhỏ hơn (KT001..KT025) dùng câu mẫu chung.
--              Câu riêng sinh từ câu mẫu chung của gói, mỗi cặp (đối tác × gói) một câu khác nhau, dài ngắn tùy cặp:
--                - thêm 0..4 cột phụ (ktPartner, ktNo, ktReadAt, ktPad dài ngắn khác nhau) ngay sau cột __lastKey
--                  (câu chụp nhanh: ngay sau SELECT);
--                - đổi TOP 100 thành TOP 20 / 50 / 200 / 500 (chỉ câu viết TOP 100 bằng số);
--                - đổi xuống dòng CRLF thành LF;
--                - thêm khối chú thích /* ... */ cuối câu, độ dài tùy cặp.
--              KHÔNG đụng công thức cột mốc, mệnh đề WHERE, ORDER BY và biến @lastKey / @pageSize, nên câu riêng vẫn trả
--              đúng các dòng như câu mẫu chung: đối soát mốc (L2) và số dòng đã gửi (L3) vẫn so được giữa 50 đối tác.
--              Danh sách trường (FieldsJson), bộ biến (VariablesJson) chép từ dòng mẫu chung; cột phụ không có trong ánh xạ
--              nên không được gửi đi, chỉ làm câu đọc nặng hơn.
-- THỨ TỰ CHẠY: K4, chạy sau K1, K2, K3 (cần đăng ký của đối tác kiểm tải để biết gói nào cần câu riêng).
--              DỌN: đặt @XoaDuLieuKiemTai = 1, chạy ĐẦU TIÊN (K4 → K3 → K2 → K1).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Staging (chạy theo yêu cầu kiểm tải). ⛔ Không chạy trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): cặp (gói × đối tác) đã có dòng thì bỏ qua (chỉ mục duy nhất PacketId, PartnerId).
--           Cùng cặp luôn sinh cùng một câu (biến tấu theo CHECKSUM mã đối tác + mã gói), chạy lại không ra câu khác.
-- CẬP NHẬT LẦN CUỐI: 11112026 (tạo mới)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-kiem-tai-ShareDataSql.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataSql]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataSql] chưa tồn tại (chưa chạy CodeFirst)!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ — giữ @TienToMa giống K1.
-- -----------------------------------------------------------------------------------------
DECLARE @TienToMa NVARCHAR(16) = N'KT';
DECLARE @TuSoDoiTacRieng INT = 26;                    -- KT026 trở đi có câu riêng; KT001..KT025 dùng mẫu chung
DECLARE @XoaDuLieuKiemTai BIT = 0;                    -- 1 = xóa câu riêng kiểm tải (chạy đầu tiên khi dọn)

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-KIEM-TAI';
DECLARE @CrLf NVARCHAR(2) = NCHAR(13) + NCHAR(10);
DECLARE @Lf NVARCHAR(1) = NCHAR(10);

-- -----------------------------------------------------------------------------------------
-- 1. DỌN DỮ LIỆU KIỂM TẢI
-- -----------------------------------------------------------------------------------------
IF @XoaDuLieuKiemTai = 1
BEGIN
    DELETE FROM [dbo].[ShareDataSql]
    WHERE [CreateUId] = @SeedUser
      AND [PartnerId] IS NOT NULL;
    PRINT N'-> 1. Đã xóa ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' câu SQL riêng kiểm tải.';
END
ELSE
BEGIN
    -- -------------------------------------------------------------------------------------
    -- 2. SINH CÂU SQL RIÊNG CHO TỪNG CẶP (ĐỐI TÁC KIỂM TẢI × GÓI CÓ ĐĂNG KÝ GỬI)
    -- -------------------------------------------------------------------------------------
    ;WITH [Cap] AS (
        SELECT
            p.[ID] AS [PartnerId],
            p.[Code] AS [PartnerCode],
            CAST(RIGHT(p.[Code], 3) AS INT) AS [So],
            c.[PacketId],
            c.[ExtractSql] AS [Goc],
            c.[FieldsJson],
            c.[VariablesJson],
            c.[DescribedTime],
            c.[TenantId],
            ABS(CHECKSUM(p.[Code] + N'|' + c.[PacketId]) % 1000) AS [H]
        FROM [dbo].[ShareDataPartner] p
        JOIN [dbo].[ShareDataSql] c
          ON c.[PartnerId] IS NULL
         AND c.[IsDelete] IS NULL
         AND LEN(ISNULL(c.[ExtractSql], N'')) > 0
        JOIN [dbo].[ShareDataPacket] pk
          ON pk.[ID] = c.[PacketId]
         AND pk.[IsDelete] IS NULL
        WHERE p.[CreateUId] = @SeedUser
          AND p.[IsDelete] IS NULL
          AND p.[Code] LIKE @TienToMa + N'[0-9][0-9][0-9]'
          AND CAST(RIGHT(p.[Code], 3) AS INT) >= @TuSoDoiTacRieng
          AND EXISTS (
              SELECT 1
              FROM [dbo].[ShareDataSubscription] s
              WHERE s.[PartnerId] = p.[ID]
                AND s.[IsDelete] IS NULL
                AND s.[Direction] = 0
                AND (s.[DatatypeId] = pk.[ID] OR s.[DatatypeId] = pk.[Code])
          )
          -- Chỉ mục duy nhất (PacketId, PartnerId) tính cả dòng đã xóa mềm nên kiểm không lọc IsDelete.
          AND NOT EXISTS (
              SELECT 1
              FROM [dbo].[ShareDataSql] x
              WHERE x.[PacketId] = c.[PacketId]
                AND x.[PartnerId] = p.[ID]
          )
    )
    INSERT INTO [dbo].[ShareDataSql]
        ([ID], [PacketId], [PartnerId], [ExtractSql], [FieldsJson], [VariablesJson], [DescribedTime],
         [TenantId], [CreateTime], [CreateUId])
    SELECT
        LOWER(CONVERT(NVARCHAR(64), NEWID())), k.[PacketId], k.[PartnerId], b5.[Cau], k.[FieldsJson], k.[VariablesJson], k.[DescribedTime],
        k.[TenantId], GETDATE(), @SeedUser
    FROM [Cap] k
    -- b1. Kiểu biến tấu 0..4 và các cột phụ thêm vào
    CROSS APPLY (
        SELECT
            k.[H] % 5 AS [Kieu],
            CASE k.[H] % 5
                WHEN 1 THEN @CrLf + N'    N''' + k.[PartnerCode] + N''' AS ktPartner,'
                WHEN 2 THEN @CrLf + N'    ' + CAST(k.[So] AS NVARCHAR(10)) + N' AS ktNo,'
                WHEN 3 THEN @CrLf + N'    N''' + k.[PartnerCode] + N''' AS ktPartner,'
                          + @CrLf + N'    ' + CAST(k.[So] AS NVARCHAR(10)) + N' AS ktNo,'
                          + @CrLf + N'    GETDATE() AS ktReadAt,'
                          + @CrLf + N'    REPLICATE(N''x'', ' + CAST(10 + k.[H] % 490 AS NVARCHAR(10)) + N') AS ktPad,'
                ELSE N''
            END AS [CotPhu]
    ) b1
    -- b2. Chèn cột phụ: câu nối đuôi chèn ngay sau cột __lastKey, câu chụp nhanh chèn ngay sau SELECT
    CROSS APPLY (
        SELECT CASE
            WHEN b1.[CotPhu] = N'' THEN k.[Goc]
            WHEN CHARINDEX(N'AS __lastKey,', k.[Goc]) > 0
                THEN STUFF(k.[Goc], CHARINDEX(N'AS __lastKey,', k.[Goc]) + LEN(N'AS __lastKey,'), 0, b1.[CotPhu])
            WHEN LEFT(k.[Goc], 8) = N'SELECT' + @CrLf
                THEN STUFF(k.[Goc], 7, 0, b1.[CotPhu])
            ELSE k.[Goc]
        END AS [Cau]
    ) b2
    -- b3. Đổi TOP 100 viết bằng số thành cỡ trang khác
    CROSS APPLY (
        SELECT CASE
            WHEN b1.[Kieu] IN (2, 4) AND LEFT(b2.[Cau], 16) = N'SELECT TOP 100' + @CrLf
                THEN STUFF(b2.[Cau], 12, 3, CAST(CHOOSE(k.[H] % 4 + 1, 20, 50, 200, 500) AS NVARCHAR(10)))
            ELSE b2.[Cau]
        END AS [Cau]
    ) b3
    -- b4. Đổi xuống dòng CRLF thành LF
    CROSS APPLY (
        SELECT CASE WHEN b1.[Kieu] = 4 THEN REPLACE(b3.[Cau], @CrLf, @Lf) ELSE b3.[Cau] END AS [Cau]
    ) b4
    -- b5. Khối chú thích cuối câu, dài ngắn tùy cặp
    CROSS APPLY (
        SELECT b4.[Cau] + @CrLf + N'/* ' + k.[PartnerCode] + N' - câu riêng kiểm tải'
             + CASE WHEN k.[H] % 3 = 0 THEN N'' ELSE N': ' + REPLICATE(N'dữ liệu kiểm tải ', k.[H] % 60) END
             + N' */' AS [Cau]
    ) b5;
    PRINT N'-> 2. Đã thêm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' câu SQL riêng kiểm tải.';
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-kiem-tai-ShareDataSql.sql';
PRINT N'=============================================================================';
GO
