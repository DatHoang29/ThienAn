namespace Modules.Integration.Barrier.Base.Constants;

/// <summary>
/// Hằng số tích hợp Barrier — lấy NGUYÊN BẢN từ dự án cũ CLVH.
///
/// Nguồn:
///   - TACPLC_S71200.dll / clsPLCS71200_Variables (đọc bằng reflection 03/10/2026).
///   - is4b.presentations.webapps.scs/App_Start/CommonFunctions.cs (ChangeBarrrierState):
///       var OPENBR = "DB1.DBX0.1"; var CLOSEBR = "DB1.DBX0.2";
///   - clsPLC_S71200 mặc định: CpuType.S71200, Rack 0, Slot 0, vTimeTimer 10000 (web set).
///
/// LƯU Ý: Web CLVH CHỈ GHI 2 bit OPEN/CLOSE, KHÔNG đọc trạng thái (trạng thái lưu ở DB qua
/// prc_Its_Update_Barrier_Success). Các bit đọc bên dưới có trong DLL cũ nhưng CHƯA được
/// kiểm chứng trên tủ thật — chỉ dùng tham khảo/giám sát.
/// 
/// </summary>
public static class IntgBarrierConstant
{
    #region Kết nối PLC

    /// <summary>Cổng giao thức S7 (ISO-on-TCP) chuẩn của Siemens.</summary>
    public const int DefaultS7Port = 102;

    public const short DefaultRack = 0;
    public const short DefaultSlot = 0;

    /// <summary>Timeout kết nối/ghi. Web CLVH đặt plc1.vTimeTimer = 10000.</summary>
    public const int DefaultTimeoutMs = 10000;

    /// <summary>Timeout ping trước khi kết nối (thay NetworkHelper.fn_PingHost).</summary>
    public const int DefaultPingTimeoutMs = 2000;

    #endregion

    #region Vùng nhớ GHI (lệnh) — đã dùng thật ở CLVH

    /// <summary>Mở barrier (OPENBR).</summary>
    public const string OpenBit = "DB1.DBX0.1";

    /// <summary>Đóng barrier (CLOSEBR).</summary>
    public const string CloseBit = "DB1.DBX0.2";

    #endregion

    #region Vùng nhớ ĐỌC — có trong DLL cũ, CHƯA kiểm chứng

    /// <summary>sInput_TrangThaiBarie (trùng địa chỉ với sInput_DongBarie).</summary>
    public const string StatusBit = "DB1.DBX0.4";

    /// <summary>sInput_SwitchON.</summary>
    public const string SwitchOnBit = "DB1.DBX0.0";

    /// <summary>sInput_LoopETC.</summary>
    public const string LoopEtcBit = "DB1.DBX0.5";

    /// <summary>sInput_LoopCabin.</summary>
    public const string LoopCabinBit = "DB1.DBX0.6";

    /// <summary>sInput_LoopCoi.</summary>
    public const string LoopCoiBit = "DB1.DBX0.7";

    /// <summary>sInput_LoopBarie.</summary>
    public const string LoopBarrierBit = "DB1.DBX1.0";

    #endregion
}
