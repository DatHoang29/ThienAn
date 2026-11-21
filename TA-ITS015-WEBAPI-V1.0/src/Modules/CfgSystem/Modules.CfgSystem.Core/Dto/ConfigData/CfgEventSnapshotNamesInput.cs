namespace Modules.CfgSystem.Core.Dto.ConfigData;

/// <summary>
/// Author: KienLT
/// Description: Yêu cầu tên danh mục theo các mã nhóm của snapshot VDS.
/// Created date: 22/10/2026
/// </summary>
public sealed class CfgEventSnapshotNamesInput
{
    public string[] Types { get; set; } = [];
}
