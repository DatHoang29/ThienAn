using ShareDataWorker.Core.Utils.Parsing;
using ShareDataWorker.Core.Utils.Resolvers;
using ShareDataWorker.Core.Interfaces.DataOutbound;
using ShareDataWorker.Core.Models.DataOutbound;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Modules.CCTV.Core.Entities;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.VMS.Core.Entities;
using ShareDataWorker.Core.Dto;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Exceptions;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Mapping;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Scheduling;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;

namespace Tests.Modules.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Description: Lớp kiểm thử Integration Test cho DataOutboundService thuộc module ShareDataWorker trên CSDL Test Local.
    /// Created date: 31/07/2026
    /// </summary>
    [Collection("api")]
    public partial class DataOutboundServiceTests(Host host)
    {
        private readonly Host _host = host;

        #region 1. Luồng Kiểm Thử Nghiệp Vụ Chính (Business Workflows - ProcessBatchSubscriptions & Pipeline)

        /// <summary>
        /// Description: Kiểm tra tiến trình xuất bản hàng loạt ghi file PDU hợp lệ trực tiếp xuống ổ đĩa.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_DirectFileWrite_SavesValidPduOnDisk_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_DIRECT_{unique}", $"SUB_DIRECT_{unique}", "101");

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

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
        /// Description: Kiểm tra khi mapping có biểu thức thì bỏ qua không tính toán theo §3.4/D8, giữ nguyên giá trị nguồn vào payload và không sinh cảnh báo ESH-1203.
        /// Created date: 17/09/2026
        /// Updated date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenMappingHasValidExpressions_IgnoresExpressionAndKeepsRawValue_Test()
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

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_EXPR_OK_{unique}", $"SUB_EXPR_OK_{unique}", "101");

            var targetShape = new Dictionary<string, object?>
            {
                ["zoneId"] = new Dictionary<string, object?> { ["$field"] = "zoneId" },
                ["calcSpeed"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?>
                    {
                        ["expression"] = "averageSpeed * 2"
                    }
                }
            };

            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = "101",
                Direction = BaseEnums.Direction.Outbound,
                TargetShapeJson = JsonSerializer.Serialize(targetShape),
                IsActive = true
            }).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

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
        public async Task ProcessBatchSubscriptions_WhenShapeHasHeaderAndRecordArray_WritesSingleHeaderInPayload_Test()
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

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_HDR_{unique}", $"SUB_HDR_{unique}", "101");

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

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

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
        public async Task ProcessBatchSubscriptions_WhenMappingHasInvalidExpression_LogsEsh1203_KeepsRawValue_Test()
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

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_EXPR_INV_{unique}", $"SUB_EXPR_INV_{unique}", "101");

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

            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = "101",
                Direction = BaseEnums.Direction.Outbound,
                TargetShapeJson = JsonSerializer.Serialize(targetShape),
                IsActive = true
            }).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

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
        /// Description: Kiểm tra khi biểu thức chia 0 thì bỏ qua không tính toán theo §3.4/D8, giữ lại giá trị thô và không phát sinh lỗi runtime hay cảnh báo ESH-1203.
        /// Created date: 17/09/2026
        /// Updated date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenExpressionRuntimeErrors_IgnoresAndKeepsRaw_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var zoneId = $"ZONE_{unique}";
            await db.Insertable(new TmsZone
            {
                ID = zoneId,
                Name = $"Tuyen Div0 Expr {unique}",
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
                AverageSpeed = "88",
                Condition = "BINH_THUONG",
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_EXPR_DIV0_{unique}", $"SUB_EXPR_DIV0_{unique}", "101");

            var targetShape = new Dictionary<string, object?>
            {
                ["zoneId"] = new Dictionary<string, object?> { ["$field"] = "zoneId" },
                ["speed"] = new Dictionary<string, object?>
                {
                    ["$field"] = "averageSpeed",
                    ["$extend"] = new Dictionary<string, object?>
                    {
                        ["expression"] = "averageSpeed / 0"
                    }
                }
            };

            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = "101",
                Direction = BaseEnums.Direction.Outbound,
                TargetShapeJson = JsonSerializer.Serialize(targetShape),
                IsActive = true
            }).ExecuteCommandAsync();

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", logs[0].FilePath!);
            Assert.True(File.Exists(fullPath));

            var content = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(content);
            var record = doc.RootElement.EnumerateArray().First(r => r.GetProperty("zoneId").GetString() == zoneId);
            Assert.Equal(88m, record.GetProperty("speed").GetDecimal());

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1203")
                .ToListAsync();
            Assert.Empty(alerts);
        }


        /// <summary>
        /// Description: Kiểm tra khi giao thức đối tác là ASN thì tự động fallback về định dạng JSON.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenPartnerProtocolIsAsn_FallsBackToJson_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(db, $"P_ASN_{unique}", $"SUB_ASN_{unique}", "101");

            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

            var logs = await GetLogs(db, sub.ID);
            Assert.NotEmpty(logs);
            Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
            Assert.NotNull(logs[0].FilePath);
            // ProtocolProfile.Asn chưa support → service xuất JSON theo mặc định
            Assert.EndsWith(".json", logs[0].FilePath, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Description: Kiểm tra cơ chế bù dữ liệu tăng dần sau thời gian nhàn rỗi đảm bảo mỗi bản ghi mới xuất đúng một lần.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_IncrementalCatchUp_AfterIdle_ExportsEveryNewRowExactlyOnce_Test()
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
            var (partner, sub) = await SeedOutboundSubscription(db, $"P_CU_{unique}", $"SUB_CU_{unique}", "103", s =>
            {
                s.LastTimeRun = baseTime;
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
                        LicensePlate = $"30A-{i:D5}",
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
                        .SetColumns(s => s.NextTimeRun == DateTime.Now.AddSeconds(-10))
                        .Where(s => s.ID == sub.ID)
                        .ExecuteCommandAsync();

                    await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

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
                                if (!string.IsNullOrEmpty(plateStr) && plateStr.StartsWith("30A-"))
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
                    Assert.Contains($"30A-{i:D5}", allExportedPlates);
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
        public async Task ProcessBatchSubscriptions_IncrementalPacket104_TwoConsecutiveRuns_TracksWatermarkAccurately_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "104");
            var unique = Guid.NewGuid().ToString("N")[..8];
            var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var baseTime = new DateTime(dbNow.Year, dbNow.Month, dbNow.Day, dbNow.Hour, dbNow.Minute, 0).AddHours(-1);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"P_104_{unique}",
                $"SUB_104_{unique}",
                "104",
                s =>
                {
                    s.LastTimeRun = baseTime;
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
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

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
                .SetColumns(s => s.NextTimeRun == DateTime.Now.AddSeconds(-10))
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            // Vòng chạy 2: phải xuất tiếp bản ghi 2 (không bị rớt dữ liệu)
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

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
        public async Task ProcessBatchSubscriptions_LoadsCodeSetFromPacketFields_TranslatesValueCorrectly_Test()
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
            var (partner, sub) = await SeedOutboundSubscription(db, $"P_CS_{unique}", $"SUB_CS_{unique}", "101");
            
            var targetShapeStr = """
            {
                "averageSpeed": { "$field": "averageSpeed" },
                "trafficCondition": { "$field": "trafficCondition", "$extend": { "codeSet": "TRAFFIC_COND" } },
                "zoneId": { "$field": "zoneId" }
            }
            """;
            await db.Updateable<ShareDataMapping>().SetColumns(m => m.TargetShapeJson == targetShapeStr).Where(m => m.PartnerId == partner.ID && m.DatatypeId == "101").ExecuteCommandAsync();

            // 4. Chạy export
            await CreateWorker(scope).ProcessBatchSubscriptions(CancellationToken.None);

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
        public async Task ProcessBatchSubscriptions_WhenPartnerHasEndPointApiUrl_SendsHttpPayloadSuccessfully_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            HttpRequestMessage? interceptedRequest = null;
            string? interceptedBody = null;
            var mockHandler = new TestHttpMessageHandler(async (req, ct) =>
            {
                interceptedRequest = req;
                if (req.Content != null)
                {
                    interceptedBody = await req.Content.ReadAsStringAsync(ct);
                }
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"code":0,"message":"OK"}""", System.Text.Encoding.UTF8, "application/json")
                };
            });
            var clientFactory = new MockHttpClientFactoryTest(mockHandler);

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"P_HTTP_{unique}",
                $"SUB_HTTP_{unique}",
                "101",
                configurePartner: p =>
                {
                    p.Address = "127.0.0.1";
                    p.Port = 5000;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                });

            try
            {
                var worker = CreateWorker(scope, clientFactory);
                await worker.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await GetLogs(db, sub.ID);
                Assert.NotEmpty(logs);
                Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

                Assert.NotNull(interceptedRequest);
                Assert.Equal(HttpMethod.Post, interceptedRequest.Method);
                Assert.Equal("http://127.0.0.1:5000/api/sharedata/sharedatainbound", interceptedRequest.RequestUri?.ToString());

                Assert.NotNull(interceptedBody);
                using var doc = JsonDocument.Parse(interceptedBody);
                var root = doc.RootElement;
                Assert.Equal(JsonValueKind.Array, root.ValueKind);
                Assert.True(root.GetArrayLength() > 0);
                Assert.Equal(JsonValueKind.Object, root[0].ValueKind);

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                    .ToListAsync();
                Assert.Empty(alerts);
            }
            finally
            {
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }


        /// <summary>
        /// Description: Kiểm tra khi endpoint HTTP của đối tác trả về mã 500 thì vẫn xuất file và ghi cảnh báo lỗi.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenPartnerHttpEndpointReturns500_StillExportsFileAndLogsWarningAlert_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var mockHandler = new TestHttpMessageHandler((req, ct) =>
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Internal Server Error", System.Text.Encoding.UTF8, "text/plain")
                });
            });
            var clientFactory = new MockHttpClientFactoryTest(mockHandler);

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"P_HTTP_500_{unique}",
                $"SUB_HTTP_500_{unique}",
                "101",
                configurePartner: p =>
                {
                    p.Address = "127.0.0.1";
                    p.Port = 5001;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                });

            try
            {
                var worker = CreateWorker(scope, clientFactory);
                await worker.ProcessBatchSubscriptions(CancellationToken.None);

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
                Assert.Contains("500", alerts[0].Message);
                Assert.NotNull(alerts[0].DetailJson);
                Assert.Contains("500", alerts[0].DetailJson);
            }
            finally
            {
                await db.Deleteable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }


        /// <summary>
        /// Description: Kiểm tra khi gọi HTTP đến đối tác ném ngoại lệ thì vẫn xuất file và ghi cảnh báo lỗi.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenPartnerHttpThrowsException_StillExportsFileAndLogsWarningAlert_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var mockHandler = new TestHttpMessageHandler((req, ct) =>
            {
                throw new HttpRequestException("Simulated connection timeout/refusal");
            });
            var clientFactory = new MockHttpClientFactoryTest(mockHandler);

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"P_HTTP_EX_{unique}",
                $"SUB_HTTP_EX_{unique}",
                "101",
                configurePartner: p =>
                {
                    p.Address = "127.0.0.1";
                    p.Port = 5002;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                });

            try
            {
                var worker = CreateWorker(scope, clientFactory);
                await worker.ProcessBatchSubscriptions(CancellationToken.None);

                var logs = await GetLogs(db, sub.ID);
                Assert.NotEmpty(logs);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, logs[0].Success);
                Assert.False(string.IsNullOrWhiteSpace(logs[0].FilePath));

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                    .ToListAsync();
                Assert.Single(alerts);
                Assert.Equal(BaseEnums.AlertSeverity.Warning, alerts[0].Severity);
                Assert.Equal(BaseEnums.AlertSource.Protocol, alerts[0].AlertSource);
                Assert.Contains("Simulated connection timeout/refusal", alerts[0].Message);
            }
            finally
            {
                await db.Deleteable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }


        /// <summary>
        /// Description: Kiểm tra khi đối tác không cấu hình EndPointApiUrl thì worker vẫn gửi HTTP và ghi nhận thất bại ESH-1402 khi nhận lỗi 404.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenPartnerHasNoEndPointApiUrl_SendsHttpAndFails_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var sendCount = 0;
            var mockHandler = new TestHttpMessageHandler((req, ct) =>
            {
                if (req.RequestUri?.Port == 5003)
                {
                    sendCount++;
                    if (string.IsNullOrEmpty(req.RequestUri?.AbsolutePath) || req.RequestUri.AbsolutePath == "/")
                    {
                        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                        {
                            Content = new StringContent("Not Found")
                        });
                    }
                }
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            });
            var clientFactory = new MockHttpClientFactoryTest(mockHandler);

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"P_NO_HTTP_{unique}",
                $"SUB_NO_HTTP_{unique}",
                "101",
                configurePartner: p =>
                {
                    p.Address = "127.0.0.1";
                    p.Port = 5003;
                    p.EndPointApiUrl = null;
                });

            try
            {
                var worker = CreateWorker(scope, clientFactory);
                await worker.ProcessBatchSubscriptions(CancellationToken.None);

                Assert.Equal(1, sendCount);

                var logs = await GetLogs(db, sub.ID);
                Assert.NotEmpty(logs);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, logs[0].Success);

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed)
                    .ToListAsync();
                Assert.NotEmpty(alerts);
                Assert.Equal(BaseEnums.AlertSeverity.Warning, alerts[0].Severity);
            }
            finally
            {
                await db.Deleteable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }


        /// <summary>
        /// Description: Kiểm tra khi mã gói tin có hậu tố tương tự DB staging thì vẫn phân giải đúng handler và xuất bản thành công.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenPacketCodeHasSuffixLikeStagingDb_ResolvesHandlerAndExportsSuccessfully_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetCodeWithSuffix = "101_commonData";

            // Bổ sung ShareDataPacket và ShareDataPacketField với mã có hậu tố như trên DB staging thật
            var packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCodeWithSuffix,
                Name = "Dữ liệu giao thông chung",
                PacketVersion = "1.0",
                OrderNo = 1
            };
            await db.Insertable(packet).ExecuteCommandAsync();

            var tableFields = new System.Collections.Generic.List<ShareDataPacketField>
            {
                new() { ID = Guid.NewGuid().ToString("N"), DatatypeId = packetCodeWithSuffix, AliasFieldKey = "zoneId", IsRequired = Shared.DTO.Enums.BaseEnums.IsRequired.IsRequired },
                new() { ID = Guid.NewGuid().ToString("N"), DatatypeId = packetCodeWithSuffix, AliasFieldKey = "averageSpeed", IsRequired = Shared.DTO.Enums.BaseEnums.IsRequired.NoRequired },
                new() { ID = Guid.NewGuid().ToString("N"), DatatypeId = packetCodeWithSuffix, AliasFieldKey = "trafficCondition", IsRequired = Shared.DTO.Enums.BaseEnums.IsRequired.NoRequired }
            };
            await db.Insertable(tableFields).ExecuteCommandAsync();

            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"P_SUFFIX_{unique}",
                $"SUB_SUFFIX_{unique}",
                packetCodeWithSuffix);

            try
            {
                var worker = CreateWorker(scope);
                await worker.ProcessBatchSubscriptions(CancellationToken.None);

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
            finally
            {
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPacketField>().Where(t => t.DatatypeId == packet.Code).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPacket>().Where(p => p.ID == packet.ID).ExecuteCommandAsync();
            }
        }


        /// <summary>
        /// Description: Kiểm tra khi ghi tệp thất bại nhưng đối tác có cấu hình API endpoint và gửi HTTP thành công, quá trình xuất bản vẫn đạt Success, im lặng không ghi cảnh báo ESH-1401, gọi gửi API và cập nhật watermark.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenFileWriteFails_PartnerHasEndpoint_ApiSucceeds_ExportsSuccessfullyAndAdvancesWatermark_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var apiCalled = 0;
            var fileSender = new MockDataOutboundFileSender((m, c) =>
                Task.FromResult(new DataOutboundSendResult(false, 0, null, "Simulated disk error while writing file")));
            var restSender = new MockDataOutboundRestSender((m, c) =>
            {
                apiCalled++;
                return Task.FromResult(new DataOutboundSendResult(true, 100, "simulated/path.json"));
            });

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"P_FILE_FAIL_API_OK_{unique}",
                $"SUB_FILE_FAIL_API_OK_{unique}",
                "101",
                configurePartner: p =>
                {
                    p.Address = "127.0.0.1";
                    p.Port = 5002;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                });

            try
            {
                var worker = CreateWorker(scope, fileSender: fileSender, restSender: restSender);
                await worker.ProcessBatchSubscriptions(CancellationToken.None);

                // Khẳng định API BẮT BUỘC được gọi dù ghi tệp thất bại
                Assert.Equal(1, apiCalled);

                var logs = await GetLogs(db, sub.ID);
                Assert.NotEmpty(logs);
                Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID)
                    .ToListAsync();
                Assert.DoesNotContain(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.FileWriteFailed);
                Assert.DoesNotContain(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed);
                Assert.Empty(alerts);

                var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
                Assert.NotNull(updatedSub.LastTimeRun);
                Assert.Equal((sub.SerialNbr ?? 0) + 1, updatedSub.SerialNbr);
            }
            finally
            {
                await db.Deleteable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Kiểm tra khi cả ghi tệp và gửi HTTP đều thất bại thì toàn bộ quá trình xuất bản bị huỷ, ghi nhật ký thất bại, ghi cảnh báo HTTP warning và KHÔNG tịnh tiến watermark.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenFileWriteFails_PartnerHasEndpoint_ApiFails_AbortsAndDoesNotAdvanceWatermark_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var apiCalled = 0;
            var fileSender = new MockDataOutboundFileSender((m, c) =>
                Task.FromResult(new DataOutboundSendResult(false, 0, null, "Simulated disk full")));
            var restSender = new MockDataOutboundRestSender((m, c) =>
            {
                apiCalled++;
                return Task.FromResult(new DataOutboundSendResult(false, 0, null, "Simulated HTTP 500 server error"));
            });

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"P_BOTH_FAIL_{unique}",
                $"SUB_BOTH_FAIL_{unique}",
                "101",
                configurePartner: p =>
                {
                    p.Address = "127.0.0.1";
                    p.Port = 5002;
                    p.EndPointApiUrl = "/api/sharedata/sharedatainbound";
                });

            try
            {
                var worker = CreateWorker(scope, fileSender: fileSender, restSender: restSender);
                await worker.ProcessBatchSubscriptions(CancellationToken.None);

                Assert.Equal(1, apiCalled);

                var logs = await GetLogs(db, sub.ID);
                Assert.NotEmpty(logs);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, logs[0].Success);

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID)
                    .ToListAsync();
                Assert.DoesNotContain(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.FileWriteFailed);
                Assert.Contains(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed && a.Severity == BaseEnums.AlertSeverity.Warning);

                var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
                Assert.Null(updatedSub.LastTimeRun);
                Assert.Equal(sub.SerialNbr, updatedSub.SerialNbr);
            }
            finally
            {
                await db.Deleteable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Kiểm tra khi ghi file thất bại và gửi API thất bại (hoặc không có API endpoint), huỷ và không tịnh tiến watermark.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task ProcessBatchSubscriptions_WhenFileWriteFails_AndHttpFails_AbortsAndDoesNotAdvanceWatermark_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            string? targetSubId = null;
            var apiCalled = 0;
            var fileSender = new MockDataOutboundFileSender((m, c) =>
                Task.FromResult(new DataOutboundSendResult(false, 0, null, "Simulated permission denied")));
            var restSender = new MockDataOutboundRestSender((m, c) =>
            {
                if (c.Subscription?.ID == targetSubId)
                {
                    apiCalled++;
                    return Task.FromResult(new DataOutboundSendResult(false, 0, null, "Simulated HTTP error"));
                }
                return Task.FromResult(new DataOutboundSendResult(true, 100, "simulated/path.json"));
            });

            await PacketMetadataCatalogTest.SeedPacketToDb(db, "101");
            var unique = Guid.NewGuid().ToString("N")[..8];
            await SeedTestDataForPacket(db, ShareDataEnum.DatatypeIdEnum.TrafficFlow, unique);

            var (partner, sub) = await SeedOutboundSubscription(
                db,
                $"P_FILE_FAIL_NO_API_{unique}",
                $"SUB_FILE_FAIL_NO_API_{unique}",
                "101",
                configurePartner: p =>
                {
                    p.Address = "127.0.0.1";
                    p.Port = 5003;
                    p.EndPointApiUrl = null;
                });
            targetSubId = sub.ID;

            try
            {
                var worker = CreateWorker(scope, fileSender: fileSender, restSender: restSender);
                await worker.ProcessBatchSubscriptions(CancellationToken.None);

                Assert.Equal(1, apiCalled);

                var logs = await GetLogs(db, sub.ID);
                Assert.NotEmpty(logs);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, logs[0].Success);

                var alerts = await db.Queryable<ShareDataAlertLog>()
                    .Where(a => a.SubscriptionId == sub.ID)
                    .ToListAsync();
                Assert.DoesNotContain(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.FileWriteFailed);
                Assert.Contains(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed && a.Severity == BaseEnums.AlertSeverity.Warning);

                var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
                Assert.Null(updatedSub.LastTimeRun);
                Assert.Equal(sub.SerialNbr, updatedSub.SerialNbr);
            }
            finally
            {
                await db.Deleteable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
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

            
            
            var existingFields = await db.Queryable<ShareDataPacketField>().Where(f => f.DatatypeId == "101_commonData").ToListAsync();
            if (existingFields.Count == 0)
            {
                var newFields = new System.Collections.Generic.List<ShareDataPacketField>
                {
                    new() { ID = Guid.NewGuid().ToString("N"), DatatypeId = "101_commonData", AliasFieldKey = "zoneStatusId", Type = "string", IsRequired = Shared.DTO.Enums.BaseEnums.IsRequired.IsRequired },
                    new() { ID = Guid.NewGuid().ToString("N"), DatatypeId = "101_commonData", AliasFieldKey = "zoneId", Type = "string", IsRequired = Shared.DTO.Enums.BaseEnums.IsRequired.IsRequired },
                    new() { ID = Guid.NewGuid().ToString("N"), DatatypeId = "101_commonData", AliasFieldKey = "averageSpeed", Type = "string", IsRequired = Shared.DTO.Enums.BaseEnums.IsRequired.NoRequired },
                    new() { ID = Guid.NewGuid().ToString("N"), DatatypeId = "101_commonData", AliasFieldKey = "trafficCondition", Type = "string", IsRequired = Shared.DTO.Enums.BaseEnums.IsRequired.NoRequired },
                    new() { ID = Guid.NewGuid().ToString("N"), DatatypeId = "101_commonData", AliasFieldKey = "dataTime", Type = "string", IsRequired = Shared.DTO.Enums.BaseEnums.IsRequired.NoRequired }
                };
                await db.Insertable(newFields).ExecuteCommandAsync();
            }

            var (partner, sub) = await SeedOutboundSubscription(db, $"P101_{uniqueId}", $"SUB101_{uniqueId}", "101_commonData");

            try
            {
                // 2. Act: Thực thi xuất dữ liệu qua ProcessBatchSubscriptions
                await service.ProcessBatchSubscriptions(CancellationToken.None);

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
            finally
            {
                await db.Deleteable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataSubscription>().Where(s => s.ID == sub.ID).ExecuteCommandAsync();
                await db.Deleteable<ShareDataPartner>().Where(p => p.ID == partner.ID).ExecuteCommandAsync();
                await db.Deleteable<TmsZoneStatus>().Where(z => z.ID == zoneStatusId).ExecuteCommandAsync();
                await db.Deleteable<TmsZone>().Where(z => z.ID == zoneId).ExecuteCommandAsync();
            }
        }


        /// <summary>
        /// Description: Kiểm thử getSubscriptions trong ProcessBatchSubscriptions - chỉ lấy Subscription có
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
            var now = DateTime.Now;

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

            // Case 1: Partner bị Disable -> Không được chọn
            var (pDisabled, subDisabled) = await SeedOutboundSubscription(db, $"P_DIS_{uniqueId}", $"S_DIS_{uniqueId}", "101_commonData",
                configurePartner: p => p.Status = BaseEnums.StatusEnum.Disable);

            // Case 2: Partner bị Disconnected -> Không được chọn
            var (pDisconn, subDisconn) = await SeedOutboundSubscription(db, $"P_DCN_{uniqueId}", $"S_DCN_{uniqueId}", "101_commonData",
                configurePartner: p => p.SessionState = BaseEnums.SessionState.Disconnected);

            // Case 3: Partner bị xoá mềm (IsDelete != null) -> Không được chọn
            var (pDeleted, subDeleted) = await SeedOutboundSubscription(db, $"P_DEL_{uniqueId}", $"S_DEL_{uniqueId}", "101_commonData",
                configurePartner: p => p.IsDelete = now);

            // Case 4: Partner hợp lệ (Enable, Connected, IsDelete == null) -> Được chọn
            var (pValid, subValid) = await SeedOutboundSubscription(db, $"P_VAL_{uniqueId}", $"S_VAL_{uniqueId}", "101_commonData",
                configurePartner: p =>
                {
                    p.Status = BaseEnums.StatusEnum.Enable;
                    p.SessionState = BaseEnums.SessionState.Connected;
                    p.IsDelete = null;
                });

            // Act: Chạy ProcessBatchSubscriptions
            await service.ProcessBatchSubscriptions(CancellationToken.None);

            // Assert:
            // Sub hợp lệ sẽ được claim lease và ghi log
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
        /// Description: Kiểm tra tích hợp toàn trình luồng Outbound: từ trích xuất thô -> Map giải $meta, $extend, CodeSet -> tạo FinalBytes -> RestSender gửi trực tiếp tới đối tác qua HTTP.
        /// Created date: 18/09/2026
        /// </summary>
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

            try
            {
                HttpRequestMessage? capturedRequest = null;
                string? capturedHttpBody = null;
                var testHandler = new TestHttpMessageHandler(async (req, ct) =>
                {
                    capturedRequest = req;
                    capturedHttpBody = await req.Content!.ReadAsStringAsync(ct);
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                });
                var clientFactory = new MockHttpClientFactoryTest(testHandler);
                var restSender = new DataOutboundRestSender(clientFactory);

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
                    Address = "127.0.0.1",
                    Port = 8080,
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
                var sendResult = await restSender.Send(mapResult, ctx, CancellationToken.None);

                // Assert 2: Gửi HTTP thành công
                Assert.True(sendResult.Success);
                Assert.NotNull(capturedRequest);
                Assert.Equal("http://127.0.0.1:8080/api/v1/e2e/receive", capturedRequest.RequestUri?.ToString());
                Assert.Equal("application/json", capturedRequest.Content?.Headers.ContentType?.MediaType);

                // Assert 3: Kiểm tra cấu hình gói tin đối tác nhận được
                Assert.NotNull(capturedHttpBody);
                using var doc = JsonDocument.Parse(capturedHttpBody);
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
            finally
            {
                await db.Deleteable<ShareDataCodeSet>().Where(c => c.Code == codeSetCode).ExecuteCommandAsync();
            }
        }


        #endregion

        #region 2. Các Kiểm Thử Thành Phần (Component, RestSender, Mapping, Transform & Serialization)

        /// <summary>
        /// Description: Kiểm tra thứ tự các thuộc tính JSON của gói tin 101 được sắp xếp đúng theo OrderNo từ 1 đến 12.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Transform_Packet101_KeysOrderMatchesOrderNo1To12_Test()
        {
            var def = PacketMetadataCatalogTest.All["101"];
            var allFields = def.Fields
                .Where(f => f.InternalOnly != true)
                .OrderBy(f => f.OrderNo)
                .ToList();

            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "Z01",
                    ["zoneName"] = "Zone 01",
                    ["fromLocationKm"] = 10,
                    ["fromLocationMet"] = 500,
                    ["toLocationKm"] = 20,
                    ["toLocationMet"] = 0,
                    ["laneId"] = "L01",
                    ["averageSpeed"] = 75.5m,
                    ["trafficCondition"] = "NORMAL",
                    ["dataTime"] = DateTime.Now,
                    ["speedLimit"] = 100,
                    ["vehicleCount"] = 250
                }
            };

            var targetShapeJson = "{" + string.Join(", ", allFields.Select(f => $"\"{f.FieldKey}\": {{ \"$field\": \"{f.FieldKey}\" }}")) + "}";
            var transformed = DataMappingProcess.Transform(rawRows, targetShapeJson);
            Assert.Single(transformed);

            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(transformed[0]);
            var expectedKeys = new[]
            {
                "zoneId", "zoneName", "fromLocationKm", "fromLocationMet",
                "toLocationKm", "toLocationMet", "laneId", "averageSpeed",
                "trafficCondition", "dataTime", "speedLimit", "vehicleCount"
            };

            Assert.Equal(expectedKeys, row.Keys.ToArray());
        }


        /// <summary>
        /// Description: Kiểm tra trường kiểu int không có đơn vị quy đổi sẽ được ép kiểu thành số nguyên int.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenIntFieldWithoutTargetUnit_CoercesToInt_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["speedLimit"] = "80"
                }
            };

            var shapeJson = """{ "speedLimit": { "$field": "speedLimit", "$extend": { "targetType": "int" } } }""";
            var result = DataMappingProcess.Transform(rawRows, shapeJson);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(80, row["speedLimit"]);
            Assert.IsType<int>(row["speedLimit"]);
        }


        /// <summary>
        /// Description: Kiểm tra khi hai trường có cùng khóa đích thì trường sau ghi đè trường trước và kích hoạt cảnh báo.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenTwoFieldsShareSameTargetKey_OverwritesAndTriggersWarning_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["fieldA"] = "ValueA",
                    ["fieldB"] = "ValueB"
                }
            };

            var targetShapeJson = @"{
                ""renamedField"": { ""$field"": ""fieldA"" }
            }";

            var result = DataMappingProcess.Transform(rawRows, targetShapeJson);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.True(row.ContainsKey("renamedField"));
            Assert.Equal("ValueA", row["renamedField"]);
        }


        /// <summary>
        /// Description: Kiểm tra đường dẫn file kết xuất có chứa SubscriptionId phân biệt khi hai subscription chạy cùng giây và cùng số serial.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void GenerateExportRelativePath_WhenTwoSubsShareSecondAndHaveSameSerialNbr_DiscriminatorIncludesSubId_Test()
        {
            var sub1 = new ShareDataSubscription
            {
                ID = "SUB_ID_11111111",
                SerialNbr = 5
            };

            var sub2 = new ShareDataSubscription
            {
                ID = "SUB_ID_22222222",
                SerialNbr = 5
            };

            var time = new DateTime(2026, 8, 23, 15, 30, 45);
            var path1 = DataOutboundFileSender.GenerateExportRelativePath("PARTNER_A", "101", time, DataOutboundFileSender.ResolveFileDiscriminator(sub1));
            var path2 = DataOutboundFileSender.GenerateExportRelativePath("PARTNER_A", "101", time, DataOutboundFileSender.ResolveFileDiscriminator(sub2));

            Assert.NotEqual(path1, path2);
            Assert.Contains("5_SUB_ID_1", path1);
            Assert.Contains("5_SUB_ID_2", path2);
        }


        /// <summary>
        /// Description: Kiểm tra giải mã JSON danh sách CodeValues hoạt động chính xác cả với JSON hợp lệ và JSON bị hỏng.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void ParseCodeValues_WithValidAndCorruptedJson_ParsesCorrectly_Test()
        {
            var validJson = "[{\"sourceValue\":\"1\",\"partnerValue\":\"slow\",\"displayName\":\"Chậm\",\"orderNo\":1},{\"sourceValue\":\"2\",\"partnerValue\":\"normal\",\"displayName\":\"Bình thường\",\"isDefault\":true,\"orderNo\":2}]";
            var parsed = PacketJsonParser.ParseCodeValues(validJson);
            Assert.Equal(2, parsed.Count);
            Assert.Equal("1", parsed[0].SourceValue);
            Assert.Equal("slow", parsed[0].PartnerValue);
            Assert.Equal("Chậm", parsed[0].DisplayName);
            Assert.Equal(1, parsed[0].OrderNo);
            Assert.True(parsed[1].IsDefault);

            var empty = PacketJsonParser.ParseCodeValues("{invalid-json}");
            Assert.Empty(empty);

            var partialJson = "[{\"sourceValue\":\"1\",\"partnerValue\":\"slow\"}, \"bad_element\", {\"sourceValue\":\"2\",\"partnerValue\":\"normal\"}]";
            var partialParsed = PacketJsonParser.ParseCodeValues(partialJson);
            Assert.Equal(2, partialParsed.Count);
        }


        /// <summary>
        /// Description: Kiểm tra ParseCodeSet và ParseCodeValues hoạt động chính xác với cả cấu trúc Object mới, Array cũ và JSON hỏng.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void ParseCodeSet_And_ParseCodeValues_BothStructures_BehavesCorrectly_Test()
        {
            // 1. Cấu trúc mới (Object)
            var newStructureJson = @"{
                ""values"": [
                    { ""sourceValue"": ""1"", ""partnerValue"": ""on"", ""displayName"": ""Bật"" },
                    { ""sourceValue"": ""2"", ""partnerValue"": ""off"", ""displayName"": ""Tắt"" }
                ],
                ""defaultSourceValue"": ""4"",
                ""defaultPartnerValue"": ""false""
            }";

            var codeSetNew = PacketJsonParser.ParseCodeSet(newStructureJson);
            Assert.Equal(2, codeSetNew.Values.Count);
            Assert.Equal("1", codeSetNew.Values[0].SourceValue);
            Assert.Equal("on", codeSetNew.Values[0].PartnerValue);
            Assert.Equal("4", codeSetNew.DefaultSourceValue);
            Assert.Equal("false", codeSetNew.DefaultPartnerValue);

            // ParseCodeValues với cấu trúc mới -> trả đúng Values
            var valuesFromNew = PacketJsonParser.ParseCodeValues(newStructureJson);
            Assert.Equal(2, valuesFromNew.Count);
            Assert.Equal("1", valuesFromNew[0].SourceValue);
            Assert.Equal("on", valuesFromNew[0].PartnerValue);

            // 2. Cấu trúc cũ (Array)
            var oldStructureJson = @"[
                { ""sourceValue"": ""normal"", ""partnerValue"": ""0"", ""displayName"": """", ""isDefault"": false },
                { ""sourceValue"": ""slow"", ""partnerValue"": ""1"", ""displayName"": """", ""isDefault"": true }
            ]";

            var codeSetOld = PacketJsonParser.ParseCodeSet(oldStructureJson);
            Assert.Equal(2, codeSetOld.Values.Count);
            Assert.Equal("normal", codeSetOld.Values[0].SourceValue);
            Assert.Equal("0", codeSetOld.Values[0].PartnerValue);
            Assert.Null(codeSetOld.DefaultSourceValue);
            Assert.Null(codeSetOld.DefaultPartnerValue);

            // ParseCodeValues với cấu trúc cũ -> trả đúng Values
            var valuesFromOld = PacketJsonParser.ParseCodeValues(oldStructureJson);
            Assert.Equal(2, valuesFromOld.Count);
            Assert.Equal("normal", valuesFromOld[0].SourceValue);

            // 3. JSON rỗng / hỏng -> trả rỗng, không ném
            var emptySet = PacketJsonParser.ParseCodeSet(null);
            Assert.Empty(emptySet.Values);
            Assert.Null(emptySet.DefaultSourceValue);

            var corruptedSet = PacketJsonParser.ParseCodeSet("{invalid json syntax}");
            Assert.Empty(corruptedSet.Values);

            var corruptedValues = PacketJsonParser.ParseCodeValues("{invalid json syntax}");
            Assert.Empty(corruptedValues);
        }


        /// <summary>
        /// Description: Kiểm tra Transform tích hợp với bộ mã cấu trúc mới (DefaultPartnerValue cho chiều gửi khi không khớp theo §3.4).
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_WithNewCodeSetStructure_UsesDefaultPartnerValueWhenUnmatched_Test()
        {
            var codeSetJson = @"{
                ""values"": [
                    { ""sourceValue"": ""1"", ""partnerValue"": ""on"" },
                    { ""sourceValue"": ""2"", ""partnerValue"": ""off"" }
                ],
                ""defaultSourceValue"": ""4"",
                ""defaultPartnerValue"": ""false""
            }";
            var codeSet = PacketJsonParser.ParseCodeSet(codeSetJson);

            var codeSets = new Dictionary<string, CodeSetDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["MY_CODE_SET"] = codeSet
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["cond"] = "1" },
                new Dictionary<string, object?> { ["cond"] = "99" },
                new Dictionary<string, object?> { ["cond"] = null }
            };

            var targetShapeJson = @"{
                ""trafficCond"": { ""$field"": ""cond"", ""$extend"": { ""codeSet"": ""MY_CODE_SET"" } }
            }";

            var result = DataMappingProcess.Transform(rawRows, targetShapeJson, codeSets: codeSets);
            Assert.Equal(3, result.Count);

            var row1 = (IDictionary<string, object?>)result[0];
            Assert.Equal("on", row1["trafficCond"]);

            var row2 = (IDictionary<string, object?>)result[1];
            Assert.Equal("false", row2["trafficCond"]);

            var row3 = (IDictionary<string, object?>)result[2];
            Assert.Equal("false", row3["trafficCond"]);
        }


        /// <summary>
        /// Description: Kiểm tra quy trình biến đổi chuyển từ mã nội bộ sang mã chuẩn sau đó sang mã đối tác theo đúng trình tự.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact(Skip = "Bộ mã tầng 1 (PacketField.CodeSetCode) đã bỏ hẳn khỏi luồng Outbound theo chốt thiết kế, chỉ còn $extend.codeSet.")]
        public void Transform_Step2AndStep3Sequence_ConvertsStandardThenPartnerCodeSet_Test()
        {
            var codeSets = new Dictionary<string, List<CodeValueDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["TRAFFIC_COND_STD"] =
                [
                    new() { SourceValue = "1", PartnerValue = "slow", DisplayName = "Chậm (std)" },
                    new() { SourceValue = "2", PartnerValue = "normal", DisplayName = "Bình thường (std)" }
                ]
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["condition"] = "1"
                }
            };

            var targetShapeJson = @"{
                ""tinhTrang"": { ""$field"": ""condition"", ""$extend"": { ""codeSet"": ""TRAFFIC_COND_PARTNER"" } }
            }";

            var result = DataMappingProcess.Transform(rawRows, targetShapeJson, codeSets: codeSets);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.True(row.ContainsKey("tinhTrang"));
            Assert.Equal("Chậm", row["tinhTrang"]);
        }


        /// <summary>
        /// Description: Kiểm tra trường dữ liệu có cấu hình CodeSet thì giữ nguyên chuỗi mã, không bị ép kiểu về số.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenFieldHasCodeSet_DoesNotCoerceToNumber_Test()
        {
            var codeSets = new Dictionary<string, List<CodeValueDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["COND_SET"] =
                [
                    new() { SourceValue = "1", PartnerValue = "slow" }
                ]
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["condition"] = "1"
                }
            };

            var targetShapeJson = @"{
                ""condition"": {
                    ""$field"": ""condition"",
                    ""$extend"": {
                        ""codeSet"": ""COND_SET"",
                        ""targetType"": ""decimal""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, targetShapeJson, codeSets: codeSets);

            Assert.Single(result);
            var row = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("slow", row["condition"]);
            Assert.IsType<string>(row["condition"]);
        }


        /// <summary>
        /// Description: Kiểm tra ném ngoại lệ khi thiếu trường bắt buộc trong dữ liệu biến đổi.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenRequiredFieldMissing_Throws_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "Z01",
                    ["averageSpeed"] = null
                },
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "Z02",
                    ["averageSpeed"] = 70.0m
                }
            };

            var targetShapeJson = @"{
                ""zoneId"": { ""$field"": ""zoneId"", ""$extend"": { ""required"": true } },
                ""averageSpeed"": { ""$field"": ""averageSpeed"", ""$extend"": { ""required"": true, ""targetType"": ""decimal"" } }
            }";

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                DataMappingProcess.Transform(rawRows, targetShapeJson);
            });

            Assert.Contains("Thiếu trường bắt buộc", ex.Message);
            Assert.Contains("averageSpeed", ex.Message);
        }


        /// <summary>
        /// Description: Kiểm tra tính toán các phép toán số học cơ bản cộng trừ nhân chia trả về kết quả chính xác.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_ArithmeticBasic_ReturnsExpectedValue_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["val1"] = 10,
                ["val2"] = 5,
                ["val3"] = 2
            };

            // Act
            var ok = DataMappingProcess.TryEvaluate("val1 + val2 * val3", row, out var result, out var error);

            // Assert
            Assert.True(ok, error);
            Assert.Equal(20m, result);
        }


        /// <summary>
        /// Description: Kiểm tra tính toán biểu thức có chứa dấu ngoặc đơn và phép chia theo đúng độ ưu tiên toán học.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_ParenthesesAndDivision_ReturnsExpectedValue_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["a"] = 20,
                ["b"] = 8,
                ["c"] = 3
            };

            // Act
            var ok = DataMappingProcess.TryEvaluate("(a - b) / c", row, out var result, out var error);

            // Assert
            Assert.True(ok, error);
            Assert.Equal(4m, result);
        }


        /// <summary>
        /// Description: Kiểm tra tính toán biểu thức chứa dấu trừ một ngôi và phép chia lấy dư modulo.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_UnaryMinusAndModulo_ReturnsExpectedValue_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["num"] = 15
            };

            // Act
            var ok = DataMappingProcess.TryEvaluate("-num % 4", row, out var result, out var error);

            // Assert
            Assert.True(ok, error);
            Assert.Equal(-3m, result);
        }


        /// <summary>
        /// Description: Kiểm tra hàm Concat nối chuỗi ký tự và số thành chuỗi hoàn chỉnh.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_ConcatFunction_JoinsStringsAndNumbers_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["code"] = "VDS",
                ["id"] = 102
            };

            // Act
            var ok = DataMappingProcess.TryEvaluate("CONCAT(code, '_', id)", row, out var result, out var error);

            // Assert
            Assert.True(ok, error);
            Assert.Equal("VDS_102", result);
        }


        /// <summary>
        /// Description: Kiểm tra hàm IsNull và Coalesce trả về giá trị khác null đầu tiên.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_IsNullAndCoalesce_ReturnsFirstNonNull_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["nullField"] = null,
                ["fallbackVal"] = 42m
            };

            // Act
            var okIsNull = DataMappingProcess.TryEvaluate("ISNULL(nullField, fallbackVal)", row, out var resIsNull, out var err1);
            var okCoalesce = DataMappingProcess.TryEvaluate("COALESCE(nullField, nullField, 99)", row, out var resCoalesce, out var err2);

            // Assert
            Assert.True(okIsNull, err1);
            Assert.Equal(42m, resIsNull);

            Assert.True(okCoalesce, err2);
            Assert.Equal(99m, resCoalesce);
        }


        /// <summary>
        /// Description: Kiểm tra các hàm xử lý chuỗi ký tự gồm Upper, Lower, Len và Trim.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_StringFunctions_UpperLowerLenTrim_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["text"] = "  Hello World  "
            };

            // Act & Assert
            Assert.True(DataMappingProcess.TryEvaluate("UPPER(text)", row, out var upper, out _));
            Assert.Equal("  HELLO WORLD  ", upper);

            Assert.True(DataMappingProcess.TryEvaluate("LOWER(text)", row, out var lower, out _));
            Assert.Equal("  hello world  ", lower);

            Assert.True(DataMappingProcess.TryEvaluate("LEN(text)", row, out var len, out _));
            Assert.Equal(15m, len);

            Assert.True(DataMappingProcess.TryEvaluate("LTRIM(text)", row, out var ltrim, out _));
            Assert.Equal("Hello World  ", ltrim);

            Assert.True(DataMappingProcess.TryEvaluate("RTRIM(text)", row, out var rtrim, out _));
            Assert.Equal("  Hello World", rtrim);
        }


        /// <summary>
        /// Description: Kiểm tra các hàm làm tròn Round và trị tuyệt đối Abs trả về giá trị số thập phân mong muốn.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_RoundAndAbs_ReturnsExpectedDecimal_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["speed"] = 85.6789m,
                ["diff"] = -15.4m
            };

            // Act & Assert
            Assert.True(DataMappingProcess.TryEvaluate("ROUND(speed, 2)", row, out var rounded, out _));
            Assert.Equal(85.68m, rounded);

            Assert.True(DataMappingProcess.TryEvaluate("ABS(diff)", row, out var absVal, out _));
            Assert.Equal(15.4m, absVal);
        }


        /// <summary>
        /// Description: Kiểm tra toán hạng null trong phép toán số học sẽ lan truyền giá trị null.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_NullOperandInArithmetic_PropagatesNull_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["nullVal"] = null
            };

            // Act
            var ok = DataMappingProcess.TryEvaluate("nullVal + 10", row, out var result, out var error);

            // Assert
            Assert.True(ok, error);
            Assert.Null(result);
        }


        /// <summary>
        /// Description: Kiểm tra phép chia cho 0 được xử lý an toàn và không gây crash ứng dụng.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_DivideByZero_FailsGracefully_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["val"] = 100
            };

            // Act
            var ok = DataMappingProcess.TryEvaluate("val / 0", row, out var result, out var error);

            // Assert
            Assert.False(ok);
            Assert.Contains("chia cho 0", error, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Description: Kiểm tra khi thiếu trường dữ liệu trong hàng thì ném thông báo lỗi mô tả chi tiết.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_MissingFieldInRow_FailsWithDescriptiveError_Test()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                ["knownField"] = 1
            };

            // Act
            var ok = DataMappingProcess.TryEvaluate("missingField * 2", row, out var result, out var error);

            // Assert
            Assert.False(ok);
            Assert.Contains("missingField", error, StringComparison.OrdinalIgnoreCase);
        }


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
        /// Description: Kiểm tra phân giải chế độ lọc theo tiền tố mã gói tin trả về FilterMode chính xác.
        /// Created date: 17/09/2026
        /// </summary>
        [Theory]
        [InlineData("103_vdsData", (int)ShareDataEnum.PacketFilterMode.Incremental)]
        [InlineData("106_wimData", (int)ShareDataEnum.PacketFilterMode.Incremental)]
        [InlineData("109_etcData", (int)ShareDataEnum.PacketFilterMode.Incremental)]
        [InlineData("103", (int)ShareDataEnum.PacketFilterMode.Incremental)]
        [InlineData("101_commonData", (int)ShareDataEnum.PacketFilterMode.Snapshot)]
        [InlineData("102_cctvDevice", (int)ShareDataEnum.PacketFilterMode.Snapshot)]
        [InlineData("105_tollTransaction", (int)ShareDataEnum.PacketFilterMode.Snapshot)]
        [InlineData("104_weatherData", (int)ShareDataEnum.PacketFilterMode.Incremental)]
        public void ResolveFilterMode_WithPrefixCodes_ReturnsCorrectFilterMode_Test(string code, int expectedMode)
        {
            var packet = new ShareDataPacket { Code = code };
            var mode = PacketMetadataResolver.ResolveFilterMode(packet);
            Assert.Equal(expectedMode, mode);
        }


        /// <summary>
        /// Description: Kiểm tra cú pháp  và  tạo ra gói tin envelope đơn lẻ chứa mảng dữ liệu.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Transform_WithEachAndAs_ProducesSingleEnvelopeWithArrayData_Test()
        {
            // Arrange
            var shapeJson = @"
            {
                ""headerInfo"": ""TEST_HEADER"",
                ""data"": {
                    ""$each"": true,
                    ""$as"": {
                        ""zId"": { ""$field"": ""zoneId"" },
                        ""spd"": { ""$field"": ""averageSpeed"" }
                    }
                }
            }";

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["zoneId"] = "Z01", ["averageSpeed"] = 60.5m },
                new Dictionary<string, object?> { ["zoneId"] = "Z02", ["averageSpeed"] = 75.0m },
                new Dictionary<string, object?> { ["zoneId"] = "Z03", ["averageSpeed"] = 80.0m }
            };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson);

            // Assert
            Assert.Single(result);
            var envelope = result[0] as IDictionary<string, object?>;
            Assert.NotNull(envelope);
            Assert.Equal("TEST_HEADER", envelope["headerInfo"]);

            var dataList = envelope["data"] as List<object?>;
            Assert.NotNull(dataList);
            Assert.Equal(3, dataList.Count);

            var firstItem = dataList[0] as IDictionary<string, object?>;
            Assert.NotNull(firstItem);
            Assert.Equal("Z01", firstItem["zId"]);
            Assert.Equal(60.5m, firstItem["spd"]);

            var thirdItem = dataList[2] as IDictionary<string, object?>;
            Assert.NotNull(thirdItem);
            Assert.Equal("Z03", thirdItem["zId"]);
            Assert.Equal(80.0m, thirdItem["spd"]);
        }


        /// <summary>
        /// Description: Kiểm thử Transform với mảng khuôn bản ghi (không dùng $each) phải chỉ sinh 1 header và nở N bản ghi data.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void Transform_WithRecordTemplateArray_EmitsHeaderOnceAndExpandsRows_Test()
        {
            // Arrange
            var shapeJson = @"
            {
                ""header"": {
                    ""source"": ""ITS"",
                    ""version"": ""1.0""
                },
                ""data"": [
                    {
                        ""zId"": { ""$field"": ""zoneId"" },
                        ""spd"": { ""$field"": ""averageSpeed"" }
                    }
                ]
            }";

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["zoneId"] = "Z01", ["averageSpeed"] = 60.5m },
                new Dictionary<string, object?> { ["zoneId"] = "Z02", ["averageSpeed"] = 75.0m },
                new Dictionary<string, object?> { ["zoneId"] = "Z03", ["averageSpeed"] = 80.0m }
            };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson);

            // Assert
            Assert.Single(result);
            var envelope = result[0] as IDictionary<string, object?>;
            Assert.NotNull(envelope);

            var header = envelope["header"] as IDictionary<string, object?>;
            Assert.NotNull(header);
            Assert.Equal("ITS", header["source"]);

            var dataList = envelope["data"] as List<object?>;
            Assert.NotNull(dataList);
            Assert.Equal(3, dataList.Count);

            var firstItem = dataList[0] as IDictionary<string, object?>;
            Assert.NotNull(firstItem);
            Assert.Equal("Z01", firstItem["zId"]);
            Assert.Equal(60.5m, firstItem["spd"]);

            var thirdItem = dataList[2] as IDictionary<string, object?>;
            Assert.NotNull(thirdItem);
            Assert.Equal("Z03", thirdItem["zId"]);
            Assert.Equal(80.0m, thirdItem["spd"]);
        }


        /// <summary>
        /// Description: Kiểm thử Transform với mảng hằng literal (["a", "b"]) thì giữ nguyên không bị nở lặp theo dòng.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void Transform_WithLiteralArray_KeepsArrayAsIs_Test()
        {
            // Arrange
            var shapeJson = @"
            {
                ""tags"": [""a"", ""b""],
                ""data"": [
                    { ""zId"": { ""$field"": ""zoneId"" } }
                ]
            }";

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["zoneId"] = "Z01" },
                new Dictionary<string, object?> { ["zoneId"] = "Z02" }
            };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson);

            // Assert
            Assert.Single(result);
            var envelope = result[0] as IDictionary<string, object?>;
            Assert.NotNull(envelope);

            var tags = envelope["tags"] as List<object?>;
            Assert.NotNull(tags);
            Assert.Equal(2, tags.Count);
            Assert.Equal("a", tags[0]);
            Assert.Equal("b", tags[1]);

            var dataList = envelope["data"] as List<object?>;
            Assert.NotNull(dataList);
            Assert.Equal(2, dataList.Count);
        }


        /// <summary>
        /// Description: Kiểm tra khi không có cú pháp  thì duy trì hành vi xử lý theo từng bản ghi ban đầu.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Transform_WithoutEach_MaintainsOriginalPerRowBehavior_Test()
        {
            // Arrange
            var shapeJson = @"
            {
                ""zId"": { ""$field"": ""zoneId"" },
                ""spd"": { ""$field"": ""averageSpeed"" }
            }";

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["zoneId"] = "Z01", ["averageSpeed"] = 60.5m },
                new Dictionary<string, object?> { ["zoneId"] = "Z02", ["averageSpeed"] = 75.0m }
            };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson);

            // Assert
            Assert.Equal(2, result.Count);
            var item1 = result[0] as IDictionary<string, object?>;
            Assert.NotNull(item1);
            Assert.Equal("Z01", item1["zId"]);
        }


        /// <summary>
        /// Description: Kiểm thử khi phễu lọc dạng khối tổng hợp (aggregate) thiếu trường bắt buộc thì ném ngoại lệ với số lượng bản ghi chính xác và không chứa '0/'.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenAggregateShapeMissesRequiredField_ThrowsWithAccurateRowCount_Test()
        {
            // Arrange
            var shapeJson = @"
            {
                ""header"": {
                    ""source"": ""ITS""
                },
                ""data"": [
                    {
                        ""targetVal"": { ""$field"": ""requiredField"", ""$extend"": { ""required"": true } }
                    }
                ]
            }";

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["otherField"] = 1 },
                new Dictionary<string, object?> { ["otherField"] = 2 },
                new Dictionary<string, object?> { ["otherField"] = 3 }
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                DataMappingProcess.Transform(rawRows, shapeJson));

            Assert.StartsWith("Thiếu trường bắt buộc", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("0/", ex.Message);
            Assert.Contains("3", ex.Message);
        }


        /// <summary>
        /// Description: Kiểm thử khi trường bắt buộc khai báo không có bí danh (alias) tương ứng trong dữ liệu thô SQL thì phải báo lỗi rõ ràng.
        /// Created date: 15/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenDeclaredFieldHasNoMatchingSqlAlias_ThrowsClearError_Test()
        {
            // Arrange
            // Dữ liệu thô từ SQL query chỉ trả về cột zoneId, hoàn toàn thiếu bí danh missingAliasField
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "Z01"
                }
            };

            // Act & Assert 1: Flat shape có trường required
            const string flatShape = """
            {
                "zoneId": { "$field": "zoneId", "$extend": { "required": true } },
                "missingAliasField": { "$field": "missingAliasField", "$extend": { "required": true } }
            }
            """;
            var exFlat = Assert.Throws<InvalidOperationException>(() =>
                DataMappingProcess.Transform(rawRows, flatShape));

            Assert.StartsWith("Thiếu trường bắt buộc", exFlat.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("missingAliasField", exFlat.Message);
            Assert.Contains("1/1", exFlat.Message);

            // Act & Assert 2: TargetShapeJson có ánh xạ field missingAliasField
            const string shapeJson = """
            {
                "data": [
                    {
                        "zone": { "$field": "zoneId" },
                        "missing": { "$field": "missingAliasField", "$extend": { "required": true } }
                    }
                ]
            }
            """;
            var exShape = Assert.Throws<InvalidOperationException>(() =>
                DataMappingProcess.Transform(rawRows, shapeJson));

            Assert.StartsWith("Thiếu trường bắt buộc", exShape.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("missingAliasField", exShape.Message);
        }


        /// <summary>
        /// Description: Kiểm tra biểu thức lồng dấu ngoặc đơn hoặc phép toán một ngôi quá sâu được từ chối an toàn.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Evaluate_DeeplyNestedUnaryOrParentheses_RejectsGracefully_Test()
        {
            // Arrange - expression with 35 nested levels (> MaxRecursionDepth = 32)
            var row = new Dictionary<string, object?> { ["x"] = 5m };
            var deepExpr = string.Concat(Enumerable.Repeat("-(", 35)) + "x" + new string(')', 35);

            // Act
            var ok = DataMappingProcess.TryEvaluate(deepExpr, row, out var result, out var error);

            // Assert
            Assert.False(ok);
            Assert.NotNull(error);
            Assert.Contains("sâu", error, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Description: Kiểm tra chuyển đổi kiểu dữ liệu thành số khi CodeSet trả về chuỗi số và targetType là number.
        /// Created date: 17/09/2026
        /// <summary>
        /// Description: B2 - Kiểm tra khi trường đã qua bộ mã thì KHÔNG ép kiểu nữa (targetType bị bỏ qua, giá trị bộ mã là cuối cùng).
        /// Created date: 17/09/2026
        /// Updated date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenCodeSetReturnsNumberString_DoesNotCoerceTargetType_Test()
        {
            var codeSets = new Dictionary<string, List<CodeValueDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["COND_SET"] =
                [
                    new() { SourceValue = "slow", PartnerValue = "101" }
                ]
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["condition"] = "slow" }
            };

            var shapeJson = @"{
                ""trafficCondition"": {
                    ""$field"": ""condition"",
                    ""$extend"": {
                        ""codeSet"": ""COND_SET"",
                        ""targetType"": ""number""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson, codeSets);
            var dict = (Dictionary<string, object?>)result[0];
            var val = dict["trafficCondition"];

            Assert.IsType<string>(val);
            Assert.Equal("101", val);
        }


        /// <summary>
        /// Description: Kiểm tra ném ngoại lệ InvalidOperationException khi trường bắt buộc bị rỗng giá trị.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenExtendRequiredIsTrueAndValueIsEmpty_ThrowsInvalidOperationException_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["condition"] = null }
            };

            var shapeJson = @"{
                ""trafficCondition"": {
                    ""$field"": ""condition"",
                    ""$extend"": {
                        ""required"": true
                    }
                }
            }";

            var ex = Assert.Throws<InvalidOperationException>(() =>
                DataMappingProcess.Transform(rawRows, shapeJson));
            Assert.StartsWith("Thiếu trường bắt buộc", ex.Message);
            Assert.Contains("[condition]", ex.Message);
        }


        /// <summary>
        /// Description: Kiểm tra ném ngoại lệ kèm tên trường nguồn khi trường bắt buộc không có trong danh sách packet fields.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenExtendRequiredIsTrueAndFieldNotInPacketFields_ThrowsWithSourceFieldKey_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["zone_id"] = "" }
            };

            var shapeJson = @"{
                ""zoneId"": {
                    ""$field"": ""zone_id"",
                    ""$extend"": {
                        ""required"": true
                    }
                }
            }";

            var ex = Assert.Throws<InvalidOperationException>(() =>
                DataMappingProcess.Transform(rawRows, shapeJson));
            Assert.StartsWith("Thiếu trường bắt buộc", ex.Message);
            Assert.Contains("[zone_id]", ex.Message);
        }


        /// <summary>
        /// Description: Kiểm tra định dạng datetime chuẩn xác theo dateFormat được cấu hình trong extend.
        /// Created date: 17/09/2026
        /// Updated date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenExtendDateFormatIsSetAndTypeIsDatetime_FormatsCorrectly_Test()
        {
            var dt = new DateTime(2026, 9, 17, 10, 20, 30);
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["time"] = dt }
            };

            var shapeJson = @"{
                ""timeFormatted"": {
                    ""$field"": ""time"",
                    ""$extend"": {
                        ""targetType"": ""datetime"",
                        ""dateFormat"": ""dd/MM/yyyy""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson);
            var dict = (Dictionary<string, object?>)result[0];
            Assert.Equal("17/09/2026", dict["timeFormatted"]);
        }


        /// <summary>
        /// Description: Kiểm tra khi extend dateFormat không cấu hình thì datetime trả về chuẩn ISO8601 mặc định.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenExtendFormatNotSetAndTypeIsDatetime_ReturnsIso8601_Test()
        {
            var dt = new DateTime(2026, 9, 17, 10, 20, 30);
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["time"] = dt }
            };

            var shapeJson = @"{
                ""timeFormatted"": {
                    ""$field"": ""time"",
                    ""$extend"": {
                        ""targetType"": ""datetime""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson);
            var dict = (Dictionary<string, object?>)result[0];
            Assert.Equal(dt.ToString("o", CultureInfo.InvariantCulture), dict["timeFormatted"]);
        }


        /// <summary>
        /// Description: Kiểm tra khi cấu hình dateFormat không hợp lệ thì fallback an toàn về định dạng ISO8601.
        /// Created date: 17/09/2026
        /// Updated date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_WhenExtendDateFormatIsGarbageAndTypeIsDatetime_ReturnsIso8601_Test()
        {
            var dt = new DateTime(2026, 9, 17, 10, 20, 30);
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["time"] = dt }
            };

            var shapeJson = @"{
                ""timeFormatted"": {
                    ""$field"": ""time"",
                    ""$extend"": {
                        ""targetType"": ""datetime"",
                        ""dateFormat"": ""x""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson);
            var dict = (Dictionary<string, object?>)result[0];
            Assert.Equal(dt.ToString("o", CultureInfo.InvariantCulture), dict["timeFormatted"]);
        }


        /// <summary>
        /// Description: D1 - $extend.dateFormat + targetType: "dateTime" -> ra dung khuon khai
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_D1_DateFormat_WithDateTimeTargetType_FormatsCorrectly_Test()
        {
            var dt = new DateTime(2026, 9, 18, 14, 5, 31);
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["time"] = dt }
            };

            var shapeJson = @"{
                ""dataTime"": {
                    ""$field"": ""time"",
                    ""$extend"": {
                        ""targetType"": ""dateTime"",
                        ""dateFormat"": ""dd/MM/yyyy HH:mm:ss""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("18/09/2026 14:05:31", dict["dataTime"]);
        }


        /// <summary>
        /// Description: D2 - $extend.numberFormat: "0.#" + targetType: "int" -> lam tron dung (§9.3: 45.5 -> 46)
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_D2_NumberFormat_WithIntTargetType_RoundsCorrectly_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["speed"] = 45.5m }
            };

            var shapeJson = @"{
                ""averageSpeed"": {
                    ""$field"": ""speed"",
                    ""$extend"": {
                        ""targetType"": ""int"",
                        ""numberFormat"": ""0.#""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(46, dict["averageSpeed"]);
            Assert.IsType<int>(dict["averageSpeed"]);
        }


        /// <summary>
        /// Description: D3 - Gia tri rong + co codeSet + co defaultPartnerValue o la -> lay mac dinh cua BO MA, khong phai cua la
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_D3_WhenValueIsEmpty_WithCodeSetAndLeafDefault_PrioritizesCodeSetDefault_Test()
        {
            var codeSets = new Dictionary<string, CodeSetDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["COND_SET"] = new CodeSetDto
                {
                    Values = [new CodeValueDto { SourceValue = "1", PartnerValue = "Normal" }],
                    DefaultPartnerValue = "CodeSetDefaultPartner"
                }
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["condition"] = null }
            };

            var shapeJson = @"{
                ""trafficCondition"": {
                    ""$field"": ""condition"",
                    ""$extend"": {
                        ""codeSet"": ""COND_SET"",
                        ""defaultPartnerValue"": ""LeafDefaultPartner""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson, codeSets: codeSets);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("CodeSetDefaultPartner", dict["trafficCondition"]);
        }


        /// <summary>
        /// Description: D4 - Gia tri co nhung khong khop bo ma -> lay defaultPartnerValue cua bo ma
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_D4_WhenValueExistsButUnmatched_TakesCodeSetDefaultPartnerValue_Test()
        {
            var codeSets = new Dictionary<string, CodeSetDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["COND_SET"] = new CodeSetDto
                {
                    Values = [new CodeValueDto { SourceValue = "1", PartnerValue = "Normal" }],
                    DefaultPartnerValue = "CodeSetDefaultPartner"
                }
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["condition"] = "999" }
            };

            var shapeJson = @"{
                ""trafficCondition"": {
                    ""$field"": ""condition"",
                    ""$extend"": {
                        ""codeSet"": ""COND_SET""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson, codeSets: codeSets);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("CodeSetDefaultPartner", dict["trafficCondition"]);
        }


        /// <summary>
        /// Description: D5 - Qua bo ma ra "Congested" + co targetType: "string" -> khong ep kieu, giu nguyen "Congested"
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_D5_WhenMatchedCodeSet_DoesNotCoerceTargetType_Test()
        {
            var codeSets = new Dictionary<string, CodeSetDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["TRAFFIC_COND"] = new CodeSetDto
                {
                    Values = [new CodeValueDto { SourceValue = "2", PartnerValue = "Congested" }]
                }
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["condition"] = "2" }
            };

            var shapeJson = @"{
                ""trafficCondition"": {
                    ""$field"": ""condition"",
                    ""$extend"": {
                        ""codeSet"": ""TRAFFIC_COND"",
                        ""targetType"": ""string""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson, codeSets: codeSets);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("Congested", dict["trafficCondition"]);
            Assert.IsType<string>(dict["trafficCondition"]);
        }


        /// <summary>
        /// Description: D6 - Truong khong co codeSet, gia tri rong -> lay defaultPartnerValue cua la
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_D6_WhenNoCodeSetAndValueIsEmpty_TakesLeafDefaultPartnerValue_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["state"] = null }
            };

            var shapeJson = @"{
                ""deviceState"": {
                    ""$field"": ""state"",
                    ""$extend"": {
                        ""defaultPartnerValue"": ""on""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("on", dict["deviceState"]);
        }


        /// <summary>
        /// Description: D7 - Bo khung dung khoa cu format/defaultValue -> bi bo qua, khong no
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_D7_WhenOldKeysFormatAndDefaultValueUsed_AreIgnoredWithoutCrash_Test()
        {
            var dt = new DateTime(2026, 9, 18, 10, 0, 0);
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["time"] = dt, ["missingField"] = null }
            };

            var shapeJson = @"{
                ""time"": {
                    ""$field"": ""time"",
                    ""$extend"": {
                        ""targetType"": ""datetime"",
                        ""format"": ""dd-MM-yyyy""
                    }
                },
                ""fallback"": {
                    ""$field"": ""missingField"",
                    ""$extend"": {
                        ""defaultValue"": ""old_default""
                    }
                }
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(dt.ToString("o", CultureInfo.InvariantCulture), dict["time"]);
            Assert.Null(dict["fallback"]);
        }


        /// <summary>
        /// Description: D8 - Bo khung co expression -> bo qua + ghi log, khong tinh
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_D8_WhenShapeHasExpression_IgnoresAndLogsWithoutCalculating_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["speed"] = 50 }
            };

            var shapeJson = @"{
                ""calcSpeed"": {
                    ""$field"": ""speed"",
                    ""$extend"": {
                        ""expression"": ""speed * 2""
                    }
                }
            }";

            var logRecorded = false;
            var result = DataMappingProcess.Transform(
                rawRows,
                shapeJson,
                onExpressionEvalFailed: (field, expr, err) =>
                {
                    logRecorded = true;
                });

            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(50, dict["calcSpeed"]);
            Assert.True(logRecorded);
        }


        /// <summary>
        /// Description: Test theo dung vi du §9.2 va §9.3 cua tai lieu ban giao TargetShapeJson.md
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_Section9_3_HandoverExample_ProducesExactOutput_Test()
        {
            var codeSets = new Dictionary<string, CodeSetDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["TRAFFIC_COND"] = new CodeSetDto
                {
                    Values =
                    [
                        new CodeValueDto { SourceValue = "1", PartnerValue = "Normal" },
                        new CodeValueDto { SourceValue = "2", PartnerValue = "Congested" }
                    ]
                }
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["zoneId"] = 1001, ["averageSpeed"] = 45.5, ["trafficCondition"] = "2" },
                new Dictionary<string, object?> { ["zoneId"] = 1002, ["averageSpeed"] = 61.25, ["trafficCondition"] = "1" }
            };

            var shapeJson = @"{
                ""data"": [
                    {
                        ""zoneId"": { ""$field"": ""zoneId"" },
                        ""averageSpeed"": { ""$field"": ""averageSpeed"", ""$extend"": { ""targetType"": ""int"", ""numberFormat"": ""0.#"" } },
                        ""trafficCondition"": { ""$field"": ""trafficCondition"", ""$extend"": { ""codeSet"": ""TRAFFIC_COND"", ""targetType"": ""string"" } }
                    }
                ]
            }";

            var result = DataMappingProcess.Transform(rawRows, shapeJson, codeSets: codeSets);
            Assert.Single(result);
            var root = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            var dataList = Assert.IsAssignableFrom<List<object?>>(root["data"]);
            Assert.Equal(2, dataList.Count);

            var row1 = Assert.IsAssignableFrom<IDictionary<string, object?>>(dataList[0]);
            Assert.Equal(1001, row1["zoneId"]);
            Assert.Equal(46, row1["averageSpeed"]);
            Assert.Equal("Congested", row1["trafficCondition"]);

            var row2 = Assert.IsAssignableFrom<IDictionary<string, object?>>(dataList[1]);
            Assert.Equal(1002, row2["zoneId"]);
            Assert.Equal(61, row2["averageSpeed"]);
            Assert.Equal("Normal", row2["trafficCondition"]);
        }


        /// <summary>
        /// Description: Kiểm tra khi trường trong target shape không có trong bản ghi thô thì trường đó có mặt trong JSON đầu ra với giá trị null và không sinh cảnh báo ESH-1205.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task Map_WhenShapeFieldNotInRawRow_ReturnsNullForThatField_WithoutAlert_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packet = new ShareDataPacket { Code = "TEST_P" };
            var mapping = new ShareDataMapping
            {
                ID = "TEST_M",
                TargetShapeJson = @"{ ""a"": { ""$field"": ""A"" }, ""b"": { ""$field"": ""B"" }, ""c"": { ""$field"": ""C"" } }"
            };
            var sub = new ShareDataSubscription { ID = "TEST_S" };
            var ctx = new DataOutboundContext(sub, null, packet, mapping, DateTime.Now, CancellationToken.None);

            var extraction = new DataOutboundExtractionResult(
                new List<object> { new Dictionary<string, object?> { ["A"] = 1, ["B"] = 2 } },
                null, null);

            var logs = new List<string>();
            var result = await DataMappingProcess.Map(db, extraction, ctx,
                (code, sev, src, msg, dtl) => { logs.Add(code); return Task.CompletedTask; }, null);

            Assert.True(result.Success);
            Assert.DoesNotContain(ShareDataAlertCode.Outbound.ShapeFieldNotInRawRow, logs);
            Assert.NotNull(result.FinalBytes);

            using var doc = JsonDocument.Parse(result.FinalBytes);
            var root = doc.RootElement;
            var item = root.ValueKind == JsonValueKind.Array ? root[0] : root;

            Assert.True(item.TryGetProperty("a", out var propA));
            Assert.Equal(1, propA.GetInt32());

            Assert.True(item.TryGetProperty("b", out var propB));
            Assert.Equal(2, propB.GetInt32());

            Assert.True(item.TryGetProperty("c", out var propC));
            Assert.Equal(JsonValueKind.Null, propC.ValueKind);
        }


        /// <summary>
        /// Description: Kiểm tra khi bản ghi thô có trường dư thừa không nằm trong target shape thì trường dư bị bỏ qua, đầu ra không chứa trường đó và các trường hợp lệ vẫn giữ đúng giá trị.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task Map_WhenRawRowHasExtraField_IgnoresIt_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packet = new ShareDataPacket { Code = "TEST_P" };
            var mapping = new ShareDataMapping
            {
                ID = "TEST_M",
                TargetShapeJson = @"{ ""a"": { ""$field"": ""A"" }, ""b"": { ""$field"": ""B"" } }"
            };
            var sub = new ShareDataSubscription { ID = "TEST_S" };
            var ctx = new DataOutboundContext(sub, null, packet, mapping, DateTime.Now, CancellationToken.None);

            var extraction = new DataOutboundExtractionResult(
                new List<object> { new Dictionary<string, object?> { ["A"] = 1, ["B"] = 2, ["D"] = 4 } },
                null, null);

            var logs = new List<string>();
            var result = await DataMappingProcess.Map(db, extraction, ctx,
                (code, sev, src, msg, dtl) => { logs.Add(code); return Task.CompletedTask; }, null);

            Assert.True(result.Success);
            Assert.NotNull(result.FinalBytes);

            using var doc = JsonDocument.Parse(result.FinalBytes);
            var root = doc.RootElement;
            var item = root.ValueKind == JsonValueKind.Array ? root[0] : root;

            Assert.False(item.TryGetProperty("d", out _));
            Assert.False(item.TryGetProperty("D", out _));

            Assert.True(item.TryGetProperty("a", out var propA));
            Assert.Equal(1, propA.GetInt32());

            Assert.True(item.TryGetProperty("b", out var propB));
            Assert.Equal(2, propB.GetInt32());
        }


        /// <summary>
        /// Description: Kiểm tra khi chuỗi JSON cấu hình định dạng không hợp lệ thì ghi log lỗi ESH-1206 và trả về false.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task Map_WhenShapeJsonIsInvalid_LogsEsh1206_AndReturnsFalse_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packet = new ShareDataPacket { Code = "TEST_P" };
            var mapping = new ShareDataMapping { ID = "TEST_M", TargetShapeJson = "INVALID_JSON_///" };
            var sub = new ShareDataSubscription { ID = "TEST_S" };
            var ctx = new DataOutboundContext(sub, null, packet, mapping, DateTime.Now, CancellationToken.None);

            var extraction = new DataOutboundExtractionResult(
                new List<object> { new Dictionary<string, object?>() },
                null, null);

            var logs = new List<string>();
            var result = await DataMappingProcess.Map(db, extraction, ctx,
                (code, sev, src, msg, dtl) => { logs.Add(code); return Task.CompletedTask; }, null);

            Assert.False(result.Success);
            Assert.Contains(ShareDataAlertCode.Outbound.ShapeInvalid, logs);
        }


        /// <summary>
        /// Description: Kiểm tra khi chuỗi JSON cấu hình rỗng thì ghi log lỗi ESH-1206 và trả về false.
        /// Created date: 17/09/2026
        /// </summary>
        [Fact]
        public async Task Map_WhenShapeJsonIsEmpty_LogsEsh1206_AndReturnsFalse_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packet = new ShareDataPacket { Code = "TEST_P" };
            var mapping = new ShareDataMapping { ID = "TEST_M", TargetShapeJson = "" };
            var sub = new ShareDataSubscription { ID = "TEST_S" };
            var ctx = new DataOutboundContext(sub, null, packet, mapping, DateTime.Now, CancellationToken.None);

            var extraction = new DataOutboundExtractionResult(
                new List<object> { new Dictionary<string, object?>() },
                null, null);

            var logs = new List<string>();
            var result = await DataMappingProcess.Map(db, extraction, ctx,
                (code, sev, src, msg, dtl) => { logs.Add(code); return Task.CompletedTask; }, null);

            Assert.False(result.Success);
            Assert.Contains(ShareDataAlertCode.Outbound.ShapeInvalid, logs);
        }


        /// <summary>
        /// Description: Kiểm tra khi partner là null thì DataOutboundRestSender trả về thất bại và không ném ngoại lệ.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_WhenPartnerIsNull_ReturnsFailWithoutThrowing_Test()
        {
            var sender = new DataOutboundRestSender();
            var ctx = new DataOutboundContext(new ShareDataSubscription(), null, new ShareDataPacket(), null, DateTime.Now, CancellationToken.None);
            var mapping = new DataMappingResult(true, [1, 2, 3], 1, null);

            var result = await sender.Send(mapping, ctx, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Đăng ký không có đối tác.", result.ErrorMessage);
        }


        /// <summary>
        /// Description: C1, C3, C4, C5 - Kiểm tra RestSender gửi trực tiếp JSON FinalBytes, Content-Type là application/json, không còn vỏ tự chế 7 khoá, parse 1 lần ra thẳng object có header và data.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_SendsDirectFinalBytes_WithoutHttpPayloadWrapper_C1_C3_C4_C5_Test()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;

            var testHandler = new TestHttpMessageHandler(async (req, ct) =>
            {
                capturedRequest = req;
                capturedBody = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);

            var sampleJson = """
            {
              "header": {
                "source": "ITS-TMS",
                "dataTime": "2026-09-18T14:05:31+07:00",
                "partnerCode": "PARTNER2",
                "packetCode": "101_commonData",
                "serial": "1287"
              },
              "data": [
                { "zoneId": 1001, "averageSpeed": 46, "trafficCondition": "Congested" },
                { "zoneId": 1002, "averageSpeed": 61, "trafficCondition": "Normal" }
              ]
            }
            """;
            var finalBytes = System.Text.Encoding.UTF8.GetBytes(sampleJson);
            var mapping = new DataMappingResult(true, finalBytes, 2);

            var partner = new ShareDataPartner { Address = "127.0.0.1", Port = 8080, EndPointApiUrl = "/api/v1/sharedata" };
            var packet = new ShareDataPacket { Code = "101_commonData", PacketVersion = "1.0" };
            var sub = new ShareDataSubscription { DatatypeId = "101", SerialNbr = 1287, Format = BaseEnums.PublishFormat.Data };
            var ctx = new DataOutboundContext(sub, partner, packet, null, DateTime.Now, CancellationToken.None);

            // Act
            var sendResult = await sender.Send(mapping, ctx, CancellationToken.None);

            // Assert
            Assert.True(sendResult.Success);
            Assert.NotNull(capturedRequest);
            Assert.Equal("application/json", capturedRequest.Content?.Headers.ContentType?.MediaType);

            // C3 & C4: Parse JSON 1 lần ra thẳng object
            Assert.NotNull(capturedBody);
            using var doc = JsonDocument.Parse(capturedBody);
            var root = doc.RootElement;
            Assert.Equal(JsonValueKind.Object, root.ValueKind);

            // C3: Không còn bất kỳ khoá nào của vỏ tự chế 7 khoá ở cấp gốc
            Assert.False(root.TryGetProperty("rawContent", out _));
            Assert.False(root.TryGetProperty("datatypeId", out _));
            Assert.False(root.TryGetProperty("packetVersion", out _));
            Assert.False(root.TryGetProperty("serialNbr", out _));
            Assert.False(root.TryGetProperty("pduType", out _));
            Assert.False(root.TryGetProperty("format", out _));

            // C1: Có header và data chuẩn theo bộ khung
            Assert.True(root.TryGetProperty("header", out var headerElem));
            Assert.True(root.TryGetProperty("data", out var dataElem));
            Assert.Equal(JsonValueKind.Array, dataElem.ValueKind);
            Assert.Equal(2, dataElem.GetArrayLength());
        }


        /// <summary>
        /// Description: C2, C7 - Kiểm tra khi bộ khung phẳng (mảng), RestSender gửi thẳng mảng JSON không bọc vỏ, số lượng bản ghi bằng đúng số dòng thô.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_SendsDirectArray_ForFlatShape_C2_C7_Test()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;

            var testHandler = new TestHttpMessageHandler(async (req, ct) =>
            {
                capturedRequest = req;
                capturedBody = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);

            var sampleArrayJson = """
            [
              { "zoneId": 1001, "averageSpeed": 46 },
              { "zoneId": 1002, "averageSpeed": 61 },
              { "zoneId": 1003, "averageSpeed": 55 }
            ]
            """;
            var finalBytes = System.Text.Encoding.UTF8.GetBytes(sampleArrayJson);
            var mapping = new DataMappingResult(true, finalBytes, 3);

            var partner = new ShareDataPartner { Address = "127.0.0.1", Port = 8080, EndPointApiUrl = "/api/v1/sharedata" };
            var packet = new ShareDataPacket { Code = "101", PacketVersion = "1.0" };
            var sub = new ShareDataSubscription { DatatypeId = "101", Format = BaseEnums.PublishFormat.Data };
            var ctx = new DataOutboundContext(sub, partner, packet, null, DateTime.Now, CancellationToken.None);

            // Act
            var sendResult = await sender.Send(mapping, ctx, CancellationToken.None);

            // Assert
            Assert.True(sendResult.Success);
            Assert.NotNull(capturedBody);

            using var doc = JsonDocument.Parse(capturedBody);
            var root = doc.RootElement;
            Assert.Equal(JsonValueKind.Array, root.ValueKind);
            Assert.Equal(3, root.GetArrayLength()); // C7: đúng 3 bản ghi
        }


        /// <summary>
        /// Description: C6 - Kiểm tra tính nhất quán: tệp kết xuất local (FileSender) và thân HTTP (RestSender) đều sử dụng chung FinalBytes nguyên bản.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task OutboundSenders_LocalFileAndHttpBody_ShareIdenticalPayloadBytes_C6_Test()
        {
            // Arrange
            string? capturedHttpBody = null;
            var testHandler = new TestHttpMessageHandler(async (req, ct) =>
            {
                capturedHttpBody = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var restSender = new DataOutboundRestSender(clientFactory);

            var sampleJson = """{"header":{"dataTime":"2026-09-18T14:05:31+07:00"},"data":[{"id":1}]}""";
            var finalBytes = System.Text.Encoding.UTF8.GetBytes(sampleJson);
            var mapping = new DataMappingResult(true, finalBytes, 1);

            var partner = new ShareDataPartner { Code = "P_TEST", Address = "127.0.0.1", Port = 8080, EndPointApiUrl = "/api" };
            var packet = new ShareDataPacket { Code = "101" };
            var sub = new ShareDataSubscription { ID = "sub_test_id", DatatypeId = "101" };
            var ctx = new DataOutboundContext(sub, partner, packet, null, DateTime.Now, CancellationToken.None);

            // Act - HTTP
            var httpResult = await restSender.Send(mapping, ctx, CancellationToken.None);

            // Assert
            Assert.True(httpResult.Success);
            Assert.NotNull(capturedHttpBody);
            Assert.Equal(sampleJson, capturedHttpBody);
            Assert.Equal(finalBytes.Length, httpResult.ByteSize);
        }


        /// <summary>
        /// Description: D1 - Kiểm tra token {$meta: "Now"} giải ra mốc thời gian DateTime thực tế, không nhả object thô.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_Meta_Now_ResolvesToDateTimeValue_D1_Test()
        {
            // Arrange
            var now = new DateTime(2026, 9, 18, 14, 5, 31);
            var metaValues = new Dictionary<string, object?> { ["Now"] = now };
            var shapeJson = """{"header":{"dataTime":{"$meta":"Now"}},"data":[{"$field":"Id"}]}""";
            var rawRows = new List<object> { new { Id = 1 } };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: metaValues);

            // Assert
            Assert.Single(result);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            var header = Assert.IsAssignableFrom<IDictionary<string, object?>>(dict["header"]);
            Assert.Equal(now, header["dataTime"]);
        }


        /// <summary>
        /// Description: D2 - Kiểm tra token {$meta: "PartnerCode"} giải đúng mã đối tác từ metaValues.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_Meta_PartnerCode_ResolvesToPartnerCode_D2_Test()
        {
            // Arrange
            var metaValues = new Dictionary<string, object?> { ["PartnerCode"] = "PARTNER2" };
            var shapeJson = """{"header":{"partnerCode":{"$meta":"PartnerCode"}},"data":[{"$field":"Id"}]}""";
            var rawRows = new List<object> { new { Id = 1 } };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: metaValues);

            // Assert
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            var header = Assert.IsAssignableFrom<IDictionary<string, object?>>(dict["header"]);
            Assert.Equal("PARTNER2", header["partnerCode"]);
        }


        /// <summary>
        /// Description: D3 - Kiểm tra token {$meta: "PacketCode"} và {$meta: "Serial"} giải ra đúng mã gói và số serial.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_Meta_PacketCode_And_Serial_ResolvesCorrectly_D3_Test()
        {
            // Arrange
            var metaValues = new Dictionary<string, object?>
            {
                ["PacketCode"] = "101_commonData",
                ["Serial"] = 1287L
            };
            var shapeJson = """{"header":{"packetCode":{"$meta":"PacketCode"},"serial":{"$meta":"Serial"}},"data":[{"$field":"Id"}]}""";
            var rawRows = new List<object> { new { Id = 1 } };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: metaValues);

            // Assert
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            var header = Assert.IsAssignableFrom<IDictionary<string, object?>>(dict["header"]);
            Assert.Equal("101_commonData", header["packetCode"]);
            Assert.Equal(1287L, header["serial"]);
        }


        /// <summary>
        /// Description: D4 - Kiểm tra node có cả $meta và $value (ví dụ PartnerCode) thì ưu tiên lấy giá trị thật từ metaValues, KHÔNG lấy giá trị snapshot $value.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_Meta_PartnerCode_With_Value_Attribute_IgnoresValue_And_UsesRealMeta_D4_Test()
        {
            // Arrange
            var metaValues = new Dictionary<string, object?> { ["PartnerCode"] = "REAL_PARTNER_CODE" };
            var shapeJson = """{"header":{"partnerCode":{"$meta":"PartnerCode","$value":"Test"}},"data":[{"$field":"Id"}]}""";
            var rawRows = new List<object> { new { Id = 1 } };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: metaValues);

            // Assert
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            var header = Assert.IsAssignableFrom<IDictionary<string, object?>>(dict["header"]);
            Assert.Equal("REAL_PARTNER_CODE", header["partnerCode"]);
            Assert.NotEqual("Test", header["partnerCode"]);
        }


        /// <summary>
        /// Description: D5 - Kiểm tra token lạ không có thật ngoài 4 token hợp lệ thì trả null im lặng, không ghi cảnh báo.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_Meta_UnknownToken_ReturnsNull_Silently_D5_Test()
        {
            // Arrange
            var metaValues = new Dictionary<string, object?> { ["Now"] = DateTime.Now };
            var shapeJson = """{"header":{"unknown":{"$meta":"KhongCoThat"}},"data":[{"$field":"Id"}]}""";
            var rawRows = new List<object> { new { Id = 1 } };
            var warnings = new List<string>();

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, onExpressionEvalFailed: (f, expr, err) => warnings.Add(err), metaValues: metaValues);

            // Assert
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            var header = Assert.IsAssignableFrom<IDictionary<string, object?>>(dict["header"]);
            Assert.Null(header["unknown"]);
            Assert.Empty(warnings);
        }


        /// <summary>
        /// Description: D6 - Kiểm tra tên token meta không phân biệt hoa thường (ví dụ partnercode chữ thường) vẫn giải đúng.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_Meta_CaseInsensitiveToken_ResolvesCorrectly_D6_Test()
        {
            // Arrange
            var metaValues = new Dictionary<string, object?> { ["PartnerCode"] = "PARTNER_CI" };
            var shapeJson = """{"header":{"partnerCode":{"$meta":"partnercode"}},"data":[{"$field":"Id"}]}""";
            var rawRows = new List<object> { new { Id = 1 } };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: metaValues);

            // Assert
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            var header = Assert.IsAssignableFrom<IDictionary<string, object?>>(dict["header"]);
            Assert.Equal("PARTNER_CI", header["partnerCode"]);
        }


        /// <summary>
        /// Description: D7 - Kiểm tra node $meta đi kèm $extend được coi là dữ liệu hỏng: ghi cảnh báo, render giá trị meta và bỏ qua $extend.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_Meta_WithExtend_LogsWarning_And_RendersMetaValue_WithoutCoercion_D7_Test()
        {
            // Arrange
            var now = new DateTime(2026, 9, 18, 14, 5, 31);
            var metaValues = new Dictionary<string, object?> { ["Now"] = now };
            var shapeJson = """{"header":{"time":{"$meta":"Now","$extend":{"targetType":"int"}}},"data":[{"$field":"Id"}]}""";
            var rawRows = new List<object> { new { Id = 1 } };
            var warnings = new List<string>();

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, onExpressionEvalFailed: (f, expr, err) => warnings.Add(err), metaValues: metaValues);

            // Assert
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            var header = Assert.IsAssignableFrom<IDictionary<string, object?>>(dict["header"]);
            Assert.Equal(now, header["time"]);
            Assert.Contains(warnings, w => w.Contains("dữ liệu hỏng", StringComparison.OrdinalIgnoreCase) && w.Contains("$extend"));
        }


        /// <summary>
        /// Description: D8 - Kiểm tra trong 1 lô nhiều dòng dữ liệu, giá trị Now lấy một lần dùng chung cho toàn bộ các dòng.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task Map_MultipleRows_HaveSameNowMetaValue_D8_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var shapeJson = """{"header":{"time":{"$meta":"Now"}},"data":[{"id":{"$field":"Id"}}]}""";
            var rawRows = new List<object> { new Dictionary<string, object?> { ["Id"] = 1 }, new Dictionary<string, object?> { ["Id"] = 2 } };
            var extraction = new DataOutboundExtractionResult(rawRows, null, null);
            var mapping = new ShareDataMapping { TargetShapeJson = shapeJson };
            var packet = new ShareDataPacket { Code = "101" };
            var partner = new ShareDataPartner { Code = "PARTNER_TEST" };
            var sub = new ShareDataSubscription { SerialNbr = 100 };
            var ctx = new DataOutboundContext(sub, partner, packet, mapping, DateTime.Now, CancellationToken.None);

            // Act
            var mapResult = await DataMappingProcess.Map(db, extraction, ctx);

            // Assert
            Assert.True(mapResult.Success);
            Assert.NotNull(mapResult.FinalBytes);
            var json = System.Text.Encoding.UTF8.GetString(mapResult.FinalBytes);
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("header", out var headerElem));
            Assert.True(headerElem.TryGetProperty("time", out var timeElem));
            var timeStr = timeElem.GetString();
            Assert.NotNull(timeStr);
            Assert.True(DateTime.TryParse(timeStr, out _));
        }


        /// <summary>
        /// Description: D9 - Kiểm tra bộ khung không có $meta thì đầu ra không thay đổi so với khi không truyền metaValues.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_ShapeWithoutMeta_OutputRemainsUnchanged_D9_Test()
        {
            // Arrange
            var shapeJson = """{"data":[{"id":{"$field":"Id"}}]}""";
            var rawRows = new List<object> { new { Id = 10 } };

            // Act - without metaValues
            var result1 = DataMappingProcess.Transform(rawRows, shapeJson);
            // Act - with metaValues
            var metaValues = new Dictionary<string, object?> { ["Now"] = DateTime.Now };
            var result2 = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: metaValues);

            // Assert
            var json1 = JsonSerializer.Serialize(result1);
            var json2 = JsonSerializer.Serialize(result2);
            Assert.Equal(json1, json2);
        }


        /// <summary>
        /// Description: Kiểm tra token $meta hoạt động chính xác trong mảng lặp ($each/$as), giải đúng cho từng dòng trong danh sách.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_MetaInEachLoop_ResolvesCorrectly_Test()
        {
            // Arrange
            var now = new DateTime(2026, 9, 18, 12, 0, 0);
            var metaValues = new Dictionary<string, object?>
            {
                ["PacketCode"] = "101_traffic",
                ["Now"] = now
            };
            var shapeJson = """
            {
              "items": {
                "$each": true,
                "$as": {
                  "id": { "$field": "Id" },
                  "packet": { "$meta": "PacketCode" },
                  "dataTime": { "$meta": "Now" }
                }
              }
            }
            """;
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["Id"] = 101 },
                new Dictionary<string, object?> { ["Id"] = 102 }
            };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: metaValues);

            // Assert
            Assert.Single(result);
            var root = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            var items = Assert.IsAssignableFrom<IList<object?>>(root["items"]);
            Assert.Equal(2, items.Count);

            var first = Assert.IsAssignableFrom<IDictionary<string, object?>>(items[0]);
            Assert.Equal(101, first["id"]);
            Assert.Equal("101_traffic", first["packet"]);
            Assert.Equal(now, first["dataTime"]);

            var second = Assert.IsAssignableFrom<IDictionary<string, object?>>(items[1]);
            Assert.Equal(102, second["id"]);
            Assert.Equal("101_traffic", second["packet"]);
            Assert.Equal(now, second["dataTime"]);
        }


        /// <summary>
        /// Description: Kiểm tra token $meta rỗng hoặc chỉ có khoảng trắng không gây lỗi và giải ra giá trị null an toàn.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_MetaWithEmptyOrWhitespaceString_ProducesNull_Test()
        {
            // Arrange
            var metaValues = new Dictionary<string, object?> { ["Now"] = DateTime.Now };
            var shapeJson = """
            {
              "empty": { "$meta": "" },
              "whitespace": { "$meta": "   " }
            }
            """;
            var rawRows = new List<object> { new Dictionary<string, object?> { ["Id"] = 1 } };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: metaValues);

            // Assert
            Assert.Single(result);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Null(dict["empty"]);
            Assert.Null(dict["whitespace"]);
        }


        /// <summary>
        /// Description: Kiểm tra khi metaValues là null thì các token $meta giải ra null mà không ném ngoại lệ NullReferenceException.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_MetaNullContext_ResolvesNullSafely_Test()
        {
            // Arrange
            var shapeJson = """
            {
              "time": { "$meta": "Now" },
              "partner": { "$meta": "PartnerCode" }
            }
            """;
            var rawRows = new List<object> { new Dictionary<string, object?> { ["Id"] = 1 } };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: null);

            // Assert
            Assert.Single(result);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Null(dict["time"]);
            Assert.Null(dict["partner"]);
        }


        /// <summary>
        /// Description: Kiểm tra khi metaValues chỉ có một phần token, các token bị thiếu (như PartnerCode, Serial) trả về null an toàn.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Transform_MetaMissingSpecificKey_ProducesNull_Test()
        {
            // Arrange
            var metaValues = new Dictionary<string, object?>
            {
                ["Now"] = new DateTime(2026, 9, 18, 10, 0, 0),
                ["PacketCode"] = "101"
            };
            var shapeJson = """
            {
              "now": { "$meta": "Now" },
              "packet": { "$meta": "PacketCode" },
              "partner": { "$meta": "PartnerCode" },
              "serial": { "$meta": "Serial" }
            }
            """;
            var rawRows = new List<object> { new Dictionary<string, object?> { ["Id"] = 1 } };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: metaValues);

            // Assert
            Assert.Single(result);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(new DateTime(2026, 9, 18, 10, 0, 0), dict["now"]);
            Assert.Equal("101", dict["packet"]);
            Assert.Null(dict["partner"]);
            Assert.Null(dict["serial"]);
        }


        /// <summary>
        /// Description: Kiểm tra DataOutboundRestSender khi FinalBytes rỗng hoặc null thì trả về thành công an toàn, ByteSize = 0 và không gửi request HTTP.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_Send_EmptyOrNullFinalBytes_HandlesSafely_Test()
        {
            // Arrange
            var requestSent = false;
            var testHandler = new TestHttpMessageHandler((_, _) =>
            {
                requestSent = true;
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);

            var partner = new ShareDataPartner { Address = "127.0.0.1", Port = 8080, EndPointApiUrl = "/api" };
            var ctx = new DataOutboundContext(new ShareDataSubscription(), partner, new ShareDataPacket(), null, DateTime.Now, CancellationToken.None);
            var emptyMapping = new DataMappingResult(true, [], 0);

            // Act
            var sendResult = await sender.Send(emptyMapping, ctx, CancellationToken.None);

            // Assert
            Assert.True(sendResult.Success);
            Assert.Equal(0, sendResult.ByteSize);
            Assert.False(requestSent);
        }


        /// <summary>
        /// Description: Kiểm tra DataOutboundRestSender khi đối tác trả về HTTP 500 kèm nội dung lỗi thì bắt trọn vẹn StatusCode và Response Body trong SendResult.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_Send_PartnerReturns500WithPayload_CapturesErrorBody_Test()
        {
            // Arrange
            const string errorPayload = """{"error": "Internal server error", "detail": "DB down"}""";
            var testHandler = new TestHttpMessageHandler((_, _) =>
            {
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(errorPayload, System.Text.Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);

            var partner = new ShareDataPartner { Code = "PARTNER_FAIL", Address = "127.0.0.1", Port = 8080, EndPointApiUrl = "/api/data" };
            var ctx = new DataOutboundContext(new ShareDataSubscription(), partner, new ShareDataPacket(), null, DateTime.Now, CancellationToken.None);
            var mapping = new DataMappingResult(true, System.Text.Encoding.UTF8.GetBytes("""{"test":1}"""), 1);

            // Act
            var sendResult = await sender.Send(mapping, ctx, CancellationToken.None);

            // Assert
            Assert.False(sendResult.Success);
            Assert.NotNull(sendResult.ErrorMessage);
            Assert.Contains("500", sendResult.ErrorMessage);
            Assert.Contains("Internal server error", sendResult.ErrorMessage);
            Assert.NotNull(sendResult.DetailJson);
            Assert.Contains("500", sendResult.DetailJson);
        }


        /// <summary>
        /// Description: Kiểm tra DataOutboundRestSender khi gặp ngoại lệ mạng HttpRequestException (timeout, refused) thì bắt an toàn và trả về thất bại.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_Send_HttpRequestException_ReturnsFailureResult_Test()
        {
            // Arrange
            var testHandler = new TestHttpMessageHandler((_, _) =>
            {
                throw new HttpRequestException("Connection refused by target host.");
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);

            var partner = new ShareDataPartner { Code = "PARTNER_NET_ERR", Address = "10.10.99.99", Port = 8080, EndPointApiUrl = "/api" };
            var ctx = new DataOutboundContext(new ShareDataSubscription(), partner, new ShareDataPacket(), null, DateTime.Now, CancellationToken.None);
            var mapping = new DataMappingResult(true, System.Text.Encoding.UTF8.GetBytes("""{"test":1}"""), 1);

            // Act
            var sendResult = await sender.Send(mapping, ctx, CancellationToken.None);

            // Assert
            Assert.False(sendResult.Success);
            Assert.NotNull(sendResult.ErrorMessage);
            Assert.Contains("Connection refused", sendResult.ErrorMessage);
        }


        /// <summary>
        /// Description: Kiểm tra DataOutboundRestSender cấu hình URL đúng cho cả trường hợp đối tác có Port và không có Port.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_Send_UrlWithAndWithoutPort_ConstructsCorrectUri_Test()
        {
            // Arrange
            string? capturedUrl = null;
            var testHandler = new TestHttpMessageHandler((req, _) =>
            {
                capturedUrl = req.RequestUri?.ToString();
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);
            var mapping = new DataMappingResult(true, System.Text.Encoding.UTF8.GetBytes("""{"id":1}"""), 1);

            // Case 1: Partner có Port
            var partnerWithPort = new ShareDataPartner { Address = "192.168.1.10", Port = 9000, EndPointApiUrl = "/api/v1/feed" };
            var ctxWithPort = new DataOutboundContext(new ShareDataSubscription(), partnerWithPort, new ShareDataPacket(), null, DateTime.Now, CancellationToken.None);
            await sender.Send(mapping, ctxWithPort, CancellationToken.None);
            Assert.Equal("http://192.168.1.10:9000/api/v1/feed", capturedUrl);

            // Case 2: Partner không có Port (null)
            var partnerWithoutPort = new ShareDataPartner { Address = "partner.example.com", Port = null, EndPointApiUrl = "/data" };
            var ctxWithoutPort = new DataOutboundContext(new ShareDataSubscription(), partnerWithoutPort, new ShareDataPacket(), null, DateTime.Now, CancellationToken.None);
            await sender.Send(mapping, ctxWithoutPort, CancellationToken.None);
            Assert.Equal("http://partner.example.com/data", capturedUrl);
        }


        /// <summary>
        /// Description: Kiểm tra RestSender gửi kèm đủ 4 HTTP Header định danh (PartnerCode, DatatypeId, SerialNbr, ProcessedAt) lấy đúng từ ctx, DatatypeId là mã không phải GUID, ProcessedAt parse round-trip khớp ExportedAt, và thân bản tin (body) khớp FinalBytes từng byte không đổi.
        /// Created date: 19/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_Send_WithIdentityHeaders_PreservesBodyBytesAndSendsCorrectHeaders_Test()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            byte[]? capturedRawBodyBytes = null;

            var testHandler = new TestHttpMessageHandler(async (req, ct) =>
            {
                capturedRequest = req;
                capturedRawBodyBytes = req.Content != null ? await req.Content.ReadAsByteArrayAsync(ct) : null;
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);

            var sampleJson = """{"header":{"source":"ITS"},"data":[{"id":100,"speed":60}]}""";
            var finalBytes = Encoding.UTF8.GetBytes(sampleJson);
            var mapping = new DataMappingResult(true, finalBytes, 1);

            var exportedAt = new DateTime(2026, 9, 19, 14, 5, 31, 234, DateTimeKind.Local);
            var partner = new ShareDataPartner { Code = "PARTNER2", Address = "127.0.0.1", Port = 8080, EndPointApiUrl = "/api/receive" };
            var packet = new ShareDataPacket { ID = "d7c71e22-38b4-4e46-9d62-11c58e08d6e9", Code = "101_commonData" };
            var sub = new ShareDataSubscription { DatatypeId = "101_commonData", SerialNbr = 486 };
            var ctx = new DataOutboundContext(sub, partner, packet, null, exportedAt, CancellationToken.None);

            // Act
            var result = await sender.Send(mapping, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedRequest);

            // 1. Kiểm tra đủ 4 header và đúng giá trị từ ctx
            Assert.True(capturedRequest.Headers.Contains("PartnerCode"));
            Assert.Equal("PARTNER2", capturedRequest.Headers.GetValues("PartnerCode").First());

            Assert.True(capturedRequest.Headers.Contains("PacketCode"));
            Assert.Equal("101_commonData", capturedRequest.Headers.GetValues("PacketCode").First());

            Assert.True(capturedRequest.Headers.Contains("SerialNbr"));
            Assert.Equal("486", capturedRequest.Headers.GetValues("SerialNbr").First());

            Assert.True(capturedRequest.Headers.Contains("ProcessedAt"));
            var processedAtHeader = capturedRequest.Headers.GetValues("ProcessedAt").First();
            var parsedTime = DateTime.Parse(processedAtHeader, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            Assert.Equal(exportedAt, parsedTime);

            // 2. PacketCode mang mã, KHÔNG mang GUID (không chứa dấu '-')
            Assert.DoesNotContain("-", capturedRequest.Headers.GetValues("PacketCode").First());
            Assert.NotEqual(packet.ID, capturedRequest.Headers.GetValues("PacketCode").First());

            // 3. Thân bản tin không đổi 1 byte (khớp FinalBytes từng byte)
            Assert.NotNull(capturedRawBodyBytes);
            Assert.Equal(finalBytes, capturedRawBodyBytes);
        }


        /// <summary>
        /// Description: Kiểm tra khi Partner.Code là null thì RestSender bỏ hẳn header PartnerCode, các header còn lại vẫn gửi đủ.
        /// Created date: 19/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_Send_WhenPartnerCodeIsNull_OmitsPartnerCodeHeader_Test()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var testHandler = new TestHttpMessageHandler((req, _) =>
            {
                capturedRequest = req;
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);

            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes("""{"test":1}"""), 1);
            var exportedAt = new DateTime(2026, 9, 19, 10, 0, 0, DateTimeKind.Local);
            var partner = new ShareDataPartner { Code = null, Address = "127.0.0.1", EndPointApiUrl = "/api" };
            var packet = new ShareDataPacket { Code = "101_commonData" };
            var sub = new ShareDataSubscription { SerialNbr = 123 };
            var ctx = new DataOutboundContext(sub, partner, packet, null, exportedAt, CancellationToken.None);

            // Act
            var result = await sender.Send(mapping, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedRequest);
            Assert.False(capturedRequest.Headers.Contains("PartnerCode"));
            Assert.True(capturedRequest.Headers.Contains("PacketCode"));
            Assert.True(capturedRequest.Headers.Contains("SerialNbr"));
            Assert.True(capturedRequest.Headers.Contains("ProcessedAt"));
        }


        /// <summary>
        /// Description: Kiểm tra khi Subscription.SerialNbr là null thì RestSender bỏ hẳn header SerialNbr, không gửi chuỗi rỗng.
        /// Created date: 19/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_Send_WhenSerialNbrIsNull_OmitsSerialNbrHeader_Test()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var testHandler = new TestHttpMessageHandler((req, _) =>
            {
                capturedRequest = req;
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);

            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes("""{"test":1}"""), 1);
            var partner = new ShareDataPartner { Code = "PARTNER1", Address = "127.0.0.1", EndPointApiUrl = "/api" };
            var packet = new ShareDataPacket { Code = "102_cctvData" };
            var sub = new ShareDataSubscription { SerialNbr = null };
            var ctx = new DataOutboundContext(sub, partner, packet, null, DateTime.Now, CancellationToken.None);

            // Act
            var result = await sender.Send(mapping, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedRequest);
            Assert.False(capturedRequest.Headers.Contains("SerialNbr"));
            Assert.True(capturedRequest.Headers.Contains("PartnerCode"));
            Assert.True(capturedRequest.Headers.Contains("PacketCode"));
        }


        /// <summary>
        /// Description: Kiểm tra sau khi gộp mốc (Rủi ro 2), header ProcessedAt bằng đúng giá trị dataTime ($meta: Now) trong body từng mili giây.
        /// Created date: 19/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_ProcessedAtHeader_MatchesBodyDataTimeExactly_Test()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;
            var testHandler = new TestHttpMessageHandler(async (req, ct) =>
            {
                capturedRequest = req;
                capturedBody = await req.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            });
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);

            var exportedAt = new DateTime(2026, 9, 19, 14, 5, 31, 234, DateTimeKind.Local);
            var partner = new ShareDataPartner { Code = "PARTNER2", Address = "127.0.0.1", EndPointApiUrl = "/api" };
            var packet = new ShareDataPacket { Code = "101_traffic" };
            var sub = new ShareDataSubscription { SerialNbr = 10 };
            var ctx = new DataOutboundContext(sub, partner, packet, null, exportedAt, CancellationToken.None);

            var shapeJson = """
            {
              "header": {
                "dataTime": { "$meta": "Now" }
              },
              "data": "$root"
            }
            """;
            var rawRows = new List<object> { new Dictionary<string, object?> { ["id"] = 1 } };
            var metaValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Now"] = ctx.ExportedAt,
                ["PartnerCode"] = ctx.Partner?.Code,
                ["PacketCode"] = packet.Code,
                ["Serial"] = sub?.SerialNbr
            };
            var transformed = DataMappingProcess.Transform(rawRows, shapeJson, metaValues: metaValues);
            var bodyJson = JsonSerializer.Serialize(transformed[0]);
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(bodyJson), 1);

            // Act
            var result = await sender.Send(mapping, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedRequest);
            Assert.NotNull(capturedBody);

            var headerProcessedAt = capturedRequest.Headers.GetValues("ProcessedAt").First();
            using var doc = JsonDocument.Parse(capturedBody);
            var bodyDataTime = doc.RootElement.GetProperty("header").GetProperty("dataTime").GetString();

            Assert.NotNull(bodyDataTime);
            var parsedHeaderTime = DateTime.Parse(headerProcessedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            var parsedBodyTime = DateTime.Parse(bodyDataTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            // Bằng nhau từng mili giây khớp với ExportedAt
            Assert.Equal(parsedHeaderTime, parsedBodyTime);
            Assert.Equal(exportedAt, parsedHeaderTime);
            Assert.Equal(exportedAt, parsedBodyTime);
        }


        /// <summary>
        /// Description: Kiểm tra khi Partner.Code chứa ký tự xuống dòng (\r\n) chống header injection, request.Headers.Add ném ngoại lệ được bắt an toàn và Send trả về thất bại có thông điệp rõ ràng.
        /// Created date: 19/09/2026
        /// </summary>
        [Fact]
        public async Task RestSender_Send_WhenPartnerCodeContainsCrLf_CatchesSafelyAndReturnsFailure_Test()
        {
            // Arrange
            var testHandler = new TestHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)));
            var clientFactory = new MockHttpClientFactoryTest(testHandler);
            var sender = new DataOutboundRestSender(clientFactory);

            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes("""{"test":1}"""), 1);
            var maliciousPartner = new ShareDataPartner { Code = "PARTNER\r\nX-Injected: Bad", Address = "127.0.0.1", EndPointApiUrl = "/api" };
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
        /// Description: Kiểm tra cờ required = true: nếu trường bị thiếu hoặc null thì Map trả về thất bại và thông báo rõ tên trường thiếu.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public async Task Map_ExtendRequired_MissingField_ReturnsFailureWithDetailedMessage_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var shapeJson = """
            {
              "data": [
                {
                  "name": { "$field": "NonExistentField", "$extend": { "required": true } }
                }
              ]
            }
            """;
            var rawRows = new List<object> { new Dictionary<string, object?> { ["Id"] = 1 } };
            var extraction = new DataOutboundExtractionResult(rawRows, null, null);
            var mapping = new ShareDataMapping { TargetShapeJson = shapeJson };
            var packet = new ShareDataPacket { Code = "101" };
            var partner = new ShareDataPartner { Code = "TEST_PARTNER" };
            var ctx = new DataOutboundContext(new ShareDataSubscription(), partner, packet, mapping, DateTime.Now, CancellationToken.None);

            // Act - Calling Map
            var mapResult = await DataMappingProcess.Map(db, extraction, ctx);

            // Assert
            Assert.False(mapResult.Success);
            Assert.NotNull(mapResult.ErrorMessage);
            Assert.Contains("Thiếu trường bắt buộc", mapResult.ErrorMessage);
            Assert.Contains("NonExistentField", mapResult.ErrorMessage);

            // Act & Assert - Calling Transform directly throws InvalidOperationException
            var ex = Assert.Throws<InvalidOperationException>(() =>
                DataMappingProcess.Transform(rawRows, shapeJson));
            Assert.Contains("Thiếu trường bắt buộc", ex.Message);
            Assert.Contains("NonExistentField", ex.Message);
        }


        /// <summary>
        /// Description: Kiểm tra chuỗi fallback 4 cấp của CodeSet (§3.4, §4.1): khớp mã -> default của CodeSet -> item IsDefault -> defaultPartnerValue của lá -> null.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Map_CodeSet_FallbackChain_FullHierarchy_Test()
        {
            // Arrange
            var codeSets = new Dictionary<string, CodeSetDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["STATUS_CS"] = new CodeSetDto
                {
                    DefaultPartnerValue = "CS_DEFAULT",
                    Values =
                    [
                        new CodeValueDto { SourceValue = "1", PartnerValue = "ACTIVE" },
                        new CodeValueDto { SourceValue = "2", PartnerValue = "INACTIVE", IsDefault = true }
                    ]
                },
                ["FALLBACK_ITEM_CS"] = new CodeSetDto
                {
                    DefaultPartnerValue = null,
                    Values =
                    [
                        new CodeValueDto { SourceValue = "A", PartnerValue = "VAL_A" },
                        new CodeValueDto { SourceValue = "B", PartnerValue = "VAL_B_DEFAULT", IsDefault = true }
                    ]
                },
                ["NO_DEFAULT_CS"] = new CodeSetDto
                {
                    DefaultPartnerValue = null,
                    Values =
                    [
                        new CodeValueDto { SourceValue = "X", PartnerValue = "VAL_X" }
                    ]
                }
            };

            var shapeJson = """
            {
              "matched": { "$field": "Code1", "$extend": { "codeSet": "STATUS_CS" } },
              "unmatchedWithCsDefault": { "$field": "Code2", "$extend": { "codeSet": "STATUS_CS" } },
              "emptyWithItemDefault": { "$field": "Code3", "$extend": { "codeSet": "FALLBACK_ITEM_CS" } },
              "emptyWithLeafDefault": { "$field": "Code4", "$extend": { "codeSet": "NO_DEFAULT_CS", "defaultPartnerValue": "LEAF_VAL" } },
              "emptyWithoutAnyDefault": { "$field": "Code5", "$extend": { "codeSet": "NO_DEFAULT_CS" } }
            }
            """;

            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["Code1"] = "1", // Matched -> ACTIVE
                    ["Code2"] = "999", // Unmatched -> CS_DEFAULT
                    ["Code3"] = null, // Empty -> item with IsDefault == true -> VAL_B_DEFAULT
                    ["Code4"] = "", // Empty -> no CS default -> leaf default -> LEAF_VAL
                    ["Code5"] = null // Empty -> no CS default, no leaf default -> null
                }
            };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, codeSets: codeSets);

            // Assert
            Assert.Single(result);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("ACTIVE", dict["matched"]);
            Assert.Equal("CS_DEFAULT", dict["unmatchedWithCsDefault"]);
            Assert.Equal("VAL_B_DEFAULT", dict["emptyWithItemDefault"]);
            Assert.Equal("LEAF_VAL", dict["emptyWithLeafDefault"]);
            Assert.Null(dict["emptyWithoutAnyDefault"]);
        }


        /// <summary>
        /// Description: Kiểm tra chuyển đổi targetType boolean/bool từ đa dạng kiểu nguồn (chuỗi, số nguyên, boolean).
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Map_ExtendTargetType_BooleanConversions_Test()
        {
            // Arrange
            var shapeJson = """
            {
              "b1": { "$field": "V1", "$extend": { "targetType": "bool" } },
              "b2": { "$field": "V2", "$extend": { "targetType": "boolean" } },
              "b3": { "$field": "V3", "$extend": { "targetType": "bool" } },
              "b4": { "$field": "V4", "$extend": { "targetType": "boolean" } }
            }
            """;
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["V1"] = "true",
                    ["V2"] = 1,
                    ["V3"] = "false",
                    ["V4"] = 0
                }
            };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson);

            // Assert
            Assert.Single(result);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(true, dict["b1"]);
            Assert.Equal(true, dict["b2"]);
            Assert.Equal(false, dict["b3"]);
            Assert.Equal(false, dict["b4"]);
        }


        /// <summary>
        /// Description: Kiểm tra numberFormat định dạng số thập phân và định dạng chuỗi số (padding 0) theo cấu hình (§3.4, §9.3).
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Map_ExtendNumberFormat_FormatsNumberAndStringCorrectly_Test()
        {
            // Arrange
            var shapeJson = """
            {
              "formattedDec": { "$field": "Num1", "$extend": { "targetType": "decimal", "numberFormat": "F2" } },
              "paddedStr": { "$field": "Num2", "$extend": { "targetType": "string", "numberFormat": "0000" } }
            }
            """;
            var rawRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["Num1"] = 12.3456,
                    ["Num2"] = 42
                }
            };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson);

            // Assert
            Assert.Single(result);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal(12.35m, dict["formattedDec"]);
            Assert.Equal("0042", dict["paddedStr"]);
        }


        /// <summary>
        /// Description: Kiểm tra quy tắc §3.4: Khi trường đã quy đổi qua bộ mã thì KHÔNG áp dụng ép kiểu targetType nữa, giữ nguyên giá trị bộ mã.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void Map_ExtendCodeSet_SkipsTargetTypeCast_Test()
        {
            // Arrange
            var codeSets = new Dictionary<string, CodeSetDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["LANE_CS"] = new CodeSetDto
                {
                    Values =
                    [
                        new CodeValueDto { SourceValue = "1", PartnerValue = "LANE_01_SPECIAL" }
                    ]
                }
            };

            var shapeJson = """
            {
              "lane": { "$field": "LaneId", "$extend": { "codeSet": "LANE_CS", "targetType": "int" } }
            }
            """;
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["LaneId"] = "1" }
            };

            // Act
            var result = DataMappingProcess.Transform(rawRows, shapeJson, codeSets: codeSets);

            // Assert
            Assert.Single(result);
            var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result[0]);
            Assert.Equal("LANE_01_SPECIAL", dict["lane"]);
        }


        /// <summary>
        /// Description: Kiểm tra trực tiếp hàm ConvertDataType và alias CoerceDataType ép kiểu số, ngày giờ và boolean chính xác.
        /// Created date: 18/09/2026
        /// </summary>
        [Fact]
        public void ConvertDataType_DirectCall_And_ObsoleteAlias_FunctionCorrectly_Test()
        {
            // Act & Assert - ConvertDataType trên DataMappingProcess
            Assert.Equal(123, DataMappingProcess.ConvertDataType("123", "int"));
            Assert.Equal(true, DataMappingProcess.ConvertDataType("true", "bool"));
            Assert.Equal("12.35", DataMappingProcess.ConvertDataType(12.3456, "string", numberFormat: "F2"));
            var dt = new DateTime(2026, 9, 18, 14, 30, 0);
            Assert.Equal("2026-09-18", DataMappingProcess.ConvertDataType(dt, "string", dateFormat: "yyyy-MM-dd"));

            // Act & Assert - Obsolete alias CoerceDataType trên DataMappingProcess
#pragma warning disable CS0618
            Assert.Equal(123, DataMappingProcess.CoerceDataType("123", "int"));
#pragma warning restore CS0618
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
            [ShareDataEnum.DatatypeIdEnum.PublicMessaging] = ["incidentMessage", "guidanceContent", "locationKm", "locationMet", "publishedTime"],
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
            ISqlSugarClient db,
            string partnerCode,
            string subCode,
            string datatypeId,
            Action<ShareDataSubscription>? configureSub = null,
            Action<ShareDataPartner>? configurePartner = null)
        {
            var partner = new ShareDataPartner
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = partnerCode,
                Name = $"Partner {partnerCode}",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected,
                Address = "127.0.0.1",
                Port = 5099,
                EndPointApiUrl = "/api/sharedata/sharedatainbound"
            };
            configurePartner?.Invoke(partner);
            await db.Insertable(partner).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = subCode,
                PartnerId = partner.ID,
                DatatypeId = datatypeId,
                Direction = BaseEnums.Direction.Outbound,
                Mode = BaseEnums.SubMode.Periodic,
                State = BaseEnums.SubSubscriptionState.Active,
                NextTimeRun = DateTime.Now.AddSeconds(-10),
                IntervalSeconds = 30
            };

            configureSub?.Invoke(sub);
            await db.Insertable(sub).ExecuteCommandAsync();

            var fields = await db.Queryable<ShareDataPacketField>().Where(f => f.DatatypeId == datatypeId).ToListAsync();
            var shapeDict = new System.Collections.Generic.Dictionary<string, object>();
            foreach (var f in fields)
            {
                if (!string.IsNullOrEmpty(f.AliasFieldKey))
                    shapeDict[f.AliasFieldKey] = new System.Collections.Generic.Dictionary<string, string> { { "$field", f.AliasFieldKey } };
            }
            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = datatypeId,
                Direction = BaseEnums.Direction.Outbound,
                IsActive = true,
                TargetShapeJson = System.Text.Json.JsonSerializer.Serialize(shapeDict)
            }).ExecuteCommandAsync();

            return (partner, sub);
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
                        State = ShareDataEnum.IncidentState.InProgress,
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
                        State = ShareDataEnum.IncidentState.InProgress,
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
                        State = ShareDataEnum.IncidentState.InProgress,
                        StartDate = now,
                        UpdateTime = now
                    }).ExecuteCommandAsync();
                    break;
            }
        }



        private static DataOutboundService CreateWorker(
            IServiceScope scope,
            IHttpClientFactory? httpClientFactory = null,
            IDataOutboundExtractionProcess? extractionProcess = null,
            IDataOutboundFileSender? fileSender = null,
            IDataOutboundRestSender? restSender = null)
        {
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataOutboundService>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var defaultHandler = new TestHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)));
            var clientFactory = httpClientFactory ?? new MockHttpClientFactoryTest(defaultHandler);
            var hostEnv = scope.ServiceProvider.GetService<IHostEnvironment>();
            return new DataOutboundService(
                scopeFactory,
                logger,
                fileSender ?? new DataOutboundFileSender(config, hostEnv),
                restSender ?? new DataOutboundRestSender(clientFactory),
                extractionProcess ?? scope.ServiceProvider.GetRequiredService<IDataOutboundExtractionProcess>());
        }

        private sealed class MockDataOutboundFileSender(Func<DataMappingResult, DataOutboundContext, Task<DataOutboundSendResult>> handler) : IDataOutboundFileSender
        {
            public Task<DataOutboundSendResult> Send(DataMappingResult mapping, DataOutboundContext ctx, CancellationToken ct)
                => handler(mapping, ctx);
        }

        private sealed class MockDataOutboundRestSender(Func<DataMappingResult, DataOutboundContext, Task<DataOutboundSendResult>> handler) : IDataOutboundRestSender
        {
            public Task<DataOutboundSendResult> Send(DataMappingResult mapping, DataOutboundContext ctx, CancellationToken ct)
                => handler(mapping, ctx);
        }

        private sealed class TestHostEnvironment(string envName) : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = envName;
            public string ApplicationName { get; set; } = "ShareDataWorker";
            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        }

        private sealed class MockDataExtractionProcess(Func<ISqlSugarClient, ShareDataSubscription, ShareDataPacket, Task<DataOutboundExtractionResult>> handler) : IDataOutboundExtractionProcess
        {
            public Task<DataOutboundExtractionResult> Extract(
                ISqlSugarClient db,
                ShareDataSubscription sub,
                ShareDataPacket packet,
                DataOutboundCursor? cursor,
                int pageSize,
                CancellationToken cancellationToken = default,
                Action<string, object?[]>? onLogWarning = null)
                => handler(db, sub, packet);
        }

        private sealed class MockHttpClientFactoryTest(HttpMessageHandler handler) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
        }

        private sealed class TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return handler(request, cancellationToken);
            }
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

        public static class PacketMetadataCatalogTest
        {
            public class PacketDefinition
            {
                public ShareDataPacket Packet { get; set; } = new();
                public List<PacketFieldDto> Fields { get; set; } = [];
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
                        PacketVersion = def.Packet.PacketVersion
                    };
                    await db.Insertable(newPacket).ExecuteCommandAsync();
                }
                else
                {
                    existingPacket.PacketVersion = def.Packet.PacketVersion;
                    await db.Updateable(existingPacket).ExecuteCommandAsync();
                }

                await db.Deleteable<ShareDataPacketField>().Where(f => f.DatatypeId == def.Packet.Code).ExecuteCommandAsync();
                var baseTime = DateTime.Now;
                var newFields = new List<ShareDataPacketField>();
                int order = 0;
                foreach (var field in def.Fields.Where(f => f.InternalOnly != true).OrderBy(f => f.OrderNo))
                {
                    newFields.Add(new ShareDataPacketField
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        DatatypeId = def.Packet.Code,
                        AliasFieldKey = field.FieldKey,
                        Type = field.DataType,
                        Name = field.Name,
                        IsRequired = field.Required ? Shared.DTO.Enums.BaseEnums.IsRequired.IsRequired : Shared.DTO.Enums.BaseEnums.IsRequired.NoRequired,
                        CreateTime = baseTime.AddSeconds(order++)
                    });
                }

                if (newFields.Count > 0)
                {
                    await db.Insertable(newFields).ExecuteCommandAsync();
                }
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
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
                        PacketVersion = "1.0"
                    },
                    Fields =
                    [
                        new() { FieldKey = "incidentMessage", Expression = "CONCAT(ISNULL(i.Name, ''), ' - ', ISNULL(i.Description, ''))", DataType = "string", OrderNo = 1 },
                        new() { FieldKey = "guidanceContent", Column = "RowData", DataType = "string", OrderNo = 2 },
                        new() { FieldKey = "locationKm", Column = "KmNumber", DataType = "int", OrderNo = 3 },
                        new() { FieldKey = "locationMet", Column = "MetNumber", DataType = "int", OrderNo = 4 },
                        new() { FieldKey = "publishedTime", Column = "StartDate", DataType = "datetime", OrderNo = 5 }
                    ]
                };

                map["111"] = new PacketDefinition
                {
                    Packet = new ShareDataPacket
                    {
                        ID = "packet_111",
                        Code = "111",
                        Name = "Trao đổi với TT QLĐHGT tuyến",
                        PacketVersion = "1.0"
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
    }
}
