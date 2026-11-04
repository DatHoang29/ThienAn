-- ==============================================================================
-- BẢNG GHI: ShareDataSql
-- THỨ TỰ CHẠY: D2, sau tệp 2 sharedata-seed-goi-tin-ShareDataSql.sql (xem README.md). Chạy lại nhiều lần không sao.
-- MÔ TẢ: Sửa câu SQL riêng của đối tác còn mốc dạng 2 cột cũ (cột __lastTime, biến @lastTime) sang một chuỗi ghép,
--        giống nút Sửa câu trên giao diện:
--          cột mốc  : CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID AS __lastKey
--          WHERE    : cùng biểu thức đó > @lastKey
--          ORDER BY : __lastKey ASC
--        và bỏ biến lastTime khỏi VariablesJson. Cột, điều kiện, biến khác của đối tác giữ nguyên.
-- PHẠM VI: chỉ sửa câu chép từ câu mẫu gói 101 dạng 2 cột (bí danh zs). Không sửa, chỉ in tên, khi:
--          - câu khác khuôn đó;
--          - đối tác đã có mốc gửi của gói (mốc cũ không so được với công thức mới).
--          Hai trường hợp này mở tab đối tác ở màn Cấu hình gói tin, bấm Sửa câu rồi Lưu & phân tích
--          (giao diện hỏi xác nhận và xóa mốc cũ).
-- THỰC HIỆN: 04/11/2026
-- ==============================================================================

IF OBJECT_ID(N'dbo.ShareDataSql', N'U') IS NULL
BEGIN
    PRINT N'[BỎ QUA] Bảng ShareDataSql không tồn tại trên CSDL.';
    SET NOEXEC ON;
END
GO

-- 1. SỬA CÂU RIÊNG CÒN MỐC 2 CỘT
SET NOCOUNT ON;

DECLARE @nl NCHAR(1) = NCHAR(10);
DECLARE @Formula NVARCHAR(200) = N'CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + ''_'' + zs.ID';
DECLARE @OldTimeColumn NVARCHAR(200) = N'    zs.UpdateTime AS __lastTime,' + @nl;
DECLARE @OldKeyColumn NVARCHAR(200) = N'zs.ID AS __lastKey,';
DECLARE @OldWhere NVARCHAR(400) = N'WHERE zs.UpdateTime >= ISNULL(@lastTime, ''17530101'')' + @nl
    + N'  AND (@lastTime IS NULL OR zs.UpdateTime > @lastTime OR zs.ID > @lastKey)';
DECLARE @OldOrder NVARCHAR(200) = N'ORDER BY zs.UpdateTime ASC, zs.ID ASC';

DECLARE @Legacy TABLE (
    [ID] NVARCHAR(128) NOT NULL PRIMARY KEY,
    [Label] NVARCHAR(512) NOT NULL,
    [NewSql] NVARCHAR(MAX) NOT NULL,
    [NewVariables] NVARCHAR(MAX) NULL,
    [CanFix] BIT NOT NULL,
    [HasCursor] BIT NOT NULL
);

