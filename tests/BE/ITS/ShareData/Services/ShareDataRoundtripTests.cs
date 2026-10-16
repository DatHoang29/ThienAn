using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Controllers.Inbound.Commands;
using Modules.Integration.ShareData;
using Modules.Integration.ShareData.Encryption;
using Modules.Integration.ShareData.Formats;
using Module.ShareData.Core.Dto.Inbound;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Core.Interfaces;
using ShareDataWorker.Infrastructure.Logging;
using Shared.DTO.Enums;
using SqlSugar;

namespace Tests.ShareData.Services
{
    /// <summary>
    /// Description: Bộ kiểm thử roundtrip hai chiều gửi – nhận chuẩn hoá (#14).
    ///              - Kiểm thử tầng định dạng payload Formats (Json, KafkaJson, Csv)
    ///              - Kiểm thử phân giải khóa và Codec hai chiều (Outbound vs Inbound)
    ///              - Kiểm thử roundtrip toàn trình gói 101/103 với cùng bộ khung TargetShapeJson S,
    ///                hỗ trợ MERGE idempotency khi gửi trùng gói.
    /// Created date: 14/10/2026
    /// </summary>
    [Collection("api")]
    public class ShareDataRoundtripTests(Host host)
    {
        private readonly Host _host = host;
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly ISqlSugarClient _inboundDb = host.Services.GetRequiredService<ISqlSugarClient>()
            .AsTenant().GetConnectionScope(ShareDataWorker.Extensions.ShareDataWorkerExtensions.InboundConnectionKey);

        private static readonly byte[] TestAes256Key =
        [
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
        ];

        #region 1. Unit Tests tầng Định dạng (Formats)

        [Fact]
        public void JsonFormat_ToAndFromTransport_RoundtripMatches_Test()
        {
            // Arrange
            var format = new JsonFormat();
            var ctx = new FormatContext("PARTNER_01", "101", 1, DateTime.UtcNow);
            var originalJson = """[{"id":"Z1","val":100},{"id":"Z2","val":200}]""";

            // Act
            var payload = format.ToTransport(originalJson, ctx);
            var restoredJson = format.FromTransport(payload.Bytes, ctx);

            // Assert
            Assert.Equal("application/json", payload.ContentType);
            Assert.Equal(".json", payload.FileExtension);
            Assert.Equal(originalJson, restoredJson);
        }

        [Fact]
        public void KafkaJsonFormat_ToTransport_HasExpectedHeadersAndRoundtripMatches_Test()
        {
            // Arrange
            var format = new KafkaJsonFormat();
            var now = DateTime.UtcNow;
            var ctx = new FormatContext("PARTNER_KAFKA", "103", 42, now);
            var originalJson = """{"records":[{"plate":"29A-12345","speed":80}]}""";

            // Act
            var payload = format.ToTransport(originalJson, ctx);
            var restoredJson = format.FromTransport(payload.Bytes, ctx);

            // Assert
            Assert.Equal("application/json", payload.ContentType);
            Assert.True(payload.Headers.TryGetValue("content-type", out var ct) && ct == "application/json");
            Assert.True(payload.Headers.TryGetValue("PartnerCode", out var pCode) && pCode == "PARTNER_KAFKA");
            Assert.True(payload.Headers.TryGetValue("PacketCode", out var pktCode) && pktCode == "103");
            Assert.True(payload.Headers.TryGetValue("SerialNbr", out var sNbr) && sNbr == "42");
            Assert.True(payload.Headers.TryGetValue("ProcessedAt", out var pAt) && !string.IsNullOrWhiteSpace(pAt));
            Assert.Equal(originalJson, restoredJson);
        }

        [Fact]
        public void CsvFormat_FromTransport_ThrowsNotSupportedException_Test()
        {
            // Arrange
            var format = new CsvFormat();
            var ctx = new FormatContext("PARTNER_CSV", "101", 1);
            var dummyBytes = Encoding.UTF8.GetBytes("id,name\r\n1,Test");

            // Act & Assert
            var ex = Assert.Throws<NotSupportedException>(() => format.FromTransport(dummyBytes, ctx));
            Assert.Contains("Chiều nhận hiện chỉ hỗ trợ định dạng JSON", ex.Message);
        }

        #endregion

        #region 2. Unit Tests EncryptManager hai chiều

        [Fact]
        public void GetEncryptor_WhenInbound_ReadsInboundCodecAndDecryptionKeyRef_Test()
        {
            // Arrange
            var mockKeyResolver = new TestKeyResolver("test-inbound-key", TestAes256Key);
            var encryptManager = new EncryptManager(mockKeyResolver);
            var config = new Dictionary<string, string>
            {
                ["Codec"] = "None",
                ["EncryptionKeyRef"] = "outbound-key",
                ["InboundCodec"] = "AesGcm",
                ["DecryptionKeyRef"] = "test-inbound-key"
            };

            // Act
            var (encryptor, ctx) = encryptManager.GetEncryptor(config, PayloadDirection.Inbound);

            // Assert
            Assert.IsType<AesGcmEncryptor>(encryptor);
            Assert.Equal("test-inbound-key", ctx.KeyRef);
            Assert.Equal(TestAes256Key, ctx.KeyBytes);
        }

