
using System.ComponentModel;

namespace Shared.DTO.Enums;

/// <summary>
/// Filter Conditions
/// </summary>
[Description("Filter Conditions")]
public enum FilterLogicEnum
{
    /// <summary>
    /// And
    /// </summary>
    [Description("And")]
    And,

    /// <summary>
    /// Or
    /// </summary>
    [Description("Or")]
    Or,

    /// <summary>
    /// Exclusive Or
    /// </summary>
    [Description("Exclusive Or")]
    Xor
}