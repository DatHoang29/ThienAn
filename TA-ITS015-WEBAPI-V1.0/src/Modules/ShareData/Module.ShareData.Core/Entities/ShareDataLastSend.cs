using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Description: Mốc bản ghi cuối cùng đã gửi thành công theo cặp đối tác, gói tin và nhóm mốc (CursorKey = null với nhóm mặc định, CursorKey = &lt;nhóm&gt; với câu SQL gộp nhiều nguồn).
    /// Created date: 21/09/2026
    /// Modified date: 03/11/2026
    /// </summary>
    [SugarTable("ShareDataLastSend")]
    [SugarIndex("index_{table}_Partner_Packet_Key", nameof(PartnerCode), OrderByType.Asc, nameof(PacketCode), OrderByType.Asc, nameof(CursorKey), OrderByType.Asc, true)]
    public class ShareDataLastSend : EntityTenant
    {
        /// <summary>
        /// Mã đối tác nhận dữ liệu (ShareDataPartner.Code).
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? PartnerCode { get; set; }

        /// <summary>
        /// Mã gói tin dữ liệu (ShareDataPacket.Code).
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? PacketCode { get; set; }

        /// <summary>
        /// Tên nhóm mốc (hậu tố sau dấu gạch dưới của cột __lastKey_&lt;nhóm&gt; và biến @lastKey_&lt;nhóm&gt;) cho câu SQL gộp nhiều nguồn; để trống (null) với nhóm mốc mặc định.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? CursorKey { get; set; }

        /// <summary>
        /// Giờ Worker ghi mốc nhóm mặc định (lượt chạy đã gửi thành công), chỉ để tra cứu, không dùng để lọc dữ liệu.
        /// Nhóm mốc có tên và Đặt mốc để trống.
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? LastTime { get; set; }

        /// <summary>
        /// Mốc nối đuôi: giá trị cột __lastKey (hoặc __lastKey_&lt;nhóm&gt;) của dòng cuối đã gửi thành công, ví dụ 2026-10-21 07:58:45.487_&lt;ID&gt;.
        /// Lượt sau truyền vào biến @lastKey để lấy các dòng sau mốc; trống thì dùng Mốc bắt đầu (DefaultValue của biến), cũng trống thì gửi từ đầu.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? LastKey { get; set; }

        /// <summary>
        /// Chữ ký công thức cột mốc __lastKey của câu SQL đã ghi ra LastKey — SqlValidatorHelper.GetCursorSignature.
        /// Người dùng đổi công thức mốc thì chữ ký đổi: mốc cũ không so được với công thức mới nên Worker bỏ mốc cũ, gửi lại từ mốc khởi tạo.
        /// Trống ở dòng ghi trước 29/10/2026 (chưa biết công thức) thì dùng mốc như cũ.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? CursorSignature { get; set; }
    }
}
