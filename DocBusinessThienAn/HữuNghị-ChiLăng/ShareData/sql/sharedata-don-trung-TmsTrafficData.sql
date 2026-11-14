USE [DEV_ITS015_Inbound];
GO

-- =========================================================================================
-- FILE: sharedata-don-trung-TmsTrafficData.sql
-- BẢNG GHI: [dbo].[TmsTrafficData] trên CSDL nhận (*_Inbound) — chỉ XÓA dòng trùng gói 106 do Worker nhận ghi; không bảng nào khác (Rule 19.103).
-- DESCRIPTION: Dọn dòng trùng gói 106 (cân động WIM) mà câu ghi mẫu trước 13/11/2026 để lại.
--              Câu cũ so DetectTime (datetime) với thời gian JSON (datetime2): mili-giây lẻ không bao giờ khớp nên mỗi lần cùng bản ghi
--              tới lại thì chèn thêm một dòng. Sau D10 (sharedata-sua-cau-ghi-thoi-gian-ShareDataSql.sql) không sinh trùng mới nữa,
--              nhưng câu mới gặp nhóm trùng cũ sẽ cập nhật cả nhóm, nên cần dọn để mỗi thời điểm, làn, vị trí còn một dòng.
--              Nhóm trùng: cùng DetectTime, Lane, Location (trống coi như nhau), đúng khóa so khớp của câu ghi gói 106.
--              Chỉ xét dòng gói 106 chèn: Worker nhận ghi (CreateUId = SHAREDATA_INBOUND), chưa xóa mềm, ID là GUID do câu ghi sinh,
--              không có biển số, loại xe, hướng, thiết bị (các cột gói 103 mới ghi). Dòng gói 103 và dòng do nơi khác ghi giữ nguyên.
--              Mỗi nhóm giữ dòng ghi sau cùng (UpdateTime, không có thì CreateTime), xóa hẳn các dòng còn lại.
-- THỨ TỰ CHẠY: Sau D10 trên CSDL chính (để không sinh trùng mới); chạy trên CSDL nhận, chọn đúng CSDL *_Inbound trước khi chạy.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi CSDL nhận *_Inbound (dòng USE đầu tệp trỏ DEV_ITS015_Inbound; môi trường khác đổi tên CSDL). Chạy trên CSDL không có hậu tố _Inbound thì tệp tự bỏ qua.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): hết trùng thì không xóa gì. Muốn xem trước: đặt @ChiXem = 1.
-- CẬP NHẬT LẦN CUỐI: 14/11/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-don-trung-TmsTrafficData.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF DB_NAME() NOT LIKE N'%[_]Inbound'
   OR OBJECT_ID(N'[dbo].[TmsTrafficData]', N'U') IS NULL
BEGIN
    PRINT N'⛔ Tệp này chỉ chạy trên CSDL nhận (*_Inbound) có bảng TmsTrafficData, đang kết nối ' + DB_NAME() + N'. Bỏ qua toàn bộ tệp.';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @ChiXem BIT = 0;                                  -- 1 = chỉ liệt kê, không xóa
DECLARE @Operator NVARCHAR(64) = N'SHAREDATA_INBOUND';    -- người ghi của Worker nhận

IF OBJECT_ID(N'tempdb..#Xoa') IS NOT NULL
    DROP TABLE #Xoa;

WITH [Nhom] AS
(
    SELECT t.[ID], t.[DetectTime], t.[Lane], t.[Location],
           ROW_NUMBER() OVER (PARTITION BY t.[DetectTime], ISNULL(t.[Lane], N''), ISNULL(t.[Location], N'')
                              ORDER BY ISNULL(t.[UpdateTime], t.[CreateTime]) DESC, t.[CreateTime] DESC, t.[ID] DESC) AS [ThuTu]
    FROM [dbo].[TmsTrafficData] t
    WHERE t.[CreateUId] = @Operator
      AND t.[IsDelete] IS NULL
      AND t.[DetectTime] IS NOT NULL
      AND TRY_CONVERT(uniqueidentifier, t.[ID]) IS NOT NULL
      AND t.[LicensePlate] IS NULL
      AND t.[Type] IS NULL
      AND t.[Direction] IS NULL
      AND t.[EquipmentId] IS NULL
)
SELECT [ID], [DetectTime], [Lane], [Location]
INTO #Xoa
FROM [Nhom]
WHERE [ThuTu] > 1;

-- Hiện trạng: các nhóm trùng nhiều nhất và tổng số dòng sẽ xóa
SELECT TOP (50) [DetectTime] AS [ThoiDiem], [Lane] AS [Lan], [Location] AS [ViTri], COUNT(*) AS [SoDongSeXoa]
FROM #Xoa
GROUP BY [DetectTime], [Lane], [Location]
ORDER BY COUNT(*) DESC, [DetectTime];

DECLARE @SoNhom INT = (SELECT COUNT(*) FROM (SELECT DISTINCT [DetectTime], [Lane], [Location] FROM #Xoa) n);
DECLARE @SoDong INT = (SELECT COUNT(*) FROM #Xoa);
PRINT N'   - Nhóm trùng: ' + CAST(@SoNhom AS NVARCHAR(20)) + N', dòng thừa: ' + CAST(@SoDong AS NVARCHAR(20));

IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ liệt kê, không xóa dữ liệu.';
ELSE
BEGIN
    DELETE t
    FROM [dbo].[TmsTrafficData] t
    JOIN #Xoa x
        ON x.[ID] = t.[ID];

    PRINT N'   - Đã xóa ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng trùng.';
END;

DROP TABLE #Xoa;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-don-trung-TmsTrafficData.sql';
PRINT N'=============================================================================';
GO
