-- Bảng trạng thái worker giám sát Change Tracking. Chạy 1 lần, an toàn khi chạy lại.
--
-- Cập nhật 27/09/2026: 
--   - Bỏ [MachineName] và [ProcessId]: toàn hệ thống dùng chung đúng 1 dòng duy nhất.
--   - Bỏ [MissingTables], [NextTableRefreshTime], [RetryIntervalSeconds]: không lưu động trong DB hay RAM vì mốc version đã chặn hoàn toàn các truy vấn thừa.
--   - Đổi [LastProcessedVersion] -> [LastVersion].
--
-- ⚠️ Nếu CSDL đã có bảng bản cũ: xoá đi rồi chạy lại script này:
--      DROP TABLE IF EXISTS [ShareDataTrackVersion];
--    An toàn: bảng chỉ chứa trạng thái làm việc của worker, không có dữ liệu nghiệp vụ; worker tự
--    dựng lại dòng mới ở chu kỳ polling kế tiếp.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShareDataTrackVersion')
BEGIN
    CREATE TABLE [ShareDataTrackVersion] (
        [ID]                   NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [LastVersion]          BIGINT        NULL,
        [TenantId]             NVARCHAR(64)  NULL,
        [Code]                 NVARCHAR(64)  NULL,
        [CreateTime]           DATETIME      NULL,
        [CreateUId]            NVARCHAR(64)  NULL,
        [UpdateTime]           DATETIME      NULL,
        [UpdateUId]            NVARCHAR(64)  NULL,
        [RowStatus]            NVARCHAR(32)  NULL,
        [IsDelete]             DATETIME      NULL
    );
END

