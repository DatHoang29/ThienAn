using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Modules.Notification.Core.Entities
{
    /// <summary>
    /// Author: SonTH
    /// Description: Một thông báo (phần nội dung dùng chung cho mọi người nhận).
    ///
    ///              Title/Content lưu ĐÃ RENDER theo ngôn ngữ lúc tạo — đọc danh sách không phải
    ///              re-render từng dòng. Không lưu template key/params: module chỉ ghi nhận + hiển thị,
    ///              không có nhu cầu dịch lại; module gọi muốn truy vết thì tự giữ key của nó.
    ///
    ///              KHÔNG FK cứng tới SysUser/SysOrg: dữ liệu user/org thuộc phân hệ System,
    ///              người nhận chi tiết nằm ở NotiRecipient (fan-out row-per-recipient).
    ///
    ///              BẢNG TỰ GENERATE bằng SqlSugar CodeFirst (Database.json: ConfigId NotiDefault
    ///              bật EnableInitDb + EnableInitTable). Index khai báo ngay trên entity bằng
    ///              [SugarIndex]; riêng unique index có điều kiện WHERE IdempotencyKey IS NOT NULL
    ///              (SQL Server unique thường chỉ cho 1 dòng NULL) do NotiSystemWarmUpService tạo.
    /// Created date: 17/09/2026
    /// </summary>
    [SugarTable("NotiMessage")]
    [Tenant("NotiDefault")]
    [SugarIndex("IX_NotiMessage_Category_SendTime", nameof(Category), OrderByType.Asc, nameof(SendTime), OrderByType.Asc)]
    public class NotiMessage : EntityTenant
    {
        /// <summary>Tiêu đề đã render.</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length256)]
        public string Title { get; set; } = null!;

        /// <summary>Nội dung đã render.</summary>
        [SugarColumn(IsNullable = false, ColumnDataType = "nvarchar(max)")]
        public string Content { get; set; } = null!;

        /// <summary>Phân loại {Module}.{Loại} (vd TMS.Incident) để frontend lọc/nhóm.</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length64)]
        public string Category { get; set; } = null!;

        /// <summary>Mức độ: Info | Warning | Critical (NotiEnums.SeverityEnum).</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length16)]
        public string Severity { get; set; } = null!;

        /// <summary>Phạm vi gửi: User | Org | All (NotiEnums.TargetTypeEnum).</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length16)]
        public string TargetType { get; set; } = null!;

        /// <summary>Định danh bản ghi nguồn (vd IncidentId) để frontend điều hướng tới.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? SourceKey { get; set; }

        /// <summary>
        /// Chống gửi trùng: caller truyền "{Module}:{SourceKey}:{event}".
        /// Gặp key trùng thì handler trả kết quả cũ, không fan-out lần hai.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? IdempotencyKey { get; set; }

        /// <summary>Thời điểm gửi (BaseUtil.TimeServer). Cột duy nhất quyết định retention.</summary>
        [SugarColumn(IsNullable = false)]
        public DateTime SendTime { get; set; }
    }
}
