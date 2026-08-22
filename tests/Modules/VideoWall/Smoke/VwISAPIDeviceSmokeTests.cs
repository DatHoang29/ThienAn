namespace Tests.Modules.VideoWall.Smoke;

/// <summary>
/// Author: Đạt
/// Description: Smoke test THỦ CÔNG — chạy tay 1 lần khi có thiết bị Video Wall Controller thật,
///              KHÔNG bao giờ chạy trong CI/dotnet test bình thường (mọi [Fact] đều Skip mặc định).
///              Mục đích khác hẳn VwISAPIMockServerHikvision: mock kiểm tra CODE có đúng như mình NGHĨ
///              không; smoke test này kiểm tra GIẢ ĐỊNH của mình về thiết bị thật có đúng không —
///              đặc biệt là VwISAPIResponseStatus.Id (VWMWID), field mà comment gốc trong
///              VwISAPIResponse.cs ghi rõ "chưa được xác nhận trên thiết bị thật".
///
///              CÁCH CHẠY:
///              1. Set 3 biến môi trường trỏ tới thiết bị thật:
///                 VW_SMOKE_IP, VW_SMOKE_ACCOUNT, VW_SMOKE_PASSWORD
///              2. Tạm xoá tham số Skip = "..." khỏi [Fact] muốn chạy — CHỈ tạm ở máy mình, KHÔNG
///                 commit việc xoá Skip.
///              3. dotnet test tests\test.csproj --filter FullyQualifiedName~VwISAPIDeviceSmokeTests
///              4. Gắn lại Skip = "..." trước khi commit.
/// Created date: 18/08/2026
/// </summary>
[Collection("api")]
public class VwISAPIDeviceSmokeTests(Host host)
{
    private readonly IVwISAPIDeviceClient _client = host.Services.GetRequiredService<IVwISAPIDeviceClient>();

    // ═══════════════════════════════════════════════════════════════════════════════════════
    // CHƯA CÓ THIẾT BỊ THẬT — 3 giá trị dưới là placeholder, chưa phải secret.
    // Khi nào có thiết bị: sửa tay 3 dòng dưới thành IP/Account/PassWord thật để chạy smoke test,
    // rồi TRẢ VỀ LẠI PLACEHOLDER trước khi commit — không commit giá trị thật dưới bất kỳ hình
    // thức nào (kể cả không push, xem giải thích lượt chat trước: commit local vẫn có thể lộ qua
    // backup .git, force-push của người khác, chia sẻ màn hình...).
    // ═══════════════════════════════════════════════════════════════════════════════════════
    private const string RealDeviceIp = "0.0.0.0";
    private const string RealDeviceAccount = "admin";
    private const string RealDevicePassword = "CHANGE_ME";

    /// <summary>
    /// Author: Đạt
    /// Description: Dựng VwController trỏ tới thiết bị thật — IP/Account/PassWord sửa tay ở 3
    ///              const phía trên, không đọc/ghi DB.
    /// Created date: 18/08/2026
    /// </summary>
    private static VwController RealController => new()
    {
        ID = "smoke-real-device",
        Name = "Smoke Test Real Device",
        IP = RealDeviceIp,
        Account = RealDeviceAccount,
        PassWord = RealDevicePassword
    };

