namespace Modules.VDS.Core.Constants;

/// <summary>
/// Author: KienLT
/// Description: Khóa cấu hình VDS trong bảng <c>VdsConfig</c> — một nơi khai duy nhất.
///
/// <para><b>Vì sao gom:</b> trước 21/09/2026 các mã này nằm rải 21 chỗ trên 10 file (command handler, query
/// handler, service, filter, rules). Gõ sai một ký tự là điều kiện lọc lặng lẽ không khớp dòng nào — không
/// có lỗi build, không có lỗi runtime, chỉ ra kết quả rỗng. Đã từng xảy ra thật với
/// <c>vehicleType</c>/<c>vehicle_type</c> (sửa 18/09/2026).</para>
///
/// <para><c>public</c> chứ không <c>internal</c>: worker và test cần đối chiếu cùng bộ mã. Lớp
/// <c>VdsCatalogGroups</c> cũ là <c>internal</c> và không phủ <c>vds_config</c>/<c>vds_config_default</c>,
/// nên không dùng lại được — đã bỏ cùng API Catalog.</para>
/// Created date: 21/09/2026
/// </summary>
public static class VdsConfigConst
{
    /// <summary>Giá trị của cột <c>GroupCode</c>.</summary>
    public static class Group
    {
        /// <summary>Tham số vận hành: ngưỡng tốc độ tuyến, chu kỳ thống kê, thư mục media, nhóm camera theo hướng.</summary>
        public const string Config = "vds_config";

        /// <summary>Giá trị rơi về khi nhóm chi tiết không khai: thời lượng skip/dedup/display, ngưỡng quá hạn vùng.</summary>
        public const string ConfigDefault = "vds_config_default";

        /// <summary>Danh sách camera VDS; <c>Code</c> là ID thiết bị, <c>Value</c> không mang nghĩa.</summary>
        public const string ListEquipment = "vds_list_equipment";

        /// <summary>Mapping loại sự kiện vendor sang mã loại sự kiện của TMS.</summary>
        public const string MappingEventType = "vds_mapping_event_type";

        /// <summary>Mapping cỡ xe theo chiều dài; xem <see cref="VehicleSize"/> cho tập khóa hợp lệ.</summary>
        public const string MappingVehicleSize = "vds_mapping_vehicle_size";

        /// <summary>Thời lượng bỏ qua bản tin trùng theo loại sự kiện.</summary>
        public const string EventSkip = "vds_event_skip";

        /// <summary>Thời lượng chống trùng hồ sơ sự cố theo loại sự kiện.</summary>
        public const string EventDedup = "vds_event_dedup";

        /// <summary>Automation image reuse: Code = TMS EventType.Code, Value = positive integer seconds.</summary>
        public const string ImageReuse = "vds_image_reuse";


        /// <summary>Loại sự kiện cần kiểm tra biển số; <c>Value</c> được phép rỗng.</summary>
        public const string EventCheckPlate = "vds_event_check_plate";

        /// <summary>
        /// thời gian một sự kiện còn hiện trên SSE <c>api/vds/vdsEvent/Stream</c> kể từ lần phát hiện
        /// cuối. <c>Code</c> = mã <c>TmsEventType</c> nhóm event, <c>Value</c> = thời lượng có hậu tố (5s..1h). Thiếu dòng
        /// ⇒ <see cref="Code.EventDisplayDefault"/>.
        /// </summary>
        public const string EventDisplay = "vds_event_display";

