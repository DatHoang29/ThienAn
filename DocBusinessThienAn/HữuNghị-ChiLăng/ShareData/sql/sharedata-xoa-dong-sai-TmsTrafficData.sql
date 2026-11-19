USE [DEV_ITS015_Inbound];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-dong-sai-TmsTrafficData.sql
-- BẢNG GHI: [dbo].[TmsTrafficData] trên CSDL nhận (*_Inbound). Chỉ đọc bảng nguồn [@CsdlNguon].[dbo].[TmsTrafficData].
-- DESCRIPTION: Xóa dòng sai do câu ghi cũ để lại sau khi đã chạy Tệp 2 bản 19/11/2026 và D12:
--              (1) Dòng gói 103 (ID có ở bảng nguồn) lệch nguồn ở Speed, hoặc Length/Height/Width đã có giá trị
--                  mà khác nguồn: do câu ghi 106 cũ (trước 18/11/2026 16:50) đè số đo của xe khác cùng thời điểm.
--              (2) @XoaDong106Trung = 1: dòng gói 106 mồ côi (ID GUID không có ở nguồn, chưa có thiết bị, loại xe,
--                  biển số, hướng) nằm ở khóa (DetectTime, Lane, Location) đã có dòng gói 103, là bản sao của xe
--                  103 mà D12 bỏ qua vì khóa có từ 2 xe 103.
--              Chỉ xóa dòng Worker nhận ghi (CreateUId = @Operator). Dòng gói 103 đã xóa không tự gửi lại vì mốc
--              gửi đã đi qua: bảng nhận sẽ thiếu đúng số dòng đó so với nguồn.
--              ⛔ Chỉ dùng trên staging kiểm tải vòng về (CSDL nguồn và CSDL nhận cùng máy chủ).
-- THỨ TỰ CHẠY: D13 (sau Tệp 2 bản 19/11/2026 và D12, trên CSDL nhận DEV_ITS015_Inbound).
--              Tắt dịch vụ có chiều nhận trước khi chạy. Đặt @ChiXem = 1 để xem trước số dòng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Staging kiểm tải. ⛔ Không chạy trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); lỗi giữa chừng thì hoàn tác toàn bộ.
-- CẬP NHẬT LẦN CUỐI: 19/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-dong-sai-TmsTrafficData.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF DB_NAME() NOT LIKE N'%[_]Inbound'
BEGIN
    PRINT N'⛔ Tệp này chỉ chạy trên CSDL nhận (*_Inbound), đang kết nối ' + DB_NAME() + N' — bỏ qua toàn bộ tệp.';
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'[dbo].[TmsTrafficData]', N'U') IS NULL
BEGIN
    PRINT N'⛔ Chưa có bảng [dbo].[TmsTrafficData] trên CSDL nhận — bỏ qua toàn bộ tệp.';
    SET NOEXEC ON;
END;
GO

IF DB_ID(N'DEV_ITS015') IS NULL OR OBJECT_ID(N'[DEV_ITS015].[dbo].[TmsTrafficData]', N'U') IS NULL
BEGIN
    PRINT N'⛔ Không thấy bảng nguồn [DEV_ITS015].[dbo].[TmsTrafficData] trên cùng máy chủ — bỏ qua toàn bộ tệp.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0;          -- 1 = chỉ thống kê và liệt kê mẫu, không xóa dữ liệu
DECLARE @XoaDong106Trung BIT = 1; -- 1 = xóa thêm dòng gói 106 mồ côi trùng xe gói 103 (nhóm 2)
DECLARE @Operator NVARCHAR(64) = N'SHAREDATA_INBOUND';

IF OBJECT_ID(N'tempdb..#Xoa') IS NOT NULL DROP TABLE #Xoa;
CREATE TABLE #Xoa ([ID] NVARCHAR(64) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY, [Nhom] TINYINT NOT NULL);

-- (1) Dòng gói 103 lệch nguồn
INSERT INTO #Xoa ([ID], [Nhom])
SELECT r.[ID], 1
FROM [dbo].[TmsTrafficData] AS r
INNER JOIN [DEV_ITS015].[dbo].[TmsTrafficData] AS s
        ON s.[ID] COLLATE DATABASE_DEFAULT = r.[ID]
WHERE r.[CreateUId] = @Operator
  AND (ISNULL(r.[Speed], -1) <> ISNULL(s.[Speed], -1)
       OR (r.[Length] IS NOT NULL AND ISNULL(s.[Length], -1) <> r.[Length])
       OR (r.[Height] IS NOT NULL AND ISNULL(s.[Height], -1) <> r.[Height])
       OR (r.[Width]  IS NOT NULL AND ISNULL(s.[Width], -1)  <> r.[Width]));

