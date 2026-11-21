using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    [SugarTable("TmsWorkExecute")]
    public class TmsWorkExecute : EntityTenant
    {
        //TmsWorkExecute {
        // ID varchar(64) pk
        // IncidentId varchar(64) null
        // TrafficPlanId varchar(64) null
        // WorkDetailId varchar(64) null
        // Step integer null
        // WorkName string(256) null
        // Description string(512) null
        // Remark string(512) null
        // ExecuteOrg varchar(64) null
        // ExecuteBy varchar(64) null
        // ExecuteDate datetime null
        // State varchar(16) null def(0)
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? IncidentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? TrafficPlanId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? WorkDetailId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Step { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? WorkName { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Description { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ExecuteOrg { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ExecuteBy { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ExecuteDate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? State { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? WorkMode { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? ControlType { get; set; }

        /// <summary>
        /// Nguồn tạo work execute: Manual hoặc Automatic.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, DefaultValue = "Manual", ColumnDescription = "Nguồn tạo: Manual/Automatic")]
        public string ExecutionSource { get; set; } = "Manual";

        /// <summary>
        /// Liên kết lần chạy tự động (TmsIncidentAutomationRun.ID) nếu work execute này do luồng tự động sinh ra.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Liên kết lần chạy tự động")]
        public string? AutomationRunId { get; set; }

    }
}
