using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shared.Core.Utilities.Constants;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;
using ShareDataWorker.Core.Models.DataOutbound;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Dto.ActivityLog;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Modules.CCTV.Core.Entities;
using Modules.CfgSystem.Core.Entities;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.VMS.Core.Entities;
using ShareDataWorker.Core.Dto;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Mapping;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Senders;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using Tests.ShareData.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Module.ShareData.Core.Constants;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Description: Lớp kiểm thử Integration Test cho DataOutboundService thuộc module ShareDataWorker trên CSDL Test Local.
    /// Created date: 31/07/2026
    /// </summary>
    [Collection("api")]
    public class DataOutboundServiceTests
    {
        private readonly Host _host;

        public DataOutboundServiceTests(Host host)
        {
            /// xUnit tạo instance mới trước mỗi [Fact] → constructor này chạy mỗi test
            host.PartnerServer.ResetDefaults();
            _host = host;

            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            ClearOutboundSimulationData(db);

            db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .ExecuteCommand();
        }

        /// <summary>
        /// Description: Dọn dẹp dữ liệu của các bảng mô phỏng luồng xuất bản (TmsTrafficData, TmsIncident, TmsZoneStatus).
        /// Created date: 29/10/2026
        /// </summary>
        private static void ClearOutboundSimulationData(ISqlSugarClient db)
        {
            db.Deleteable<TmsTrafficData>().ExecuteCommand();
            db.Deleteable<TmsIncident>().ExecuteCommand();
            // Gói 101 đọc TmsZoneStatus: đối tác mới chưa có mốc thì quét từ đầu bảng, dòng của test trước sẽ lẫn vào trang đầu
            db.Deleteable<TmsZoneStatus>().ExecuteCommand();
        }


        #region 1. Luồng Kiểm Thử Nghiệp Vụ Chính (Business Workflows - ProcessSubscriptions & Pipeline)

        /// <summary>
        /// Description: Kiểm tra tiến trình xuất bản hàng loạt ghi file PDU hợp lệ trực tiếp xuống ổ đĩa.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_DirectFileWrite_SavesValidPduOnDisk_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_DIRECT_{unique}", SubCode = $"SUB_DIRECT_{unique}", DatatypeId = "101" });

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.False(string.IsNullOrWhiteSpace(logs[0].FilePath));

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath), $"File phải được ghi trực tiếp xuống đĩa tại: {fullPath}");

            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.True(doc.RootElement.GetArrayLength() > 0);

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(BaseEnums.SubSubscriptionState.Active, updatedSub.State);
            Assert.Equal((sub.SerialNbr ?? 0) + 1, updatedSub.SerialNbr);
            Assert.NotNull(updatedSub.NextTimeRun);
        }

        /// <summary>
        /// Description: Kiểm thử luồng worker Outbound hoạt động mapping ánh xạ đầy đủ 10 kiểu dữ liệu
        ///              (string, number, int, dateTime, bool, long, float, double, decimal, guid):
        ///              trích xuất từ CSDL, chuyển đổi kiểu dữ liệu qua DataMappingProcess,
        ///              ghi file kết xuất trực tiếp xuống ổ đĩa và kiểm tra JSON payload trong file sinh ra đúng từng kiểu.
        /// Created date: 28/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WithAllSupportedDataTypes_MapsCorrectlyAndWritesFileOnDisk_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var zoneId = $"ZONE_TYPES_{unique}";
            var testTime = new DateTime(2026, 10, 28, 10, 0, 0);

            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Tuyen Test Types {unique}",
                FromKmNumber = 42,
                FromMetNumber = 500,
                ToKmNumber = 100,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80,
                CreateTime = testTime
            }).ExecuteCommandAsync();

            var statusId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsZoneStatus
            {
                ID = statusId,
                ZoneId = zoneId,
                AverageSpeed = "75.5",
                Condition = "NORMAL",
                UpdateTime = testTime
            }).ExecuteCommandAsync();

            // Cấu hình TargetShapeJson định nghĩa ánh xạ cho tất cả 10 kiểu dữ liệu được hỗ trợ
            var targetShape = new Dictionary<string, object?>
            {
                ["guidField"] = new Dictionary<string, object?>
                {
                    ["$field"] = "zoneStatusId",
                    ["$extend"] = new Dictionary<string, object?> { ["targetType"] = "guid" }
                },
                ["stringField"] = new Dictionary<string, object?>
                {
                    ["$field"] = "trafficCondition",
                    ["$extend"] = new Dictionary<string, object?> { ["targetType"] = "string" }
                },
                ["intField"] = new Dictionary<string, object?>
                {
                    ["$field"] = "fromLocationKm",
                    ["$extend"] = new Dictionary<string, object?> { ["targetType"] = "int" }
                },
                ["longField"] = new Dictionary<string, object?>
                {
                    ["$field"] = "fromLocationMet",
                    ["$extend"] = new Dictionary<string, object?> { ["targetType"] = "long" }
                },
                ["floatField"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?> { ["targetType"] = "float" }
                },
                ["doubleField"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?> { ["targetType"] = "double" }
                },
                ["decimalField"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?> { ["targetType"] = "decimal" }
                },
                ["numberField"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?> { ["targetType"] = "number" }
                },
                ["boolField"] = new Dictionary<string, object?>
                {
                    ["$field"] = "fromLocationKm",
                    ["$extend"] = new Dictionary<string, object?> { ["targetType"] = "bool" }
                },
                ["dateTimeField"] = new Dictionary<string, object?>
                {
                    ["$field"] = "dataTime",
                    ["$extend"] = new Dictionary<string, object?> { ["targetType"] = "dateTime" }
                }
            };

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_TYPES_{unique}",
                SubCode = $"SUB_TYPES_{unique}",
                DatatypeId = "101",
                MappingShape = targetShape
            });

            // Act: Chạy luồng worker xử lý xuất bản
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert: Kiểm tra log xuất bản thành công
            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.False(string.IsNullOrWhiteSpace(logs[0].FilePath));

            // Kiểm tra file kết xuất thực tế đã ghi xuống ổ đĩa
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath), $"File kết xuất phải tồn tại trên đĩa tại: {fullPath}");

            // Đọc và parse JSON payload trong file kết xuất
            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.True(doc.RootElement.GetArrayLength() > 0);

            var record = doc.RootElement.EnumerateArray().First(r => r.GetProperty("stringField").GetString() == "NORMAL");

            // 1. guid
            Assert.Equal(JsonValueKind.String, record.GetProperty("guidField").ValueKind);
            Assert.True(Guid.TryParse(record.GetProperty("guidField").GetString(), out var parsedGuid));
            Assert.NotEqual(Guid.Empty, parsedGuid);

            // 2. string
            Assert.Equal(JsonValueKind.String, record.GetProperty("stringField").ValueKind);
            Assert.Equal("NORMAL", record.GetProperty("stringField").GetString());

            // 3. int
            Assert.Equal(JsonValueKind.Number, record.GetProperty("intField").ValueKind);
            Assert.Equal(42, record.GetProperty("intField").GetInt32());

            // 4. long
            Assert.Equal(JsonValueKind.Number, record.GetProperty("longField").ValueKind);
            Assert.Equal(500L, record.GetProperty("longField").GetInt64());

            // 5. float
            Assert.Equal(JsonValueKind.Number, record.GetProperty("floatField").ValueKind);
            Assert.Equal(75.5f, record.GetProperty("floatField").GetSingle());

            // 6. double
            Assert.Equal(JsonValueKind.Number, record.GetProperty("doubleField").ValueKind);
            Assert.Equal(75.5d, record.GetProperty("doubleField").GetDouble());

            // 7. decimal
            Assert.Equal(JsonValueKind.Number, record.GetProperty("decimalField").ValueKind);
            Assert.Equal(75.5m, record.GetProperty("decimalField").GetDecimal());

            // 8. number
            Assert.Equal(JsonValueKind.Number, record.GetProperty("numberField").ValueKind);
            Assert.Equal(75.5m, record.GetProperty("numberField").GetDecimal());

            // 9. bool
            Assert.True(record.GetProperty("boolField").ValueKind is JsonValueKind.True or JsonValueKind.False);
            Assert.True(record.GetProperty("boolField").GetBoolean());

            // 10. dateTime
            Assert.Equal(JsonValueKind.String, record.GetProperty("dateTimeField").ValueKind);
            Assert.True(DateTime.TryParse(record.GetProperty("dateTimeField").GetString(), out var parsedDt));
            Assert.Equal(testTime.Year, parsedDt.Year);
            Assert.Equal(testTime.Month, parsedDt.Month);
            Assert.Equal(testTime.Day, parsedDt.Day);
        }


        /// <summary>
        /// Description: Kiểm tra khi mapping có biểu thức (hợp lệ hoặc lỗi chia 0) thì bỏ qua không tính toán theo §3.4/D8, giữ nguyên giá trị nguồn vào payload và không sinh cảnh báo ESH-1203.
        /// Created date: 17/09/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Theory]
        [InlineData("averageSpeed * 2")]
        [InlineData("averageSpeed / 0")]
        public async Task ProcessSubscriptions_WhenMappingHasIgnoredExpression_KeepsRawValue_Test(string expression)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var zoneId = $"ZONE_{unique}";
            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Tuyen Expr {unique}",
                FromKmNumber = 10,
                FromMetNumber = 0,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = zoneId,
                AverageSpeed = "60",
                Condition = "BINH_THUONG",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var targetShape = new Dictionary<string, object?>
            {
                ["zoneId"] = new Dictionary<string, object?> { ["$field"] = "zoneId" },
                ["calcSpeed"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?>
                    {
                        ["expression"] = expression
                    }
                }
            };

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_EXPR_OK_{unique}", SubCode = $"SUB_EXPR_OK_{unique}", DatatypeId = "101", MappingShape = targetShape });

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));

            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            var record = doc.RootElement.EnumerateArray().First(r => r.GetProperty("zoneId").GetString() == zoneId);
            Assert.Equal(60m, record.GetProperty("calcSpeed").GetDecimal());

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1203")
                .ToListAsync();
            Assert.Empty(alerts);
        }


        /// <summary>
        /// Description: Kiểm thử luồng xuất bản thực tế ghi file xuống đĩa với phễu có header và data:[{...}], payload sinh ra chỉ có 1 header và 3 bản ghi data.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenShapeHasHeaderAndRecordArray_WritesSingleHeaderInPayload_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var now = DateTime.Now;

            var zoneIds = new List<string>();
            var statusIds = new List<string>();

            for (var i = 1; i <= 3; i++)
            {
                var zoneId = $"ZONE_HDR_{unique}_{i}";
                zoneIds.Add(zoneId);
                await db.Insertable(new TmsZone
                {
                    ID = zoneId,
                    Name = $"Tuyen Test {unique}_{i}",
                    FromKmNumber = 10 * i,
                    FromMetNumber = 0,
                    ToKmNumber = 20 * i,
                    ToMetNumber = 0,
                    LaneId = "LANE_1",
                    MaxSpeed = 80
                }).ExecuteCommandAsync();

                var statusId = Guid.NewGuid().ToString("N");
                statusIds.Add(statusId);
                await db.Insertable(new TmsZoneStatus
                {
                    ID = statusId,
                    ZoneId = zoneId,
                    AverageSpeed = (60 + i).ToString(),
                    Condition = "NORMAL",
                    UpdateTime = now.AddSeconds(i)
                }).ExecuteCommandAsync();
            }

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_HDR_{unique}", SubCode = $"SUB_HDR_{unique}", DatatypeId = "101" });

            var targetShape = new Dictionary<string, object?>
            {
                ["header"] = new Dictionary<string, object?>
                {
                    ["source"] = "ITS",
                    ["version"] = "1.0"
                },
                ["data"] = new List<object?>
                {
                    new Dictionary<string, object?>
                    {
                        ["zId"] = new Dictionary<string, object?> { ["$field"] = "zoneId" },
                        ["spd"] = new Dictionary<string, object?> { ["$field"] = "averageSpeed" }
                    }
                }
            };

            var mappingId = Guid.NewGuid().ToString("N");
            await db.Insertable(new ShareDataMapping
            {
                ID = mappingId,
                PartnerId = partner.ID,
                DatatypeId = "101",
                Direction = BaseEnums.Direction.Outbound,
                TargetShapeJson = JsonSerializer.Serialize(targetShape),
                IsActive = true
            }).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));

            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            var envelope = doc.RootElement;
            Assert.True(envelope.TryGetProperty("header", out var headerElem));
            Assert.Equal("ITS", headerElem.GetProperty("source").GetString());

            Assert.True(envelope.TryGetProperty("data", out var dataElem));
            Assert.True(dataElem.GetArrayLength() >= 3);

            for (var i = 0; i < dataElem.GetArrayLength(); i++)
            {
                Assert.False(dataElem[i].TryGetProperty("header", out _));
            }

            // Dọn dữ liệu test cụ thể tự chèn
            await db.Deleteable<TmsZoneStatus>().In(statusIds).ExecuteCommandAsync();
            await db.Deleteable<TmsZone>().In(zoneIds).ExecuteCommandAsync();
            await db.Deleteable<ShareDataMapping>().Where(m => m.ID == mappingId).ExecuteCommandAsync();
        }


        /// <summary>
        /// Description: Kiểm tra khi mapping có biểu thức cú pháp không hợp lệ thì ghi log ESH-1203 và giữ lại giá trị thô.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenMappingHasInvalidExpression_LogsEsh1203_KeepsRawValue_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var zoneId = $"ZONE_{unique}";
            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Tuyen Invalid Expr {unique}",
                FromKmNumber = 10,
                FromMetNumber = 0,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = zoneId,
                AverageSpeed = "75",
                Condition = "BINH_THUONG",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var targetShape = new Dictionary<string, object?>
            {
                ["zoneId"] = new Dictionary<string, object?> { ["$field"] = "zoneId" },
                ["speed"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?>
                    {
                        ["expression"] = "1; DROP TABLE users"
                    }
                }
            };

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_EXPR_INV_{unique}", SubCode = $"SUB_EXPR_INV_{unique}", DatatypeId = "101", MappingShape = targetShape });

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));

            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            var record = doc.RootElement.EnumerateArray().First(r => r.GetProperty("zoneId").GetString() == zoneId);
            // Raw value preserved
            Assert.Equal(75m, record.GetProperty("speed").GetDecimal());

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1203")
                .ToListAsync();
            Assert.NotEmpty(alerts);
            Assert.Equal(BaseEnums.AlertSeverity.Warning, alerts[0].Severity);
        }



        /// <summary>
        /// Description: Kiểm tra khi giao thức đối tác là ASN thì tự động fallback về định dạng JSON.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPartnerProtocolIsAsn_FallsBackToJson_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_ASN_{unique}", SubCode = $"SUB_ASN_{unique}", DatatypeId = "101" });

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.NotNull(logs[0].FilePath);
            // BaseEnums.ProtocolProfile.Asn chưa support → service xuất JSON theo mặc định
            Assert.EndsWith(".json", logs[0].FilePath, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Description: Kiểm tra cơ chế bù dữ liệu tăng dần sau thời gian nhàn rỗi đảm bảo mỗi bản ghi mới xuất đúng một lần.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_IncrementalCatchUp_AfterIdle_ExportsEveryNewRowExactlyOnce_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_CATCHUP_{unique}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_CU_{unique}",
                KmNumber = 45,
                MetNumber = 100
            }).ExecuteCommandAsync();

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddHours(-2);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_CU_{unique}",
                SubCode     = $"SUB_CU_{unique}",
                DatatypeId  = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            // Simulate worker stopped while 50 VDS records were inserted consecutively
            var insertedRecords = new List<TmsTrafficData>();
            for (var i = 1; i <= 50; i++)
            {
                var recordTime = baseTime.AddSeconds(i * 10);
                insertedRecords.Add(new TmsTrafficData
                {
                    ID = Guid.NewGuid().ToString("N"),
                    DetectTime = recordTime,
                    CreateTime = recordTime,
                    UpdateTime = recordTime,
                    Type = "CAR",
                    LicensePlate = $"CU{unique}-{i:D5}",
                    Speed = 60.0f + (i % 30),
                    Lane = $"L{((i % 3) + 1)}",
                    Direction = "1",
                    Location = "KM45",
                    EquipmentId = eqId
                });
            }
            await db.Insertable(insertedRecords).ExecuteCommandAsync();
            await db.Updateable(insertedRecords).UpdateColumns(t => new { t.CreateTime, t.UpdateTime }).ExecuteCommandAsync();

            // Run catchup batches
            var allExportedPlates = new List<string>();
            var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var maxRuns = 10;
            var runs = 0;

            while (runs < maxRuns)
            {
                runs++;
                // Reset NextTimeRun to past so subscription is immediately eligible for next batch run
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.NextTimeRun == db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-10))
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();

                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync();

                var newLogs = logs.Where(l => !string.IsNullOrEmpty(l.FilePath) && processedFiles.Add(l.FilePath!)).ToList();
                if (newLogs.Count == 0 || newLogs.All(l => l.RecordCount == 0))
                    break;

                foreach (var log in newLogs)
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", log.FilePath!);
                    if (!File.Exists(fullPath))
                        continue;

                    var json = await File.ReadAllTextAsync(fullPath);
                    using var doc = JsonDocument.Parse(json);
                    foreach (var rec in doc.RootElement.EnumerateArray())
                    {
                        if (rec.TryGetProperty("licensePlate", out var lp))
                        {
                            var plateStr = lp.GetString();
                            if (!string.IsNullOrEmpty(plateStr) && plateStr.StartsWith($"CU{unique}-"))
                            {
                                allExportedPlates.Add(plateStr);
                            }
                        }
                    }
                }

                if (allExportedPlates.Count >= 50)
                    break;
            }

            // Assert: Every record was exported exactly once, no duplicates, no omissions
            Assert.Equal(50, allExportedPlates.Count);
            Assert.Equal(50, allExportedPlates.Distinct().Count());
            for (var i = 1; i <= 50; i++)
            {
                Assert.Contains($"CU{unique}-{i:D5}", allExportedPlates);
            }

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updatedSub.LastTimeRun);
            Assert.True(updatedSub.LastTimeRun >= insertedRecords.Last().DetectTime!.Value.AddSeconds(-1));
        }


        /// <summary>
        /// Description: Kiểm thử 2 vòng chạy liên tiếp cho gói Incremental 104 (Thời tiết):
        /// Vòng 1 export bản ghi đầu tiên và cập nhật LastTimeRun theo watermark.
        /// Vòng 2 chỉ export bản ghi mới phát sinh, không bị rớt dữ liệu về rỗng hay lặp lại.
        /// Created date: 15/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_IncrementalPacket104_TwoConsecutiveRuns_TracksWatermarkAccurately_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "104");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var maxExisting = await db.Queryable<TmsWeather>().MaxAsync(w => (DateTime?)w.UpdateTime) ?? dbNow;
            var baseTime = (maxExisting > dbNow ? maxExisting : dbNow).AddSeconds(1);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_104_{unique}",
                SubCode     = $"SUB_104_{unique}",
                DatatypeId  = "104",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });

            // Bản ghi 1: tại baseTime + 5 phút
            var time1 = baseTime.AddMinutes(5);
            var weatherId1 = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsWeather
            {
                ID = weatherId1,
                RefId = $"WS_RUN1_{unique}",
                LocationDetail = "Vị trí km 10",
                Temperature = 25.0f,
                Hudmidity = 70.0f,
                WindSpeed = 10.0f,
                TimeDetect = time1,
                CreateTime = time1,
                UpdateTime = time1
            }).ExecuteCommandAsync();
            await db.Updateable<TmsWeather>()
                .SetColumns(w => w.UpdateTime == time1)
                .SetColumns(w => w.CreateTime == time1)
                .Where(w => w.ID == weatherId1)
                .ExecuteCommandAsync();

            // Vòng chạy 1: phải xuất đúng bản ghi 1
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            var logs1 = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs1);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs1[0].Success);
            Assert.Equal(1, logs1[0].RecordCount);

            // Kiểm tra subscription được cập nhật LastTimeRun theo watermark
            var subAfterRun1 = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.ID == sub.ID)
                .FirstAsync();
            Assert.NotNull(subAfterRun1.LastTimeRun);
            Assert.True(subAfterRun1.LastTimeRun >= time1, $"subAfterRun1.LastTimeRun={subAfterRun1.LastTimeRun:O} vs time1={time1:O}");

            // Bản ghi 2: tại baseTime + 15 phút
            var time2 = baseTime.AddMinutes(15);
            var weatherId2 = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsWeather
            {
                ID = weatherId2,
                RefId = $"WS_RUN2_{unique}",
                LocationDetail = "Vị trí km 20",
                Temperature = 27.0f,
                Hudmidity = 65.0f,
                WindSpeed = 12.0f,
                TimeDetect = time2,
                CreateTime = time2,
                UpdateTime = time2
            }).ExecuteCommandAsync();
            await db.Updateable<TmsWeather>()
                .SetColumns(w => w.UpdateTime == time2)
                .SetColumns(w => w.CreateTime == time2)
                .Where(w => w.ID == weatherId2)
                .ExecuteCommandAsync();

            // Reset NextTimeRun về quá khứ để worker nhận xử lý tiếp ở vòng 2
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            // Vòng chạy 2: phải xuất tiếp bản ghi 2 (không bị rớt dữ liệu)
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            var logs2 = await GetLogs(db, sub.ID);
            Assert.True(logs2.Count >= 2);
            var latestLog = logs2.OrderByDescending(l => l.OccurredAt).First();
            Assert.Equal(BaseEnums.SuccessEnums.Success, latestLog.Success);
            Assert.True(latestLog.RecordCount >= 1);

            var subAfterRun2 = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.ID == sub.ID)
                .FirstAsync();
            Assert.True(subAfterRun2.LastTimeRun >= time2);
        }


        /// <summary>
        /// Description: Kiểm thử cấu hình TargetShapeJson nạp đúng CodeSet và Transform áp dụng bảng quy đổi ShareDataCodeSet cho gói tin 101.
        /// Created date: 15/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_LoadsCodeSetFromPacketFields_TranslatesValueCorrectly_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];

            // 1. Seed ShareDataCodeSet với Code = "TRAFFIC_COND" (đã có trong cấu hình trường của gói 101)
            var codeSetId = Guid.NewGuid().ToString("N");
            await db.Deleteable<ShareDataCodeSet>()
                .Where(c => c.Code == "TRAFFIC_COND")
                .ExecuteCommandAsync();

            var codeValuesJson = """
                [
                    {
                        "sourceValue": "CONGESTED",
                        "partnerValue": "UNCS_02",
                        "displayName": "Ùn tắc"
                    },
                    {
                        "sourceValue": "NORMAL",
                        "partnerValue": "UNCS_01",
                        "displayName": "Bình thường"
                    }
                ]
                """;

            await db.Insertable(new ShareDataCodeSet
            {
                ID = codeSetId,
                Code = "TRAFFIC_COND",
                Name = "Tình trạng giao thông",
                Status = BaseEnums.StatusEnum.Enable,
                ValuesJson = codeValuesJson
            }).ExecuteCommandAsync();

            // 2. Seed dữ liệu nghiệp vụ cho gói 101 với Condition = "NORMAL"
            var zoneId = $"ZONE_CS_{unique}";
            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Tuyến Test CodeSet {unique}",
                FromKmNumber = 10,
                FromMetNumber = 0,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = zoneId,
                AverageSpeed = "60",
                Condition = "NORMAL",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficStatistic
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = zoneId,
                TotalVehicleNumber = 100
            }).ExecuteCommandAsync();

            // 3. Seed Subscription cho gói 101
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_CS_{unique}", SubCode = $"SUB_CS_{unique}", DatatypeId = "101" });

            var targetShapeStr = """
            {
                "averageSpeed": { "$field": "averageSpeed" },
                "trafficCondition": { "$field": "trafficCondition", "$extend": { "codeSet": "TRAFFIC_COND" } },
                "zoneId": { "$field": "zoneId" }
            }
            """;
            await db.Updateable<ShareDataMapping>().SetColumns(m => m.TargetShapeJson == targetShapeStr).Where(m => m.PartnerId == partner.ID && m.DatatypeId == "101").ExecuteCommandAsync();

            // 4. Chạy export
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.NotNull(logs[0].FilePath);

            // 5. Đọc file xuất và verify giá trị trafficCondition được quy đổi thành "UNCS_01" (PartnerValue)
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));

            var content = await File.ReadAllTextAsync(fullPath);
            Assert.Contains("UNCS_01", content);
        }


        /// Description: Kiểm tra khi đối tác cấu hình EndPointApiUrl thì gửi payload qua HTTP thành công.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPartnerHasEndPointApiUrl_SendsHttpPayloadSuccessfully_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_HTTP_{unique}",
                SubCode     = $"SUB_HTTP_{unique}",
                DatatypeId  = "101",
                ConfigurePartner = p =>
                {
                    p.Address = ShareDataPartnerServerMock.DefaultHost;
                    p.Port = ShareDataPartnerServerMock.DefaultPort;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                }
            });

            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            Assert.NotNull(partnerServer.LastRequest);
            Assert.Equal("POST", partnerServer.LastRequest!.Method);
            Assert.Equal("/api/sharedata/sharedatainbound", partnerServer.LastRequest!.Path);

            Assert.NotNull(partnerServer.LastRequest!.Body);
            using var doc = JsonDocument.Parse(partnerServer.LastRequest!.Body);
            var root = doc.RootElement;
            Assert.Equal(JsonValueKind.Array, root.ValueKind);
            Assert.True(root.GetArrayLength() > 0);
            Assert.Equal(JsonValueKind.Object, root[0].ValueKind);

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                .ToListAsync();
            Assert.Empty(alerts);
        }


        /// <summary>
        /// Description: Kiểm tra khi gọi HTTP đến đối tác thất bại (trả về mã 500 hoặc cổng đóng ném ngoại lệ) thì vẫn xuất file và ghi cảnh báo lỗi ESH-1402.
        /// Created date: 17/09/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Theory]
        [InlineData("Http500")]
        [InlineData("PortClosed")]
        public async Task ProcessSubscriptions_WhenPartnerHttpFails_StillExportsFileAndLogsWarningAlert_Test(string failureMode)
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            if (failureMode == "Http500")
            {
                partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;
                partnerServer.DefaultResponseBody = "Internal Server Error";
            }

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_HTTP_F_{unique}",
                SubCode     = $"SUB_HTTP_F_{unique}",
                DatatypeId  = "101",
                ConfigurePartner = p =>
                {
                    p.Address = ShareDataPartnerServerMock.DefaultHost;
                    p.Port = failureMode == "PortClosed" ? ShareDataPartnerServerMock.ClosedPort : ShareDataPartnerServerMock.DefaultPort;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                }
            });

            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, logs[0].Success);
            Assert.False(string.IsNullOrWhiteSpace(logs[0].FilePath));

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath), "File phải được xuất thành công dù HTTP thất bại");

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                .ToListAsync();
            Assert.Single(alerts);
            Assert.Equal(BaseEnums.AlertSeverity.Warning, alerts[0].Severity);
            Assert.Equal(BaseEnums.AlertSource.Protocol, alerts[0].AlertSource);
            Assert.False(string.IsNullOrWhiteSpace(alerts[0].Message));
        }


        /// <summary>
        /// Description: Kiểm tra khi đối tác không cấu hình EndPointApiUrl thì worker vẫn gửi HTTP và ghi nhận thất bại ESH-1402 khi nhận lỗi 404.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPartnerHasNoEndPointApiUrl_SendsHttpAndFails_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.NotFound;

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_NO_HTTP_{unique}",
                SubCode     = $"SUB_NO_HTTP_{unique}",
                DatatypeId  = "101",
                ConfigurePartner = p =>
                {
                    p.Address = ShareDataPartnerServerMock.DefaultHost;
                    p.Port = ShareDataPartnerServerMock.DefaultPort;
                    p.EndPointApiUrl = null;
                }
            });

            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions(CancellationToken.None);

            Assert.Equal(1, partnerServer.TotalReceivedRequests);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, logs[0].Success);

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                .ToListAsync();
            Assert.NotEmpty(alerts);
            Assert.Equal(BaseEnums.AlertSeverity.Warning, alerts[0].Severity);
        }


        /// <summary>
        /// Description: Kiểm tra khi mã gói tin có hậu tố tương tự DB staging thì vẫn phân giải đúng handler và xuất bản thành công.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPacketCodeHasSuffixLikeStagingDb_ResolvesHandlerAndExportsSuccessfully_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetCodeWithSuffix = "101_commonData";

            var packet = await db.Queryable<ShareDataPacket>()
                .Where(p => p.Code == packetCodeWithSuffix && p.IsDelete == null)
                .FirstAsync();
            if (packet == null)
            {
                packet = new ShareDataPacket
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = packetCodeWithSuffix,
                    Name = "Dữ liệu giao thông chung",
                    PacketVersion = "1.0",
                    OrderNo = 1
                };
                await db.Insertable(packet).ExecuteCommandAsync();
            }
            if (string.IsNullOrWhiteSpace((await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql))
            {
                await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, PacketTemplateSeed.TryGet(packetCodeWithSuffix, out var tpl) ? tpl.ExtractSql : null);
            }

            await EnsureTrafficFlowFieldsAsync(db, packet.ID);

            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_SUFFIX_{unique}",
                SubCode     = $"SUB_SUFFIX_{unique}",
                DatatypeId  = packetCodeWithSuffix
            });

            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.True(logs[0].RecordCount > 0);
            Assert.False(string.IsNullOrWhiteSpace(logs[0].FilePath));

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath), $"File kết xuất cho mã [{packetCodeWithSuffix}] phải được ghi thành công xuống đĩa");

            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            Assert.Equal(JsonValueKind.Array, root.ValueKind);
            Assert.True(root.GetArrayLength() > 0);
        }


        /// <summary>
        /// Description: Kiểm tra khi lưu đệm/ghi tệp thất bại thì toàn bộ quá trình xuất bản dừng ngay, ghi cảnh báo ESH-1401 ArchiveFailed, không gọi gửi HTTP và không tịnh tiến mốc watermark.
        /// Created date: 18/09/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenFileWriteFails_AbortsAndLogsArchiveFailed_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            var (failingConfig, blockerPath) = CreateFailingConfig();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_FILE_FAIL_{unique}",
                SubCode     = $"SUB_FILE_FAIL_{unique}",
                DatatypeId  = "101",
                ConfigurePartner = p =>
                {
                    p.Address = ShareDataPartnerServerMock.DefaultHost;
                    p.Port = ShareDataPartnerServerMock.DefaultPort;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                }
            });

            try
            {
                var worker = CreateWorker(scope, configuration: failingConfig);
                await worker.ProcessSubscriptions(CancellationToken.None);

                // Lưu đệm thất bại: KHÔNG gửi API và KHÔNG tiến mốc
                Assert.Equal(0, partnerServer.TotalReceivedRequests);

                var logs = await GetLogs(db, sub.ID);
                Assert.NotEmpty(logs);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, logs[0].Success);

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID)
                    .ToListAsync();
                Assert.Contains(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.ArchiveFailed);

                var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
                Assert.Null(updatedSub.LastTimeRun);
                Assert.Equal(sub.SerialNbr, updatedSub.SerialNbr);
            }
            finally
            {
                File.Delete(blockerPath);
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }


        /// <summary>
        /// Description: Kiểm thử gói tin 101_commonData - nạp dữ liệu đầu vào (TmsZone, TmsZoneStatus, TmsTrafficStatistic),
        /// gọi xuất bản và xác nhận câu truy vấn QueryPacket101 trả về đầy đủ các trường, đặc biệt là zoneStatusId.
        /// Created date: 15/09/2026
        /// </summary>
        [Fact]
        public async Task QueryPacket101_WithSeededData_ReturnsZoneStatusIdAndExportsSuccessfully_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = CreateWorker(scope);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var zoneId = $"ZONE_{uniqueId}";
            var zoneStatusId = Guid.NewGuid().ToString("N");
            var now = DateTime.Now;

            // 1. Arrange: Nạp dữ liệu giả lập vào bảng nguồn cho gói 101
            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Tuyến Test {uniqueId}",
                FromKmNumber = 10,
                FromMetNumber = 500,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsZoneStatus
            {
                ID = zoneStatusId,
                ZoneId = zoneId,
                AverageSpeed = "65.50",
                Condition = "NORMAL",
                UpdateTime = now
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficStatistic
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = zoneId,
                TotalVehicleNumber = 150
            }).ExecuteCommandAsync();

            // Đảm bảo Packet 101_commonData tồn tại
            var packet = await db.Queryable<ShareDataPacket>()
                .Where(p => p.Code == "101_commonData" && p.IsDelete == null)
                .FirstAsync();
            if (packet == null)
            {
                packet = new ShareDataPacket
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = "101_commonData",
                    Name = "Dữ liệu giao thông chung",
                    PacketVersion = "1.0",
                    OrderNo = 1
                };
                await db.Insertable(packet).ExecuteCommandAsync();
            }
            if (string.IsNullOrWhiteSpace((await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql))
            {
                await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, PacketTemplateSeed.TryGet("101_commonData", out var tpl) ? tpl.ExtractSql : null);
            }



            await EnsureTrafficFlowFieldsAsync(db, packet.ID);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P101_{uniqueId}", SubCode = $"SUB101_{uniqueId}", DatatypeId = "101_commonData" });

            // 2. Act: Thực thi xuất dữ liệu qua ProcessSubscriptions
            await service.ProcessSubscriptions(CancellationToken.None);

            // 3. Assert: Kiểm tra log xuất bản thành công (không bị dính lỗi thiếu trường bắt buộc)
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.True(logs[0].Success == BaseEnums.SuccessEnums.Success, $"Xuất bản thất bại: {logs[0].ErrorMessage}");
            Assert.True(logs[0].RecordCount > 0);

            // Đọc tệp kết xuất và kiểm tra bản ghi chứa zoneStatusId đúng với ID đã insert
            Assert.False(string.IsNullOrEmpty(logs[0].FilePath));
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath), $"Tệp kết xuất không tồn tại: {fullPath}");

            var jsonContent = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var payload = doc.RootElement;
            Assert.Equal(JsonValueKind.Array, payload.ValueKind);
            Assert.True(payload.GetArrayLength() > 0);

            var matchedRecord = payload.EnumerateArray()
                .FirstOrDefault(r => r.TryGetProperty("zoneId", out var zid) && zid.GetString() == zoneId);

            Assert.True(matchedRecord.ValueKind != JsonValueKind.Undefined, $"Không tìm thấy bản ghi có zoneId={zoneId} trong payload!");
            Assert.True(matchedRecord.TryGetProperty("zoneStatusId", out var actualZid), "Không tìm thấy trường zoneStatusId trong payload!");
            Assert.Equal(zoneStatusId, actualZid.GetString());
            Assert.Equal(zoneId, matchedRecord.GetProperty("zoneId").GetString());
            Assert.Equal("NORMAL", matchedRecord.GetProperty("trafficCondition").GetString());
        }


        /// <summary>
        /// Description: Kiểm thử getSubscriptions trong ProcessSubscriptions - chỉ lấy Subscription có
        /// Partner hợp lệ (chưa bị xoá mềm IsDelete == null, Status == Enable và SessionState == Connected).
        /// Created date: 15/09/2026
        /// </summary>
        [Fact]
        public async Task GetSubscriptions_FiltersByPartnerStatusAndSessionStateAndIsDelete_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = CreateWorker(scope);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            // Đảm bảo Packet 101_commonData tồn tại
            var packet = await db.Queryable<ShareDataPacket>()
                .Where(p => p.Code == "101_commonData" && p.IsDelete == null)
                .FirstAsync();
            if (packet == null)
            {
                packet = new ShareDataPacket
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = "101_commonData",
                    Name = "Dữ liệu giao thông chung",
                    PacketVersion = "1.0",
                    OrderNo = 1
                };
                await db.Insertable(packet).ExecuteCommandAsync();
            }
            if (string.IsNullOrWhiteSpace((await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql))
            {
                await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, PacketTemplateSeed.TryGet("101_commonData", out var tpl) ? tpl.ExtractSql : null);
            }

            // Case 1: Partner bị Disable -> Không được chọn
            var (pDisabled, subDisabled) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_DIS_{uniqueId}", SubCode = $"S_DIS_{uniqueId}", DatatypeId = "101_commonData", ConfigurePartner = p => p.Status = BaseEnums.StatusEnum.Disable });

            // Case 2: Partner bị Disconnected -> Không được chọn
            var (pDisconn, subDisconn) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_DCN_{uniqueId}", SubCode = $"S_DCN_{uniqueId}", DatatypeId = "101_commonData", ConfigurePartner = p => p.SessionState = BaseEnums.SessionState.Disconnected });

            // Case 3: Partner bị xoá mềm (IsDelete != null) -> Không được chọn
            var (pDeleted, subDeleted) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_DEL_{uniqueId}", SubCode = $"S_DEL_{uniqueId}", DatatypeId = "101_commonData", ConfigurePartner = p => p.IsDelete = now });

            // Case 4: Partner hợp lệ (Enable, Connected, IsDelete == null) -> Được chọn
            var (pValid, subValid) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_VAL_{uniqueId}",
                SubCode     = $"S_VAL_{uniqueId}",
                DatatypeId  = "101_commonData",
                ConfigurePartner = p =>
                {
                    p.Status = BaseEnums.StatusEnum.Enable;
                    p.SessionState = BaseEnums.SessionState.Connected;
                    p.IsDelete = null;
                }
            });

            // Act: Chạy ProcessSubscriptions
            await service.ProcessSubscriptions(CancellationToken.None);

            // Assert:
            // Sub hợp lệ sẽ được giữ khóa và ghi log
            var validLogs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subValid.ID)
                .ToListAsync();
            Assert.NotEmpty(validLogs);

            // Các sub không hợp lệ sẽ KHÔNG được xử lý (không có log)
            var disabledLogs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subDisabled.ID)
                .ToListAsync();
            Assert.Empty(disabledLogs);

            var disconnLogs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subDisconn.ID)
                .ToListAsync();
            Assert.Empty(disconnLogs);

            var deletedLogs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subDeleted.ID)
                .ToListAsync();
            Assert.Empty(deletedLogs);
        }

        /// <summary>
        /// Description: Kiểm tra cô lập lỗi khi ReleaseLock bị exception trong khối finally:
        ///              Khi 1 Subscription hoàn tất lượt chạy nhưng câu lệnh nhả lock ReleaseLock
        ///              ném SqlException (mô phỏng đứt kết nối/timeout DB), ngoại lệ phải được bắt lại
        ///              và ghi log lỗi, KHÔNG ĐƯỢC lan ra ngoài làm gián đoạn hoặc hủy các Subscription
        ///              còn lại trong cùng batch.
        /// Created date: 26/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenReleaseLockThrows_DoesNotCrashBatchAndExportsRemainingSubscriptions_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            // Sub 1: Sub sẽ bị lỗi khi ReleaseLock (Code chứa 'FAIL_RELEASE')
            var (badPartner, badSub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_FAIL_RELEASE_{unique}",
                SubCode     = $"SUB_FAIL_RELEASE_{unique}",
                DatatypeId  = "101",
                ConfigureSub = s =>
                {
                    s.ID = $"00_FAIL_RELEASE_{unique}";
                    s.NextTimeRun = db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-10);
                }
            });

            // Sub 2: Sub hợp lệ cùng batch
            var (goodPartner, goodSub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_GOOD_{unique}",
                SubCode     = $"SUB_GOOD_{unique}",
                DatatypeId  = "101",
                ConfigureSub = s =>
                {
                    s.ID = $"99_GOOD_{unique}";
                    s.NextTimeRun = db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-10);
                }
            });

            var triggerName = $"trg_test_fail_release_{unique}";
            // Dọn dẹp trigger cũ (nếu có từ trước do crash)
            await db.Ado.ExecuteCommandAsync("DROP TRIGGER IF EXISTS trg_test_fail_release_lock;");

            await db.Ado.ExecuteCommandAsync($@"
CREATE OR ALTER TRIGGER {triggerName} ON ShareDataSubscription
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(State) AND EXISTS (SELECT 1 FROM inserted WHERE Code = 'SUB_FAIL_RELEASE_{unique}')
        THROW 51000, 'Mô phỏng lỗi DB đứt kết nối/timeout khi ReleaseLock', 1;
END");

            try
            {
                // Act: Chạy batch ProcessSubscriptions
                // Khi CHƯA FIX: ReleaseLock ném lỗi trong finally -> ProcessSubscriptions văng ngoại lệ
                // hoặc sập batch khiến goodSub không được xử lý -> TEST FAIL!
                // Khi ĐÃ FIX: ReleaseLock được bọc try/catch -> log error và nuốt lỗi an toàn
                // -> goodSub tiếp tục được xuất bản bình thường -> TEST PASS!
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                // Assert: Sub hợp lệ trong batch phải được xuất bản thành công
                var goodLogs = await GetLogs(db, goodSub.ID);
                Assert.NotEmpty(goodLogs);
                Assert.Equal(BaseEnums.SuccessEnums.Success, goodLogs[0].Success);

                var updatedGoodSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(goodSub.ID);
                Assert.Equal(BaseEnums.SubSubscriptionState.Active, updatedGoodSub.State);
                Assert.Equal((goodSub.SerialNbr ?? 0) + 1, updatedGoodSub.SerialNbr);
            }
            finally
            {
                // Dọn dẹp trigger để không ảnh hưởng bài test khác
                await db.Ado.ExecuteCommandAsync($"DROP TRIGGER IF EXISTS {triggerName};");
                await db.Ado.ExecuteCommandAsync("DROP TRIGGER IF EXISTS trg_test_fail_release_lock;");
            }
        }

        /// <summary>
        /// Description: Kiểm tra cô lập lỗi theo batch: khi 1 Subscription trong lượt ProcessSubscriptions
        ///              thất bại do không phân giải được gói tin (PacketNotFound), Subscription hợp lệ khác
        ///              trong CÙNG batch vẫn phải được xử lý và xuất bản thành công bình thường.
        /// Created date: 26/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenOneSubscriptionFailsWithPacketNotFound_StillExportsRemainingSubscriptionsInBatch_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (badPartner, badSub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_BAD_{unique}", SubCode = $"SUB_BAD_{unique}", DatatypeId = "999_khongtontai" });

            var (goodPartner, goodSub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_GOOD_{unique}", SubCode = $"SUB_GOOD_{unique}", DatatypeId = "101" });

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            var goodLogs = await GetLogs(db, goodSub.ID);
            Assert.NotEmpty(goodLogs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, goodLogs[0].Success);

            var updatedGoodSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(goodSub.ID);
            Assert.Equal(BaseEnums.SubSubscriptionState.Active, updatedGoodSub.State);
            Assert.Equal((goodSub.SerialNbr ?? 0) + 1, updatedGoodSub.SerialNbr);

            var badLogs = await GetLogs(db, badSub.ID);
            Assert.DoesNotContain(badLogs, l => l.Success == BaseEnums.SuccessEnums.Success);
            Assert.Single(badLogs);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, badLogs[0].Success);
            Assert.NotNull(badLogs[0].PartnerName);

            var badAlerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == badSub.ID)
                .ToListAsync();
            Assert.Single(badAlerts);
            Assert.Equal(ShareDataAlertCode.Outbound.PacketNotFound, badAlerts[0].AlertCode);

            var updatedBadSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(badSub.ID);
            Assert.Equal(BaseEnums.SubSubscriptionState.Active, updatedBadSub.State);
            Assert.NotNull(updatedBadSub.NextTimeRun);
            Assert.True(updatedBadSub.NextTimeRun > (await db.Ado.GetDateTimeAsync("SELECT GETDATE()")).AddSeconds(-5), "NextTimeRun phải được ReleaseLock cập nhật lại (không còn kẹt ở mốc lockDurationSeconds cũ).");
        }


        /// <summary>
        /// Description: Kiểm tra tích hợp toàn trình luồng Outbound: từ trích xuất thô -> Map giải $meta, $extend, CodeSet -> tạo FinalBytes -> HttpSender gửi trực tiếp tới đối tác qua HTTP.
        /// Created date: 18/09/2026
        /// </summary>
        /// <summary>
        /// Description: Kiểm chứng bất biến của deadline lock — sau khi quét, NextTimeRun ghi vào CSDL phải
        ///              nằm trong khoảng [mốc CSDL lúc gọi, mốc CSDL + thời lượng lock tối đa]. Bắt được các
        ///              lỗi thô về đơn vị/dấu/công thức khi đổi nguồn mốc thời gian sang đồng hồ CSDL.
        ///              📌 Bài test này KHÔNG chứng minh được đã dùng đồng hồ CSDL, vì CSDL test là local
        ///              (cùng máy, cùng đồng hồ) — xem mục 4.1 của prompt.
        /// Created date: 27/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessScheduledSubscriptions_WritesNextTimeRunWithinLockWindowOfDbClock_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = CreateWorker(scope);

            var suffix = Guid.NewGuid().ToString("N")[..8];
            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_{suffix}",
                SubCode     = $"S_{suffix}",
                DatatypeId  = "101",
                ConfigureSub = s =>
                {
                    s.IntervalSeconds = 60;
                    s.NextTimeRun = null;   // đến hạn ngay
                }
            });

            // Mốc CSDL NGAY TRƯỚC khi gọi — dùng làm biên dưới của khoảng hợp lệ
            var dbNowBefore = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            // Act
            await service.ProcessSubscriptions(CancellationToken.None);

            // Assert: NextTimeRun phải rơi trong [dbNowBefore, dbNowAfter + lockDuration]
            //         lockDuration = Max(300, IntervalSeconds * 3) theo công thức trong ExportSubscription
            var dbNowAfter = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var saved = await db.Queryable<ShareDataSubscription>()
                .Where(x => x.ID == sub.ID)
                .FirstAsync();

            Assert.NotNull(saved);
            Assert.NotNull(saved!.NextTimeRun);
            Assert.Null(saved.ProcessingUntil);

            var lockDurationSeconds = Math.Max(300, (sub.IntervalSeconds ?? 60) * 3);
            Assert.True(saved.NextTimeRun >= dbNowBefore,
                $"NextTimeRun ({saved.NextTimeRun}) phải >= mốc CSDL lúc gọi ({dbNowBefore}).");
            Assert.True(saved.NextTimeRun <= dbNowAfter.AddSeconds(lockDurationSeconds),
                $"NextTimeRun ({saved.NextTimeRun}) không được vượt quá mốc CSDL + {lockDurationSeconds}s ({dbNowAfter.AddSeconds(lockDurationSeconds)}).");
        }


        [Fact]
        public async Task OutboundPipeline_EndToEnd_MapTransformAndSend_Succeeds_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var codeSetCode = $"E2E_CS_{Guid.NewGuid():N}";
            var codeSet = new ShareDataCodeSet
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = codeSetCode,
                Name = "E2E Test CodeSet",
                Status = BaseEnums.StatusEnum.Enable,
                ValuesJson = """
                {
                  "defaultPartnerValue": "UNKNOWN",
                  "values": [
                    { "sourceValue": "ALERT", "partnerValue": "DANGER" },
                    { "sourceValue": "OK", "partnerValue": "NORMAL" }
                  ]
                }
                """
            };
            await db.Insertable(codeSet).ExecuteCommandAsync();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            var httpSender = new HttpDeliverySender(_host.Services.GetRequiredService<IHttpClientFactory>());

            var shapeJson = $$"""
            {
              "header": {
                "source": "ITS-TMS",
                "dataTime": { "$meta": "Now" },
                "partnerCode": { "$meta": "PartnerCode" },
                "packetCode": { "$meta": "PacketCode" },
                "serial": { "$meta": "Serial" }
              },
              "data": {
                "$each": true,
                "$as": {
                  "zoneId": { "$field": "ZoneId", "$extend": { "targetType": "int" } },
                  "status": { "$field": "Status", "$extend": { "codeSet": "{{codeSetCode}}" } },
                  "speed": { "$field": "Speed", "$extend": { "targetType": "decimal", "numberFormat": "F1" } }
                }
              }
            }
            """;

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["ZoneId"] = "1001", ["Status"] = "ALERT", ["Speed"] = 65.432 },
                new Dictionary<string, object?> { ["ZoneId"] = "1002", ["Status"] = "OK", ["Speed"] = 80.0 }
            };
            var extraction = new DataOutboundExtractionResult(rawRows, null, null);

            var partner = new ShareDataPartner
            {
                Code = "E2E_PARTNER",
                Address = ShareDataPartnerServerMock.DefaultHost,
                Port = ShareDataPartnerServerMock.DefaultPort,
                EndPointApiUrl = "/api/v1/e2e/receive"
            };
            var packet = new ShareDataPacket
            {
                Code = "101_e2ePacket",
                PacketVersion = "1.0"
            };
            var mapping = new ShareDataMapping
            {
                TargetShapeJson = shapeJson
            };
            var sub = new ShareDataSubscription
            {
                DatatypeId = "101",
                SerialNbr = 9999,
                Format = BaseEnums.PublishFormat.Data
            };
            var ctx = new DataOutboundContext(sub, partner, packet, mapping, DateTime.Now, CancellationToken.None);

            // Act 1: Map
            var mapResult = await DataMappingProcess.Map(db, extraction, ctx);

            // Assert 1: Mapping thành công
            Assert.True(mapResult.Success);
            Assert.NotNull(mapResult.FinalBytes);
            Assert.Equal(1, mapResult.RecordCount);

            // Act 2: Send via HTTP REST
            var sendResult = await httpSender.Send(mapResult, ctx, CancellationToken.None);

            // Assert 2: Gửi HTTP thành công
            Assert.True(sendResult.Success);
            Assert.NotNull(partnerServer.LastRequest);
            Assert.Equal("/api/v1/e2e/receive", partnerServer.LastRequest!.Path);
            Assert.StartsWith("application/json", partnerServer.LastRequest!.ContentType);

            // Assert 3: Kiểm tra cấu hình gói tin đối tác nhận được
            Assert.NotNull(partnerServer.LastRequest!.Body);
            using var doc = JsonDocument.Parse(partnerServer.LastRequest!.Body);
            var root = doc.RootElement;

            // Không có vỏ cũ 7 khoá
            Assert.False(root.TryGetProperty("rawContent", out _));
            Assert.False(root.TryGetProperty("datatypeId", out _));

            // Header có giải $meta đầy đủ
            Assert.True(root.TryGetProperty("header", out var header));
            Assert.Equal("ITS-TMS", header.GetProperty("source").GetString());
            Assert.Equal("E2E_PARTNER", header.GetProperty("partnerCode").GetString());
            Assert.Equal("101_e2ePacket", header.GetProperty("packetCode").GetString());
            Assert.Equal(9999, header.GetProperty("serial").GetInt64());
            Assert.True(DateTime.TryParse(header.GetProperty("dataTime").GetString(), out _));

            // Data mảng 2 phần tử được giải $extend và CodeSet đúng
            Assert.True(root.TryGetProperty("data", out var data));
            Assert.Equal(2, data.GetArrayLength());

            var item1 = data[0];
            Assert.Equal(1001, item1.GetProperty("zoneId").GetInt32());
            Assert.Equal("DANGER", item1.GetProperty("status").GetString());
            Assert.Equal(65.4m, item1.GetProperty("speed").GetDecimal());

            var item2 = data[1];
            Assert.Equal(1002, item2.GetProperty("zoneId").GetInt32());
            Assert.Equal("NORMAL", item2.GetProperty("status").GetString());
            Assert.Equal(80.0m, item2.GetProperty("speed").GetDecimal());
        }


        /// <summary>
        /// Description: Kiểm thử đối chứng chống dội: Khi Subscription gói 103 có DebounceSec=300s
        ///              và LastTimeRun đã cách đây 400s (đã qua cửa sổ chống dội),
        ///              lệnh chiếm quyền khớp điều kiện WhereIF, kết xuất bình thường,
        ///              sinh log gửi thành công và SerialNbr tăng.
        /// Created date: 27/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_Packet103_WhenDebounceWindowElapsed_ExportsNormally_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            var eqId = $"EQ_DB_OK_{unique}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_DB_OK_{unique}",
                KmNumber = 45,
                MetNumber = 100
            }).ExecuteCommandAsync();

            var initialSerial = 10;
            var pastNextRun = dbNow.AddSeconds(-20);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_DB_OK_{unique}",
                SubCode     = $"SUB_DB_OK_{unique}",
                DatatypeId  = "103",
                ConfigureSub = s =>
                {
                    s.SendOnNewData = true;
                    s.DebounceSec = 300;
                    s.LastTimeRun = dbNow.AddSeconds(-400);
                    s.NextTimeRun = pastNextRun;
                    s.SerialNbr = initialSerial;
                }
            });

            var trafficId = Guid.NewGuid().ToString("N");
            await db.Insertable(new TmsTrafficData
            {
                ID = trafficId,
                DetectTime = dbNow,
                CreateTime = dbNow,
                UpdateTime = dbNow,
                Type = "CAR",
                LicensePlate = $"30A-OK{unique[..4]}",
                Speed = 70.0f,
                Lane = "L1",
                Direction = "1",
                Location = "KM45",
                EquipmentId = eqId
            }).ExecuteCommandAsync();

            var service = CreateWorker(scope);
            await service.ProcessSubscriptions("103", CancellationToken.None);

            // Assert: Có nhật ký gửi thành công, SerialNbr tăng
            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(updatedSub);
            Assert.True(updatedSub.SerialNbr > initialSerial,
                $"SerialNbr phải tăng sau khi xuất bản thành công. Ban đầu: {initialSerial}, Hiện tại: {updatedSub.SerialNbr}");
        }

        /// <summary>
        /// Description: Kiểm thử luồng gửi theo sự kiện (SendOnNewData): khi Subscription chưa tới hạn chạy định kỳ
        ///              (NextTimeRun ở tương lai), NATS trigger vẫn kích hoạt gửi ngay lập tức và thành công.
        /// Created date: 28/09/2026
        /// </summary>
        [Fact]
        public async Task TriggerFlow_WhenScheduleNotDueYet_StillExportsImmediately_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_TRIG_{unique}",
                SubCode     = $"SUB_TRIG_{unique}",
                DatatypeId  = "101",
                ConfigureSub = s =>
                {
                    s.SendOnNewData = true;
                    s.IntervalSeconds = 300;
                    s.NextTimeRun = now.AddMinutes(5);
                }
            });

            await CreateWorker(scope).ProcessSubscriptions("101", CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
        }

        /// <summary>
        /// Description: Kiểm thử chống hồi quy cho luồng quét định kỳ: khi Subscription chưa tới hạn
        ///              (NextTimeRun ở tương lai), ProcessSubscriptions định kỳ không được phép xuất bản.
        /// Created date: 28/09/2026
        /// </summary>
        [Fact]
        public async Task ScheduledFlow_WhenScheduleNotDueYet_DoesNotExport_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_SCHED_{unique}",
                SubCode     = $"SUB_SCHED_{unique}",
                DatatypeId  = "101",
                ConfigureSub = s =>
                {
                    s.SendOnNewData = true;
                    s.IntervalSeconds = 300;
                    s.NextTimeRun = now.AddMinutes(5);
                }
            });

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Empty(logs);
        }

        /// <summary>
        /// Description: Kiểm thử luồng gửi theo sự kiện khi nằm ngoài khung giờ cho phép trong ScheduleJson:
        ///              ProcessSubscriptions nhận trigger nhưng bỏ qua không xuất bản.
        /// Created date: 28/09/2026
        /// </summary>
        [Fact]
        public async Task TriggerFlow_WhenOutsideScheduleTimeWindow_DoesNotExport_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var isUtc = Math.Abs((now - DateTime.UtcNow).TotalMinutes) < 120;
            var localNow = isUtc ? now.AddHours(7) : now;
            var (outsideStart, outsideEnd) = localNow.TimeOfDay.TotalHours < 12
                ? ("18:00", "20:00")
                : ("01:00", "03:00");
            var scheduleJson = $"{{\"startTime\":\"{outsideStart}\",\"endTime\":\"{outsideEnd}\"}}";

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_WND_{unique}",
                SubCode     = $"SUB_WND_{unique}",
                DatatypeId  = "101",
                ConfigureSub = s =>
                {
                    s.SendOnNewData = true;
                    s.IntervalSeconds = 30;
                    s.ScheduleJson = scheduleJson;
                }
            });

            await CreateWorker(scope).ProcessSubscriptions("101", CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.Empty(logs);
        }

        /// <summary>
        /// Description: Kiểm thử quan hệ giữa hai luồng: sau khi trigger NATS kết xuất thành công,
        ///              ReleaseLock đẩy NextTimeRun đi một chu kỳ nên luồng quét định kỳ chạy ngay
        ///              sau đó phải bỏ qua đăng ký đó, không gửi lặp lần hai.
        /// Created date: 28/09/2026
        /// </summary>
        [Fact]
        public async Task TriggerFlow_WhenExportSucceeds_AdvancesNextTimeRunSoScheduledSweepSkipsIt_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var initialSerialNbr = 100;
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_TRIG_ADV_{unique}",
                SubCode     = $"SUB_TRIG_ADV_{unique}",
                DatatypeId  = "101",
                ConfigureSub = s =>
                {
                    s.SendOnNewData = true;
                    s.IntervalSeconds = 30;
                    s.NextTimeRun = now.AddSeconds(300);
                    s.LastTimeRun = null;
                    s.SerialNbr = initialSerialNbr;
                }
            });

            var service = CreateWorker(scope);

            await service.ProcessSubscriptions("101", CancellationToken.None);

            var logsAfterTrigger = await GetLogs(db, sub.ID);
            var successLogsCount = logsAfterTrigger.Count(l => l.Success == BaseEnums.SuccessEnums.Success);
            Assert.True(successLogsCount > 0);

            var subAfterTrigger = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(subAfterTrigger);
            Assert.NotNull(subAfterTrigger.LastTimeRun);
            Assert.True(subAfterTrigger.SerialNbr > initialSerialNbr);

            var serialAfterTrigger = subAfterTrigger.SerialNbr;
            var lastTimeRunAfterTrigger = subAfterTrigger.LastTimeRun;

            var dbNowAfterTrigger = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            Assert.True(subAfterTrigger.NextTimeRun > now && subAfterTrigger.NextTimeRun <= dbNowAfterTrigger.AddSeconds(35));

            await service.ProcessSubscriptions(CancellationToken.None);

            var logsAfterSweep = await GetLogs(db, sub.ID);
            var successLogsCountAfterSweep = logsAfterSweep.Count(l => l.Success == BaseEnums.SuccessEnums.Success);
            Assert.Equal(successLogsCount, successLogsCountAfterSweep);

            var subAfterSweep = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(subAfterSweep);
            Assert.Equal(serialAfterTrigger, subAfterSweep.SerialNbr);
            Assert.Equal(lastTimeRunAfterTrigger, subAfterSweep.LastTimeRun);
        }

        /// <summary>
        /// Description: Kiểm thử làm rõ nghi vấn logic NATS vs Periodic Worker:
        ///              1) Khi có trigger NATS cho gói tin khác hoặc trigger gói 102 nhưng gói 102 có SendOnNewData = false:
        ///                 Worker TUYỆT ĐỐI KHÔNG xuất bản gói 102.
        ///              2) Chỉ khi đến hạn chu kỳ định kỳ (NextTimeRun <= now), Worker định kỳ mới xuất bản gói 102
        ///                 bất kể cờ SendOnNewData là true hay false.
        /// Created date: 06/10/2026
        /// </summary>
        [Fact]
        public async Task TriggerFlow_WhenSendOnNewDataIsFalse_NeverExportsOnTrigger_AndScheduledFlowExportsWhenDue_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            await PacketMetadataCatalogTest.SeedPacketToDb(db, "102");

            var unique101 = Guid.NewGuid().ToString("N")[..8];
            var unique102 = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique101);
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.CctvImage, unique102);

            var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            // Gói 101: Bật SendOnNewData = true, lịch định kỳ chưa tới hạn (+5 phút)
            var (partner101, sub101) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_101_{unique101}",
                SubCode = $"SUB_101_{unique101}",
                DatatypeId = "101",
                ConfigureSub = s =>
                {
                    s.SendOnNewData = true;
                    s.IntervalSeconds = 30;
                    s.NextTimeRun = now.AddMinutes(5);
                }
            });

            // Gói 102 (Sub A): SendOnNewData = false, lịch định kỳ chưa tới hạn (+5 phút)
            var (partner102F, sub102Future) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_102F_{unique102}",
                SubCode = $"SUB_102F_{unique102}",
                DatatypeId = "102",
                ConfigureSub = s =>
                {
                    s.SendOnNewData = false;
                    s.IntervalSeconds = 30;
                    s.NextTimeRun = now.AddMinutes(5);
                }
            });

            // Gói 102 (Sub B): SendOnNewData = false, nhưng lịch định kỳ ĐÃ TỚI HẠN (<= now)
            var (partner102D, sub102Due) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_102D_{unique102}",
                SubCode = $"SUB_102D_{unique102}",
                DatatypeId = "102",
                ConfigureSub = s =>
                {
                    s.SendOnNewData = false;
                    s.IntervalSeconds = 30;
                    s.NextTimeRun = now.AddSeconds(-10);
                }
            });

            var worker = CreateWorker(scope);

            // Act 1: Nhận trigger NATS cho gói 102 (bị chặn bởi SendOnNewData == false)
            await worker.ProcessSubscriptions("102", CancellationToken.None);

            // Assert 1: Không có bất kỳ log nào cho gói 102 được tạo ra từ trigger
            var logs102FutureAfterTrigger102 = await GetLogs(db, sub102Future.ID);
            var logs102DueAfterTrigger102 = await GetLogs(db, sub102Due.ID);
            Assert.Empty(logs102FutureAfterTrigger102);
            Assert.Empty(logs102DueAfterTrigger102);

            // Act 2: Nhận trigger NATS cho gói 101 (được xử lý ngay do SendOnNewData == true)
            await worker.ProcessSubscriptions("101", CancellationToken.None);

            // Assert 2: Gói 101 gửi thành công, gói 102 vẫn hoàn toàn không bị động tới
            var logs101AfterTrigger101 = await GetLogs(db, sub101.ID);
            Assert.NotEmpty(logs101AfterTrigger101);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs101AfterTrigger101[0].Success);

            var logs102FutureAfterTrigger101 = await GetLogs(db, sub102Future.ID);
            Assert.Empty(logs102FutureAfterTrigger101);

            // Act 3: Luồng định kỳ (Periodic Worker) quét tự động
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 3: Gói 102 đã đến hạn định kỳ ĐƯỢC XUẤT BẢN THÀNH CÔNG bởi luồng định kỳ
            var logs102DueAfterScheduled = await GetLogs(db, sub102Due.ID);
            Assert.NotEmpty(logs102DueAfterScheduled);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs102DueAfterScheduled[0].Success);

            // Gói 102 chưa đến hạn định kỳ vẫn không xuất bản
            var logs102FutureAfterScheduled = await GetLogs(db, sub102Future.ID);
            Assert.Empty(logs102FutureAfterScheduled);
        }


        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm thử thông luồng E2E Worker Outbound:
        ///              - Trích xuất động qua câu truy vấn SQL trong ShareDataPacket.ExtractSql (Gói 101 Snapshot & Gói 103 Incremental).
        ///              - Ánh xạ JSON schema chuẩn và Dispatch tới Đối tác REST API (HttpListener).
        ///              - Ghi ActivityLog phân cấp cha - con (Step 1, Step 2, Step 3) sạch sẽ, không nối lặp Description.
        ///              - Gói 103 (Incremental): Tịnh tiến mốc LastTime và LastKey trong ShareDataLastSend và ShareDataSubscription.
        /// Created date: 14/10/2026
        /// </summary>
        [Fact]
        public async Task OutboundE2E_DynamicQuery_ExtractMapAndDispatchViaRest_WithCleanParentChildLogs_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            var unique = Guid.NewGuid().ToString("N")[..8];

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");

            // Seed Template SQL động cho gói 101 (Snapshot)
            var sql101 = @"SELECT
    zs.ID AS zoneStatusId,
    zs.ZoneId AS zoneId,
    z.Name AS zoneName,
    z.FromKmNumber AS fromLocationKm,
    z.FromMetNumber AS fromLocationMet,
    z.ToKmNumber AS toLocationKm,
    z.ToMetNumber AS toLocationMet,
    z.LaneId AS laneId,
    CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed,
    zs.Condition AS trafficCondition,
    zs.UpdateTime AS dataTime,
    z.MaxSpeed AS speedLimit,
    ts.TotalVehicleNumber AS vehicleCount
