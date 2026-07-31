using SqlSugar;
using TA_ShareData_WorkerService.Core.Enums;

namespace TA_ShareData_WorkerService.Core.Entities
{
    /// <summary>
    /// Đăng ký chia sẻ dữ liệu / Hợp đồng kết nối (ESHARE V1)
    /// Author: Đạt
    /// Created date: 27/07/2026
    /// </summary>
    [SugarTable("EshSubscription")]
    [SugarIndex("UX_EshSubscription_SerialNbr", "SerialNbr", OrderByType.Asc, "IsDelete", OrderByType.Asc, true)]
    [SugarIndex("IX_EshSubscription_State", nameof(State), OrderByType.Asc)]
    public class EshSubscription : EntityTenant
    {
        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? PartnerId { get; set; }

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? Direction { get; set; } = EshEnums.SubDirection.Outbound;

        [SugarColumn(Length = EntityConst.Length64, IsNullable = true)]
        public string? SerialNbr { get; set; }

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? DatatypeId { get; set; } = EshEnums.DatatypeIdEnum.TrafficInfo.ToString();

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? Mode { get; set; } = EshEnums.SubMode.Batch;

        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? ScheduleJson { get; set; }

        public int IntervalSeconds { get; set; } = 300;

        [SugarColumn(IsNullable = true)]
        public DateTime? LastTimeRun { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? NextTimeRun { get; set; }

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? RunStatus { get; set; } = EshEnums.RunStatus.Idle;

        [SugarColumn(Length = EntityConst.Length64, IsNullable = true)]
        public string? ProcessLockId { get; set; }

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? Format { get; set; } = EshEnums.PackagingFormat.Raw;

        public int Priority { get; set; } = 3;

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? State { get; set; } = EshEnums.SubState.Pending;

        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? DataSourceId { get; set; }

        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? MappingProfileId { get; set; }

        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? EventSourceId { get; set; }

        public int DebounceSec { get; set; } = 0;

        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? RejectReason { get; set; }

        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? CancelReason { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? RequestedAt { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ResolvedAt { get; set; }

        [SugarColumn(Length = EntityConst.Length64, IsNullable = true)]
        public string? ResolvedBy { get; set; }
    }
}
