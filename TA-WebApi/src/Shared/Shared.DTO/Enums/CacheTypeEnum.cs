
using System.ComponentModel;

namespace Shared.DTO.Enums;

/// <summary>
/// Enums Cache
/// </summary>
[Description("CacheTypeEnum")]
public enum CacheTypeEnum
{
    /// <summary>
    /// Memory
    /// </summary>
    [Description("Memory")]
    Memory,

    /// <summary>
    /// Redis
    /// </summary>
    [Description("Redis")]
    Redis
}