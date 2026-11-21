/*
    Author: HieuNV
    Created date: 20/10/2026

    Mục đích: seed cấu hình hoạt động của Video Wall vào SysOpConfig để người vận hành sửa trên màn hình
    Cấu hình hoạt động thay vì sửa code.

      1. SysConfigData nhóm 'vw_config' (loại operation_config_type) — màn hình Cấu hình hoạt động gom theo nhóm này.
      2. SysOpConfig 'vw_schedule_conflict_buffer_minutes' — khoảng đệm (phút) chống trùng giờ của Lịch phát kịch bản:
         2 lịch đang bật, khác kịch bản, chạy cách nhau không quá số phút này thì bị chặn khi lưu.
         0 = chỉ chặn khi trùng đúng phút. Không có khoá / sai định dạng / số âm ⇒ BE dùng 0.

    BE đọc qua cache của SysOpConfig ⇒ sửa trên web có hiệu lực ngay; chạy script thẳng DB thì cần xoá cache / khởi động lại.
    Script luỹ đẳng: chỉ thêm bản ghi chưa có, không ghi đè giá trị người vận hành đã sửa.
*/

DECLARE @tenant NVARCHAR(128) = N'Default';
DECLARE @now DATETIME = GETDATE();
DECLARE @groupCode NVARCHAR(64) = N'vw_config';

-- 1. Nhóm cấu hình
DECLARE @opConfigTypeId NVARCHAR(128) =
    (SELECT TOP 1 ID FROM [dbo].[SysConfigType] WHERE Code = N'operation_config_type' AND IsDelete IS NULL);

IF @opConfigTypeId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[SysConfigData]
                   WHERE ConfigTypeId = @opConfigTypeId AND Code = @groupCode AND TenantId = @tenant AND IsDelete IS NULL)
BEGIN
    INSERT INTO [dbo].[SysConfigData]
        (ID, ConfigTypeId, Code, Name, OrderNo, Status, TenantId, CreateTime, CreateUId)
    VALUES
        (CONVERT(NVARCHAR(128), NEWID()), @opConfigTypeId, @groupCode, N'VideoWall Config', 100, 1, @tenant, @now, N'VW-SEED-20261020');
END

-- 2. Khoảng đệm chống trùng giờ
IF NOT EXISTS (SELECT 1 FROM [dbo].[SysOpConfig]
               WHERE GroupCode = @groupCode AND Code = N'vw_schedule_conflict_buffer_minutes' AND TenantId = @tenant AND IsDelete IS NULL)
BEGIN
    INSERT INTO [dbo].[SysOpConfig]
        (ID, Code, GroupCode, Name, Value, IsSysConfig, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
    VALUES
        (CONVERT(NVARCHAR(128), NEWID()), N'vw_schedule_conflict_buffer_minutes', @groupCode,
         N'Lịch phát kịch bản: khoảng đệm chống trùng giờ (phút)', N'5', 0, 100,
         N'Số nguyên ≥ 0. 2 lịch đang bật khác kịch bản chạy cách nhau không quá số phút này thì không cho lưu. 0 = chỉ chặn trùng đúng phút.',
         1, @tenant, @now, N'VW-SEED-20261020');
END
GO
