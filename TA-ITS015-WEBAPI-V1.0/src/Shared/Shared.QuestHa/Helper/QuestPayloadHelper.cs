using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shared.QuestHa.Helper;

/// <summary>
/// Author: KienLT
/// Description: Chuyển payload JSON của journal ⇒ <see cref="QuestRow"/> theo <see cref="QuestTableSpec"/>,
/// và chuẩn hoá giá trị để tính EventKey / fingerprint giống hệt nhau ở journal, node A, node B.
/// Timestamp cắt về micro giây (độ phân giải TIMESTAMP của QuestDB) và luôn là UTC.
/// Created date: 09/10/2026
/// </summary>
internal class QuestPayloadHelper
{
    protected QuestPayloadHelper() { }

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>Payload → dòng. Ném <see cref="PayloadConversionException"/> nếu JSON hỏng / sai kiểu.</summary>
    public static QuestRow ToRow(QuestTableSpec spec, long seq, string payload)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payload);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new PayloadConversionException($"Payload không phải JSON hợp lệ: {ex.Message}", ex);
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new PayloadConversionException("Payload phải là JSON object.");

            var props = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in doc.RootElement.EnumerateObject()) props[p.Name] = p.Value;

            var values = new object?[spec.Columns.Count];
            for (var i = 0; i < spec.Columns.Count; i++)
            {
                var col = spec.Columns[i];
                if (col.FromJournalSeq)
                {
                    values[i] = seq;
                    continue;
                }

                if (!props.TryGetValue(col.Name, out var el) || el.ValueKind == JsonValueKind.Null)
                {
                    if (col.IsDesignated)
                        throw new PayloadConversionException($"Thiếu designated timestamp '{col.Name}'.");
                    // SHORT không có NULL trong QuestDB (null lưu thành 0) ⇒ chuẩn hoá như node lưu,
                    // nếu không đối soát luôn thấy journal (null) khác node (0).
                    values[i] = col.Type == QuestColumnTypeEnum.Short ? (short)0 : null;
                    continue;
                }

                try
                {
                    values[i] = Convert(col, el);
                    if (col.IsDesignated && values[i] is DateTime timestamp && timestamp < DateTime.UnixEpoch)
                        throw new PayloadConversionException(
                            $"Cột '{col.Name}' ({col.Type}) không nhận giá trị {el.GetRawText()}: designated timestamp phải từ Unix epoch trở đi.");
                    // QuestDB dùng giá trị nhỏ nhất của LONG/INT làm NULL, không giữ được số gốc.
                    if ((col.Type == QuestColumnTypeEnum.Long && values[i] is long.MinValue)
                        || (col.Type == QuestColumnTypeEnum.Int && values[i] is int.MinValue))
                        throw new PayloadConversionException(
                            $"Cột '{col.Name}' ({col.Type}) không nhận giá trị {el.GetRawText()}: giá trị sentinel NULL của QuestDB.");
                    if (col.Type is QuestColumnTypeEnum.Symbol or QuestColumnTypeEnum.Varchar
                        && values[i] is string text && text.Contains('\0'))
                        throw new PayloadConversionException(
                            $"Cột '{col.Name}' ({col.Type}) không nhận giá trị {el.GetRawText()}: chuỗi chứa ký tự NUL.");
                }
                catch (Exception ex) when (ex is FormatException or InvalidOperationException or OverflowException or ArgumentOutOfRangeException)
                {
                    throw new PayloadConversionException(
                        $"Cột '{col.Name}' ({col.Type}) không nhận giá trị {el.GetRawText()}: {ex.Message}", ex);
                }
            }

            return new QuestRow(seq, values);
        }
    }

    private static object Convert(QuestColumn col, JsonElement el) => col.Type switch
    {
        QuestColumnTypeEnum.Timestamp => el.ValueKind switch
        {
            JsonValueKind.String => NormalizeTs(ParseTs(el.GetString()!)),
            // Số = epoch micro giây (giống cách QuestDB lưu).
            JsonValueKind.Number => DateTime.UnixEpoch.AddTicks(checked(el.GetInt64() * 10)),
            _ => throw new FormatException("cần chuỗi ISO-8601 hoặc số epoch µs"),
        },
        QuestColumnTypeEnum.Long => el.ValueKind == JsonValueKind.Number
            ? el.GetInt64()
            : throw new FormatException("cần số nguyên"),
        QuestColumnTypeEnum.Int => el.ValueKind == JsonValueKind.Number
            ? el.GetInt32()
            : throw new FormatException("cần số nguyên 32-bit"),
        QuestColumnTypeEnum.Short => el.ValueKind == JsonValueKind.Number
            ? el.GetInt16()
            : throw new FormatException("cần số nguyên 16-bit"),
        QuestColumnTypeEnum.Double => el.ValueKind == JsonValueKind.Number
            ? el.GetDouble()
            : throw new FormatException("cần số thực"),
        QuestColumnTypeEnum.Boolean => el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new FormatException("cần true/false"),
        },
        QuestColumnTypeEnum.Symbol or QuestColumnTypeEnum.Varchar => el.ValueKind == JsonValueKind.String
            ? el.GetString()!
            : throw new FormatException("cần chuỗi"),
        _ => throw new InvalidOperationException($"Kiểu cột chưa hỗ trợ: {col.Type}"),
    };

    private static DateTime ParseTs(string s)
    {
        var dt = DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        return dt.Kind switch
        {
            DateTimeKind.Local => dt.ToUniversalTime(),
            // Không có offset ⇒ quy ước UTC (journal chỉ chứa UTC).
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => dt,
        };
    }

    /// <summary>UTC + cắt về micro giây.</summary>
    public static DateTime NormalizeTs(DateTime dt)
    {
        var utc = dt.Kind switch
        {
            DateTimeKind.Local => dt.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => dt,
        };
        return new DateTime(utc.Ticks - utc.Ticks % 10, DateTimeKind.Utc);
    }

    /// <summary>Chuẩn hoá giá trị đọc từ node (DataTable) về đúng kiểu .NET như <see cref="ToRow"/> sinh ra.</summary>
    public static object? NormalizeFromDb(QuestColumn col, object? v)
    {
        if (v is null || v is DBNull) return null;
        return col.Type switch
        {
            QuestColumnTypeEnum.Timestamp => v is DateTime d ? NormalizeTs(d) : NormalizeTs(System.Convert.ToDateTime(v, CultureInfo.InvariantCulture)),
            QuestColumnTypeEnum.Long => System.Convert.ToInt64(v, CultureInfo.InvariantCulture),
            QuestColumnTypeEnum.Int => System.Convert.ToInt32(v, CultureInfo.InvariantCulture),
            QuestColumnTypeEnum.Short => System.Convert.ToInt16(v, CultureInfo.InvariantCulture),
            QuestColumnTypeEnum.Double => System.Convert.ToDouble(v, CultureInfo.InvariantCulture),
            QuestColumnTypeEnum.Boolean => System.Convert.ToBoolean(v, CultureInfo.InvariantCulture),
            _ => System.Convert.ToString(v, CultureInfo.InvariantCulture),
        };
    }

    /// <summary>
    /// Dòng → payload JSON, chiều ngược của <see cref="ToRow"/> (dùng để đưa lại dòng đọc từ một node vào journal khi vá).
    /// Tên trường = tên cột trong spec; timestamp là chuỗi ISO-8601 UTC micro giây; double không hữu hạn → null; bỏ cột
    /// <c>FromJournalSeq</c> (replicator tự điền). <c>ToRow(ToPayload(r))</c> cho fingerprint trùng với <c>r</c>.
    /// </summary>
    // Đối soát/vá node nguồn → đích cần dùng cùng phép chuyển payload với journal.
    public static string ToPayload(QuestTableSpec spec, QuestRow row)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>(256);
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            for (var i = 0; i < spec.Columns.Count; i++)
            {
                var col = spec.Columns[i];
                if (col.FromJournalSeq) continue;
                var v = row.Values[i];
                if (v is null) { w.WriteNull(col.Name); continue; }
                switch (col.Type)
                {
                    case QuestColumnTypeEnum.Timestamp:
                        w.WriteString(col.Name, NormalizeTs((DateTime)v).ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture));
                        break;
                    case QuestColumnTypeEnum.Long: w.WriteNumber(col.Name, (long)v); break;
                    case QuestColumnTypeEnum.Int: w.WriteNumber(col.Name, (int)v); break;
                    case QuestColumnTypeEnum.Short: w.WriteNumber(col.Name, (short)v); break;
                    case QuestColumnTypeEnum.Double:
                        if (double.IsFinite((double)v)) w.WriteNumber(col.Name, (double)v); else w.WriteNull(col.Name);
                        break;
                    case QuestColumnTypeEnum.Boolean: w.WriteBoolean(col.Name, (bool)v); break;
                    default: w.WriteString(col.Name, (string)v); break;
                }
            }

            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>Chuỗi chuẩn của 1 giá trị (dùng cho key + fingerprint).</summary>
    public static string Canon(object? v) => v switch
    {
        null => "∅",
        DateTime d => NormalizeTs(d).ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture),
        bool b => b ? "1" : "0",
        double x => x.ToString("R", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        // Chuỗi: tiền tố độ dài để "a|b" không trùng với ghép 2 chuỗi.
        string s => $"{s.Length}:{s}",
        _ => v.ToString() ?? "",
    };

    /// <summary>EventKey = các cột DEDUP key, nối bằng '|'.</summary>
    public static string KeyOf(QuestTableSpec spec, QuestRow row)
    {
        var sb = new StringBuilder();
        for (var k = 0; k < spec.KeyIndexes.Count; k++)
        {
            if (k > 0) sb.Append('|');
            sb.Append(Canon(row.Values[spec.KeyIndexes[k]]));
        }

        return sb.ToString();
    }

    /// <summary>Fingerprint = mọi cột theo thứ tự spec (gồm journalSeq).</summary>
    public static string FingerprintOf(QuestTableSpec spec, QuestRow row)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < spec.Columns.Count; i++)
        {
            if (i > 0) sb.Append('\u001f');
            sb.Append(Canon(row.Values[i]));
        }

        return sb.ToString();
    }

    /// <summary>Hash tổng hợp không phụ thuộc thứ tự: SHA-256 trên danh sách (key, fingerprint) đã sắp xếp.</summary>
    public static string AggregateHash(IEnumerable<(string Key, string Fingerprint)> items)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var (key, fp) in items.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            sha.AppendData(Encoding.UTF8.GetBytes(key));
            sha.AppendData([0x1e]);
            sha.AppendData(Encoding.UTF8.GetBytes(fp));
            sha.AppendData([0x1d]);
        }

        return System.Convert.ToHexString(sha.GetHashAndReset())[..16];
    }

    /// <summary>Giá trị cột version của dòng theo spec; null nếu spec không có cột version.</summary>
    public static long? VersionOf(QuestTableSpec spec, QuestRow row) =>
        spec.VersionIndex < 0 || row.Values[spec.VersionIndex] is null
            ? null
            : System.Convert.ToInt64(row.Values[spec.VersionIndex], CultureInfo.InvariantCulture);
}
