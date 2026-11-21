using System.ComponentModel;

namespace Shared.DTO.Enums;

/// <summary>
/// Filter Logic Operators
/// </summary>
[Description("Filter Logic Operators")]
public enum FilterOperatorEnum
{
    /// <summary>
    /// Equal (=)
    /// </summary>
    [Description("Equal")]
    EQ,

    /// <summary>
    /// Not Equal (!=)
    /// </summary>
    [Description("Not Equal")]
    NEQ,

    /// <summary>
    /// Less Than (<)
    /// </summary>
    [Description("Less Than")]
    LT,

    /// <summary>
    /// Less Than or Equal To (<=)
    /// </summary>
    [Description("Less Than or Equal To")]
    LTE,

    /// <summary>
    /// Greater Than (>)
    /// </summary>
    [Description("Greater Than")]
    GT,

    /// <summary>
    /// Greater Than or Equal To (>=)
    /// </summary>
    [Description("Greater Than or Equal To")]
    GTE,

    /// <summary>
    /// Starts With
    /// </summary>
    [Description("Starts With")]
    StartsWith,

    /// <summary>
    /// Ends With
    /// </summary>
    [Description("Ends With")]
    EndsWith,

    /// <summary>
    /// Contains
    /// </summary>
    [Description("Contains")]
    Contains
}