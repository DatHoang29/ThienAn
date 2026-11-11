USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-kiem-tai-ShareDataSubscription.sql
-- BẢNG GHI: [dbo].[ShareDataSubscription] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Kiểm tải ShareData trên Staging (nhiều đối tác × nhiều gói tin):
--              1. Bật sang trạng thái Đang hoạt động (State = 1) cho đăng ký sẵn có của PC08, TTDHT đang tạm dừng
--                 (trên staging hiện chỉ có 1 đăng ký PC08 × 101_commonData đang State = 2 Tạm dừng).
--              2. Tạo đăng ký chiều gửi (Direction = 0, Mode = 2 Liên tục, State = 1 Đang hoạt động, chạy 24/24,
--                 chu kỳ @ChuKyGiay mặc định 30 giây) cho mọi cặp:
--                   (PC08, TTDHT, KT001..KT050) × (toàn bộ gói tin đang bật có câu ExtractSql và hồ sơ ánh xạ gửi).
--                 Gói chưa có câu ghi chiều nhận (vd 110) tự bỏ. Trên staging: 52 đối tác × 8 gói (101..104, 106..109) = 416 đăng ký gửi.
--              3. Tạo đăng ký chiều nhận (Direction = 1, State = 1 Đang hoạt động) khi @TaoDangKyChieuNhan = 1.
--              Đăng ký mới được gán SerialNbr như khi tạo trên màn hình: số lớn nhất của đối tác (kể cả đăng ký đã xóa) cộng 1,
--              nên gói gửi đầu tiên đã mang số thứ tự và WebAPI chặn được gói gửi trùng.
--              @GoiBoQua: mã gói bỏ khỏi kiểm tải (giữ giống K2).
-- THỨ TỰ CHẠY: K3, chạy sau K1 và K2, trước K4.
--              DỌN: đặt @XoaDuLieuKiemTai = 1, chạy sau K4, rồi mới tới K2, K1.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Staging (chạy theo yêu cầu kiểm tải). ⛔ Không chạy trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): cặp đối tác × gói × chiều đã có đăng ký đang sống thì bỏ qua.
-- CẬP NHẬT LẦN CUỐI: 11112026 (tự bỏ gói chưa có câu ghi chiều nhận, vd 110; gán SerialNbr như màn hình, thêm @GoiBoQua)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-kiem-tai-ShareDataSubscription.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataSubscription]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataSubscription] chưa tồn tại (chưa chạy CodeFirst)!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ — giữ @TienToMa giống K1, K2.
-- -----------------------------------------------------------------------------------------
DECLARE @TienToMa NVARCHAR(16) = N'KT';
DECLARE @GoiBoQua NVARCHAR(256) = N'';                           -- Mã gói bỏ thêm khỏi kiểm tải, cách nhau dấu phẩy, vd N'109'; N'' = mọi gói đang bật. Gói chưa có câu ghi chiều nhận (vd 110) tệp tự bỏ
DECLARE @ChuKyGiay INT = 30;                                    -- Chu kỳ gửi (giây), đặt 10 nếu muốn tăng tải
DECLARE @TaoDangKyChieuNhan BIT = 1;                             -- 1 = tạo cả đăng ký chiều nhận (Direction = 1)
DECLARE @XoaDuLieuKiemTai BIT = 0;                               -- 1 = xóa đăng ký kiểm tải (chạy đầu tiên khi dọn)

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-KIEM-TAI';
DECLARE @TenantIdSub NVARCHAR(64) = (
    SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64))
    FROM [dbo].[SysOpConfig]
    WHERE [TenantId] IS NOT NULL
);
IF @TenantIdSub IS NULL
    SET @TenantIdSub = N'Default';

-- -----------------------------------------------------------------------------------------
-- 1. DỌN DỮ LIỆU KIỂM TẢI
-- -----------------------------------------------------------------------------------------
IF @XoaDuLieuKiemTai = 1
BEGIN
    DELETE FROM [dbo].[ShareDataSubscription]
    WHERE [CreateUId] = @SeedUser;
    PRINT N'-> 1.1. Đã xóa ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đăng ký kiểm tải tạo mới.';

    UPDATE s
    SET s.[State]           = 2,
        s.[ProcessingUntil] = NULL,
        s.[UpdateTime]      = GETDATE(),
        s.[UpdateUId]       = N'superadmin'
    FROM [dbo].[ShareDataSubscription] s
    WHERE s.[IsDelete] IS NULL
      AND s.[UpdateUId] = @SeedUser
      AND ISNULL(s.[CreateUId], N'') <> @SeedUser;
    PRINT N'-> 1.2. Đã trả về trạng thái Tạm dừng (State = 2) cho ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đăng ký sẵn có.';
