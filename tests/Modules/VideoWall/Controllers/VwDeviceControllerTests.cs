using Microsoft.Extensions.Localization;
using Module.VideoWall.Controllers.Device.Queries;
using Module.VideoWall.Controllers.Device.Validators;
using Module.VideoWall.Core.Dto.Device;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Dto.ISAPI;
using Xunit;

namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Description: Bộ kiểm thử tích hợp cho VwDeviceController (Proxy API thiết bị Video Wall)
    /// Created date: 07/09/2026
    /// </summary>
    [Collection("api")]
    public class VwDeviceControllerTests(Host host)
    {
        private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
        private readonly VwISAPIMockServerHikvision _mock = host.MockServer;
        private readonly IStringLocalizer _localizer = host.Localizer;

        private VwISAPIPassthroughDevice MockDevice => new()
        {
            Ip = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            Password = VwISAPIMockServerHikvision.DefaultPassword
        };

        [Fact]
        public async Task VwDeviceQuery_UserCheck_ReturnsExpectedResult_Test()
        {
            // Arrange
            var input = new VwDeviceUserCheckInput
            {
                Device = MockDevice
            };

            // Act
            var result = await _bus.InvokeAsync<VwDeviceGenericOutput<VwISAPIUserCheckResponse>>(input);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Step);
            Assert.Equal("UserCheck", result.Step.Name);
        }

        [Fact]
        public async Task VwDeviceQuery_Capabilities_ReturnsCapabilities_Test()
        {
            // Arrange
            var input = new VwDeviceCapabilitiesInput
            {
                Device = MockDevice
            };

            // Act
            var result = await _bus.InvokeAsync<VwDeviceGenericOutput<VwISAPICapabilitiesResponse>>(input);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Step);
            Assert.Equal("GetCapabilities", result.Step.Name);
        }

        [Fact]
        public async Task VwDeviceQuery_Walls_ReturnsVideoWalls_Test()
        {
            // Arrange
            var input = new VwDeviceWallsInput
            {
                Device = MockDevice
            };

            // Act
            var result = await _bus.InvokeAsync<VwDeviceGenericOutput<VwISAPIVideoWallList>>(input);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Step);
            Assert.Equal("GetVideoWalls", result.Step.Name);
        }

        /// <summary>
        /// Description: Chuỗi endpoint ghi vào Step của các handler ĐỌC phải trùng đúng path
        ///              mà thiết bị thật sự nhận (nhật ký không được nói sai việc mình vừa làm).
        ///              Bắt lại lỗi tiền tố hư cấu "Video/walls/..." thay vì "VideoWall/...".
        /// Created date: 07/09/2026
        /// </summary>
        [Fact]
        public async Task VwDeviceQuery_StepEndpoint_MatchesActualDeviceCall_Test()
        {
            // Arrange
            _mock.ResetDefaults();

            // Act + Assert — mỗi handler đọc: Step.Endpoint == path thiết bị nhận
            var walls = await _bus.InvokeAsync<VwDeviceGenericOutput<VwISAPIVideoWallList>>(
                new VwDeviceWallsInput { Device = MockDevice });
            AssertStepEndpointMatchesDeviceCall(walls.Step, "GET");

            var caps = await _bus.InvokeAsync<VwDeviceGenericOutput<VwISAPICapabilitiesResponse>>(
                new VwDeviceCapabilitiesInput { Device = MockDevice });
            AssertStepEndpointMatchesDeviceCall(caps.Step, "GET");

            var inputs = await _bus.InvokeAsync<VwDeviceGenericOutput<VwISAPIInputChannelsResponse>>(
                new VwDeviceInputChannelsInput { Device = MockDevice });
            AssertStepEndpointMatchesDeviceCall(inputs.Step, "GET");

            var outputs = await _bus.InvokeAsync<VwDeviceGenericOutput<VwISAPIOutputsResponse>>(
                new VwDeviceOutputsInput { Device = MockDevice });
            AssertStepEndpointMatchesDeviceCall(outputs.Step, "GET");

            var windows = await _bus.InvokeAsync<VwDeviceGenericOutput<VwISAPIWallWindowList>>(
                new VwDeviceWindowListInput { Device = MockDevice });
            AssertStepEndpointMatchesDeviceCall(windows.Step, "GET");

            var running = await _bus.InvokeAsync<VwDeviceGenericOutput<VwISAPISceneStatusResponse>>(
                new VwDeviceSceneRunningInput { Device = MockDevice });
            AssertStepEndpointMatchesDeviceCall(running.Step, "GET");
        }

        /// <summary>
        /// Description: Cùng bất biến cho các handler GHI — Step.Endpoint phải là path thật
        ///              (VideoWall/{n}/windows/{id}/top, .../scene/{sid}/activate...),
        ///              không phải chuỗi hư cấu "position/top", "sceneControl".
        /// Created date: 07/09/2026
        /// </summary>
        [Fact]
        public async Task VwDeviceCommand_StepEndpoint_MatchesActualDeviceCall_Test()
        {
            // Arrange
            _mock.ResetDefaults();

            var deleteAllInput = new VwDeviceWindowDeleteAllInput { Device = MockDevice };
            var topInput = new VwDeviceWindowTopInput { Device = MockDevice, WindowId = "33554433" };
            var activateInput = new VwDeviceSceneActivateInput { Device = MockDevice, Sid = "1" };

            Assert.True((await new VwDeviceWindowDeleteAllValidator(_localizer).ValidateAsync(deleteAllInput)).IsValid);
            Assert.True((await new VwDeviceWindowTopValidator(_localizer).ValidateAsync(topInput)).IsValid);
            Assert.True((await new VwDeviceSceneActivateValidator(_localizer).ValidateAsync(activateInput)).IsValid);

            // Act + Assert
            var deleteAll = await _bus.InvokeAsync<VwSetupSceneStep>(deleteAllInput);
            AssertStepEndpointMatchesDeviceCall(deleteAll, "DELETE");

            var top = await _bus.InvokeAsync<VwSetupSceneStep>(topInput);
            AssertStepEndpointMatchesDeviceCall(top, "PUT");

            var activate = await _bus.InvokeAsync<VwSetupSceneStep>(activateInput);
            AssertStepEndpointMatchesDeviceCall(activate, "PUT");
        }

        /// <summary>
        /// Description: Helper — Step phải thành công, đúng Method, và Endpoint ghi trong nhật ký
        ///              phải khớp một request có thật trong nhật ký thiết bị (bỏ query string).
        /// Created date: 07/09/2026
        /// </summary>
        private void AssertStepEndpointMatchesDeviceCall(VwSetupSceneStep? step, string expectedMethod)
        {
            Assert.NotNull(step);
            Assert.True(step.Success, $"Step '{step.Name}' thất bại: {step.Message}");
            Assert.Equal(expectedMethod, step.Method);
            Assert.DoesNotContain("Video/walls", step.Endpoint);

            var loggedPath = step.Endpoint.Split('?')[0].Trim('/');
            var matched = _mock.ReceivedRequests.Any(entry =>
            {
                var parts = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    return false;

                var method = parts[1];
                var path = parts[2].Split('?')[0].Trim('/');

                return string.Equals(method, expectedMethod, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(path, loggedPath, StringComparison.OrdinalIgnoreCase);
            });

            Assert.True(
                matched,
                $"Step '{step.Name}' ghi endpoint '{step.Endpoint}' nhưng thiết bị không nhận request "
                + $"'{expectedMethod} /{loggedPath}'. Nhật ký: {string.Join(" | ", _mock.ReceivedRequests)}");
        }
    }
}
