USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-cau-hinh-SysOpConfig.sql
-- BẢNG GHI: [dbo].[SysOpConfig] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: 4.2 - 4.3 Nạp 46 tham số vận hành & giới hạn kiểm tra nhóm 'sharedata_config' (19 mã Worker, 23 mã FE, 4 mã BE);
--                        tham số còn giá trị ở nhóm cũ 'sharedata_fe_config' (SysConfigData) thì ưu tiên giữ giá trị đó.
--              4.5 Xóa mềm các tham số không còn sử dụng (cleanup_enabled, cleanup_retention_days, packet_lookback_days_min, packet_lookback_days_max).
-- THỨ TỰ CHẠY: 7 (trước hai tệp xoa-nhom-cu, vì đọc dữ liệu nhóm cũ sharedata_fe_config) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 26/10/2026 (bỏ packet_lookback_days_min/max)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-cau-hinh-SysOpConfig.sql';
PRINT N'=============================================================================';

DECLARE @TenantIdOp NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdOp IS NULL
    SET @TenantIdOp = N'Default';

-- 4.2 Khai báo danh sách 46 tham số nhóm sharedata_config (19 mã Worker + 23 mã FE + 4 mã BE)
DECLARE @OpConfigs TABLE (
    [Code]    NVARCHAR(128),
    [Name]    NVARCHAR(128),
    [Value]   NVARCHAR(512),
    [OrderNo] INT,
    [Remark]  NVARCHAR(2048)
);

