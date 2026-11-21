using Newtonsoft.Json;
using Services.Vds.Shared.Parsing;
using Services.Vds.Pipeline.Contracts;

namespace Services.Vds.Pipeline.Mapping;

/// <summary>
/// Author: KienLT
/// Description: Parse frame JSON tại biên ingestion; lỗi dữ liệu trả về thay vì ném làm dừng queue.
/// Created date: 25/08/2026
/// </summary>
public static class VdsEnvelopeCodec
{
    public static VdsSseEnvelope? TryParse(string json, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "frame rỗng";
            return null;
        }

        VdsSseEnvelope? envelope;
        try
        {
            envelope = JsonConvert.DeserializeObject<VdsSseEnvelope>(json, VdsParse.JsonSettings);
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return null;
        }

        if (envelope is null)
        {
            error = "không phân tích được envelope";
            return null;
        }

        if (string.IsNullOrWhiteSpace(envelope.Type))
        {
            error = "thiếu trường type";
            return null;
        }

        return envelope;
    }
}
