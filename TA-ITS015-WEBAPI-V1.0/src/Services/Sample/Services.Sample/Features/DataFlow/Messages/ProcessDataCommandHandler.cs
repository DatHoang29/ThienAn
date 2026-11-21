using Microsoft.Extensions.Logging;
using Services.Sample.Configuration;
using Services.Sample.Features.DataFlow.Persistence;
using Services.Sample.Features.DataFlow.Processing;
using Services.Shared.Events;
using Services.Shared.Logging;
using Services.Shared.Messaging;
using Shared.Utility.Utilities;
using Wolverine;

namespace Services.Sample.Features.DataFlow.Messages;

/// <summary>
/// Author: TAC Team
/// Description: Wolverine handler xử lý <see cref="ProcessDataCommand"/> — thay thế DataWorker cũ.
/// Mỗi command đi qua: DataProcessor → Repository.InsertIfNewAsync → log persisted → publish kết quả
/// lên NATS output subject (nếu cấu hình). Logic giữ nguyên từ DataWorker.ProcessAsync, chỉ chuyển từ
/// BackgroundService consumer loop sang Wolverine handler (route qua local queue "sample-data").
/// Created date: 20/07/2026
/// </summary>
public sealed class ProcessDataCommandHandler : IWolverineHandler
{
    private readonly DataProcessor _processor;
    private readonly ServiceDataRepository _repository;
    private readonly IConfigManager _configService;
    private readonly TransportManager _transport;
    private readonly ActivityPublisher _events;

    public ProcessDataCommandHandler(
        DataProcessor processor,
        ServiceDataRepository repository,
        IConfigManager configService,
        TransportManager transport,
        ActivityPublisher events)
    {
        _processor = processor;
        _repository = repository;
        _configService = configService;
        _transport = transport;
        _events = events;
    }

    public async Task HandleAsync(ProcessDataCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var record = await _processor.ProcessAsync(command, cancellationToken).ConfigureAwait(false);
            var inserted = await _repository.InsertIfNewAsync(record, cancellationToken).ConfigureAwait(false);

            if (!inserted)
            {
                LogManager.Log(
                    LogLevel.Debug,
                    $"Skip duplicate {record.MessageId} from {record.Source}",
                    kind: LogKind.Data);
                _events.Publish(
                    "Store",
                    $"Bỏ qua bản ghi trùng {record.MessageId} từ {record.Source}.",
                    ActivityLevel.Warning);
                return;
            }

            var persistedAt = BaseUtil.TimeServer.GetTimeServer();
            _events.Publish(
                "Store",
                $"Đã lưu {record.MessageId} từ {record.Source} lúc {persistedAt:HH:mm:ss}.",
                ActivityLevel.Success);

            // Log nội bộ — thay MediatR DataPersistedNotification cũ (handler cũ chỉ có việc này).
            LogManager.Info(
                $"Stored {record.MessageId} from {record.Source} at {persistedAt}",
                kind: LogKind.Data);

            var outputSubject = _configService.Current.NatsOutputSubject;
            if (string.IsNullOrWhiteSpace(outputSubject) || !_transport.IsConnected)
                return;

            // Output inline thay ProcessedMessage DTO — chỉ handler dùng, không ai consume lại.
            var output = new
            {
                source = record.Source,
                messageId = record.MessageId,
                processedAt = persistedAt,
                status = "success",
                dataJson = record.NormalizedJson
            };

            try
            {
                var published = await _transport
                    .PublishAsync(outputSubject, output, cancellationToken)
                    .ConfigureAwait(false);

                if (published)
                {
                    _events.Publish(
                        "Nats",
                        $"Đã publish {record.MessageId} tới {outputSubject}.",
                        ActivityLevel.Success);
                }
                else
                {
                    LogManager.Log(
                        LogLevel.Warning,
                        $"Stored {record.MessageId}, but publish to {outputSubject} returned false",
                        kind: LogKind.Nats);
                    _events.Publish(
                        "Nats",
                        $"Publish {record.MessageId} tới {outputSubject} trả về false.",
                        ActivityLevel.Warning);
                }
            }
            catch (Exception exception)
            {
                LogManager.Log(
                    LogLevel.Error,
                    $"Stored {record.MessageId}, but publish to {outputSubject} failed",
                    kind: LogKind.Nats,
                    exception: exception);
                _events.Publish(
                    "Nats",
                    $"Publish {record.MessageId} tới {outputSubject} thất bại: {exception.Message}",
                    ActivityLevel.Error);
            }
        }
        catch (Exception exception)
        {
            LogManager.Log(
                LogLevel.Error,
                $"Processing failed for {command.MessageId} from {command.Source}",
                kind: LogKind.Data,
                exception: exception);
            _events.Publish(
                "Process",
                $"Xử lý thất bại {command.MessageId} từ {command.Source}: {exception.Message}",
                ActivityLevel.Error);
        }
    }
}
