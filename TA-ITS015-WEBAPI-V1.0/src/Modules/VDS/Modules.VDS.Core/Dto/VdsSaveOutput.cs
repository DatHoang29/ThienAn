namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả một thao tác ghi kèm kết quả gửi refresh.
/// Created date: 22/09/2026
/// </summary>
public class VdsSaveOutput : VdsRefreshOutput
{
    /// <summary>Dữ liệu đã ghi xuống DB thành công hay chưa.</summary>
    public bool Saved { get; set; }
}
