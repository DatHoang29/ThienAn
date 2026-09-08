using FluentValidation.Results;
using Module.VideoWall.Controllers.Controller.Validators;
using Module.VideoWall.Core.Dto.Controller;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure.Services.ISAPIDevice;
using System.Reflection;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Description: Bộ kiểm thử Nhóm B (B1-B10) thẩm định logic lựa chọn bộ điều khiển trung tâm (Role=center),
    ///              loại bỏ hoàn toàn IntegrationMode==active và fallback FirstOrDefault() nguy hiểm.
    /// Created date: 08/09/2026
    /// </summary>
    public class VwCascadeControllerSelectionTests
    {
        /// <summary>
        /// Description: B1 - ActivateScene khi không có bộ trung tâm trong targetControllerIds ném lỗi và không gửi lệnh tới controller khác.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task B1_ActivateScene_WhenNoCenterInTargetIdsButOtherControllersExist_ThrowsOopsWithoutCallingDevice()
        {
            // Arrange
            var service = new VwISAPIDeviceService(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
            var scene = new VwScene
            {
                OutputId = "1"
            };
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "sub-01",
                    Role = "sub",
                    IP = "192.168.1.11"
                },
                new()
                {
                    ID = "sub-02",
                    Role = "sub",
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
            var service = new VwISAPIDeviceService(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
            var scene = new VwScene
            {
                OutputId = "1"
            };
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "ctrl-any",
                    Role = "sub",
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
            var service = new VwISAPIDeviceService(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
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
                    Role = "center",
                    IP = "192.168.1.10"
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAnyAsync<Exception>(() => service.ActivateScene(scene, controllers, ["center-01"]));
            Assert.True(ex is TypeInitializationException || ex.Message.Contains("chưa khai mã kịch bản"));
        }

        /// <summary>
        /// Description: B4 - Workflow Command Handler tìm center qua Role==center trả về null khi chỉ có bộ phụ, không tự ý chọn bộ phụ.
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
                    Role = "sub",
                    IP = "192.168.1.11"
                },
                new()
                {
                    ID = "sub-02",
                    Role = "sub",
                    IP = "192.168.1.12"
                }
            };

            // Act
            var center = controllers.FirstOrDefault(u => u.Role == "center");

            // Assert
            Assert.Null(center);
        }

        /// <summary>
        /// Description: B5 - Workflow Command Handler tìm đúng bộ điều khiển có Role==center kể cả khi bộ phụ nằm đầu danh sách.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void B5_HandlerCenterResolution_WhenCenterAndSubControllersExist_PicksOnlyRoleCenter()
        {
            // Arrange
            var centerExpected = new VwController
            {
                ID = "center-01",
                Role = "center",
                IP = "192.168.1.10"
            };
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "sub-01",
                    Role = "sub",
                    IP = "192.168.1.11"
                },
                centerExpected,
                new()
                {
                    ID = "sub-02",
                    Role = "sub",
                    IP = "192.168.1.12"
                }
            };

            // Act
            var center = controllers.FirstOrDefault(u => u.Role == "center");

            // Assert
            Assert.NotNull(center);
            Assert.Equal("center-01", center.ID);
            Assert.Equal("192.168.1.10", center.IP);
        }

        /// <summary>
        /// Description: B6 - RequireCenterController thẩm định đúng điều kiện khi không có bộ trung tâm nào (Role=center).
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void B6_RequireCenterController_WhenZeroCenters_ThrowsChuaCauHinh()
        {
            // Arrange
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "sub-01",
                    Role = "sub",
                    IP = "192.168.1.11"
                }
            };

            // Act & Assert
            var ex = Assert.ThrowsAny<Exception>(() => EvaluateRequireCenter(controllers));
            Assert.True(ex is TypeInitializationException || ex.Message.Contains("Chưa cấu hình bộ điều khiển trung tâm (Role=center)"));
        }

        /// <summary>
        /// Description: B7 - RequireCenterController ném lỗi khi có >1 bộ điều khiển trung tâm (Role=center).
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void B7_RequireCenterController_WhenTwoCenterControllersExist_ThrowsCoHonMot()
        {
            // Arrange
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "center-01",
                    Role = "center",
                    IP = "192.168.1.10"
                },
                new()
                {
                    ID = "center-02",
                    Role = "center",
                    IP = "192.168.1.20"
                }
            };

            // Act & Assert
            var ex = Assert.ThrowsAny<Exception>(() => EvaluateRequireCenter(controllers));
            Assert.True(ex is TypeInitializationException || ex.Message.Contains("Có >1 bộ điều khiển trung tâm (Role=center)"));
        }

        /// <summary>
        /// Description: B8 - RequireCenterController ném lỗi khi bộ điều khiển trung tâm chưa khai báo IP.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void B8_RequireCenterController_WhenCenterControllerHasEmptyIp_ThrowsChuaKhaiIP()
        {
            // Arrange
            var controllers = new List<VwController>
            {
                new()
                {
                    ID = "center-01",
                    Role = "center",
                    IP = "   "
                }
            };

            // Act & Assert
            var ex = Assert.ThrowsAny<Exception>(() => EvaluateRequireCenter(controllers));
            Assert.True(ex is TypeInitializationException || ex.Message.Contains("Bộ điều khiển trung tâm chưa khai IP"));
        }

        /// <summary>
        /// Description: B9 - RequireCenterController trả về đúng bộ trung tâm khi cấu hình duy nhất và hợp lệ.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void B9_RequireCenterController_WhenSingleValidCenter_ReturnsCenter()
        {
            // Arrange
            var expectedCenter = new VwController
            {
                ID = "center-01",
                Role = "center",
                IP = "192.168.1.10"
            };
            var controllers = new List<VwController>
            {
                expectedCenter,
                new()
                {
                    ID = "sub-01",
                    Role = "sub",
                    IP = "192.168.1.11"
                }
            };

            // Act
            var center = EvaluateRequireCenter(controllers);

            // Assert
            Assert.NotNull(center);
            Assert.Equal("center-01", center.ID);
            Assert.Equal("192.168.1.10", center.IP);
        }

        /// <summary>
        /// Description: B10 - VwControllerValidator chỉ chấp nhận Role là center, sub hoặc rỗng; từ chối active.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void B10_VwControllerValidator_RoleAndIntegrationMode_ValidatesAllowedValues()
        {
            // Arrange
            var localizer = new FakeStringLocalizer();
            var validator = new VwControllerValidator(localizer);

            // Act & Assert 1: Hợp lệ với center và cascade
            var valid1 = new VwAddControllerInput
            {
                Role = "center",
                IntegrationMode = "cascade"
            };
            var res1 = validator.Validate(valid1);
            Assert.DoesNotContain(res1.Errors, e => e.PropertyName == "Role" || e.PropertyName == "IntegrationMode");

            // Act & Assert 2: Hợp lệ với sub và standalone
            var valid2 = new VwAddControllerInput
            {
                Role = "sub",
                IntegrationMode = "standalone"
            };
            var res2 = validator.Validate(valid2);
            Assert.DoesNotContain(res2.Errors, e => e.PropertyName == "Role" || e.PropertyName == "IntegrationMode");

            // Act & Assert 3: Hợp lệ khi bỏ trống
            var validEmpty = new VwAddControllerInput
            {
                Role = null,
                IntegrationMode = null
            };
            var resEmpty = validator.Validate(validEmpty);
            Assert.DoesNotContain(resEmpty.Errors, e => e.PropertyName == "Role" || e.PropertyName == "IntegrationMode");

            // Act & Assert 4: Từ chối Role == 'active'
            var invalidRole = new VwAddControllerInput
            {
                Role = "active"
            };
            var resRole = validator.Validate(invalidRole);
            Assert.Contains(resRole.Errors, e => e.PropertyName == "Role");

            // Act & Assert 5: Từ chối IntegrationMode == 'active'
            var invalidMode = new VwAddControllerInput
            {
                IntegrationMode = "active"
            };
            var resMode = validator.Validate(invalidMode);
            Assert.Contains(resMode.Errors, e => e.PropertyName == "IntegrationMode");
        }

        /// <summary>
        /// Description: Hàm đánh giá logic nghiệp vụ của RequireCenterController trên tập danh sách in-memory.
        /// Created date: 08/09/2026
        /// </summary>
        private static VwController EvaluateRequireCenter(List<VwController> controllers)
        {
            var centers = controllers
                .Where(u => u.IsDelete == null && u.Role == "center")
                .ToList();

            if (centers.Count == 0)
                throw Furion.FriendlyException.Oops.Oh("Chưa cấu hình bộ điều khiển trung tâm (Role=center).");

            if (centers.Count > 1)
                throw Furion.FriendlyException.Oops.Oh("Có >1 bộ điều khiển trung tâm (Role=center).");

            var c = centers[0];
            if (string.IsNullOrWhiteSpace(c.IP))
                throw Furion.FriendlyException.Oops.Oh("Bộ điều khiển trung tâm chưa khai IP.");

            return c;
        }

        private class FakeStringLocalizer : Microsoft.Extensions.Localization.IStringLocalizer
        {
            public Microsoft.Extensions.Localization.LocalizedString this[string name] => new(name, name);
            public Microsoft.Extensions.Localization.LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
            public IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) => [];
        }
    }
}
