namespace Services.Vds.Shared.State;

/// <summary>
/// Author: KienLT
/// Description: Đường dẫn thư mục state + ghi file an toàn. Mọi thứ service lưu ra đĩa
/// (override cấu hình, ngưỡng tốc độ, checkpoint…) đều đi qua đây.
/// Created date: 24/08/2026
/// </summary>
public static class StatePath
{
    /// <summary>
    /// Dựng đường dẫn tuyệt đối từ thư mục state.
    ///
    /// <paramref name="root"/> tương đối thì neo vào <see cref="AppContext.BaseDirectory"/>, KHÔNG neo
    /// vào working directory: chạy như Windows Service thì working directory là
    /// <c>C:\Windows\System32</c>, file sẽ rơi vào đó.
    /// </summary>
    public static string Resolve(string root, params string[] parts)
    {
        var baseDir = Path.IsPathRooted(root) ? root : Path.Combine(AppContext.BaseDirectory, root);
        return parts.Length == 0
            ? baseDir
            : Path.Combine(new[] { baseDir }.Concat(parts).ToArray());
    }

    /// <summary>
    /// Ghi ra file <c>.tmp</c> rồi mới thay thế file đích.
    ///
    /// Ghi thẳng thì tiến trình chết giữa lúc ghi sẽ để lại file rỗng hoặc JSON đứt đoạn — lần khởi động
    /// sau đọc lên là mất hết cấu hình mà không có lỗi nào để thấy. Đổi tên là thao tác nguyên tử nên
    /// file đích luôn ở một trong hai trạng thái: bản cũ nguyên vẹn, hoặc bản mới nguyên vẹn.
    /// </summary>
    public static void WriteAtomic(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tmp = path + ".tmp";
        using (var file = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            file.Write(bytes);
            file.Flush(flushToDisk: true);
        }

        if (File.Exists(path)) File.Replace(tmp, path, null);
        else File.Move(tmp, path);
    }
}