        /// <summary>
        /// Nhóm "Đoạn tính tốc độ trung bình": hình học tuyến để TỰ SINH đoạn A→B (khoảng cách giữa hai camera làm
        /// mẫu số của tốc độ trung bình). Một nhóm, ba loại dòng phân biệt bằng tiền tố <c>Code</c>:
        /// <list type="bullet">
        /// <item><see cref="Pattern.KmDirPrefix"/><c>{tuyến|default}_{hướng}</c> — chiều km, <c>Value</c> = <c>asc</c>/<c>desc</c>.</item>
        /// <item><see cref="Pattern.JunctionPrefix"/><c>{mã nút}</c> — nút giao, <c>Value</c> = JSON <c>{"a","aPos","b","bPos"}</c>.</item>
        /// <item><see cref="Pattern.TurnPrefix"/><c>{mã lượt rẽ}</c> — lượt rẽ ĐƯỢC PHÉP, <c>Value</c> = JSON
        /// <c>{"junction","in","inDir","out","outDir"}</c> (<c>junction</c> = mã nút, không kèm tiền tố).</item>
        /// </list>
        /// </summary>
        public const string Segment = "vds_segment";
    }

    /// <summary>Giá trị của cột <c>Code</c> cho các tham số đơn lẻ.</summary>
    public static class Code
    {
        /// <summary>Thư mục gốc kho media; thuộc nhóm <see cref="Group.Config"/>.</summary>
        public const string MediaStorageDirectory = "vds_media_storage_directory";
        public const string MediaStorageUsername = "vds_media_storage_username";
        public const string MediaStoragePassword = "vds_media_storage_password";
        public const string MediaStorageDomain = "vds_media_storage_domain";

        /// <summary>
        /// Ngưỡng coi trạng thái vùng là quá hạn; thuộc nhóm <see cref="Group.ConfigDefault"/>. Service VDS đọc cùng
        /// khoá này để tính nhịp ghi giữ của vùng đang chậm/nghẽn — đổi tên/ý nghĩa phải sửa cả hai bên.
        /// </summary>
        public const string ZoneStaleSeconds = "vds_zone_stale_seconds";

        /// <summary>
        /// Khoảng nghỉ tối đa giữa hai lượt cùng biển trong MỘT chuyến lọc xe (dạng 10m/1h); thuộc nhóm
        /// <see cref="Group.ConfigDefault"/>. Service VDS dùng để gộp lượt thành chuyến và chỉ thông báo một lần mỗi chuyến.
        /// </summary>
        public const string VehicleFilterTripGap = "vds_vehicle_filter_trip_gap";

        /// <summary>Bật đánh giá vùng chậm theo tốc độ; thuộc nhóm <see cref="Group.Config"/>.</summary>
        public const string ZoneSlowSpeedEnabled = "vds_zone_slow_speed_enabled";

        /// <summary>Số xe tối thiểu trong cửa sổ để kết luận vùng chậm; thuộc nhóm <see cref="Group.Config"/>.</summary>
        public const string ZoneSlowMinVehicles = "vds_zone_slow_min_vehicles";

        /// <summary>Tỉ lệ xe dưới tốc độ tối thiểu để kết luận vùng chậm; thuộc nhóm <see cref="Group.Config"/>.</summary>
        public const string ZoneSlowShare = "vds_zone_slow_share";

        /// <summary>Biên km/h tốc độ TB phải vượt tốc độ tối thiểu để hết chậm; thuộc nhóm <see cref="Group.Config"/>.</summary>
        public const string ZoneSlowClearMarginKmh = "vds_zone_slow_clear_margin_kmh";

        /// <summary>
        /// Cửa sổ trượt tính tốc độ TB của vùng (thời lượng có hậu tố, không hậu tố = phút); thuộc nhóm
        /// <see cref="Group.Config"/>. Service VDS đọc cùng khoá này cho <c>ZoneStatus:AvgWindowSeconds</c>.
        /// </summary>
        public const string ZoneSlowWindow = "vds_zone_slow_window";

        /// <summary>Chốt chậm theo sự kiện <c>Slow Moving</c> (thời lượng có hậu tố); thuộc nhóm <see cref="Group.Config"/>.</summary>
        public const string ZoneSlowEventTtl = "vds_zone_slow_event_ttl";

        /// <summary>Chốt nghẽn theo sự kiện <c>Congestion</c> (thời lượng có hậu tố); thuộc nhóm <see cref="Group.Config"/>.</summary>
        public const string ZoneCongestionEventTtl = "vds_zone_congestion_event_ttl";