FROM TmsZoneStatus zs
LEFT JOIN TmsZone z ON zs.ZoneId = z.ID
LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId";

            foreach (var packet101 in await db.Queryable<ShareDataPacket>().Where(p => p.Code == "101" || p.Code == "101_commonData").ToListAsync())
            {
                await ShareDataSqlSeed.SetCommonAsync(db, packet101.ID, sql101);
            }

            // Seed Template SQL động cho gói 103 (Incremental)
            var sql103 = @"SELECT TOP 100
    td.ID AS detectionId,
    td.DetectTime AS detectTime,
    td.Type AS vehicleType,
    td.LicensePlate AS licensePlate,
    td.Speed AS speed,
    td.Lane AS lane,
    td.Direction AS direction,
    td.Location AS locationRoute,
    td.EquipmentId AS equipmentId,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) AS __lastTime,
    td.ID AS __lastKey,
    td.UpdateTime AS __auditUpdate,
    td.CreateTime AS __auditCreate
FROM TmsTrafficData td
LEFT JOIN TmsEquipment e ON td.EquipmentId = e.ID
WHERE td.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) > @lastTime
      OR (COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) = @lastTime AND td.ID > @lastKey)
  )
ORDER BY COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) ASC, td.ID ASC";

            foreach (var packet103 in await db.Queryable<ShareDataPacket>().Where(p => p.Code == "103" || p.Code == "103_vdsData").ToListAsync())
            {
                await ShareDataSqlSeed.SetCommonAsync(db, packet103.ID, sql103, "DetectTime");
            }

            // Seed Dữ liệu nghiệp vụ kiểm thử
            var zoneId = $"ZONE_{unique}";
            var zoneNow = DateTime.Now;
            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Tuyến Test E2E {unique}",
                FromKmNumber = 10,
                FromMetNumber = 500,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80
            }).ExecuteCommandAsync();
            await db.Insertable(new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = zoneId,
                AverageSpeed = "72.5",
                Condition = "NORMAL",
                UpdateTime = zoneNow
            }).ExecuteCommandAsync();
            await db.Insertable(new TmsTrafficStatistic
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = zoneId,
                TotalVehicleNumber = 250
            }).ExecuteCommandAsync();

            var eqId = $"EQ_{unique}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"VDS_EQ_{unique}",
                KmNumber = 35,
                MetNumber = 150
            }).ExecuteCommandAsync();

            var baseDetectTime = DateTime.Parse("2026-10-14 10:00:00");
            var trafficDataId = Guid.NewGuid().ToString("N");
            var trafficRow = new TmsTrafficData
            {
                ID = trafficDataId,
                EquipmentId = eqId,
                DetectTime = baseDetectTime,
                CreateTime = baseDetectTime,
                UpdateTime = baseDetectTime,
                Type = "CAR",
                LicensePlate = $"30A-{unique[..5].ToUpper()}",
                Speed = 88.0f,
                Lane = "L1",
                Direction = "NORTH",
                Location = "KM35",
                Height = 150,
                Width = 180,
                Length = 450
            };
            await db.Insertable(trafficRow).ExecuteCommandAsync();
            await db.Ado.ExecuteCommandAsync(
                "UPDATE TmsTrafficData SET CreateTime = DetectTime, UpdateTime = DetectTime WHERE ID = @id",
                new { id = trafficDataId });

            // Seed Đối tác REST API & Subscription 101
            var (partner, sub101) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_E2E_{unique}",
                SubCode = $"SUB_101_{unique}",
                DatatypeId = "101",
                ConfigurePartner = p =>
                {
                    p.Address = ShareDataPartnerServerMock.DefaultHost;
                    p.Port = ShareDataPartnerServerMock.DefaultPort;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                },
                ConfigureSub = s =>
                {
                    s.SendOnNewData = true;
                    s.IntervalSeconds = 30;
                }
            });

            // Seed Subscription 103 thuộc cùng đối tác
            var sub103 = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"SUB_103_{unique}",
                PartnerId = partner.ID,
                DatatypeId = "103",
                Direction = BaseEnums.Direction.Outbound,
                Mode = BaseEnums.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                SendOnNewData = true,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
                IntervalSeconds = 30,
                LastTimeRun = baseDetectTime.AddMinutes(-10)
            };
            await db.Insertable(sub103).ExecuteCommandAsync();

            var shapeDict103 = new Dictionary<string, object>();
            foreach (var key in await GetCommonFieldKeysAsync(db, "103"))
            {
                shapeDict103[key] = new Dictionary<string, string> { { "$field", key } };
            }
            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = "103",
                Direction = BaseEnums.Direction.Outbound,
                IsActive = true,
                TargetShapeJson = JsonSerializer.Serialize(shapeDict103)
            }).ExecuteCommandAsync();

            // Act - Kích hoạt TriggerFlow (ProcessSubscriptions theo từng mã gói tin)
            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions("101", CancellationToken.None);
            await worker.ProcessSubscriptions("103", CancellationToken.None);

            // Assert 1 & 2: Partner server nhận được ít nhất 2 requests với payload chuẩn
            Assert.True(partnerServer.TotalReceivedRequests >= 2,
                $"Partner server phải nhận được ít nhất 2 requests, thực tế nhận {partnerServer.TotalReceivedRequests}");

            var req101 = partnerServer.ReceivedRequests.FirstOrDefault(r => r.Body != null && r.Body.Contains(zoneId));
            Assert.NotNull(req101);
            using var doc101 = JsonDocument.Parse(req101!.Body);
            Assert.Equal(JsonValueKind.Array, doc101.RootElement.ValueKind);
            Assert.True(doc101.RootElement.GetArrayLength() > 0);
            var item101 = doc101.RootElement[0];
            Assert.True(item101.TryGetProperty("zoneId", out _));
            Assert.True(item101.TryGetProperty("averageSpeed", out _));
            Assert.True(item101.TryGetProperty("trafficCondition", out _));

            var req103 = partnerServer.ReceivedRequests.FirstOrDefault(r => r.Body != null && r.Body.Contains(trafficDataId));
            Assert.NotNull(req103);
            using var doc103 = JsonDocument.Parse(req103!.Body);
            Assert.Equal(JsonValueKind.Array, doc103.RootElement.ValueKind);
            Assert.True(doc103.RootElement.GetArrayLength() > 0);
            var item103 = doc103.RootElement[0];
            Assert.True(item103.TryGetProperty("detectionId", out _));
            Assert.True(item103.TryGetProperty("licensePlate", out _));
            Assert.True(item103.TryGetProperty("speed", out _));

            // Assert 3: ActivityLog phân cấp cha - con sạch sẽ, không nối lặp Description
            var logs101 = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub101.ID)
                .OrderBy(l => l.OccurredAt)
                .ToListAsync();
            Assert.True(logs101.Count >= 4, $"Gói 101 phải có ít nhất 4 dòng log (1 cha + 3 con), thực tế: {logs101.Count}");

            var parent101 = logs101.First(l => l.ParentId == null);
            Assert.Null(parent101.StepNbr);
            Assert.Equal(BaseEnums.SuccessEnums.Success, parent101.Success);
            Assert.True(parent101.RecordCount > 0);
            Assert.False(string.IsNullOrWhiteSpace(parent101.Description));
            Assert.DoesNotContain(": :", parent101.Description);

            var step1_101 = logs101.Single(l => l.ParentId == parent101.ID && l.StepNbr == 1);
            Assert.Equal(BaseEnums.SuccessEnums.Success, step1_101.Success);
            Assert.DoesNotContain(": :", step1_101.Description ?? string.Empty);

            var step2_101 = logs101.Single(l => l.ParentId == parent101.ID && l.StepNbr == 2);
            Assert.Equal(BaseEnums.SuccessEnums.Success, step2_101.Success);
            Assert.DoesNotContain(": :", step2_101.Description ?? string.Empty);

            var step3_101 = logs101.Single(l => l.ParentId == parent101.ID && l.StepNbr == 3);
            Assert.Equal(BaseEnums.SuccessEnums.Success, step3_101.Success);
            Assert.DoesNotContain(": :", step3_101.Description ?? string.Empty);

            var logs103 = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub103.ID)
                .OrderBy(l => l.OccurredAt)
                .ToListAsync();
            Assert.True(logs103.Count >= 4, $"Gói 103 phải có ít nhất 4 dòng log (1 cha + 3 con), thực tế: {logs103.Count}");

            var parent103 = logs103.First(l => l.ParentId == null);
            Assert.Null(parent103.StepNbr);
            Assert.Equal(BaseEnums.SuccessEnums.Success, parent103.Success);
            Assert.True(parent103.RecordCount > 0);
            Assert.DoesNotContain(": :", parent103.Description ?? string.Empty);

            var step1_103 = logs103.Single(l => l.ParentId == parent103.ID && l.StepNbr == 1);
            Assert.Equal(BaseEnums.SuccessEnums.Success, step1_103.Success);
            Assert.DoesNotContain(": :", step1_103.Description ?? string.Empty);

            var step2_103 = logs103.Single(l => l.ParentId == parent103.ID && l.StepNbr == 2);
            Assert.Equal(BaseEnums.SuccessEnums.Success, step2_103.Success);
            Assert.DoesNotContain(": :", step2_103.Description ?? string.Empty);

            var step3_103 = logs103.Single(l => l.ParentId == parent103.ID && l.StepNbr == 3);
            Assert.Equal(BaseEnums.SuccessEnums.Success, step3_103.Success);
            Assert.DoesNotContain(": :", step3_103.Description ?? string.Empty);

            // Assert 4: Gói 103 mốc LastTime / LastKey được cập nhật tịnh tiến chính xác
            var updatedSub103 = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub103.ID);
            Assert.NotNull(updatedSub103.LastTimeRun);
            Assert.Equal(baseDetectTime, updatedSub103.LastTimeRun);

            var lastSend103 = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partner.Code && c.PacketCode == "103")
                .FirstAsync();
            Assert.NotNull(lastSend103);
            Assert.Equal(baseDetectTime, lastSend103.LastTime);
            Assert.Equal(trafficDataId, lastSend103.LastKey);
        }

        /// <summary>
        /// Description: Kiểm thử toàn trình gói 110 (PublicMessaging - Phương án A, dùng bảng có sẵn TmsIncident, TmsEquipment, VmsCurrent, TmsWeather):
        ///              - Trích xuất dữ liệu từ các bảng có sẵn qua SQL Template động Snapshot.
        ///              - Kiểm tra payload JSON gửi qua REST có đủ các trường: messageId, incidentMessage, weatherMessage, guidanceContent, locationKm, locationMet, publishedTime.
        ///              - Chế độ Snapshot: Không tịnh tiến cursor ShareDataLastSend (LastKey luôn null).
        ///              - Chạy chu kỳ 2: Tiếp tục gửi toàn bộ bản tin Snapshot hợp lệ.
        ///              - Trạng thái Status = 0 (Disable): Worker bỏ qua không trích xuất và không sinh AlertLog.
        /// Created date: 16/10/2026
        /// </summary>
        [Fact]
        public async Task OutboundE2E_Packet110_SnapshotFromExistingTables_DispatchViaRest_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            var unique = Guid.NewGuid().ToString("N")[..8];
            var now = DateTime.Now;
            var km = 85;
            var met = 200;
            var incidentId = $"INC_110_{unique}";

            // 1. Seed dữ liệu vào các bảng nguồn có sẵn
            await db.Insertable(new TmsIncident
            {
                ID = incidentId,
                Code = $"INC_CODE_{unique}",
                Name = "Sự cố kiểm thử 110",
                Description = "Va chạm giao thông",
                StartDate = now,
                KmNumber = km,
                MetNumber = met,
                State = "PROCESSING",
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            var eqId = $"EQ_110_{unique}";
            await db.Insertable(new TmsEquipment
            {
                ID = eqId,
                Code = $"EQ_CODE_{unique}",
                KmNumber = km,
                MetNumber = met,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            await db.Insertable(new VmsCurrent
            {
                ID = $"VMS_110_{unique}",
                EquipmentId = eqId,
                RowData = "CHÚ Ý SỰ CỐ KM85 - GIẢM TỐC ĐỘ",
                ExecutedDate = now,
                CreateTime = now,
                UpdateTime = now
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsWeather
            {
                ID = $"WEA_110_{unique}",
                Description = "Trời mưa nhỏ tầm nhìn 1km",
                TimeDetect = now,
                CreateTime = now
            }).ExecuteCommandAsync();

            // 2. Seed Packet 110 với SQL Template chuẩn
            await PacketMetadataCatalogTest.SeedPacketToDb(db, "110");
            PacketTemplateSeed.TryGet("110", out var tpl110);
            await db.Updateable<ShareDataPacket>()
                .SetColumns(p => p.Status == BaseEnums.StatusEnum.Enable)
                .Where(p => p.Code == "110" || p.Code == "110_publicMessaging")
                .ExecuteCommandAsync();
            foreach (var packet110 in await db.Queryable<ShareDataPacket>().Where(p => p.Code == "110" || p.Code == "110_publicMessaging").ToListAsync())
            {
                await ShareDataSqlSeed.SetCommonAsync(db, packet110.ID, tpl110 != null ? tpl110.ExtractSql : null);
            }

            // 3. Đăng ký chia sẻ (Subscription)
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_110_{unique}",
                SubCode = $"SUB_110_{unique}",
                DatatypeId = "110",
                ConfigureSub = s =>
                {
                    s.Mode = BaseEnums.SubMode.Periodic;
                    s.IntervalSeconds = 30;
                }
            });

            var worker = CreateWorker(scope);

            // Act: Chu kỳ 1
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert Chu kỳ 1
            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            var parentLog = logs.First(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Success, parentLog.Success);
            Assert.True(parentLog.RecordCount > 0);

            Assert.True(partnerServer.TotalReceivedRequests >= 1);
            var lastRequest = partnerServer.LastRequest;
            Assert.NotNull(lastRequest);
            using var doc = JsonDocument.Parse(lastRequest.Body);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            var item = doc.RootElement[0];

            Assert.True(item.TryGetProperty("incidentMessage", out var incMsg));
            Assert.Contains("Sự cố kiểm thử 110", incMsg.GetString());

            Assert.True(item.TryGetProperty("weatherMessage", out var weaMsg));
            Assert.Contains("Trời mưa nhỏ", weaMsg.GetString());

            Assert.True(item.TryGetProperty("guidanceContent", out var guidMsg));
            Assert.Contains("CHÚ Ý SỰ CỐ", guidMsg.GetString());

            Assert.True(item.TryGetProperty("messageId", out var msgId));
            Assert.Equal(incidentId, msgId.GetString());

            // Chế độ Snapshot: LastKey của LastSend luôn là null
            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == "110")
                .FirstAsync();
            Assert.Null(lastSend?.LastKey);

            // Act & Assert: Chu kỳ 2 tiếp tục gửi full bản tin Snapshot
            var dbNowCycle2 = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == dbNowCycle2.AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
            await worker.ProcessSubscriptions(CancellationToken.None);

            var logsCycle2 = await GetLogs(db, sub.ID);
            var parentLogs = logsCycle2.Where(l => l.ParentId == null).ToList();
            Assert.Equal(2, parentLogs.Count);
            Assert.All(parentLogs, p => Assert.Equal(BaseEnums.SuccessEnums.Success, p.Success));
            Assert.Equal(parentLogs[0].RecordCount, parentLogs[1].RecordCount);

            // Assert: Khi gói bị tắt (Status = 0 / Disable) -> Worker bỏ qua, không sinh thêm AlertLog
            await db.Updateable<ShareDataPacket>()
                .SetColumns(p => p.Status == BaseEnums.StatusEnum.Disable)
                .Where(p => p.Code == "110" || p.Code == "110_publicMessaging")
                .ExecuteCommandAsync();
            var dbNowDisabled = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == dbNowDisabled.AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            var alertsBefore = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).CountAsync();
            await worker.ProcessSubscriptions(CancellationToken.None);
            var alertsAfter = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).CountAsync();
            Assert.Equal(alertsBefore, alertsAfter);
        }

        #endregion

        #region 2. Các Kiểm Thử Thành Phần & Bảo Mật (Security & Paging Workflows)



        /// <summary>
        /// Description: Kiểm tra kiểm tra tĩnh từ chối các cấu trúc biểu thức nguy hiểm như câu lệnh SQL mutation.
        /// Created date: 17/09/2026
        /// </summary>
        [Theory]
        [InlineData("SELECT * FROM users")]
        [InlineData("1; DROP TABLE EshPartner")]
        [InlineData("1 + /* comment */ 2")]
        [InlineData("EXEC('sp_who')")]
        [InlineData("sys.tables")]
        public void IsStaticallyValid_RejectsDangerousConstructs_Test(string dangerousExpr)
        {
            // Act
            var valid = DataMappingProcess.IsStaticallyValid(dangerousExpr, out var error);

            // Assert
            Assert.False(valid);
            Assert.NotNull(error);
        }


        /// <summary>
        /// Description: Kiểm tra kiểm tra tĩnh từ chối các biểu thức rỗng hoặc vượt quá độ dài quy định.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void IsStaticallyValid_RejectsEmptyOrOversizedExpression_Test()
        {
            // Empty
            Assert.False(DataMappingProcess.IsStaticallyValid("", out var err1));
            Assert.NotNull(err1);

            // Oversized (> 512 chars)
            var longExpr = "1 + " + new string('1', 515);
            Assert.False(DataMappingProcess.IsStaticallyValid(longExpr, out var err2));
            Assert.NotNull(err2);
        }

        /// <summary>
        /// Description: Kiểm tra khi Partner.Code chứa ký tự xuống dòng (\r\n) chống header injection, request.Headers.Add ném ngoại lệ được bắt an toàn và Send trả về thất bại có thông điệp rõ ràng.
        /// Created date: 19/09/2026
        /// </summary>
        [Fact]
        public async Task HttpSender_Send_WhenPartnerCodeContainsCrLf_CatchesSafelyAndReturnsFailure_Test()
        {
            // Arrange
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            var sender = new HttpDeliverySender(_host.Services.GetRequiredService<IHttpClientFactory>());

            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes("""{"test":1}"""), 1);
            var maliciousPartner = new ShareDataPartner
            {
                Code = "PARTNER\r\nX-Injected: Bad",
                Address = ShareDataPartnerServerMock.DefaultHost,
                Port = ShareDataPartnerServerMock.DefaultPort,
                EndPointApiUrl = "/api"
            };
            var packet = new ShareDataPacket { Code = "101_commonData" };
            var sub = new ShareDataSubscription { SerialNbr = 1 };
            var ctx = new DataOutboundContext(sub, maliciousPartner, packet, null, DateTime.Now, CancellationToken.None);

            // Act
            var result = await sender.Send(mapping, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.NotEmpty(result.ErrorMessage);
        }



        /// <summary>
        /// Description: Kiểm thử qua ranh giới phân trang: khi có 250 dòng dữ liệu, luồng gửi chạy qua 3 trang
        ///              (100 + 100 + 50), không trùng lặp và không bỏ sót bản ghi nào ở biên trang.
        /// Created date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenRowsSpanThreePages_ExportsEveryRowExactlyOnceAcrossPages_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_PAGING_{unique}";
            var platePrefix = $"PG{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_PG_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_PG_{unique}", SubCode = $"SUB_PG_{unique}", DatatypeId = "103", ConfigureSub = s => s.LastTimeRun = baseTime });

            var rows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 250);

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            try
            {
                // Act
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                // Assert
                // 3 trang: 100 + 100 + 50 (lọc ParentId == null để đếm 3 trang cha)
                var logs = (await GetLogs(db, sub.ID)).Where(l => l.ParentId == null && l.RecordCount > 0).OrderBy(l => l.OccurredAt).ToList();
                Assert.Equal(3, logs.Count);
                Assert.All(logs, l => Assert.Equal(BaseEnums.SuccessEnums.Success, l.Success));
                Assert.Equal([100, 100, 50], logs.Select(l => l.RecordCount).ToArray());

                // Không trùng ở biên trang, không sót
                var plates = await CollectExportedPlates(db, sub.ID, platePrefix);
                Assert.Equal(250, plates.Count);
                Assert.Equal(250, plates.Distinct().Count());

                // Cursor tiến tới bản ghi cuối cùng
                var lastSend = await db.Queryable<ShareDataLastSend>().Where(c => c.PartnerCode == partner.Code).FirstAsync();
                Assert.NotNull(lastSend);
                Assert.Equal(rows[^1].DetectTime!.Value, lastSend.LastTime!.Value, TimeSpan.FromSeconds(1));

                var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
                Assert.Equal(3, updatedSub.SerialNbr);
            }
            finally
            {
                await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
                await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Chứng minh trần maxPagesPerRun = 20: khi có 2100 dòng, lượt 1 dừng ở đúng 20 trang (2000 dòng),
        ///              lượt 2 quét tiếp 100 dòng còn lại đầy đủ mà không mất dữ liệu.
        /// Created date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenBacklogExceedsPageLimit_StopsAtConfiguredPageLimitAndResumesNextRun_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_PAGING_{unique}";
            var platePrefix = $"PG{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_PG_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-34);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_PG_{unique}", SubCode = $"SUB_PG_{unique}", DatatypeId = "103", ConfigureSub = s => s.LastTimeRun = baseTime });

            var rows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 310);

            var workerConfig = await CreateWorkerConfigWithMaxPages(scope, db, 3);
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            // Act 1: Lượt 1 — dừng đúng ở trần 3 trang
            await CreateWorker(scope, workerConfig: workerConfig).ProcessSubscriptions(CancellationToken.None);

            // Assert 1 (lọc ParentId == null để đếm đúng 3 trang cha)
            var firstRun = (await GetLogs(db, sub.ID)).Where(l => l.ParentId == null && l.RecordCount > 0).ToList();
            Assert.Equal(3, firstRun.Count);
            Assert.Equal(300, firstRun.Sum(l => l.RecordCount));

            // Act 2: Lượt 2 — lấy nốt 10 dòng còn lại, không mất dữ liệu
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == baseTime)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            await CreateWorker(scope, workerConfig: workerConfig).ProcessSubscriptions(CancellationToken.None);

            // Assert 2
            var plates = await CollectExportedPlates(db, sub.ID, platePrefix);
            Assert.Equal(310, plates.Count);
            Assert.Equal(310, plates.Distinct().Count());

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Chứng minh commit theo từng trang: seed 250 dòng, khi HTTP fail ở trang 3 thì 2 trang
        ///              đầu vẫn commit thành công, lượt quét kế tiếp gửi tiếp từ bản ghi 201 không trùng lặp.
        /// Created date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenHttpFailsOnThirdPage_CommitsFirstTwoPagesAndHalts_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_PAGING_{unique}";
            var platePrefix = $"PG{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_PG_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-33);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_PG_{unique}", SubCode = $"SUB_PG_{unique}", DatatypeId = "103", ConfigureSub = s => s.LastTimeRun = baseTime });

            var rows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 250);

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            var requestCount = 0;
            partnerServer.Responder = (callIndex, req) =>
            {
                if (req.Headers.TryGetValue("PartnerCode", out var code) && code == partner.Code)
                {
                    requestCount++;
                    if (requestCount >= 3)
                        return new PartnerResponsePlan(HttpStatusCode.InternalServerError);
                }

                return new PartnerResponsePlan(HttpStatusCode.OK);
            };

            // Act 1
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 1
            var logs = await GetLogs(db, sub.ID);
            Assert.Equal(2, logs.Count(l => l.ParentId == null && l.Success == BaseEnums.SuccessEnums.Success && l.RecordCount > 0));
            Assert.Equal(1, logs.Count(l => l.ParentId == null && l.Success == BaseEnums.SuccessEnums.Fail));

            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ToListAsync();
            Assert.Contains(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed && a.Severity == BaseEnums.AlertSeverity.Warning);

            // Watermark dừng đúng bản ghi thứ 200 — không nhảy tới 250, không lùi
            var lastSend = await db.Queryable<ShareDataLastSend>().Where(c => c.PartnerCode == partner.Code).FirstAsync();
            Assert.Equal(rows[199].DetectTime!.Value, lastSend.LastTime!.Value, TimeSpan.FromSeconds(1));

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(2, updatedSub.SerialNbr);

            // Act 2: Đưa mock server về mặc định 200, đặt NextTimeRun về quá khứ, chạy lượt 2
            partnerServer.ResetDefaults();

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == baseTime)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 2: Gửi tiếp từ bản ghi 201, không gửi lại 200 bản ghi đầu
            var plates = await CollectExportedPlates(db, sub.ID, platePrefix);
            Assert.Equal(250, plates.Distinct().Count());

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Chứng minh khoá độc quyền ProcessingUntil: hai worker cùng quét 250 dòng đồng thời,
        ///              chỉ một worker chiếm lock xử lý và không bị gửi lặp bản ghi.
        /// Created date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenTwoWorkersScanConcurrently_OnlyOneGetsLockAndNoDuplicateExport_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_PAGING_{unique}";
            var platePrefix = $"PG{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_PG_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-32);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_PG_{unique}", SubCode = $"SUB_PG_{unique}", DatatypeId = "103", ConfigureSub = s => s.LastTimeRun = baseTime });

            var rows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 250);

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            var workerA = CreateWorker(scope);
            var workerB = CreateWorker(scope);

            // Act
            await Task.WhenAll(
                workerA.ProcessSubscriptions(CancellationToken.None),
                workerB.ProcessSubscriptions(CancellationToken.None));

            // Assert
            var plates = await CollectExportedPlates(db, sub.ID, platePrefix);
            Assert.Equal(250, plates.Count);
            Assert.Equal(250, plates.Distinct().Count());

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(3, updatedSub.SerialNbr);

            await db.Deleteable<TmsTrafficData>().Where(t => t.EquipmentId == eqId).ExecuteCommandAsync();
            await db.Deleteable<TmsEquipment>().Where(e => e.ID == eqId).ExecuteCommandAsync();
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Chứng minh khi lock bị cướp giữa vòng phân trang: worker dừng sạch sau trang đã commit,
        ///              không gửi trùng, lượt sau khôi phục gửi nốt dữ liệu còn lại.
        /// Created date: 29/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenLockStolenMidPaging_HaltsAfterCommittedPageAndResumesCleanly_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_PAGING_{unique}";
            var platePrefix = $"PG{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_PG_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-31);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed { PartnerCode = $"P_PG_{unique}", SubCode = $"SUB_PG_{unique}", DatatypeId = "103", ConfigureSub = s => s.LastTimeRun = baseTime });

            var rows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 250);

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            var requestCount = 0;
            using var stealDb = db.CopyNew();   // Kết nối RIÊNG — không dùng chung với worker đang chạy
            partnerServer.Responder = (callIndex, req) =>
            {
                if (req.Headers.TryGetValue("PartnerCode", out var code) && code == partner.Code)
                {
                    requestCount++;
                    if (requestCount == 2)
                    {
                        // Worker khác cướp lock: đổi ProcessingUntil sang giá trị khác mốc worker đang giữ
                        stealDb.Updateable<ShareDataSubscription>()
                            .SetColumns(s => s.ProcessingUntil == baseTime.AddYears(1))
                            .Where(s => s.ID == sub.ID)
                            .ExecuteCommand();
                    }
                }

                return new PartnerResponsePlan(HttpStatusCode.OK);
            };

            // Act 1
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 1: Chỉ trang 1 được commit — trang 2 mất lock nên CommitSuccess rollback và vòng lặp dừng
            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(1, updatedSub.SerialNbr);

            var lastSend = await db.Queryable<ShareDataLastSend>().Where(c => c.PartnerCode == partner.Code).FirstAsync();
            Assert.Equal(rows[99].DetectTime!.Value, lastSend.LastTime!.Value, TimeSpan.FromSeconds(1));

            // Act 2: Trả ProcessingUntil về null, đặt NextTimeRun về quá khứ, chạy lượt 2 với mock server mặc định 200
            partnerServer.ResetDefaults();

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.ProcessingUntil == null)
                .SetColumns(s => s.NextTimeRun == baseTime)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 2: Gửi tiếp từ bản ghi 101, tổng cộng Distinct() = 250
            var plates = await CollectExportedPlates(db, sub.ID, platePrefix);
            Assert.Equal(250, plates.Distinct().Count());

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: SV-8a: Gửi 1 trang thành công ghi 1 dòng cha kèm 2 dòng con Bước 1 (Trích xuất) và Bước 2 (Ánh xạ & Gửi).
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_ExportOnePage_Success_LogsParentWithTwoStepChildren_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            _host.PartnerServer.ResetDefaults();

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .ExecuteCommandAsync();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_ONE_{unique}",
                SubCode = $"SUB_ONE_{unique}",
                DatatypeId = "101",
                ConfigureSub = s => s.SerialNbr = 1
            });

            try
            {
                // Act
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                // Assert
                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderBy(l => l.OccurredAt)
                    .ToListAsync();

                Assert.Equal(4, logs.Count);

                var parent = logs.Single(l => l.ParentId == null);
                Assert.Null(parent.StepNbr);
                Assert.Equal(BaseEnums.SuccessEnums.Success, parent.Success);
                Assert.True(parent.RecordCount > 0);
                Assert.True(parent.ByteSize > 0);
                Assert.False(string.IsNullOrWhiteSpace(parent.FilePath));
                Assert.Equal(1, parent.SerialNbr);

                var step1 = logs.Single(l => l.ParentId == parent.ID && l.StepNbr == 1);
                Assert.Equal(BaseEnums.SuccessEnums.Success, step1.Success);
                Assert.Equal(parent.RecordCount, step1.RecordCount);

                var step2 = logs.Single(l => l.ParentId == parent.ID && l.StepNbr == 2);
                Assert.Equal(BaseEnums.SuccessEnums.Success, step2.Success);
                Assert.Equal(parent.RecordCount, step2.RecordCount);

                var step3 = logs.Single(l => l.ParentId == parent.ID && l.StepNbr == 3);
                Assert.Equal(BaseEnums.SuccessEnums.Success, step3.Success);
                Assert.Equal(parent.RecordCount, step3.RecordCount);
            }
            finally
            {
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: SV-8a: Khi thiếu trường bắt buộc trong phễu ánh xạ: Cha Fail, con bước 1 Success kèm RecordCount, con bước 2 Fail có ErrorMessage.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenRequiredFieldMissing_LogsParentFail_Step1Success_Step2Fail_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var mappingShape = new Dictionary<string, object>
            {
                ["zoneId"] = new Dictionary<string, object>
                {
                    ["$field"] = "nonExistentFieldRequired",
                    ["$extend"] = new Dictionary<string, object> { ["required"] = true }
                }
            };

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_REQ_{unique}",
                SubCode = $"SUB_REQ_{unique}",
                DatatypeId = "101",
                MappingShape = mappingShape
            });

            try
            {
                // Act
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                // Assert
                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderBy(l => l.OccurredAt)
                    .ToListAsync();

                Assert.Equal(3, logs.Count);

                var parent = logs.Single(l => l.ParentId == null);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);
                Assert.True(parent.RecordCount > 0);

                var step1 = logs.Single(l => l.ParentId == parent.ID && l.StepNbr == 1);
                Assert.Equal(BaseEnums.SuccessEnums.Success, step1.Success);
                Assert.Equal(parent.RecordCount, step1.RecordCount);

                var step2 = logs.Single(l => l.ParentId == parent.ID && l.StepNbr == 2);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, step2.Success);
                Assert.False(string.IsNullOrWhiteSpace(step2.ErrorMessage));

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.RequiredFieldMissing)
                    .ToListAsync();
                Assert.Single(alerts);
            }
            finally
            {
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: SV-8a: Khi HTTP trả về mã 500: Cha Fail, con bước 1 Success, con bước 2 Fail, Cha vẫn giữ nguyên RecordCount.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenHttpFails500_LogsParentFail_Step1Success_Step2Fail_ParentKeepsRecordCount_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;
            partnerServer.DefaultResponseBody = "Mock 500 Internal Error";

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_H500_{unique}",
                SubCode = $"SUB_H500_{unique}",
                DatatypeId = "101"
            });

            try
            {
                // Act
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                // Assert
                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderBy(l => l.OccurredAt)
                    .ToListAsync();

                Assert.Equal(4, logs.Count);

                var parent = logs.Single(l => l.ParentId == null);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);
                Assert.True(parent.RecordCount > 0);

                var step1 = logs.Single(l => l.ParentId == parent.ID && l.StepNbr == 1);
                Assert.Equal(BaseEnums.SuccessEnums.Success, step1.Success);
                Assert.Equal(parent.RecordCount, step1.RecordCount);

                var step2 = logs.Single(l => l.ParentId == parent.ID && l.StepNbr == 2);
                Assert.Equal(BaseEnums.SuccessEnums.Success, step2.Success);
                Assert.Equal(parent.RecordCount, step2.RecordCount);

                var step3 = logs.Single(l => l.ParentId == parent.ID && l.StepNbr == 3);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, step3.Success);
                Assert.False(string.IsNullOrWhiteSpace(step3.ErrorMessage));
                Assert.Contains("500", step3.ErrorMessage);
            }
            finally
            {
                partnerServer.ResetDefaults();
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: SV-8: Khi không có dữ liệu mới, chỉ ghi duy nhất 1 log thông báo NoNewData độc lập (không sinh dòng con).
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenNoNewData_LogsSuccess_SingleLogOnly_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "107");
            var unique = Guid.NewGuid().ToString("N")[..8];

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_NODATA_{unique}",
                SubCode = $"SUB_NODATA_{unique}",
                DatatypeId = "107",
                ConfigureSub = s => s.LastTimeRun = dbNow
            });

            try
            {
                // Act
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                // Assert
                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderBy(l => l.OccurredAt)
                    .ToListAsync();

                Assert.Single(logs);

                var log = logs[0];
                Assert.Null(log.ParentId);
                Assert.Null(log.StepNbr);
                Assert.Equal(BaseEnums.SuccessEnums.Success, log.Success);
                Assert.Equal(0, log.RecordCount);
                Assert.Equal(ShareDataEnum.ExportMessage.NoNewData, log.ErrorMessage);
            }
            finally
            {
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: SV-8a: Khi commit thất bại do mất lock giữa chừng, không sinh dòng con mồ côi.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenCommitFailsDueToLockLost_DoesNotCreateOrphanChildLogs_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "107");
            var unique = Guid.NewGuid().ToString("N")[..8];

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-2);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_LOCK_{unique}",
                SubCode = $"SUB_LOCK_{unique}",
                DatatypeId = "107",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });

            var eventTypeId = $"ET_LOCK_{unique}";
            await db.Insertable(new TmsEventType { ID = eventTypeId, Name = "Incident Lock Test" })
                .InsertColumns(x => new { x.ID, x.Name })
                .ExecuteCommandAsync();

            var incidents = new List<TmsIncident>();
            for (var i = 1; i <= 250; i++)
            {
                var t = baseTime.AddSeconds(i);
                incidents.Add(new TmsIncident
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"INC_LK_{unique}_{i:D4}",
                    Name = $"Incident Lock {i}",
                    EventTypeId = eventTypeId,
                    StartDate = t,
                    CreateTime = t,
                    UpdateTime = t,
                    KmNumber = 50,
                    MetNumber = 0,
                    State = "3"
                });
            }
            await db.Insertable(incidents).ExecuteCommandAsync();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            var requestCount = 0;
            using var stealDb = db.CopyNew();
            partnerServer.Responder = (callIndex, req) =>
            {
                if (req.Headers.TryGetValue("PartnerCode", out var code) && code == partner.Code)
                {
                    requestCount++;
                    if (requestCount == 2)
                    {
                        stealDb.Updateable<ShareDataSubscription>()
                            .SetColumns(s => s.ProcessingUntil == baseTime.AddYears(1))
                            .Where(s => s.ID == sub.ID)
                            .ExecuteCommand();
                    }
                }

                return new PartnerResponsePlan(HttpStatusCode.OK);
            };

            try
            {
                // Act
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                // Assert: Mọi log con đều phải có log cha tồn tại hợp lệ, không có dòng con mồ côi
                var allLogs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .ToListAsync();

                var parentIds = allLogs.Where(l => l.ParentId == null).Select(l => l.ID).ToHashSet();
                var childLogs = allLogs.Where(l => l.ParentId != null).ToList();

                Assert.All(childLogs, c => Assert.Contains(c.ParentId!, parentIds));
            }
            finally
            {
                partnerServer.ResetDefaults();
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: SV-8a: Khi lỗi xảy ra trước bước 1 (PacketNotFound), chỉ ghi đúng 1 dòng cha duy nhất (ParentId null, StepNbr null).
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPacketNotFound_LogsParentOnly_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            _host.PartnerServer.ResetDefaults();

            var unique = Guid.NewGuid().ToString("N")[..8];
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_PNF_{unique}",
                SubCode = $"SUB_PNF_{unique}",
                DatatypeId = "999_khongtontai"
            });

            try
            {
                // Act
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                // Assert
                var logs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .ToListAsync();

                Assert.Single(logs);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, logs[0].Success);
                Assert.Null(logs[0].ParentId);
                Assert.Null(logs[0].StepNbr);
            }
            finally
            {
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: SV-8: Khi xuất dữ liệu nhiều trang, mỗi trang ghi đúng 1 dòng nhật ký độc lập.
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenMultiplePages_LogsDistinctSingleLogsForEachPage_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "107");
            var unique = Guid.NewGuid().ToString("N")[..8];

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(10);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_TREE_{unique}",
                SubCode = $"SUB_TREE_{unique}",
                DatatypeId = "107",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });

            var eventTypeId = $"ET_TREE_{unique}";
            await db.Insertable(new TmsEventType { ID = eventTypeId, Name = "Incident Tree Test" })
                .InsertColumns(x => new { x.ID, x.Name })
                .ExecuteCommandAsync();

            var incidents = new List<TmsIncident>();
            for (var i = 1; i <= 250; i++)
            {
                var t = baseTime.AddSeconds(i);
                incidents.Add(new TmsIncident
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"INC_TR_{unique}_{i:D4}",
                    Name = $"Incident Tree {i}",
                    EventTypeId = eventTypeId,
                    StartDate = t,
                    CreateTime = t,
                    UpdateTime = t,
                    KmNumber = 60,
                    MetNumber = 0,
                    State = "3"
                });
            }
            await db.Insertable(incidents).ExecuteCommandAsync();
            // Ghi đè lại CreateTime và UpdateTime bằng câu lệnh SQL trực tiếp để tránh AOP của SqlSugar tự gán lại DateTime.Now
            await db.Ado.ExecuteCommandAsync(
                "UPDATE TmsIncident SET CreateTime = StartDate, UpdateTime = StartDate WHERE EventTypeId = @eventTypeId",
                new { eventTypeId });

            try
            {
                // Act
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                // Assert: 250 dòng / trang 100 -> 3 trang -> 3 cây * 4 = 12 logs
                var allLogs = await db.Queryable<ShareDataActivityLog>()
                    .Where(l => l.SubscriptionId == sub.ID)
                    .OrderBy(l => l.OccurredAt)
                    .ToListAsync();

                Assert.Equal(12, allLogs.Count);

                var parentLogs = allLogs.Where(l => l.ParentId == null).ToList();
                Assert.Equal(3, parentLogs.Count);
                Assert.All(parentLogs, l =>
                {
                    Assert.Null(l.StepNbr);
                    Assert.Equal(BaseEnums.SuccessEnums.Success, l.Success);
                });
                Assert.Equal([100, 100, 50], parentLogs.Select(p => p.RecordCount).ToArray());

                foreach (var parent in parentLogs)
                {
                    var steps = allLogs.Where(l => l.ParentId == parent.ID).OrderBy(l => l.StepNbr).ToList();
                    Assert.Equal(3, steps.Count);
                    Assert.Equal(1, steps[0].StepNbr);
                    Assert.Equal(2, steps[1].StepNbr);
                    Assert.Equal(3, steps[2].StepNbr);
                    Assert.Equal(BaseEnums.SuccessEnums.Success, steps[0].Success);
                    Assert.Equal(BaseEnums.SuccessEnums.Success, steps[1].Success);
                    Assert.Equal(BaseEnums.SuccessEnums.Success, steps[2].Success);
                    Assert.Equal(parent.RecordCount, steps[0].RecordCount);
                    Assert.Equal(parent.RecordCount, steps[1].RecordCount);
                    Assert.Equal(parent.RecordCount, steps[2].RecordCount);
                }
            }
            finally
            {
                await db.Deleteable<TmsIncident>().Where(i => i.EventTypeId == eventTypeId).ExecuteCommandAsync();
                await db.Deleteable<TmsEventType>().Where(e => e.ID == eventTypeId).ExecuteCommandAsync();
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: SV-8a: Khi xuất dữ liệu nhiều trang, truy vấn phân trang ShareDataPageActivityLogInput chỉ trả về các dòng cha (lưới không phình).
        /// Created date: 02/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenMultiplePages_PageQueryReturnsOnlyParents_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "107");
            var unique = Guid.NewGuid().ToString("N")[..8];

            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(20);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_GRID_{unique}",
                SubCode = $"SUB_GRID_{unique}",
                DatatypeId = "107",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });

            var eventTypeId = $"ET_GRID_{unique}";
            await db.Insertable(new TmsEventType { ID = eventTypeId, Name = "Incident Grid Test" })
                .InsertColumns(x => new { x.ID, x.Name })
                .ExecuteCommandAsync();

            var incidents = new List<TmsIncident>();
            for (var i = 1; i <= 250; i++)
            {
                var t = baseTime.AddSeconds(i);
                incidents.Add(new TmsIncident
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"INC_GD_{unique}_{i:D4}",
                    Name = $"Incident Grid {i}",
                    EventTypeId = eventTypeId,
                    StartDate = t,
                    CreateTime = t,
                    UpdateTime = t,
                    KmNumber = 70,
                    MetNumber = 0,
                    State = "3"
                });
            }
            await db.Insertable(incidents).ExecuteCommandAsync();
            // Ghi đè lại CreateTime và UpdateTime bằng câu lệnh SQL trực tiếp để tránh AOP của SqlSugar tự gán lại DateTime.Now
            await db.Ado.ExecuteCommandAsync(
                "UPDATE TmsIncident SET CreateTime = StartDate, UpdateTime = StartDate WHERE EventTypeId = @eventTypeId",
                new { eventTypeId });

            try
            {
                // Act
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                var pagedResult = await bus.InvokeAsync<SqlSugarPagedList<ShareDataPageActivityLogOutput>>(new ShareDataPageActivityLogInput
                {
                    SubscriptionId = sub.ID,
                    Page = 1,
                    PageSize = 20
                });

                // Assert: Lưới chỉ nhận 3 dòng cha, không nhận 6 dòng con
                Assert.NotNull(pagedResult);
                Assert.Equal(3, pagedResult.Total);
                var records = pagedResult.Records.ToList();
                Assert.Equal(3, records.Count);
                Assert.All(records, r => Assert.Null(r.ParentId));
            }
            finally
            {
                await db.Deleteable<TmsIncident>().Where(i => i.EventTypeId == eventTypeId).ExecuteCommandAsync();
                await db.Deleteable<TmsEventType>().Where(e => e.ID == eventTypeId).ExecuteCommandAsync();
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                    .Where(s => s.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        [Fact]
        public async Task ProcessSubscriptions_WhenLegacyArchiveEnabledIsFalse_IgnoresKeyAndSavesEvidence_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_NOARCH_{unique}",
                SubCode = $"SUB_NOARCH_{unique}",
                DatatypeId = "101",
                ConfigurePartner = p =>
                {
                    p.ConnectionConfig = "{\"ArchiveEnabled\": false}";
                }
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            var parentLog = logs.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Success, parentLog.Success);
            Assert.NotNull(parentLog.FilePath);
            Assert.DoesNotContain("ArchiveEnabled=false", parentLog.Description ?? string.Empty);
            Assert.Contains("Gửi thành công", parentLog.Description ?? string.Empty);
            Assert.Contains("bản ghi cho đối tác", parentLog.Description ?? string.Empty);
        }

        [Fact]
        public async Task ProcessSubscriptions_WhenPartnerConfiguresCsvFormat_SavesEvidenceAsCsv_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_CSV_{unique}",
                SubCode = $"SUB_CSV_{unique}",
                DatatypeId = "101",
                ConfigurePartner = p =>
                {
                    p.ConnectionConfig = "{\"Format\": \"Csv\"}";
                }
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.NotNull(logs[0].FilePath);
            Assert.EndsWith(".csv", logs[0].FilePath);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));

            var content = await File.ReadAllTextAsync(fullPath);
            Assert.Contains("zoneId", content);
            Assert.DoesNotContain("{\"", content);
        }

        [Fact]
        public async Task ProcessSubscriptions_WhenPacketDisabled_SkipsExportSilentlyWithoutAlertLog_Test()
        {
            // 2.2: Gói Status = Disable (như 110/111) có đăng ký -> không trích xuất, không gửi, không ghi ShareDataAlertLog
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetCode = $"110_DIS_{unique}";

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Disabled Packet {unique}",
                Status = BaseEnums.StatusEnum.Disable
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_DIS_{unique}",
                SubCode = $"SUB_DIS_{unique}",
                DatatypeId = packetCode
            });

            var worker = CreateWorker(scope);

            // Act: Chạy 2 chu kỳ xuất bản
            await worker.ProcessSubscriptions(CancellationToken.None);
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert: Không ghi nhận ShareDataAlertLog nào cho subscription / partner
            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID || a.PartnerId == partner.ID)
                .ToListAsync();
            Assert.Empty(alerts);

            var logs = await GetLogs(db, sub.ID);
            Assert.Empty(logs);
        }

        [Fact]
        public async Task ProcessSubscriptions_WhenPacketAndMappingSyncKindNull_DefaultsToSnapshot_Test()
        {
            // 2.2: Câu SQL không có @lastTime/@lastKey -> Snapshot (không ghi ShareDataLastSend)
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            // Câu mẫu 101 hiện là nối đuôi: gán câu 101 kiểu gửi toàn bộ cho đúng kịch bản
            var packet101 = await db.Queryable<ShareDataPacket>().Where(p => p.Code == "101").FirstAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packet101.ID, LegacyPacketSql.Packet101);

            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_SNAP_{unique}",
                SubCode = $"SUB_SNAP_{unique}",
                DatatypeId = "101"
            });

            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == "101")
                .FirstAsync();
            Assert.Null(lastSend);
        }

        [Fact]
        public async Task ProcessSubscriptions_WhenPacketDefaultAndMappingSyncKind2_RunsIncrementalAndUpdatesLastSend_Test()
        {
            // 2.2: Gói 105 có câu SQL dùng @lastTime -> Chạy Incremental, cập nhật mốc ShareDataLastSend
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "105");
            var packet105 = await db.Queryable<ShareDataPacket>().Where(p => p.Code == "105").FirstAsync();
            // Câu cũ dùng TOP (@pageSize); bỏ hằng số nên viết thẳng giới hạn dòng vào câu SQL
            await ShareDataSqlSeed.SetCommonAsync(db, packet105.ID, LegacyPacketSql.Packet109.Replace("TOP (@pageSize)", "TOP 100"));

            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.VehicleIdentification, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_INCR_{unique}",
                SubCode = $"SUB_INCR_{unique}",
                DatatypeId = "105"
            });

            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == "105")
                .FirstAsync();
            Assert.NotNull(lastSend);
        }

        [Fact]
        public async Task ProcessSubscriptions_WhenPacketMissingExtractSql_LogsEsh1305_AndDoesNotAdvanceCursor_Test()
        {
            // Gói không có ExtractSql -> Ghi cảnh báo ESH-1305, không gửi, không tiến mốc LastSend
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetCode = $"103_NO_SQL_{unique}";

            // Tạo gói tin không có ExtractSql
            var packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"No SQL Packet {unique}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable,
            };
            await db.Insertable(packet).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, null);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_NOSQL_{unique}",
                SubCode = $"SUB_NOSQL_{unique}",
                DatatypeId = packetCode
            });

            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Kiểm tra: Bị ghi alert ESH-1305
            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.ExtractSqlMissing)
                .ToListAsync();
            Assert.NotEmpty(alerts);

            // Không tiến mốc LastSend (LastKey vẫn null, không có bản ghi commit)
            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Null(lastSend);

            var subAfter = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(0, subAfter.SerialNbr ?? 0);
        }

        #endregion

        #region 3. Dữ Liệu Khởi Tạo (Seed), Cấu Hình Từ Điển (Dictionary) & Lớp Trợ Giúp (Private Helpers)

        private static readonly Dictionary<ShareDataEnum.DatatypeIdEnum, string[]> ExpectedPacketFields = new()
        {
            [ShareDataEnum.DatatypeIdEnum.TrafficFlow] = ["zoneId", "zoneName", "fromLocationKm", "fromLocationMet", "toLocationKm", "toLocationMet", "laneId", "averageSpeed", "trafficCondition", "dataTime", "speedLimit", "vehicleCount"],
            [ShareDataEnum.DatatypeIdEnum.CctvImage] = ["cameraCode", "cameraName", "snapshot", "snapshotTime", "deviceState", "locationKm", "locationMet", "direction"],
            [ShareDataEnum.DatatypeIdEnum.VehicleDetection] = ["detectionId", "detectTime", "vehicleType", "licensePlate", "speed", "lane", "direction", "locationRoute", "equipmentId", "locationKm", "locationMet"],
            [ShareDataEnum.DatatypeIdEnum.Weather] = ["weatherStationId", "locationDetail", "temperature", "humidity", "windSpeed", "windDirection", "rainfall", "rainfallHour", "visibility", "weatherDescription", "weatherCode", "detectTime"],
            [ShareDataEnum.DatatypeIdEnum.VehicleIdentification] = ["transactionId", "tagId", "licensePlate", "vehicleTypeId", "entryTime", "exitTime", "laneId", "stationId", "vehicleBrand", "vehicleOwner"],
            [ShareDataEnum.DatatypeIdEnum.WeighInMotion] = ["detectTime", "lane", "locationCode", "speed", "height", "width", "length"],
            [ShareDataEnum.DatatypeIdEnum.TrafficIncident] = ["incidentCode", "incidentName", "eventTypeId", "eventTypeName", "occurredTime", "locationKm", "locationMet", "locationRoute", "direction", "injuredCount", "vehicleCount", "incidentState", "description", "source"],
            [ShareDataEnum.DatatypeIdEnum.VmsDisplay] = ["equipmentCode", "vmsName", "locationKm", "locationMet", "direction", "laneId", "displayContent", "displayImageUrl", "displaySize", "priority", "executedTime"],
            [ShareDataEnum.DatatypeIdEnum.TollCollection] = ["transactionId", "entryTime", "exitTime", "vehicleTypeId", "licensePlate", "tagId", "laneId", "laneName", "stationId", "stationName", "tollPrice", "syncTime"],
            [ShareDataEnum.DatatypeIdEnum.PublicMessaging] = ["messageId", "incidentMessage", "weatherMessage", "guidanceContent", "locationKm", "locationMet", "publishedTime"],
            [ShareDataEnum.DatatypeIdEnum.InterCenterExchange] = ["incidentCode", "incidentName", "locationKm", "locationMet", "description"]
        };

        public static IEnumerable<object[]> AllPacketsData =>
            Enum.GetValues<ShareDataEnum.DatatypeIdEnum>()
                .Where(e => (int)e >= 101 && (int)e <= 111)
                .Select(e => new object[] { e });



        private static async Task AssertPacketJsonSchema(
            ISqlSugarClient db,
            string subscriptionId,
            ShareDataEnum.DatatypeIdEnum datatypeEnum,
            string[]? expectedFields = null)
        {
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subscriptionId)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            Assert.NotEmpty(logs);
            Assert.True(logs[0].Success == BaseEnums.SuccessEnums.Success, $"Export failed. DB ErrorMessage: {logs[0].ErrorMessage}");
            Assert.True(logs[0].RecordCount > 0);

            Assert.False(string.IsNullOrEmpty(logs[0].FilePath), "FilePath của ExportLog không được để rỗng");
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath), $"File kết xuất không tồn tại tại đường dẫn: {fullPath}");

            var jsonContent = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            Assert.Equal(JsonValueKind.Array, root.ValueKind);
            Assert.True(root.GetArrayLength() > 0, "JSON payload không được chứa mảng rỗng");

            var firstRecord = root[0];
            Assert.True(firstRecord.EnumerateObject().Any(), $"Gói tin {datatypeEnum} không trả về thuộc tính JSON nào!");

            if (expectedFields != null)
            {
                foreach (var fieldName in expectedFields)
                {
                    Assert.True(
                        firstRecord.TryGetProperty(fieldName, out _),
                        $"Gói tin {datatypeEnum} thiếu field JSON [{fieldName}] trong payload JSON trả về!"
                    );
                }
            }
        }


        private static async Task<(ShareDataPartner Partner, ShareDataSubscription Subscription)> SeedOutboundSubscription(
            ISqlSugarClient db, OutboundSubSeed s)
        {
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = s.PartnerCode,
                Name = $"Partner {s.PartnerCode}",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected,
                Address = ShareDataPartnerServerMock.DefaultHost,
                Port = ShareDataPartnerServerMock.DefaultPort,
                EndPointApiUrl = "/api/sharedata/sharedatainbound"
            };
            s.ConfigurePartner?.Invoke(partner);
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = s.SubCode,
                PartnerId = partner.ID,
                DatatypeId = s.DatatypeId,
                Direction = BaseEnums.Direction.Outbound,
                Mode = BaseEnums.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = dbNow.AddSeconds(-10),
                IntervalSeconds = 30
            };
            s.ConfigureSub?.Invoke(sub);
            await db.Insertable(sub).ExecuteCommandAsync();

            object shapeToUse;
            if (s.MappingShape != null)
            {
                shapeToUse = s.MappingShape;
            }
            else
            {
                var shapeDict = new System.Collections.Generic.Dictionary<string, object>();
                foreach (var key in await GetCommonFieldKeysAsync(db, s.DatatypeId))
                {
                    shapeDict[key] = new System.Collections.Generic.Dictionary<string, string> { { "$field", key } };
                }
                shapeToUse = shapeDict;
            }

            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = s.DatatypeId,
                Direction = BaseEnums.Direction.Outbound,
                IsActive = true,
                TargetShapeJson = System.Text.Json.JsonSerializer.Serialize(shapeToUse)
            }).ExecuteCommandAsync();

            return (partner, sub);
        }

        /// <summary>Bổ sung 5 trường gói 101 (luồng giao thông) còn thiếu vào FieldsJson câu mẫu chung; zoneStatusId, zoneId bắt buộc.</summary>
        private static async Task EnsureTrafficFlowFieldsAsync(ISqlSugarClient db, string? packetId)
        {
            var fields = SqlFieldHelper.Read((await ShareDataSqlSeed.GetCommonAsync(db, packetId))?.FieldsJson);
            foreach (var key in new[] { "zoneStatusId", "zoneId", "averageSpeed", "trafficCondition", "dataTime" })
            {
                if (!fields.Any(f => f.Key == key))
                {
                    fields.Add(new SqlFieldItem { Key = key, Type = SqlFieldType.String, Required = key is "zoneStatusId" or "zoneId" });
                }
            }

            await PacketMetadataCatalogTest.SetCommonFieldsAsync(db, packetId, fields);
        }

        /// <summary>Khoá các trường (chưa mất) trong FieldsJson câu mẫu chung của gói, tìm gói theo mã hoặc ID.</summary>
        private static async Task<List<string>> GetCommonFieldKeysAsync(ISqlSugarClient db, string? datatypeId)
        {
            var packetId = await db.Queryable<ShareDataPacket>()
                .Where(p => p.IsDelete == null && (p.Code == datatypeId || p.ID == datatypeId))
                .Select(p => p.ID)
                .FirstAsync();
            var common = await ShareDataSqlSeed.GetCommonAsync(db, packetId);
            return SqlFieldHelper.Read(common?.FieldsJson).Where(f => !f.Lost && !string.IsNullOrEmpty(f.Key)).Select(f => f.Key).ToList();
        }

        private static async Task SeedTestDataForPacket(ISqlSugarClient db, ShareDataEnum.DatatypeIdEnum datatypeEnum, string uniqueId)
        {
            var now = DateTime.Now;

            switch (datatypeEnum)
            {
                case ShareDataEnum.DatatypeIdEnum.TrafficFlow:
                    var zoneId = $"ZONE_{uniqueId}";
                    await db.Insertable(new TmsZone
                    {
                        ID = zoneId,
                        Name = $"Tuyến Test {uniqueId}",
                        FromKmNumber = 10,
                        FromMetNumber = 500,
                        ToKmNumber = 20,
                        ToMetNumber = 0,
                        LaneId = "LANE_1",
                        MaxSpeed = 80
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsZoneStatus
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        ZoneId = zoneId,
                        AverageSpeed = "65.5",
                        Condition = "NORMAL",
                        UpdateTime = now
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsTrafficStatistic
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        ZoneId = zoneId,
                        TotalVehicleNumber = 150
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.CctvImage:
                    var eqIdCctv = $"EQ_CCTV_{uniqueId}";
                    var ip = $"192.168.1.{Random.Shared.Next(10, 250)}";
                    await db.Insertable(new TmsEquipment
                    {
                        ID = eqIdCctv,
                        Code = $"CAM_EQ_{uniqueId}",
                        Ip = ip,
                        KmNumber = 15,
                        MetNumber = 200,
                        DirectionId = 1
                    }).ExecuteCommandAsync();
                    await db.Insertable(new CctvDevice
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        DeviceId = $"DEV_{uniqueId}",
                        Name = $"Camera Test {uniqueId}",
                        Ip = ip,
                        SnapshotUrl = "data:image/jpeg;base64,sample_snapshot",
                        SnapshotTime = now,
                        DeviceState = 1
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.VehicleDetection:
                case ShareDataEnum.DatatypeIdEnum.WeighInMotion:
                    var eqIdVds = $"EQ_VDS_{uniqueId}";
                    await db.Insertable(new TmsEquipment
                    {
                        ID = eqIdVds,
                        Code = $"VDS_EQ_{uniqueId}",
                        KmNumber = 30,
                        MetNumber = 0
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsTrafficData
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        EquipmentId = eqIdVds,
                        DetectTime = now,
                        Type = "CAR",
                        LicensePlate = "30A-99999",
                        Speed = 75.0f,
                        Lane = "L1",
                        Direction = "NORTH",
                        Location = "KM30",
                        Height = 150,
                        Width = 180,
                        Length = 450
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.Weather:
                    await db.Insertable(new TmsWeather
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        RefId = $"WS_{uniqueId}",
                        LocationDetail = "Trạm Thời Tiết Km45",
                        Temperature = 28.5f,
                        Hudmidity = 75.0f,
                        WindSpeed = 12.0f,
                        WindDirection = "NE",
                        Rain = 0.0f,
                        RainHour = 0.0f,
                        Foresight = 10.0f,
                        Description = "Trời quang",
                        ShortDescription = "CLEAR",
                        TimeDetect = now
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.VehicleIdentification:
                    await db.Insertable(new TollTransactionOut
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        TransactionId = $"TXN_{uniqueId}",
                        TagId = $"TAG_{uniqueId}",
                        PlateEdit = "30A-12345",
                        VehicleTypeId = "1",
                        TransactionDateTimeIn = now.AddMinutes(-30),
                        TransactionDateTime = now,
                        LaneId = "LANE_01",
                        StationId = "STA_01",
                        SyncTime = now
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsVehicleRegistration
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        LicensePlate = "30A-12345",
                        Brand = "Toyota",
                        Owner = "Nguyen Van A"
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.TrafficIncident:
                    var eventTypeId = $"ET_{uniqueId}";
                    await db.Insertable(new TmsEventType
                    {
                        ID = eventTypeId,
                        Name = "Va chạm giao thông"
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsIncident
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        Code = $"INC_{uniqueId}",
                        Name = "Sự cố giao thông",
                        EventTypeId = eventTypeId,
                        StartDate = now.AddHours(-1),
                        KmNumber = 55,
                        MetNumber = 500,
                        Location = "KM55+500",
                        InfluenceScope = "1",
                        InjuredNumber = 0,
                        VehicleNumber = 2,
                        State = "3",
                        Description = "Va chạm nhẹ 2 xe ô tô con",
                        Source = "CCTV",
                        UpdateTime = now
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.VmsDisplay:
                    var eqIdVms = $"EQ_VMS_{uniqueId}";
                    await db.Insertable(new TmsEquipment
                    {
                        ID = eqIdVms,
                        Code = $"VMS_EQ_{uniqueId}",
                        KmNumber = 70,
                        MetNumber = 0,
                        DirectionId = 1,
                        LaneId = "LANE_ALL"
                    }).ExecuteCommandAsync();
                    await db.Insertable(new VmsCurrent
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        EquipmentId = eqIdVms,
                        Name = $"Biển Báo VMS Km70 {uniqueId}",
                        RowData = "CHU Y LAI XE AN TOAN",
                        Url = "http://sample.vms/preview.png",
                        Size = "192x64",
                        Priority = 1,
                        ExecutedDate = now
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.TollCollection:
                    await db.Insertable(new TollTransactionOut
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        TransactionId = $"TOLL_{uniqueId}",
                        TransactionDateTimeIn = now.AddMinutes(-20),
                        TransactionDateTime = now,
                        VehicleTypeId = "1",
                        PlateLpr = "29A-88888",
                        TagId = $"TAG_{uniqueId[..8]}",
                        LaneId = "LANE_ETC_01",
                        StationId = "STA_ETC_01",
                        SyncTime = now
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TollLane
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        LaneId = "LANE_ETC_01",
                        Name = "Làn ETC 01"
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TollStation
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        StationId = "STA_ETC_01",
                        Name = "Trạm Thu Phí Km10"
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.PublicMessaging:
                    var eqIdPm = $"EQ_PM_{uniqueId}";
                    await db.Insertable(new TmsIncident
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        Code = $"INC_PM_{uniqueId}",
                        Name = "Cảnh báo sương mù",
                        StartDate = now,
                        KmNumber = 80,
                        MetNumber = 0,
                        State = "3",
                        Description = "Sương mù dày đặc tầm nhìn giảm",
                        UpdateTime = now
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsEquipment
                    {
                        ID = eqIdPm,
                        Code = $"EQ_PM_{uniqueId}",
                        KmNumber = 80,
                        MetNumber = 0
                    }).ExecuteCommandAsync();
                    await db.Insertable(new VmsCurrent
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        EquipmentId = eqIdPm,
                        RowData = "SUONG MU DAY - GIAM TOC DO",
                        ExecutedDate = now
                    }).ExecuteCommandAsync();
                    await db.Insertable(new TmsWeather
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        Description = "Sương mù tầm nhìn dưới 100m",
                        TimeDetect = now,
                        CreateTime = now
                    }).ExecuteCommandAsync();
                    break;

                case ShareDataEnum.DatatypeIdEnum.InterCenterExchange:
                    await db.Insertable(new TmsIncident
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        Code = $"INC_IC_{uniqueId}",
                        Name = "Sự cố liên trung tâm",
                        KmNumber = 95,
                        MetNumber = 100,
                        Description = "Thông báo điều phối xe cứu hộ",
                        State = "3",
                        StartDate = now,
                        UpdateTime = now
                    }).ExecuteCommandAsync();
                    break;
            }
        }



        private static DataOutboundService CreateWorker(
            IServiceScope scope,
            IHttpClientFactory? httpClientFactory = null,
            IConfiguration? configuration = null,
            DataWorkerConfigService? workerConfig = null)
        {
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();
            var config = configuration ?? scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var clientFactory = httpClientFactory ?? scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var fileConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ExportPathHelper.BasePathConfigKey] = config.GetSection(ExportPathHelper.BasePathConfigKey).Value ?? Path.Combine(Path.GetTempPath(), "sharedata_send")
            }).Build();
            var dispatcher = scope.ServiceProvider.GetService<DeliveryDispatcher>()
                ?? new DeliveryDispatcher(
                    new HttpDeliverySender(clientFactory),
                    new FileDeliverySender(fileConfig, scope.ServiceProvider.GetRequiredService<ILogger<FileDeliverySender>>()),
                    new FtpDeliverySender(),
                    new KafkaDeliverySender());

            return new DataOutboundService(
                scopeFactory,
                logger,
                scope.ServiceProvider.GetRequiredService<DataOutboundExtractionProcess>(),
                new DataOutboundTransport(dispatcher, config),
                workerConfig ?? scope.ServiceProvider.GetRequiredService<DataWorkerConfigService>());
        }

        /// <summary>
        /// Description: Cấu hình NasStorage:BasePath trỏ vào một TỆP đang tồn tại,
        ///              nên Directory.CreateDirectory hoặc File.WriteAllBytes ném IOException — lỗi ghi tệp thật.
        ///              Bên gọi phải xoá BlockerPath trong khối finally.
        /// Created date: 29/09/2026
        /// </summary>
        private static (IConfiguration FailingConfig, string BlockerPath) CreateFailingConfig()
        {
            var blockerPath = Path.Combine(Path.GetTempPath(), $"sharedata-blocked-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(blockerPath, "block");

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["NasStorage:BasePath"] = blockerPath })
                .Build();

            return (config, blockerPath);
        }
        private static async Task<List<ShareDataActivityLog>> GetLogs(ISqlSugarClient db, string subId)
        {
            return await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subId)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();
        }

        private static async Task<string> ReadExportedJson(string relativePath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", relativePath);
            return await File.ReadAllTextAsync(fullPath);
        }

        /// <summary>
        /// Description: Tạo DataWorkerConfigService đọc nhóm cấu hình cô lập với trần số trang mỗi lượt do test đặt,
        ///              không ảnh hưởng cấu hình dùng chung của các bài test khác.
        /// Created date: 20/10/2026
        /// </summary>
        private static async Task<DataWorkerConfigService> CreateWorkerConfigWithMaxPages(IServiceScope scope, ISqlSugarClient db, int maxPagesPerRun)
        {
            var isolatedGroup = $"sd_outbound_pages_{Guid.NewGuid():N}";
            await db.Insertable(new SysOpConfig
            {
                ID = Guid.NewGuid().ToString("N"),
                GroupCode = isolatedGroup,
                Code = ShareDataConst.ConfigKeyOutboundMaxPagesPerRun,
                Value = maxPagesPerRun.ToString(CultureInfo.InvariantCulture),
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup })
                .Build();

            return new DataWorkerConfigService(
                scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<DataWorkerConfigService>.Instance,
                config);
        }

        /// <summary>
        /// Description: Seed N bản ghi TmsTrafficData mốc thời gian tăng dần cho gói 103. Biển số mang tiền tố
        ///              RIÊNG của từng bài để đối chiếu được không trùng / không sót qua các trang mà không
        ///              đụng dữ liệu bài khác — xem mục "Thiết kế cô lập".
        /// Created date: 29/09/2026
        /// </summary>
        private static async Task<List<TmsTrafficData>> SeedTrafficRows(ISqlSugarClient db, string eqId, string platePrefix, DateTime baseTime, int count)
        {
            var rows = new List<TmsTrafficData>();
            for (var i = 1; i <= count; i++)
            {
                var recordTime = baseTime.AddSeconds(i);
                rows.Add(new TmsTrafficData
                {
                    ID = Guid.NewGuid().ToString("N"),
                    DetectTime = recordTime,
                    CreateTime = recordTime,
                    UpdateTime = recordTime,
                    Type = "CAR",
                    LicensePlate = $"{platePrefix}{i:D5}",
                    Speed = 60.0f + (i % 30),
                    Lane = $"L{((i % 3) + 1)}",
                    Direction = "1",
                    Location = "KM45",
                    EquipmentId = eqId
                });
            }

            await db.Insertable(rows).PageSize(100).ExecuteCommandAsync();
            // Ghi đè lại 2 cột mốc bằng câu lệnh SQL trực tiếp để tránh AOP của SqlSugar tự gán lại UpdateTime = DateTime.Now
            await db.Ado.ExecuteCommandAsync(
                "UPDATE TmsTrafficData SET CreateTime = DetectTime, UpdateTime = DetectTime WHERE EquipmentId = @eqId",
                new { eqId });
            return rows;
        }

        /// <summary>
        /// Description: Gom các biển số mang đúng tiền tố của bài test từ những tệp đã xuất của một đăng ký.
        ///              Lọc theo tiền tố để dữ liệu còn sót của bài khác không lọt vào phép đếm.
        /// Created date: 29/09/2026
        /// </summary>
        private static async Task<List<string>> CollectExportedPlates(ISqlSugarClient db, string subId, string platePrefix)
        {
            var plates = new List<string>();
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subId)
                .Where(l => l.ParentId == null)
                .Where(l => l.FilePath != null)
                .OrderBy(l => l.OccurredAt)
                .ToListAsync();

            foreach (var log in logs)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", log.FilePath!);
                if (!File.Exists(fullPath))
                    continue;

                using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(fullPath));
                foreach (var rec in doc.RootElement.EnumerateArray())
                {
                    if (rec.TryGetProperty("licensePlate", out var lp)
                        && lp.GetString() is { Length: > 0 } plate
                        && plate.StartsWith(platePrefix, StringComparison.Ordinal))
                        plates.Add(plate);
                }
            }

            return plates;
        }

        public static class PacketMetadataCatalogTest
        {
            public class PacketDefinition
            {
                public ShareDataPacket Packet { get; set; } = new();
                public List<PacketFieldDto> Fields { get; set; } = [];
            }

            /// <summary>
            /// Trường của gói tin trong bộ dữ liệu test; nạp vào ShareDataSql.FieldsJson của câu mẫu chung
            /// (thay bảng ShareDataPacketField đã bỏ 26/10/2026).
            /// </summary>
            public class PacketFieldDto
            {
                public string FieldKey { get; set; } = string.Empty;
                public string? Column { get; set; }
                public bool? IsKey { get; set; }
                public string? Expression { get; set; }
                public string? Name { get; set; }
                public string? DataType { get; set; }
                public string? Unit { get; set; }
                public string? CodeSetCode { get; set; }
                public bool Required { get; set; }
                public bool NoSource { get; set; }
                public int OrderNo { get; set; }
                public bool? InternalOnly { get; set; }
            }

            private static readonly Dictionary<string, PacketDefinition> Definitions = BuildDefinitions();

            public static IReadOnlyDictionary<string, PacketDefinition> All => Definitions;

            public static PacketDefinition Get(string packetCode)
            {
                if (Definitions.TryGetValue(packetCode, out var def))
                    return def;

                throw new KeyNotFoundException($"Không tìm thấy định nghĩa metadata cho gói tin '{packetCode}'.");
            }

            public static PacketDefinition Get(ShareDataEnum.DatatypeIdEnum datatypeEnum) =>
                Get(((int)datatypeEnum).ToString());

            public static bool TryGet(string? packetCode, out PacketDefinition? definition)
            {
                definition = null;
                if (string.IsNullOrWhiteSpace(packetCode))
                    return false;

                return Definitions.TryGetValue(packetCode.Trim(), out definition);
            }

            /// <summary>
            /// Description: Nạp cấu hình metadata của gói tin và các trường ShareDataPacketField vào CSDL test.
            /// Created date: 16/09/2026
            /// </summary>
            public static async Task SeedPacketToDb(ISqlSugarClient db, string packetCode)
            {
                var def = Get(packetCode);

                var existingPacket = await db.Queryable<ShareDataPacket>()
                    .Where(p => p.Code == def.Packet.Code && p.IsDelete == null)
                    .FirstAsync();

                if (existingPacket == null)
                {
                    var newPacket = new ShareDataPacket
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        Code = def.Packet.Code,
                        Name = def.Packet.Name,
                        PacketVersion = def.Packet.PacketVersion,
                        Status = def.Packet.Status,
                    };
                    await db.Insertable(newPacket).ExecuteCommandAsync();
                    await ShareDataSqlSeed.SetCommonAsync(db, newPacket.ID, PacketTemplateSeed.TryGet(def.Packet.Code!, out var tplNew) ? tplNew.ExtractSql : null);
                }
                else
                {
                    existingPacket.PacketVersion = def.Packet.PacketVersion;
                    existingPacket.Status = def.Packet.Status;
                    await db.Updateable(existingPacket).ExecuteCommandAsync();
                    await ShareDataSqlSeed.SetCommonAsync(db, existingPacket.ID, PacketTemplateSeed.TryGet(def.Packet.Code!, out var tplOld) ? tplOld.ExtractSql : null);
                }

                var packetId = existingPacket?.ID ?? (await db.Queryable<ShareDataPacket>()
                    .Where(p => p.Code == def.Packet.Code && p.IsDelete == null)
                    .Select(p => p.ID)
                    .FirstAsync());
                await SetCommonFieldsAsync(db, packetId, def.Fields
                    .Where(f => f.InternalOnly != true)
                    .OrderBy(f => f.OrderNo)
                    .Select(f => new SqlFieldItem { Key = f.FieldKey, Type = f.DataType ?? SqlFieldType.String, Label = f.Name, Required = f.Required })
                    .ToList());
            }

            /// <summary>Ghi danh sách trường vào FieldsJson của câu mẫu chung của gói (thêm dòng mẫu chung nếu chưa có).</summary>
            public static async Task SetCommonFieldsAsync(ISqlSugarClient db, string? packetId, List<SqlFieldItem> fields)
            {
                if (string.IsNullOrWhiteSpace(packetId))
                {
                    return;
                }

                var common = await ShareDataSqlSeed.GetCommonAsync(db, packetId);
                if (common == null)
                {
                    await ShareDataSqlSeed.SetCommonAsync(db, packetId, null);
                    common = await ShareDataSqlSeed.GetCommonAsync(db, packetId);
                }

                common!.FieldsJson = SqlFieldHelper.Write(fields);
                await db.Updateable(common).ExecuteCommandAsync();
            }

            /// <summary>
            /// Description: Nạp toàn bộ 11 gói tin vào CSDL test.
            /// Created date: 16/09/2026
            /// </summary>
            public static async Task SeedAllPacketsToDb(ISqlSugarClient db)
            {
                foreach (var kvp in Definitions)
                {
                    await SeedPacketToDb(db, kvp.Key);
                }
            }

            private static Dictionary<string, PacketDefinition> BuildDefinitions()
            {
                var map = new Dictionary<string, PacketDefinition>();

                map["101"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_101",
                        Code = "101",
                        Name = "Thông tin chung / luồng giao thông",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Enable
                    },
                    Fields =
                    [
                        new() { FieldKey = "zoneId", Column = "ZoneId", DataType = "string", Required = true, OrderNo = 1 },
                        new() { FieldKey = "zoneName", Column = "Name", DataType = "string", OrderNo = 2 },
                        new() { FieldKey = "fromLocationKm", Column = "FromKmNumber", Unit = "km", DataType = "int", OrderNo = 3 },
                        new() { FieldKey = "fromLocationMet", Column = "FromMetNumber", Unit = "m", DataType = "int", OrderNo = 4 },
                        new() { FieldKey = "toLocationKm", Column = "ToKmNumber", Unit = "km", DataType = "int", OrderNo = 5 },
                        new() { FieldKey = "toLocationMet", Column = "ToMetNumber", Unit = "m", DataType = "int", OrderNo = 6 },
                        new() { FieldKey = "laneId", Column = "LaneId", CodeSetCode = "LANE_DIR", DataType = "string", OrderNo = 7 },
                        new() { FieldKey = "averageSpeed", Column = "AverageSpeed", Expression = "CAST(zs.AverageSpeed AS DECIMAL(18, 2))", DataType = "decimal", Unit = "km/h", Required = true, OrderNo = 8 },
                        new() { FieldKey = "trafficCondition", Column = "Condition", CodeSetCode = "TRAFFIC_COND", DataType = "string", Required = true, OrderNo = 9 },
                        new() { FieldKey = "dataTime", Column = "UpdateTime", DataType = "datetime", Required = true, OrderNo = 10 },
                        new() { FieldKey = "speedLimit", Column = "MaxSpeed", Unit = "km/h", DataType = "int", OrderNo = 11 },
                        new() { FieldKey = "vehicleCount", Column = "TotalVehicleNumber", DataType = "int", OrderNo = 12 }
                    ]
                };

                map["102"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_102",
                        Code = "102",
                        Name = "Dữ liệu hình ảnh giao thông (CCTV)",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Enable
                    },
                    Fields =
                    [
                        new() { FieldKey = "cameraCode", Column = "Code", DataType = "string", OrderNo = 1 },
                        new() { FieldKey = "cameraName", Column = "Name", DataType = "string", OrderNo = 2 },
                        new() { FieldKey = "snapshot", Column = "SnapshotUrl", DataType = "string", OrderNo = 3, NoSource = true },
                        new() { FieldKey = "snapshotTime", Column = "SnapshotTime", DataType = "datetime", OrderNo = 4 },
                        new() { FieldKey = "deviceState", Column = "DeviceState", DataType = "int", OrderNo = 5 },
                        new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 6 },
                        new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 7 },
                        new() { FieldKey = "direction", Column = "DirectionId", DataType = "int", OrderNo = 8 }
                    ]
                };

                map["103"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_103",
                        Code = "103",
                        Name = "Dữ liệu dò xe (VDS)",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Enable
                    },
                    Fields =
                    [
                        new() { FieldKey = "detectionId", Column = "ID", DataType = "string", OrderNo = 1 },
                        new() { FieldKey = "detectTime", Column = "DetectTime", DataType = "datetime", Required = true, OrderNo = 2 },
                        new() { FieldKey = "vehicleType", Column = "Type", DataType = "string", OrderNo = 3 },
                        new() { FieldKey = "licensePlate", Column = "LicensePlate", DataType = "string", OrderNo = 4 },
                        new() { FieldKey = "speed", Column = "Speed", DataType = "decimal", Unit = "km/h", OrderNo = 5 },
                        new() { FieldKey = "lane", Column = "Lane", DataType = "string", OrderNo = 6 },
                        new() { FieldKey = "direction", Column = "Direction", DataType = "string", OrderNo = 7 },
                        new() { FieldKey = "locationRoute", Column = "Location", DataType = "string", OrderNo = 8 },
                        new() { FieldKey = "equipmentId", Column = "EquipmentId", DataType = "string", OrderNo = 9 },
                        new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 10 },
                        new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 11 }
                    ]
                };

                map["104"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_104",
                        Code = "104",
                        Name = "Dữ liệu thời tiết",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Enable
                    },
                    Fields =
                    [
                        new() { FieldKey = "weatherStationId", Column = "RefId", DataType = "string", OrderNo = 1 },
                        new() { FieldKey = "locationDetail", Column = "LocationDetail", DataType = "string", OrderNo = 2 },
                        new() { FieldKey = "temperature", Column = "Temperature", DataType = "decimal", Unit = "°C", OrderNo = 3 },
                        new() { FieldKey = "humidity", Column = "Hudmidity", DataType = "decimal", Unit = "%", OrderNo = 4 },
                        new() { FieldKey = "windSpeed", Column = "WindSpeed", DataType = "decimal", Unit = "m/s", OrderNo = 5 },
                        new() { FieldKey = "windDirection", Column = "WindDirection", DataType = "string", OrderNo = 6 },
                        new() { FieldKey = "rainfall", Column = "Rain", DataType = "decimal", Unit = "mm", OrderNo = 7 },
                        new() { FieldKey = "rainfallHour", Column = "RainHour", DataType = "decimal", Unit = "mm", OrderNo = 8 },
                        new() { FieldKey = "visibility", Column = "Foresight", DataType = "decimal", Unit = "m", OrderNo = 9 },
                        new() { FieldKey = "weatherDescription", Column = "Description", DataType = "string", OrderNo = 10 },
                        new() { FieldKey = "weatherCode", Column = "ShortDescription", DataType = "string", OrderNo = 11 },
                        new() { FieldKey = "detectTime", Column = "TimeDetect", DataType = "datetime", Required = true, OrderNo = 12 }
                    ]
                };

                map["105"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_105",
                        Code = "105",
                        Name = "Dữ liệu định danh phương tiện (AVI/RFID)",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Enable
                    },
                    Fields =
                    [
                        new() { FieldKey = "transactionId", Column = "TransactionId", DataType = "string", OrderNo = 1 },
                        new() { FieldKey = "tagId", Column = "TagId", DataType = "string", OrderNo = 2 },
                        new() { FieldKey = "licensePlate", Expression = "ISNULL(t.PlateEdit, t.PlateLpr)", DataType = "string", OrderNo = 3 },
                        new() { FieldKey = "vehicleTypeId", Column = "VehicleTypeId", DataType = "string", OrderNo = 4 },
                        new() { FieldKey = "entryTime", Column = "TransactionDateTimeIn", DataType = "datetime", OrderNo = 5 },
                        new() { FieldKey = "exitTime", Column = "TransactionDateTime", DataType = "datetime", OrderNo = 6 },
                        new() { FieldKey = "laneId", Column = "LaneId", DataType = "string", OrderNo = 7 },
                        new() { FieldKey = "stationId", Column = "StationId", DataType = "string", OrderNo = 8 },
                        new() { FieldKey = "vehicleBrand", Column = "Brand", DataType = "string", OrderNo = 9 },
                        new() { FieldKey = "vehicleOwner", Column = "Owner", DataType = "string", OrderNo = 10 }
                    ]
                };

                map["106"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_106",
                        Code = "106",
                        Name = "Dữ liệu kiểm tra tải trọng xe (WIM)",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Enable
                    },
                    Fields =
                    [
                        new() { FieldKey = "detectTime", Column = "DetectTime", DataType = "datetime", Required = true, OrderNo = 1 },
                        new() { FieldKey = "lane", Column = "Lane", DataType = "string", OrderNo = 2 },
                        new() { FieldKey = "locationCode", Column = "Location", DataType = "string", OrderNo = 3 },
                        new() { FieldKey = "speed", Column = "Speed", DataType = "decimal", Unit = "km/h", OrderNo = 4 },
                        new() { FieldKey = "height", Column = "Height", DataType = "decimal", Unit = "cm", OrderNo = 5 },
                        new() { FieldKey = "width", Column = "Width", DataType = "decimal", Unit = "cm", OrderNo = 6 },
                        new() { FieldKey = "length", Column = "Length", DataType = "decimal", Unit = "cm", OrderNo = 7 }
                    ]
                };

                map["107"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_107",
                        Code = "107",
                        Name = "Thông tin sự kiện giao thông",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Enable
                    },
                    Fields =
                    [
                        new() { FieldKey = "incidentCode", Column = "Code", DataType = "string", OrderNo = 1 },
                        new() { FieldKey = "incidentName", Column = "Name", DataType = "string", OrderNo = 2 },
                        new() { FieldKey = "eventTypeId", Column = "EventTypeId", DataType = "string", OrderNo = 3 },
                        new() { FieldKey = "eventTypeName", Column = "Name", DataType = "string", OrderNo = 4 },
                        new() { FieldKey = "occurredTime", Column = "StartDate", DataType = "datetime", OrderNo = 5 },
                        new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 6 },
                        new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 7 },
                        new() { FieldKey = "locationRoute", Column = "Location", DataType = "string", OrderNo = 8 },
                        new() { FieldKey = "direction", Column = "InfluenceScope", DataType = "int", OrderNo = 9 },
                        new() { FieldKey = "injuredCount", Column = "InjuredNumber", DataType = "int", OrderNo = 10 },
                        new() { FieldKey = "vehicleCount", Column = "VehicleNumber", DataType = "int", OrderNo = 11 },
                        new() { FieldKey = "incidentState", Column = "State", DataType = "string", OrderNo = 12 },
                        new() { FieldKey = "description", Column = "Description", DataType = "string", OrderNo = 13 },
                        new() { FieldKey = "source", Column = "Source", DataType = "string", OrderNo = 14 }
                    ]
                };

                map["108"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_108",
                        Code = "108",
                        Name = "Thông tin biển báo điện tử (VMS)",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Enable
                    },
                    Fields =
                    [
                        new() { FieldKey = "equipmentCode", Column = "Code", DataType = "string", OrderNo = 1 },
                        new() { FieldKey = "vmsName", Column = "Name", DataType = "string", OrderNo = 2 },
                        new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 3 },
                        new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 4 },
                        new() { FieldKey = "direction", Column = "DirectionId", DataType = "int", OrderNo = 5 },
                        new() { FieldKey = "laneId", Column = "LaneId", DataType = "string", OrderNo = 6 },
                        new() { FieldKey = "displayContent", Column = "RowData", DataType = "string", OrderNo = 7 },
                        new() { FieldKey = "displayImageUrl", Column = "Url", DataType = "string", OrderNo = 8 },
                        new() { FieldKey = "displaySize", Column = "Size", DataType = "string", OrderNo = 9 },
                        new() { FieldKey = "priority", Column = "Priority", DataType = "int", OrderNo = 10 },
                        new() { FieldKey = "executedTime", Column = "ExecutedDate", DataType = "datetime", OrderNo = 11 }
                    ]
                };

                map["109"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_109",
                        Code = "109",
                        Name = "Dữ liệu thu phí (ETC/MTC)",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Enable
                    },
                    Fields =
                    [
                        new() { FieldKey = "transactionId", Column = "TransactionId", DataType = "string", OrderNo = 1 },
                        new() { FieldKey = "entryTime", Column = "TransactionDateTimeIn", DataType = "datetime", OrderNo = 2 },
                        new() { FieldKey = "exitTime", Column = "TransactionDateTime", DataType = "datetime", OrderNo = 3 },
                        new() { FieldKey = "vehicleTypeId", Column = "VehicleTypeId", DataType = "string", OrderNo = 4 },
                        new() { FieldKey = "licensePlate", Expression = "ISNULL(t.PlateEdit, t.PlateLpr)", DataType = "string", OrderNo = 5 },
                        new() { FieldKey = "tagId", Column = "TagId", DataType = "string", OrderNo = 6 },
                        new() { FieldKey = "laneId", Column = "LaneId", DataType = "string", OrderNo = 7 },
                        new() { FieldKey = "laneName", Column = "Name", DataType = "string", OrderNo = 8 },
                        new() { FieldKey = "stationId", Column = "StationId", DataType = "string", OrderNo = 9 },
                        new() { FieldKey = "stationName", Column = "Name", DataType = "string", OrderNo = 10 },
                        new() { FieldKey = "tollPrice", Expression = "CAST(NULL AS DECIMAL(18, 2))", DataType = "decimal", OrderNo = 11 },
                        new() { FieldKey = "syncTime", Column = "SyncTime", DataType = "datetime", OrderNo = 12 }
                    ]
                };

                map["110"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_110",
                        Code = "110",
                        Name = "Trao đổi với người tham gia giao thông",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Disable
                    },
                    Fields =
                    [
                        new() { FieldKey = "messageId", Expression = "CAST(i.ID AS NVARCHAR(64))", DataType = "string", OrderNo = 1 },
                        new() { FieldKey = "incidentMessage", Expression = "CONCAT(ISNULL(i.Name, ''), ' - ', ISNULL(i.Description, ''))", DataType = "string", OrderNo = 2 },
                        new() { FieldKey = "weatherMessage", Column = "Description", DataType = "string", OrderNo = 3 },
                        new() { FieldKey = "guidanceContent", Column = "RowData", DataType = "string", OrderNo = 4 },
                        new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 5 },
                        new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 6 },
                        new() { FieldKey = "publishedTime", Column = "StartDate", DataType = "datetime", OrderNo = 7 },
                        new() { FieldKey = "channel", Expression = "CAST(NULL AS NVARCHAR(64))", DataType = "string", OrderNo = 8 },
                        new() { FieldKey = "deliveryState", Expression = "CAST(NULL AS NVARCHAR(64))", DataType = "string", OrderNo = 9 }
                    ]
                };

                map["111"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_111",
                        Code = "111",
                        Name = "Trao đổi với TT QLĐHGT tuyến",
                        PacketVersion = "1.0",
                        Status = BaseEnums.StatusEnum.Disable
                    },
                    Fields =
                    [
                        new() { FieldKey = "incidentCode", Column = "Code", DataType = "string", OrderNo = 1 },
                        new() { FieldKey = "incidentName", Column = "Name", DataType = "string", OrderNo = 2 },
                        new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 3 },
                        new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 4 },
                        new() { FieldKey = "description", Column = "Description", DataType = "string", OrderNo = 5 }
                    ]
                };

                return map;
            }
        }

        #endregion

        #region Seed & Helper Types

        /// <summary>Tham số seed cho SeedOutboundSubscription — thay thế positional params dài dòng.</summary>
        private sealed record OutboundSubSeed
        {
            public required string PartnerCode                    { get; init; }
            public required string SubCode                        { get; init; }
            public required string DatatypeId                     { get; init; }
            public object?                        MappingShape    { get; init; }
            public Action<ShareDataSubscription>? ConfigureSub    { get; init; }
            public Action<ShareDataPartner>?      ConfigurePartner { get; init; }
        }

        #endregion

        #region Cursor

        #region 9. Mốc nối đuôi (cursor) khi gửi thất bại, retry và không có dữ liệu mới

        /// <summary>
        /// Description: Gói 103 nối đuôi khi gửi HTTP thất bại (đối tác trả HTTP 500 hoặc cổng đóng):
        ///              log cha Fail, cảnh báo ESH-1402, mốc KHÔNG tiến (ShareDataLastSend.LastTime/LastKey,
        ///              Subscription.LastTimeRun/SerialNbr giữ nguyên).
        /// Created date: 12/10/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Theory]
        [InlineData("Http500")]
        [InlineData("ClosedPort")]
        public async Task ProcessSubscriptions_WhenIncrementalHttpFails_LogsEsh1402AndKeepsCursor_Test(string failureMode)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            if (failureMode == "Http500")
            {
                partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;
                partnerServer.DefaultResponseBody = "Mock 500 Internal Error";
            }

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T01_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T01_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T01_{unique}",
                SubCode = $"SUB_T01_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime,
                ConfigurePartner = p =>
                {
                    if (failureMode == "ClosedPort")
                    {
                        p.Port = ShareDataPartnerServerMock.ClosedPort;
                    }
                }
            });
            await SeedTrafficRows(db, eqId, $"T1{unique}-", baseTime, 5);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var logs = await GetLogs(db, sub.ID);
            var parent = logs.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                .ToListAsync();
            Assert.NotEmpty(alerts);

            await AssertCursorUnchanged(db, partner.Code, sub.ID, baseTime);
        }

        /// <summary>
        /// Description: T03 - Retry sau khi đối tác phục hồi: lượt 1 trả 500; lượt 2 (200) gửi lại ĐÚNG các bản ghi của lượt 1;
        ///              lượt 3 không gửi lại gì (không gọi HTTP, log NoNewData).
        ///              Nguồn: ExportPage không commit khi gửi lỗi (dòng 516-535) nên GetLastSend vẫn trả mốc cũ ở lượt kế.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPartnerRecoversAfter500_ResendsSameRecordsOnceAndThenStops_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T03_{unique}";
            var platePrefix = $"T3{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T03_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T03_{unique}",
                SubCode = $"SUB_T03_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            await SeedTrafficRows(db, eqId, platePrefix, baseTime, 5);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var platesRun1 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);

            partnerServer.ResetDefaults();
            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var platesRun2 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);
            var requestsAfterRun2 = partnerServer.TotalReceivedRequests;

            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(5, platesRun1.Count);
            Assert.Equal(platesRun1, platesRun2);
            Assert.Equal(requestsAfterRun2, partnerServer.TotalReceivedRequests);

            var latest = (await GetLogs(db, sub.ID)).Where(l => l.ParentId == null).OrderByDescending(l => l.OccurredAt).First();
            Assert.Equal(BaseEnums.SuccessEnums.Success, latest.Success);
            Assert.Equal(0, latest.RecordCount);
            Assert.Equal(ShareDataEnum.ExportMessage.NoNewData, latest.ErrorMessage);
        }

        /// <summary>
        /// Description: T04 - Gói 103 nối đuôi không có bản ghi mới: không gọi HTTP, đúng 1 log phẳng Success/NoNewData,
        ///              mốc (LastTimeRun, SerialNbr, ShareDataLastSend) không đổi.
        ///              Nguồn: ExportPage dòng 432-447 (RawRows.Count == 0 ở trang 0 chỉ ghi 1 log, không commit).
        ///              Đã có test tương tự cho gói 107 (…WhenNoNewData_LogsSuccess_SingleLogOnly_Test); test này bổ sung gói nối đuôi và mốc.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenIncrementalHasNoNewRows_SendsNothingAndKeepsCursor_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T04_{unique}",
                SubCode = $"SUB_T04_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = dbNow
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);

            var logs = await GetLogs(db, sub.ID);
            var log = Assert.Single(logs);
            Assert.Null(log.ParentId);
            Assert.Equal(BaseEnums.SuccessEnums.Success, log.Success);
            Assert.Equal(0, log.RecordCount);
            Assert.Equal(ShareDataEnum.ExportMessage.NoNewData, log.ErrorMessage);

            await AssertCursorUnchanged(db, partner.Code, sub.ID, dbNow);
        }

        /// <summary>
        /// Description: T05 - Nối đuôi gói 103 khi 150 bản ghi có CÙNG mốc thời gian (chỉ khác ID): trải qua 2 trang
        ///              (100 + 50), mỗi bản ghi xuất đúng 1 lần, không sót ở biên trang.
        ///              Nguồn: QueryPacket103 (DataOutboundExtractionProcess.cs) ORDER BY mốc ASC, ID ASC với điều kiện
        ///              mốc = @lastTime AND ID > @lastKey; DefaultPageSize = 100 là hằng số nên dùng 150 dòng thay cho "page size nhỏ".
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenRowsShareSameWatermark_ExportsEachRowExactlyOnceAcrossPages_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T05_{unique}";
            var platePrefix = $"T5{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T05_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T05_{unique}",
                SubCode = $"SUB_T05_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            await SeedTrafficRows(db, eqId, platePrefix, baseTime, 150);
            var sameTime = baseTime.AddMinutes(1);
            await db.Ado.ExecuteCommandAsync(
                "UPDATE TmsTrafficData SET DetectTime = @t, CreateTime = @t, UpdateTime = @t WHERE EquipmentId = @eqId",
                new { t = sameTime, eqId });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var plates = partnerServer.ReceivedRequests.SelectMany(r => ExtractPlates(r.Body, platePrefix)).ToList();
            Assert.Equal(150, plates.Count);
            Assert.Equal(150, plates.Distinct().Count());

            var pages = (await GetLogs(db, sub.ID)).Where(l => l.ParentId == null && l.RecordCount > 0).ToList();
            Assert.Equal(2, pages.Count);
        }

        /// <summary>
        /// Description: T06 - Nối đuôi gói 103: bản ghi mới hơn về thời gian nhưng ID NHỎ hơn khoá của mốc cũ
        ///              vẫn phải được xuất (điều kiện là OR giữa "mốc lớn hơn" và "mốc bằng AND ID lớn hơn";
        ///              nếu ai đổi OR thành AND thì test này đỏ).
        ///              Nguồn: QueryPacket103 (DataOutboundExtractionProcess.cs).
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenNewerRowHasSmallerIdThanCursorKey_StillExportsIt_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T06_{unique}";
            var platePrefix = $"T6{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T06_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T06_{unique}",
                SubCode = $"SUB_T06_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            var olderId = $"ZZZZ{unique}";
            var newerId = $"0000{unique}";
            var olderRows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 1);
            await db.Ado.ExecuteCommandAsync("UPDATE TmsTrafficData SET ID = @id WHERE ID = @old", new { id = olderId, old = olderRows[0].ID });

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var platesRun1 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);

            var eq2Id = $"EQ_T06B_{unique}";
            await db.Insertable(new TmsEquipment { ID = eq2Id, Code = $"VDS_T06B_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var newerRows = await SeedTrafficRows(db, eq2Id, $"{platePrefix}NEWER-", baseTime.AddMinutes(10), 1);
            await db.Ado.ExecuteCommandAsync("UPDATE TmsTrafficData SET ID = @id WHERE ID = @old", new { id = newerId, old = newerRows[0].ID });
            await MakeSubscriptionDue(db, sub.ID);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Single(platesRun1);
            var platesRun2 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);
            Assert.Equal([newerRows[0].LicensePlate!], platesRun2);
        }

        /// <summary>
        /// Description: T07 - Bản ghi bị UPDATE (UpdateTime tăng) sau khi đã xuất được xuất lại đúng 1 lần
        ///              (lượt 2), lượt 3 không gửi lại.
        ///              Nguồn: mốc dùng COALESCE(UpdateTime, CreateTime, DetectTime) trong QueryPacket103.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenExportedRowIsUpdated_ReExportsItExactlyOnce_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T07_{unique}";
            var platePrefix = $"T7{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T07_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T07_{unique}",
                SubCode = $"SUB_T07_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            var rows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 3);

            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var platesRun1 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);

            await db.Ado.ExecuteCommandAsync(
                "UPDATE TmsTrafficData SET UpdateTime = @t WHERE ID = @id",
                new { t = baseTime.AddMinutes(30), id = rows[0].ID });
            await MakeSubscriptionDue(db, sub.ID);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var platesRun2 = ExtractPlates(partnerServer.LastRequest!.Body, platePrefix);
            var requestsAfterRun2 = partnerServer.TotalReceivedRequests;

            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(3, platesRun1.Count);
            Assert.Equal([rows[0].LicensePlate!], platesRun2);
            Assert.Equal(requestsAfterRun2, partnerServer.TotalReceivedRequests);
        }

        /// <summary>
        /// Description: T08 - Snapshot gói 101 với 150 dòng TmsZoneStatus: payload chứa ĐỦ 150 (câu 101 kiểu gửi toàn bộ, không có TOP);
        ///              lượt kế tiếp dữ liệu không đổi vẫn gửi lại đủ 150 (snapshot không nối đuôi).
        ///              Câu mẫu 101 hiện là nối đuôi nên test gán câu 101 cũ (LegacyPacketSql.Packet101) làm mẫu chung.
        /// Created date: 12/10/2026
        /// Modified date: 29/10/2026 (Câu mẫu 101 chuyển sang nối đuôi: gán câu gửi toàn bộ cho đúng kịch bản snapshot)
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSnapshot101Has150Rows_SendsAllAndResendsNextRun_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var packet101 = await db.Queryable<ShareDataPacket>().Where(p => p.Code == "101").FirstAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packet101.ID, LegacyPacketSql.Packet101);
            var unique = Guid.NewGuid().ToString("N")[..8];
            var zonePrefix = $"ZONE_T08_{unique}_";
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var zones = Enumerable.Range(1, 150).Select(i => new TmsZone
            {
                ID = $"{zonePrefix}{i:D3}",
                Name = $"Tuyến T08 {i}",
                FromKmNumber = 10,
                FromMetNumber = 500,
                ToKmNumber = 20,
                ToMetNumber = 0,
                LaneId = "LANE_1",
                MaxSpeed = 80
            }).ToList();
            await db.Insertable(zones).PageSize(100).ExecuteCommandAsync();
            var statuses = zones.Select(z => new TmsZoneStatus
            {
                ID = Guid.NewGuid().ToString("N"),
                ZoneId = z.ID,
                AverageSpeed = "65.5",
                Condition = "NORMAL",
                UpdateTime = dbNow
            }).ToList();
            await db.Insertable(statuses).PageSize(100).ExecuteCommandAsync();
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T08_{unique}",
                SubCode = $"SUB_T08_{unique}",
                DatatypeId = "101"
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var zonesRun1 = ExtractZones(partnerServer.LastRequest!.Body, zonePrefix);

            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var zonesRun2 = ExtractZones(partnerServer.LastRequest!.Body, zonePrefix);

            // Assert
            Assert.Equal(150, zonesRun1.Count);
            Assert.Equal(150, zonesRun2.Count);
        }

        /// <summary>
        /// Description: T10 - Gói 110 (policy NotReady): không gọi HTTP, không ném lỗi, kể cả khi có 1 TmsIncident State='1'.
        ///              Hành vi thực tế (DataOutboundService.cs GetExportConfig dòng 292-305): chỉ ghi logger Information,
        ///              KHÔNG ghi ShareDataActivityLog/ShareDataAlertLog nào — khác kỳ vọng "có cảnh báo NotReady" trong prompt
        ///              (cảnh báo NotReady chỉ có ở luồng Tracking, ShareDataAlertCode.Tracking.NotReady). Cần BA chốt.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPacket110IsNotReady_SendsNothingAndDoesNotThrow_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "110");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T10_{unique}",
                SubCode = $"SUB_T10_{unique}",
                DatatypeId = "110"
            });
            await db.Insertable(new TmsIncident { ID = Guid.NewGuid().ToString("N"), State = "1" }).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ToListAsync();
            Assert.Empty(alerts);
        }

        /// <summary>
        /// Description: T11 - Không có mapping chiều gửi đang bật: ghi ESH-1304, log cha Fail, không gọi HTTP.
        ///              Nguồn: DataOutboundService.cs GetExportConfig dòng 321-329 (ném ShareDataException MappingNotFound).
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenNoActiveMapping_LogsEsh1304AndSendsNothing_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T11_{unique}",
                SubCode = $"SUB_T11_{unique}",
                DatatypeId = "103"
            });
            await db.Updateable<ShareDataMapping>()
                .SetColumns(m => m.IsActive == false)
                .Where(m => m.PartnerId == partner.ID)
                .ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.MappingNotFound)
                .ToListAsync();
            Assert.NotEmpty(alerts);

            var parent = (await GetLogs(db, sub.ID)).Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);
        }

        /// <summary>
        /// Description: T14 - Header HTTP khi chạy qua pipeline: PartnerCode, PacketCode, SerialNbr và Content-Type
        ///              "application/json; charset=utf-8" đúng giá trị; SerialNbr giữ nguyên sau khi gửi thất bại
        ///              (lượt retry gửi lại cùng giá trị) và tăng thêm 1 sau khi gửi thành công.
        ///              Nguồn: DataOutboundRestSender.AddIdentityHeaders (dòng 93-105); ExportPage tăng SerialNbr sau CommitSuccess (dòng 583).
        ///              Test HttpSender_Send_WithIdentityHeaders… đã phủ riêng bộ gửi; test này phủ toàn pipeline.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_SerialNbrHeaderStaysOnFailureAndAdvancesOnSuccess_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T14_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T14_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T14_{unique}",
                SubCode = $"SUB_T14_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => { s.LastTimeRun = baseTime; s.SerialNbr = 5; }
            });
            await SeedTrafficRows(db, eqId, $"T14{unique}-", baseTime, 2);
            var expectedPacketCode = PacketMetadataCatalogTest.Get("103").Packet.Code;

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var failedRequest = partnerServer.LastRequest!;
            var serialAfterFailure = (await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID)).SerialNbr;

            partnerServer.ResetDefaults();
            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            var retryRequest = partnerServer.LastRequest!;
            var serialAfterSuccess = (await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID)).SerialNbr;

            // Assert
            Assert.Equal(partner.Code, failedRequest.Headers["PartnerCode"]);
            Assert.Equal(expectedPacketCode, failedRequest.Headers["PacketCode"]);
            Assert.Equal("5", failedRequest.Headers["SerialNbr"]);
            Assert.Equal("application/json; charset=utf-8", failedRequest.ContentType);
            Assert.Equal(5, serialAfterFailure);

            Assert.Equal("5", retryRequest.Headers["SerialNbr"]);
            Assert.Equal(6, serialAfterSuccess);
        }

        /// <summary>
        /// Description: T09 - Snapshot gói 105 với 120 dòng TollTransactionIn: payload chứa đủ 120 dòng
        ///              (theo quyết định 14/10/2026: các gói Snapshot gửi full, bỏ trần TOP cứng).
        ///              Nguồn: DataOutboundExtractionProcess.cs QueryPacket105 (không giới hạn TOP).
        /// Created date: 12/10/2026
        /// Modified date: 14/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSnapshot105Has120Rows_SendsAllWithoutTopCeiling_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "105");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var txnPrefix = $"TXN_T09_{unique}_";
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var futureBase = dbNow.AddDays(30);
            var rows = Enumerable.Range(1, 120).Select(i => new TollTransactionIn
            {
                ID = Guid.NewGuid().ToString("N"),
                TransactionId = $"{txnPrefix}{i:D3}",
                TagId = $"TAG_T09_{unique}_{i}",
                PlateEdit = $"T09-{i:D3}",
                VehicleTypeId = "1",
                TransactionDateTime = futureBase.AddSeconds(i),
                LaneId = "LANE_01",
                StationId = "STA_01"
            }).ToList();
            await db.Insertable(rows).PageSize(100).ExecuteCommandAsync();
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T09_{unique}",
                SubCode = $"SUB_T09_{unique}",
                DatatypeId = "105"
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            using var doc = JsonDocument.Parse(partnerServer.LastRequest!.Body);
            var sentIds = doc.RootElement.EnumerateArray()
                .Select(r => r.TryGetProperty("transactionId", out var t) ? t.GetString() : null)
                .Where(t => t != null && t.StartsWith(txnPrefix, StringComparison.Ordinal))
                .ToList();
            Assert.Equal(120, sentIds.Count);
            Assert.Equal(120, sentIds.Distinct().Count());
        }

        /// <summary>
        /// Description: T15 - Đối tác Disable hoặc Disconnected thì đăng ký không được chọn: không HTTP, không log, mốc không đổi.
        ///              Nguồn: DataOutboundService.cs chọn sub theo Status == Enable và SessionState == Connected.
        /// Created date: 12/10/2026
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ProcessSubscriptions_WhenPartnerDisabledOrDisconnected_SkipsSubscription_Test(bool disabled)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            _host.PartnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_T15_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_T15_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_T15_{unique}",
                SubCode = $"SUB_T15_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime,
                ConfigurePartner = p =>
                {
                    if (disabled)
                        p.Status = BaseEnums.StatusEnum.Disable;
                    else
                        p.SessionState = BaseEnums.SessionState.Disconnected;
                }
            });
            await SeedTrafficRows(db, eqId, $"T15{unique}-", baseTime, 3);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);
            Assert.Empty(await GetLogs(db, sub.ID));
            var subAfter = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(baseTime, subAfter.LastTimeRun!.Value, TimeSpan.FromSeconds(1));
            Assert.Equal(0, subAfter.SerialNbr ?? 0);
            Assert.False(await db.Queryable<ShareDataLastSend>().AnyAsync(c => c.PartnerCode == partner.Code));
        }

        private static List<string> ExtractZones(string body, string zonePrefix)
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.EnumerateArray()
                .Select(r => r.TryGetProperty("zoneId", out var z) ? z.GetString() : null)
                .Where(z => z != null && z.StartsWith(zonePrefix, StringComparison.Ordinal))
                .Select(z => z!)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Description: Khẳng định mốc nối đuôi chưa tiến: Subscription.LastTimeRun và ShareDataLastSend.LastTime
        ///              bằng mốc khởi tạo, LastKey rỗng, SerialNbr chưa tăng.
        /// Created date: 12/10/2026
        /// </summary>
        private static async Task AssertCursorUnchanged(ISqlSugarClient db, string? partnerCode, string subId, DateTime expectedTime)
        {
            var sub = await db.Queryable<ShareDataSubscription>().InSingleAsync(subId);
            Assert.NotNull(sub.LastTimeRun);
            Assert.Equal(expectedTime, sub.LastTimeRun!.Value, TimeSpan.FromSeconds(1));
            Assert.True((sub.SerialNbr ?? 0) == 0, $"SerialNbr={sub.SerialNbr}");

            var lastSend = await db.Queryable<ShareDataLastSend>().Where(c => c.PartnerCode == partnerCode).FirstAsync();
            Assert.NotNull(lastSend);
            Assert.Equal(expectedTime, lastSend.LastTime!.Value, TimeSpan.FromSeconds(1));
            Assert.Null(lastSend.LastKey);
        }

        /// <summary>
        /// Description: Đưa Subscription về trạng thái Active và đến hạn chạy để worker nhận ở lượt kế tiếp.
        /// Created date: 12/10/2026
        /// </summary>
        private static async Task MakeSubscriptionDue(ISqlSugarClient db, string subId)
        {
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .SetColumns(s => s.NextTimeRun == dbNow.AddSeconds(-10))
                .Where(s => s.ID == subId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Lấy danh sách biển số mang đúng tiền tố từ body JSON gửi tới đối tác.
        /// Created date: 12/10/2026
        /// </summary>
        private static List<string> ExtractPlates(string body, string platePrefix)
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.EnumerateArray()
                .Select(r => r.TryGetProperty("licensePlate", out var lp) ? lp.GetString() : null)
                .Where(p => p != null && p.StartsWith(platePrefix, StringComparison.Ordinal))
                .Select(p => p!)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Description: F3 - Gói Single khi gửi thất bại (HTTP 500) không được bị chuyển sang Expired
        ///              mà phải giữ Active và có NextTimeRun để thử lại ở chu kỳ tiếp theo.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSingleModeHttpFails500_ShouldNotBecomeExpired_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;
            partnerServer.DefaultResponseBody = "Mock 500 Error";

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_F3_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_F3_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_F3_{unique}",
                SubCode = $"SUB_F3_{unique}",
                DatatypeId = "103",
                ConfigureSub = s =>
                {
                    s.Mode = BaseEnums.SubMode.Single;
                    s.LastTimeRun = baseTime;
                }
            });
            await SeedTrafficRows(db, eqId, $"F3{unique}-", baseTime, 5);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var reloadedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.NotNull(reloadedSub);
            // KỲ VỌNG: Gửi lỗi thì KHÔNG ĐƯỢC Expired! Phải giữ Active để retry!
            Assert.Equal(BaseEnums.SubSubscriptionState.Active, reloadedSub.State);
            Assert.NotNull(reloadedSub.NextTimeRun);
        }

        /// <summary>
        /// Description: F4 - Kịch bản At-least-once: Khi gửi REST thành công nhưng mất lock lúc CommitSuccess
        ///              (do timeout/OCC trượt), dòng log cha báo Fail, cursor KHÔNG tiến, SerialNbr KHÔNG tăng.
        ///              Ở lượt quét tiếp theo, Worker tự động gửi lại đúng trang đó với cùng SerialNbr để đối tác nhận diện idempotent.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenLockLostAtCommit_KeepsCursorAndResendsWithSameSerialNbr_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_F4_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_F4_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_F4_{unique}",
                SubCode = $"SUB_F4_{unique}",
                DatatypeId = "103",
                ConfigureSub = s =>
                {
                    s.LastTimeRun = baseTime;
                    s.SerialNbr = 10;
                }
            });
            await SeedTrafficRows(db, eqId, $"F4{unique}-", baseTime, 5);

            using var stealDb = db.CopyNew();
            var callIndex = 0;
            partnerServer.Responder = (call, req) =>
            {
                callIndex++;
                if (callIndex == 1)
                {
                    // Lượt 1: Phá ProcessingUntil trong DB để CommitSuccess bị trượt OCC
                    stealDb.Updateable<ShareDataSubscription>()
                        .SetColumns(s => s.ProcessingUntil == DateTime.Now.AddDays(-1))
                        .Where(s => s.ID == sub.ID)
                        .ExecuteCommand();
                }
                return new PartnerResponsePlan(HttpStatusCode.OK, "{\"success\":true}");
            };

            // Act 1: Lượt đầu tiên chạy -> Gửi REST OK nhưng CommitSuccess thất bại do mất lock
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 1:
            // 1. Đối tác đã nhận 1 request
            Assert.Equal(1, partnerServer.TotalReceivedRequests);
            var req1 = partnerServer.ReceivedRequests[0];

            // 2. Log cha ghi nhận Fail (mất lock khi commit)
            var logs = await GetLogs(db, sub.ID);
            var parent = logs.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);
            Assert.Contains("Mất lock", parent.ErrorMessage ?? string.Empty);

            // 3. Mốc cursor KHÔNG tiến, SerialNbr KHÔNG tăng trong DB
            var subAfterFail = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(10, subAfterFail.SerialNbr);
            Assert.Equal(baseTime, subAfterFail.LastTimeRun!.Value, TimeSpan.FromSeconds(1));

            // Act 2: Cho Subscription đến hạn chạy lại ở lượt quét kế tiếp
            await MakeSubscriptionDue(db, sub.ID);
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 2:
            // 4. Đối tác nhận thêm request thứ 2 (resend)
            Assert.Equal(2, partnerServer.TotalReceivedRequests);
            var req2 = partnerServer.ReceivedRequests[1];

            // 5. Cả 2 request gửi sang đều có CÙNG SerialNbr header = 10 (chứng minh tính Idempotent)
            var serial1 = req1.Headers.TryGetValue("SerialNbr", out var s1) ? s1 : null;
            var serial2 = req2.Headers.TryGetValue("SerialNbr", out var s2) ? s2 : null;
            Assert.NotNull(serial1);
            Assert.Equal("10", serial1);
            Assert.Equal(serial1, serial2);

            // 6. Dữ liệu gửi đi ở 2 lượt giống hệt nhau
            var plates1 = ExtractPlates(req1.Body, $"F4{unique}-");
            var plates2 = ExtractPlates(req2.Body, $"F4{unique}-");
            Assert.Equal(5, plates1.Count);
            Assert.Equal(plates1, plates2);

            // 7. Ở lượt 2 thành công, Cursor đã tiến lên và SerialNbr trong DB đã tăng từ 10 lên 11
            var subAfterSuccess = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(11, subAfterSuccess.SerialNbr);
            Assert.True(subAfterSuccess.LastTimeRun > baseTime);
        }

        /// <summary>
        /// Description: F5 - Kiểm tra hành vi hiện tại: Khi gửi HTTP fail 500, fileSender vẫn chạy và ghi tệp ra đĩa
        ///              nhưng toàn bộ lượt gửi bị coi là thất bại, cursor không tiến.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenHttpFails500_FileSenderStillWritesFile_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();
            partnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;
            partnerServer.DefaultResponseBody = "Mock 500 Error";

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_F5_{unique}";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_F5_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_F5_{unique}",
                SubCode = $"SUB_F5_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => s.LastTimeRun = baseTime
            });
            await SeedTrafficRows(db, eqId, $"F5{unique}-", baseTime, 5);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            // 1. HTTP gửi thất bại -> Log cha là Fail
            var logs = await GetLogs(db, sub.ID);
            var parent = logs.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parent.Success);

            // 2. Cursor không tiến
            await AssertCursorUnchanged(db, partner.Code, sub.ID, baseTime);

            // 3. NHƯNG File vẫn bị ghi ra đĩa dù HTTP thất bại! (Hành vi F5)
            Assert.False(string.IsNullOrWhiteSpace(parent.FilePath), "FilePath trong log không được rỗng");
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", parent.FilePath!);
            Assert.True(File.Exists(fullPath), $"File [{fullPath}] vẫn bị ghi ra đĩa dù HTTP fail");
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        /// <summary>
        /// Description: TC2 - Sửa cấu hình đối tác giữa 2 kỳ chạy thì worker tự gửi bù đúng các bản ghi cũ và tiến cursor.
        ///              - Kỳ 1: Gửi HTTP lỗi (ClosedPort hoặc AbortOnCall). Log cha ghi nhận Fail, cảnh báo HttpSendFailed,
        ///                ErrorMessage chứa URL đã gọi, file vẫn được lưu đệm (bằng chứng), LastTimeRun / SerialNbr / ShareDataLastSend không đổi.
        ///              - Giữa 2 kỳ: Sửa cấu hình đối tác (Port/phục hồi mạng), ResetDefaults và kích hoạt kỳ chạy tiếp theo (MakeSubscriptionDue).
        ///              - Kỳ 2: Worker tự gửi bù đúng 5 bản ghi cũ, mock nhận đủ 5 biển số, header PartnerCode / PacketCode đúng,
        ///                header SerialNbr bằng giá trị trước kỳ 1 (không tăng khi lỗi). Sau kỳ 2: LastTimeRun tiến lên, SerialNbr tăng 1, log bước gửi thành công.
        /// Created date: 15/10/2026
        /// </summary>
        [Theory]
        [InlineData("ClosedPort")]
        [InlineData("AbortOnCall")]
        public async Task ProcessSubscriptions_WhenPartnerConnectionFixedBetweenRuns_ResendsSameRecordsAndAdvancesCursor_Test(string failureMode)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            if (failureMode == "AbortOnCall")
            {
                partnerServer.AbortOnCall.Add(1);
            }

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "103");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var eqId = $"EQ_TC2_{unique}";
            var platePrefix = $"TC2{unique}-";
            await db.Insertable(new TmsEquipment { ID = eqId, Code = $"VDS_TC2_{unique}", KmNumber = 45, MetNumber = 100 }).ExecuteCommandAsync();
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.AddDays(-35);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_TC2_{unique}",
                SubCode = $"SUB_TC2_{unique}",
                DatatypeId = "103",
                ConfigureSub = s => { s.LastTimeRun = baseTime; s.SerialNbr = 0; },
                ConfigurePartner = p =>
                {
                    if (failureMode == "ClosedPort")
                    {
                        p.Port = ShareDataPartnerServerMock.ClosedPort;
                    }
                }
            });

            var seededRows = await SeedTrafficRows(db, eqId, platePrefix, baseTime, 5);
            var expectedPlates = seededRows.Select(r => r.LicensePlate!).OrderBy(p => p, StringComparer.Ordinal).ToList();
            var expectedPacketCode = PacketMetadataCatalogTest.Get("103").Packet.Code;

            var subBefore = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            var initialLastTime = subBefore.LastTimeRun;
            var initialSerial = subBefore.SerialNbr ?? 0;

            // Act 1: Kỳ 1 chạy khi kết nối đối tác lỗi
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 1:
            var logsRun1 = await GetLogs(db, sub.ID);
            var parentRun1 = logsRun1.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, parentRun1.Success);

            // Cảnh báo HttpSendFailed
            var alertsRun1 = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                .ToListAsync();
            Assert.NotEmpty(alertsRun1);

            // ErrorMessage chứa URL đã gọi
            var expectedUrlPort = failureMode == "ClosedPort" ? ShareDataPartnerServerMock.ClosedPort : ShareDataPartnerServerMock.DefaultPort;
            Assert.Contains(expectedUrlPort.ToString(), parentRun1.ErrorMessage);

            // File vẫn được lưu đệm/xuất (log có FilePath)
            Assert.False(string.IsNullOrWhiteSpace(parentRun1.FilePath));
            var fullPathRun1 = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", parentRun1.FilePath!);
            Assert.True(File.Exists(fullPathRun1), $"File [{fullPathRun1}] phải được lưu đệm trước khi gửi");
            if (File.Exists(fullPathRun1)) File.Delete(fullPathRun1);

            // LastTimeRun, SerialNbr, ShareDataLastSend KHÔNG đổi
            await AssertCursorUnchanged(db, partner.Code, sub.ID, baseTime);

            // 3. Mô phỏng sửa cấu hình đối tác giữa 2 kỳ chạy
            if (failureMode == "ClosedPort")
            {
                await db.Updateable<ShareDataPartner>()
                    .SetColumns(p => p.Port == ShareDataPartnerServerMock.DefaultPort)
                    .Where(p => p.ID == partner.ID)
                    .ExecuteCommandAsync();
            }
            partnerServer.ResetDefaults();
            await MakeSubscriptionDue(db, sub.ID);

            // Act 2: Kỳ 2 chạy sau khi sửa cấu hình
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert 2:
            // Mock nhận đúng 5 biển số đã seed
            var receivedPlates = partnerServer.ReceivedRequests.SelectMany(r => ExtractPlates(r.Body, platePrefix)).ToList();
            Assert.Equal(5, receivedPlates.Count);
            Assert.Equal(expectedPlates, receivedPlates);

            // Headers đúng: PartnerCode, PacketCode, SerialNbr bằng giá trị trước kỳ 1
            var successfulRequest = partnerServer.LastRequest!;
            Assert.Equal(partner.Code, successfulRequest.Headers["PartnerCode"]);
            Assert.Equal(expectedPacketCode, successfulRequest.Headers["PacketCode"]);
            Assert.Equal(initialSerial.ToString(), successfulRequest.Headers["SerialNbr"]);

            // Sau kỳ 2: LastTimeRun tiến lên, SerialNbr tăng 1, log gửi thành công
            var subAfterRun2 = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.True(subAfterRun2.LastTimeRun > initialLastTime, "LastTimeRun phải tiến lên sau kỳ gửi thành công");
            Assert.Equal(initialSerial + 1, subAfterRun2.SerialNbr);

            var logsRun2 = await GetLogs(db, sub.ID);
            var parentRun2 = logsRun2.Where(l => l.ParentId == null).OrderByDescending(l => l.OccurredAt).First();
            Assert.Equal(BaseEnums.SuccessEnums.Success, parentRun2.Success);
            Assert.Equal(5, parentRun2.RecordCount);

            if (!string.IsNullOrWhiteSpace(parentRun2.FilePath))
            {
                var fullPathRun2 = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", parentRun2.FilePath!);
                if (File.Exists(fullPathRun2)) File.Delete(fullPathRun2);
            }
        }

        #endregion

        #endregion

        #region SelfIdentity

        #region 10. Đa tiến trình & Đa đối tác (SV-4 Concurrency & SV-5 Multi-Partner)

        /// <summary>
        /// Description: SV-4 · Khi gửi dữ liệu cho đối tác, header HTTP PartnerCode, PacketCode,
        ///              SerialNbr được truyền chính xác theo cấu hình của đối tác và lượt gửi.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        [Trait("Feature", "SV-4")]
        public async Task ProcessSubscriptions_WhenPartnerCodeIsPresent_SendsPartnerCodeHeader_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"DEST_P_{unique}",
                SubCode = $"SUB_SV4_{unique}",
                DatatypeId = "101",
                ConfigureSub = s => s.SerialNbr = 1
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.NotNull(partnerServer.LastRequest);
            var lastReq = partnerServer.LastRequest;

            Assert.True(lastReq.Headers.ContainsKey("PartnerCode"), "Request phải có header PartnerCode");
            Assert.Equal(partner.Code, lastReq.Headers["PartnerCode"]);
            Assert.Equal("101", lastReq.Headers["PacketCode"]);
            Assert.Equal((sub.SerialNbr ?? 1).ToString(), lastReq.Headers["SerialNbr"]);

            var logs = await GetLogs(db, sub.ID);
            var parent = logs.Single(l => l.ParentId == null);
            Assert.Equal(BaseEnums.SuccessEnums.Success, parent.Success);
        }

        /// <summary>
        /// Description: SV-4 · Chứng minh khoá phân tán CSDL (ProcessingUntil): 4 worker tiến trình cùng quét
        ///              đồng thời một subscription, chỉ duy nhất 1 worker giành được quyền gửi và không gửi đúp (0 duplicate).
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        [Trait("Feature", "SV-4")]
        public async Task ProcessSubscriptions_MultiWorkerConcurrency_FourWorkersCompeteForSameSubscription_OnlyOneProcesses_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"CONC_P_{unique}",
                SubCode = $"SUB_CONC_{unique}",
                DatatypeId = "101"
            });

            var worker1 = CreateWorker(scope);
            var worker2 = CreateWorker(scope);
            var worker3 = CreateWorker(scope);
            var worker4 = CreateWorker(scope);

            // Act: 4 worker cùng kích hoạt tranh chấp đồng thời một subscription
            await Task.WhenAll(
                worker1.ProcessSubscriptions(CancellationToken.None),
                worker2.ProcessSubscriptions(CancellationToken.None),
                worker3.ProcessSubscriptions(CancellationToken.None),
                worker4.ProcessSubscriptions(CancellationToken.None));

            // Assert: Chỉ duy nhất 1 worker được gửi, phía đối tác chỉ nhận đúng 1 request, không bị trùng lặp
            Assert.Equal(1, partnerServer.TotalReceivedRequests);

            var logs = await GetLogs(db, sub.ID);
            var parentLogs = logs.Where(l => l.ParentId == null).ToList();
            Assert.Single(parentLogs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, parentLogs[0].Success);
        }

        /// <summary>
        /// Description: SV-5 · Kịch bản test đa đối tác: 4 node clone (A101..A104) cùng gửi về một Receiver,
        ///              mỗi node gửi kèm header PartnerCode định danh chính mình, dữ liệu hoàn toàn độc lập.
        /// Created date: 12/10/2026
        /// </summary>
        [Fact]
        [Trait("Feature", "SV-4")]
        [Trait("Feature", "SV-5")]
        public async Task ProcessSubscriptions_FourCloneNodes_SendWithDistinctPartnerCodes_SV5_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var expectedCloneCodes = new[] { "A101", "A102", "A103", "A104" };
            var subs = new List<ShareDataSubscription>();
            for (var i = 0; i < 4; i++)
            {
                var (_, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
                {
                    PartnerCode = expectedCloneCodes[i],
                    SubCode = $"SUB_SV5_{i}_{unique}",
                    DatatypeId = "101",
                    ConfigureSub = s => s.NextTimeRun = DateTime.MaxValue
                });
                subs.Add(sub);
            }

            // Act: Lần lượt từng clone node xử lý subscription của mình
            for (var i = 0; i < 4; i++)
            {
                await MakeSubscriptionDue(db, subs[i].ID);
                await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);
            }

            // Assert: Phía server nhận nhận đủ 4 request độc lập với đúng header PartnerCode tương ứng
            Assert.Equal(4, partnerServer.TotalReceivedRequests);

            for (var i = 0; i < 4; i++)
            {
                var req = partnerServer.ReceivedRequests[i];
                Assert.True(req.Headers.ContainsKey("PartnerCode"), $"Request {i + 1} phải có header PartnerCode");
                Assert.Equal(expectedCloneCodes[i], req.Headers["PartnerCode"]);

                var logs = await GetLogs(db, subs[i].ID);
                var parent = logs.Single(l => l.ParentId == null);
                Assert.Equal(BaseEnums.SuccessEnums.Success, parent.Success);
            }
        }

        #endregion

        #endregion

        #region SqlVariables

        #region 10. Biến ở bảng tham số câu truy vấn (ShareDataSql.VariablesJson) khi Worker gửi

        /// <summary>
        /// Câu SQL dùng đủ 6 nguồn "Lấy từ" (mã đối tác, thời điểm chạy, đầu năm, lần chạy trước, mốc thời gian / mốc khóa lần trước)
        /// và hằng số "Giá trị nhập" (String, Int, Decimal).
        /// Không dùng @lastTime/@lastKey: chế độ nối đuôi nhận ra qua @since/@afterKey. Mã gói và mã đối tác viết thẳng trong câu
        /// nên @pc chỉ khớp khi lấy đúng từ nguồn "Mã đối tác".
        /// </summary>
        private static string AllSourcesSql(string packetId, string partnerCode) =>
            "SELECT TOP (@pageSize) UpdateTime AS __lastTime, ID AS __lastKey, Code AS code FROM ShareDataPacket "
            + $"WHERE ID = '{packetId}' AND @pc = '{partnerCode}' AND @constTag = 'ACTIVE' AND @minRate <= 1.5 "
            + "AND UpdateTime <= @runAt AND @yearStart <= @runAt "
            + "AND (@prevRun IS NULL OR @prevRun < @runAt) "
            + "AND (@since IS NULL OR UpdateTime > @since OR (UpdateTime = @since AND ID > @afterKey)) "
            + "ORDER BY UpdateTime, ID";

        /// <summary>
        /// Description: Một lượt ProcessSubscriptions với câu SQL dùng đủ nguồn giá trị của bảng tham số (không còn hằng số):
        ///              - common-all-sources: bộ biến mẫu chung đúng hết → gửi đúng dòng, nhận là nối đuôi nên mốc ShareDataLastSend tiến tới dòng vừa gửi.
        ///              - common-wrong-source: @pc lấy nhầm nguồn (mốc khóa = rỗng) → không gửi, mốc giữ nguyên (chứng minh giá trị biến gắn đúng theo nguồn).
        ///              - partner-own-set: mẫu chung lấy nhầm nguồn, đối tác có SQL riêng với bộ biến riêng lấy đúng "Mã đối tác" → gửi được
        ///                (Worker dùng bộ biến của SQL riêng).
        /// Created date: 25/10/2026
        /// Modified date: 26/10/2026 (Bỏ hằng số: chỉ còn nguồn hệ thống, thêm nguồn Mã đối tác)
        /// </summary>
        [Theory]
        [InlineData("common-all-sources", true)]
        [InlineData("common-wrong-source", false)]
        [InlineData("partner-own-set", true)]
        public async Task ProcessSubscriptions_WhenSqlUsesDeclaredVariables_BindsEverySource_Test(string scenario, bool expectSent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-var-{unique}";
            var packetCode = $"PKT_VAR_{unique}";
            var partnerCode = $"P_VAR_{unique}";
            var initialLastTime = new DateTime(2026, 10, 20, 8, 0, 0);
            var rowTime = initialLastTime.AddMinutes(5);
            var sql = AllSourcesSql(packetId, partnerCode);

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>()
                .SetColumns(p => p.UpdateTime == rowTime)
                .Where(p => p.ID == packetId)
                .ExecuteCommandAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql, "UpdateTime");

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = partnerCode,
                SubCode = $"SUB_VAR_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["code"] = new Dictionary<string, string> { ["$field"] = "code" } },
                ConfigureSub = s => s.LastTimeRun = initialLastTime
            });

            var commonPcSource = scenario == "common-all-sources" ? ShareDataConst.SqlVariableSourcePartnerCode : ShareDataConst.SqlVariableSourceLastKey;
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, BuildVariableSet(commonPcSource));
            if (scenario == "partner-own-set")
            {
                await ShareDataSqlSeed.SetPartnerAsync(db, packetId, partner.ID, sql);
                await ShareDataSqlSeed.SetVariablesAsync(db, packetId, partner.ID, BuildVariableSet(ShareDataConst.SqlVariableSourcePartnerCode));
            }

            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partner.Code,
                PacketCode = packetCode,
                LastTime = initialLastTime,
                LastKey = string.Empty
            }).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var received = _host.PartnerServer.ReceivedRequests.Count(r => r.Body.Contains(packetCode));
            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partner.Code && c.PacketCode == packetCode)
                .FirstAsync();
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).Select(a => a.Message).ToListAsync();

            if (expectSent)
            {
                Assert.True(received == 1, $"Đối tác phải nhận đúng 1 lần gửi, nhận {received}. Cảnh báo: {string.Join(" | ", alerts)}");
                Assert.Equal(rowTime, lastSend.LastTime!.Value, TimeSpan.FromSeconds(1));
                Assert.Equal(packetId, lastSend.LastKey);
            }
            else
            {
                Assert.Equal(0, received);
                Assert.Equal(initialLastTime, lastSend.LastTime!.Value, TimeSpan.FromSeconds(1));
                Assert.Empty(alerts);
            }
        }

        /// <summary>Bộ biến cho AllSourcesSql của một câu SQL (partnerId trống = mẫu chung); @pc lấy từ nguồn chỉ định.</summary>
        private static List<SqlVariableItem> BuildVariableSet(string pcSource)
        {
            SqlVariableItem Builtin(string code, string source, int order) => new()
            {
                Code = code,
                DataType = ShareDataConst.SqlVariableBuiltinTypes[source], ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source, OrderNo = order
            };
            SqlVariableItem Const(string code, string dataType, string value, int order) => new()
            {
                Code = code,
                DataType = dataType, ValueKind = ShareDataConst.SqlVariableKindConst, Value = value, OrderNo = order
            };

            return
            [
                Builtin("pc", pcSource, 1),
                Builtin("runAt", ShareDataConst.SqlVariableSourceNow, 2),
                Builtin("yearStart", ShareDataConst.SqlVariableSourceStartOfYear, 3),
                Builtin("prevRun", ShareDataConst.SqlVariableSourceLastRunTime, 4),
                Builtin("since", ShareDataConst.SqlVariableSourceLastTime, 5),
                Builtin("afterKey", ShareDataConst.SqlVariableSourceLastKey, 6),
                Const("pageSize", ShareDataConst.SqlVariableTypeInt, "100", 7),
                Const("constTag", ShareDataConst.SqlVariableTypeString, "ACTIVE", 8),
                Const("minRate", ShareDataConst.SqlVariableTypeDecimal, "1.25", 9)
            ];
        }

        #endregion

        #region 11. Trường thiếu trong câu SQL: Bỏ qua thì không đưa vào ánh xạ, không bỏ qua thì giá trị null (biên bản 23/10/2026 §1.1)

        /// <summary>
        /// Description: Ánh xạ trỏ tới cột mà câu SQL không trả về:
        ///              - skip: nút ánh xạ đánh dấu bỏ qua ($exclude) → khóa không có trong gói gửi đi.
        ///              - keep-null: không bỏ qua → khóa vẫn có trong gói gửi đi với giá trị null.
        /// Created date: 25/10/2026
        /// </summary>
        [Theory]
        [InlineData("skip", false)]
        [InlineData("keep-null", true)]
        public async Task ProcessSubscriptions_WhenMappedColumnMissingFromSql_SkipsOrSendsNull_Test(string scenario, bool expectKeyWithNull)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-miss-{unique}";
            var packetCode = $"PKT_MISS_{unique}";
            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, $"SELECT Code AS code FROM ShareDataPacket WHERE ID = '{packetId}'");

            var missingNode = new Dictionary<string, object> { ["$field"] = "lanecode" };
            if (scenario == "skip")
            {
                missingNode["$exclude"] = true;
            }
            await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_MISS_{unique}",
                SubCode = $"SUB_MISS_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object>
                {
                    ["code"] = new Dictionary<string, string> { ["$field"] = "code" },
                    ["lanecode"] = missingNode
                }
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var body = _host.PartnerServer.ReceivedRequests.Select(r => r.Body).Single(b => b.Contains(packetCode));
            Assert.Equal(expectKeyWithNull, body.Contains("\"lanecode\":null"));
            if (!expectKeyWithNull)
            {
                Assert.DoesNotContain("lanecode", body);
            }
        }

        #endregion

        #region 12. Nối đuôi khi nhiều dòng trùng mốc thời gian (biên bản 23/10/2026 [19:24])

        /// <summary>
        /// Description: Giả lập 5 dòng cùng một mốc thời gian, mỗi trang lấy ít dòng hơn nên ranh giới trang rơi giữa nhóm trùng giờ.
        ///              - two-conditions: lọc "thời gian lớn hơn mốc, hoặc bằng mốc và khóa lớn hơn mốc khóa" → gửi đủ 5 dòng, không dòng nào lặp.
        ///              - time-only: chỉ lọc "thời gian lớn hơn mốc", không khai biến mốc khóa → trang đầu gửi xong thì các dòng cùng giờ còn lại
        ///                bị bỏ sót, nên Worker không chạy câu này: không gửi dòng nào và ghi cảnh báo CursorOrderInvalid, kể cả khi trang đủ lớn
        ///                để lấy hết một lần (trước 30/10/2026 gửi 2/5 dòng với trang 2 dòng).
        /// Created date: 25/10/2026
        /// Modified date: 30/10/2026
        /// </summary>
        [Theory]
        [InlineData("two-conditions", 2, 5)]
        [InlineData("two-conditions", 1, 5)]
        [InlineData("time-only", 2, 0)]
        [InlineData("time-only", 10, 0)]
        public async Task ProcessSubscriptions_WhenRowsShareSameTime_SendsEachRowOnceOnlyWithTwoConditions_Test(string filter, int pageSize, int expectedRows)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-dup-{unique}";
            var packetCode = $"PKT_DUP_{unique}";
            var rowPrefix = $"ROW_DUP_{unique}_";
            var initialLastTime = new DateTime(2026, 10, 20, 8, 0, 0);
            var sameTime = initialLastTime.AddMinutes(5);

            var rows = Enumerable.Range(1, 5).Select(i => new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"{rowPrefix}{i}",
                Name = $"Row {i}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList();
            await db.Insertable(rows).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>()
                .SetColumns(p => p.UpdateTime == sameTime)
                .Where(p => p.Code!.StartsWith(rowPrefix))
                .ExecuteCommandAsync();

            var cursorFilter = filter == "two-conditions"
                ? "(@since IS NULL OR UpdateTime > @since OR (UpdateTime = @since AND ID > @afterKey))"
                : "(@since IS NULL OR UpdateTime > @since)";
            // Cỡ trang (@pageSize kiểu Int) và tiền tố mã (@prefix kiểu String) truyền qua biến "Giá trị nhập" (Const); có TOP nên Worker lặp nhiều trang mỗi lượt
            var sql = $"SELECT TOP (@pageSize) UpdateTime AS __lastTime, ID AS __lastKey, Code AS code FROM ShareDataPacket "
                + $"WHERE Code LIKE @prefix AND {cursorFilter} ORDER BY UpdateTime, ID";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            SqlVariableItem Variable(string code, string source, int order) => new()
            {
                Code = code, DataType = ShareDataConst.SqlVariableBuiltinTypes[source],
                ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source, OrderNo = order
            };
            var variables = new List<SqlVariableItem>
            {
                Variable("since", ShareDataConst.SqlVariableSourceLastTime, 1),
                new()
                {
                    Code = "pageSize",
                    DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = pageSize.ToString(), OrderNo = 3
                },
                new()
                {
                    Code = "prefix",
                    DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{rowPrefix}%", OrderNo = 4
                }
            };
            if (filter == "two-conditions")
            {
                variables.Add(Variable("afterKey", ShareDataConst.SqlVariableSourceLastKey, 2));
            }
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql, "UpdateTime");

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_DUP_{unique}",
                SubCode = $"SUB_DUP_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["code"] = new Dictionary<string, string> { ["$field"] = "code" } },
                ConfigureSub = s => s.LastTimeRun = initialLastTime
            });
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partner.Code,
                PacketCode = packetCode,
                LastTime = initialLastTime,
                LastKey = string.Empty
            }).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert: mỗi dòng gửi tối đa một lần, số dòng gửi được đúng kỳ vọng
            var bodies = _host.PartnerServer.ReceivedRequests.Select(r => r.Body).ToList();
            var sentPerRow = rows.ToDictionary(r => r.Code!, r => bodies.Count(b => b.Contains(r.Code!)));
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).Select(a => a.Message).ToListAsync();
            Assert.True(sentPerRow.Values.All(n => n <= 1), $"Có dòng bị gửi lặp: {string.Join(", ", sentPerRow.Select(p => $"{p.Key}={p.Value}"))}");
            Assert.True(sentPerRow.Values.Count(n => n == 1) == expectedRows,
                $"Kỳ vọng gửi {expectedRows} dòng, thực tế {sentPerRow.Values.Count(n => n == 1)}. Cảnh báo: {string.Join(" | ", alerts)}");
            if (filter == "time-only")
            {
                var filterAlerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.CursorOrderInvalid)
                    .CountAsync();
                Assert.Equal(1, filterAlerts);
            }
        }

        /// <summary>
        /// Description: Câu SQL nối đuôi nạp thẳng vào CSDL (seed / sửa tay, không qua API lưu) mà sắp sai thứ tự
        ///              (không bắt đầu bằng __lastTime rồi __lastKey) thì Worker kiểm lại cấu trúc trước khi chạy: không gửi,
        ///              giữ nguyên mốc và ghi cảnh báo ESH-1306. Sắp đúng thứ tự thì gửi bình thường.
        /// Created date: 28/10/2026
        /// </summary>
        [Theory]
        [InlineData("ORDER BY UpdateTime, ID", true)]
        [InlineData("ORDER BY ID, UpdateTime", false)]
        [InlineData("ORDER BY Code", false)]
        public async Task ProcessSubscriptions_WhenIncrementalSqlOrderInvalidAtRuntime_SkipsSendKeepsCursorAndRaisesEsh1306_Test(string orderBy, bool expectSent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-ord-{unique}";
            var packetCode = $"PKT_ORD_{unique}";
            var rowPrefix = $"ROW_ORD_{unique}_";
            var initialLastTime = new DateTime(2026, 10, 20, 8, 0, 0);

            var rows = Enumerable.Range(1, 2).Select(i => new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"{rowPrefix}{i}",
                Name = $"Row {i}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList();
            await db.Insertable(rows).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>()
                .SetColumns(p => p.UpdateTime == initialLastTime.AddMinutes(5))
                .Where(p => p.Code!.StartsWith(rowPrefix))
                .ExecuteCommandAsync();

            var sql = "SELECT UpdateTime AS __lastTime, ID AS __lastKey, Code AS code FROM ShareDataPacket "
                + $"WHERE Code LIKE '{rowPrefix}%' AND (@since IS NULL OR UpdateTime > @since OR (UpdateTime = @since AND ID > @afterKey)) {orderBy}";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            SqlVariableItem Variable(string code, string source, int order) => new()
            {
                Code = code, DataType = ShareDataConst.SqlVariableBuiltinTypes[source],
                ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source, OrderNo = order
            };
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                Variable("since", ShareDataConst.SqlVariableSourceLastTime, 1),
                Variable("afterKey", ShareDataConst.SqlVariableSourceLastKey, 2)
            ]);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql, "UpdateTime");

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_ORD_{unique}",
                SubCode = $"SUB_ORD_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["code"] = new Dictionary<string, string> { ["$field"] = "code" } },
                ConfigureSub = s => s.LastTimeRun = initialLastTime
            });
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partner.Code,
                PacketCode = packetCode,
                LastTime = initialLastTime,
                LastKey = string.Empty
            }).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var sentAny = _host.PartnerServer.ReceivedRequests.Any(r => rows.Any(row => r.Body.Contains(row.Code!)));
            var orderAlerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.CursorOrderInvalid)
                .CountAsync();
            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(l => l.PartnerCode == partner.Code && l.PacketCode == packetCode)
                .FirstAsync();

            Assert.Equal(expectSent, sentAny);
            Assert.Equal(expectSent ? 0 : 1, orderAlerts);
            if (!expectSent)
            {
                Assert.Equal(initialLastTime, lastSend.LastTime);
                Assert.Equal(string.Empty, lastSend.LastKey ?? string.Empty);
            }
        }

        /// <summary>
        /// Description: Nối đuôi gộp 1 cột (@lastKey = CONVERT(varchar(23), UpdateTime, 121) + '_' + ID):
        ///              - Lượt 1: 5 dòng trùng đúng một thời điểm, phân trang TOP (@pageSize = 2) gửi đủ 3 trang (2+2+1) không lặp không sót,
        ///                không rò cột hệ thống __lastKey ra payload JSON, và lưu mốc dòng thứ 5.
        ///              - Lượt 2: dòng thứ 6 có thời gian lớn hơn nhưng ID nhỏ hơn từ điển vẫn được gửi đúng và tiến mốc.
        ///              - Lượt 3: không có dữ liệu mới thì không gửi bản tin HTTP và ghi nhận NoNewData.
        /// Created date: 27/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_CompositeLastKey_SameMillisecondMultiPage_SmallerGuidLaterTime_AndStripsLastKey_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-comp-{unique}";
            var packetCode = $"PKT_COMP_{unique}";
            var rowPrefix = $"ZC_MP_{unique}_";
            var sameTime = new DateTime(2026, 10, 27, 10, 0, 0, 120);
            var idPrefixes = new[] { "b1", "c2", "d3", "e4", "f5" };

            var initialZones = idPrefixes.Select((prefix, idx) => new TmsZone
            {
                ID = $"{prefix}_{unique}",
                Code = $"{rowPrefix}{idx + 1}",
                Name = $"Zone {idx + 1}"
            }).ToList();
            await db.Insertable(initialZones).ExecuteCommandAsync();
            await db.Updateable<TmsZone>()
                .SetColumns(z => z.UpdateTime == sameTime)
                .Where(z => z.Code!.StartsWith(rowPrefix))
                .ExecuteCommandAsync();

            const string compositeExpr = "CONVERT(varchar(23), UpdateTime, 121) + '_' + ID";
            var sql = $"SELECT TOP (@pageSize) Code AS zoneCode, {compositeExpr} AS __lastKey FROM TmsZone "
                + $"WHERE Code LIKE @prefix AND ({compositeExpr}) > @lastKey ORDER BY __lastKey ASC";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                new SqlVariableItem { Code = "lastKey", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = ShareDataConst.SqlVariableSourceLastKey, OrderNo = 1 },
                new SqlVariableItem { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = "2", OrderNo = 2 },
                new SqlVariableItem { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{rowPrefix}%", OrderNo = 3 }
            ]);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_COMP_{unique}",
                SubCode = $"SUB_COMP_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["zoneCode"] = new Dictionary<string, string> { ["$field"] = "zoneCode" } }
            });
            var worker = CreateWorker(scope);

            // Act 1: 5 dòng trùng mốc thời gian với pageSize = 2 -> 3 trang (2 + 2 + 1)
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 1
            var run1Bodies = _host.PartnerServer.ReceivedRequests.Select(r => r.Body).ToList();
            Assert.Equal(3, run1Bodies.Count);
            Assert.All(run1Bodies, body => Assert.DoesNotContain("__lastKey", body, StringComparison.OrdinalIgnoreCase));
            foreach (var zone in initialZones)
            {
                Assert.Equal(1, run1Bodies.Count(b => b.Contains(zone.Code!)));
            }
            Assert.Contains($"{rowPrefix}1", run1Bodies[0]);
            Assert.Contains($"{rowPrefix}2", run1Bodies[0]);
            Assert.Contains($"{rowPrefix}3", run1Bodies[1]);
            Assert.Contains($"{rowPrefix}4", run1Bodies[1]);
            Assert.Contains($"{rowPrefix}5", run1Bodies[2]);

            var lastSendAfterRun1 = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Equal($"2026-10-27 10:00:00.120_f5_{unique}", lastSendAfterRun1.LastKey);

            // Act 2: thêm dòng thứ 6 có thời gian mới hơn nhưng ID nhỏ hơn từ điển ("00_..." < "f5_...")
            var laterTime = sameTime.AddSeconds(1);
            var sixthZone = new TmsZone { ID = $"00_{unique}", Code = $"{rowPrefix}6", Name = "Zone 6" };
            await db.Insertable(sixthZone).ExecuteCommandAsync();
            await db.Updateable<TmsZone>().SetColumns(z => z.UpdateTime == laterTime).Where(z => z.ID == sixthZone.ID).ExecuteCommandAsync();
            var dueNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>().SetColumns(s => s.NextTimeRun == dueNow.AddSeconds(-10)).Where(s => s.ID == sub.ID).ExecuteCommandAsync();

            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 2
            Assert.Equal(4, _host.PartnerServer.ReceivedRequests.Count);
            var run2Body = _host.PartnerServer.ReceivedRequests[3].Body;
            Assert.Contains(sixthZone.Code, run2Body);
            Assert.DoesNotContain($"{rowPrefix}5", run2Body);
            var lastSendAfterRun2 = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Equal($"2026-10-27 10:00:01.120_00_{unique}", lastSendAfterRun2.LastKey);

            // Act 3: không phát sinh dòng mới -> không gửi HTTP, giữ nguyên mốc
            dueNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>().SetColumns(s => s.NextTimeRun == dueNow.AddSeconds(-10)).Where(s => s.ID == sub.ID).ExecuteCommandAsync();
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 3
            Assert.Equal(4, _host.PartnerServer.ReceivedRequests.Count);
            var latestParentLog = (await GetLogs(db, sub.ID)).First();
            Assert.Equal(ShareDataEnum.ExportMessage.NoNewData, latestParentLog.ErrorMessage);
            var lastSendAfterRun3 = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Equal($"2026-10-27 10:00:01.120_00_{unique}", lastSendAfterRun3.LastKey);
        }

        /// <summary>
        /// Description: Vòng đời @lastKey có DefaultValue và nhiều đối tác dùng chung câu SQL mẫu chung:
        ///              - Lượt đầu chưa có mốc chạy: áp DefaultValue để bỏ qua dữ liệu cũ trước mốc cắt.
        ///              - Đối tác A thành công tiến mốc độc lập, đối tác B lỗi HTTP 500 giữ nguyên mốc rồi lượt sau tự bù đủ từ DefaultValue.
        ///              - Tách rồi đặt lại SQL riêng (ResetPartnerExtractSqlInput) bảo toàn mốc ShareDataLastSend;
        ///                đặt lại mốc chạy (ResetLastSendInput) của đối tác A khiến chỉ đối tác A xuất lại từ DefaultValue.
        /// Created date: 27/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_CompositeLastKey_DefaultValueCutoff_MultiPartnerIsolation_AndResetLifecycle_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-iso-{unique}";
            var packetCode = $"PKT_ISO_{unique}";
            var rowPrefix = $"ZC_ISO_{unique}_";
            const string cutoff = "2026-10-27 09:00:00.000_";

            var oldZones = new[]
            {
                (Zone: new TmsZone { ID = $"old1_{unique}", Code = $"{rowPrefix}OLD1", Name = "Old 1" }, Time: new DateTime(2026, 10, 27, 8, 30, 0)),
                (Zone: new TmsZone { ID = $"old2_{unique}", Code = $"{rowPrefix}OLD2", Name = "Old 2" }, Time: new DateTime(2026, 10, 27, 8, 45, 0))
            };
            var newZones = new[]
            {
                (Zone: new TmsZone { ID = $"new1_{unique}", Code = $"{rowPrefix}NEW1", Name = "New 1" }, Time: new DateTime(2026, 10, 27, 9, 10, 0)),
                (Zone: new TmsZone { ID = $"new2_{unique}", Code = $"{rowPrefix}NEW2", Name = "New 2" }, Time: new DateTime(2026, 10, 27, 9, 20, 0))
            };

            foreach (var (zone, time) in oldZones.Concat(newZones))
            {
                await db.Insertable(zone).ExecuteCommandAsync();
                await db.Updateable<TmsZone>().SetColumns(z => z.UpdateTime == time).Where(z => z.ID == zone.ID).ExecuteCommandAsync();
            }

            const string compositeExpr = "CONVERT(varchar(23), UpdateTime, 121) + '_' + ID";
            var commonSql = $"SELECT TOP 50 Code AS zoneCode, {compositeExpr} AS __lastKey FROM TmsZone "
                + $"WHERE Code LIKE @prefix AND ({compositeExpr}) > @lastKey ORDER BY __lastKey ASC";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, commonSql);
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                new SqlVariableItem
                {
                    Code = "lastKey",
                    DataType = ShareDataConst.SqlVariableTypeString,
                    ValueKind = ShareDataConst.SqlVariableKindBuiltin,
                    Value = ShareDataConst.SqlVariableSourceLastKey,
                    DefaultValue = cutoff,
                    OrderNo = 1
                },
                new SqlVariableItem
                {
                    Code = "prefix",
                    DataType = ShareDataConst.SqlVariableTypeString,
                    ValueKind = ShareDataConst.SqlVariableKindConst,
                    Value = $"{rowPrefix}%",
                    OrderNo = 2
                }
            ]);

            var shape = new Dictionary<string, object> { ["zoneCode"] = new Dictionary<string, string> { ["$field"] = "zoneCode" } };
            var (partnerA, subA) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"PA_{unique}",
                SubCode = $"SUB_PA_{unique}",
                DatatypeId = packetId,
                MappingShape = shape,
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/partner-a-{unique}"
            });
            var (partnerB, subB) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"PB_{unique}",
                SubCode = $"SUB_PB_{unique}",
                DatatypeId = packetId,
                MappingShape = shape,
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/partner-b-{unique}"
            });

            var failPartnerB = true;
            _host.PartnerServer.Responder = (_, req) => req.Path.Contains($"partner-b-{unique}", StringComparison.OrdinalIgnoreCase) && failPartnerB
                ? new ShareDataPartnerServerMock.PartnerResponsePlan(HttpStatusCode.InternalServerError, "{\"error\":\"partner_b_down\"}")
                : null;

            var worker = CreateWorker(scope);

            // Act 1: Partner A thành công (nhận NEW1, NEW2, bỏ qua OLD1, OLD2); Partner B lỗi HTTP 500 giữ mốc null
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 1
            var partnerABodiesRun1 = _host.PartnerServer.ReceivedRequests.Where(r => r.Path.Contains($"partner-a-{unique}")).Select(r => r.Body).ToList();
            Assert.Single(partnerABodiesRun1);
            Assert.DoesNotContain($"{rowPrefix}OLD1", partnerABodiesRun1[0]);
            Assert.DoesNotContain($"{rowPrefix}OLD2", partnerABodiesRun1[0]);
            Assert.Contains($"{rowPrefix}NEW1", partnerABodiesRun1[0]);
            Assert.Contains($"{rowPrefix}NEW2", partnerABodiesRun1[0]);

            var lastSendA1 = await db.Queryable<ShareDataLastSend>().Where(ls => ls.PartnerCode == partnerA.Code && ls.PacketCode == packetCode).FirstAsync();
            var lastSendB1 = await db.Queryable<ShareDataLastSend>().Where(ls => ls.PartnerCode == partnerB.Code && ls.PacketCode == packetCode).FirstAsync();
            Assert.Equal($"2026-10-27 09:20:00.000_new2_{unique}", lastSendA1.LastKey);
            Assert.Null(lastSendB1?.LastKey);

            // Act 2: thêm NEW3, khôi phục Partner B -> Partner A chỉ nhận NEW3, Partner B nhận đủ NEW1..NEW3 từ DefaultValue
            var zoneNew3 = new TmsZone { ID = $"new3_{unique}", Code = $"{rowPrefix}NEW3", Name = "New 3" };
            await db.Insertable(zoneNew3).ExecuteCommandAsync();
            await db.Updateable<TmsZone>().SetColumns(z => z.UpdateTime == new DateTime(2026, 10, 27, 9, 30, 0)).Where(z => z.ID == zoneNew3.ID).ExecuteCommandAsync();
            failPartnerB = false;
            var dueNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == dueNow.AddSeconds(-10))
                .Where(s => s.ID == subA.ID || s.ID == subB.ID)
                .ExecuteCommandAsync();

            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 2
            var partnerABodiesRun2 = _host.PartnerServer.ReceivedRequests.Where(r => r.Path.Contains($"partner-a-{unique}")).Select(r => r.Body).ToList();
            var partnerBBodiesAll = _host.PartnerServer.ReceivedRequests.Where(r => r.Path.Contains($"partner-b-{unique}")).Select(r => r.Body).ToList();
            Assert.Equal(2, partnerABodiesRun2.Count);
            Assert.DoesNotContain($"{rowPrefix}NEW1", partnerABodiesRun2[1]);
            Assert.DoesNotContain($"{rowPrefix}NEW2", partnerABodiesRun2[1]);
            Assert.Contains($"{rowPrefix}NEW3", partnerABodiesRun2[1]);

            var partnerBRecoveredBody = partnerBBodiesAll.Last();
            Assert.DoesNotContain($"{rowPrefix}OLD1", partnerBRecoveredBody);
            Assert.Contains($"{rowPrefix}NEW1", partnerBRecoveredBody);
            Assert.Contains($"{rowPrefix}NEW2", partnerBRecoveredBody);
            Assert.Contains($"{rowPrefix}NEW3", partnerBRecoveredBody);

            // Act 3: Partner A tách SQL riêng rồi đặt lại về mẫu chung -> mốc chạy không mất, lượt kế không gửi lặp
            var httpContextAccessor = _host.Services.GetRequiredService<IHttpContextAccessor>();
            httpContextAccessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimConst.AccountType, ((int)BaseEnums.AccountTypeEnum.SuperAdmin).ToString()),
                    new Claim(ClaimConst.Account, $"super_{unique}"),
                    new Claim(ClaimConst.UserId, Guid.NewGuid().ToString("N")),
                    new Claim(ClaimTypes.Name, $"super_{unique}")
                ], "TestAuth"))
            };
            await ShareDataSqlSeed.SetPartnerAsync(db, packetId, partnerA.ID, commonSql);
            await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(
                new ResetPartnerExtractSqlInput { PartnerId = partnerA.ID!, PacketCode = packetCode });

            dueNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>().SetColumns(s => s.NextTimeRun == dueNow.AddSeconds(-10)).Where(s => s.ID == subA.ID).ExecuteCommandAsync();
            await worker.ProcessSubscriptions(CancellationToken.None);
            Assert.Equal(2, _host.PartnerServer.ReceivedRequests.Count(r => r.Path.Contains($"partner-a-{unique}")));

            // Act 4: SuperAdmin đặt lại mốc chạy của riêng Partner A -> Partner A gửi lại đủ NEW1..NEW3 từ DefaultValue, Partner B đứng yên
            await scope.ServiceProvider.CallApiAsync(new ResetLastSendInput { PacketCode = packetCode, PartnerId = partnerA.ID! });
            var partnerBCountBeforeResetRun = _host.PartnerServer.ReceivedRequests.Count(r => r.Path.Contains($"partner-b-{unique}"));
            dueNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == dueNow.AddSeconds(-10))
                .Where(s => s.ID == subA.ID || s.ID == subB.ID)
                .ExecuteCommandAsync();
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 4
            var partnerABodiesAfterReset = _host.PartnerServer.ReceivedRequests.Where(r => r.Path.Contains($"partner-a-{unique}")).Select(r => r.Body).ToList();
            Assert.Equal(3, partnerABodiesAfterReset.Count);
            Assert.DoesNotContain($"{rowPrefix}OLD1", partnerABodiesAfterReset[2]);
            Assert.Contains($"{rowPrefix}NEW1", partnerABodiesAfterReset[2]);
            Assert.Contains($"{rowPrefix}NEW2", partnerABodiesAfterReset[2]);
            Assert.Contains($"{rowPrefix}NEW3", partnerABodiesAfterReset[2]);
            Assert.Equal(partnerBCountBeforeResetRun, _host.PartnerServer.ReceivedRequests.Count(r => r.Path.Contains($"partner-b-{unique}")));
        }

        /// <summary>
        /// Description: Các chốt bảo vệ con trỏ ở chế độ @lastKey gộp 1 cột:
        ///              - key-too-long: __lastKey dài quá 256 ký tự -> huỷ trước khi gửi HTTP, ghi ESH-1304 và giữ nguyên mốc cũ.
        ///              - key-empty: __lastKey rỗng/khoảng trắng -> huỷ trước khi gửi HTTP, ghi ESH-1302 và giữ nguyên mốc cũ
        ///                (chưa có mốc khóa nên @lastKey = '' và câu so >= mới lấy được dòng khóa khoảng trắng).
        ///              - non-advancing-gte: câu so khóa bằng >= (lưu được, có cảnh báo) trả về đúng dòng có __lastKey bằng mốc đã lưu kèm
        ///                TOP (@pageSize = 1) -> mốc không tiến nên giữ nguyên và dừng phân trang ngay sau trang 1 (tránh lặp vô hạn maxPagesPerRun).
        ///              Cột mốc và điều kiện WHERE viết cùng một biểu thức, so với biến mốc, để qua bộ kiểm cấu trúc trước khi chạy (30/10/2026).
        ///              - clock-behind-lasttime: LastTime cũ trong bảng lớn hơn giờ máy hiện tại nhưng __lastKey mới lớn hơn ->
        ///                vẫn phải cập nhật tiến mốc LastKey.
        /// Created date: 27/10/2026
        /// </summary>
        [Theory]
        [InlineData("key-too-long")]
        [InlineData("key-empty")]
        [InlineData("non-advancing-gte")]
        [InlineData("clock-behind-lasttime")]
        public async Task ProcessSubscriptions_CompositeLastKey_CursorGuardsAndMonotonicProtection_Test(string scenario)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-grd-{unique}";
            var packetCode = $"PKT_GRD_{unique}";
            var rowPrefix = $"ZC_GRD_{unique}_";
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");

            var zone1 = new TmsZone { ID = $"a1_{unique}", Code = $"{rowPrefix}1", Name = "Guard 1" };
            var zone2 = new TmsZone { ID = $"b2_{unique}", Code = $"{rowPrefix}2", Name = "Guard 2" };
            await db.Insertable(new List<TmsZone> { zone1, zone2 }).ExecuteCommandAsync();
            await db.Updateable<TmsZone>().SetColumns(z => z.UpdateTime == new DateTime(2026, 10, 27, 10, 0, 0)).Where(z => z.ID == zone1.ID).ExecuteCommandAsync();
            await db.Updateable<TmsZone>().SetColumns(z => z.UpdateTime == new DateTime(2026, 10, 27, 10, 5, 0)).Where(z => z.ID == zone2.ID).ExecuteCommandAsync();

            var key1 = $"2026-10-27 10:00:00.000_{zone1.ID}";
            var key2 = $"2026-10-27 10:05:00.000_{zone2.ID}";
            const string compositeExpr = "CONVERT(varchar(23), UpdateTime, 121) + '_' + ID";

            var (sql, priorLastTime, priorLastKey) = scenario switch
            {
                "key-too-long" => (
                    $"SELECT TOP 10 Code AS zoneCode, ID + REPLICATE('K', 260) AS __lastKey FROM TmsZone WHERE Code LIKE @prefix AND ID + REPLICATE('K', 260) > @lastKey ORDER BY __lastKey ASC",
                    dbNow.AddMinutes(-5),
                    (string?)"000_prior"),
                "key-empty" => (
                    $"SELECT TOP 10 Code AS zoneCode, REPLACE(ID, ID, '   ') AS __lastKey FROM TmsZone WHERE Code LIKE @prefix AND REPLACE(ID, ID, '   ') >= @lastKey ORDER BY __lastKey ASC",
                    dbNow.AddMinutes(-5),
                    null),
                "non-advancing-gte" => (
                    $"SELECT TOP (@pageSize) Code AS zoneCode, {compositeExpr} AS __lastKey FROM TmsZone WHERE Code LIKE @prefix AND ({compositeExpr}) >= @lastKey ORDER BY __lastKey ASC",
                    dbNow.AddMinutes(-10),
                    key2),
                _ => (
                    $"SELECT TOP (@pageSize) Code AS zoneCode, {compositeExpr} AS __lastKey FROM TmsZone WHERE Code LIKE @prefix AND ({compositeExpr}) > @lastKey ORDER BY __lastKey ASC",
                    dbNow.AddHours(1),
                    key1)
            };

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                new SqlVariableItem { Code = "lastKey", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = ShareDataConst.SqlVariableSourceLastKey, OrderNo = 1 },
                new SqlVariableItem { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = "1", OrderNo = 2 },
                new SqlVariableItem { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{rowPrefix}%", OrderNo = 3 }
            ]);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_GRD_{unique}",
                SubCode = $"SUB_GRD_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["zoneCode"] = new Dictionary<string, string> { ["$field"] = "zoneCode" } }
            });
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partner.Code,
                PacketCode = packetCode,
                LastTime = priorLastTime,
                LastKey = priorLastKey
            }).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var savedLastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID)
                .ToListAsync();

            if (scenario == "key-too-long")
            {
                Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);
                Assert.Contains(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.CursorKeyTooLong);
                Assert.Equal("000_prior", savedLastSend.LastKey);
            }
            else if (scenario == "key-empty")
            {
                Assert.Equal(0, _host.PartnerServer.TotalReceivedRequests);
                Assert.Contains(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.RequiredFieldMissing);
                Assert.Null(savedLastSend.LastKey);
            }
            else if (scenario == "non-advancing-gte")
            {
                Assert.Equal(key2, savedLastSend.LastKey);
                Assert.Equal(1, _host.PartnerServer.TotalReceivedRequests);
            }
            else
            {
                Assert.Equal(1, _host.PartnerServer.TotalReceivedRequests);
                Assert.Contains($"{rowPrefix}2", _host.PartnerServer.ReceivedRequests[0].Body);
                Assert.Equal(key2, savedLastSend.LastKey);
            }
        }

        #endregion

        #endregion

        #region WarningAlert

        [Fact]
        public async Task ProcessSubscriptions_WhenConnectionConfigHasIgnoredValues_LogsEsh1403Warning_DeduplicatesAlert_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partnerServer = _host.PartnerServer;
            partnerServer.ResetDefaults();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            // Mỗi lượt phải có dữ liệu để gửi (mới kiểm cấu hình kết nối): dùng câu 101 kiểu gửi toàn bộ thay cho câu mẫu nối đuôi
            var packet101 = await db.Queryable<ShareDataPacket>().Where(p => p.Code == "101").FirstAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packet101.ID, LegacyPacketSql.Packet101);
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var invalidConfig1 = "{\"TimeoutSeconds\": 10, \"AuthType\": \"None\", \"Token\": \"stray_token\", \"OutputPath\": \"/deprecated/path\"}";

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_WARN_{unique}",
                SubCode = $"SUB_WARN_{unique}",
                DatatypeId = "101",
                ConfigurePartner = p =>
                {
                    p.Address = ShareDataPartnerServerMock.DefaultHost;
                    p.Port = ShareDataPartnerServerMock.DefaultPort;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                    p.ConnectionConfig = invalidConfig1;
                }
            });

            var worker = CreateWorker(scope);

            // Run 1: Must succeed in delivery AND log ESH-1403 alert
            await worker.ProcessSubscriptions(CancellationToken.None);

            var logs1 = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs1);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs1[0].Success);

            var alerts1 = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.ConfigValueIgnored)
                .ToListAsync();
            Assert.Single(alerts1);
            Assert.Equal(BaseEnums.AlertSeverity.Warning, alerts1[0].Severity);
            Assert.Equal(BaseEnums.AlertSource.Protocol, alerts1[0].AlertSource);
            Assert.Contains("OutputPath", alerts1[0].Message);
            Assert.Contains("xác thực", alerts1[0].Message);

            // Run 2 (same config): Must succeed in delivery, but NOT add duplicate ESH-1403 alert
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            await worker.ProcessSubscriptions(CancellationToken.None);

            var alerts2 = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.ConfigValueIgnored)
                .ToListAsync();
            Assert.Single(alerts2);

            // Run 3 (changed config): Change config to new invalid config, update partner
            var invalidConfig2 = "{\"TimeoutSeconds\": 10, \"AuthType\": \"UnsupportedMethodXYZ\", \"OutputPath\": \"/another/path\"}";
            await db.Updateable<ShareDataPartner>()
                .SetColumns(p => p.ConnectionConfig == invalidConfig2)
                .Where(p => p.ID == partner.ID)
                .ExecuteCommandAsync();

            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == db.Ado.GetDateTimeAsync("SELECT GETDATE()").GetAwaiter().GetResult().AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            await worker.ProcessSubscriptions(CancellationToken.None);

            var alerts3 = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.ConfigValueIgnored)
                .ToListAsync();
            Assert.Equal(2, alerts3.Count);
        }

        #endregion

        #region 13. Phương án 4 - Composite Keyset Pagination (@lastTime + @lastKey) - 6 Ca nghiệp vụ kiểm thử

        /// <summary>
        /// Description: Phương án 4 (Mốc kép @lastTime + @lastKey) - Ca 1: Trùng mốc thời gian giữa nhiều bản ghi.
        ///              5 dòng cùng UpdateTime, phân trang pageSize = 2 (ranh giới trang rơi vào giữa nhóm trùng giờ).
        ///              Worker gửi đủ 5 dòng qua 3 trang (2+2+1), không lặp, không sót dòng nào, và mốc ShareDataLastSend
        ///              dừng chính xác ở dòng cuối cùng (UpdateTime = sameTime, LastKey = ID dòng cuối).
        /// Created date: 28/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_CompositeCursor_Case1_DuplicateTimestamp_SendsAllRowsWithoutLossOrDuplication_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-c1-{unique}";
            var packetCode = $"PKT_C1_{unique}";
            var rowPrefix = $"ROW_C1_{unique}_";
            var initialLastTime = new DateTime(2026, 10, 28, 8, 0, 0);
            var sameTime = initialLastTime.AddMinutes(10);

            var rows = Enumerable.Range(1, 5).Select(i => new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"{rowPrefix}{i}",
                Name = $"Row {i}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).OrderBy(r => r.ID).ToList();
            await db.Insertable(rows).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>()
                .SetColumns(p => p.UpdateTime == sameTime)
                .Where(p => p.Code!.StartsWith(rowPrefix))
                .ExecuteCommandAsync();

            var sql = "SELECT TOP (@pageSize) UpdateTime AS __lastTime, ID AS __lastKey, Code AS code FROM ShareDataPacket "
                + $"WHERE Code LIKE @prefix AND (@since IS NULL OR UpdateTime > @since OR (UpdateTime = @since AND ID > @afterKey)) ORDER BY UpdateTime, ID";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            SqlVariableItem Variable(string code, string source, int order) => new()
            {
                Code = code, DataType = ShareDataConst.SqlVariableBuiltinTypes[source],
                ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source, OrderNo = order
            };
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                Variable("since", ShareDataConst.SqlVariableSourceLastTime, 1),
                Variable("afterKey", ShareDataConst.SqlVariableSourceLastKey, 2),
                new SqlVariableItem { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = "2", OrderNo = 3 },
                new SqlVariableItem { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{rowPrefix}%", OrderNo = 4 }
            ]);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql, "UpdateTime");

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_C1_{unique}",
                SubCode = $"SUB_C1_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["code"] = new Dictionary<string, string> { ["$field"] = "code" } },
                ConfigureSub = s => s.LastTimeRun = initialLastTime
            });
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partner.Code,
                PacketCode = packetCode,
                LastTime = initialLastTime,
                LastKey = string.Empty
            }).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var bodies = _host.PartnerServer.ReceivedRequests.Select(r => r.Body).ToList();
            var sentPerRow = rows.ToDictionary(r => r.Code!, r => bodies.Count(b => b.Contains(r.Code!)));
            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();

            Assert.All(sentPerRow.Values, count => Assert.Equal(1, count));
            Assert.Equal(sameTime, lastSend.LastTime);
            Assert.Equal(rows.Last().ID, lastSend.LastKey);
        }

        /// <summary>
        /// Description: Phương án 4 (Mốc kép @lastTime + @lastKey) - Ca 2: Thứ tự thời gian chuẩn nhị phân DateTime vs thứ tự chuỗi.
        ///              Với các ngày rải qua các tháng (15/10/2026, 02/11/2026, 20/11/2026), nếu so sánh chuỗi theo định dạng ngày
        ///              (dd/MM/yyyy) thì "15/10" &gt; "02/11" sẽ gây nhảy cóc bỏ sót ngày 02/11. Phương án 4 dùng DateTime nhị phân
        ///              trong CSDL nên luôn bảo đảm trật tự thời gian vật lý chính xác, gửi tuần tự không nhảy cóc.
        /// Created date: 28/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_CompositeCursor_Case2_ChronologicalDateTimeOrdering_NeverSkipsLaterDates_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-c2-{unique}";
            var packetCode = $"PKT_C2_{unique}";
            var rowPrefix = $"ROW_C2_{unique}_";
            var initialLastTime = new DateTime(2026, 10, 1, 0, 0, 0);

            var dates = new[]
            {
                new DateTime(2026, 10, 15, 8, 0, 0),
                new DateTime(2026, 11, 2, 8, 0, 0),
                new DateTime(2026, 11, 20, 8, 0, 0)
            };

            var rows = dates.Select((dt, idx) => new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"{rowPrefix}{idx + 1}",
                Name = $"Row {idx + 1}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList();
            await db.Insertable(rows).ExecuteCommandAsync();

            for (var i = 0; i < rows.Count; i++)
            {
                await db.Updateable<ShareDataPacket>()
                    .SetColumns(p => p.UpdateTime == dates[i])
                    .Where(p => p.ID == rows[i].ID)
                    .ExecuteCommandAsync();
            }

            var sql = "SELECT TOP (@pageSize) UpdateTime AS __lastTime, ID AS __lastKey, Code AS code FROM ShareDataPacket "
                + $"WHERE Code LIKE @prefix AND (@since IS NULL OR UpdateTime > @since OR (UpdateTime = @since AND ID > @afterKey)) ORDER BY UpdateTime, ID";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            SqlVariableItem Variable(string code, string source, int order) => new()
            {
                Code = code, DataType = ShareDataConst.SqlVariableBuiltinTypes[source],
                ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source, OrderNo = order
            };
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                Variable("since", ShareDataConst.SqlVariableSourceLastTime, 1),
                Variable("afterKey", ShareDataConst.SqlVariableSourceLastKey, 2),
                new SqlVariableItem { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = "1", OrderNo = 3 },
                new SqlVariableItem { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{rowPrefix}%", OrderNo = 4 }
            ]);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql, "UpdateTime");

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_C2_{unique}",
                SubCode = $"SUB_C2_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["code"] = new Dictionary<string, string> { ["$field"] = "code" } },
                ConfigureSub = s => s.LastTimeRun = initialLastTime
            });
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partner.Code,
                PacketCode = packetCode,
                LastTime = initialLastTime,
                LastKey = string.Empty
            }).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert: gửi đúng 3 trang theo đúng thứ tự ngày (Row 1 -> Row 2 -> Row 3)
            var relevantBodies = _host.PartnerServer.ReceivedRequests
                .Where(r => rows.Any(row => r.Body.Contains(row.Code!)))
                .Select(r => r.Body)
                .ToList();

            Assert.Equal(3, relevantBodies.Count);
            Assert.Contains(rows[0].Code!, relevantBodies[0]);
            Assert.Contains(rows[1].Code!, relevantBodies[1]);
            Assert.Contains(rows[2].Code!, relevantBodies[2]);

            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Equal(dates[2], lastSend.LastTime);
            Assert.Equal(rows[2].ID, lastSend.LastKey);
        }

        /// <summary>
        /// Description: Phương án 4 (Mốc kép @lastTime + @lastKey) - Ca 3: Xử lý bản ghi có UpdateTime IS NULL bằng COALESCE.
        ///              Bản ghi mới Insert có UpdateTime IS NULL. Nếu dùng nối chuỗi (UpdateTime + '_' + ID), SQL Server trả về NULL
        ///              khiến WHERE bị FALSE và nuốt chửng bản ghi. Phương án 4 dùng COALESCE(UpdateTime, CreateTime) AS __lastTime
        ///              đảm bảo dòng chưa update vẫn được gửi đầy đủ và tịnh tiến mốc bình thường.
        /// Created date: 28/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_CompositeCursor_Case3_NullUpdateTime_FallsBackToCreateTimeWithoutDataLoss_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-c3-{unique}";
            var packetCode = $"PKT_C3_{unique}";
            var rowPrefix = $"ROW_C3_{unique}_";
            var initialLastTime = new DateTime(2026, 10, 28, 8, 0, 0);

            var time1 = initialLastTime.AddMinutes(10);
            var time2 = initialLastTime.AddMinutes(20);
            var time3 = initialLastTime.AddMinutes(30);

            var row1 = new ShareDataPacket { ID = Guid.NewGuid().ToString("N"), Code = $"{rowPrefix}1", Name = "Row 1", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };
            var row2NullUpdate = new ShareDataPacket { ID = Guid.NewGuid().ToString("N"), Code = $"{rowPrefix}2", Name = "Row 2", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };
            var row3 = new ShareDataPacket { ID = Guid.NewGuid().ToString("N"), Code = $"{rowPrefix}3", Name = "Row 3", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };

            await db.Insertable(new[] { row1, row2NullUpdate, row3 }).ExecuteCommandAsync();

            await db.Ado.ExecuteCommandAsync(
                "UPDATE ShareDataPacket SET CreateTime = @ct, UpdateTime = @ut WHERE ID = @id",
                new { ct = time1, ut = time1, id = row1.ID });
            // Row 2: CreateTime = time2 nhưng UpdateTime = NULL
            await db.Ado.ExecuteCommandAsync(
                "UPDATE ShareDataPacket SET CreateTime = @ct, UpdateTime = NULL WHERE ID = @id",
                new { ct = time2, id = row2NullUpdate.ID });
            await db.Ado.ExecuteCommandAsync(
                "UPDATE ShareDataPacket SET CreateTime = @ct, UpdateTime = @ut WHERE ID = @id",
                new { ct = time3, ut = time3, id = row3.ID });

            var sql = "SELECT TOP (@pageSize) COALESCE(UpdateTime, CreateTime) AS __lastTime, ID AS __lastKey, Code AS code FROM ShareDataPacket "
                + $"WHERE Code LIKE @prefix AND (@since IS NULL OR COALESCE(UpdateTime, CreateTime) > @since OR (COALESCE(UpdateTime, CreateTime) = @since AND ID > @afterKey)) "
                + "ORDER BY COALESCE(UpdateTime, CreateTime), ID";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            SqlVariableItem Variable(string code, string source, int order) => new()
            {
                Code = code, DataType = ShareDataConst.SqlVariableBuiltinTypes[source],
                ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source, OrderNo = order
            };
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                Variable("since", ShareDataConst.SqlVariableSourceLastTime, 1),
                Variable("afterKey", ShareDataConst.SqlVariableSourceLastKey, 2),
                new SqlVariableItem { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = "1", OrderNo = 3 },
                new SqlVariableItem { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{rowPrefix}%", OrderNo = 4 }
            ]);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql, "UpdateTime");

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_C3_{unique}",
                SubCode = $"SUB_C3_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["code"] = new Dictionary<string, string> { ["$field"] = "code" } },
                ConfigureSub = s => s.LastTimeRun = initialLastTime
            });
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partner.Code,
                PacketCode = packetCode,
                LastTime = initialLastTime,
                LastKey = string.Empty
            }).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var relevantBodies = _host.PartnerServer.ReceivedRequests
                .Where(r => r.Body.Contains(rowPrefix))
                .Select(r => r.Body)
                .ToList();

            Assert.Equal(3, relevantBodies.Count);
            Assert.Contains(row1.Code!, relevantBodies[0]);
            Assert.Contains(row2NullUpdate.Code!, relevantBodies[1]);
            Assert.Contains(row3.Code!, relevantBodies[2]);

            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Equal(time3, lastSend.LastTime);
            Assert.Equal(row3.ID, lastSend.LastKey);
        }

        /// <summary>
        /// Description: Phương án 4 (Mốc kép @lastTime + @lastKey) - Ca 4: Streaming phân trang nhiều đợt liên tiếp không trùng không sót.
        ///              7 bản ghi rải qua 3 mốc thời gian, pageSize = 2 -> Worker stream đủ 4 trang (2+2+2+1).
        ///              Tất cả 7 bản ghi được gửi mỗi bản ghi đúng 1 lần, mốc cuối cập nhật dòng 7. Lượt chạy kế tiếp
        ///              khi không có dữ liệu mới thì ghi nhận NoNewData và không gửi thêm request nào.
        /// Created date: 28/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_CompositeCursor_Case4_MultipleBatchStreaming_MaintainsMonotonicProgress_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-c4-{unique}";
            var packetCode = $"PKT_C4_{unique}";
            var rowPrefix = $"ROW_C4_{unique}_";
            var initialLastTime = new DateTime(2026, 10, 28, 8, 0, 0);

            var t1 = initialLastTime.AddMinutes(5);
            var t2 = initialLastTime.AddMinutes(10);
            var t3 = initialLastTime.AddMinutes(15);

            var rows = Enumerable.Range(1, 7).Select(i => new ShareDataPacket
            {
                ID = $"{i:D2}_{unique}",
                Code = $"{rowPrefix}{i}",
                Name = $"Row {i}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList();
            await db.Insertable(rows).ExecuteCommandAsync();

            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == t1).Where(p => p.ID == rows[0].ID || p.ID == rows[1].ID).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == t2).Where(p => p.ID == rows[2].ID || p.ID == rows[3].ID).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == t3).Where(p => p.ID == rows[4].ID || p.ID == rows[5].ID || p.ID == rows[6].ID).ExecuteCommandAsync();

            var sql = "SELECT TOP (@pageSize) UpdateTime AS __lastTime, ID AS __lastKey, Code AS code FROM ShareDataPacket "
                + $"WHERE Code LIKE @prefix AND (@since IS NULL OR UpdateTime > @since OR (UpdateTime = @since AND ID > @afterKey)) ORDER BY UpdateTime, ID";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            SqlVariableItem Variable(string code, string source, int order) => new()
            {
                Code = code, DataType = ShareDataConst.SqlVariableBuiltinTypes[source],
                ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source, OrderNo = order
            };
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                Variable("since", ShareDataConst.SqlVariableSourceLastTime, 1),
                Variable("afterKey", ShareDataConst.SqlVariableSourceLastKey, 2),
                new SqlVariableItem { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = "2", OrderNo = 3 },
                new SqlVariableItem { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{rowPrefix}%", OrderNo = 4 }
            ]);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql, "UpdateTime");

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_C4_{unique}",
                SubCode = $"SUB_C4_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["code"] = new Dictionary<string, string> { ["$field"] = "code" } },
                ConfigureSub = s => s.LastTimeRun = initialLastTime
            });
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partner.Code,
                PacketCode = packetCode,
                LastTime = initialLastTime,
                LastKey = string.Empty
            }).ExecuteCommandAsync();

            var worker = CreateWorker(scope);

            // Act 1: chạy streaming đợt 1 -> gửi 7 dòng qua 4 trang (2+2+2+1)
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 1
            var bodiesRun1 = _host.PartnerServer.ReceivedRequests
                .Where(r => r.Body.Contains(rowPrefix))
                .Select(r => r.Body)
                .ToList();
            Assert.Equal(4, bodiesRun1.Count);
            foreach (var row in rows)
            {
                Assert.Equal(1, bodiesRun1.Count(b => b.Contains(row.Code!)));
            }

            var lastSendRun1 = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Equal(t3, lastSendRun1.LastTime);
            Assert.Equal(rows[6].ID, lastSendRun1.LastKey);

            // Act 2: kích hoạt chạy lượt tiếp theo khi không phát sinh dòng mới
            var dueNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == dueNow.AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 2: không gửi thêm request HTTP nào, ghi nhận NoNewData
            var bodiesRun2 = _host.PartnerServer.ReceivedRequests
                .Where(r => r.Body.Contains(rowPrefix))
                .ToList();
            Assert.Equal(4, bodiesRun2.Count);

            var latestLog = (await GetLogs(db, sub.ID)).First();
            Assert.Equal(ShareDataEnum.ExportMessage.NoNewData, latestLog.ErrorMessage);
        }

        /// <summary>
        /// Description: Phương án 4 (Mốc kép @lastTime + @lastKey) - Ca 5: Khóa ID bất biến bảo vệ mốc con trỏ trước đột biến mã nghiệp vụ (Code).
        ///              Ở phương án ghép chuỗi (UpdateTime + '_' + Code), nếu bản ghi bị đổi tên hoặc mã mới nhỏ hơn mã cũ theo từ điển
        ///              thì sẽ bị sót vĩnh viễn. Phương án 4 dùng ID (khóa bất biến) làm tie-breaker:
        ///              - Đợt 1: gửi Row 1 và Row 2. Mốc lưu LastKey = ID Row 2.
        ///              - Giữa chừng: Row 3 và Row 4 được chèn với Code có thứ tự từ điển nhỏ hơn Row 2 (vd "AAA_...").
        ///              - Đợt 2: Worker vẫn quét trúng Row 3 và Row 4 nhờ ID tăng dần, gửi đủ 4 dòng không sót không lặp.
        /// Created date: 28/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_CompositeCursor_Case5_CodeRenaming_CursorContinuesSeamlesslyUsingImmutableId_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-c5-{unique}";
            var packetCode = $"PKT_C5_{unique}";
            var initialLastTime = new DateTime(2026, 10, 28, 8, 0, 0);
            var sameTime = initialLastTime.AddMinutes(10);

            var row1 = new ShareDataPacket { ID = $"1_{unique}", Code = $"ZZZ_{unique}_1", Name = "Row 1", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };
            var row2 = new ShareDataPacket { ID = $"2_{unique}", Code = $"ZZZ_{unique}_2", Name = "Row 2", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };
            await db.Insertable(new[] { row1, row2 }).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == sameTime).Where(p => p.ID == row1.ID || p.ID == row2.ID).ExecuteCommandAsync();

            var sql = "SELECT TOP (@pageSize) UpdateTime AS __lastTime, ID AS __lastKey, Code AS code FROM ShareDataPacket "
                + $"WHERE Code LIKE @prefix AND (@since IS NULL OR UpdateTime > @since OR (UpdateTime = @since AND ID > @afterKey)) ORDER BY UpdateTime, ID";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            SqlVariableItem Variable(string code, string source, int order) => new()
            {
                Code = code, DataType = ShareDataConst.SqlVariableBuiltinTypes[source],
                ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source, OrderNo = order
            };
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                Variable("since", ShareDataConst.SqlVariableSourceLastTime, 1),
                Variable("afterKey", ShareDataConst.SqlVariableSourceLastKey, 2),
                new SqlVariableItem { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = "10", OrderNo = 3 },
                new SqlVariableItem { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"%{unique}%", OrderNo = 4 }
            ]);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql, "UpdateTime");

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_C5_{unique}",
                SubCode = $"SUB_C5_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["code"] = new Dictionary<string, string> { ["$field"] = "code" } },
                ConfigureSub = s => s.LastTimeRun = initialLastTime
            });
            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partner.Code,
                PacketCode = packetCode,
                LastTime = initialLastTime,
                LastKey = string.Empty
            }).ExecuteCommandAsync();

            var worker = CreateWorker(scope);

            // Act 1: Chạy đợt 1 -> gửi Row 1 và Row 2
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 1: Đã nhận Row 1 và Row 2, mốc LastKey dừng ở "2_..."
            var lastSendRun1 = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Equal(sameTime, lastSendRun1.LastTime);
            Assert.Equal(row2.ID, lastSendRun1.LastKey);

            // Act 2: Thêm Row 3 và Row 4 cùng mốc sameTime, ID lớn hơn ("3_...", "4_...") nhưng Code bắt đầu bằng "AAA_" (nhỏ hơn từ điển)
            var row3 = new ShareDataPacket { ID = $"3_{unique}", Code = $"AAA_{unique}_3", Name = "Row 3", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };
            var row4 = new ShareDataPacket { ID = $"4_{unique}", Code = $"AAA_{unique}_4", Name = "Row 4", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };
            await db.Insertable(new[] { row3, row4 }).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == sameTime).Where(p => p.ID == row3.ID || p.ID == row4.ID).ExecuteCommandAsync();

            var dueNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == dueNow.AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 2: Nhận đủ Row 3 và Row 4, không gửi lặp Row 1, 2
            var allBodies = _host.PartnerServer.ReceivedRequests
                .Where(r => r.Body.Contains(unique))
                .Select(r => r.Body)
                .ToList();

            Assert.Equal(2, allBodies.Count);
            Assert.Contains(row1.Code!, allBodies[0]);
            Assert.Contains(row2.Code!, allBodies[0]);
            Assert.Contains(row3.Code!, allBodies[1]);
            Assert.Contains(row4.Code!, allBodies[1]);

            var lastSendRun2 = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Equal(sameTime, lastSendRun2.LastTime);
            Assert.Equal(row4.ID, lastSendRun2.LastKey);
        }

        /// <summary>
        /// Description: Phương án 4 (Mốc kép @lastTime + @lastKey) - Ca 6: Chế độ con trỏ đơn (Single-Key Fallback Mode).
        ///              Với các bảng dữ liệu chỉ có ID hoặc Sequence tăng dần (không có cột thời gian UpdateTime):
        ///              - Gói tin chỉ cấu hình biến @afterKey (Source = lastKey), không có biến lastTime.
        ///              - Câu SQL ORDER BY ID (chỉ sắp theo khóa).
        ///              - Worker tự động nhận diện chế độ con trỏ đơn, không ép buộc có __lastTime, tịnh tiến LastKey mượt mà,
        ///                lọc sạch cột hệ thống __lastKey khỏi payload JSON và không gửi lặp.
        /// Created date: 28/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_CompositeCursor_Case6_SingleKeyFallback_RunsPureIncrementalCursor_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-c6-{unique}";
            var packetCode = $"PKT_C6_{unique}";
            var rowPrefix = $"ROW_C6_{unique}_";

            var rows = Enumerable.Range(1, 4).Select(i => new ShareDataPacket
            {
                ID = $"K{i}_{unique}",
                Code = $"{rowPrefix}{i}",
                Name = $"Row {i}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList();
            await db.Insertable(rows).ExecuteCommandAsync();

            var sql = "SELECT TOP (@pageSize) ID AS __lastKey, Code AS code FROM ShareDataPacket "
                + $"WHERE Code LIKE @prefix AND (@afterKey IS NULL OR ID > @afterKey) ORDER BY ID";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();

            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                new SqlVariableItem
                {
                    Code = "afterKey",
                    DataType = ShareDataConst.SqlVariableTypeString,
                    ValueKind = ShareDataConst.SqlVariableKindBuiltin,
                    Value = ShareDataConst.SqlVariableSourceLastKey,
                    OrderNo = 1
                },
                new SqlVariableItem { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = "2", OrderNo = 2 },
                new SqlVariableItem { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{rowPrefix}%", OrderNo = 3 }
            ]);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_C6_{unique}",
                SubCode = $"SUB_C6_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["code"] = new Dictionary<string, string> { ["$field"] = "code" } }
            });

            var worker = CreateWorker(scope);

            // Act 1: Chạy đợt 1 -> gửi 4 dòng qua 2 trang (2 + 2)
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 1
            var relevantBodies = _host.PartnerServer.ReceivedRequests
                .Where(r => r.Body.Contains(rowPrefix))
                .Select(r => r.Body)
                .ToList();

            Assert.Equal(2, relevantBodies.Count);
            Assert.All(relevantBodies, body => Assert.DoesNotContain("__lastKey", body, StringComparison.OrdinalIgnoreCase));
            foreach (var row in rows)
            {
                Assert.Equal(1, relevantBodies.Count(b => b.Contains(row.Code!)));
            }

            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Equal(rows[3].ID, lastSend.LastKey);
            Assert.NotNull(lastSend.LastTime);

            // Act 2: Lượt kế không có dữ liệu mới -> ghi nhận NoNewData
            var dueNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == dueNow.AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 2
            var latestLog = (await GetLogs(db, sub.ID)).First();
            Assert.Equal(ShareDataEnum.ExportMessage.NoNewData, latestLog.ErrorMessage);
        }

        /// <summary>
        /// Description: Gói nối đuôi 2 cột, @lastTime có DefaultValue = T; chưa có ShareDataLastSend thì chỉ gửi từ T trở đi,
        ///              sau đó ResetLastSend thì lượt kế tiếp lại bắt đầu quét từ T.
        /// Created date: 29/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_TwoColumnCursor_WithLastTimeDefaultValue_FiltersFromCutoffAndRestartsOnReset_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-2col-{unique}";
            var packetCode = $"PKT_2COL_{unique}";
            var rowPrefix = $"ROW_2COL_{unique}_";
            const string cutoffStr = "2026-10-27 09:00:00";

            var oldRow = new ShareDataPacket { ID = $"old_{unique}", Code = $"{rowPrefix}OLD", Name = "Old", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };
            var newRow1 = new ShareDataPacket { ID = $"new1_{unique}", Code = $"{rowPrefix}NEW1", Name = "New 1", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };
            var newRow2 = new ShareDataPacket { ID = $"new2_{unique}", Code = $"{rowPrefix}NEW2", Name = "New 2", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };

            await db.Insertable(new List<ShareDataPacket> { oldRow, newRow1, newRow2 }).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == new DateTime(2026, 10, 27, 8, 30, 0)).Where(p => p.ID == oldRow.ID).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == new DateTime(2026, 10, 27, 9, 10, 0)).Where(p => p.ID == newRow1.ID).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == new DateTime(2026, 10, 27, 9, 20, 0)).Where(p => p.ID == newRow2.ID).ExecuteCommandAsync();

            var commonSql = "SELECT TOP 50 Code AS rowCode, UpdateTime AS __lastTime, ID AS __lastKey FROM ShareDataPacket "
                + "WHERE Code LIKE @prefix AND (UpdateTime > @lastTime OR (UpdateTime = @lastTime AND ID > @lastKey)) ORDER BY UpdateTime ASC, ID ASC";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, commonSql);
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null,
            [
                new SqlVariableItem
                {
                    Code = "lastTime",
                    DataType = ShareDataConst.SqlVariableTypeDateTime,
                    ValueKind = ShareDataConst.SqlVariableKindBuiltin,
                    Value = ShareDataConst.SqlVariableSourceLastTime,
                    DefaultValue = cutoffStr,
                    OrderNo = 1
                },
                new SqlVariableItem
                {
                    Code = "lastKey",
                    DataType = ShareDataConst.SqlVariableTypeString,
                    ValueKind = ShareDataConst.SqlVariableKindBuiltin,
                    Value = ShareDataConst.SqlVariableSourceLastKey,
                    OrderNo = 2
                },
                new SqlVariableItem
                {
                    Code = "prefix",
                    DataType = ShareDataConst.SqlVariableTypeString,
                    ValueKind = ShareDataConst.SqlVariableKindConst,
                    Value = $"{rowPrefix}%",
                    OrderNo = 3
                }
            ]);

            var shape = new Dictionary<string, object> { ["rowCode"] = new Dictionary<string, string> { ["$field"] = "rowCode" } };
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P2C_{unique}",
                SubCode = $"SUB_2C_{unique}",
                DatatypeId = packetId,
                MappingShape = shape,
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/partner-2c-{unique}"
            });

            var worker = CreateWorker(scope);

            // Act 1: Chưa có mốc chạy -> gửi từ cutoff (NEW1, NEW2), bỏ qua OLD
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 1
            var requestsRun1 = _host.PartnerServer.ReceivedRequests.Where(r => r.Path.Contains($"partner-2c-{unique}")).Select(r => r.Body).ToList();
            Assert.Single(requestsRun1);
            Assert.DoesNotContain($"{rowPrefix}OLD", requestsRun1[0]);
            Assert.Contains($"{rowPrefix}NEW1", requestsRun1[0]);
            Assert.Contains($"{rowPrefix}NEW2", requestsRun1[0]);

            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.NotNull(lastSend);
            Assert.Equal(new DateTime(2026, 10, 27, 9, 20, 0), lastSend.LastTime);
            Assert.Equal(newRow2.ID, lastSend.LastKey);

            // Act 2: Đặt lại mốc chạy (ResetLastSend) và chạy tiếp -> lại bắt đầu từ cutoff
            var httpContextAccessor = _host.Services.GetRequiredService<IHttpContextAccessor>();
            httpContextAccessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimConst.AccountType, ((int)BaseEnums.AccountTypeEnum.SuperAdmin).ToString()),
                    new Claim(ClaimConst.Account, $"super_{unique}"),
                    new Claim(ClaimConst.UserId, Guid.NewGuid().ToString("N")),
                    new Claim(ClaimTypes.Name, $"super_{unique}")
                ], "TestAuth"))
            };
            await scope.ServiceProvider.CallApiAsync(new ResetLastSendInput { PacketCode = packetCode, PartnerId = partner.ID! });

            var dueNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.NextTimeRun == dueNow.AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 2
            var requestsAfterReset = _host.PartnerServer.ReceivedRequests.Where(r => r.Path.Contains($"partner-2c-{unique}")).Select(r => r.Body).ToList();
            Assert.Equal(2, requestsAfterReset.Count);
            Assert.DoesNotContain($"{rowPrefix}OLD", requestsAfterReset[1]);
            Assert.Contains($"{rowPrefix}NEW1", requestsAfterReset[1]);
            Assert.Contains($"{rowPrefix}NEW2", requestsAfterReset[1]);
        }

        #endregion

        #region 14. Mốc nối đuôi khi người dùng tự viết câu SQL — tái hiện lỗi (29/10/2026)

        /// <summary>
        /// Description: Đổi kiểu câu SQL nối đuôi giữa hai lượt gửi làm mốc cũ bị dùng sai nghĩa, vì bảng mốc chỉ có hai cột
        ///              cố định LastTime / LastKey dùng chung cho mọi kiểu câu:
        ///              - chiKhoaSang2Cot: câu chỉ dùng @lastKey vẫn ghi LastTime = giờ chạy; đổi sang câu 2 cột thì @lastTime nhận
        ///                giờ chạy đó (mốc khởi tạo của biến bị bỏ qua) nên các dòng còn tồn chưa gửi bị bỏ sót.
        ///              - haiCotSangChiKhoa: LastKey đang là ID gốc (bắt đầu bằng chữ như GUID) đem so với chuỗi ghép thời gian + ID
        ///                bắt đầu bằng năm "2026-..." nên câu không trả dòng nào, mọi dòng sau đó không bao giờ được gửi.
        ///              Kỳ vọng: không dòng nào bị bỏ sót; gửi lại dòng đã gửi thì chấp nhận được.
        /// Created date: 29/10/2026
        /// </summary>
        [Theory]
        [InlineData("chiKhoaSang2Cot")]
        [InlineData("haiCotSangChiKhoa")]
        public async Task ProcessSubscriptions_WhenCursorSqlSwitchesMode_SendsEveryRowWithoutLoss_Test(string direction)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-mode-{unique}";
            var packetCode = $"PKT_MODE_{unique}";
            var rowPrefix = $"ROW_MODE_{unique}_";
            var startsKeyOnly = direction == "chiKhoaSang2Cot";
            var keyOnlySql = "SELECT TOP (@pageSize) Code AS rowCode, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey FROM ShareDataPacket "
                + "WHERE Code LIKE @prefix AND CONVERT(varchar(23), UpdateTime, 121) + '_' + ID > @lastKey ORDER BY __lastKey";
            var twoColumnSql = "SELECT TOP (@pageSize) Code AS rowCode, UpdateTime AS __lastTime, ID AS __lastKey FROM ShareDataPacket "
                + "WHERE Code LIKE @prefix AND UpdateTime >= ISNULL(@lastTime, '17530101') AND (@lastTime IS NULL OR UpdateTime > @lastTime OR ID > @lastKey) "
                + "ORDER BY UpdateTime ASC, ID ASC";

            // Dữ liệu cũ hơn giờ chạy (hôm qua 08:00, cách 10 phút): dòng 1–4 có sẵn, dòng 5 phát sinh sau khi đổi câu
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = dbNow.Date.AddDays(-1).AddHours(8);
            var lastTimeDefault = baseTime.Date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var rows = Enumerable.Range(1, 5).Select(i => new ShareDataPacket
            {
                ID = $"k{i}_{unique}",
                Code = $"{rowPrefix}{i}",
                Name = $"Row {i}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList();
            await db.Insertable(rows.Take(4).ToList()).ExecuteCommandAsync();
            for (var i = 0; i < 4; i++)
            {
                var rowId = rows[i].ID;
                var rowTime = baseTime.AddMinutes(i * 10);
                await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == rowTime).Where(p => p.ID == rowId).ExecuteCommandAsync();
            }

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, CursorVariables(2, rowPrefix, withLastTime: !startsKeyOnly, lastTimeDefault));
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, startsKeyOnly ? keyOnlySql : twoColumnSql);

            var (_, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_MODE_{unique}",
                SubCode = $"SUB_MODE_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["rowCode"] = new Dictionary<string, string> { ["$field"] = "rowCode" } },
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/partner-mode-{unique}"
            });
            // Mỗi lượt chỉ gửi một trang để còn dòng tồn lúc người dùng đổi câu
            var worker = CreateWorker(scope, workerConfig: await CreateWorkerConfigWithMaxPages(scope, db, 1));

            // Act 1: lượt đầu gửi trang 1 (dòng 1, 2) theo kiểu câu ban đầu
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 1
            Assert.Contains(PartnerBodies($"partner-mode-{unique}"), b => b.Contains($"\"{rows[1].Code}\""));

            // Act 2: người dùng đổi sang kiểu câu kia, dòng 5 phát sinh sau đó; Worker chạy thêm 3 lượt
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, CursorVariables(2, rowPrefix, withLastTime: startsKeyOnly, lastTimeDefault));
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, startsKeyOnly ? twoColumnSql : keyOnlySql);
            await db.Insertable(rows[4]).ExecuteCommandAsync();
            var row5Id = rows[4].ID;
            var row5Time = baseTime.AddMinutes(40);
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == row5Time).Where(p => p.ID == row5Id).ExecuteCommandAsync();
            for (var run = 0; run < 3; run++)
            {
                await MakeSubscriptionDue(db, sub.ID);
                await worker.ProcessSubscriptions(CancellationToken.None);
            }

            // Assert 2
            var bodies = PartnerBodies($"partner-mode-{unique}");
            var missing = rows.Select(r => r.Code!).Where(code => !bodies.Any(b => b.Contains($"\"{code}\""))).ToList();
            Assert.True(missing.Count == 0, $"Đổi kiểu câu ({direction}) làm bỏ sót: {string.Join(", ", missing)}");
        }

        /// <summary>
        /// Description: Một câu SQL gộp hai nguồn, mỗi nguồn một nhóm mốc — bảng (gói, đối tác, key) → value (biên bản 27/10/2026 Phần 4
        ///              [03:06 – 03:15]: "nhiều tình huống trong cùng một câu có hai ba câu điều kiện khác nhau"): nguồn 1 là bảng gói tin
        ///              (nhóm pkt), nguồn 2 là bảng giá thu phí (TollPriceList, nhóm toll) đồng bộ định kỳ từ hệ thống thu phí nên về trễ.
        ///              Dòng giá 09:20 tới sau khi nguồn 1 đã gửi tới 09:30 vẫn được gửi vì nguồn 2 giữ mốc riêng; dòng đã gửi không gửi lại.
        /// Created date: 29/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSqlMergesTwoSourcesWithCursorGroups_SendsLateSourceRowOnce_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-2src-{unique}";
            var packetCode = $"PKT_2SRC_{unique}";
            var rowPrefix = $"ROW_2SRC_{unique}_";

            var a1 = new ShareDataPacket { ID = $"a1_{unique}", Code = $"{rowPrefix}A1", Name = "A1", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };
            var a2 = new ShareDataPacket { ID = $"a2_{unique}", Code = $"{rowPrefix}A2", Name = "A2", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable };
            await db.Insertable(new List<ShareDataPacket> { a1, a2 }).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == new DateTime(2026, 10, 27, 9, 0, 0)).Where(p => p.ID == a1.ID).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == new DateTime(2026, 10, 27, 9, 30, 0)).Where(p => p.ID == a2.ID).ExecuteCommandAsync();

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, GroupCursorVariables(50, rowPrefix, "pkt", "toll"));
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, TwoSourceGroupSql);

            var (_, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_2SRC_{unique}",
                SubCode = $"SUB_2SRC_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["rowCode"] = new Dictionary<string, string> { ["$field"] = "rowCode" } },
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/partner-2src-{unique}"
            });
            var worker = CreateWorker(scope);

            // Act 1: nguồn 1 có dòng 09:00 và 09:30, nguồn 2 chưa đồng bộ về
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 1
            Assert.Contains(PartnerBodies($"partner-2src-{unique}"), b => b.Contains($"\"{a2.Code}\""));

            // Act 2: bảng giá đồng bộ về sau, dòng mang giờ 09:20 bên hệ thống thu phí
            var priceCode = $"{rowPrefix}PRICE";
            await db.Insertable(new TollPriceList
            {
                ID = Guid.NewGuid().ToString("N"),
                SourceId = priceCode,
                Name = "Bảng giá về trễ",
                SourceWriteDate = new DateTime(2026, 10, 27, 9, 20, 0)
            }).ExecuteCommandAsync();
            await MakeSubscriptionDue(db, sub.ID);
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert 2: dòng nguồn về trễ được gửi, dòng nguồn 1 không gửi lại; mỗi nhóm giữ mốc riêng
            var allBodies = string.Join(" ", PartnerBodies($"partner-2src-{unique}"));
            Assert.True(allBodies.Contains($"\"{priceCode}\""), $"Dòng của nguồn về trễ ({priceCode}, 09:20) bị bỏ");
            Assert.Equal(1, CountOccurrences(allBodies, $"\"{a1.Code}\""));
            Assert.Equal(1, CountOccurrences(allBodies, $"\"{a2.Code}\""));
            var groupCursors = await db.Queryable<ShareDataLastSendKey>().Where(k => k.PacketCode == packetCode).ToListAsync();
            Assert.Equal(a2.ID, groupCursors.Single(k => k.CursorKey == "pkt").LastKey);
            Assert.Equal(new DateTime(2026, 10, 27, 9, 20, 0), groupCursors.Single(k => k.CursorKey == "toll").LastTime);
        }

        /// <summary>
        /// Description: Đặt mốc riêng cho một nhóm mốc qua API SetLastSend (CursorKey = toll): lượt gửi chỉ lấy dòng của nguồn đó sau mốc,
        ///              nguồn còn lại gửi từ đầu. Đặt lại mốc (ResetLastSend) xóa mốc của mọi nhóm nên lượt sau gửi lại cả hai nguồn từ đầu.
        /// Created date: 29/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenCursorGroupSetViaApi_SendsThatSourceAfterCursorUntilReset_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-grp-{unique}";
            var packetCode = $"PKT_GRP_{unique}";
            var rowPrefix = $"ROW_GRP_{unique}_";
            var baseTime = new DateTime(2026, 10, 27, 9, 0, 0);

            var packets = Enumerable.Range(1, 3).Select(i => new ShareDataPacket
            {
                ID = $"p{i}_{unique}",
                Code = $"{rowPrefix}P{i}",
                Name = $"P{i}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList();
            await db.Insertable(packets).ExecuteCommandAsync();
            for (var i = 0; i < packets.Count; i++)
            {
                var rowId = packets[i].ID;
                var rowTime = baseTime.AddMinutes(i * 10);
                await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == rowTime).Where(p => p.ID == rowId).ExecuteCommandAsync();
            }
            var prices = Enumerable.Range(1, 3).Select(i => new TollPriceList
            {
                ID = $"t{i}_{unique}",
                SourceId = $"{rowPrefix}T{i}",
                Name = $"T{i}",
                SourceWriteDate = baseTime.AddMinutes((i - 1) * 10)
            }).ToList();
            await db.Insertable(prices).ExecuteCommandAsync();

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, GroupCursorVariables(50, rowPrefix, "pkt", "toll"));
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, TwoSourceGroupSql);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_GRP_{unique}",
                SubCode = $"SUB_GRP_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["rowCode"] = new Dictionary<string, string> { ["$field"] = "rowCode" } },
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/partner-grp-{unique}"
            });
            UseSuperAdmin(unique);
            var worker = CreateWorker(scope);

            // Act 1: đặt mốc nhóm toll tại dòng T2 (09:10) rồi chạy
            await scope.ServiceProvider.CallApiAsync(new SetLastSendInput
            {
                PacketCode = packetCode,
                PartnerId = partner.ID,
                CursorKey = "toll",
                LastTime = baseTime.AddMinutes(10).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                LastKey = prices[1].ID
            });
            await worker.ProcessSubscriptions(CancellationToken.None);
            var firstRun = string.Join(" ", PartnerBodies($"partner-grp-{unique}"));

            // Act 2: đặt lại mốc rồi chạy tiếp
            await scope.ServiceProvider.CallApiAsync(new ResetLastSendInput { PacketCode = packetCode, PartnerId = partner.ID! });
            var groupRowsAfterReset = await db.Queryable<ShareDataLastSendKey>()
                .Where(k => k.PartnerCode == partner.Code && k.PacketCode == packetCode)
                .CountAsync();
            await MakeSubscriptionDue(db, sub.ID);
            await worker.ProcessSubscriptions(CancellationToken.None);
            var allRuns = string.Join(" ", PartnerBodies($"partner-grp-{unique}"));

            // Assert 1: nguồn pkt gửi đủ từ đầu, nguồn toll chỉ gửi sau mốc T2
            Assert.All(packets, p => Assert.Contains($"\"{p.Code}\"", firstRun));
            Assert.DoesNotContain($"\"{prices[0].SourceId}\"", firstRun);
            Assert.DoesNotContain($"\"{prices[1].SourceId}\"", firstRun);
            Assert.Contains($"\"{prices[2].SourceId}\"", firstRun);

            // Assert 2: đặt lại xóa mốc mọi nhóm, lượt sau gửi lại cả hai nguồn từ đầu
            Assert.Equal(0, groupRowsAfterReset);
            Assert.Contains($"\"{prices[0].SourceId}\"", allRuns);
            Assert.Equal(2, CountOccurrences(allRuns, $"\"{packets[0].Code}\""));
        }

        /// <summary>
        /// Description: Câu 2 cột mốc nạp thẳng vào CSDL (không qua bộ kiểm lúc lưu) mà WHERE không so cả cặp (thời gian, khóa) như chuỗi ghép
        ///              của PA1: so thời gian AND khóa thì bỏ dòng giờ mới hơn mà ID nhỏ hơn, chỉ so thời gian thì bỏ dòng trùng giờ với dòng cuối.
        ///              Worker phải dừng trước khi gửi và ghi cảnh báo CursorOrderInvalid, thay vì gửi rồi bỏ sót.
        /// Created date: 29/10/2026
        /// </summary>
        [Theory]
        [InlineData("soThoiGianAndKhoa", "Code LIKE @prefix AND UpdateTime >= ISNULL(@lastTime, '17530101') AND ID > @lastKey")]
        [InlineData("chiSoThoiGian", "Code LIKE @prefix AND (@lastTime IS NULL OR UpdateTime > @lastTime)")]
        public async Task ProcessSubscriptions_WhenCursorFilterSkipsTimeKeyPair_StopsWithAlertInsteadOfSending_Test(string caseName, string where)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-flt-{unique}";
            var packetCode = $"PKT_FLT_{unique}";
            var rowPrefix = $"ROW_FLT_{unique}_";
            var sql = $"SELECT TOP (@pageSize) Code AS rowCode, UpdateTime AS __lastTime, ID AS __lastKey FROM ShareDataPacket WHERE {where} ORDER BY UpdateTime ASC, ID ASC";

            await db.Insertable(Enumerable.Range(1, 3).Select(i => new ShareDataPacket
            {
                ID = $"f{i}_{unique}",
                Code = $"{rowPrefix}{i}",
                Name = $"F{i}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList()).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>()
                .SetColumns(p => p.UpdateTime == new DateTime(2026, 10, 27, 9, 0, 0))
                .Where(p => p.Code!.StartsWith(rowPrefix))
                .ExecuteCommandAsync();
            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, CursorVariables(10, rowPrefix, withLastTime: true));
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);

            var (_, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_FLT_{unique}",
                SubCode = $"SUB_FLT_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["rowCode"] = new Dictionary<string, string> { ["$field"] = "rowCode" } },
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/partner-flt-{unique}"
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var sentCount = PartnerBodies($"partner-flt-{unique}").Count;
            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.CursorOrderInvalid)
                .CountAsync();
            Assert.True(sentCount == 0, $"Câu lọc thiếu cặp (thời gian, khóa) ({caseName}) vẫn được gửi {sentCount} lần");
            Assert.Equal(1, alerts);
        }

        /// <summary>
        /// Description: Khóa nối đuôi kiểu số ở chế độ 2 cột mốc: câu SQL so OrderNo > @lastKey theo SỐ, còn chốt chặn ghi mốc
        ///              (CommitSuccess) so ShareDataLastSend.LastKey theo CHUỖI nên '18' bị coi là nhỏ hơn '9'.
        ///              20 dòng cùng một thời điểm (ghi hàng loạt), trang 9 dòng: trang 2 dừng ở khóa 18 nên mốc không được ghi,
        ///              lượt nào cũng gửi lại trang 2, khóa 19–20 kẹt mãi.
        ///              Đối chứng pa1 (chuỗi ghép thời gian + khóa đệm 0, chỉ dùng @lastKey): so chuỗi ở mọi chỗ nên chạy đúng.
        ///              Kỳ vọng: mỗi dòng tới đối tác đúng một lần.
        /// Created date: 29/10/2026
        /// </summary>
        [Theory]
        [InlineData("haiCot")]
        [InlineData("pa1")]
        public async Task ProcessSubscriptions_WhenCursorKeyIsNumeric_SendsEachRowExactlyOnce_Test(string sqlStyle)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-num-{unique}";
            var packetCode = $"PKT_NUM_{unique}";
            var rowPrefix = $"ROW_NUM_{unique}_";
            var isTwoColumn = sqlStyle == "haiCot";
            var twoColumnSql = "SELECT TOP (@pageSize) Code AS rowCode, UpdateTime AS __lastTime, OrderNo AS __lastKey FROM ShareDataPacket "
                + "WHERE Code LIKE @prefix AND UpdateTime >= ISNULL(@lastTime, '17530101') AND (@lastTime IS NULL OR UpdateTime > @lastTime OR OrderNo > @lastKey) "
                + "ORDER BY UpdateTime ASC, OrderNo ASC";
            var keyExpr = "CONVERT(varchar(23), UpdateTime, 121) + '_' + RIGHT('0000000000' + CAST(OrderNo AS varchar(10)), 10)";
            var pa1Sql = $"SELECT TOP (@pageSize) Code AS rowCode, {keyExpr} AS __lastKey FROM ShareDataPacket "
                + $"WHERE Code LIKE @prefix AND {keyExpr} > @lastKey ORDER BY __lastKey";

            var rows = Enumerable.Range(1, 20).Select(i => new ShareDataPacket
            {
                ID = $"n{i}_{unique}",
                Code = $"{rowPrefix}N{i:D2}",
                Name = $"Row {i}",
                OrderNo = i,
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList();
            await db.Insertable(rows).ExecuteCommandAsync();
            var batchTime = new DateTime(2026, 10, 27, 8, 0, 0);
            var rowIds = rows.Select(r => r.ID).ToList();
            await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == batchTime).Where(p => rowIds.Contains(p.ID)).ExecuteCommandAsync();

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, CursorVariables(9, rowPrefix, withLastTime: isTwoColumn));
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, isTwoColumn ? twoColumnSql : pa1Sql);

            var (_, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_NUM_{unique}",
                SubCode = $"SUB_NUM_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["rowCode"] = new Dictionary<string, string> { ["$field"] = "rowCode" } },
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/partner-num-{unique}"
            });
            var worker = CreateWorker(scope, workerConfig: await CreateWorkerConfigWithMaxPages(scope, db, 3));

            // Act: 3 lượt gửi liên tiếp
            await worker.ProcessSubscriptions(CancellationToken.None);
            for (var run = 0; run < 2; run++)
            {
                await MakeSubscriptionDue(db, sub.ID);
                await worker.ProcessSubscriptions(CancellationToken.None);
            }

            // Assert
            var bodies = PartnerBodies($"partner-num-{unique}");
            var wrongCounts = rows
                .Select(r => (Code: r.Code!, Count: bodies.Sum(b => CountOccurrences(b, $"\"{r.Code}\""))))
                .Where(x => x.Count != 1)
                .Select(x => $"{x.Code[rowPrefix.Length..]}×{x.Count}")
                .ToList();
            Assert.True(wrongCounts.Count == 0, $"Khóa kiểu số ({sqlStyle}) gửi sai số lần: {string.Join(", ", wrongCounts)}");
        }

        /// <summary>
        /// Description: Đặt mốc gửi dữ liệu cho đối tác qua API SetLastSend: mốc được lưu vào ShareDataLastSend
        ///              kèm chữ ký công thức cột mốc, LastTimeRun của đăng ký bị xóa, và lượt gửi kế tiếp của Worker
        ///              chỉ trích xuất và gửi các dòng phát sinh sau mốc đã đặt.
        /// Created date: 29/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenCursorIsSetViaSetLastSendApi_SendsOnlySubsequentRows_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-setcur-{unique}";
            var packetCode = $"PKT_SETCUR_{unique}";
            var rowPrefix = $"ROW_SETCUR_{unique}_";
            var twoColumnSql = "SELECT TOP (@pageSize) Code AS rowCode, UpdateTime AS __lastTime, ID AS __lastKey FROM ShareDataPacket "
                + "WHERE Code LIKE @prefix AND UpdateTime >= ISNULL(@lastTime, '17530101') AND (@lastTime IS NULL OR UpdateTime > @lastTime OR ID > @lastKey) "
                + "ORDER BY UpdateTime ASC, ID ASC";

            var baseTime = new DateTime(2026, 10, 28, 8, 0, 0);
            var rows = Enumerable.Range(1, 6).Select(i => new ShareDataPacket
            {
                ID = $"c{i:D2}_{unique}",
                Code = $"{rowPrefix}{i}",
                Name = $"Row {i}",
                OrderNo = i,
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList();
            await db.Insertable(rows).ExecuteCommandAsync();
            for (var i = 0; i < rows.Count; i++)
            {
                var rowId = rows[i].ID;
                var rowTime = baseTime.AddMinutes(i * 10);
                await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == rowTime).Where(p => p.ID == rowId).ExecuteCommandAsync();
            }

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, CursorVariables(10, rowPrefix, withLastTime: true));
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, twoColumnSql);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_SETCUR_{unique}",
                SubCode = $"SUB_SETCUR_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["rowCode"] = new Dictionary<string, string> { ["$field"] = "rowCode" } },
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/partner-setcur-{unique}"
            });

            // Act 1: Đặt mốc tại dòng 3 (thời điểm baseTime + 20 phút, khóa c03_unique) qua API SetLastSend
            var httpContextAccessor = _host.Services.GetRequiredService<IHttpContextAccessor>();
            httpContextAccessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimConst.AccountType, ((int)BaseEnums.AccountTypeEnum.SuperAdmin).ToString()),
                    new Claim(ClaimConst.Account, $"super_{unique}"),
                    new Claim(ClaimConst.UserId, Guid.NewGuid().ToString("N")),
                    new Claim(ClaimTypes.Name, $"super_{unique}")
                ], "TestAuth"))
            };

            var cursorTime = baseTime.AddMinutes(20).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var cursorKey = $"c03_{unique}";
            await scope.ServiceProvider.CallApiAsync(new SetLastSendInput
            {
                PacketCode = packetCode,
                PartnerId = partner.ID,
                LastTime = cursorTime,
                LastKey = cursorKey
            });

            // Kiểm tra mốc đã ghi đúng vào ShareDataLastSend kèm CursorSignature
            var savedCursor = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.NotNull(savedCursor);
            Assert.Equal(cursorKey, savedCursor.LastKey);
            Assert.NotNull(savedCursor.CursorSignature);

            // Act 2: Worker chạy tiến trình gửi
            var worker = CreateWorker(scope, workerConfig: await CreateWorkerConfigWithMaxPages(scope, db, 2));
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert: Chỉ các dòng 4, 5, 6 (sau mốc dòng 3) được gửi tới đối tác; dòng 1, 2, 3 không được gửi
            var bodies = PartnerBodies($"partner-setcur-{unique}");
            var combinedBody = string.Join(" ", bodies);
            Assert.DoesNotContain($"\"{rowPrefix}1\"", combinedBody);
            Assert.DoesNotContain($"\"{rowPrefix}2\"", combinedBody);
            Assert.DoesNotContain($"\"{rowPrefix}3\"", combinedBody);
            Assert.Contains($"\"{rowPrefix}4\"", combinedBody);
            Assert.Contains($"\"{rowPrefix}5\"", combinedBody);
            Assert.Contains($"\"{rowPrefix}6\"", combinedBody);
        }

        /// <summary>
        /// Description: Khi người dùng bấm Đặt lại mốc (ResetLastSend) giữa lúc Worker đang gửi một trang dữ liệu:
        ///              Worker phát hiện mốc bị xóa (snapshot mismatch), KHÔNG ghi đè tạo lại mốc cũ ở cuối trang,
        ///              giữ LastTimeRun = null và dừng vòng lặp phân trang để lượt chạy kế tiếp quét lại từ đầu.
        /// Created date: 30/10/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenResetLastSendCalledWhilePageSending_DoesNotRecreateLastSend_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-midreset-{unique}";
            var packetCode = $"PKT_MIDRESET_{unique}";
            var rowPrefix = $"ROW_MIDRESET_{unique}_";
            var partnerUrl = $"/api/sharedata/partner-midreset-{unique}";

            var baseTime = new DateTime(2026, 10, 30, 8, 0, 0);
            var rows = Enumerable.Range(1, 4).Select(i => new ShareDataPacket
            {
                ID = $"r{i:D2}_{unique}",
                Code = $"{rowPrefix}{i}",
                Name = $"Row {i}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Disable
            }).ToList();
            await db.Insertable(rows).ExecuteCommandAsync();
            for (var i = 0; i < rows.Count; i++)
            {
                var time = baseTime.AddMinutes(i * 10);
                await db.Updateable<ShareDataPacket>().SetColumns(p => p.UpdateTime == time).Where(p => p.ID == rows[i].ID).ExecuteCommandAsync();
            }

            var commonSql = "SELECT TOP (@pageSize) Code AS rowCode, UpdateTime AS __lastTime, ID AS __lastKey FROM ShareDataPacket "
                + "WHERE Code LIKE @prefix AND UpdateTime >= ISNULL(@lastTime, '17530101') AND (@lastTime IS NULL OR UpdateTime > @lastTime OR ID > @lastKey) "
                + "ORDER BY UpdateTime ASC, ID ASC";

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, commonSql);
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, CursorVariables(pageSize: 2, prefix: rowPrefix, withLastTime: true));

            var shape = new Dictionary<string, object> { ["rowCode"] = new Dictionary<string, string> { ["$field"] = "rowCode" } };
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"PMR_{unique}",
                SubCode = $"SUB_MR_{unique}",
                DatatypeId = packetId,
                MappingShape = shape,
                ConfigurePartner = p => p.EndPointApiUrl = partnerUrl
            });

            UseSuperAdmin(unique);
            var resetTriggered = false;
            _host.PartnerServer.Responder = (callNumber, req) =>
            {
                if (req.Path.Contains(partnerUrl))
                {
                    resetTriggered = true;
                    using var resetScope = _host.Services.CreateScope();
                    var accessor = resetScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                    accessor.HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim(ClaimConst.AccountType, ((int)BaseEnums.AccountTypeEnum.SuperAdmin).ToString()),
                            new Claim(ClaimConst.Account, $"super_{unique}"),
                            new Claim(ClaimConst.UserId, Guid.NewGuid().ToString("N")),
                            new Claim(ClaimTypes.Name, $"super_{unique}")
                        ], "TestAuth"))
                    };
                    resetScope.ServiceProvider.CallApiAsync(new ResetLastSendInput
                    {
                        PacketCode = packetCode,
                        PartnerId = partner.ID!
                    }).GetAwaiter().GetResult();
                }
                return new PartnerResponsePlan(HttpStatusCode.OK);
            };

            var worker = CreateWorker(scope, workerConfig: await CreateWorkerConfigWithMaxPages(scope, db, 2));

            // Act: Worker chạy tiến trình gửi
            await worker.ProcessSubscriptions(CancellationToken.None);
            _host.PartnerServer.Responder = null;

            // Assert
            Assert.True(resetTriggered, "Responder phải được kích hoạt trong lúc Worker đang gửi HTTP.");

            // Mốc ShareDataLastSend không được tạo lại bởi Worker sau khi đã bị xóa
            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode)
                .FirstAsync();
            Assert.Null(lastSend);

            // Subscription.LastTimeRun không bị ghi đè bởi thời gian cuối trang 1
            var updatedSub = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.ID == sub.ID)
                .FirstAsync();
            Assert.Null(updatedSub.LastTimeRun);

            // Chỉ trang 1 được gửi đi; trang 2 bị ngắt do vòng lặp phân trang dừng khi mốc bị gián đoạn
            var bodies = PartnerBodies(partnerUrl);
            Assert.Single(bodies);
            Assert.Contains($"\"{rowPrefix}1\"", bodies[0]);
            Assert.Contains($"\"{rowPrefix}2\"", bodies[0]);
            Assert.DoesNotContain($"\"{rowPrefix}3\"", bodies[0]);
            Assert.DoesNotContain($"\"{rowPrefix}4\"", bodies[0]);
        }

        /// <summary>Bộ biến của câu nối đuôi tự viết: @pageSize, @prefix, @lastKey và (chế độ 2 cột) @lastTime kèm mốc khởi tạo.</summary>
        private static List<SqlVariableItem> CursorVariables(int pageSize, string prefix, bool withLastTime, string? lastTimeDefault = null)
        {
            var items = new List<SqlVariableItem>
            {
                new() { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = pageSize.ToString(CultureInfo.InvariantCulture), OrderNo = 1 },
                new() { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{prefix}%", OrderNo = 2 },
                new() { Code = "lastKey", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = ShareDataConst.SqlVariableSourceLastKey, OrderNo = 3 }
            };
            if (withLastTime)
            {
                items.Add(new() { Code = "lastTime", DataType = ShareDataConst.SqlVariableTypeDateTime, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = ShareDataConst.SqlVariableSourceLastTime, DefaultValue = lastTimeDefault, OrderNo = 4 });
            }
            return items;
        }

        /// <summary>Câu gộp hai nguồn, mỗi nguồn một nhóm mốc: pkt (ShareDataPacket) và toll (TollPriceList); lọc dòng theo tiền tố @prefix.</summary>
        private const string TwoSourceGroupSql = "SELECT TOP (@pageSize) u.rowCode, u.__lastTime_pkt, u.__lastKey_pkt, u.__lastTime_toll, u.__lastKey_toll FROM ("
            + "SELECT p.Code AS rowCode, p.UpdateTime AS sortTime, p.ID AS sortKey, p.UpdateTime AS __lastTime_pkt, p.ID AS __lastKey_pkt, "
            + "CAST(NULL AS datetime) AS __lastTime_toll, CAST(NULL AS nvarchar(64)) AS __lastKey_toll FROM ShareDataPacket p "
            + "WHERE p.Code LIKE @prefix AND p.UpdateTime >= ISNULL(@lastTime_pkt, '17530101') AND (@lastTime_pkt IS NULL OR p.UpdateTime > @lastTime_pkt OR p.ID > @lastKey_pkt) "
            + "UNION ALL SELECT t.SourceId, t.SourceWriteDate, t.ID, NULL, NULL, t.SourceWriteDate, t.ID FROM TollPriceList t "
            + "WHERE t.SourceId LIKE @prefix AND t.SourceWriteDate >= ISNULL(@lastTime_toll, '17530101') AND (@lastTime_toll IS NULL OR t.SourceWriteDate > @lastTime_toll OR t.ID > @lastKey_toll)"
            + ") u ORDER BY u.sortTime ASC, u.sortKey ASC";

        /// <summary>Bộ biến của câu gộp nhiều nguồn: @pageSize, @prefix và mỗi nhóm mốc một cặp @lastTime_&lt;nhóm&gt; / @lastKey_&lt;nhóm&gt;.</summary>
        private static List<SqlVariableItem> GroupCursorVariables(int pageSize, string prefix, params string[] groups)
        {
            var items = CursorVariables(pageSize, prefix, withLastTime: false).Where(v => v.Code != "lastKey").ToList();
            var order = items.Count;
            foreach (var group in groups)
            {
                items.Add(new() { Code = $"lastTime_{group}", DataType = ShareDataConst.SqlVariableTypeDateTime, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = ShareDataConst.SqlVariableSourceLastTime, OrderNo = ++order });
                items.Add(new() { Code = $"lastKey_{group}", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = ShareDataConst.SqlVariableSourceLastKey, OrderNo = ++order });
            }
            return items;
        }

        /// <summary>Gắn người dùng SuperAdmin vào HttpContext để gọi các API chỉ SuperAdmin dùng được (đặt / đặt lại mốc).</summary>
        private void UseSuperAdmin(string unique) =>
            _host.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimConst.AccountType, ((int)BaseEnums.AccountTypeEnum.SuperAdmin).ToString()),
                    new Claim(ClaimConst.Account, $"super_{unique}"),
                    new Claim(ClaimConst.UserId, Guid.NewGuid().ToString("N")),
                    new Claim(ClaimTypes.Name, $"super_{unique}")
                ], "TestAuth"))
            };

        /// <summary>Thân các yêu cầu máy chủ đối tác giả lập đã nhận ở đường dẫn chứa pathPart.</summary>
        private List<string> PartnerBodies(string pathPart) =>
            _host.PartnerServer.ReceivedRequests.Where(r => r.Path.Contains(pathPart)).Select(r => r.Body).ToList();

        private static int CountOccurrences(string text, string token)
        {
            var count = 0;
            for (var i = text.IndexOf(token, StringComparison.Ordinal); i >= 0; i = text.IndexOf(token, i + token.Length, StringComparison.Ordinal))
            {
                count++;
            }
            return count;
        }

        #endregion
    }

    internal static class HttpDeliverySenderTestExtensions
    {
        public static Task<DeliveryResult> Send(
            this HttpDeliverySender handler,
            DataMappingResult mapping,
            DataOutboundContext ctx,
            CancellationToken ct = default)
        {
            var (req, connCtx) = DataOutboundTransport.Build(mapping, ctx);
            return handler.Send(req, connCtx, ct);
        }

    }
}
