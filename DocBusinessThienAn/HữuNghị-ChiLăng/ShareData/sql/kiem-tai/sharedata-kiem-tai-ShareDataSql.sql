USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-kiem-tai-ShareDataSql.sql
-- BẢNG GHI: [dbo].[ShareDataSql] — tệp chỉ thêm / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Kiểm tải ShareData: đối tác kiểm tải từ số @TuSoDoiTacRieng trở đi (mặc định KT005..KT008) có câu SQL
--              riêng cho mỗi gói tin mà đối tác đó có đăng ký gửi; các đối tác số nhỏ hơn (KT001..KT004) dùng câu mẫu chung.
--              Câu riêng sinh từ câu mẫu chung của gói, mỗi cặp (đối tác × gói) một câu khác nhau, dài ngắn tùy cặp:
--                - thêm 0..4 cột phụ (ktPartner, ktNo, ktReadAt, ktPad dài ngắn khác nhau) ngay sau cột __lastKey
--                  (câu chụp nhanh: ngay sau SELECT);
--                - đổi TOP 100 thành TOP 20 / 50 / 200 / 500 (chỉ câu viết TOP 100 bằng số);
--                - đổi dòng cuối phần đuôi __lastKey sang cột nghiệp vụ (101: z.Code, chỉ lấy vùng chưa xóa vì Code chỉ duy nhất trong vùng còn sống; 104: w.RefId,
--                  109: t.LaneId + '_' + t.TransactionId, 107: i.Code + '_' + i.ID, và 103/106 kiểu 1,3: td.Lane + '_' + td.ID)
--                  với ISNULL(..., '') để kiểm thử trường hợp đuôi khác ID theo quy chuẩn 16/11 (câu mẫu còn lại giữ nguyên ID);
--                - đổi xuống dòng CRLF thành LF;
--                - thêm khối chú thích /* ... */ cuối câu, độ dài tùy cặp.
--              Giữ nguyên công thức phần thời gian (112 + 114), lọc > @lastKey, ORDER BY __lastKey ASC và đuôi đảm bảo duy nhất,
--              nên câu riêng vẫn trả đúng các dòng như câu mẫu chung để đối soát mốc và số dòng giữa các đối tác kiểm tải.
--              Danh sách trường (FieldsJson) được dò theo cột thật của từng câu riêng ở bước 2b; bộ biến (VariablesJson) chép từ dòng mẫu chung;
--              cột phụ không có trong ánh xạ nên không được gửi đi, chỉ làm câu đọc nặng hơn.
-- THỨ TỰ CHẠY: K4, chạy sau K1, K2, K3 (cần đăng ký của đối tác kiểm tải để biết gói nào cần câu riêng).
--              DỌN: đặt @XoaDuLieuKiemTai = 1, chạy ĐẦU TIÊN (K4 → K3 → K2 → K1).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Staging (chạy theo yêu cầu kiểm tải). ⛔ Không chạy trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): cặp đã có cùng nội dung thì bỏ qua; nếu công thức/nội dung đổi thì bước 2a cập nhật ExtractSql và đặt lại FieldsJson để bước 2b dò lại.
--           Cùng cặp luôn sinh cùng một câu (biến tấu theo CHECKSUM mã đối tác + mã gói), chạy lại không ra câu khác.
-- CẬP NHẬT LẦN CUỐI: 18/11/2026 (gói 101 đổi đuôi __lastKey từ zs.ZoneId sang z.Code và lọc z.IsDelete IS NULL: TmsZone.Code trùng giữa vùng đang dùng và vùng đã xóa; thêm biến thể đuôi __lastKey khác ID cho 101/104/109/107 và 103/106 kiểu 1,3 + bước 2a cập nhật câu đổi, 2b dò FieldsJson); 17/11/2026 (câu riêng mặc định từ KT005 theo 8 đối tác kiểm tải; nhận cả câu mẫu xuống dòng LF khi chèn cột phụ và đổi TOP 100)
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
DECLARE @TuSoDoiTacRieng INT = 5;                     -- KT005 trở đi có câu riêng; KT001..KT004 dùng mẫu chung
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
    -- 1b. DỌN CÂU SQL RIÊNG DƯ THỪA / TRỐNG CỦA ĐỐI TÁC KIỂM TẢI
    --     (ExtractSql trống hoặc thuộc nhóm số nhỏ hơn @TuSoDoiTacRieng dùng mẫu chung)
    -- -------------------------------------------------------------------------------------
    DELETE s
    FROM [dbo].[ShareDataSql] s
    JOIN [dbo].[ShareDataPartner] p
      ON p.[ID] = s.[PartnerId]
     AND p.[IsDelete] IS NULL
    WHERE s.[PartnerId] IS NOT NULL
      AND p.[Code] LIKE @TienToMa + N'[0-9][0-9][0-9]'
      AND (
          LEN(LTRIM(RTRIM(ISNULL(s.[ExtractSql], N'')))) = 0
          OR TRY_CAST(RIGHT(p.[Code], 3) AS INT) < @TuSoDoiTacRieng
      );
    PRINT N'-> 1b. Đã dọn ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' câu SQL riêng dư thừa/trống của đối tác kiểm tải.';

    -- -------------------------------------------------------------------------------------
    -- 2. SINH CÂU SQL RIÊNG CHO TỪNG CẶP (ĐỐI TÁC KIỂM TẢI × GÓI CÓ ĐĂNG KÝ GỬI)
    -- -------------------------------------------------------------------------------------
    DECLARE @CauRieng TABLE (
        [PacketId]      NVARCHAR(64)  NOT NULL,
        [PartnerId]     NVARCHAR(64)  NOT NULL,
        [ExtractSql]    NVARCHAR(MAX) NOT NULL,
        [VariablesJson] NVARCHAR(MAX) NULL,
        [TenantId]      NVARCHAR(64)  NULL
    );

    ;WITH [Cap] AS (
        SELECT
            p.[ID] AS [PartnerId],
            p.[Code] AS [PartnerCode],
            CAST(RIGHT(p.[Code], 3) AS INT) AS [So],
            c.[PacketId],
            pk.[Code] AS [PacketCode],
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
    )
    INSERT INTO @CauRieng ([PacketId], [PartnerId], [ExtractSql], [VariablesJson], [TenantId])
    SELECT
        k.[PacketId], k.[PartnerId], b6.[Cau], k.[VariablesJson], k.[TenantId]
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
            WHEN LEFT(k.[Goc], 8) = N'SELECT' + @CrLf OR LEFT(k.[Goc], 7) = N'SELECT' + @Lf
                THEN STUFF(k.[Goc], 7, 0, b1.[CotPhu])
            ELSE k.[Goc]
        END AS [Cau]
    ) b2
    -- b3. Đổi TOP 100 viết bằng số thành cỡ trang khác
    CROSS APPLY (
        SELECT CASE
            WHEN b1.[Kieu] IN (2, 4) AND (LEFT(b2.[Cau], 16) = N'SELECT TOP 100' + @CrLf OR LEFT(b2.[Cau], 15) = N'SELECT TOP 100' + @Lf)
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
    ) b5
    -- b6. Đổi phần đuôi __lastKey sang cột nghiệp vụ (101/104/109/107 và 103/106 kiểu 1,3)
    --     để kiểm thử đuôi khác ID theo quy chuẩn 16/11 (bọc ISNULL(..., '') thỏa HasNonNullKeyTail).
    CROSS APPLY (
        SELECT CASE k.[PacketCode]
            WHEN N'101_commonData' THEN
                -- Câu mẫu chung từ 18/11/2026 đã dùng đuôi z.Code và lọc vùng chưa xóa: giữ nguyên, không gắn lọc lần hai.
                CASE WHEN CHARINDEX(N'+ ''_'' + zs.ID', b5.[Cau]) = 0 THEN b5.[Cau]
                     ELSE REPLACE(REPLACE(b5.[Cau], N'+ ''_'' + zs.ID', N'+ ''_'' + ISNULL(z.Code, '''')'),
                                  N'> @lastKey', N'> @lastKey AND z.ID IS NOT NULL AND z.IsDelete IS NULL') END
            WHEN N'104_weatherData' THEN
                REPLACE(b5.[Cau], N'+ ''_'' + w.ID', N'+ ''_'' + ISNULL(w.RefId, '''')')
            WHEN N'109_etcData' THEN
                REPLACE(b5.[Cau], N'+ ''_'' + t.ID', N'+ ''_'' + ISNULL(t.LaneId, '''') + ''_'' + ISNULL(t.TransactionId, '''')')
            WHEN N'107_incidentData' THEN
                REPLACE(b5.[Cau], N'+ ''_'' + i.ID', N'+ ''_'' + ISNULL(i.Code, '''') + ''_'' + i.ID')
            WHEN N'103_vdsData' THEN
                CASE WHEN b1.[Kieu] IN (1, 3)
                     THEN REPLACE(b5.[Cau], N'+ ''_'' + td.ID', N'+ ''_'' + ISNULL(td.Lane, '''') + ''_'' + td.ID')
                     ELSE b5.[Cau] END
            WHEN N'106_wimData' THEN
                CASE WHEN b1.[Kieu] IN (1, 3)
                     THEN REPLACE(b5.[Cau], N'+ ''_'' + td.ID', N'+ ''_'' + ISNULL(td.Lane, '''') + ''_'' + td.ID')
                     ELSE b5.[Cau] END
            ELSE b5.[Cau]
        END AS [Cau]
    ) b6;

    -- 2a. Cập nhật câu riêng kiểm tải đã có nếu nội dung thay đổi; xóa FieldsJson để bước 2b dò lại.
    UPDATE s
    SET s.[ExtractSql]    = c.[ExtractSql],
        s.[VariablesJson] = COALESCE(s.[VariablesJson], c.[VariablesJson]),
        s.[FieldsJson]    = NULL,
        s.[DescribedTime] = NULL,
        s.[UpdateTime]    = GETDATE()
    FROM [dbo].[ShareDataSql] s
    JOIN @CauRieng c
      ON c.[PacketId] = s.[PacketId]
     AND c.[PartnerId] = s.[PartnerId]
    WHERE s.[CreateUId] = @SeedUser
      AND s.[PartnerId] IS NOT NULL
      AND s.[IsDelete] IS NULL
      AND ISNULL(s.[ExtractSql], N'') <> c.[ExtractSql];
    PRINT N'-> 2a. Đã cập nhật ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' câu SQL riêng kiểm tải thay đổi.';

    -- Chỉ mục duy nhất (PacketId, PartnerId) tính cả dòng đã xóa mềm nên kiểm không lọc IsDelete.
    INSERT INTO [dbo].[ShareDataSql]
        ([ID], [PacketId], [PartnerId], [ExtractSql], [FieldsJson], [VariablesJson], [DescribedTime],
         [TenantId], [CreateTime], [CreateUId])
    SELECT
        LOWER(CONVERT(NVARCHAR(64), NEWID())), c.[PacketId], c.[PartnerId], c.[ExtractSql], NULL, c.[VariablesJson], NULL,
        c.[TenantId], GETDATE(), @SeedUser
    FROM @CauRieng c
    WHERE NOT EXISTS (
        SELECT 1
        FROM [dbo].[ShareDataSql] x
        WHERE x.[PacketId] = c.[PacketId]
          AND x.[PartnerId] = c.[PartnerId]
    );
    PRINT N'-> 2. Đã thêm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' câu SQL riêng kiểm tải.';

    -- 2b. Dò cột thật của từng câu riêng kiểm tải (bao gồm các cột phụ ktPartner, ktNo, ktReadAt, ktPad)
    UPDATE s
    SET s.[FieldsJson] = f.[FieldsJson],
        s.[DescribedTime] = GETDATE(),
        s.[UpdateTime] = GETDATE()
    FROM [dbo].[ShareDataSql] s
    CROSS APPLY (
        SELECT CASE
            WHEN ISJSON(s.[VariablesJson]) = 1 THEN (
                SELECT STRING_AGG(
                    CAST(
                        N'@' + v.[Code] + CASE LOWER(ISNULL(v.[DataType], N''))
                            WHEN N'datetime' THEN N' datetime'
                            WHEN N'int' THEN N' int'
                            WHEN N'decimal' THEN N' decimal(38,10)'
                            ELSE N' nvarchar(max)'
                        END
                    AS NVARCHAR(MAX)),
                    N', '
                )
                FROM OPENJSON(s.[VariablesJson]) WITH (
                    [Code] nvarchar(128) '$.Code',
                    [DataType] nvarchar(64) '$.DataType'
                ) v
                WHERE v.[Code] IS NOT NULL
                  AND v.[Code] <> N''
                  AND v.[Code] NOT LIKE N'%[^A-Za-z0-9_]%'
            )
            ELSE NULL
        END AS [Params]
    ) prm
    CROSS APPLY (
        SELECT (
            SELECT
                d.[name] AS [key],
                CASE
                    WHEN LOWER(CASE WHEN CHARINDEX(N'(', d.[system_type_name]) > 0 THEN LEFT(d.[system_type_name], CHARINDEX(N'(', d.[system_type_name]) - 1) ELSE d.[system_type_name] END)
                         IN (N'int', N'bigint', N'smallint', N'tinyint', N'decimal', N'numeric', N'float', N'real', N'money', N'smallmoney') THEN N'number'
                    WHEN LOWER(CASE WHEN CHARINDEX(N'(', d.[system_type_name]) > 0 THEN LEFT(d.[system_type_name], CHARINDEX(N'(', d.[system_type_name]) - 1) ELSE d.[system_type_name] END)
                         IN (N'date', N'datetime', N'datetime2', N'smalldatetime', N'datetimeoffset', N'time') THEN N'datetime'
                    WHEN LOWER(d.[system_type_name]) = N'bit' THEN N'bool'
                    ELSE N'string'
                END AS [type],
                d.[system_type_name] AS [dbType],
                CAST(d.[is_nullable] AS bit) AS [nullable],
                CASE WHEN d.[source_table] IS NOT NULL AND d.[source_column] IS NOT NULL THEN d.[source_table] + N'.' + d.[source_column] ELSE NULL END AS [source],
                CAST(0 AS bit) AS [lost],
                LEFT(NULLIF(LTRIM(RTRIM(CAST(ep.[value] AS nvarchar(512)))), N''), 128) AS [label],
                CAST(0 AS bit) AS [required],
                CAST(0 AS bit) AS [skip],
                CAST(NULL AS nvarchar(128)) AS [group],
                CAST(NULL AS nvarchar(256)) AS [remark],
                CAST(NULL AS nvarchar(32)) AS [typeOverride]
            FROM sys.dm_exec_describe_first_result_set(s.[ExtractSql], ISNULL(prm.[Params], N'@pageSize int, @lastKey nvarchar(max)'), 1) d
            LEFT JOIN sys.extended_properties ep
                   ON ep.[class] = 1
                  AND ep.[name] = N'MS_Description'
                  AND ep.[major_id] = OBJECT_ID(QUOTENAME(ISNULL(d.[source_schema], N'dbo')) + N'.' + QUOTENAME(d.[source_table]))
                  AND ep.[minor_id] = COLUMNPROPERTY(ep.[major_id], d.[source_column], 'ColumnId')
            WHERE ISNULL(d.[is_hidden], 0) = 0
              AND d.[error_number] IS NULL
              AND d.[name] IS NOT NULL
              AND LEFT(d.[name], 2) <> N'__'
            ORDER BY d.[column_ordinal]
            FOR JSON PATH, INCLUDE_NULL_VALUES
        ) AS [FieldsJson]
    ) f
    WHERE s.[CreateUId] = @SeedUser
      AND s.[PartnerId] IS NOT NULL
      AND s.[IsDelete] IS NULL
      AND (s.[FieldsJson] IS NULL OR LTRIM(RTRIM(s.[FieldsJson])) = N'')
      AND f.[FieldsJson] IS NOT NULL;
    PRINT N'-> 2b. Đã dò cột và điền FieldsJson cho ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' câu SQL riêng kiểm tải.';
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-kiem-tai-ShareDataSql.sql';
PRINT N'=============================================================================';
GO