        /// <summary>Chu kỳ chốt số liệu thống kê (phút); thuộc nhóm <see cref="Group.Config"/>.</summary>
        public const string StatisticPeriodMinutes = "vds_statistic_period_minutes";

        /// <summary>Thời lượng skip mặc định; thuộc nhóm <see cref="Group.ConfigDefault"/>.</summary>
        public const string SkipDefault = "vds_skip_default";

        /// <summary>Thời lượng dedup mặc định; thuộc nhóm <see cref="Group.ConfigDefault"/>.</summary>
        public const string DedupDefault = "vds_dedup_default";

        /// <summary>Thời gian hiển thị sự kiện mặc định (5s..1h); thuộc nhóm <see cref="Group.ConfigDefault"/>.</summary>
        public const string EventDisplayDefault = "vds_event_display_default";


        /// <summary>Biên dưới thời gian đi qua đoạn khi đoạn chưa khai; đơn vị trong Value: s/m/h/d.</summary>
        public const string JourneyMinTravelDefault = "vds_journey_min_travel_default";

        /// <summary>Biên trên thời gian đi qua đoạn khi đoạn chưa khai; đơn vị trong Value: s/m/h/d.</summary>
        public const string JourneyMaxTravelDefault = "vds_journey_max_travel_default";

        /// <summary>
        /// khoảng nghỉ tối đa giữa hai hành trình liên tiếp của cùng một CHUYẾN (lưới gom nhóm trang
        /// Tốc độ trung bình); thuộc nhóm <see cref="Group.ConfigDefault"/>, thời lượng có hậu tố (1m..24h). Thiếu/sai ⇒ 2h.
        /// </summary>
        public const string JourneyTripGap = "vds_journey_trip_gap";

        /// <summary>
        /// Nhóm <see cref="Group.Segment"/>: tốc độ TRẦN hợp lý (km/h) để suy ra thời gian đi tối thiểu của đoạn tự
        /// sinh (<c>Min = khoảng cách ÷ tốc độ này</c>). Chuyến nhanh hơn bị coi là ghép nhầm biển (không hợp lệ).
        /// Thiếu dòng ⇒ 200. Tăng lên khi cần test bằng bản tin bắn sát nhau.
        /// </summary>
        public const string SegmentPlausibleMaxSpeed = "vds_segment_plausible_max_speed";
    }

    /// <summary>Tiền tố và hậu tố của các mã sinh theo tuyến/hướng.</summary>
    public static class Pattern
    {
        /// <summary>Tiền tố ngưỡng tốc độ theo tuyến: <c>vds_route_{id}_min_speed</c> / <c>_max_speed</c>.</summary>
        public const string RoutePrefix = "vds_route_";

        /// <summary>Hậu tố chung của cả ngưỡng min và max.</summary>
        public const string SpeedSuffix = "_speed";

        /// <summary>Hậu tố ngưỡng tốc độ tối thiểu.</summary>
        public const string MinSpeedSuffix = "_min_speed";

        /// <summary>Hậu tố ngưỡng tốc độ tối đa.</summary>
        public const string MaxSpeedSuffix = "_max_speed";

        /// <summary>Tiền tố danh sách camera theo hướng: <c>vds_count_direction_equipment_{direction}</c>.</summary>
        public const string CountDirectionEquipmentPrefix = "vds_count_direction_equipment_";

        /// <summary>Cặp camera A,B theo thứ tự để tính tốc độ hành trình dashboard, độc lập với nhóm đếm.</summary>
        public const string SpeedDirectionEquipmentPrefix = "vds_speed_direction_equipment_";

        /// <summary>Nhóm <see cref="Group.Segment"/>: chiều km — <c>vds_km_dir_default_1</c>, <c>vds_km_dir_second_route_1</c>.</summary>
        public const string KmDirPrefix = "vds_km_dir_";

