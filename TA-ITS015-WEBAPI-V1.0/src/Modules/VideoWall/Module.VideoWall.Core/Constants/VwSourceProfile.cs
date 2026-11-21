namespace Module.VideoWall.Core.Constants
{
    /// <summary>
    /// Phân loại nguồn tín hiệu VwSource theo cách nó vào tường màn hình.
    /// Created date: 09/09/2026
    /// </summary>
    public static class VwSourceProfile
    {
        /// <summary>
        /// Các loại nguồn CỤC BỘ: tín hiệu đi vào tường bằng dây cắm thẳng vào card đầu vào
        /// của khung máy, không qua mạng.
        ///
        /// VÌ SAO CẦN DANH SÁCH NÀY: trong cấu hình cascade DS-C66S, toàn bộ card đầu vào nằm ở
        /// bộ điều khiển TRUNG TÂM; bộ con chỉ nhận ảnh đã ghép từ bộ trung tâm truyền xuống
        /// (KienTruc_VideoWall_DS-C66S-Cascade.md §2). Nên nguồn cục bộ chỉ gắn được vào bộ
        /// trung tâm — gắn vào bộ con là mô tả sai hiện trạng đấu nối.
        /// Nguồn qua mạng (rtsp/stream) không bị ràng buộc này.
        ///
        /// Mã phải khớp `Code` của danh mục `videowall_source_type` (SysConfigData) và FE
        /// `VW_SOURCE_TYPE.LocalSignal`. 25/09/2026: đổi từ ["hdmi_in", "local", "video_file"] —
        /// các mã cũ không tồn tại trong dữ liệu nên IsLocal() luôn false và ràng buộc trên không chạy.
        /// </summary>
        public static readonly string[] LocalSourceTypes = ["local_signal"];

        /// <summary>
        /// Description: Nguồn có phải loại cục bộ (cắm dây vào card đầu vào) hay không.
        /// Created date: 09/09/2026
        /// </summary>
        public static bool IsLocal(string? sourceType) =>
            !string.IsNullOrWhiteSpace(sourceType)
            && LocalSourceTypes.Contains(sourceType, StringComparer.OrdinalIgnoreCase);
    }
}
