-- ==============================================================================
-- Script Dữ liệu Khởi tạo: VideoWall Cascade DS-C66S (1 Trung tâm + 3 Bộ con / 32 Màn)
-- Created date: 08/09/2026
-- 
-- LƯU Ý BẢO MẬT (Safeguard #3 & Rule 9):
-- File này CHỈ ĐƯỢC TẠO để lưu trữ và nạp thủ công khi cần.
-- TUYỆT ĐỐI KHÔNG TỰ ĐỘNG THỰC THI trên bất kỳ môi trường CSDL nào.
--
-- Quy ước Panel: 3840 x 2160 (khớp VwWallProfile và FE wallConstants.ts)
-- ==============================================================================

-- 1. VwWallTopology (1 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [VwWallTopology] WHERE [Code] = 'WALL-01' AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [VwWallTopology] (
        [ID], [Code], [Name], [Rows], [Cols], [ScreenWidth], [ScreenHeight], [Remark], [CreateTime], [UpdateTime]
    ) VALUES (
        'TOPO_CASCADE_01', 'WALL-01', N'Tường Video Wall Cascade 8x4', 4, 8, 3840, 2160, N'Hệ thống 32 màn ghép 8x4 điều khiển bởi DS-C66S cascade', GETDATE(), GETDATE()
    );
END
ELSE
BEGIN
    UPDATE [VwWallTopology]
    SET [Rows] = 4, [Cols] = 8, [ScreenWidth] = 3840, [ScreenHeight] = 2160, [UpdateTime] = GETDATE()
    WHERE [Code] = 'WALL-01' AND [IsDelete] IS NULL;
END
GO

-- 2. VwController (4 bản ghi: 1 center + 3 sub)
-- C1: Bộ trung tâm DS-C66S-S12
IF NOT EXISTS (SELECT 1 FROM [VwController] WHERE [Code] = 'CTRL-01' AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [VwController] (
        [ID], [Code], [Name], [Model], [IP], [Account], [PassWord], [Chasis], [Role], [IntegrationMode],
        [OriginCol], [OriginRow], [CoverCols], [CoverRows], [InputSlotsNumber], [SlotsNumber],
        [GenlockInConnected], [GenlockOutConnected], [Status], [Remark], [CreateTime], [UpdateTime]
    ) VALUES (
        'CTRL_CENTER_01', 'CTRL-01', N'Bộ Điều Khiển Trung Tâm DS-C66S-S12', 'DS-C66S-S12', '172.25.0.32:80', 'admin', 'Tcp@2025', 'Chassis-Center', 'center', 'active',
        0, 0, 8, 4, 8, 12,
        0, 1, 1, N'Compositor trung tâm lái toàn tường ISAPI', GETDATE(), GETDATE()
    );
END
ELSE
BEGIN
    UPDATE [VwController]
    SET [Role] = 'center', [IntegrationMode] = 'active', [IP] = '172.25.0.32:80', [Account] = 'admin', [PassWord] = 'Tcp@2025', [UpdateTime] = GETDATE()
    WHERE [Code] = 'CTRL-01' AND [IsDelete] IS NULL;
END
GO

-- C2: Bộ con DS-C66S-S6 (Vùng Cột 0-3 / 16 màn)
IF NOT EXISTS (SELECT 1 FROM [VwController] WHERE [Code] = 'CTRL-02' AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [VwController] (
        [ID], [Code], [Name], [Model], [IP], [Account], [PassWord], [Chasis], [Role], [IntegrationMode],
        [OriginCol], [OriginRow], [CoverCols], [CoverRows], [InputSlotsNumber], [SlotsNumber],
        [GenlockInConnected], [GenlockOutConnected], [Status], [Remark], [CreateTime], [UpdateTime]
    ) VALUES (
        'CTRL_SUB_01', 'CTRL-02', N'Bộ Điều Khiển Phụ C2 (Cột 0-3)', 'DS-C66S-S6', '172.25.0.33:80', 'admin', 'Tcp@2025', 'Chassis-Sub1', 'sub', 'inventory',
        0, 0, 4, 4, 4, 6,
        1, 1, 1, N'Fan-out tĩnh vùng cột 0-3 (16 màn)', GETDATE(), GETDATE()
    );
END
ELSE
BEGIN
    UPDATE [VwController]
    SET [Role] = 'sub', [IntegrationMode] = 'inventory', [UpdateTime] = GETDATE()
    WHERE [Code] = 'CTRL-02' AND [IsDelete] IS NULL;
END
GO

-- C3: Bộ con DS-C66S-S6 (Vùng Cột 4-5 / 8 màn)
IF NOT EXISTS (SELECT 1 FROM [VwController] WHERE [Code] = 'CTRL-03' AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [VwController] (
        [ID], [Code], [Name], [Model], [IP], [Account], [PassWord], [Chasis], [Role], [IntegrationMode],
        [OriginCol], [OriginRow], [CoverCols], [CoverRows], [InputSlotsNumber], [SlotsNumber],
        [GenlockInConnected], [GenlockOutConnected], [Status], [Remark], [CreateTime], [UpdateTime]
    ) VALUES (
        'CTRL_SUB_02', 'CTRL-03', N'Bộ Điều Khiển Phụ C3 (Cột 4-5)', 'DS-C66S-S6', '172.25.0.34:80', 'admin', 'Tcp@2025', 'Chassis-Sub2', 'sub', 'inventory',
        4, 0, 2, 4, 4, 6,
        1, 1, 1, N'Fan-out tĩnh vùng cột 4-5 (8 màn)', GETDATE(), GETDATE()
    );
END
ELSE
BEGIN
    UPDATE [VwController]
    SET [Role] = 'sub', [IntegrationMode] = 'inventory', [UpdateTime] = GETDATE()
    WHERE [Code] = 'CTRL-03' AND [IsDelete] IS NULL;
END
GO

-- C4: Bộ con DS-C66S-S6 (Vùng Cột 6-7 / 8 màn)
IF NOT EXISTS (SELECT 1 FROM [VwController] WHERE [Code] = 'CTRL-04' AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [VwController] (
        [ID], [Code], [Name], [Model], [IP], [Account], [PassWord], [Chasis], [Role], [IntegrationMode],
        [OriginCol], [OriginRow], [CoverCols], [CoverRows], [InputSlotsNumber], [SlotsNumber],
        [GenlockInConnected], [GenlockOutConnected], [Status], [Remark], [CreateTime], [UpdateTime]
    ) VALUES (
        'CTRL_SUB_03', 'CTRL-04', N'Bộ Điều Khiển Phụ C4 (Cột 6-7)', 'DS-C66S-S6', '172.25.0.35:80', 'admin', 'Tcp@2025', 'Chassis-Sub3', 'sub', 'inventory',
        6, 0, 2, 4, 4, 6,
        1, 0, 1, N'Fan-out tĩnh vùng cột 6-7 (8 màn)', GETDATE(), GETDATE()
    );
END
ELSE
BEGIN
    UPDATE [VwController]
    SET [Role] = 'sub', [IntegrationMode] = 'inventory', [UpdateTime] = GETDATE()
    WHERE [Code] = 'CTRL-04' AND [IsDelete] IS NULL;
END
GO

-- 3. VwScreen (32 màn: 8 cột x 4 hàng)
-- Cột 0..3 gắn C2 (16 màn: port 1..16)
-- Cột 4..5 gắn C3 (8 màn: port 1..8)
-- Cột 6..7 gắn C4 (8 màn: port 1..8)
DECLARE @c INT = 0;
DECLARE @r INT = 0;
DECLARE @screenCode VARCHAR(32);
DECLARE @screenName NVARCHAR(64);
DECLARE @ctrlId VARCHAR(64);
DECLARE @port INT;

WHILE @r < 4
BEGIN
    SET @c = 0;
    WHILE @c < 8
    BEGIN
        SET @screenCode = 'SCR-' + CAST(@c AS VARCHAR(2)) + '-' + CAST(@r AS VARCHAR(2));
        SET @screenName = N'Màn hình [' + CAST(@c AS NVARCHAR(2)) + ',' + CAST(@r AS NVARCHAR(2)) + ']';

        IF @c <= 3
        BEGIN
            SET @ctrlId = 'CTRL_SUB_01';
            SET @port = @r * 4 + @c + 1;
        END
        ELSE IF @c <= 5
        BEGIN
            SET @ctrlId = 'CTRL_SUB_02';
            SET @port = @r * 2 + (@c - 4) + 1;
        END
        ELSE
        BEGIN
            SET @ctrlId = 'CTRL_SUB_03';
            SET @port = @r * 2 + (@c - 6) + 1;
        END

        IF NOT EXISTS (SELECT 1 FROM [VwScreen] WHERE [Code] = @screenCode AND [IsDelete] IS NULL)
        BEGIN
            INSERT INTO [VwScreen] (
                [ID], [Code], [Name], [GridCol], [GridRow], [ControllerId], [OutPutPort],
                [Resolution], [WidthPx], [HeightPx], [Status], [CreateTime], [UpdateTime]
            ) VALUES (
                'SCR_' + CAST(@c AS VARCHAR(2)) + '_' + CAST(@r AS VARCHAR(2)),
                @screenCode, @screenName, @c, @r, @ctrlId, CAST(@port AS VARCHAR(8)),
                '3840x2160', '3840', '2160', 1, GETDATE(), GETDATE()
            );
        END
        ELSE
        BEGIN
            UPDATE [VwScreen]
            SET [GridCol] = @c, [GridRow] = @r, [ControllerId] = @ctrlId, [OutPutPort] = CAST(@port AS VARCHAR(8)),
                [Resolution] = '3840x2160', [WidthPx] = '3840', [HeightPx] = '2160', [UpdateTime] = GETDATE()
            WHERE [Code] = @screenCode AND [IsDelete] IS NULL;
        END

        SET @c = @c + 1;
    END
    SET @r = @r + 1;
END
GO

-- 4. VwSource (21 nguồn HDMI: 1 ITS Bản đồ + 20 Camera CCTV)
IF NOT EXISTS (SELECT 1 FROM [VwSource] WHERE [Code] = 'SRC-ITS-01' AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [VwSource] (
        [ID], [Code], [Name], [SourceType], [SignalNo], [Status], [Resolution], [CreateTime], [UpdateTime]
    ) VALUES (
        'SRC_ITS_01', 'SRC-ITS-01', N'ITS Bản đồ Giao thông Trung tâm', 'hdmi_in', 1, 1, '3840x2160', GETDATE(), GETDATE()
    );
END
GO

DECLARE @camIdx INT = 1;
DECLARE @camCode VARCHAR(32);
DECLARE @camName NVARCHAR(64);

WHILE @camIdx <= 20
BEGIN
    SET @camCode = 'SRC-CAM-' + RIGHT('0' + CAST(@camIdx AS VARCHAR(2)), 2);
    SET @camName = N'Camera CCTV Giám sát ' + RIGHT('0' + CAST(@camIdx AS VARCHAR(2)), 2);

    IF NOT EXISTS (SELECT 1 FROM [VwSource] WHERE [Code] = @camCode AND [IsDelete] IS NULL)
    BEGIN
        INSERT INTO [VwSource] (
            [ID], [Code], [Name], [SourceType], [SignalNo], [Status], [Resolution], [CreateTime], [UpdateTime]
        ) VALUES (
            'SRC_CAM_' + RIGHT('0' + CAST(@camIdx AS VARCHAR(2)), 2),
            @camCode, @camName, 'hdmi_in', @camIdx + 1, 1, '1920x1080', GETDATE(), GETDATE()
        );
    END

    SET @camIdx = @camIdx + 1;
END
GO

-- 5. VwScene (1 Kịch bản toàn tường)
IF NOT EXISTS (SELECT 1 FROM [VwScene] WHERE [Code] = 'SCN-FULL-01' AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [VwScene] (
        [ID], [Code], [Name], [ControllerId], [OutputId], [GridCols], [GridRows],
        [IsDefault], [Status], [ActiveScene], [OrderNo], [Remark], [CreateTime], [UpdateTime]
    ) VALUES (
        'SCN_FULL_01', 'SCN-FULL-01', N'Giám sát bình thường', NULL, '1', 8, 4,
        1, 1, 1, 1, N'Kịch bản toàn tường 32 màn ghép cascade', GETDATE(), GETDATE()
    );
END
ELSE
BEGIN
    UPDATE [VwScene]
    SET [OutputId] = '1', [GridCols] = 8, [GridRows] = 4, [ControllerId] = NULL, [UpdateTime] = GETDATE()
    WHERE [Code] = 'SCN-FULL-01' AND [IsDelete] IS NULL;
END
GO

-- 6. VwWindowScene (21 Cửa sổ: 1 ITS giữa 12 màn + 20 Camera viền)
-- Cửa sổ ITS: Cột 2-7 (6 cột) x Hàng 1-2 (2 hàng)
-- X = 2 * 3840 = 7680, Y = 1 * 2160 = 2160, W = 6 * 3840 = 23040, H = 2 * 2160 = 4320
IF NOT EXISTS (SELECT 1 FROM [VwWindowScene] WHERE [Code] = 'WND-ITS-01' AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [VwWindowScene] (
        [ID], [Code], [Name], [SceneId], [SourceId], [X], [Y], [W], [H], [ZIndex], [Visible], [CreateTime], [UpdateTime]
    ) VALUES (
        'WND_ITS_01', 'WND-ITS-01', N'Cửa sổ ITS Bản đồ Trung tâm', 'SCN_FULL_01', 'SRC_ITS_01',
        7680, 2160, 23040, 4320, 10, 1, GETDATE(), GETDATE()
    );
END
GO

-- 20 Cửa sổ camera viền lát kín:
-- Hàng 0: 8 màn (cột 0..7)
-- Hàng 3: 8 màn (cột 0..7)
-- Hàng 1: 2 màn (cột 0, 1)
-- Hàng 2: 2 màn (cột 0, 1)
-- Bảng toạ độ viền theo thứ tự cam 1..20:
-- Cam 1..8: Hàng 0, Cột 0..7
-- Cam 9..10: Hàng 1, Cột 0, 1
-- Cam 11..12: Hàng 2, Cột 0, 1
-- Cam 13..20: Hàng 3, Cột 0..7

DECLARE @borderIdx INT = 1;
DECLARE @bCol INT;
DECLARE @bRow INT;
DECLARE @wndCode VARCHAR(32);
DECLARE @wndName NVARCHAR(64);
DECLARE @srcId VARCHAR(64);

WHILE @borderIdx <= 20
BEGIN
    IF @borderIdx <= 8
    BEGIN
        SET @bRow = 0;
        SET @bCol = @borderIdx - 1;
    END
    ELSE IF @borderIdx <= 10
    BEGIN
        SET @bRow = 1;
        SET @bCol = @borderIdx - 9;
    END
    ELSE IF @borderIdx <= 12
    BEGIN
        SET @bRow = 2;
        SET @bCol = @borderIdx - 11;
    END
    ELSE
    BEGIN
        SET @bRow = 3;
        SET @bCol = @borderIdx - 13;
    END

    SET @wndCode = 'WND-CAM-' + RIGHT('0' + CAST(@borderIdx AS VARCHAR(2)), 2);
    SET @wndName = N'Cửa sổ Camera Viền ' + RIGHT('0' + CAST(@borderIdx AS VARCHAR(2)), 2);
    SET @srcId = 'SRC_CAM_' + RIGHT('0' + CAST(@borderIdx AS VARCHAR(2)), 2);

    IF NOT EXISTS (SELECT 1 FROM [VwWindowScene] WHERE [Code] = @wndCode AND [IsDelete] IS NULL)
    BEGIN
        INSERT INTO [VwWindowScene] (
            [ID], [Code], [Name], [SceneId], [SourceId],
            [X], [Y], [W], [H], [ZIndex], [Visible], [CreateTime], [UpdateTime]
        ) VALUES (
            'WND_CAM_' + RIGHT('0' + CAST(@borderIdx AS VARCHAR(2)), 2),
            @wndCode, @wndName, 'SCN_FULL_01', @srcId,
            @bCol * 3840, @bRow * 2160, 3840, 2160, 1, 1, GETDATE(), GETDATE()
        );
    END
    ELSE
    BEGIN
        UPDATE [VwWindowScene]
        SET [X] = @bCol * 3840, [Y] = @bRow * 2160, [W] = 3840, [H] = 2160, [UpdateTime] = GETDATE()
        WHERE [Code] = @wndCode AND [IsDelete] IS NULL;
    END

    SET @borderIdx = @borderIdx + 1;
END
GO