        /// <summary>Nhóm <see cref="Group.Segment"/>: nút giao — <c>vds_junction_1</c>.</summary>
        public const string JunctionPrefix = "vds_junction_";

        /// <summary>Nhóm <see cref="Group.Segment"/>: lượt rẽ — <c>vds_turn_1_1</c>.</summary>
        public const string TurnPrefix = "vds_turn_";
    }

    /// <summary>Tập khóa hợp lệ trong nhóm <see cref="Group.MappingVehicleSize"/>.</summary>
    public static class VehicleSize
    {
        public const string Mode = "mode";
        public const string Unit = "unit";
        public const string ShortMax = "shortMax";
        public const string MediumMax = "mediumMax";
    }

    // KienLT - 31/10/2026: tham số vận hành QuestDB HA chuyển từ appsettings sang VdsConfig (CODING_RULES §11).
    /// <summary>
    /// Author: KienLT
    /// Description: Mã tham số VẬN HÀNH QuestDB HA, nhóm <see cref="Group.Config"/>. Thời lượng bắt buộc hậu tố s/m/h/d; số/byte là
    /// số nguyên; cờ là 1/0; giờ là HH:mm (lịch backup <c>off</c> = tắt). Thiếu dòng ⇒ section <c>QuestHa</c> của appsettings. Worker
    /// (<c>OpConfigManager</c>) và WebAPI (router đọc) cùng đọc bộ mã này qua <c>VdsBaseHelper.QuestHaConfigHp</c>.
    /// Created date: 31/10/2026
    /// </summary>
    public static class QuestHa
    {
        /// <summary>Tiền tố chung của mọi mã QuestDB HA.</summary>
        public const string Prefix = "vds_questha_";
        /// <summary>Tiền tố mã router (WebAPI chỉ áp nhóm này cho đường đọc của nó).</summary>
        public const string RouterPrefix = "vds_questha_router_";
        // KienLT - 31/10/2026: router/snapshot QuestDB HA là singleton toàn host ⇒ chỉ MỘT tenant sở hữu cấu hình HA của host.
        /// <summary>
        /// Khoá cấu hình hạ tầng (appsettings/Database.json) chọn tenant sở hữu tham số QuestDB HA của host. Trống ⇒ Worker dùng
        /// <c>Service:TenantId</c>, WebAPI dùng <see cref="DefaultSettingsTenantId"/>. Dòng <c>vds_questha_*</c> của tenant khác không được áp.
        /// </summary>
        public const string SettingsTenantConfigKey = "QuestHa:SettingsTenantId";
        /// <summary>Tenant sở hữu mặc định ở WebAPI (trùng mặc định <c>Service:TenantId</c> của Worker).</summary>
        public const string DefaultSettingsTenantId = "Default";

        public const string HealthCheckInterval = "vds_questha_health_check_interval";
        public const string HealthHeartbeatStaleAfter = "vds_questha_health_heartbeat_stale_after";
        public const string HealthLagWarnSeconds = "vds_questha_health_lag_warn_seconds";
        public const string HealthLagCriticalSeconds = "vds_questha_health_lag_critical_seconds";
        public const string HealthBackupWarnAfter = "vds_questha_health_backup_warn_after";
        public const string HealthBackupCriticalAfter = "vds_questha_health_backup_critical_after";
        public const string HealthBackupRequired = "vds_questha_health_backup_required";
        // KienLT - 31/10/2026: ngưỡng đối soát và nhịp cảnh báo Health cũng là tham số vận hành (CODING_RULES §11).
        public const string HealthReconcileWarnAfter = "vds_questha_health_reconcile_warn_after";
        public const string HealthRepeatWarningEvery = "vds_questha_health_repeat_warning_every";
        public const string HealthMinAlertGap = "vds_questha_health_min_alert_gap";

