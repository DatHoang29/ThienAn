USE [DEV_ITS10];
GO

-- 1. Cập nhật nhóm lz.exception còn chứa chữ 'phễu lọc' trong cột Value
UPDATE [dbo].[SysTerminology]
SET [Value] = REPLACE([Value], N'phễu lọc', N'hồ sơ ánh xạ'),
    [UpdateTime] = GETDATE(),
    [UpdateUId] = N'system'
WHERE [Name] = N'lz.exception'
  AND [Code] IN (
      N'sharedata.codeSetInUse',
      N'sharedata.mappingActiveDuplicated',
      N'sharedata.packetCodeNameLocked',
      N'sharedata.packetFieldInUse',
      N'sharedata.packetFieldLocked',
      N'sharedata.packetInUse'
  )
  AND [Lang] = N'vi-VN'
  AND [Value] LIKE N'%phễu lọc%';

-- 2. Cập nhật nhóm lz.validation còn chứa chữ 'phễu lọc' trong cột Value
UPDATE [dbo].[SysTerminology]
SET [Value] = REPLACE([Value], N'phễu lọc', N'hồ sơ ánh xạ'),
    [UpdateTime] = GETDATE(),
    [UpdateUId] = N'system'
WHERE [Name] = N'lz.validation'
  AND [Code] = N'sharedata.mappingFieldKeyRequired'
  AND [Lang] = N'vi-VN'
  AND [Value] LIKE N'%phễu lọc%';

-- 3. Chuẩn hóa câu lặp chữ 'đang ... đang' ở mã sharedata.mappingInUse
UPDATE [dbo].[SysTerminology]
SET [Value] = N'Hồ sơ ánh xạ đang được sử dụng trong cấu hình đăng ký, không thể sửa đổi hoặc xóa.',
    [UpdateTime] = GETDATE(),
    [UpdateUId] = N'system'
WHERE [Name] = N'lz.exception'
  AND [Code] = N'sharedata.mappingInUse'
  AND [Lang] = N'vi-VN';