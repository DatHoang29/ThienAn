USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-kiem-tai-doi-duoi-101-ShareDataSql.sql
-- BẢNG GHI: [dbo].[ShareDataSql] — tệp chỉ sửa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Đổi phần đuôi __lastKey trong câu SQL riêng gói 101 (101_commonData) của một đối tác kiểm tải
--              (mặc định KT005) sang mã vùng: ISNULL(z.Code, ''), mốc dạng yyyyMMddHHmmssfff_<Code>.
--              TmsZone.Code chỉ duy nhất trong vùng còn sống (vùng đã xóa mềm trùng mã, vd 5 vùng D100 cùng giờ cập nhật),
--              nên câu thêm điều kiện z.ID IS NOT NULL AND z.IsDelete IS NULL: bỏ trạng thái của vùng đã xóa / không còn vùng.
--              Đối tác này vì vậy gửi ít dòng gói 101 hơn câu mẫu chung.
--              Nhận câu đang dùng đuôi zs.ID, ISNULL(zs.ZoneId, ''), ISNULL(z.Code, '') hoặc ISNULL(z.Code, '') + '_' + zs.ID;
--              đổi cả cột __lastKey lẫn điều kiện WHERE nên mốc và lọc > @lastKey vẫn khớp nhau.
--              Không đổi FieldsJson vì chỉ cột ẩn __lastKey và WHERE thay đổi, danh sách trường gửi đi giữ nguyên.
--              Câu đổi thì Worker tính lại chữ ký và gửi lại gói 101 của đối tác từ đầu (mốc trống).
-- THỨ TỰ CHẠY: K8, chạy sau K4 trên CSDL đã sinh câu riêng trước bản 18/11/2026 (câu mẫu chung `101_commonData` ở Tệp 2 bản 18/11/2026 đã dùng ISNULL(z.Code, '') và lọc z.ID IS NOT NULL AND z.IsDelete IS NULL nên DB mới chạy K4 không cần tệp này).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Staging (chạy theo yêu cầu kiểm tải). ⛔ Không chạy trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): câu đã đúng dạng mới thì bỏ qua.
-- CẬP NHẬT LẦN CUỐI: 18/11/2026 (mốc chỉ còn Code, bỏ ID; lọc vùng chưa xóa để Code không trùng)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-kiem-tai-doi-duoi-101-ShareDataSql.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataSql]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataSql] chưa tồn tại (chưa chạy CodeFirst)!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @MaDoiTac NVARCHAR(16) = N'KT005';
DECLARE @MaGoi    NVARCHAR(64) = N'101_commonData';

DECLARE @DuoiCodeId NVARCHAR(128) = N'+ ''_'' + ISNULL(z.Code, '''') + ''_'' + zs.ID';
DECLARE @DuoiZoneId NVARCHAR(64)  = N'+ ''_'' + ISNULL(zs.ZoneId, '''')';
DECLARE @DuoiId     NVARCHAR(64)  = N'+ ''_'' + zs.ID';
DECLARE @DuoiCode   NVARCHAR(64)  = N'+ ''_'' + ISNULL(z.Code, '''')';
DECLARE @LocMoc     NVARCHAR(32)  = N'> @lastKey';
DECLARE @LocVung    NVARCHAR(64)  = N' AND z.ID IS NOT NULL AND z.IsDelete IS NULL';

-- -----------------------------------------------------------------------------------------
-- 1. ĐỔI ĐUÔI __lastKey SANG z.Code VÀ LỌC VÙNG CHƯA XÓA
--    Đưa mọi dạng đuôi về z.Code (dạng Code + ID đổi trước để không còn sót zs.ID),
--    rồi thêm điều kiện lọc vùng ngay sau > @lastKey nếu câu chưa có.
-- -----------------------------------------------------------------------------------------
UPDATE s
SET s.[ExtractSql] = m.[CauMoi],
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPartner] p
  ON p.[ID] = s.[PartnerId]
 AND p.[IsDelete] IS NULL
JOIN [dbo].[ShareDataPacket] pk
  ON pk.[ID] = s.[PacketId]
 AND pk.[IsDelete] IS NULL
CROSS APPLY (
    SELECT REPLACE(REPLACE(REPLACE(s.[ExtractSql],
               @DuoiCodeId, @DuoiCode),
               @DuoiZoneId, @DuoiCode),
               @DuoiId, @DuoiCode) AS [Cau]
) d
CROSS APPLY (
    SELECT CASE
        WHEN CHARINDEX(@LocVung, d.[Cau]) > 0 THEN d.[Cau]
        ELSE REPLACE(d.[Cau], @LocMoc, @LocMoc + @LocVung)
    END AS [CauMoi]
) m
WHERE p.[Code] = @MaDoiTac
  AND pk.[Code] = @MaGoi
  AND s.[IsDelete] IS NULL
  AND m.[CauMoi] <> s.[ExtractSql];

IF @@ROWCOUNT = 0
    PRINT N'-> 1. Không có câu riêng ' + @MaDoiTac + N' / ' + @MaGoi + N' cần đổi (đã đổi rồi, hoặc chưa có thì chạy K4).';
ELSE
    PRINT N'-> 1. Đã đổi mốc sang thời gian + Code và lọc vùng chưa xóa cho ' + @MaDoiTac + N' / ' + @MaGoi + N'.';

-- -----------------------------------------------------------------------------------------
-- 2. KIỂM LẠI
-- -----------------------------------------------------------------------------------------
SELECT
    p.[Code] AS [PartnerCode],
    pk.[Code] AS [PacketCode],
    s.[ExtractSql],
    s.[UpdateTime]
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPartner] p
  ON p.[ID] = s.[PartnerId]
JOIN [dbo].[ShareDataPacket] pk
  ON pk.[ID] = s.[PacketId]
WHERE p.[Code] = @MaDoiTac
  AND pk.[Code] = @MaGoi
  AND s.[IsDelete] IS NULL;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-kiem-tai-doi-duoi-101-ShareDataSql.sql';
PRINT N'=============================================================================';
GO
