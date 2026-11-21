using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Helpers;
using ShareDataWorker.Core.Enums;

namespace ShareDataWorker.Core.Models.DataOutbound
{
    /// <summary>
    /// Description: Ngữ cảnh xuất bản dữ liệu mang thông tin Subscription, Partner, Packet, Mapping, Policy và PartnerSql xuyên suốt pipeline.
    /// Created date: 16/09/2026
    /// Modified date: 17/11/2026
    /// </summary>
    /// <param name="Subscription">Cấu hình đăng ký chia sẻ dữ liệu.</param>
    /// <param name="Partner">Đối tác nhận dữ liệu.</param>
    /// <param name="Packet">Cấu hình gói tin dữ liệu.</param>
    /// <param name="Mapping">Cấu hình ánh xạ trường dữ liệu.</param>
    /// <param name="ExportedAt">Thời điểm xuất bản.</param>
    /// <param name="CancelToken">CancellationToken.</param>
    /// <param name="Policy">Chính sách gói tin (Snapshot/AlwaysIncremental).</param>
    /// <param name="PartnerSql">SQL trích xuất hiệu lực đọc từ ShareDataSql (câu riêng của đối tác nếu có, không thì câu mẫu chung).</param>
    /// <param name="Variables">Biến đã khai ở bảng tham số câu truy vấn (mẫu chung, ghi đè và biến riêng của đối tác).</param>
    /// <param name="CursorSignature">Chữ ký công thức cột mốc của PartnerSql (SqlValidatorHelper.GetCursorSignature), ghi cùng mốc vào ShareDataLastSend.</param>
    /// <param name="GroupSignatures">Chữ ký công thức của từng nhóm mốc có tên (câu gộp nhiều nguồn), ghi cùng mốc nhóm vào ShareDataLastSend (CursorKey = &lt;nhóm&gt;).</param>
    /// <param name="UsesDefaultCursor">Câu dùng nhóm mốc mặc định (cột __lastKey không hậu tố); câu chỉ có nhóm có tên thì false.</param>
    /// <param name="CodeSets">Danh sách bộ mã chuẩn hóa nạp trước cho vòng quét; null thì tra CSDL khi ánh xạ.</param>
    public sealed record DataOutboundContext(
        ShareDataSubscription Subscription,
        ShareDataPartner? Partner,
        ShareDataPacket Packet,
        ShareDataMapping? Mapping,
        DateTime ExportedAt,
        CancellationToken CancelToken,
        ShareDataEnum.PacketPolicy Policy = ShareDataEnum.PacketPolicy.Snapshot,
        string? PartnerSql = null,
        SqlVariableSet? Variables = null,
        string? CursorSignature = null,
        IReadOnlyDictionary<string, string?>? GroupSignatures = null,
        bool UsesDefaultCursor = true,
        IReadOnlyList<ShareDataCodeSet>? CodeSets = null)
    {
        public string? PacketCode => Packet.Code;
        public string? PartnerCode => Partner?.Code;
    }
}
