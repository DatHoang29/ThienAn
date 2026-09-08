using FluentValidation.TestHelper;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Controllers.Device.Validators;
using Module.VideoWall.Core.Dto.Device;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Dto.ISAPI;
using Xunit;

namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Description: Test suite cho bộ validator VwDeviceValidator, sử dụng IStringLocalizer từ Host
    /// Created date: 07/09/2026
    /// </summary>
    [Collection("api")]
    public class VwDeviceValidatorTests(Host host)
    {
        private readonly IStringLocalizer _localizer = host.Localizer;

        [Fact]
        public void TargetValidator_WhenBothControllerIdAndDeviceProvided_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceTargetValidator(_localizer);
            var input = new VwDeviceUserCheckInput
            {
                ControllerId = "ctrl-01",
                Device = new VwISAPIPassthroughDevice { Ip = "192.168.1.100", Account = "admin" }
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void TargetValidator_WhenNeitherControllerIdNorDeviceProvided_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceTargetValidator(_localizer);
            var input = new VwDeviceUserCheckInput
            {
                ControllerId = null,
                Device = null
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void TargetValidator_WhenOnlyControllerIdProvided_ShouldPass()
        {
            // Arrange
            var validator = new VwDeviceTargetValidator(_localizer);
            var input = new VwDeviceUserCheckInput
            {
                ControllerId = "ctrl-01"
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void TargetValidator_WhenAdhocDeviceHasInvalidPort_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceTargetValidator(_localizer);
            var input = new VwDeviceUserCheckInput
            {
                Device = new VwISAPIPassthroughDevice
                {
                    Ip = "192.168.1.100",
                    Account = "admin",
                    Port = 70000 // Out of range 1..65535
                }
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Device!.Port);
        }

        [Fact]
        public void WallValidator_WhenWallNoIsOutOfRange_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceWallValidator(_localizer);
            var input = new VwDeviceOutputsInput
            {
                ControllerId = "ctrl-01",
                WallNo = 9 // Max is 8
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.WallNo);
        }

        [Fact]
        public void SceneCreate_WhenNameContainsDiacritics_ShouldFail()
        {
            // Arrange — tên có dấu dẫn tới 4/4 lần badParameters trong log thực tế
            var validator = new VwDeviceSceneCreateValidator(_localizer);
            var input = new VwDeviceSceneCreateInput
            {
                ControllerId = "ctrl-01",
                Name = "Kịch bản 2: Cửa sổ CHỒNG nhau"
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void SceneCreate_WhenNameIsPureAscii_ShouldPass()
        {
            // Arrange — tên thuần ASCII được thiết bị chấp nhận trong log
            var validator = new VwDeviceSceneCreateValidator(_localizer);
            var input = new VwDeviceSceneCreateInput
            {
                ControllerId = "ctrl-01",
                Name = "Scene_8_20251121161431"
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void SceneCreate_WhenNameExceeds64Chars_ShouldFail()
        {
            // Arrange — max 64 ký tự theo capabilities
            var validator = new VwDeviceSceneCreateValidator(_localizer);
            var input = new VwDeviceSceneCreateInput
            {
                ControllerId = "ctrl-01",
                Name = new string('A', 65)
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void WindowSubStart_WhenSubIdIsOutOfRange_ShouldFail()
        {
            // Arrange — subId 1..16
            var validator = new VwDeviceWindowSubStartValidator(_localizer);
            var input = new VwDeviceWindowSubStartInput
            {
                ControllerId = "ctrl-01",
                WindowId = "16777217",
                SubId = 17
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SubId);
        }

        [Fact]
        public void WindowSubStart_WhenValid_ShouldPass()
        {
            // Arrange — subId 1..9 hợp lệ trong log
            var validator = new VwDeviceWindowSubStartValidator(_localizer);
            var input = new VwDeviceWindowSubStartInput
            {
                ControllerId = "ctrl-01",
                WindowId = "16777217",
                SubId = 1
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void WindowAdd_WhenRectWidthOrHeightIsZeroOrNegative_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceWindowAddValidator(_localizer);
            var input = new VwDeviceWindowAddInput
            {
                ControllerId = "ctrl-01",
                Window = new VwISAPIWindowRequest
                {
                    Rect = new VwISAPIRect
                    {
                        Width = 0,
                        Height = 1920,
                        Coordinate = new VwISAPICoordinate { X = 0, Y = 0 }
                    }
                }
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Window!.Rect!.Width);
        }
    }
}
