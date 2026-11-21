using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace ITS.Toll.Core.Persistence;

/// <summary>
/// So sánh "có thay đổi không" cho entity đồng bộ, phục vụ quy tắc CHỈ LƯU KHI DỮ LIỆU THAY ĐỔI.
/// Nguyên tắc: chỉ xét các property KHAI BÁO Ở LỚP CON (DeclaredOnly) — tức là khóa + các cột nghiệp vụ
/// + trường thời điểm đồng bộ; KHÔNG đụng tới trường audit của EntityTenant (ID/TenantId/CreateTime/UpdateTime...).
/// Bỏ qua các trường thời điểm đồng bộ (LastSyncTime/SyncTime) khi so sánh vì chúng đổi mỗi lần chạy.
/// Cách dùng: clone entity hiện có → gọi Fill(exist, item) → so sánh clone(before) với exist(after);
/// nhờ vậy field mà Fill KHÔNG map (giữ nguyên ở cả 2 phía) sẽ không bị coi là thay đổi.
/// Author: ChuongLP
/// Created date: 09/09/2026
/// </summary>
internal static class TollEntityDiff
{
    /// <summary>Trường kỹ thuật không tính là "thay đổi dữ liệu".</summary>
    private static readonly HashSet<string> IgnoreCompare = new(StringComparer.OrdinalIgnoreCase)
    {
        "LastSyncTime",
        "SyncTime",
    };

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _declaredCache = new();

    /// <summary>Các property đọc/ghi được, KHAI BÁO trực tiếp ở lớp entity con (không gồm audit kế thừa).</summary>
    private static PropertyInfo[] Declared(Type type) => _declaredCache.GetOrAdd(type, t =>
        t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
         .Where(p => p.CanRead && p.CanWrite)
         .ToArray());

    /// <summary>Tạo bản sao (shallow) các property khai báo — dùng làm ảnh "trước khi map".</summary>
    public static T CloneDeclared<T>(T source) where T : new()
    {
        var clone = new T();
        foreach (var p in Declared(typeof(T)))
            p.SetValue(clone, p.GetValue(source));
        return clone;
    }

    /// <summary>
    /// Áp dụng <paramref name="fill"/> lên entity hiện có và cho biết có thay đổi dữ liệu nghiệp vụ hay không.
    /// Trả về true nếu (sau khi map) có ít nhất một cột nghiệp vụ khác giá trị trước đó.
    /// </summary>
    public static bool ApplyAndCheckChanged<T>(T existing, Action<T, JsonElement> fill, JsonElement item) where T : new()
    {
        var before = CloneDeclared(existing);
        fill(existing, item);
        return ChangedDeclared(before, existing);
    }

    /// <summary>So sánh 2 entity trên các property khai báo (bỏ qua trường thời điểm đồng bộ).</summary>
    public static bool ChangedDeclared<T>(T before, T after)
    {
        foreach (var p in Declared(typeof(T)))
        {
            if (IgnoreCompare.Contains(p.Name)) continue;
            if (!Equals(p.GetValue(before), p.GetValue(after)))
                return true;
        }
        return false;
    }
}
