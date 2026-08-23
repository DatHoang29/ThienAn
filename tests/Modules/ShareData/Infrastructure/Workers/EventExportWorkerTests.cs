using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Modules.TMS.Core.Entities;
using ShareDataWorker.Core.Dto;
using ShareDataWorker.Core.Entities;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Interfaces;
using ShareDataWorker.Infrastructure.Services.DataExport;
using ShareDataWorker.Infrastructure.Workers;
using SqlSugar;
using Xunit;

namespace Tests.Modules.ShareData.Infrastructure.Workers
{
    [Collection("api")]
    public class EventExportWorkerTests(Host host)
    {
        private readonly Host _host = host;

        [Fact]
        public async Task FlowType_WhenSetOrNull_LogsCorrectFlowTypeInActivityLog_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var exportService = scope.ServiceProvider.GetRequiredService<IDataExportService>();

            var partner = new ShareDataPartner
            {
                ID = "P_FLOW_" + Guid.NewGuid().ToString("N")[..8],
                Code = "P_FLOW_" + Guid.NewGuid().ToString("N")[..8],
                Name = "Partner Flow Test",
                Status = BaseEnums.StatusEnum.Enable
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var packet = new ShareDataPacket
            {
                ID = "PK_FLOW_" + Guid.NewGuid().ToString("N")[..8],
                Code = "101",
                Name = "Packet Flow Test",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                PacketVersion = "1.0",
                IsActive = true
            };
            await db.Insertable(packet).ExecuteCommandAsync();

            var subInternal = new ShareDataSubscription
            {
                ID = "SUB_INTERNAL_" + Guid.NewGuid().ToString("N")[..8],
                PartnerId = partner.ID,
                DatatypeId = packet.Code,
                FlowType = ShareDataEnum.FlowType.Internal,
                Direction = ShareDataEnum.SubDirection.Outbound,
                State = BaseEnums.SubSubscriptionState.Active,
                Format = "DATA"
            };

            var logId1 = await DataExportService.LogExportResult(db, subInternal, partner, 10, 500, "Out/test1.json",
                ShareDataEnum.ExportStatus.Success, mappingId: "M1", packetVersion: "1.0");

            var log1 = await db.Queryable<ShareDataActivityLog>().InSingleAsync(logId1);
            Assert.NotNull(log1);
            Assert.Equal(ShareDataEnum.FlowType.Internal, log1.Remark);

            var subNullFlow = new ShareDataSubscription
            {
                ID = "SUB_NULL_FLOW_" + Guid.NewGuid().ToString("N")[..8],
                PartnerId = partner.ID,
                DatatypeId = packet.Code,
                FlowType = null,
                Direction = ShareDataEnum.SubDirection.Outbound,
                State = BaseEnums.SubSubscriptionState.Active,
                Format = "DATA"
            };

            var logId2 = await DataExportService.LogExportResult(db, subNullFlow, partner, 10, 500, "Out/test2.json",
                ShareDataEnum.ExportStatus.Success, mappingId: "M2", packetVersion: "1.0");

            var log2 = await db.Queryable<ShareDataActivityLog>().InSingleAsync(logId2);
            Assert.NotNull(log2);
            Assert.Equal(ShareDataEnum.FlowType.External, log2.Remark);
        }

