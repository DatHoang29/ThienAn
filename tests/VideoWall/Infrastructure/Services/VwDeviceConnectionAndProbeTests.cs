using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using Module.VideoWall.Core.Dto.ISAPI;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Extensions;
using ITS.VideoWall.Services.ISAPIDevice;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Author: Đạt
    /// Description: Bộ kiểm thử đơn vị cho cấu hình kết nối P2 và trường nullable Probe P4
    /// Created date: 08/09/2026
    /// </summary>
    [Collection("api")]
    public class VwDeviceConnectionAndProbeTests(Host host)
    {
        #region P2 Connection Resolution Tests

        [Fact]
        public void ResolveDeviceUri_WhenUseMockDeviceIsTrue_UsesConfigIgnoringControllerIp()
        {
            // UseMockDevice = true -> dùng config, KHÔNG đọc controller.IP (truyền controller có IP khác để chứng minh)
            var controller = new VwController { IP = "10.99.99.99:9999" };
            var config = new VwDeviceConnectionOptions
            {
                UseMockDevice = true,
                Ip = "127.0.0.1",
                Port = 8080,
                Scheme = "http"
            };

            var uri = VwISAPIDeviceClient.ResolveDeviceUri(controller, config);

            Assert.Equal("http://127.0.0.1:8080/", uri.ToString());
        }

        [Fact]
        public void ResolveDeviceUri_WhenUseMockDeviceIsFalse_ResolvesControllerIpAndDefaultPort()
        {
            // UseMockDevice = false, controller.IP = "10.0.0.5" -> http://10.0.0.5:80/
            var controller = new VwController { IP = "10.0.0.5" };
            var config = new VwDeviceConnectionOptions { UseMockDevice = false };

            var uri = VwISAPIDeviceClient.ResolveDeviceUri(controller, config);

            Assert.Equal("http", uri.Scheme);
            Assert.Equal("10.0.0.5", uri.Host);
            Assert.Equal(80, uri.Port);
            Assert.Equal("/", uri.AbsolutePath);
        }

        [Fact]
        public void ResolveDeviceUri_WhenControllerIpContainsPort_SplitsPortCorrectly()
        {
            // controller.IP = "10.0.0.5:8000" -> port tách đúng 8000
            var controller = new VwController { IP = "10.0.0.5:8000" };
            var config = new VwDeviceConnectionOptions { UseMockDevice = false };

            var uri = VwISAPIDeviceClient.ResolveDeviceUri(controller, config);

            Assert.Equal("http://10.0.0.5:8000/", uri.ToString());
        }

        [Fact]
        public void ResolveDeviceUri_WhenConfigPortSpecified_ConfigPortWinsOverDefault()
        {
            // config.Port = 8080 thắng port mặc định
            var controller = new VwController { IP = "10.0.0.5" };
            var config = new VwDeviceConnectionOptions { UseMockDevice = false, Port = 8080 };

            var uri = VwISAPIDeviceClient.ResolveDeviceUri(controller, config);

            Assert.Equal("http://10.0.0.5:8080/", uri.ToString());
        }

        [Fact]
        public void ResolveDeviceUri_WhenControllerIpEmptyAndConfigHasIp_UsesConfig()
        {
            // controller.IP rỗng + config có Ip -> dùng config
            var controller = new VwController { IP = string.Empty };
            var config = new VwDeviceConnectionOptions
            {
                UseMockDevice = false,
                Ip = "192.168.1.50",
                Port = 8000
            };

            var uri = VwISAPIDeviceClient.ResolveDeviceUri(controller, config);

            Assert.Equal("http://192.168.1.50:8000/", uri.ToString());
        }

        [Fact]
        public void ResolveDeviceUri_WhenBothControllerAndConfigIpEmpty_ThrowsBusinessExceptionWithExactMessage()
        {
            // Cả hai rỗng -> lỗi nghiệp vụ có thông báo chỉ đúng key cấu hình
            var controller = new VwController { IP = string.Empty };
            var config = new VwDeviceConnectionOptions { UseMockDevice = false, Ip = string.Empty };

            var ex = Assert.ThrowsAny<Exception>(() => VwISAPIDeviceClient.ResolveDeviceUri(controller, config));

            Assert.Contains("Chưa cấu hình IP thiết bị. Khai VideoWall:Device:Ip trong Configuration/DeviceIntegration.json, hoặc điền IP cho bản ghi VwController.", ex.Message);
        }

        [Theory]
        [InlineData("ftp")]
        [InlineData("ws")]
        [InlineData("ssh")]
        public void ValidateDeviceOptions_WhenSchemeInvalid_ThrowsInvalidOperationException(string invalidScheme)
        {
            // Scheme = "ftp" -> khởi động thất bại
            var config = new VwDeviceConnectionOptions { Scheme = invalidScheme };

            Assert.Throws<InvalidOperationException>(() => Module.VideoWall.Extensions.ServiceCollectionExtensions.ValidateDeviceOptions(config));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(70000)]
        public void ValidateDeviceOptions_WhenPortOutOfRange_ThrowsInvalidOperationException(int invalidPort)
        {
            var config = new VwDeviceConnectionOptions { Port = invalidPort };

            Assert.Throws<InvalidOperationException>(() => Module.VideoWall.Extensions.ServiceCollectionExtensions.ValidateDeviceOptions(config));
        }

        [Fact]
        public void ValidateDeviceOptions_WhenUseMockDeviceIsTrueAndIpIsEmpty_ThrowsInvalidOperationException()
        {
            var config = new VwDeviceConnectionOptions { UseMockDevice = true, Ip = string.Empty };

            Assert.Throws<InvalidOperationException>(() => Module.VideoWall.Extensions.ServiceCollectionExtensions.ValidateDeviceOptions(config));
        }

        #endregion

        #region P4 Nullable Capabilities & DTO Tests

        [Fact]
        public void CapabilitiesResponse_WhenFieldsOmittedInXml_DeserializesToNull()
        {
            // Deserialize response Probe THIẾU 4 trường -> ra null, không ném, không ra 0
            var xml = """
                <VideoWallCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
                  <maxWallNums>8</maxWallNums>
                </VideoWallCap>
                """;

            var serializer = new XmlSerializer(typeof(VwISAPICapabilitiesResponse));
            using var reader = new StringReader(xml);
            var caps = (VwISAPICapabilitiesResponse)serializer.Deserialize(reader)!;

            Assert.NotNull(caps);
            Assert.Null(caps.IsSupportScene);
            Assert.Null(caps.MaxWindowNums);
            Assert.Null(caps.MaxSceneNums);
            Assert.Null(caps.BaseOutputSize);
        }

#if NET10_0_WINDOWS
        [Fact]
        public void WpfProbeDto_WhenBackendReturnsNull_DeserializesWithoutException()
        {
            // Test WPF: deserialize "maxSceneNums": null vào DTO của WPF không ném JsonException
            var json = """
                {
                    "controllerId": "ctrl-1",
                    "reachable": true,
                    "isSupportScene": null,
                    "maxWindowNums": null,
                    "maxSceneNums": null,
                    "baseOutputSize": null
                }
                """;

            var wpfDto = JsonSerializer.Deserialize<Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(wpfDto);
            Assert.Null(wpfDto.IsSupportScene);
            Assert.Null(wpfDto.MaxWindowNums);
            Assert.Null(wpfDto.MaxSceneNums);
            Assert.Null(wpfDto.BaseOutputSize);
        }

        [Fact]
        public void WpfSetupSceneDto_WhenBackendReturnsNull_DeserializesWithoutException()
        {
            var json = """
                {
                    "controllerId": "ctrl-1",
                    "sceneId": "scene-1",
                    "maxSceneNums": null
                }
                """;

            var wpfDto = JsonSerializer.Deserialize<Module.VideoWall.WPF.Api.Dto.VwSetupSceneOutput>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(wpfDto);
            Assert.Null(wpfDto.MaxSceneNums);
        }
#endif

        #endregion
    }
}
