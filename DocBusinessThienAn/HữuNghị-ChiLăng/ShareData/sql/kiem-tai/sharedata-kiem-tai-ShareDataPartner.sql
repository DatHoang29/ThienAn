USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-kiem-tai-ShareDataPartner.sql
-- BẢNG GHI: [dbo].[ShareDataPartner] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Kiểm tải ShareData trên Staging (nhiều đối tác × nhiều gói tin):
--              1. Khôi phục đối tác PC08 với đúng ID đang được ShareDataMapping / ShareDataSubscription tham chiếu
--                 nếu trên CSDL đang bị mất dòng PC08.
--              2. Đảm bảo 2 đối tác gốc (PC08, TTDHT) đều ở trạng thái Bật (Status = 1), Đã kết nối (SessionState = 1)
--                 và có cấu hình kênh kết nối hợp lệ (nếu đang trống thì điền cấu hình mặc định đầu tệp).
--              3. Tạo @SoDoiTac đối tác kiểm tải (KT001 .. KT050, mặc định 50) ở trạng thái Bật (Status = 1)
--                 và Đã kết nối (SessionState = 1), chép cấu hình kết nối của đối tác mẫu @MaDoiTacMau
--                 (để NULL thì dùng cấu hình mặc định đầu tệp); đối tác kiểm tải đã có thì cập nhật theo cấu hình đó.
--              Cấu hình mặc định gọi API nhận của chính WebAPI staging (https://10.10.12.80:12204/api/sharedata/sharedatainbound/receive,
--              kênh WebAPI): gói gửi đi mang header PartnerCode = mã đối tác nên WebAPI nhận dưới chính đối tác đó, dịch vụ chỉ nhận
--              ghi vào CSDL nhận, nhờ vậy kiểm tải được cả chiều gửi lẫn chiều nhận. Gọi bằng IP nên tên không khớp chứng chỉ
--              của máy chủ (cấp cho tên miền): cấu hình kết nối bật BypassSsl, bỏ đi thì Worker báo lỗi SSL.
--              Dùng để 6 dịch vụ ShareDataWorker đang chạy gửi nhiều gói tin cho nhiều đối tác cùng lúc,
--              theo dõi CPU, RAM trên Task Manager của máy chủ.
--              Danh sách đối tác trên màn hình đọc qua cache của WebAPI (ghi thẳng bảng không làm mới cache): chạy xong
--              thì sửa rồi Lưu một đối tác bất kỳ trên màn hình (không cần đổi gì) hoặc khởi động lại WebAPI để thấy đối tác mới.
-- THỨ TỰ CHẠY: K1 → K2 (sharedata-kiem-tai-ShareDataMapping.sql) → K3 (sharedata-kiem-tai-ShareDataSubscription.sql) → K4 (sharedata-kiem-tai-ShareDataSql.sql).
--              DỌN: đặt @XoaDuLieuKiemTai = 1 rồi chạy ngược K4 → K3 → K2 → K1.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Staging (chạy theo yêu cầu kiểm tải). ⛔ Không chạy trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): đối tác đã có cùng mã thì bỏ qua.
--           Mọi dòng kiểm tải tạo mới gắn CreateUId = N'SHAREDATA-KIEM-TAI' để dọn sạch.
-- CẬP NHẬT LẦN CUỐI: 11112026 (mặc định gọi API nhận của WebAPI staging cổng 12204, cập nhật kết nối đối tác kiểm tải đã có)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-kiem-tai-ShareDataPartner.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataPartner]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataPartner] chưa tồn tại (chưa chạy CodeFirst)!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ — giữ @TienToMa giống nhau ở cả K1, K2, K3.
-- -----------------------------------------------------------------------------------------
DECLARE @MaDoiTacMau NVARCHAR(64) = NULL;                        -- Mã đối tác mẫu lấy cấu hình kết nối; NULL = dùng cấu hình mặc định dưới đây
DECLARE @TienToMa NVARCHAR(16) = N'KT';                          -- Mã đối tác kiểm tải: KT001, KT002, ...
DECLARE @SoDoiTac INT = 50;                                      -- Số đối tác kiểm tải cần tạo thêm (1..999)
DECLARE @DiaChiMacDinh NVARCHAR(128) = N'https://10.10.12.80';   -- Địa chỉ WebAPI nhận, ghi kèm https:// (màn hình web là cổng 12205)
DECLARE @CongMacDinh INT = 12204;                                -- Cổng WebAPI
DECLARE @EndPointMacDinh NVARCHAR(256) = N'/api/sharedata/sharedatainbound/receive';  -- API nhận gói tin của WebAPI
DECLARE @KieuKetNoiMacDinh INT = 1;                              -- 1 = WebAPI; 2 = Xuất tệp tin vào sharedata/send (khi đó để @EndPointMacDinh = NULL)
DECLARE @CauHinhKetNoiMacDinh NVARCHAR(MAX) = N'{"Format":"Json","BypassSsl":"true"}';  -- BypassSsl: gọi bằng IP, tên không khớp chứng chỉ
DECLARE @XoaDuLieuKiemTai BIT = 0;                               -- 1 = xóa đối tác kiểm tải (chạy sau khi đã dọn K4, K3, K2)

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-KIEM-TAI';
DECLARE @TenantIdPartner NVARCHAR(64) = (
    SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64))
    FROM [dbo].[SysOpConfig]
    WHERE [TenantId] IS NOT NULL
);
IF @TenantIdPartner IS NULL
    SET @TenantIdPartner = N'Default';