        [Fact]
        public void GetEncryptor_WhenInboundMissingKeyRef_ThrowsInvalidOperationException_Test()
        {
            // Arrange
            var encryptManager = new EncryptManager();
            var config = new Dictionary<string, string>
            {
                ["InboundCodec"] = "AesGcm"
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => encryptManager.GetEncryptor(config, PayloadDirection.Inbound));
            Assert.Contains("DecryptionKeyRef", ex.Message);
        }

        #endregion

        #region 3. Full Roundtrip Tests (Gói 101 và 103, cùng bộ khung TargetShapeJson S)

        [Fact]
        public async Task Roundtrip_Packet101_WithCodeSetAndDateFormat_SavesToDestinationTable_Test()
        {
            // Arrange: Thiết lập bảng đích tạm và câu WriteSql MERGE
            var unique = Guid.NewGuid().ToString("N")[..8];
            var destTable = $"TestDest101_{unique}";
            await _inboundDb.Ado.ExecuteCommandAsync($"""
                CREATE TABLE dbo.{destTable} (
                    id NVARCHAR(64) PRIMARY KEY,
                    code NVARCHAR(64) NULL,
                    recorded_time DATETIME2 NULL,
                    val INT NULL
                )
            """);

            // Cùng bộ khung TargetShapeJson S dùng cho cả gửi và nhận
            var targetShapeJson = """
            {
                "data": [
                    {
                        "id": { "$field": "id" },
                        "code": { "$field": "code", "$extend": { "codeSet": "TEST_STATUS" } },
                        "time": { "$field": "time", "$extend": { "dateFormat": "yyyy-MM-dd HH:mm:ss" } },
                        "val": { "$field": "val" }
                    }
                ]
            }
            """;

            var writeSql = $"""
            MERGE dbo.{destTable} AS target
            USING (
                SELECT id, code, recorded_time, val
                FROM OPENJSON(@records)
                WITH (
                    id NVARCHAR(64) '$.id',
                    code NVARCHAR(64) '$.code',
                    recorded_time DATETIME2 '$.time',
                    val INT '$.val'
                )
            ) AS src
            ON target.id = src.id
            WHEN MATCHED THEN
                UPDATE SET target.code = src.code, target.recorded_time = src.recorded_time, target.val = src.val
            WHEN NOT MATCHED THEN
                INSERT (id, code, recorded_time, val) VALUES (src.id, src.code, src.recorded_time, src.val)
            OUTPUT $action;
            """;

            var (partner, packet, sub) = await SeedPartnerAndPacketAsync(unique, null, packetCode: "101", targetShapeJson: targetShapeJson, writeSql: writeSql);

            // Giả lập dữ liệu JSON chuẩn sinh ra từ bộ khung S của gói 101
            var payloadJson = """
            {
                "data": [
                    {
                        "id": "ITEM_01",
                        "code": "ACTIVE",
                        "time": "2026-10-14 08:30:00",
                        "val": 150
                    },
                    {
                        "id": "ITEM_02",
                        "code": "STANDBY",
                        "time": "2026-10-14 09:45:00",
                        "val": 280
                    }
                ]
            }
            """;

            using var scope = _host.Services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<InboundCommandHandler>();

            // Act 1: WebAPI Handler tiếp nhận
            var input = new ShareDataAddInboundInput
            {
                PartnerCode = partner.Code,
                PacketCode = packet.Code,
                SerialNbr = 1,
                RawContent = payloadJson
            };
            await handler.HandleAsync(input);

            // Act 2: Worker xử lý
            var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
            await inboundService.ProcessPendingPackets(CancellationToken.None);

            // Assert: Bản ghi vào bảng đích đúng cấu trúc và giá trị
            var count = await _inboundDb.Ado.GetIntAsync($"SELECT COUNT(1) FROM dbo.{destTable}");
            Assert.Equal(2, count);

            var item1Val = await _inboundDb.Ado.GetIntAsync($"SELECT val FROM dbo.{destTable} WHERE id = 'ITEM_01'");
            Assert.Equal(150, item1Val);

            var item2Code = await _inboundDb.Ado.GetStringAsync($"SELECT code FROM dbo.{destTable} WHERE id = 'ITEM_02'");
            Assert.Equal("STANDBY", item2Code);

            // Dọn dẹp bảng tạm
            await _inboundDb.Ado.ExecuteCommandAsync($"DROP TABLE dbo.{destTable}");
        }

