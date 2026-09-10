-- ============================================================
-- VwUserAreaPermission — Phân quyền Tầng 3: khu vực màn hình
-- Sơ đồ: thietkevideowall.jpg (8 cột × 4 hàng, panel 1920×1080)
-- Không phụ thuộc WallNo / KB-00.
-- Kiểm tra quyền: chạy trước ToCenterUniformRect.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'VwUserAreaPermission'
)
BEGIN
    CREATE TABLE [dbo].[VwUserAreaPermission] (
        [ID]          NVARCHAR(64)   NOT NULL DEFAULT NEWID(),
        [UserId]      NVARCHAR(64)   NOT NULL,
        [ColStart]    INT            NOT NULL DEFAULT 0,
        [ColEnd]      INT            NOT NULL DEFAULT 7,
        [RowStart]    INT            NOT NULL DEFAULT 0,
        [RowEnd]      INT            NOT NULL DEFAULT 3,
        [Description] NVARCHAR(256)  NULL,
        [OrgId]       NVARCHAR(64)   NULL,

        -- EntityTenant base columns
        [TenantId]    BIGINT         NULL,
        [CreateTime]  DATETIME2      NOT NULL DEFAULT GETDATE(),
        [UpdateTime]  DATETIME2      NULL,
        [CreateUId]   NVARCHAR(64)   NULL,
        [UpdateUId]   NVARCHAR(64)   NULL,
        [IsDelete]    INT            NULL,

        CONSTRAINT [PK_VwUserAreaPermission] PRIMARY KEY ([ID])
    );

    -- Index tra nhanh theo UserId (phổ biến nhất khi check quyền mỗi lệnh cửa sổ)
    CREATE INDEX [IX_VwUserAreaPermission_UserId]
        ON [dbo].[VwUserAreaPermission] ([UserId])
        WHERE [IsDelete] IS NULL;

    PRINT 'CREATE TABLE VwUserAreaPermission — OK';
END
ELSE
BEGIN
    PRINT 'TABLE VwUserAreaPermission already exists — skipped.';
END
GO
