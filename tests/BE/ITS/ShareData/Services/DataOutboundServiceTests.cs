using System.Net;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;
using ShareDataWorker.Core.Models.DataOutbound;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Dto.ActivityLog;
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

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Description: Lớp kiểm thử Integration Test cho DataOutboundService thuộc module ShareDataWorker trên CSDL Test Local.
    /// Created date: 31/07/2026
    /// </summary>
    [Collection("api")]
    public partial class DataOutboundServiceTests
    {
        private readonly Host _host;

        public DataOutboundServiceTests(Host host)
        {
            /// xUnit tạo instance mới trước mỗi [Fact] → constructor này chạy mỗi test
            host.PartnerServer.ResetDefaults();
            _host = host;

            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Expired)
                .SetColumns(s => s.NextTimeRun == DateTime.MaxValue)
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .ExecuteCommand();

            db.Deleteable<TmsTrafficData>().ExecuteCommand();
            db.Deleteable<TmsIncident>().ExecuteCommand();
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

            var existingFields = await db.Queryable<ShareDataPacketField>().Where(f => f.DatatypeId == packetCodeWithSuffix).ToListAsync();
            var requiredKeys = new[] { "zoneStatusId", "zoneId", "averageSpeed", "trafficCondition", "dataTime" };
            var missingKeys = requiredKeys.Where(k => !existingFields.Any(f => f.AliasFieldKey == k)).ToList();
            if (missingKeys.Count > 0)
            {
                var newFields = missingKeys.Select(k => new ShareDataPacketField
                {
                    ID = Guid.NewGuid().ToString("N"),
                    DatatypeId = packetCodeWithSuffix,
                    AliasFieldKey = k,
                    Type = "string",
                    IsRequired = (k == "zoneStatusId" || k == "zoneId") ? BaseEnums.IsRequired.IsRequired : BaseEnums.IsRequired.NoRequired
                }).ToList();
                await db.Insertable(newFields).ExecuteCommandAsync();
            }

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



            var existingFields = await db.Queryable<ShareDataPacketField>().Where(f => f.DatatypeId == "101_commonData").ToListAsync();
            var requiredKeys = new[] { "zoneStatusId", "zoneId", "averageSpeed", "trafficCondition", "dataTime" };
            var missingKeys = requiredKeys.Where(k => !existingFields.Any(f => f.AliasFieldKey == k)).ToList();
            if (missingKeys.Count > 0)
            {
                var newFields = missingKeys.Select(k => new ShareDataPacketField
                {
                    ID = Guid.NewGuid().ToString("N"),
                    DatatypeId = "101_commonData",
                    AliasFieldKey = k,
                    Type = "string",
                    IsRequired = (k == "zoneStatusId" || k == "zoneId") ? BaseEnums.IsRequired.IsRequired : BaseEnums.IsRequired.NoRequired
                }).ToList();
                await db.Insertable(newFields).ExecuteCommandAsync();
            }

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
            var sql103 = @"SELECT TOP (@pageSize)
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

            var fields103 = await db.Queryable<ShareDataPacketField>().Where(f => f.DatatypeId == "103").ToListAsync();
            var shapeDict103 = new Dictionary<string, object>();
            foreach (var f in fields103)
            {
                if (!string.IsNullOrEmpty(f.AliasFieldKey))
                    shapeDict103[f.AliasFieldKey] = new Dictionary<string, string> { { "$field", f.AliasFieldKey } };
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
            await ShareDataSqlSeed.SetCommonAsync(db, packet105.ID, LegacyPacketSql.Packet109);

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
                var fields = await db.Queryable<ShareDataPacketField>().Where(f => f.DatatypeId == s.DatatypeId).ToListAsync();
                var shapeDict = new System.Collections.Generic.Dictionary<string, object>();
                foreach (var f in fields)
                {
                    if (!string.IsNullOrEmpty(f.AliasFieldKey))
                        shapeDict[f.AliasFieldKey] = new System.Collections.Generic.Dictionary<string, string> { { "$field", f.AliasFieldKey } };
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
                        IsRequired = field.Required ? BaseEnums.IsRequired.IsRequired : BaseEnums.IsRequired.NoRequired,
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
