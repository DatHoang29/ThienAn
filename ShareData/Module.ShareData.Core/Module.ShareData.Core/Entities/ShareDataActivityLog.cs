using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Description: Nhật ký hoạt động của phân hệ Chia sẻ dữ liệu — phục vụ 2 tab màn Lịch sử:
    ///              - LogType = CONFIG   → tab "Cấu hình": thêm/sửa/xóa đối tác, đăng ký, nguồn dữ liệu, ánh xạ...
    ///              - LogType = TRANSFER → tab "Truyền nhận": thao tác gửi/nhận gói tin, xuất file.
    ///              Ghi tường minh từ các CommandHandler qua ShareDataActivityLogger.
    ///              Bảng CHỈ GHI THÊM — không sửa, không xóa qua API.
    /// Created date: 2026-08-05
    /// </summary>
    [SugarTable("ShareDataActivityLog")]
    [SugarIndex("index_{table}_LT", nameof(LogType), OrderByType.Asc)]
    [SugarIndex("index_{table}_OA", nameof(OccurredAt), OrderByType.Desc)]
    [SugarIndex("index_{table}_PI", nameof(PartnerId), OrderByType.Asc)]
    public class ShareDataActivityLog : EntityTenant
    {
        // ── Phân loại chung ──────────────────────────────────────────────

        /// <summary>Nhóm nhật ký: CONFIG | TRANSFER. Quyết định bản ghi thuộc tab nào.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "CONFIG | TRANSFER")]
        public string? LogType { get; set; }

        /// <summary>
        /// Hành động: CREATE | UPDATE | DELETE | CONNECT | DISCONNECT | PAUSE | RESUME
        ///          | CANCEL | APPROVE | REJECT | SEND | RECEIVE | EXPORT.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Action { get; set; }

        /// <summary>Thời điểm phát sinh.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? OccurredAt { get; set; }

        /// <summary>Kết quả: SUCCESS | FAILED.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "SUCCESS | FAILED")]
        public string? Status { get; set; }

        /// <summary>Nội dung mô tả tiếng Việt, dựng sẵn để hiển thị thẳng lên lưới.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Description { get; set; }

        /// <summary>Thông báo lỗi khi Status = FAILED.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? ErrorMessage { get; set; }

        // ── Đối tượng tác động ───────────────────────────────────────────

        /// <summary>Loại đối tượng: Partner | Subscription | DataSource | MappingProfile | EventSource | Session.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? TargetType { get; set; }

        /// <summary>ID bản ghi bị tác động.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? TargetId { get; set; }

        /// <summary>Tên/mã bản ghi tại thời điểm ghi log — lưu kèm để không phải join khi tra cứu.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? TargetName { get; set; }

        /// <summary>Đối tác liên quan (ShareDataPartner.ID) — dùng cho bộ lọc "Đối tác".</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }

        /// <summary>Tên đối tác tại thời điểm ghi log.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? PartnerName { get; set; }

        // ── Phần riêng cho LogType = CONFIG ──────────────────────────────

        /// <summary>Giá trị trước khi thay đổi (JSON). Rỗng khi Action = CREATE.</summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Giá trị trước thay đổi dạng JSON")]
        public string? BeforeJson { get; set; }

        /// <summary>Giá trị sau khi thay đổi (JSON). Rỗng khi Action = DELETE.</summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Giá trị sau thay đổi dạng JSON")]
        public string? AfterJson { get; set; }

        /// <summary>Danh sách trường đã đổi, ngăn cách bởi dấu phẩy — để hiển thị nhanh không cần diff lại JSON.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? ChangedFields { get; set; }

        // ── Phần riêng cho LogType = TRANSFER ────────────────────────────

        /// <summary>Đăng ký chia sẻ phát sinh gói tin (ShareDataSubscription.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SubscriptionId { get; set; }

        /// <summary>Phiên kết nối truyền gói tin (ShareDataSession.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SessionId { get; set; }

        /// <summary>Chiều truyền: SND (gửi đi) | RCV (nhận về).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length8, ColumnDescription = "SND | RCV")]
        public string? TransferDirection { get; set; }

        /// <summary>Loại dữ liệu chia sẻ (101–111).</summary>
        [SugarColumn(IsNullable = true)]
        public int? DatatypeId { get; set; }

        /// <summary>Serial number của gói xuất bản.</summary>
        [SugarColumn(IsNullable = true)]
        public int? SerialNbr { get; set; }

        /// <summary>Số thứ tự gói tin trong phiên.</summary>
        [SugarColumn(IsNullable = true)]
        public int? PacketNbr { get; set; }

        /// <summary>Loại gói PDU: initiate | login | fred | publication | subscription | logout...</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? PduType { get; set; }

        /// <summary>Định dạng dữ liệu: DATA | FILE.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length8)]
        public string? Format { get; set; }

        /// <summary>Dung lượng gói tin (byte).</summary>
        [SugarColumn(IsNullable = true)]
        public long? ByteSize { get; set; }

        /// <summary>Số bản ghi trong gói.</summary>
        [SugarColumn(IsNullable = true)]
        public int? RecordCount { get; set; }

        /// <summary>Đường dẫn file bằng chứng khi Action = EXPORT.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? FilePath { get; set; }

        /// <summary>Hash nội dung file bằng chứng.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Hash { get; set; }

        // ── Người thực hiện ──────────────────────────────────────────────

        /// <summary>Tài khoản thực hiện. Luồng tự động ghi 'system'.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? OperatorName { get; set; }

        /// <summary>Địa chỉ IP của người thực hiện (rỗng với luồng tự động).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? OperatorIp { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