        public const string JournalInterval = "vds_questha_journal_interval";
        public const string JournalRetention = "vds_questha_journal_retention";
        public const string JournalProtectedBackupDays = "vds_questha_journal_protected_backup_days";
        public const string JournalBatchSize = "vds_questha_journal_batch_size";
        public const string JournalMaxRowsPerRun = "vds_questha_journal_max_rows_per_run";
        public const string JournalMaxRunDuration = "vds_questha_journal_max_run_duration";
        public const string JournalAlertThrottle = "vds_questha_journal_alert_throttle";
        public const string JournalWarnPendingRows = "vds_questha_journal_warn_pending_rows";
        public const string JournalCriticalPendingRows = "vds_questha_journal_critical_pending_rows";
        public const string JournalWarnPendingAge = "vds_questha_journal_warn_pending_age";
        public const string JournalCriticalPendingAge = "vds_questha_journal_critical_pending_age";
        public const string JournalWarnBytes = "vds_questha_journal_warn_bytes";
        public const string JournalCriticalBytes = "vds_questha_journal_critical_bytes";
        public const string JournalDryRun = "vds_questha_journal_dry_run";

        public const string BackupSchedule = "vds_questha_backup_schedule";
        public const string BackupWeeklyDay = "vds_questha_backup_weekly_day";
        public const string BackupWindowStart = "vds_questha_backup_window_start";
        public const string BackupWindowEnd = "vds_questha_backup_window_end";
        public const string BackupNodeOffset = "vds_questha_backup_node_offset";
        public const string BackupMaxAttempts = "vds_questha_backup_max_attempts";
        public const string BackupRetryDelay = "vds_questha_backup_retry_delay";
        public const string BackupDailyCopies = "vds_questha_backup_daily_copies";
        public const string BackupWeeklyCopies = "vds_questha_backup_weekly_copies";
        public const string BackupManualCopies = "vds_questha_backup_manual_copies";
        public const string BackupMaxStoppedDuration = "vds_questha_backup_max_stopped_duration";
        public const string BackupMinThroughputBytesPerSecond = "vds_questha_backup_min_throughput_bytes_per_second";
        public const string BackupMinimumFreeBytes = "vds_questha_backup_minimum_free_bytes";
        public const string BackupDryRun = "vds_questha_backup_dry_run";

        public const string RouterCoverageWait = "vds_questha_router_coverage_wait";
        public const string RouterFailbackStableFor = "vds_questha_router_failback_stable_for";
        public const string RouterBreakerCooldown = "vds_questha_router_breaker_cooldown";

        // KienLT - 02/11/2026: tham số vận hành của backup xuất Parquet hằng ngày (bật/tắt và thư mục là hạ tầng QuestHa:Export).
        public const string ExportSchedule = "vds_questha_export_schedule";
        public const string ExportRetentionDays = "vds_questha_export_retention_days";
        public const string ExportRecheckDays = "vds_questha_export_recheck_days";
        public const string ExportMaxAttempts = "vds_questha_export_max_attempts";
        public const string ExportRetryDelay = "vds_questha_export_retry_delay";
        public const string ExportWarnAfter = "vds_questha_export_warn_after";
        public const string ExportCriticalAfter = "vds_questha_export_critical_after";
        public const string ExportDryRun = "vds_questha_export_dry_run";
    }

    /// <summary>
    /// Mã <c>SysConfigType</c> chứa danh sách nhóm cấu hình VDS cho dropdown.
    ///
    /// <para>Đây là mã của bảng danh mục CfgSystem, KHÔNG phải giá trị <c>GroupCode</c> của
    /// <c>VdsConfig</c> — đặt riêng để không bị dùng lẫn.</para>
    /// </summary>
    public const string ConfigGroupTypeCode = "vds_config_group";

    /// <summary>
    /// Mã <c>SysConfigType</c> của danh mục vị trí tuyến/hầm (<c>main_route</c> → "CT01"): đặt tên tuyến trên sơ đồ
    /// và tên vị trí trong snapshot sự kiện.
    /// </summary>
    public const string ZoneLocationTypeCode = "tms_zone_location";
}
