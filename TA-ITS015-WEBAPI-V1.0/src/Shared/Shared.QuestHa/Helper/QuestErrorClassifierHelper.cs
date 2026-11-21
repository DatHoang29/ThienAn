using System.Net.Sockets;
using Npgsql;

namespace Shared.QuestHa.Helper;

/// <summary>
/// Author: KienLT
/// Description: Phân loại lỗi QuestDB. Nhánh "kết nối" giữ đúng danh sách của
/// <c>src/Shared/Shared.Utility/Resilience/QuestAvailabilityGate.cs:183</c> (Socket/Timeout/IO +
/// "Connection is not open" mà SqlSugar QuestDBProvider trả khi Open() thất bại) và bổ sung vài câu Npgsql.
/// Nhánh "node" = lỗi không phải của riêng dòng nào (thiếu bảng/cột, WAL suspend, read-only) ⇒ Fenced,
/// tuyệt đối KHÔNG quarantine (nếu không, mất bảng sẽ đẩy cả journal vào quarantine).
/// Chỉ lỗi chuyển đổi giá trị được nhận diện rõ là "dữ liệu"; còn lại = "node" để giữ nghĩa vụ giao.
/// Created date: 09/10/2026
/// </summary>
internal class QuestErrorClassifierHelper
{
    protected QuestErrorClassifierHelper() { }

    private static readonly string[] ConnectivityMessages =
    {
        "Connection is not open",
        "Exception while connecting",
        "Exception while reading from stream",
        "Exception while writing to stream",
        "No connection could be made",
        "connection refused",
        "An existing connection was forcibly closed",
        "The operation has timed out",
        "Timeout during",
        "terminating connection",
        "server closed the connection",
    };

    private static readonly string[] NodeMessages =
    {
        "table does not exist",
        "does not exist",
        "suspended",
        "table is dropped",
        "read only",
        "read-only",
        "disk full",
        "No space left",
        "could not open",
        "cannot open",
        "Invalid column:",
        "permission denied",
        "access denied",
        "out of memory",
        "internal error",
        // CỐ Ý là Node. QuestPayloadHelper.ToRow đã chặn designated timestamp thiếu/NULL (dòng đó vào
        // quarantine trước khi tới node) ⇒ node còn báo lỗi này là lỗi không thuộc riêng dòng nào (vd bảng trên node lệch
        // spec/ánh xạ cột) — mọi dòng sẽ cùng hỏng; xếp Data sẽ đẩy cả journal vào quarantine.
        "designated timestamp column cannot be NULL",
    };

    // QuestDB 10.0.0: cairo/ImplicitCastException.inconvertibleValue (không phải inconvertible types/schema).
    private static readonly string[] DataMessages = { "inconvertible value:" };

    // Npgsql PostgresErrorCodes: chỉ mã chuyển đổi giá trị cụ thể, không nhận cả lớp 22/DataException.
    private static bool IsValueConversionState(string sqlState) => sqlState is
        PostgresErrorCodes.InvalidTextRepresentation or
        PostgresErrorCodes.NumericValueOutOfRange or
        PostgresErrorCodes.InvalidDatetimeFormat or
        PostgresErrorCodes.DatetimeFieldOverflow;

    /// <summary>Phân loại lỗi: kết nối (thử node khác/backoff), node (rào node, không quarantine) hoặc dữ liệu (cô lập dòng).</summary>
    public static QuestErrorKindEnum Classify(Exception ex)
    {
        if (IsConnectivity(ex)) return QuestErrorKindEnum.Connectivity;
        if (IsNode(ex)) return QuestErrorKindEnum.Node;
        return IsData(ex) ? QuestErrorKindEnum.Data : QuestErrorKindEnum.Node;
    }

    /// <summary>true nếu chuỗi lỗi (kể cả inner) là lỗi kết nối: Socket/Timeout/IO hoặc thông điệp kết nối của Npgsql/SqlSugar.</summary>
    public static bool IsConnectivity(Exception? ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is SocketException or TimeoutException or IOException) return true;
            if (ContainsAny(e.Message, ConnectivityMessages)) return true;
        }

        return false;
    }

    private static bool IsNode(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (ContainsAny(e.Message, NodeMessages)) return true;
            // SQLSTATE có nghĩa phải thắng chuỗi lỗi; QuestDB dùng 00000 cho lỗi PG không phân loại.
            if (e is PostgresException pg && pg.SqlState != "00000" && !IsValueConversionState(pg.SqlState)) return true;
        }

        return false;
    }

    private static bool IsData(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is PostgresException pg && IsValueConversionState(pg.SqlState)) return true;
            if (ContainsAny(e.Message, DataMessages)) return true;
        }

        return false;
    }

    private static bool ContainsAny(string? message, string[] needles)
    {
        if (string.IsNullOrEmpty(message)) return false;
        foreach (var n in needles)
        {
            if (message.Contains(n, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    /// <summary>Rút gọn chuỗi lỗi (lưu SQL/hiện dashboard).</summary>
    public static string Describe(Exception ex, int max = 1800)
    {
        var parts = new List<string>();
        for (var e = ex; e is not null && parts.Count < 4; e = e.InnerException)
            parts.Add($"{e.GetType().Name}: {e.Message}");
        var s = string.Join(" ← ", parts);
        return s.Length <= max ? s : s[..max];
    }

    /// <summary>Chuỗi cho log/timeline: chỉ loại exception và mã SQL, không đưa nội dung nhà cung cấp ra ngoài Debug.</summary>
    // SqlSugar có thể chứa tài khoản/máy/chuỗi kết nối ngay trong Message.
    public static string SafeDescribe(Exception ex)
    {
        var parts = new List<string>();
        for (var e = ex; e is not null && parts.Count < 4; e = e.InnerException)
            parts.Add(e is Microsoft.Data.SqlClient.SqlException sql ? $"SqlException({sql.Number})" : e.GetType().Name);
        return string.Join(" <- ", parts);
    }
}
