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

        [Fact]
        public void TargetValidator_WhenDeviceIpIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceTargetValidator(_localizer);
            var input = new VwDeviceUserCheckInput
            {
                Device = new VwISAPIPassthroughDevice
                {
                    Ip = "",
                    Account = "admin"
                }
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Device!.Ip);
        }

        [Fact]
        public void TargetValidator_WhenDeviceAccountIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceTargetValidator(_localizer);
            var input = new VwDeviceUserCheckInput
            {
                Device = new VwISAPIPassthroughDevice
                {
                    Ip = "192.168.1.100",
                    Account = ""
                }
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Device!.Account);
        }

        [Fact]
        public void TargetValidator_WhenDeviceAccountExceeds64Chars_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceTargetValidator(_localizer);
            var input = new VwDeviceUserCheckInput
            {
                Device = new VwISAPIPassthroughDevice
                {
                    Ip = "192.168.1.100",
                    Account = new string('A', 65)
                }
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Device!.Account);
        }

        [Fact]
        public void SceneRename_WhenSidIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceSceneRenameValidator(_localizer);
            var input = new VwDeviceSceneRenameInput
            {
                ControllerId = "ctrl-01",
                Sid = "",
                Name = "ValidSceneName"
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Sid);
        }

        [Fact]
        public void SceneRename_WhenNameHasDiacritics_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceSceneRenameValidator(_localizer);
            var input = new VwDeviceSceneRenameInput
            {
                ControllerId = "ctrl-01",
                Sid = "1",
                Name = "Đổi tên có dấu tiếng Việt"
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void SceneSave_WhenSidIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceSceneSaveValidator(_localizer);
            var input = new VwDeviceSceneSaveInput
            {
                ControllerId = "ctrl-01",
                Sid = ""
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Sid);
        }

        [Fact]
        public void SceneActivate_WhenSidIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceSceneActivateValidator(_localizer);
            var input = new VwDeviceSceneActivateInput
            {
                ControllerId = "ctrl-01",
                Sid = ""
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Sid);
        }

        [Fact]
        public void SceneInfo_WhenSidIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceSceneInfoValidator(_localizer);
            var input = new VwDeviceSceneInfoInput
            {
                ControllerId = "ctrl-01",
                Sid = ""
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Sid);
        }

        [Fact]
        public void WindowDelete_WhenWindowIdIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceWindowDeleteValidator(_localizer);
            var input = new VwDeviceWindowDeleteInput
            {
                ControllerId = "ctrl-01",
                WindowId = ""
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.WindowId);
        }

        [Fact]
        public void WindowTop_WhenWindowIdIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceWindowTopValidator(_localizer);
            var input = new VwDeviceWindowTopInput
            {
                ControllerId = "ctrl-01",
                WindowId = ""
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.WindowId);
        }

        [Fact]
        public void WindowBottom_WhenWindowIdIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceWindowBottomValidator(_localizer);
            var input = new VwDeviceWindowBottomInput
            {
                ControllerId = "ctrl-01",
                WindowId = ""
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.WindowId);
        }

        [Fact]
        public void WindowGet_WhenWindowIdIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceWindowGetValidator(_localizer);
            var input = new VwDeviceWindowGetInput
            {
                ControllerId = "ctrl-01",
                WindowId = ""
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.WindowId);
        }

        [Fact]
        public void WindowUpdate_WhenWindowIdIsEmpty_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceWindowUpdateValidator(_localizer);
            var input = new VwDeviceWindowUpdateInput
            {
                ControllerId = "ctrl-01",
                WindowId = ""
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.WindowId);
        }

        [Fact]
        public void WindowAdd_WhenWindowIsNull_ShouldFail()
        {
            // Arrange
            var validator = new VwDeviceWindowAddValidator(_localizer);
            var input = new VwDeviceWindowAddInput
            {
                ControllerId = "ctrl-01",
                Window = null
            };

            // Act
            var result = validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Window);
        }
    }
}
