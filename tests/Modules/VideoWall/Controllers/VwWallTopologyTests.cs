namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Description: IStringLocalizer trả lại chính khoá được tra, không cần dựng cả ứng dụng.
    ///
    ///              VÌ SAO CẦN: validator chỉ dùng localizer để dựng CHỮ trong thông báo lỗi, còn
    ///              test dưới đây chỉ quan tâm rule có chặn đúng field hay không. Lấy localizer từ
    ///              Host sẽ buộc test thuần phải boot cả WebApplicationFactory — chậm, và chết chung
    ///              khi Host lỗi dù bản thân rule không liên quan gì.
    /// Created date: 09/09/2026
    /// </summary>
    public sealed class EchoLocalizerTest : IStringLocalizer
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    /// <summary>
    /// Description: Kiểm thử rule validator của cấu hình tường VwWallTopology. Thuần, không nối
    ///              CSDL và không cần Host.
    /// Created date: 09/09/2026
    /// </summary>
    public class VwWallTopologyValidatorTests
    {
        private static VwUpsertWallTopologyValidator CreateValidator() => new(new EchoLocalizerTest());

        /// <summary>
        /// Description: Nhận cấu hình tường hợp lệ của công trình: 8 cột × 4 hàng, panel FHD.
        /// Created date: 09/09/2026
        /// </summary>
        [Fact]
        public void VwWallTopologyValidator_AcceptsValidWallConfiguration_Test()
        {
            var result = CreateValidator().Validate(new VwUpsertWallTopologyInput
            {
                Rows = 4,
                Cols = 8,
                ScreenWidth = 1920,
                ScreenHeight = 1080
            });

            Assert.True(result.IsValid);
        }

        /// <summary>
        /// Description: Chặn số hàng / số cột trống, không dương, hoặc vượt ngưỡng.
        ///              Bỏ chặn trên thì canvas vọt lên hàng triệu pixel và mọi phép kiểm biên
        ///              cửa sổ sau đó đều vô nghĩa mà không báo lỗi ở đâu.
        /// Created date: 09/09/2026
        /// </summary>
        [Theory]
        [InlineData(null, 8)]
        [InlineData(4, null)]
        [InlineData(0, 8)]
        [InlineData(4, 0)]
        [InlineData(-1, 8)]
        [InlineData(4, -1)]
        [InlineData(17, 8)]
        [InlineData(4, 33)]
        public void VwWallTopologyValidator_RejectsGridOutOfRange_Test(int? rows, int? cols)
        {
            var result = CreateValidator().Validate(new VwUpsertWallTopologyInput
            {
                Rows = rows,
                Cols = cols,
                ScreenWidth = 1920,
                ScreenHeight = 1080
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(VwUpsertWallTopologyInput.Rows)
                                             || e.PropertyName == nameof(VwUpsertWallTopologyInput.Cols));
        }

        /// <summary>
        /// Description: Chặn kích thước panel không dương. Panel 0 pixel làm canvas bằng 0,
        ///              khi đó cửa sổ nào cũng bị coi là tràn ra ngoài tường.
        /// Created date: 09/09/2026
        /// </summary>
        [Theory]
        [InlineData(null, 1080)]
        [InlineData(1920, null)]
        [InlineData(0, 1080)]
        [InlineData(1920, 0)]
        [InlineData(-1920, 1080)]
        [InlineData(1920, -1080)]
        public void VwWallTopologyValidator_RejectsNonPositivePanelSize_Test(int? screenWidth, int? screenHeight)
        {
            var result = CreateValidator().Validate(new VwUpsertWallTopologyInput
            {
                Rows = 4,
                Cols = 8,
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(VwUpsertWallTopologyInput.ScreenWidth)
                                             || e.PropertyName == nameof(VwUpsertWallTopologyInput.ScreenHeight));
        }
    }

    /// <summary>
    /// Description: Kiểm thử truy vấn cấu hình tường VwWallTopology qua message bus.
    ///
    ///              CỐ Ý KHÔNG kiểm luồng Upsert: VwWallTopology là bảng CHỈ MỘT bản ghi và
    ///              VwSceneRegionService.GetWallCanvasAsync() đọc bản ghi đầu tiên để quy đổi toạ độ
    ///              cho mọi cửa sổ. Một test ghi vào bảng này sẽ đổi canvas dùng chung và làm test
    ///              khác (VwCascadeCoordDatabaseTests.A7) phụ thuộc thứ tự chạy.
    /// Created date: 09/09/2026
    /// </summary>
    [Collection("api")]
    public class VwWallTopologyTests(Host host)
    {
        private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();

        /// <summary>
        /// Description: GetCurrent luôn trả về cấu hình dùng được — không bao giờ null, kể cả khi
        ///              chưa có bản ghi nào trong CSDL.
        /// Created date: 09/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallTopologyQuery_GetCurrent_NeverReturnsNull_Test()
        {
            var result = await _bus.InvokeAsync<VwWallTopologyOutput>(new VwCurrentWallTopologyInput());

            Assert.NotNull(result);
            Assert.True(result.Rows > 0, "Số hàng phải lớn hơn 0 để client dựng được lưới.");
            Assert.True(result.Cols > 0, "Số cột phải lớn hơn 0 để client dựng được lưới.");
        }

        /// <summary>
        /// Description: Canvas trả về phải đúng bằng Cols × ScreenWidth và Rows × ScreenHeight.
        ///              Đây là lý do tồn tại của endpoint: client khỏi tự nhân rồi lệch backend.
        /// Created date: 09/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallTopologyQuery_GetCurrent_CanvasMatchesGridTimesPanel_Test()
        {
            var result = await _bus.InvokeAsync<VwWallTopologyOutput>(new VwCurrentWallTopologyInput());

            Assert.NotNull(result);
            Assert.Equal((result.Cols ?? 0) * (result.ScreenWidth ?? 0), result.CanvasWidth);
            Assert.Equal((result.Rows ?? 0) * (result.ScreenHeight ?? 0), result.CanvasHeight);
        }

        /// <summary>
        /// Description: Khi chưa cấu hình, giá trị mặc định phải khớp VwWallProfile — nếu không thì
        ///              backend quy đổi toạ độ theo một kích thước, client vẽ theo kích thước khác.
        /// Created date: 09/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallTopologyQuery_GetCurrent_DefaultMatchesWallProfile_Test()
        {
            var result = await _bus.InvokeAsync<VwWallTopologyOutput>(new VwCurrentWallTopologyInput());

            Assert.NotNull(result);

            // Chỉ ràng buộc khi đang là giá trị mặc định; có bản ghi thật thì số liệu do người
            // vận hành khai, không phải việc của test này.
            if (!result.IsDefault)
                return;

            Assert.Equal(VwWallProfile.DefaultRows, result.Rows);
            Assert.Equal(VwWallProfile.DefaultCols, result.Cols);
            Assert.Equal(VwWallProfile.PanelWidthPx, result.ScreenWidth);
            Assert.Equal(VwWallProfile.PanelHeightPx, result.ScreenHeight);
        }
    }
}