INSERT INTO @OpConfigs ([Code], [Name], [Value], [OrderNo], [Remark])
VALUES
    -- A. 19 tham số vận hành của ShareDataWorker
    (N'outbound_check_interval_sec', N'Chu kỳ quét gửi gói tin (giây)', N'5', 10,
     N'Chu kỳ vòng lặp quét gửi gói tin của Outbound Worker (giây, khoảng cho phép: 1–300).'),
    (N'inbound_check_interval_sec', N'Chu kỳ quét nhận gói tin (giây)', N'5', 11,
     N'Chu kỳ vòng lặp quét nhận gói tin của Inbound Worker (giây, khoảng cho phép: 1–300).'),
    (N'tracking_poll_interval_sec', N'Chu kỳ quét Change Tracking (giây)', N'1', 12,
     N'Chu kỳ vòng lặp quét SQL Server Change Tracking của Tracking Worker (giây, khoảng cho phép: 1–60).'),
    (N'outbound_max_pages_per_run', N'Số trang trích xuất tối đa mỗi lượt gửi', N'20', 20,
     N'Số trang trích xuất tối đa mỗi lượt chạy của một đăng ký gửi (khoảng cho phép: 1–1000).'),
    (N'outbound_lock_budget_percent', N'Ngân sách thời gian giữ khóa gửi (%)', N'50', 21,
     N'Tỷ lệ ngân sách thời gian giữ khóa phân tán mỗi lượt gửi (%, khoảng cho phép: 10–90).'),
    (N'outbound_lock_min_sec', N'Thời gian giữ khóa gửi tối thiểu (giây)', N'300', 22,
     N'Thời gian giữ khóa phân tán tối thiểu của đăng ký gửi (giây, khoảng cho phép: 30–3600).'),
    (N'outbound_lock_interval_multiplier', N'Hệ số nhân chu kỳ gửi tính thời gian giữ khóa', N'3', 23,
     N'Hệ số nhân với chu kỳ gửi để tính thời gian giữ khóa phân tán (khoảng cho phép: 1–10).'),
    (N'outbound_default_interval_sec', N'Chu kỳ gửi mặc định của Worker (giây)', N'30', 25,
     N'Chu kỳ gửi mặc định khi đăng ký không khai báo IntervalSeconds (giây, khoảng cho phép: 1–86400).'),
    (N'inbound_max_packets_per_run', N'Số gói tin nhận xử lý tối đa mỗi lượt', N'50', 30,
     N'Số gói tin Pending xử lý tối đa mỗi lượt của Inbound Worker (khoảng cho phép: 1–1000).'),
    (N'inbound_stuck_timeout_min', N'Ngưỡng thu hồi gói nhận bị kẹt (phút)', N'10', 31,
     N'Ngưỡng thời gian thu hồi gói tin Inbound bị kẹt ở trạng thái Processing (phút, khoảng cho phép: 1–1440).'),
    (N'inbound_warn_interval_min', N'Chu kỳ tối thiểu ghi cảnh báo lặp Inbound (phút)', N'5', 32,
     N'Chu kỳ tối thiểu giữa hai lần ghi cảnh báo lặp lại ở Inbound Worker (phút, khoảng cho phép: 1–1440).'),
    (N'inbound_write_timeout_sec', N'Thời gian chờ ghi SQL chiều nhận (giây)', N'120', 33,
     N'Thời gian chờ tối đa khi thực thi câu lệnh SQL ghi dữ liệu nhận về (giây, khoảng cho phép: 10–3600).'),
    (N'tracking_table_retry_min', N'Chu kỳ thử lại bảng nguồn thiếu Change Tracking (phút)', N'5', 40,
     N'Chu kỳ thử khôi phục bảng nguồn bị cô lập do thiếu Change Tracking (phút, khoảng cho phép: 1–1440).'),
    (N'tracking_log_retention_days', N'Số ngày lưu giữ nhật ký hạ tầng Change Tracking', N'7', 41,
     N'Số ngày lưu giữ nhật ký hạ tầng Change Tracking mã ESH-16xx (ngày, khoảng cho phép: 1–365).'),
    (N'tracking_log_purge_batch', N'Số dòng xóa mỗi lô nhật ký Change Tracking', N'1000', 42,
     N'Số dòng xóa tối đa mỗi lô khi dọn nhật ký hạ tầng Change Tracking (khoảng cho phép: 100–50000).'),
    (N'tracking_change_retention_days', N'Số ngày giữ lịch sử CHANGE_RETENTION khi bật CT', N'2', 43,
     N'Số ngày giữ lịch sử thay đổi CHANGE_RETENTION khi tự bật Change Tracking cho CSDL (ngày, khoảng cho phép: 1–30).'),
    (N'cleanup_interval_min', N'Chu kỳ chạy dọn dẹp định kỳ (phút)', N'1440', 46,
     N'Chu kỳ lặp của tác vụ dọn dẹp định kỳ (phút, khoảng cho phép: 60–10080, mặc định 1440 = 24 giờ).'),
    (N'reload_interval_sec', N'Chu kỳ nạp lại cấu hình và danh sách bảng nguồn (giây)', N'30', 47,
     N'Chu kỳ nạp lại cấu hình vận hành và danh sách bảng nguồn từ CSDL (giây, khoảng cho phép: 5–3600, mặc định 30).'),

    -- B. 23 tham số giới hạn giao diện FE & dùng chung WebAPI
    (N'subscription_interval_sec_min', N'Chu kỳ gửi - tối thiểu', N'5', 101,
     N'Giây. Đăng ký gói tin (editSubscription) và kiểm tra SubscriptionValidator.'),
    (N'subscription_interval_sec_max', N'Chu kỳ gửi - tối đa', N'86400', 102,
     N'Giây (24h). Đăng ký gói tin (editSubscription) và kiểm tra SubscriptionValidator.'),
    (N'subscription_interval_sec_default', N'Chu kỳ gửi - mặc định', N'30', 103,
     N'Giây. Đăng ký gói tin (editSubscription).'),
    (N'subscription_debounce_sec_min', N'Thời gian gom sự kiện - tối thiểu', N'0', 111,
     N'Giây. Đăng ký gói tin (editSubscription).'),
    (N'subscription_debounce_sec_max', N'Thời gian gom sự kiện - tối đa', N'3600', 112,
     N'Giây (1h). Đăng ký gói tin (editSubscription).'),
    (N'subscription_debounce_sec_default', N'Thời gian gom sự kiện - mặc định', N'30', 113,
     N'Giây. Đăng ký gói tin (editSubscription).'),
    (N'subscription_priority_min', N'Độ ưu tiên - tối thiểu', N'0', 121,
     N'Đăng ký gói tin (editSubscription).'),
    (N'subscription_priority_max', N'Độ ưu tiên - tối đa', N'10', 122,
     N'Đăng ký gói tin (editSubscription).'),
    (N'subscription_priority_default', N'Độ ưu tiên - mặc định', N'5', 123,
     N'Đăng ký gói tin (editSubscription).'),
    (N'partner_heartbeat_sec_min', N'Heartbeat - tối thiểu', N'5', 201,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_heartbeat_sec_max', N'Heartbeat - tối đa', N'300', 202,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_heartbeat_sec_default', N'Heartbeat - mặc định', N'30', 203,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_response_timeout_sec_min', N'Thời gian chờ phản hồi - tối thiểu', N'3', 211,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_response_timeout_sec_max', N'Thời gian chờ phản hồi - tối đa', N'60', 212,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_response_timeout_sec_default', N'Thời gian chờ phản hồi - mặc định', N'10', 213,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_datagram_size_min', N'Kích thước datagram - tối thiểu', N'512', 221,
     N'Byte. Đối tác (editPartner).'),
    (N'partner_datagram_size_max', N'Kích thước datagram - tối đa', N'65507', 222,
     N'Byte. Payload UDP tối đa 65507. Đối tác (editPartner).'),
    (N'partner_datagram_size_default', N'Kích thước datagram - mặc định', N'1024', 223,
     N'Byte. Đối tác (editPartner).'),
    (N'mapping_sample_top_min', N'Số bản ghi mẫu - tối thiểu', N'1', 301,
     N'Bản ghi. Ánh xạ dữ liệu (editMapping) và PacketQueryHandler.SampleData.'),
    (N'mapping_sample_top_max', N'Số bản ghi mẫu - tối đa', N'50', 302,
     N'Bản ghi. Ánh xạ dữ liệu (editMapping) và PacketQueryHandler.SampleData.'),
    (N'mapping_sample_top_default', N'Số bản ghi mẫu - mặc định', N'5', 303,
     N'Bản ghi. Ánh xạ dữ liệu (editMapping) và PacketQueryHandler.SampleData.'),
    (N'history_range_default_days', N'Khoảng tra cứu - mặc định', N'7', 401,
     N'Ngày. Nhật ký lỗi (errorLog) và lịch sử chia sẻ.'),
    (N'history_range_max_days', N'Khoảng tra cứu - tối đa', N'31', 402,
     N'Ngày. Nhật ký lỗi (errorLog) và lịch sử chia sẻ.'),

    -- C. 4 tham số giới hạn kiểm tra Backend WebAPI & SQL Center
    (N'sql_sample_top_min', N'Số dòng xem mẫu SQL Center - tối thiểu', N'1', 511,
     N'Bản ghi. Giới hạn dưới khi chạy thử SQL trích xuất tại SQL Center.'),
    (N'sql_sample_top_max', N'Số dòng xem mẫu SQL Center - tối đa', N'50', 512,
     N'Bản ghi. Giới hạn trên khi chạy thử SQL trích xuất tại SQL Center.'),
    (N'sql_sample_top_default', N'Số dòng xem mẫu SQL Center - mặc định', N'10', 513,
     N'Bản ghi. Số dòng xem mẫu mặc định tại SQL Center (dataSource).'),
    (N'log_list_default_top', N'Số dòng nhật ký trả về mặc định', N'1000', 521,
     N'Bản ghi. Số dòng mặc định trả về khi truy vấn danh sách ActivityLog / AlertLog không truyền TopN.');

