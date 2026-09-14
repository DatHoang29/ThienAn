using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
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
        public void ResolveDeviceUri_ResolvesControllerIpAndDefaultPort()
        {
            // controller.IP = "10.0.0.5" -> http://10.0.0.5:80/
            var controller = new VwController { IP = "10.0.0.5" };
            var config = new VwDeviceConnectionOptions();

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
            var config = new VwDeviceConnectionOptions();

            var uri = VwISAPIDeviceClient.ResolveDeviceUri(controller, config);

            Assert.Equal("http://10.0.0.5:8000/", uri.ToString());
        }

        [Fact]
        public void ResolveDeviceUri_WhenConfigPortSpecified_ConfigPortWinsOverDefault()
        {
            // config.Port = 8080 thắng port mặc định
            var controller = new VwController { IP = "10.0.0.5" };
            var config = new VwDeviceConnectionOptions { Port = 8080 };

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
            var config = new VwDeviceConnectionOptions { Ip = string.Empty };

            var ex = Assert.ThrowsAny<Exception>(() => VwISAPIDeviceClient.ResolveDeviceUri(controller, config));

            Assert.Contains("Chưa cấu hình IP thiết bị. Khai VideoWall:Device:Ip trong appsettings.json (Worker ITS.VideoWall), hoặc điền IP cho bản ghi VwController.", ex.Message);
        }

        [Fact]
        public void ResolveDeviceUri_WhenIsLocalTrue_OverridesControllerIpAndPort()
        {
            // controller.IP = "10.10.8.30:9999", nhưng isLocal = true -> cưỡng chế replace bằng 127.0.0.1:18080
            var controller = new VwController { IP = "10.10.8.30:9999" };
            var config = new VwDeviceConnectionOptions
            {
                Ip = "127.0.0.1",
                Port = 18080
            };

            var uri = VwISAPIDeviceClient.ResolveDeviceUri(controller, config, isLocal: true);

            Assert.Equal("http://127.0.0.1:18080/", uri.ToString());
        }

        [Fact]
        public void CredentialResolver_WhenIsLocalTrue_OverridesControllerCredentials()
        {
            var controller = new VwController
            {
                Account = "real_admin",
                PassWord = "real_password"
            };

            var options = Microsoft.Extensions.Options.Options.Create(new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions
                {
                    Account = "mock_admin",
                    Password = "mock_password"
                }
            });

            var inMemoryConfig = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["VideoWall:Device:IsLocal"] = "true"
                })
                .Build();

            var resolver = new VwISAPICredentialResolver(options, inMemoryConfig);
            var (account, password) = resolver.Resolve(controller);

            Assert.Equal("mock_admin", account);
            Assert.Equal("mock_password", password);
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
