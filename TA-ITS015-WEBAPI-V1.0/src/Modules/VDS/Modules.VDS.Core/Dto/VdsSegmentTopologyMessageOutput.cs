using BaseMsg = Modules.VDS.Core.Exceptions.BaseMsg;

namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Mã (<see cref="BaseMsg.Vds.Message"/>) và tham số của thông báo sinh đoạn/sơ đồ tuyến. Tham số có thể lồng
/// một thông báo khác. WebAPI dịch theo ngôn ngữ request khi dựng kết quả; worker định dạng tiếng Việt để ghi log
/// (<see cref="VdsSegmentTopologyHelper.FormatVi"/>).
/// Created date: 22/10/2026
/// </summary>
public sealed record VdsSegmentTopologyMessageOutput(string Key, params object[] Arguments)
{
    /// <summary>
    /// Author: KienLT
    /// Description: Bọc văn bản đã dựng sẵn (chưa có mã riêng) để đi chung danh sách thông báo; khi dịch giữ nguyên văn bản.
    /// Created date: 22/10/2026
    /// </summary>
    public static VdsSegmentTopologyMessageOutput RawText(string text) => new(BaseMsg.Vds.Message.TopologyRawText, text);
}