-- 4.3 Nạp vào SysOpConfig (nếu SysConfigData của sharedata_fe_config đang có giá trị chưa xóa mềm thì ưu tiên giữ lại)
INSERT INTO [dbo].[SysOpConfig] (
    [ID], [GroupCode], [Code], [Name], [Value],
    [OrderNo], [Remark], [Status], [IsSysConfig], [TenantId], [CreateTime], [CreateUId]
)
SELECT
    LOWER(CAST(NEWID() AS NVARCHAR(64))),
    N'sharedata_config',
    src.[Code],
    COALESCE(NULLIF(LTRIM(RTRIM(oldFe.[Name])), N''), src.[Name]),
    COALESCE(NULLIF(LTRIM(RTRIM(oldFe.[Value])), N''), src.[Value]),
    src.[OrderNo],
    COALESCE(NULLIF(LTRIM(RTRIM(oldFe.[Remark])), N''), src.[Remark]),
    1,
    1,
    @TenantIdOp,
    GETDATE(),
    N'superadmin'
FROM @OpConfigs src
OUTER APPLY (
    SELECT TOP 1 d.[Name], d.[Value], d.[Remark]
    FROM [dbo].[SysConfigData] d
    INNER JOIN [dbo].[SysConfigType] t ON d.[ConfigTypeId] = t.[ID]
    WHERE t.[Code] = N'sharedata_fe_config'
      AND d.[Code] = src.[Code]
      AND d.[IsDelete] IS NULL
      AND t.[IsDelete] IS NULL
    ORDER BY d.[UpdateTime] DESC, d.[CreateTime] DESC
) oldFe
WHERE NOT EXISTS (
    SELECT 1
    FROM [dbo].[SysOpConfig] target
    WHERE target.[GroupCode] = N'sharedata_config'
      AND target.[Code] = src.[Code]
      AND target.[IsDelete] IS NULL
);

DECLARE @InsertedOpConfigs INT = @@ROWCOUNT;
DECLARE @TotalOpConfigs INT = (SELECT COUNT(1) FROM @OpConfigs);
DECLARE @SkippedOpConfigs INT = @TotalOpConfigs - @InsertedOpConfigs;
PRINT N'   -> Đã seed ' + CAST(@InsertedOpConfigs AS NVARCHAR(10)) + N' tham số SysOpConfig mới; bỏ qua ' + CAST(@SkippedOpConfigs AS NVARCHAR(10)) + N' tham số đã có sẵn trong nhóm [sharedata_config].';

-- 4.5 Xóa mềm các tham số không còn sử dụng trong nhóm sharedata_config (idempotent)
UPDATE [dbo].[SysOpConfig]
SET [IsDelete] = GETDATE(),
    [Status] = 0,
    [UpdateTime] = GETDATE()
WHERE [GroupCode] = N'sharedata_config'
  AND [Code] IN (N'cleanup_enabled', N'cleanup_retention_days', N'packet_lookback_days_min', N'packet_lookback_days_max')
  AND [IsDelete] IS NULL;

DECLARE @SoftDeletedUnusedOpConfigs INT = @@ROWCOUNT;
IF @SoftDeletedUnusedOpConfigs > 0
    PRINT N'   -> Đã xóa mềm ' + CAST(@SoftDeletedUnusedOpConfigs AS NVARCHAR(10)) + N' tham số SysOpConfig không còn sử dụng (cleanup_enabled, cleanup_retention_days, packet_lookback_days_min, packet_lookback_days_max).';

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-cau-hinh-SysOpConfig.sql';
PRINT N'=============================================================================';
GO