    /// <summary>
    /// Author: Đạt
    /// Description: Bước 1 — xác nhận kết nối + Digest Auth vào thiết bị thật thành công.
    /// Created date: 18/08/2026
    /// </summary>
    [Fact(Skip = "Chỉ chạy tay khi có thiết bị thật — xem hướng dẫn ở đầu file.")]
    public async Task Smoke_01_UserCheck_ConfirmsRealConnection()
    {
        var result = await _client.UserCheckAsync(RealController);

        Assert.True(result.Success, $"Không kết nối/đăng nhập được thiết bị thật: {result.ErrorMessage}");
        Assert.True(result.Data!.IsSuccess);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Bước 2 — đối chiếu isSupportScene THẬT với giá trị mock đang giả định (luôn true).
    /// Created date: 18/08/2026
    /// </summary>
    [Fact(Skip = "Chỉ chạy tay khi có thiết bị thật.")]
    public async Task Smoke_02_GetCapabilities_ReportsRealIsSupportScene()
    {
        var result = await _client.GetCapabilitiesAsync(RealController);

        Assert.True(result.Success, result.ErrorMessage);
        Console.WriteLine(
            $"[SMOKE] isSupportScene thật = {result.Data!.IsSupportScene} " +
            "(mock đang giả định luôn true — nếu ra false thì nhánh DeviceCapabilityUnsupported " +
            "cần kiểm lại với giá trị thật).");
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Bước 3 — QUAN TRỌNG NHẤT: xác nhận thiết bị thật có thực sự trả VWMWID khác 0
    ///              trong &lt;ID&gt; khi AddWindow thành công hay không. Đây chính là giả định mà cột
    ///              VwWindowScene.DeviceWindowId (fix Top/Bottom) đang dựa vào — nếu sai ở đây thì
    ///              Top/Bottom KHÔNG hoạt động trên máy thật dù mọi test mock đều xanh.
    /// Created date: 18/08/2026
    /// </summary>
    [Fact(Skip = "Chỉ chạy tay khi có thiết bị thật.")]
    public async Task Smoke_03_AddWindow_ReturnsNonZeroRealDeviceWindowId()
    {
        var addResult = await _client.AddWindowAsync(RealController, new VwISAPIWindowRequest
        {
            WndOperateMode = "uniformCoordinate",
            Rect = new VwISAPIRect
            {
                Coordinate = new VwISAPICoordinate { X = 0, Y = 0 },
                Width = 100,   // nhỏ, góc trên-trái — an toàn, không che khuất màn hình đang chiếu
                Height = 100
            }
        });

        Assert.True(addResult.Success, $"AddWindow thất bại trên thiết bị thật: {addResult.ErrorMessage}");
        Assert.True(addResult.Data!.Id > 0,
            "GIẢ ĐỊNH SAI: thiết bị thật KHÔNG trả VWMWID trong <ID> — fix DeviceWindowId cho " +
            "Top/Bottom phải thiết kế lại (ví dụ gọi thêm GET .../windows để tự tra ID sau khi Add).");

        Console.WriteLine($"[SMOKE] AddWindow trả về VWMWID thật = {addResult.Data.Id}");

        await _client.DeleteAllWindowsAsync(RealController); // dọn ngay, đừng để che màn hình thật
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Bước 4 — dùng đúng VWMWID vừa nhận ở bước 3 để gọi Top, xác nhận thiết bị chấp
    ///              nhận ID đó (không phải lỗi "window not found" — đúng bug đã tìm thấy khi code cũ
    ///              gửi nhầm Code thay vì VWMWID).
    /// Created date: 18/08/2026
    /// </summary>
    [Fact(Skip = "Chỉ chạy tay khi có thiết bị thật.")]
    public async Task Smoke_04_BringWindowToTop_WithRealDeviceWindowId_Succeeds()
    {
        var addResult = await _client.AddWindowAsync(RealController, new VwISAPIWindowRequest
        {
            WndOperateMode = "uniformCoordinate",
            Rect = new VwISAPIRect { Coordinate = new VwISAPICoordinate { X = 0, Y = 0 }, Width = 100, Height = 100 }
        });
        Assert.True(addResult.Success, addResult.ErrorMessage);
        var realWindowId = addResult.Data!.Id.ToString();

        var topResult = await _client.BringWindowToTopAsync(RealController, realWindowId);

        Assert.True(topResult.Success,
            $"Gọi Top thất bại với VWMWID thật '{realWindowId}': {topResult.ErrorMessage}");

        await _client.DeleteAllWindowsAsync(RealController);
    }
}
