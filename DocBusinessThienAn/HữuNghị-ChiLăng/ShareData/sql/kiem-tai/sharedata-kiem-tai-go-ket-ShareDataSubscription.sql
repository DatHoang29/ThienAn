USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-kiem-tai-go-ket-ShareDataSubscription.sql
-- BẢNG GHI: [dbo].[ShareDataSubscription] — chỉ SỬA cột SerialNbr (kèm UpdateTime) của đăng ký gửi; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Gỡ các luồng gửi bị kẹt vĩnh viễn trên Staging. Khi WebAPI khởi động lại đúng lúc vừa lưu gói (12/11/2026),
--              bên nhận đã có gói số N nhưng bên gửi không nhận được trả lời nên giữ SerialNbr = N; từ đó lượt nào cũng gửi lại N
--              và bị từ chối "đã tồn tại". ShareDataWorker từ 13/11/2026 tự tăng số khi gửi mà mất trả lời, nhưng luồng đã kẹt
--              từ trước chỉ còn nhận lời từ chối nên Worker không tự gỡ được.
--              Tệp đặt SerialNbr của đăng ký gửi = (số lớn nhất bên nhận đã lưu cho cùng mã đối tác × mã gói) + 1.
--              Không đổi mốc: lượt sau gửi lại đúng trang đang kẹt với số mới, bên nhận có thể ghi lại các dòng của trang đó một lần nữa.
--              Chỉ sửa đăng ký gửi chưa xóa (mọi trạng thái, kể cả của đối tác đang tạm dừng) có SerialNbr <= số lớn nhất bên nhận
--              và không có lượt nào đang giữ khóa (ProcessingUntil trống hoặc đã hết hạn).
-- THỨ TỰ CHẠY: K7, sau khi triển khai ShareDataWorker bản 13/11/2026 (bản cũ còn chạy thì luồng có thể kẹt lại).
--              Đăng ký đang giữ khóa lúc chạy được bỏ qua: chạy lại tới khi báo 0 đăng ký kẹt. Muốn xem trước: đặt @ChiXem = 1.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Staging, nơi WebAPI giả lập bên nhận và CSDL nhận (@CsdlNhan) nằm cùng máy chủ SQL.
--             ⛔ Không chạy trên production: bên nhận thật là hệ thống của đối tác, không đọc được số đã nhận.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): đăng ký đã gỡ có SerialNbr lớn hơn số bên nhận nên không bị sửa nữa.
-- CẬP NHẬT LẦN CUỐI: 13112026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-kiem-tai-go-ket-ShareDataSubscription.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataSubscription]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[ShareDataPartner]', N'U') IS NULL
   OR OBJECT_ID(N'[dbo].[ShareDataPacket]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Thiếu bảng ShareDataSubscription / ShareDataPartner / ShareDataPacket trên DB ' + DB_NAME() + N'!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @CsdlNhan SYSNAME = N'DEV_ITS015_Inbound';    -- CSDL nhận, nơi WebAPI lưu gói (ConfigId ShareDataDB)
DECLARE @ChiXem BIT = 0;                              -- 1 = chỉ liệt kê, không ghi

IF DB_ID(@CsdlNhan) IS NULL
BEGIN
    PRINT N'⚠️ Không thấy CSDL nhận ' + QUOTENAME(@CsdlNhan) + N' trên máy chủ này, không làm gì.';
    RETURN;
END;

-- Số lớn nhất bên nhận đã lưu cho từng mã đối tác × mã gói. Gói gửi mang header PartnerCode = mã đối tác, PacketCode = mã gói.
IF OBJECT_ID('tempdb..#SoBenNhan') IS NOT NULL DROP TABLE #SoBenNhan;
CREATE TABLE #SoBenNhan (
    [PartnerCode] NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
    [PacketCode] NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
    [MaxSerial] INT NOT NULL
);