        [Fact]
        public async Task Roundtrip_WhenDuplicatePacketSent_MergePreventsRowDuplication_Test()
        {
            // Arrange
            var unique = Guid.NewGuid().ToString("N")[..8];
            var destTable = $"TestDestMerge_{unique}";
            await _inboundDb.Ado.ExecuteCommandAsync($"""
                CREATE TABLE dbo.{destTable} (
                    id NVARCHAR(64) PRIMARY KEY,
                    val INT NULL
                )
            """);

            var targetShapeJson = """
            {
                "data": [
                    {
                        "id": { "$field": "id" },
                        "val": { "$field": "val" }
                    }
                ]
            }
            """;

            var writeSql = $"""
            MERGE dbo.{destTable} AS target
            USING (
                SELECT id, val
                FROM OPENJSON(@records)
                WITH (
                    id NVARCHAR(64) '$.id',
                    val INT '$.val'
                )
            ) AS src
            ON target.id = src.id
            WHEN MATCHED THEN
                UPDATE SET target.val = src.val
            WHEN NOT MATCHED THEN
                INSERT (id, val) VALUES (src.id, src.val)
            OUTPUT $action;
            """;

            var (partner, packet, _) = await SeedPartnerAndPacketAsync(unique, null, packetCode: "103", targetShapeJson: targetShapeJson, writeSql: writeSql);
            var payloadJson = """{"data":[{"id":"DUP_01","val":500}]}""";

            using var scope = _host.Services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<InboundCommandHandler>();
            var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();

            // Act: Gửi lần 1 và xử lý
            await handler.HandleAsync(new ShareDataAddInboundInput
            {
                PartnerCode = partner.Code,
                PacketCode = packet.Code,
                SerialNbr = 1,
                RawContent = payloadJson
            });
            await inboundService.ProcessPendingPackets(CancellationToken.None);

            // Gửi lần 2 (cùng payload do chu kỳ trích xuất lại hoặc gửi lại)
            await handler.HandleAsync(new ShareDataAddInboundInput
            {
                PartnerCode = partner.Code,
                PacketCode = packet.Code,
                SerialNbr = 2,
                RawContent = payloadJson
            });
            await inboundService.ProcessPendingPackets(CancellationToken.None);

            // Assert: Có 2 bản ghi packet trong ShareDataInboundPacket nhưng bảng đích vẫn chỉ 1 dòng nhờ MERGE
            var packetCount = await _inboundDb.Queryable<ShareDataInboundPacket>()
                .Where(p => p.PartnerCode == partner.Code && p.PacketCode == packet.Code)
                .CountAsync();
            Assert.Equal(2, packetCount);

            var destCount = await _inboundDb.Ado.GetIntAsync($"SELECT COUNT(1) FROM dbo.{destTable}");
            Assert.Equal(1, destCount);

            await _inboundDb.Ado.ExecuteCommandAsync($"DROP TABLE dbo.{destTable}");
        }

        #endregion

        #region Helpers

        private async Task<(ShareDataPartner Partner, ShareDataPacket Packet, ShareDataSubscription Subscription)>
            SeedPartnerAndPacketAsync(
                string unique,
                string? connectionConfig,
                string packetCode = "101",
                string? targetShapeJson = null,
                string? writeSql = null)
        {
            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"PARTNER_RT_{unique}",
                Name = $"Partner RT {unique}",
                Status = BaseEnums.StatusEnum.Enable,
                ConnectionType = "API",
                ConnectionConfig = connectionConfig
            };
            await _db.Insertable(partner).ExecuteCommandAsync();

            var packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"PKT_RT_{packetCode}_{unique}",
                Name = $"Packet RT {packetCode} {unique}",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(packet).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = packet.ID,
                Direction = BaseEnums.Direction.Inbound,
                State = BaseEnums.SubSubscriptionState.Active,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(sub).ExecuteCommandAsync();

            var mapping = new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = packet.ID,
                Direction = BaseEnums.Direction.Inbound,
                IsActive = true,
                TargetShapeJson = targetShapeJson ?? """{"data":[{"id":{"$field":"id"},"val":{"$field":"val"}}]}""",
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(mapping).ExecuteCommandAsync();

            var packetSql = new ShareDataPacketSql
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packet.Code,
                OrderNo = 1,
                Status = BaseEnums.StatusEnum.Enable,
                WriteSql = writeSql ?? "SELECT 'INSERT' FROM OPENJSON(@records)"
            };
            await _db.Insertable(packetSql).ExecuteCommandAsync();

            return (partner, packet, sub);
        }

        private class TestKeyResolver(string expectedKeyRef, byte[] keyBytes) : IKeyResolver
        {
            public byte[]? ResolveKey(string keyRef)
            {
                if (string.Equals(keyRef, expectedKeyRef, StringComparison.OrdinalIgnoreCase))
                {
                    return keyBytes;
                }
                return null;
            }
        }

        #endregion
    }
}