-- (2) Dòng gói 106 mồ côi nằm ở khóa đã có dòng gói 103
IF @XoaDong106Trung = 1
BEGIN
    INSERT INTO #Xoa ([ID], [Nhom])
    SELECT w.[ID], 2
    FROM [dbo].[TmsTrafficData] AS w
    WHERE w.[CreateUId] = @Operator
      AND w.[IsDelete] IS NULL
      AND TRY_CONVERT(UNIQUEIDENTIFIER, w.[ID]) IS NOT NULL
      AND w.[EquipmentId] IS NULL
      AND w.[Type] IS NULL
      AND w.[LicensePlate] IS NULL
      AND w.[Direction] IS NULL
      AND NOT EXISTS (SELECT 1 FROM [DEV_ITS015].[dbo].[TmsTrafficData] AS s
                      WHERE s.[ID] COLLATE DATABASE_DEFAULT = w.[ID])
      AND EXISTS (SELECT 1
                  FROM [dbo].[TmsTrafficData] AS v
                  INNER JOIN [DEV_ITS015].[dbo].[TmsTrafficData] AS s
                          ON s.[ID] COLLATE DATABASE_DEFAULT = v.[ID]
                  WHERE v.[DetectTime] = w.[DetectTime]
                    AND ISNULL(v.[Lane], N'') = ISNULL(w.[Lane], N'')
                    AND ISNULL(v.[Location], N'') = ISNULL(w.[Location], N''))
      AND NOT EXISTS (SELECT 1 FROM #Xoa AS x WHERE x.[ID] = w.[ID]);
END;

DECLARE @SoNhom1 INT = (SELECT COUNT(1) FROM #Xoa WHERE [Nhom] = 1);
DECLARE @SoNhom2 INT = (SELECT COUNT(1) FROM #Xoa WHERE [Nhom] = 2);
PRINT N'-> (1) Dòng gói 103 lệch nguồn: ' + CAST(@SoNhom1 AS NVARCHAR(12));
PRINT N'-> (2) Dòng gói 106 trùng xe gói 103: ' + CAST(@SoNhom2 AS NVARCHAR(12))
    + CASE WHEN @XoaDong106Trung = 1 THEN N'' ELSE N' (đang tắt @XoaDong106Trung)' END;

IF @ChiXem = 1
BEGIN
    PRINT N'-> [ChiXem] Không xóa. Mẫu 20 dòng mỗi nhóm (nhận ↔ nguồn):';
    SELECT TOP (20) x.[Nhom], r.[ID], r.[DetectTime], r.[Lane],
           r.[Speed] AS [SpeedNhan], s.[Speed] AS [SpeedNguon],
           r.[Length] AS [LengthNhan], s.[Length] AS [LengthNguon]
    FROM #Xoa AS x
    INNER JOIN [dbo].[TmsTrafficData] AS r ON r.[ID] = x.[ID]
    LEFT JOIN [DEV_ITS015].[dbo].[TmsTrafficData] AS s ON s.[ID] COLLATE DATABASE_DEFAULT = r.[ID]
    WHERE x.[Nhom] = 1
    ORDER BY r.[DetectTime];

    SELECT TOP (20) x.[Nhom], r.[ID], r.[DetectTime], r.[Lane], r.[Speed], r.[Length], r.[CreateTime]
    FROM #Xoa AS x
    INNER JOIN [dbo].[TmsTrafficData] AS r ON r.[ID] = x.[ID]
    WHERE x.[Nhom] = 2
    ORDER BY r.[DetectTime];
END
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;

        DELETE t
        FROM [dbo].[TmsTrafficData] AS t
        INNER JOIN #Xoa AS x ON x.[ID] = t.[ID];

        DECLARE @DaXoa INT = @@ROWCOUNT;
        DECLARE @DaChon INT = @SoNhom1 + @SoNhom2;
        IF @DaXoa <> @DaChon
            RAISERROR(N'Số dòng xóa %d lệch số dòng đã chọn %d, hoàn tác.', 16, 1, @DaXoa, @DaChon);

        COMMIT TRAN;
        PRINT N'✅ Đã xóa ' + CAST(@DaXoa AS NVARCHAR(12)) + N' dòng sai trong [dbo].[TmsTrafficData].';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'❌ Lỗi xóa dòng sai TmsTrafficData: %s', 16, 1, @ErrMsg);
    END CATCH;
END;

DROP TABLE IF EXISTS #Xoa;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-dong-sai-TmsTrafficData.sql';
PRINT N'=============================================================================';
GO
