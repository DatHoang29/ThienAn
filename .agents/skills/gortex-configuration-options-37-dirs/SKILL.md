---
name: gortex-configuration-options-37-dirs
description: "Work in the Configuration/Options +37 dirs area — 831 symbols across 81 files (88% cohesion)"
---

# Configuration/Options +37 dirs

831 symbols | 81 files | 88% cohesion

## When to Use

Use this skill when working on files in:
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Counting/ITrafficCountSources.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Configuration/Loaders/ApiOptionsLoader.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Configuration/Loaders/DevicesOptionsLoader.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Configuration/Loaders/OptionsLoaderBase.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Configuration/Loaders/QueueOptionsLoader.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Extensions/ServiceCollectionExtensions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Configuration/ConfigLoaderRegistry.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Configuration/IConfigLoader.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Configuration/IConfigLoaderRegistry.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Extensions/ServiceCollectionExtensions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Persistence/DatabaseManager.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Persistence/SqlSugarAccess.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/Messaging/VdsPublisher.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/Persistence/VdsCatalogRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/Persistence/VdsDiagnosticsRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/Persistence/VdsRepositoryBase.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/State/StateManager.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/State/StatePath.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/IVdsConfigManager.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Loaders/OptionsLoaderBase.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Loaders/ServiceOptionsLoaders.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/NatsConfigApplier.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/AggregationOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/BackfillOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/CatalogOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/CountingOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/EventOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/IngestionOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/JobsOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/MediaOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/PublishOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/ServiceConfig.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/StateStoreOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/TmsApiOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/VehicleFilterOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/VehicleSizeOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/ZoneStatusOptions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/VdsConfigManager.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Extensions/ServiceCollectionExtensions.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Catalog/CameraManager.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Catalog/ConfigDataManager.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Catalog/OpConfigManager.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Catalog/ZoneManager.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Jobs/CountJob.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Jobs/PurgeJob.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Jobs/SummaryJob.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Persistence/TrafficAnalysisRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Persistence/TrafficCountRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Persistence/TrafficSummaryRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Persistence/VdsTrafficCountSources.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Processing/AnalysisProjector.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Processing/TrafficCounter.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Publishing/CountPublishTrigger.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Publishing/CountPublishWorker.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Events/Persistence/EventDataRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Events/Persistence/IncidentRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Events/Processing/IncidentPromoter.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Contracts/IVdsEventSource.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Contracts/VdsSseEnvelope.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Jobs/BackfillJob.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Messages/ProcessVdsFrameCommand.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Messages/ProcessVdsFrameCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Messages/VdsFrameDispatcher.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Processing/VdsEventProcessor.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Sources/FakeEventSource.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Sources/HistoryBackfillSource.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Journey/Jobs/JourneyJob.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Journey/Persistence/JourneyRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Legacy/VdsLegacyHooks.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Media/VdsMediaQueue.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Statistic/Persistence/TrafficStatisticRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Statistic/Processing/StatisticHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/TrafficData/Persistence/TrafficDataRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/TrafficData/Processing/TrafficDataHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/Persistence/VehicleFilterRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/Processing/PlateNotifyGate.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/Processing/VehicleFilterNotifier.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/VehicleFilterManager.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/ZoneStatus/Persistence/ZoneStatusRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Manager/VdsServiceManager.Simulation.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Manager/VdsServiceManager.cs`

## Key Files

| File | Symbols |
|------|---------|
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Counting/ITrafficCountSources.cs` | ITrafficRawCountSource, ITrafficCountSliceSource |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Configuration/Loaders/ApiOptionsLoader.cs` | ApiOptionsLoader |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Configuration/Loaders/DevicesOptionsLoader.cs` | DevicesOptionsLoader |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Configuration/Loaders/OptionsLoaderBase.cs` | OptionsLoaderBase |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Configuration/Loaders/QueueOptionsLoader.cs` | QueueOptionsLoader |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Sample/Services.Sample/Extensions/ServiceCollectionExtensions.cs` | AddCommonServices, services |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Configuration/ConfigLoaderRegistry.cs` | _serviceProvider, cancellationToken, ConfigLoaderRegistry, TConfig, LoadAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Configuration/IConfigLoader.cs` | DefaultValue, IConfigLoader, cancellationToken, TConfig, LoadAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Configuration/IConfigLoaderRegistry.cs` | IConfigLoaderRegistry, TConfig, LoadAsync, cancellationToken |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Extensions/ServiceCollectionExtensions.cs` | ServiceCollectionExtensions, AddServiceFoundation, configuration, services |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Persistence/DatabaseManager.cs` | connectionId, Get, connectionId, DatabaseManager, _scope, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/Shared/Services.Shared.Runtime/Persistence/SqlSugarAccess.cs` | Client, SqlSugarAccess.<init>, databaseManager, _resolveConnectionId, ResolveConnectionId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/Messaging/VdsPublisher.cs` | logger, _logger, _transport, IsConnected, VdsPublisher, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/Persistence/VdsCatalogRepository.cs` | Ip, KmNumber, Id, Direction, Code, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/Persistence/VdsDiagnosticsRepository.cs` | config, databaseManager, VdsDiagnosticsRepository.<init> |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/Persistence/VdsRepositoryBase.cs` | ServiceUserId, TenantId, DuplicateErrors, VdsRepositoryBase, VdsRepositoryBase.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/State/StateManager.cs` | SetPurgeWatermark, expireAt |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/State/StatePath.cs` | parts, root, Resolve |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/IVdsConfigManager.cs` | IVdsConfigManager, parameterName, Apply, json, Current, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Loaders/OptionsLoaderBase.cs` | BindSection, TOptions, OptionsLoaderBase |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Loaders/ServiceOptionsLoaders.cs` | DefaultValue, StateStoreOptionsLoader, EventOptionsLoader, PublishOptionsLoader, ZoneStatusOptionsLoader, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/NatsConfigApplier.cs` | cur, cur |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/AggregationOptions.cs` | Clone, CountPeriodMinutes, CountCacheHours, AggregationOptions, CountRetentionDays |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/BackfillOptions.cs` | BackfillMaxHours, BackfillOnStartup, HistoryPath, Clone, BackfillOptions |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/CatalogOptions.cs` | CameraReloadTimeoutSeconds, CctvModuleId, VdsEquipmentTypeId, CatalogOptions, Clone, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/CountingOptions.cs` | _equipmentIdSet, CountingEquipmentIds, CountsAllEventTypes, CountDedupWindowSeconds, Analysis, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/EventOptions.cs` | DedupWindowSeconds, _dedupWindow, _thresholds, Clone, EventTypeCodes, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/IngestionOptions.cs` | StreamPath, ReconnectBackoffMaxSeconds, UseFakeSource, ReconnectBackoffInitialMs, ApiKey, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/JobsOptions.cs` | JobsOptions, Clone, _lookup, Schedules, JobShutdownTimeoutSeconds |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/MediaOptions.cs` | MediaDownloadKinds, MediaStorageDirectory, _kindSet, MediaDownloadEnabled, kind, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/PublishOptions.cs` | PublishEvent, Clone, CountCameras, PublishOptions, PublishCount, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/ServiceConfig.cs` | VehicleFilter, Counting, Aggregation, ServiceUserId, SectionName, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/StateStoreOptions.cs` | Clone, ProcessedDataMode, ProcessedIdCapacity, StateDirectory, ProcessedPayloadCapacity, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/TmsApiOptions.cs` | Password, TimeoutSeconds, QueueCapacity, MaxRetries, Enabled, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/VehicleFilterOptions.cs` | alarm, Enabled, VehicleFilterOptions, Clone, IsAlarmOn, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/VehicleSizeOptions.cs` | VehicleLengthShortMax, VehicleLengthMediumMax, VehicleSizeMode, VehicleDimensionUnit, VehicleSizeOptions, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/Options/ZoneStatusOptions.cs` | SlowStreakToSlow, SlowCondition, CongestedCondition, AvgWindowSeconds, MinWriteIntervalSeconds, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Configuration/VdsConfigManager.cs` | LoadFromFile, Apply, parameterName, cancellationToken, Current, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Extensions/ServiceCollectionExtensions.cs` | ServiceCollectionExtensions, AddVdsServices, configuration, services, SectionName |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Catalog/CameraManager.cs` | json, camera, Name, ct, TryGet, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Catalog/ConfigDataManager.cs` | ZoneLocationType, ConfigDataManager.<init>, DirectionType, ConfigDataManager.<init>, _byType, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Catalog/OpConfigManager.cs` | GroupCode, _cachePath, catalog, KeyPrefix, config, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Catalog/ZoneManager.cs` | Count, _camerasByZone, ZoneManager.<init>, logger, camerasByZone, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Jobs/CountJob.cs` | hourlyCache, analysis, payload, logger, accumulator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Jobs/PurgeJob.cs` | counts, _state, RunAsync, _counts, PurgeJob, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Jobs/SummaryJob.cs` | logger, summaries, counts, _summaries, SummaryJob, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Persistence/TrafficAnalysisRepository.cs` | TrafficAnalysisRepository, TrafficAnalysisRepository.<init>, databaseManager, config |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Persistence/TrafficCountRepository.cs` | TrafficCountRepository.<init>, databaseManager, ct, DeleteExpiredAsync, TrafficCountRepository, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Persistence/TrafficSummaryRepository.cs` | TrafficSummaryRepository, TrafficSummaryRepository.<init>, config, databaseManager |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Persistence/VdsTrafficCountSources.cs` | raw, VdsTrafficCountSliceSource.<init>, _raw, _config, _counts, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Processing/AnalysisProjector.cs` | publisher, _repo, config, AnalysisProjector.<init>, _logger, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Processing/TrafficCounter.cs` | _publishTrigger, TrafficCounter, TrafficCounter.<init>, logger, _config, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Publishing/CountPublishTrigger.cs` | _signal, CountPublishTrigger |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Publishing/CountPublishWorker.cs` | CountPublishWorker, CountPublishWorker.<init>, _trigger, trigger, _countJob, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Events/Persistence/EventDataRepository.cs` | EventDataRepository, EventDataRepository.<init>, databaseManager, config |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Events/Persistence/IncidentRepository.cs` | databaseManager, config, IncidentRepository.<init>, IncidentRepository |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Events/Processing/IncidentPromoter.cs` | config, LocationIncidentGuard.<init>, guard, zones, configData, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Contracts/IVdsEventSource.cs` | ct, IVdsEventSource, RunAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Contracts/VdsSseEnvelope.cs` | VdsMediaItem, Media, Mime, Id, Kind, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Jobs/BackfillJob.cs` | ct, state, BackfillJob, BackfillJob.<init>, history, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Messages/ProcessVdsFrameCommand.cs` | Json, IsBackfill, ProcessVdsFrameCommand |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Messages/ProcessVdsFrameCommandHandler.cs` | _processor, command, HandleAsync, cancellationToken, ProcessVdsFrameCommandHandler, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Messages/VdsFrameDispatcher.cs` | _logger, VdsFrameDispatcher.<init>, ct, DispatchAsync, scopeFactory, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Processing/VdsEventProcessor.cs` | _events, _state, _counter, _config, _legacy, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Sources/FakeEventSource.cs` | _logger, logger, FakeEventSource, RunAsync, FakeEventSource.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Ingestion/Sources/HistoryBackfillSource.cs` | fromUtc, HistoryBackfillSource.<init>, _logger, _backfill, HasEndpoint, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Journey/Jobs/JourneyJob.cs` | _logger, JourneyJob, segments, _segments, journeys, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Journey/Persistence/JourneyRepository.cs` | config, databaseManager, ct, segmentId, JourneyRepository.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Legacy/VdsLegacyHooks.cs` | ResolveCamDirection, cam, hook, VdsLegacyHooks, logger, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Media/VdsMediaQueue.cs` | VdsMediaJob, Kind, VendorEventId, media, DetectTime, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Statistic/Persistence/TrafficStatisticRepository.cs` | TrafficStatisticRepository.<init>, TrafficStatisticRepository, config, databaseManager |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Statistic/Processing/StatisticHandler.cs` | logger, entity, StatisticHandler, publisher, _publisher, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/TrafficData/Persistence/TrafficDataRepository.cs` | TrafficDataRepository, TrafficDataRepository.<init>, databaseManager, config |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/TrafficData/Processing/TrafficDataHandler.cs` | _logger, _sizeResolver, sizeResolver, media, TrafficDataHandler, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/Persistence/VehicleFilterRepository.cs` | config, InsertHistoryAsync, entity, VehicleFilterRepository, ct, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/Processing/PlateNotifyGate.cs` | PlateNotifyGate, _sync, _lastNotifyAt, TrackedPlates |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/Processing/VehicleFilterNotifier.cs` | _config, repo, cam, VehicleFilterNotifier, _repo, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/VehicleFilterManager.cs` | VehicleFilterManager, Count, _byPlate, _logger, cachePath, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/ZoneStatus/Persistence/ZoneStatusRepository.cs` | databaseManager, ZoneStatusRepository.<init>, config |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Manager/VdsServiceManager.Simulation.cs` | SimulatedFramesPerCamera, VdsServiceManager |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Manager/VdsServiceManager.cs` | _opConfig, segments, _configData, _vehicleFilters, _cts, ... |

## Connected Communities

- **VideoWall/Controllers +63 dirs** (3 cross-edges)
- **ITS.VDS.Core/Configuration** (2 cross-edges)
- **Category/Dto +14 dirs** (1 cross-edges)

## How to Explore

```
analyze(operation:"communities", id:"community-651")
explore(operation:"context", task:"understand Configuration/Options +37 dirs", format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
