namespace Modules.CfgSystem.Core.Dto.ConfigData;

/// <summary>
/// Author: KienLT
/// Description: Yêu cầu danh mục mã và tên theo mã loại cấu hình (<c>SysConfigType.Code</c>), giữ thứ tự hiển thị.
/// Created date: 22/10/2026
/// </summary>
public sealed class CfgConfigDataNamesInput
{
    public string TypeCode { get; set; } = string.Empty;
}
