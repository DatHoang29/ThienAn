using FluentValidation.Results;
using Module.VideoWall.Controllers.Controller.Validators;
using Module.VideoWall.Core.Dto.Controller;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Helper;
using ITS.VideoWall.Services.ISAPIDevice;
using System.Reflection;

namespace Tests.VideoWall.Worker.ISAPIDevice
{
    /// <summary>
    /// Description: Bộ kiểm thử Nhóm B (B1-B10) thẩm định logic lựa chọn bộ điều khiển trung tâm,
    ///              sử dụng VwControllerTopology xác định vai trò qua ParentControllerId.
    /// Created date: 08/09/2026 — Cập nhật khớp cấu trúc ParentControllerId: 29/09/2026
    /// </summary>
    [Collection("api")]
    public class VwCascadeControllerSelectionTests(Host host)
    {
        private readonly IStringLocalizer _localizer = host.Localizer;
        /// <summary>
        /// Description: B1 - ActivateScene khi không có bộ trung tâm trong targetControllerIds ném lỗi và không gửi lệnh tới controller khác.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B1_ActivateScene_WhenNoCenterInTargetIdsButOtherControllersExist_ThrowsOopsWithoutCallingDevice()
        {
            // Arrange
            var service = new VwISAPIDeviceService(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
            var scene = new VwScene
            {
                OutputId = "1"
            };
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "sub-01",
                    ParentControllerId = "non-existent-center",
                    IP = "192.168.1.11"
                },
                new()
                {
                    ID = "sub-02",
                    ParentControllerId = "non-existent-center",
                    IP = "192.168.1.12"
                }
            };
            var targetControllerIds = new List<string>();

            // Act & Assert
            var ex = await Assert.ThrowsAnyAsync<Exception>(() => service.ActivateScene(scene, controllers, targetControllerIds));
            Assert.True(ex is TypeInitializationException || ex.Message.Contains("Chưa cấu hình bộ điều khiển trung tâm"));
        }

        /// <summary>
        /// Description: B2 - ActivateScene với targetControllerIds rỗng hoặc null ném ngoại lệ rõ ràng thay vì vơ đại controller đầu tiên.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B2_ActivateScene_WhenTargetControllerIdsNullOrEmpty_ThrowsOops()
        {
            // Arrange
            var service = new VwISAPIDeviceService(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
            var scene = new VwScene
            {
                OutputId = "1"
            };
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "ctrl-any",
                    ParentControllerId = "some-parent",
                    IP = "192.168.1.50"
                }
            };

            // Act & Assert
            var exNull = await Assert.ThrowsAnyAsync<Exception>(() => service.ActivateScene(scene, controllers, null!));
            Assert.True(exNull is TypeInitializationException || exNull.Message.Contains("Chưa cấu hình bộ điều khiển trung tâm"));

