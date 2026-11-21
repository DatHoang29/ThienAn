using System.Text.Json;
using Services.Sample.Features.DataFlow.Messages;
using Services.Sample.Features.DataFlow.Models;
using Shared.Utility.Utilities;

namespace Services.Sample.Features.DataFlow.Processing;

/// <summary>
/// Author: TAC Team
/// Description: Validate và chuẩn hóa payload trước khi lưu CSDL.
/// Created date: 16/07/2026
/// </summary>
public sealed class DataProcessor
{
    public Task<DataRecord> ProcessAsync(
        ProcessDataCommand message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(message.Source))
            throw new InvalidDataException("Source is required.");

        if (string.IsNullOrWhiteSpace(message.MessageId))
            throw new InvalidDataException("MessageId is required.");

        if (message.OccurredAtUtc == default)
            throw new InvalidDataException("OccurredAtUtc is required.");

        using var document = JsonDocument.Parse(message.PayloadJson);
        var normalizedJson = JsonSerializer.Serialize(document.RootElement);

        return Task.FromResult(new DataRecord(
            message.Source.Trim(),
            message.MessageId.Trim(),
            BaseUtil.TimeServer.GetTimeServer(message.OccurredAtUtc)
                ?? throw new InvalidOperationException("Could not convert UTC time to server time."),
            normalizedJson));
    }
}
