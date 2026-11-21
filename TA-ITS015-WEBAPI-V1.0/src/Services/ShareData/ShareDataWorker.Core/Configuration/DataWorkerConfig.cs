using Module.ShareData.Core.Constants;

namespace ShareDataWorker.Core.Configuration
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Tham số vận hành của ShareDataWorker, đọc từ SysOpConfig nhóm sharedata_config qua DataWorkerConfigService.
    ///              Thiếu hoặc sai giá trị thì dùng mặc định và khoảng an toàn trong ShareDataConst.
    /// Created date: 18/10/2026
    /// Modified date: 09/11/2026
    /// </summary>
    public class DataWorkerConfig
    {
        /* ── Gửi đi ── */

        /// <summary>Chu kỳ quét gửi đi, giây (outbound_check_interval_sec). Mặc định 5.</summary>
        public int OutboundCheckIntervalSec { get; set; } = ShareDataConst.DefaultOutboundCheckIntervalSec;

        /* ── Nhận về ── */

        /// <summary>Chu kỳ quét nhận về, giây (inbound_check_interval_sec). Mặc định 5.</summary>
        public int InboundCheckIntervalSec { get; set; } = ShareDataConst.DefaultInboundCheckIntervalSec;

        /* ── Change Tracking ── */

        /// <summary>Chu kỳ quét Change Tracking, giây (tracking_poll_interval_sec). Mặc định 1.</summary>
        public int TrackingPollIntervalSec { get; set; } = ShareDataConst.DefaultTrackingPollIntervalSec;

        /* ── Gửi đi ── */

        /// <summary>Thời gian giữ khóa gửi tối thiểu, giây (outbound_lock_min_sec). Mặc định 300.</summary>
        public int OutboundLockMinSec { get; set; } = ShareDataConst.DefaultOutboundLockMinSec;

        /// <summary>Hệ số nhân chu kỳ gửi tính thời gian giữ khóa (outbound_lock_interval_multiplier). Mặc định 3.</summary>
        public int OutboundLockIntervalMultiplier { get; set; } = ShareDataConst.DefaultOutboundLockIntervalMultiplier;

        /// <summary>Chu kỳ gửi mặc định của Worker, giây (outbound_default_interval_sec). Mặc định 30.</summary>
        public int OutboundDefaultIntervalSec { get; set; } = ShareDataConst.DefaultOutboundDefaultIntervalSec;

        /* ── Nhận về ── */

        /// <summary>Số gói tin nhận xử lý tối đa mỗi lượt (inbound_max_packets_per_run). Mặc định 50.</summary>
        public int InboundMaxPacketsPerRun { get; set; } = ShareDataConst.DefaultInboundMaxPacketsPerRun;

        /// <summary>Ngưỡng thu hồi gói nhận bị kẹt, phút (inbound_stuck_timeout_min). Mặc định 10.</summary>
        public int InboundStuckTimeoutMin { get; set; } = ShareDataConst.DefaultInboundStuckTimeoutMin;

        /// <summary>Chu kỳ tối thiểu ghi cảnh báo lặp Inbound, phút (inbound_warn_interval_min). Mặc định 5.</summary>
        public int InboundWarnIntervalMin { get; set; } = ShareDataConst.DefaultInboundWarnIntervalMin;

        /// <summary>Thời gian chờ ghi SQL chiều nhận, giây (inbound_write_timeout_sec). Mặc định 120.</summary>
        public int InboundWriteTimeoutSec { get; set; } = ShareDataConst.DefaultInboundWriteTimeoutSec;

        /* ── Change Tracking ── */

        /// <summary>Chu kỳ thử lại bảng nguồn thiếu Change Tracking, phút (tracking_table_retry_min). Mặc định 5.</summary>
        public int TrackingTableRetryMin { get; set; } = ShareDataConst.DefaultTrackingTableRetryMin;

        /// <summary>Số ngày lưu giữ nhật ký hạ tầng Change Tracking, ngày (tracking_log_retention_days). Mặc định 7.</summary>
        public int TrackingLogRetentionDays { get; set; } = ShareDataConst.DefaultTrackingLogRetentionDays;

        /// <summary>Số dòng xóa mỗi lô nhật ký Change Tracking (tracking_log_purge_batch). Mặc định 1000.</summary>
        public int TrackingLogPurgeBatch { get; set; } = ShareDataConst.DefaultTrackingLogPurgeBatch;

        /// <summary>Số ngày giữ lịch sử CHANGE_RETENTION khi bật CT, ngày (tracking_change_retention_days). Mặc định 2.</summary>
        public int TrackingChangeRetentionDays { get; set; } = ShareDataConst.DefaultTrackingChangeRetentionDays;

        /* ── Dọn dẹp ── */

        /// <summary>Chu kỳ chạy dọn dẹp định kỳ, phút (cleanup_interval_min). Mặc định 1440.</summary>
        public int CleanupIntervalMin { get; set; } = ShareDataConst.DefaultCleanupIntervalMin;

        /* ── Nạp lại cấu hình ── */

        /// <summary>Chu kỳ nạp lại cấu hình và danh sách bảng nguồn, giây (reload_interval_sec). Mặc định 30.</summary>
        public int ReloadIntervalSec { get; set; } = ShareDataConst.DefaultReloadIntervalSec;
    }
}