-- -----------------------------------------------------------------------------------------
-- 1. DỌN DỮ LIỆU KIỂM TẢI
-- -----------------------------------------------------------------------------------------
IF @XoaDuLieuKiemTai = 1
BEGIN
    DELETE FROM [dbo].[ShareDataPartner]
    WHERE [CreateUId] = @SeedUser
      AND [Code] LIKE @TienToMa + N'[0-9][0-9][0-9]';
    PRINT N'-> 1. Đã xóa ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đối tác kiểm tải.';
END
ELSE
BEGIN
    IF @SoDoiTac NOT BETWEEN 1 AND 999
    BEGIN
        RAISERROR(N'⛔ @SoDoiTac phải trong khoảng 1..999.', 16, 1);
    END
    ELSE
    BEGIN
        -- ---------------------------------------------------------------------------------
        -- 2. KHÔI PHỤC PC08 NẾU BỊ MẤT DÒNG TRONG ShareDataPartner (GIỮ ĐÚNG ID LIÊN KẾT)
        -- ---------------------------------------------------------------------------------
        IF NOT EXISTS (
            SELECT 1
            FROM [dbo].[ShareDataPartner]
            WHERE [Code] = N'PC08'
              AND [IsDelete] IS NULL
        )
        BEGIN
            DECLARE @Pc08Id NVARCHAR(64) = COALESCE(
                (SELECT TOP 1 [PartnerId] FROM [dbo].[ShareDataMapping] WHERE [Code] LIKE N'PC08[_]%' AND [PartnerId] IS NOT NULL ORDER BY [IsDelete] ASC, [CreateTime] DESC),
                (SELECT TOP 1 [PartnerId] FROM [dbo].[ShareDataSubscription] WHERE [PartnerId] = N'97ade67b-9e9b-44e3-ba0e-f31670c2a578'),
                N'97ade67b-9e9b-44e3-ba0e-f31670c2a578'
            );

            IF EXISTS (SELECT 1 FROM [dbo].[ShareDataPartner] WHERE [ID] = @Pc08Id)
                SET @Pc08Id = LOWER(CONVERT(NVARCHAR(64), NEWID()));

            INSERT INTO [dbo].[ShareDataPartner]
                ([ID], [Code], [Name], [Address], [Port], [EndPointApiUrl], [Status], [SessionState], [OrderNo], [Remark],
                 [ConnectionType], [ConnectionConfig], [TenantId], [CreateTime], [CreateUId])
            VALUES
                (@Pc08Id, N'PC08', N'Phòng Cảnh sát giao thông (PC08)', @DiaChiMacDinh, @CongMacDinh, @EndPointMacDinh,
                 1, 1, 1, NULL, @KieuKetNoiMacDinh, @CauHinhKetNoiMacDinh, @TenantIdPartner, GETDATE(), N'SHAREDATA-SEED-14');

            PRINT N'-> 2.1. Đã khôi phục đối tác PC08 với ID = ' + @Pc08Id + N'.';
        END;

        -- ---------------------------------------------------------------------------------
        -- 3. BẬT KẾT NỐI VÀ ĐIỀN KÊNH KẾT NỐI NẾU CÒN TRỐNG CHO PC08, TTDHT
        -- ---------------------------------------------------------------------------------
        UPDATE p
        SET p.[Status]           = 1,
            p.[SessionState]     = 1,
            p.[Address]          = COALESCE(NULLIF(p.[Address], N''), @DiaChiMacDinh),
            p.[Port]             = COALESCE(p.[Port], @CongMacDinh),
            p.[EndPointApiUrl]   = COALESCE(p.[EndPointApiUrl], @EndPointMacDinh),
            p.[ConnectionType]   = COALESCE(p.[ConnectionType], @KieuKetNoiMacDinh),
            p.[ConnectionConfig] = COALESCE(NULLIF(p.[ConnectionConfig], N''), @CauHinhKetNoiMacDinh),
            p.[UpdateTime]       = GETDATE(),
            p.[UpdateUId]        = @SeedUser
        FROM [dbo].[ShareDataPartner] p
        WHERE p.[IsDelete] IS NULL
          AND p.[Code] IN (N'PC08', N'TTDHT')
          AND (
                ISNULL(p.[Status], 0) <> 1
                OR ISNULL(p.[SessionState], 0) <> 1
                OR NULLIF(p.[Address], N'') IS NULL
                OR p.[ConnectionType] IS NULL
              );
        PRINT N'-> 2.2. Đã cập nhật trạng thái Đã kết nối và kênh kết nối cho ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đối tác gốc (PC08, TTDHT).';

        -- ---------------------------------------------------------------------------------
        -- 4. TẠO @SoDoiTac ĐỐI TÁC KIỂM TẢI (KT001 .. KT050)
        -- ---------------------------------------------------------------------------------
        DECLARE @MauAddress NVARCHAR(128) = @DiaChiMacDinh;
        DECLARE @MauPort INT = @CongMacDinh;
        DECLARE @MauEndPoint NVARCHAR(256) = @EndPointMacDinh;
        DECLARE @MauConnType INT = @KieuKetNoiMacDinh;
        DECLARE @MauConnConfig NVARCHAR(MAX) = @CauHinhKetNoiMacDinh;
        DECLARE @MauTenantId NVARCHAR(64) = @TenantIdPartner;

        SELECT TOP 1
            @MauAddress    = COALESCE(NULLIF([Address], N''), @DiaChiMacDinh),
            @MauPort       = COALESCE([Port], @CongMacDinh),
            @MauEndPoint   = COALESCE([EndPointApiUrl], @EndPointMacDinh),
            @MauConnType   = COALESCE([ConnectionType], @KieuKetNoiMacDinh),
            @MauConnConfig = COALESCE(NULLIF([ConnectionConfig], N''), @CauHinhKetNoiMacDinh),
            @MauTenantId   = COALESCE([TenantId], @TenantIdPartner)
        FROM [dbo].[ShareDataPartner]
        WHERE [Code] = @MaDoiTacMau
          AND [IsDelete] IS NULL;

        -- Đối tác kiểm tải đã có (chạy lại sau khi đổi cấu hình): đưa kết nối về đúng cấu hình đang dùng.
        UPDATE [dbo].[ShareDataPartner]
        SET [Address]          = @MauAddress,
            [Port]             = @MauPort,
            [EndPointApiUrl]   = @MauEndPoint,
            [ConnectionType]   = @MauConnType,
            [ConnectionConfig] = @MauConnConfig,
            [Status]           = 1,
            [SessionState]     = 1,
            [UpdateTime]       = GETDATE(),
            [UpdateUId]        = @SeedUser
        WHERE [CreateUId] = @SeedUser
          AND [IsDelete] IS NULL
          AND [Code] LIKE @TienToMa + N'[0-9][0-9][0-9]'
          AND (ISNULL([Address], N'') <> ISNULL(@MauAddress, N'')
               OR ISNULL([Port], 0) <> ISNULL(@MauPort, 0)
               OR ISNULL([EndPointApiUrl], N'') <> ISNULL(@MauEndPoint, N'')
               OR ISNULL([ConnectionType], 0) <> ISNULL(@MauConnType, 0)
               OR ISNULL([ConnectionConfig], N'') <> ISNULL(@MauConnConfig, N'')
               OR ISNULL([Status], 0) <> 1
               OR ISNULL([SessionState], 0) <> 1);
        PRINT N'-> 3.1. Đã cập nhật kết nối của ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đối tác kiểm tải đã có.';

        ;WITH [So] AS (
            SELECT TOP (@SoDoiTac) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS [N]
            FROM sys.all_objects
        )
        INSERT INTO [dbo].[ShareDataPartner]
            ([ID], [Code], [Name], [Address], [Port], [EndPointApiUrl], [Status], [SessionState], [OrderNo], [Remark],
             [ConnectionType], [ConnectionConfig], [TenantId], [CreateTime], [CreateUId])
        SELECT
            LOWER(CONVERT(NVARCHAR(64), NEWID())),
            @TienToMa + RIGHT(N'000' + CAST(s.[N] AS NVARCHAR(3)), 3),
            LEFT(N'Kiểm tải ' + RIGHT(N'000' + CAST(s.[N] AS NVARCHAR(3)), 3) + ISNULL(N' (mẫu ' + @MaDoiTacMau + N')', N''), 128),
            @MauAddress, @MauPort, @MauEndPoint, 1, 1, 1000 + s.[N], N'Dữ liệu kiểm tải, xóa bằng @XoaDuLieuKiemTai = 1',
            @MauConnType, @MauConnConfig, @MauTenantId, GETDATE(), @SeedUser
        FROM [So] s
        WHERE NOT EXISTS (
            SELECT 1
            FROM [dbo].[ShareDataPartner] p
            WHERE p.[Code] = @TienToMa + RIGHT(N'000' + CAST(s.[N] AS NVARCHAR(3)), 3)
              AND p.[IsDelete] IS NULL
        );
        PRINT N'-> 3.2. Đã thêm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đối tác kiểm tải (' + @TienToMa + N'001..' + @TienToMa + RIGHT(N'000' + CAST(@SoDoiTac AS NVARCHAR(3)), 3) + N'), gửi vào '
            + ISNULL(@MauAddress, N'?') + ISNULL(N':' + CAST(@MauPort AS NVARCHAR(10)), N'') + ISNULL(@MauEndPoint, N'') + N'.';
    END;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-kiem-tai-ShareDataPartner.sql';
PRINT N'=============================================================================';
GO