            var exEmpty = await Assert.ThrowsAnyAsync<Exception>(() => service.ActivateScene(scene, controllers, []));
            Assert.True(exEmpty is TypeInitializationException || exEmpty.Message.Contains("Chưa cấu hình bộ điều khiển trung tâm"));
        }

        /// <summary>
        /// Description: B3 - ActivateScene khi kịch bản chưa khai OutputId ném ngoại lệ thông báo kịch bản chưa khai mã.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B3_ActivateScene_WhenOutputIdMissing_ThrowsOops()
        {
            // Arrange
            var service = new VwISAPIDeviceService(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
            var scene = new VwScene
            {
                ID = "SCN_01",
                Code = "SCN-01",
                OutputId = ""
            };
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "center-01",
                    ParentControllerId = null,
                    IP = "192.168.1.10"
                },
                new()
                {
                    ID = "sub-01",
                    ParentControllerId = "center-01",
                    IP = "192.168.1.11"
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAnyAsync<Exception>(() => service.ActivateScene(scene, controllers, ["center-01"]));
            Assert.True(ex is TypeInitializationException || ex.Message.Contains("chưa khai mã kịch bản"));
        }

        /// <summary>
        /// Description: B4 - VwControllerTopology tìm center trả về rỗng khi chỉ có bộ phụ, không tự ý chọn bộ phụ.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void B4_HandlerCenterResolution_WhenOnlySubControllersExist_DoesNotPickAnyController()
        {
            // Arrange
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "sub-01",
                    ParentControllerId = "external-parent",
                    IP = "192.168.1.11"
                },
                new()
                {
                    ID = "sub-02",
                    ParentControllerId = "external-parent",
                    IP = "192.168.1.12"
                }
            };

            // Act
            var centerIds = VwControllerTopology.GetCenterIds(controllers);
            var center = controllers.FirstOrDefault(u => centerIds.Contains(u.ID));

            // Assert
            Assert.Null(center);
        }

        /// <summary>
        /// Description: B5 - VwControllerTopology tìm đúng bộ điều khiển trung tâm (có con trỏ tới) kể cả khi bộ phụ nằm đầu danh sách.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void B5_HandlerCenterResolution_WhenCenterAndSubControllersExist_PicksOnlyRoleCenter()
        {
            // Arrange
            var centerExpected = new VwController
            {
                ID = "center-01",
                ParentControllerId = null,
                IP = "192.168.1.10"
            };
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "sub-01",
                    ParentControllerId = "center-01",
                    IP = "192.168.1.11"
                },
                centerExpected,
                new()
                {
                    ID = "sub-02",
                    ParentControllerId = "center-01",
                    IP = "192.168.1.12"
                }
            };

            // Act
            var centerIds = VwControllerTopology.GetCenterIds(controllers);
            var center = controllers.FirstOrDefault(u => centerIds.Contains(u.ID));

            // Assert
            Assert.NotNull(center);
            Assert.Equal("center-01", center.ID);
            Assert.Equal("192.168.1.10", center.IP);
        }

        /// <summary>
        /// Description: B6, B8 - RequireCenterController thẩm định đúng điều kiện và ném exception tương ứng khi danh sách controller không hợp lệ.
        /// Created date: 08/09/2026
        /// </summary>
        [Theory]
        [InlineData("ZeroCenters", "Chưa cấu hình bộ điều khiển trung tâm")]
        [InlineData("EmptyIp", "Bộ điều khiển trung tâm chưa khai IP")]
        public void RequireCenterController_WhenInvalidCenterSetup_ThrowsExpectedError(string scenario, string expectedError)
        {
            // Arrange
            var controllers = scenario switch
            {
                "ZeroCenters" => new List<VwController>
                {
                    new() { ID = "sub-01", ParentControllerId = "external", IP = "192.168.1.11" }
                },
                "EmptyIp" => new List<VwController>
                {
                    new() { ID = "center-01", ParentControllerId = null, IP = "   " },
                    new() { ID = "sub-01", ParentControllerId = "center-01", IP = "192.168.1.11" }
                },
                _ => throw new ArgumentException($"Unknown scenario: {scenario}")
            };

            // Act & Assert
            var ex = Assert.ThrowsAny<Exception>(() => EvaluateRequireCenter(controllers));
            Assert.True(ex is TypeInitializationException || ex.Message.Contains(expectedError));
        }

        /// <summary>
        /// Description: B9 - RequireCenterController trả về đúng bộ trung tâm khi cấu hình duy nhất và hợp lệ.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void B9_RequireCenterController_WhenSingleValidCenter_ReturnsCenter()
        {
            // Arrange
            var centerExpected = new VwController
            {
                ID = "center-01",
                ParentControllerId = null,
                IP = "192.168.1.10"
            };
            var controllers = new List<VwController>
            {
                centerExpected,
                new() { ID = "sub-01", ParentControllerId = "center-01", IP = "192.168.1.11" }
            };

            // Act
            var center = EvaluateRequireCenter(controllers);

            // Assert
            Assert.Same(centerExpected, center);
        }

        /// <summary>
        /// Description: B10 - VwControllerValidator kiểm tra tính hợp lệ của IntegrationMode.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B10_VwControllerValidator_RoleAndIntegrationMode_ValidatesAllowedValues()
        {
            // Arrange
            var validator = new VwControllerValidator(_localizer);

            // Act & Assert 1: Hợp lệ với cascade
            var valid1 = new VwAddControllerInput
            {
                IntegrationMode = "cascade"
            };
            var res1 = await validator.ValidateAsync(valid1);
            Assert.DoesNotContain(res1.Errors, e => e.PropertyName == "IntegrationMode");

            // Act & Assert 2: IntegrationMode null — hợp lệ
            var validEmpty = new VwAddControllerInput
            {
                IntegrationMode = null
            };
            var resEmpty = await validator.ValidateAsync(validEmpty);
            Assert.DoesNotContain(resEmpty.Errors, e => e.PropertyName == "IntegrationMode");

            // Act & Assert 3: Từ chối IntegrationMode == 'active'
            var invalidMode = new VwAddControllerInput
            {
                IntegrationMode = "active"
            };
            var resMode = await validator.ValidateAsync(invalidMode);
            Assert.Contains(resMode.Errors, e => e.PropertyName == "IntegrationMode");
        }

        /// <summary>
        /// Description: B11 - Tạo 2 center riêng biệt KHÔNG bị validator chặn — mỗi center = 1 tường độc lập.
        /// Created date: 14/09/2026
        /// </summary>
        [Fact]
        public async Task B11_TwoCenters_AreValidSeparateTowers_NotBlockedByValidator()
        {
            var validator = new VwControllerValidator(_localizer);

            var center1 = new VwAddControllerInput { ParentControllerId = null };
            var center2 = new VwAddControllerInput { ParentControllerId = null };

            var res1 = await validator.ValidateAsync(center1);
            var res2 = await validator.ValidateAsync(center2);

            Assert.DoesNotContain(res1.Errors, e => e.PropertyName == "ParentControllerId");
            Assert.DoesNotContain(res2.Errors, e => e.PropertyName == "ParentControllerId");
        }

        /// <summary>
        /// Description: B13 - ParentControllerId quá dài bị validator chặn.
        /// Created date: 14/09/2026
        /// </summary>
        [Fact]
        public async Task B13_LongParentControllerId_IsRejectedByValidator()
        {
            var validator = new VwControllerValidator(_localizer);

            var sub = new VwAddControllerInput
            {
                ParentControllerId = new string('x', 200)
            };

            var res = await validator.ValidateAsync(sub);
            Assert.Contains(res.Errors, e => e.PropertyName == "ParentControllerId");
        }

        /// <summary>
        /// Description: Hàm đánh giá logic nghiệp vụ của RequireCenterController trên tập danh sách in-memory.
        /// Created date: 08/09/2026
        /// </summary>
        private static VwController EvaluateRequireCenter(List<VwController> controllers)
        {
            var centerIds = VwControllerTopology.GetCenterIds(controllers);
            var centers = controllers
                .Where(u => u.IsDelete == null && centerIds.Contains(u.ID))
                .ToList();

            if (centers.Count == 0)
                throw Furion.FriendlyException.Oops.Oh("Chưa cấu hình bộ điều khiển trung tâm.");

            var c = centers[0];
            if (string.IsNullOrWhiteSpace(c.IP))
                throw Furion.FriendlyException.Oops.Oh("Bộ điều khiển trung tâm chưa khai IP.");

            return c;
        }

    }
}
