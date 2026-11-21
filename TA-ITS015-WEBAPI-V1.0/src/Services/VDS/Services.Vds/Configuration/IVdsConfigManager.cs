namespace Services.Vds.Configuration;

/// <summary>
/// Author: KienLT
/// Description: Hợp đồng của bộ giữ cấu hình. Code nghiệp vụ nhận thẳng
/// <see cref="VdsConfigManager"/> (class) cho gọn; interface này để host và unit test thay thế được
/// bằng bản dựng sẵn.
/// Created date: 24/08/2026
/// </summary>
public interface IVdsConfigManager
{
    /// <summary>
    /// Snapshot đang hiệu lực. Mỗi lần gọi có thể trả về object KHÁC nếu vừa có thay đổi.
    ///
    /// Vì vậy: đọc MỘT LẦN vào biến cục bộ khi cần nhiều tham số liên quan, đừng gọi
    /// <c>Current.A</c> rồi <c>Current.B</c> — hai lời gọi có thể rơi vào hai snapshot khác nhau.
    /// </summary>
    ServiceConfig Current { get; }

    /// <summary>
    /// Áp một tham số lúc đang chạy (nguồn: NATS, hoặc bộ nạp từ <c>VdsConfig</c>).
    /// Thành công thì tham số được LƯU RA FILE để còn hiệu lực sau khi restart.
    /// </summary>
    /// <returns>false = tên không hỗ trợ hoặc giá trị không hợp lệ; snapshot giữ nguyên.</returns>
    bool Apply(string parameterName, string json);

    /// <summary>
    /// Nạp lại từ file cấu hình, rồi phát lại các override lên trên.
    /// Không bao giờ ném lỗi — thất bại thì giữ snapshot đang hiệu lực (last-known-good).
    /// </summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
