global using Shared.QuestHa.Dto;
global using Shared.QuestHa.Enums;
global using Shared.QuestHa.Exceptions;
global using Shared.QuestHa.Helper;
global using Shared.QuestHa.Infrastructure.Services.Backup;
global using Shared.QuestHa.Infrastructure.Services.Cluster;
global using Shared.QuestHa.Infrastructure.Services.Journal;
// bảo trì journal và luật thuần.
global using Shared.QuestHa.Infrastructure.Services.Journal.Maintenance;
global using Shared.QuestHa.Infrastructure.Services.Journal.Maintenance.Rules;
global using Shared.QuestHa.Infrastructure.Services.Journal.Maintenance.Workers;
global using Shared.QuestHa.Infrastructure.Services.Journal.Schema;
global using Shared.QuestHa.Infrastructure.Services.Journal.Workers;
global using Shared.QuestHa.Infrastructure.Services.Node;
global using Shared.QuestHa.Infrastructure.Services.Reconcile;
global using Shared.QuestHa.Infrastructure.Services.Replication;
global using Shared.QuestHa.Infrastructure.Services.Routing;
global using Shared.QuestHa.Infrastructure.Services.Routing.Breaker;
global using Shared.QuestHa.Infrastructure.Services.Routing.Lease;
global using Shared.QuestHa.Infrastructure.Services.Routing.Status;
global using Shared.QuestHa.Infrastructure.Services.Timeline;
global using Shared.QuestHa.Interfaces;
global using Shared.QuestHa.Options;
global using Shared.QuestHa.Infrastructure.Services.Cluster.Workers;
// Đối soát node dùng chung các luật so sánh thuần.
global using Shared.QuestHa.Infrastructure.Services.Reconcile.Rules;

// luật backup và worker chỉ được DI ở host sở hữu replicator.
global using Shared.QuestHa.Infrastructure.Services.Backup.Rules;
global using Shared.QuestHa.Infrastructure.Services.Backup.Workers;
global using Shared.QuestHa.Infrastructure.Services.Health;
// snapshot tham số vận hành đổi lúc chạy.
global using Shared.QuestHa.Infrastructure.Services.Settings;
global using Shared.QuestHa.Infrastructure.Services.Settings.Rules;
// bản xuất Parquet hằng ngày.
global using Shared.QuestHa.Infrastructure.Services.Export;