DECLARE @Sql NVARCHAR(MAX) = N'
INSERT INTO #SoBenNhan ([PartnerCode], [PacketCode], [MaxSerial])
SELECT ip.[PartnerCode], ip.[PacketCode], MAX(ip.[SerialNbr])
FROM ' + QUOTENAME(@CsdlNhan) + N'.[dbo].[ShareDataInboundPacket] ip
WHERE ip.[PartnerCode] IS NOT NULL
  AND ip.[PacketCode] IS NOT NULL
  AND ip.[SerialNbr] IS NOT NULL
GROUP BY ip.[PartnerCode], ip.[PacketCode];';
EXEC sys.sp_executesql @Sql;

-- Đăng ký gửi đang giữ số mà bên nhận đã có
IF OBJECT_ID('tempdb..#LuongKet') IS NOT NULL DROP TABLE #LuongKet;
SELECT s.[ID],
       p.[Code] AS [MaDoiTac],
       k.[Code] AS [MaGoi],
       s.[State] AS [TrangThai],
       s.[SerialNbr] AS [SoDangGiu],
       r.[MaxSerial] AS [SoLonNhatBenNhan],
       r.[MaxSerial] + 1 AS [SoMoi]
INTO #LuongKet
FROM [dbo].[ShareDataSubscription] s
INNER JOIN [dbo].[ShareDataPartner] p
    ON p.[ID] = s.[PartnerId]
   AND p.[IsDelete] IS NULL
INNER JOIN [dbo].[ShareDataPacket] k
    ON (k.[ID] = s.[DatatypeId] OR k.[Code] = s.[DatatypeId])
   AND k.[IsDelete] IS NULL
INNER JOIN #SoBenNhan r
    ON r.[PartnerCode] = p.[Code]
   AND r.[PacketCode] = k.[Code]
WHERE s.[IsDelete] IS NULL
  AND s.[Direction] = 0
  AND s.[SerialNbr] IS NOT NULL
  AND s.[SerialNbr] <= r.[MaxSerial]
  AND (s.[ProcessingUntil] IS NULL OR s.[ProcessingUntil] <= GETDATE());

DECLARE @SoLuongKet INT = (SELECT COUNT(*) FROM #LuongKet);
DECLARE @SoDaSua INT = 0;
PRINT N'-> Đăng ký gửi đang kẹt (SerialNbr <= số bên nhận đã lưu, không giữ khóa): ' + CAST(@SoLuongKet AS NVARCHAR(12)) + N'.';

SELECT [MaDoiTac], [MaGoi], [TrangThai], [SoDangGiu], [SoLonNhatBenNhan], [SoMoi], [ID]
FROM #LuongKet
ORDER BY [MaDoiTac], [MaGoi];

IF @SoLuongKet = 0
    PRINT N'✅ Không có luồng nào kẹt, không làm gì.';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ liệt kê, không ghi dữ liệu.';
ELSE
BEGIN
    UPDATE s
    SET s.[SerialNbr] = l.[SoMoi],
        s.[UpdateTime] = GETDATE()
    FROM [dbo].[ShareDataSubscription] s
    INNER JOIN #LuongKet l
        ON l.[ID] = s.[ID]
    WHERE s.[SerialNbr] = l.[SoDangGiu]
      AND (s.[ProcessingUntil] IS NULL OR s.[ProcessingUntil] <= GETDATE());

    SET @SoDaSua = @@ROWCOUNT;
    PRINT N'✅ Đã nâng SerialNbr cho ' + CAST(@SoDaSua AS NVARCHAR(12)) + N' đăng ký gửi.';
    IF @SoDaSua < @SoLuongKet
        PRINT N'⚠️ ' + CAST(@SoLuongKet - @SoDaSua AS NVARCHAR(12)) + N' đăng ký vừa có lượt giữ khóa hoặc vừa đổi số, chạy lại tệp.';
END;
GO

SET NOEXEC OFF;
GO
