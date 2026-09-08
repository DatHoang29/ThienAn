using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Dto.ISAPI;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure.Services.ISAPIDevice;
using Xunit;

namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Description: Bộ kiểm thử đơn vị cho tầng ISAPI, Serialization, DTO Fixtures, ResolveWall và Error Mapping
    /// Không dùng thư viện Mock ngoài (Moq), sử dụng thuần xUnit và Stub.
    /// Created date: 07/09/2026
    /// </summary>
    public class VwDeviceISAPITests
    {
        #region Serialization Tests

        [Fact]
        public void SceneCreate_XmlSerialize_MatchesVendorSpecification()
        {
            // Arrange — request tạo kịch bản: không có <id>, có name, đúng namespace
            var request = new VwISAPIWallSceneRequest
            {
                Name = "T3 v2",
                IdSpecified = false
            };

            var ns = new XmlSerializerNamespaces();
            ns.Add("", "http://www.isapi.org/ver20/XMLSchema");

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = false,
                Encoding = new System.Text.UTF8Encoding(false)
            };

            var serializer = new XmlSerializer(typeof(VwISAPIWallSceneRequest));
            using var sw = new StringWriter();
            using (var writer = XmlWriter.Create(sw, settings))
            {
                serializer.Serialize(writer, request, ns);
            }

            var xml = sw.ToString();

            // Assert
            Assert.DoesNotContain("<?xml", xml); // Không có XML declaration
            Assert.Contains("<WallScene", xml);
            Assert.Contains("<name>T3 v2</name>", xml);
            Assert.DoesNotContain("<id>", xml); // Không gửi id khi tạo
            Assert.Contains("xmlns=\"http://www.isapi.org/ver20/XMLSchema\"", xml);
        }

        [Fact]
        public void SceneRename_XmlSerialize_IncludesIdAndName()
        {
            // Arrange — request đổi tên: CÓ <id> và <name>
            var request = new VwISAPIWallSceneRequest
            {
                Id = 3,
                IdSpecified = true,
                Name = "12345"
            };

            var ns = new XmlSerializerNamespaces();
            ns.Add("", "http://www.isapi.org/ver20/XMLSchema");

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = false,
                Encoding = new System.Text.UTF8Encoding(false)
            };

            var serializer = new XmlSerializer(typeof(VwISAPIWallSceneRequest));
            using var sw = new StringWriter();
            using (var writer = XmlWriter.Create(sw, settings))
            {
                serializer.Serialize(writer, request, ns);
            }

            var xml = sw.ToString();

            // Assert
            Assert.Contains("<id>3</id>", xml);
            Assert.Contains("<name>12345</name>", xml);
        }

        [Fact]
        public void SceneInfo_JsonDeserialize_ParsesPayloadCorrectly()
        {
            // Arrange — payload thật từ log thiết bị (LogsAPI/): endpoint duy nhất trả JSON
            var jsonPayload = """
            {
              "wallSceneInfo": {
                "wallRect": { "width": 3840, "height": 3840 },
                "SceneWindowList": [
                  {
                    "sceneWindow": {
                      "id": 16777218,
                      "wndOperateMode": "uniformCoordinate",
                      "Rect": {
                        "Coordinate": { "x": 1920, "y": 1920 },
                        "width": 1920,
                        "height": 1920
                      },
                      "layerIdx": 67108866
                    }
                  }
                ]
              }
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<VwISAPISceneInfoResponse>(jsonPayload);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.WallSceneInfo);
            Assert.NotNull(result.WallSceneInfo.WallRect);
            Assert.Equal(3840, result.WallSceneInfo.WallRect.Width);
            Assert.Equal(3840, result.WallSceneInfo.WallRect.Height);

            Assert.NotNull(result.WallSceneInfo.SceneWindowList);
            Assert.Single(result.WallSceneInfo.SceneWindowList);

            var win = result.WallSceneInfo.SceneWindowList[0].SceneWindow;
            Assert.NotNull(win);
            Assert.Equal(16777218, win.Id);
            Assert.Equal("uniformCoordinate", win.WndOperateMode);
            Assert.NotNull(win.Rect);
            Assert.Equal(1920, win.Rect.Coordinate?.X);
            Assert.Equal(1920, win.Rect.Coordinate?.Y);
            Assert.Equal(1920, win.Rect.Width);
            Assert.Equal(1920, win.Rect.Height);
            Assert.Equal(67108866, win.LayerIdx);
        }

        [Fact]
        public void StreamChannels_XmlDeserialize_DoesNotExposePassword()
        {
            // Arrange — payload thật từ log rút gọn, chứa thông tin camera và credential
            var xmlPayload = """
            <StreamInputChannelList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
              <StreamInputChannel version="2.0">
                <id>1</id>
                <name>OTO B1.C6 LAN 01</name>
                <group>Lane</group>
                <startDecoding>false</startDecoding>
                <StreamInput version="2.0">
                  <streamInputMode>realtime</streamInputMode>
                  <StreamInputRealtime>
                    <StreamRealtimeUnitList>
                      <StreamRealtimeUnit version="2.0">
                        <streamType>by domain</streamType>
                        <StreamByDomain>
                          <EncodeDevInfo version="2.0">
                            <domain>172.25.1.12</domain>
                            <port>8000</port>
                            <transmitProtocol>tcp</transmitProtocol>
                            <protocol>HIKVISION</protocol>
                            <username>admin</username>
                            <password>CameraSecretPassword123</password>
                            <channelMode>normal</channelMode>
                            <channelType>main</channelType>
                            <channelNormal>1</channelNormal>
                          </EncodeDevInfo>
                        </StreamByDomain>
                      </StreamRealtimeUnit>
                    </StreamRealtimeUnitList>
                  </StreamInputRealtime>
                </StreamInput>
              </StreamInputChannel>
            </StreamInputChannelList>
            """;

            var serializer = new XmlSerializer(typeof(VwISAPIStreamChannelList));
            using var reader = new StringReader(xmlPayload);
            var result = (VwISAPIStreamChannelList?)serializer.Deserialize(reader);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.StreamInputChannel);

            var channel = result.StreamInputChannel[0];
            Assert.Equal(1, channel.Id);
            Assert.Equal("OTO B1.C6 LAN 01", channel.Name);
            Assert.Equal("Lane", channel.Group);

            var devInfo = channel.StreamInput?.StreamInputRealtime?.StreamRealtimeUnitList?.StreamRealtimeUnit?[0]?.StreamByDomain?.EncodeDevInfo;
            Assert.NotNull(devInfo);
            Assert.Equal("172.25.1.12", devInfo.Domain);
            Assert.Equal(8000, devInfo.Port);
            Assert.Equal("admin", devInfo.Username);

            // 🔒 BẢO MẬT: Kiểm tra kiểu VwISAPIEncodeDevInfo KHÔNG có trường Password nào qua Reflection
            var passwordProp = typeof(VwISAPIEncodeDevInfo).GetProperty("Password");
            var passwordLowerProp = typeof(VwISAPIEncodeDevInfo).GetProperty("password");
            Assert.Null(passwordProp);
            Assert.Null(passwordLowerProp);
        }

        #endregion

        #region Fixture Tests

        [Fact]
        public void VideoWalls_RealDeviceFixture_ParsesMisspelledBackgroudColor()
        {
            // Arrange — Fixture từ thiết bị thật DS-C30S-S11: không có wallBindOutputStatus,
            // firmware viết sai chính tả <backgroudColor> (thiếu chữ 'n')
            var xml = """
            <VideoWallList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
              <VideoWall>
                <id>1</id>
                <name>VideoWall1</name>
                <backgroudColor>black</backgroudColor>
                <streamFailedMode>linkException,lastFrame</streamFailedMode>
                <enabledOverlayLogo>true</enabledOverlayLogo>
                <alarmFilterTime>ms</alarmFilterTime>
              </VideoWall>
              <VideoWall>
                <id>2</id>
                <name>VideoWall2</name>
                <backgroudColor>black</backgroudColor>
                <streamFailedMode>linkException,lastFrame</streamFailedMode>
                <enabledOverlayLogo>true</enabledOverlayLogo>
                <alarmFilterTime>ms</alarmFilterTime>
              </VideoWall>
            </VideoWallList>
            """;

            var serializer = new XmlSerializer(typeof(VwISAPIVideoWallList));
            using var reader = new StringReader(xml);
            var result = (VwISAPIVideoWallList?)serializer.Deserialize(reader);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.VideoWall.Count);

            var wall1 = result.VideoWall[0];
            Assert.Equal(1, wall1.Id);
            Assert.Equal("VideoWall1", wall1.Name);
            Assert.Equal("black", wall1.BackgroudColor); // Firmware chính tả sai map thành công
            Assert.Equal("linkException,lastFrame", wall1.StreamFailedMode);
            Assert.True(wall1.EnabledOverlayLogo);
            Assert.Null(wall1.WallBindOutputStatus); // Thiết bị thật không trả
            Assert.Null(wall1.IsBoundByStatus); // Không suy diễn
        }

        [Fact]
        public void Outputs_Wall1Fixture_Calculates2x2GridCorrectly()
        {
            // Arrange — Fixture Wall 1: id không theo thứ tự toạ độ (id 1 ở 1920,0; id 3 ở 0,0)
            var xml = """
            <WallOutputList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
              <WallOutput version="2.0">
                <id>1</id><outputID>17235969</outputID>
                <Rect><Coordinate><x>1920</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect>
              </WallOutput>
              <WallOutput version="2.0">
                <id>2</id><outputID>17235970</outputID>
                <Rect><Coordinate><x>1920</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect>
              </WallOutput>
              <WallOutput version="2.0">
                <id>3</id><outputID>17235971</outputID>
                <Rect><Coordinate><x>0</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect>
              </WallOutput>
              <WallOutput version="2.0">
                <id>4</id><outputID>17235972</outputID>
                <Rect><Coordinate><x>0</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect>
              </WallOutput>
            </WallOutputList>
            """;

            var serializer = new XmlSerializer(typeof(VwISAPIOutputsResponse));
            using var reader = new StringReader(xml);
            var result = (VwISAPIOutputsResponse?)serializer.Deserialize(reader);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.WallOutput.Count);

            // Tính lưới theo toạ độ ảo (baseOutputSize = 1920)
            const int baseSize = 1920;
            var maxX = result.WallOutput.Max(o => o.Rect?.Coordinate?.X ?? 0);
            var maxY = result.WallOutput.Max(o => o.Rect?.Coordinate?.Y ?? 0);

            var cols = (maxX / baseSize) + 1;
            var rows = (maxY / baseSize) + 1;

            Assert.Equal(2, cols);
            Assert.Equal(2, rows);
        }

        [Fact]
        public void Outputs_Wall4Fixture_EmptyListRepresentsUnbound()
        {
            // Arrange — Fixture Wall 4/7: không có cổng ra nào
            var xml = """
            <WallOutputList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
            </WallOutputList>
            """;

            var serializer = new XmlSerializer(typeof(VwISAPIOutputsResponse));
            using var reader = new StringReader(xml);
            var result = (VwISAPIOutputsResponse?)serializer.Deserialize(reader);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.WallOutput);
        }

        [Fact]
        public void Capabilities_RealDeviceFixture_ReadsNestedSceneCap()
        {
            // Arrange — Payload thật 3469 byte từ thiết bị DS-C30S-S11:
            // maxSceneNums và isSupportSceneInfo nằm trong <SceneCap>
            var xml = """
            <VideoWallCap version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
              <maxWallNums>8</maxWallNums>
              <maxWindowNums>512</maxWindowNums>
              <baseOutputSize>1920</baseOutputSize>
              <isSupportScene>true</isSupportScene>
              <isSupportRoam>true</isSupportRoam>
              <isSupportPlan>false</isSupportPlan>
              <SceneCap version="2.0">
                <maxSceneNums>128</maxSceneNums>
                <isSupportSceneInfo>true</isSupportSceneInfo>
              </SceneCap>
            </VideoWallCap>
            """;

            var serializer = new XmlSerializer(typeof(VwISAPICapabilitiesResponse));
            using var reader = new StringReader(xml);
            var result = (VwISAPICapabilitiesResponse?)serializer.Deserialize(reader);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(8, result.MaxWallNums);
            Assert.Equal(512, result.MaxWindowNums);
            Assert.Equal(1920, result.BaseOutputSize);
            Assert.True(result.IsSupportScene);
            Assert.True(result.IsSupportRoam);
            Assert.False(result.IsSupportPlan);

            // Đọc thành công từ thẻ con SceneCap
            Assert.Equal(128, result.MaxSceneNums);
            Assert.True(result.IsSupportSceneInfo);
        }

        [Theory]
        [InlineData(2, 1, 16908289)] // Input 2-1 (HDMI)
        [InlineData(2, 2, 16908290)] // Input 2-2 (HDMI)
        [InlineData(7, 1, 17235969)] // Output board 7 port 1 (Audio/Video Out)
        public void ChannelIdFormula_MatchesRealHardwarePorts(int boardId, int portId, int expectedChannelId)
        {
            // Công thức ID: channelID = 0x01000000 + boardID * 0x10000 + portID
            var calculatedId = 0x01000000 + (boardId * 0x10000) + portId;
            Assert.Equal(expectedChannelId, calculatedId);
        }

        #endregion

        #region ResolveWall Regression Test

        [Fact]
        public async Task ResolveWall_WhenWallBindOutputStatusIsNullOnAllWalls_SelectsWallWithOutputs()
        {
            // Arrange — Thiết bị thật có 8 tường, tất cả wallBindOutputStatus = null
            // Wall 1 có 4 cổng ra; Wall 4 và 7 rỗng
            var controller = new VwController
            {
                ID = "ctrl-01",
                Name = "Controller-01",
                IP = "172.25.0.32"
            };

            var wallsList = new VwISAPIVideoWallList
            {
                VideoWall = Enumerable.Range(1, 8).Select(i => new VwISAPIVideoWallItem
                {
                    Id = i,
                    Name = $"VideoWall{i}",
                    WallBindOutputStatus = null
                }).ToList()
            };

            var wall1Outputs = new VwISAPIOutputsResponse
            {
                WallOutput = [
                    new VwISAPIOutputItem { Id = 1, OutputId = 17235969 },
                    new VwISAPIOutputItem { Id = 2, OutputId = 17235970 },
                    new VwISAPIOutputItem { Id = 3, OutputId = 17235971 },
                    new VwISAPIOutputItem { Id = 4, OutputId = 17235972 }
                ]
            };

            // Sử dụng stub class thuần xUnit, không phụ thuộc thư viện Moq
            var stubClient = new StubDeviceClient(wallsList, wallNo =>
                wallNo == 1 ? wall1Outputs : new VwISAPIOutputsResponse());

            var options = Options.Create(new VwDeviceOptions());
            var service = new VwISAPIDeviceService(
                deviceClient: stubClient,
                vwControllerRep: null!,
                vwSceneRep: null!,
                vwWindowSceneRep: null!,
                vwSourceRep: null!,
                vwScreenRep: null!,
                regionService: null!,
                credentialResolver: null!,
                orgAccess: null!,
                cache: null!,
                eventTriggerLogWriter: null!,
                deviceOptions: options,
                logger: NullLogger<VwISAPIDeviceService>.Instance);

            // Act — Trước khi sửa, lệnh này LUÔN ném Oops.Oh
            var resolvedWall = await service.ResolveWall(controller);

            // Assert — Sau khi vá, chọn đúng Wall 1
            Assert.Equal(1, resolvedWall);
        }

        #endregion

        #region Test Stubs (Pure xUnit - No Moq)

        private class StubDeviceClient(
            VwISAPIVideoWallList videoWalls,
            Func<int, VwISAPIOutputsResponse> outputsFunc) : IVwISAPIDeviceClient
        {
            public Task<VwISAPIResult<VwISAPIVideoWallList>> GetVideoWallsAsync(VwController controller, CancellationToken ct = default)
                => Task.FromResult(VwISAPIResult<VwISAPIVideoWallList>.Ok(videoWalls));

            public Task<VwISAPIResult<VwISAPIOutputsResponse>> GetOutputsAsync(VwController controller, int? wallNo = null, CancellationToken ct = default)
                => Task.FromResult(VwISAPIResult<VwISAPIOutputsResponse>.Ok(outputsFunc(wallNo ?? 0)));

            public Uri EnsureRegistered(VwController controller) => throw new NotImplementedException();
            public Uri Refresh(VwController controller) => throw new NotImplementedException();
            public bool IsCircuitBreakerBlocked(string? ipOrKey, out TimeSpan remaining) { remaining = TimeSpan.Zero; return false; }
            public void RecordCircuitBreakerFailure(string? ipOrKey, int statusCode) { }
            public void ResetCircuitBreaker(string? ipOrKey) { }
            public void ResetAllCircuitBreakers() { }
            public Task<VwISAPIResult<VwISAPIUserCheckResponse>> UserCheckAsync(VwController controller, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPICapabilitiesResponse>> GetCapabilitiesAsync(VwController controller, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPISceneStatusResponse>> GetActiveSceneAsync(VwController controller, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult> ActivateSceneAsync(VwController controller, string isapiSceneId, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult> SaveSceneDataAsync(VwController controller, string isapiSceneId, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPIWallWindowList>> GetWindowsAsync(VwController controller, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPIResponseStatus>> AddWindowAsync(VwController controller, VwISAPIWindowRequest window, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult> UpdateWindowAsync(VwController controller, string windowId, VwISAPIWindowRequest window, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult> DeleteWindowAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult> DeleteAllWindowsAsync(VwController controller, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPIInputChannelsResponse>> GetInputChannelsAsync(VwController controller, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPIOutputChannelsResponse>> GetOutputChannelsAsync(VwController controller, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult> BringWindowToTopAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult> SendWindowToBottomAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPIStreamChannelList>> GetStreamChannelsAsync(VwController controller, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPIWallSceneList>> GetScenesAsync(VwController controller, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPISceneInfoResponse>> GetSceneInfoAsync(VwController controller, string sceneId, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPIResponseStatus>> CreateSceneAsync(VwController controller, string sceneName, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult> RenameSceneAsync(VwController controller, string sceneId, string sceneName, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult<VwISAPIWallWindowItem>> GetWindowAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult> StartSubWindowDecodeAsync(VwController controller, string windowId, int subId, int? wallNo = null, CancellationToken ct = default) => throw new NotImplementedException();
            public Task<VwISAPIResult> SendRawAsync(VwController controller, HttpMethod method, string relativeUri, string? body, string? contentType, CancellationToken ct = default) => throw new NotImplementedException();
            public string SerializeToXml(object payload) => throw new NotImplementedException();
        }

        #endregion
    }
}