        [Fact]
        public async Task EventExportWorker_HandleEventTrigger_ExecutesExport_AndRespectsDebounce_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var eventSubscriber = scope.ServiceProvider.GetRequiredService<IShareDataEventSubscriber>();
            var exportService = scope.ServiceProvider.GetRequiredService<IDataExportService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventExportWorker>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var partner = new ShareDataPartner
            {
                ID = "P_EVT_" + Guid.NewGuid().ToString("N")[..8],
                Code = "P_EVT_" + Guid.NewGuid().ToString("N")[..8],
                Name = "Partner Event Test",
                Status = BaseEnums.StatusEnum.Enable
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var packetCode = "995";
            var packet = new ShareDataPacket
            {
                ID = "PK_EVT_" + Guid.NewGuid().ToString("N")[..8],
                Code = packetCode,
                Name = "Packet Event Test",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                PacketVersion = "1.0",
                IsActive = true
            };
            await db.Insertable(packet).ExecuteCommandAsync();

            var table = new ShareDataTable
            {
                ID = "TB_EVT_" + Guid.NewGuid().ToString("N")[..8],
                PacketCode = packetCode,
                TableName = "TmsTrafficData",
                OrderNo = 1,
                IsRoot = true,
                IsActive = true,
                FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                {
                    new() { FieldKey = "zoneId", Column = "EquipmentId", DataType = "string", Required = true }
                })
            };
            await db.Insertable(table).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = "TD_EVT_" + Guid.NewGuid().ToString("N")[..8],
                EquipmentId = "ZONE_EVT_1",
                DetectTime = DateTime.Now
            }).ExecuteCommandAsync();

            var eventSourceCode = "EVT_SRC_" + Guid.NewGuid().ToString("N")[..8];
            var eventSource = new ShareDataEventSource
            {
                ID = eventSourceCode,
                Code = eventSourceCode,
                Name = "Nguồn sự kiện phát hiện xe"
            };
            await db.Insertable(eventSource).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                ID = "SUB_EVT_" + Guid.NewGuid().ToString("N")[..8],
                PartnerId = partner.ID,
                DatatypeId = packetCode,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Event,
                State = BaseEnums.SubSubscriptionState.Active,
                EventSourceId = eventSource.ID,
                DebounceSec = 5, // Debounce 5 giây
                Format = "DATA"
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var worker = new EventExportWorker(scopeFactory, eventSubscriber, exportService, logger);
            await worker.InitializeSubscriptions(CancellationToken.None);

            // 1. Phát sinh event lần 1 -> Phải xuất bản thành công
            await eventSubscriber.Publish(eventSourceCode, "{\"event\":\"traffic_alert\"}");

            var logsAfterFirst = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();
            Assert.Single(logsAfterFirst);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logsAfterFirst[0].Status);

            // 2. Phát sinh event lần 2 ngay lập tức (trong 5 giây debounce) -> Phải bị bỏ qua (debounce)
            await eventSubscriber.Publish(eventSourceCode, "{\"event\":\"traffic_alert_2\"}");

            var logsAfterSecond = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();
            // Số log vẫn là 1 (không sinh thêm log mới do bị debounce chặn)
            Assert.Single(logsAfterSecond);
        }

        [Fact]
        public async Task EventExportWorker_WhenSameSubscriptionTriggeredConcurrently_ExportsOnce_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var eventSubscriber = scope.ServiceProvider.GetRequiredService<IShareDataEventSubscriber>();
            var exportService = scope.ServiceProvider.GetRequiredService<IDataExportService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventExportWorker>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var partner = new ShareDataPartner
            {
                ID = $"P_C3_{uniqueId}",
                Code = $"P_C3_{uniqueId}",
                Name = $"Partner C3 {uniqueId}",
                Status = BaseEnums.StatusEnum.Enable
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var packetCode = $"993_{uniqueId}";
            await db.Insertable(new ShareDataPacket
            {
                ID = $"PK_C3_{uniqueId}",
                Code = packetCode,
                Name = "Packet C3",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                PacketVersion = "1.0",
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = $"TB_C3_{uniqueId}",
                PacketCode = packetCode,
                TableName = "TmsTrafficData",
                OrderNo = 1,
                IsRoot = true,
                IsActive = true,
                FieldsJson = "[{\"fieldKey\":\"zoneId\",\"column\":\"EquipmentId\",\"required\":true}]"
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = $"TD_C3_{uniqueId}",
                EquipmentId = "ZONE_C3",
                DetectTime = DateTime.Now
            }).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                ID = $"SUB_C3_{uniqueId}",
                PartnerId = partner.ID,
                DatatypeId = packetCode,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Event,
                State = BaseEnums.SubSubscriptionState.Active,
                DebounceSec = 1,
                Format = "DATA"
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var worker = new EventExportWorker(scopeFactory, eventSubscriber, exportService, logger);
            var barrier = new Barrier(10);
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                try
                {
                    await worker.HandleEventTrigger(sub.ID, "{}", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));

            await Task.WhenAll(tasks);

            Assert.Empty(exceptions);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == sub.ID)
                .ToListAsync();
            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);

            var finalSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(BaseEnums.SubSubscriptionState.Active, finalSub.State);
        }

        [Fact]
        public async Task EventExportWorker_WhenDifferentSubscriptionsTriggeredConcurrently_AllExport_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var eventSubscriber = scope.ServiceProvider.GetRequiredService<IShareDataEventSubscriber>();
            var exportService = scope.ServiceProvider.GetRequiredService<IDataExportService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventExportWorker>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var packetCode = $"994_{uniqueId}";

            await db.Insertable(new ShareDataPacket
            {
                ID = $"PK_C4_{uniqueId}",
                Code = packetCode,
                Name = "Packet C4",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                PacketVersion = "1.0",
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = $"TB_C4_{uniqueId}",
                PacketCode = packetCode,
                TableName = "TmsTrafficData",
                OrderNo = 1,
                IsRoot = true,
                IsActive = true,
                FieldsJson = "[{\"fieldKey\":\"zoneId\",\"column\":\"EquipmentId\",\"required\":true}]"
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = $"TD_C4_{uniqueId}",
                EquipmentId = "ZONE_C4",
                DetectTime = DateTime.Now
            }).ExecuteCommandAsync();

            var subIds = new List<string>();
            for (var i = 0; i < 5; i++)
            {
                var partner = new ShareDataPartner
                {
                    ID = $"P_C4_{uniqueId}_{i}",
                    Code = $"P_C4_{uniqueId}_{i}",
                    Name = $"Partner C4 {i}",
                    Status = BaseEnums.StatusEnum.Enable
                };
                await db.Insertable(partner).ExecuteCommandAsync();

                var sub = new ShareDataSubscription
                {
                    ID = $"SUB_C4_{uniqueId}_{i}",
                    PartnerId = partner.ID,
                    DatatypeId = packetCode,
                    Direction = ShareDataEnum.SubDirection.Outbound,
                    Mode = ShareDataEnum.SubMode.Event,
                    State = BaseEnums.SubSubscriptionState.Active,
                    DebounceSec = 0,
                    Format = "DATA"
                };
                await db.Insertable(sub).ExecuteCommandAsync();
                subIds.Add(sub.ID);
            }

            var worker = new EventExportWorker(scopeFactory, eventSubscriber, exportService, logger);

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var tasks = subIds.Select(subId => Task.Run(async () =>
            {
                try
                {
                    await worker.HandleEventTrigger(subId, "{}", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));

            await Task.WhenAll(tasks);

            Assert.Empty(exceptions);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => subIds.Contains(l.SubscriptionId!))
                .ToListAsync();
            Assert.Equal(5, logs.Count);
            Assert.All(logs, l => Assert.Equal(ShareDataEnum.ExportStatus.Success, l.Status));

            var filePaths = logs.Select(l => l.FilePath).Where(f => !string.IsNullOrEmpty(f)).Distinct().ToList();
            Assert.Equal(5, filePaths.Count);
        }

        [Fact]
        public async Task EventExportWorker_WhenClaimExpired_ReclaimsSubscription_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var eventSubscriber = scope.ServiceProvider.GetRequiredService<IShareDataEventSubscriber>();
            var exportService = scope.ServiceProvider.GetRequiredService<IDataExportService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventExportWorker>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var partner = new ShareDataPartner
            {
                ID = $"P_C5_{uniqueId}",
                Code = $"P_C5_{uniqueId}",
                Name = $"Partner C5 {uniqueId}",
                Status = BaseEnums.StatusEnum.Enable
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var packetCode = $"995_{uniqueId}";
            await db.Insertable(new ShareDataPacket
            {
                ID = $"PK_C5_{uniqueId}",
                Code = packetCode,
                Name = "Packet C5",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                PacketVersion = "1.0",
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = $"TB_C5_{uniqueId}",
                PacketCode = packetCode,
                TableName = "TmsTrafficData",
                OrderNo = 1,
                IsRoot = true,
                IsActive = true,
                FieldsJson = "[{\"fieldKey\":\"zoneId\",\"column\":\"EquipmentId\",\"required\":true}]"
            }).ExecuteCommandAsync();

            await db.Insertable(new TmsTrafficData
            {
                ID = $"TD_C5_{uniqueId}",
                EquipmentId = "ZONE_C5",
                DetectTime = DateTime.Now
            }).ExecuteCommandAsync();

            // 1. Khóa hết hạn (NextTimeRun trong quá khứ) -> Phải giành lại được
            var subExpired = new ShareDataSubscription
            {
                ID = $"SUB_EXP_{uniqueId}",
                PartnerId = partner.ID,
                DatatypeId = packetCode,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Event,
                State = BaseEnums.SubSubscriptionState.Running,
                NextTimeRun = DateTime.Now.AddMinutes(-10),
                Format = "DATA"
            };
            await db.Insertable(subExpired).ExecuteCommandAsync();

            var worker = new EventExportWorker(scopeFactory, eventSubscriber, exportService, logger);
            await worker.HandleEventTrigger(subExpired.ID, "{}", CancellationToken.None);

            var logsExpired = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subExpired.ID)
                .ToListAsync();
            Assert.Single(logsExpired);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logsExpired[0].Status);

            // 2. Khóa còn hiệu lực (NextTimeRun trong tương lai) -> Phải bị bỏ qua
            var subActiveLock = new ShareDataSubscription
            {
                ID = $"SUB_LOCK_{uniqueId}",
                PartnerId = partner.ID,
                DatatypeId = packetCode,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Event,
                State = BaseEnums.SubSubscriptionState.Running,
                NextTimeRun = DateTime.Now.AddMinutes(10),
                Format = "DATA"
            };
            await db.Insertable(subActiveLock).ExecuteCommandAsync();

            await worker.HandleEventTrigger(subActiveLock.ID, "{}", CancellationToken.None);

            var logsActiveLock = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.SubscriptionId == subActiveLock.ID)
                .ToListAsync();
            Assert.Empty(logsActiveLock);
        }

        [Fact]
        public async Task EventExportWorker_WhenStateChangedToPausedDuringExecution_PreservesPausedState_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var eventSubscriber = scope.ServiceProvider.GetRequiredService<IShareDataEventSubscriber>();
            var exportService = scope.ServiceProvider.GetRequiredService<IDataExportService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventExportWorker>>();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var partner = new ShareDataPartner
            {
                ID = $"P_C6_{uniqueId}",
                Code = $"P_C6_{uniqueId}",
                Name = $"Partner C6 {uniqueId}",
                Status = BaseEnums.StatusEnum.Enable
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var packetCode = $"996_{uniqueId}";
            await db.Insertable(new ShareDataPacket
            {
                ID = $"PK_C6_{uniqueId}",
                Code = packetCode,
                Name = "Packet C6",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                PacketVersion = "1.0",
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = $"TB_C6_{uniqueId}",
                PacketCode = packetCode,
                TableName = "TmsTrafficData",
                OrderNo = 1,
                IsRoot = true,
                IsActive = true,
                FieldsJson = "[{\"fieldKey\":\"zoneId\",\"column\":\"EquipmentId\",\"required\":true}]"
            }).ExecuteCommandAsync();

            var sub = new ShareDataSubscription
            {
                ID = $"SUB_C6_{uniqueId}",
                PartnerId = partner.ID,
                DatatypeId = packetCode,
                Direction = ShareDataEnum.SubDirection.Outbound,
                Mode = ShareDataEnum.SubMode.Event,
                State = BaseEnums.SubSubscriptionState.Active,
                Format = "DATA"
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var worker = new EventExportWorker(scopeFactory, eventSubscriber, exportService, logger);

            // Chạy HandleEventTrigger song song với việc chuyển State sang Paused
            var task = worker.HandleEventTrigger(sub.ID, "{}", CancellationToken.None);
            await Task.Delay(20);
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
                .Where(s => s.ID == sub.ID)
                .ExecuteCommandAsync();

            await task;

            var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
            Assert.Equal(BaseEnums.SubSubscriptionState.Paused, updatedSub.State);
        }
    }
}