-- Câu riêng còn @lastTime / __lastTime; xuống dòng CRLF đổi về LF để so khuôn
INSERT INTO @Legacy ([ID], [Label], [NewSql], [NewVariables], [CanFix], [HasCursor])
SELECT s.[ID],
       COALESCE(pt.[Name], pt.[Code], s.[PartnerId]) + N' - gói ' + COALESCE(pk.[Code], s.[PacketId], N'?'),
       f.[NewSql],
       (SELECT N'[' + STRING_AGG(CAST(j.[value] AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(j.[key] AS INT)) + N']'
          FROM OPENJSON(CASE WHEN ISJSON(s.[VariablesJson]) = 1 THEN s.[VariablesJson] END) j
         WHERE ISNULL(COALESCE(JSON_VALUE(j.[value], '$.Code'), JSON_VALUE(j.[value], '$.code')), N'') <> N'lastTime'),
       CASE WHEN CHARINDEX(@OldTimeColumn, x.[Sql]) > 0
                 AND CHARINDEX(@OldKeyColumn, x.[Sql]) > 0
                 AND CHARINDEX(@OldWhere, x.[Sql]) > 0
                 AND CHARINDEX(@OldOrder, x.[Sql]) > 0
                 AND f.[NewSql] NOT LIKE N'%@lastTime%'
                 AND f.[NewSql] NOT LIKE N'%[_][_]lastTime%'
                 AND EXISTS (SELECT 1
                               FROM OPENJSON(CASE WHEN ISJSON(s.[VariablesJson]) = 1 THEN s.[VariablesJson] END) j
                              WHERE COALESCE(JSON_VALUE(j.[value], '$.Code'), JSON_VALUE(j.[value], '$.code')) = N'lastKey')
            THEN 1 ELSE 0 END,
       CASE WHEN EXISTS (SELECT 1
                           FROM dbo.ShareDataLastSend ls
                          WHERE ls.[PartnerCode] = pt.[Code]
                            AND ls.[PacketCode] = pk.[Code]
                            AND ls.[IsDelete] IS NULL
                            AND ISNULL(ls.[LastKey], N'') <> N'')
            THEN 1 ELSE 0 END
FROM dbo.ShareDataSql s
CROSS APPLY (SELECT REPLACE(s.[ExtractSql], NCHAR(13) + NCHAR(10), NCHAR(10)) AS [Sql]) x
CROSS APPLY (SELECT REPLACE(REPLACE(REPLACE(REPLACE(x.[Sql],
                        @OldTimeColumn, N''),
                        @OldKeyColumn, @Formula + N' AS __lastKey,'),
                        @OldWhere, N'WHERE ' + @Formula + N' > @lastKey'),
                        @OldOrder, N'ORDER BY __lastKey ASC') AS [NewSql]) f
LEFT JOIN dbo.ShareDataPartner pt ON pt.[ID] = s.[PartnerId]
LEFT JOIN dbo.ShareDataPacket pk ON pk.[ID] = s.[PacketId]
WHERE s.[PartnerId] IS NOT NULL
  AND s.[IsDelete] IS NULL
  AND (s.[ExtractSql] LIKE N'%@lastTime%' OR s.[ExtractSql] LIKE N'%[_][_]lastTime%');

UPDATE s
SET s.[ExtractSql] = l.[NewSql],
    s.[VariablesJson] = l.[NewVariables],
    s.[UpdateTime] = GETDATE()
FROM dbo.ShareDataSql s
INNER JOIN @Legacy l ON l.[ID] = s.[ID]
WHERE l.[CanFix] = 1
  AND l.[HasCursor] = 0;

DECLARE @Fixed NVARCHAR(MAX) = (SELECT STRING_AGG([Label], N'; ') FROM @Legacy WHERE [CanFix] = 1 AND [HasCursor] = 0);
DECLARE @WithCursor NVARCHAR(MAX) = (SELECT STRING_AGG([Label], N'; ') FROM @Legacy WHERE [CanFix] = 1 AND [HasCursor] = 1);
DECLARE @OtherShape NVARCHAR(MAX) = (SELECT STRING_AGG([Label], N'; ') FROM @Legacy WHERE [CanFix] = 0);

PRINT N'[ĐÃ SỬA] ' + ISNULL(@Fixed, N'không có câu nào cần sửa.');
IF @WithCursor IS NOT NULL
    PRINT N'[CHƯA SỬA] Đối tác đã có mốc gửi: ' + @WithCursor + N'. Mở tab đối tác, bấm Sửa câu rồi Lưu & phân tích.';
IF @OtherShape IS NOT NULL
    PRINT N'[CHƯA SỬA] Câu khác khuôn câu mẫu gói 101 cũ: ' + @OtherShape + N'. Mở tab đối tác, bấm Sửa câu rồi Lưu & phân tích.';
GO

SET NOEXEC OFF;
PRINT N'=== HOÀN TẤT SỬA CÂU RIÊNG CÒN MỐC 2 CỘT (ShareDataSql) ===';
GO