END
ELSE
BEGIN
    IF @ChuKyGiay IS NULL OR @ChuKyGiay < 5
        SET @ChuKyGiay = 30;

    DECLARE @ScheduleJson NVARCHAR(MAX) =
        N'{"Kind":"continuous","IntervalSeconds":' + CAST(@ChuKyGiay AS NVARCHAR(10))
        + N',"StartTime":null,"EndTime":null,"DaysOfWeek":null,"StartDate":null,"EndDate":null,"DurationMinutes":null}';

    DECLARE @TargetPartners TABLE ([PartnerId] NVARCHAR(64), [PartnerCode] NVARCHAR(32));
    INSERT INTO @TargetPartners ([PartnerId], [PartnerCode])
    SELECT p.[ID], p.[Code]
    FROM [dbo].[ShareDataPartner] p
    WHERE p.[IsDelete] IS NULL
      AND p.[Status] = 1
      AND (
            p.[Code] IN (N'PC08', N'TTDHT')
            OR (p.[CreateUId] = @SeedUser AND p.[Code] LIKE @TienToMa + N'[0-9][0-9][0-9]')
          );

    -- -------------------------------------------------------------------------------------
    -- 2.1. BẬT ĐĂNG KÝ SẴN CÓ CỦA PC08, TTDHT ĐANG TẠM DỪNG SANG ĐANG HOẠT ĐỘNG (State = 1)
    -- -------------------------------------------------------------------------------------
    UPDATE s
    SET s.[State]           = 1,
        s.[IntervalSeconds] = @ChuKyGiay,
        s.[ScheduleJson]    = @ScheduleJson,
        s.[NextTimeRun]     = GETDATE(),
        s.[ProcessingUntil] = NULL,
        s.[ResolvedAt]      = COALESCE(s.[ResolvedAt], GETDATE()),
        s.[UpdateTime]      = GETDATE(),
        s.[UpdateUId]       = @SeedUser
    FROM [dbo].[ShareDataSubscription] s
    JOIN @TargetPartners tp ON tp.[PartnerId] = s.[PartnerId]
    WHERE s.[IsDelete] IS NULL
      AND s.[State] <> 1;
    PRINT N'-> 2.1. Đã bật sang Đang hoạt động (State = 1) cho ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đăng ký sẵn có.';

    -- -------------------------------------------------------------------------------------
    -- 2.2. TẠO ĐĂNG KÝ CHIỀU GỬI (Direction = 0, State = 1) CHO MỌI ĐỐI TÁC × GÓI ĐANG BẬT
    -- -------------------------------------------------------------------------------------
    ;WITH [GoiGuiHopLe] AS (
        SELECT p.[ID] AS [PacketId], p.[Code] AS [PacketCode]
        FROM [dbo].[ShareDataPacket] p
        WHERE p.[IsDelete] IS NULL
          AND p.[Status] = 1
          AND NOT EXISTS (
              SELECT 1 FROM STRING_SPLIT(@GoiBoQua, N',') b
              WHERE LTRIM(RTRIM(b.[value])) <> N''
                AND (p.[Code] = LTRIM(RTRIM(b.[value])) OR p.[Code] LIKE LTRIM(RTRIM(b.[value])) + N'[_]%')
          )
          AND EXISTS (
              SELECT 1
              FROM [dbo].[ShareDataSql] sq
              WHERE sq.[PacketId] = p.[ID]
                AND sq.[PartnerId] IS NULL
                AND sq.[IsDelete] IS NULL
                AND NULLIF(LTRIM(RTRIM(sq.[ExtractSql])), N'') IS NOT NULL
          )
          -- Gói chưa có câu ghi chiều nhận (vd 110) thì bên nhận chắc chắn báo lỗi: tự bỏ, không cần khai @GoiBoQua
          AND EXISTS (
              SELECT 1
              FROM [dbo].[ShareDataSql] sw
              WHERE sw.[PacketId] = p.[ID]
                AND sw.[PartnerId] IS NULL
                AND sw.[IsDelete] IS NULL
                AND NULLIF(LTRIM(RTRIM(sw.[WriteSql])), N'') IS NOT NULL
          )
    )
    INSERT INTO [dbo].[ShareDataSubscription]
        ([ID], [PartnerId], [Direction], [DatatypeId], [Mode], [Format], [ScheduleJson], [Priority], [State],
         [RequestedAt], [ResolvedAt], [EventSourceId], [DebounceSec], [SendOnNewData], [Remark],
         [NextTimeRun], [ProcessingUntil], [IntervalSeconds], [TenantId], [CreateTime], [CreateUId], [SerialNbr])
    SELECT
        LOWER(CONVERT(NVARCHAR(64), NEWID())),
        tp.[PartnerId],
        0,
        g.[PacketId],
        2,
        0,
        @ScheduleJson,
        5,
        1,
        GETDATE(),
        GETDATE(),
        NULL,
        NULL,
        0,
        N'Dữ liệu kiểm tải nhiều đối tác × gói tin',
        GETDATE(),
        NULL,
        @ChuKyGiay,
        @TenantIdSub,
        GETDATE(),
        @SeedUser,
        -- Số thứ tự gói như khi tạo đăng ký trên màn hình (NextSerialAsync): lớn nhất của đối tác, kể cả đăng ký đã xóa, cộng 1.
        ISNULL((SELECT MAX(x.[SerialNbr]) FROM [dbo].[ShareDataSubscription] x WHERE x.[PartnerId] = tp.[PartnerId]), 0)
            + ROW_NUMBER() OVER (PARTITION BY tp.[PartnerId] ORDER BY g.[PacketCode])
    FROM @TargetPartners tp
    CROSS JOIN [GoiGuiHopLe] g
    WHERE EXISTS (
            SELECT 1
            FROM [dbo].[ShareDataMapping] m
            WHERE m.[IsDelete] IS NULL
              AND m.[IsActive] = 1
              AND m.[PartnerId] = tp.[PartnerId]
              AND (m.[DatatypeId] = g.[PacketId] OR m.[DatatypeId] = g.[PacketCode])
              AND m.[Direction] IN (0, 2)
        )
      AND NOT EXISTS (
            SELECT 1
            FROM [dbo].[ShareDataSubscription] x
            WHERE x.[IsDelete] IS NULL
              AND x.[PartnerId] = tp.[PartnerId]
              AND (x.[DatatypeId] = g.[PacketId] OR x.[DatatypeId] = g.[PacketCode])
              AND x.[Direction] = 0
        );
    PRINT N'-> 2.2. Đã thêm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đăng ký chiều gửi (State = 1, chu kỳ ' + CAST(@ChuKyGiay AS NVARCHAR(10)) + N's).';

    -- -------------------------------------------------------------------------------------
    -- 2.3. TẠO ĐĂNG KÝ CHIỀU NHẬN (Direction = 1, State = 1) KHI @TaoDangKyChieuNhan = 1
    -- -------------------------------------------------------------------------------------
    IF @TaoDangKyChieuNhan = 1
    BEGIN
        ;WITH [GoiNhanHopLe] AS (
            SELECT p.[ID] AS [PacketId], p.[Code] AS [PacketCode]
            FROM [dbo].[ShareDataPacket] p
            WHERE p.[IsDelete] IS NULL
              AND p.[Status] = 1
              AND NOT EXISTS (
                  SELECT 1 FROM STRING_SPLIT(@GoiBoQua, N',') b
                  WHERE LTRIM(RTRIM(b.[value])) <> N''
                    AND (p.[Code] = LTRIM(RTRIM(b.[value])) OR p.[Code] LIKE LTRIM(RTRIM(b.[value])) + N'[_]%')
              )
              AND EXISTS (
                  SELECT 1
                  FROM [dbo].[ShareDataSql] sw
                  WHERE sw.[PacketId] = p.[ID]
                    AND sw.[PartnerId] IS NULL
                    AND sw.[IsDelete] IS NULL
                    AND NULLIF(LTRIM(RTRIM(sw.[WriteSql])), N'') IS NOT NULL
              )
        )
        INSERT INTO [dbo].[ShareDataSubscription]
            ([ID], [PartnerId], [Direction], [DatatypeId], [Mode], [Format], [ScheduleJson], [Priority], [State],
             [RequestedAt], [ResolvedAt], [EventSourceId], [DebounceSec], [SendOnNewData], [Remark],
             [NextTimeRun], [ProcessingUntil], [IntervalSeconds], [TenantId], [CreateTime], [CreateUId], [SerialNbr])
        SELECT
            LOWER(CONVERT(NVARCHAR(64), NEWID())),
            tp.[PartnerId],
            1,
            g.[PacketId],
            2,
            0,
            @ScheduleJson,
            5,
            1,
            GETDATE(),
            GETDATE(),
            NULL,
            NULL,
            NULL,
            N'Dữ liệu kiểm tải nhiều đối tác × gói tin',
            NULL,
            NULL,
            @ChuKyGiay,
            @TenantIdSub,
            GETDATE(),
            @SeedUser,
            ISNULL((SELECT MAX(x.[SerialNbr]) FROM [dbo].[ShareDataSubscription] x WHERE x.[PartnerId] = tp.[PartnerId]), 0)
                + ROW_NUMBER() OVER (PARTITION BY tp.[PartnerId] ORDER BY g.[PacketCode])
        FROM @TargetPartners tp
        CROSS JOIN [GoiNhanHopLe] g
        WHERE EXISTS (
                SELECT 1
                FROM [dbo].[ShareDataMapping] m
                WHERE m.[IsDelete] IS NULL
                  AND m.[IsActive] = 1
                  AND m.[PartnerId] = tp.[PartnerId]
                  AND (m.[DatatypeId] = g.[PacketId] OR m.[DatatypeId] = g.[PacketCode])
                  AND m.[Direction] IN (1, 2)
            )
          AND NOT EXISTS (
                SELECT 1
                FROM [dbo].[ShareDataSubscription] x
                WHERE x.[IsDelete] IS NULL
                  AND x.[PartnerId] = tp.[PartnerId]
                  AND (x.[DatatypeId] = g.[PacketId] OR x.[DatatypeId] = g.[PacketCode])
                  AND x.[Direction] = 1
            );
        PRINT N'-> 2.3. Đã thêm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đăng ký chiều nhận (State = 1).';
    END;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-kiem-tai-ShareDataSubscription.sql';
PRINT N'=============================================================================';
GO
