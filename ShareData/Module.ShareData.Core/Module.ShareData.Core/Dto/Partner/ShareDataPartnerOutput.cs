using Module.ShareData.Core.Entities;

namespace Module.ShareData.Core.Dto.Partner
{
    /// <summary>
    /// Description: DTO trả về 1 đối tác (phân trang)
    /// Created date: 2026-08-04
    /// </summary>
    /// <remarks>PasswordHash đã được đánh dấu JsonIgnore tại entity nên không lộ ra FE.</remarks>
    public class ShareDataPagePartnerOutput : ShareDataPartner { }

    /// <summary>
    /// Description: DTO trả về 1 đối tác (danh sách / chi tiết)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPartnerOutput : ShareDataPartner { }

    /// <summary>
    /// Description: DTO trả về sau khi thêm mới 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataAddPartnerOutput : ShareDataPartnerOutput { }

    /// <summary>
    /// Description: DTO trả về sau khi cập nhật 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataUpdatePartnerOutput : ShareDataPartnerOutput { }

    /// <summary>
    /// Description: DTO trả về sau khi xóa 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDeletePartnerOutput : ShareDataPartnerOutput { }

    /// <summary>
    /// Description: DTO trả về kết quả từng bước khi thử kết nối
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataTestConnectionStepOutput
    {
        /// <summary>Tên bước: initiate | login | logout.</summary>
        public string? Step { get; set; }

        /// <summary>Bước có thành công không.</summary>
        public bool Ok { get; set; }

        /// <summary>Thông tin bổ sung khi lỗi.</summary>
        public string? Message { get; set; }
    }
}
