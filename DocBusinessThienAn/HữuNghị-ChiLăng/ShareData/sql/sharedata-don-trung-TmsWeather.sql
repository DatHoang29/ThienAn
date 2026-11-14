USE [DEV_ITS015_Inbound];
GO

-- =========================================================================================
-- FILE: sharedata-don-trung-TmsWeather.sql
-- BẢNG GHI: [dbo].[TmsWeather] trên CSDL nhận (*_Inbound) — chỉ XÓA dòng trùng do Worker nhận ghi; không bảng nào khác (Rule 19.103).
-- DESCRIPTION: Dọn dòng trùng gói 104 (thời tiết) mà câu ghi mẫu trước 13/11/2026 để lại.
--              Câu cũ so TimeDetect (datetime) với thời gian JSON (datetime2): mili-giây lẻ không bao giờ khớp nên mỗi lần cùng bản ghi
--              tới lại thì chèn thêm một dòng. Sau D10 (sharedata-sua-cau-ghi-thoi-gian-ShareDataSql.sql) không sinh trùng mới nữa,
--              nhưng câu mới gặp nhóm trùng cũ sẽ cập nhật cả nhóm, nên cần dọn để mỗi trạm, mỗi thời điểm còn một dòng.
--              Nhóm trùng: cùng RefId (mã trạm) và TimeDetect, đúng khóa so khớp của câu ghi gói 104.
--              Chỉ xét dòng Worker nhận ghi (CreateUId = SHAREDATA_INBOUND) và chưa xóa mềm; dòng do nơi khác ghi giữ nguyên.
--              Mỗi nhóm giữ dòng ghi sau cùng (UpdateTime, không có thì CreateTime), xóa hẳn các dòng còn lại.
-- THỨ TỰ CHẠY: Sau D10 trên CSDL chính (để không sinh trùng mới); chạy trên CSDL nhận, chọn đúng CSDL *_Inbound trước khi chạy.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi CSDL nhận *_Inbound (dòng USE đầu tệp trỏ DEV_ITS015_Inbound; môi trường khác đổi tên CSDL). Chạy trên CSDL không có hậu tố _Inbound thì tệp tự bỏ qua.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): hết trùng thì không xóa gì. Muốn xem trước: đặt @ChiXem = 1.
-- CẬP NHẬT LẦN CUỐI: 14/11/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-don-trung-TmsWeather.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF DB_NAME() NOT LIKE N'%[_]Inbound'
   OR OBJECT_ID(N'[dbo].[TmsWeather]', N'U') IS NULL
BEGIN
    PRINT N'⛔ Tệp này chỉ chạy trên CSDL nhận (*_Inbound) có bảng TmsWeather, đang kết nối ' + DB_NAME() + N'. Bỏ qua toàn bộ tệp.';
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
    SELECT w.[ID], w.[RefId], w.[TimeDetect],
           ROW_NUMBER() OVER (PARTITION BY w.[RefId], w.[TimeDetect]
                              ORDER BY ISNULL(w.[UpdateTime], w.[CreateTime]) DESC, w.[CreateTime] DESC, w.[ID] DESC) AS [ThuTu]
    FROM [dbo].[TmsWeather] w
    WHERE w.[CreateUId] = @Operator
      AND w.[IsDelete] IS NULL
      AND w.[RefId] IS NOT NULL
      AND w.[TimeDetect] IS NOT NULL
)
SELECT [ID], [RefId], [TimeDetect]
INTO #Xoa
FROM [Nhom]
WHERE [ThuTu] > 1;

-- Hiện trạng: các nhóm trùng nhiều nhất và tổng số dòng sẽ xóa
SELECT TOP (50) [RefId] AS [MaTram], [TimeDetect] AS [ThoiDiem], COUNT(*) AS [SoDongSeXoa]
FROM #Xoa
GROUP BY [RefId], [TimeDetect]
ORDER BY COUNT(*) DESC, [RefId], [TimeDetect];

DECLARE @SoNhom INT = (SELECT COUNT(*) FROM (SELECT DISTINCT [RefId], [TimeDetect] FROM #Xoa) n);
DECLARE @SoDong INT = (SELECT COUNT(*) FROM #Xoa);
PRINT N'   - Nhóm trùng: ' + CAST(@SoNhom AS NVARCHAR(20)) + N', dòng thừa: ' + CAST(@SoDong AS NVARCHAR(20));

IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ liệt kê, không xóa dữ liệu.';
ELSE
BEGIN
    DELETE w
    FROM [dbo].[TmsWeather] w
    JOIN #Xoa x
        ON x.[ID] = w.[ID];

    PRINT N'   - Đã xóa ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng trùng.';
END;

DROP TABLE #Xoa;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-don-trung-TmsWeather.sql';
PRINT N'=============================================================================';
GO
