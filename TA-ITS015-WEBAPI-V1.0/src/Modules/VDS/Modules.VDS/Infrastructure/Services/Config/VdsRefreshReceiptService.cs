using System.Collections.Concurrent;
using Modules.DataTransporter.Base.Infrastructure;
using Modules.DataTransporter.Base.Core.Abstractions;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.VDS.Core.Dto;

namespace Modules.VDS.Infrastructure.Services.Config;

/// <summary>
/// Author: KienLT
/// Description: Correlated receipts from responding VDS processes; timeout is unknown, never success.
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsRefreshReceiptService : IScoped
{
    private sealed record Pending(string TenantId, ConcurrentDictionary<string, VdsRefreshAcknowledgement> Replies);
    private static readonly ConcurrentDictionary<string, Pending> Requests = new();
    private static readonly HashSet<IBaseEventHandler> Registered = new();
    private static readonly HashSet<IBaseEventHandler> Subscribed = new();
    private static readonly object Gate = new();

    public async Task<VdsRefreshOutput> SendAsync(int code, string tenantId)
    {
        lock (Gate)
        {
            foreach (var handler in BaseDataTransporter.handlers.ToArray())
            {
                if (Registered.Add(handler)) handler.OnDataReceived += Receive;
                if (!Subscribed.Contains(handler) && handler.SubscribeData(VdsRefreshAcknowledgement.Subject)) Subscribed.Add(handler);
            }
        }
        var id = Guid.NewGuid().ToString("N");
        var pending = new Pending(tenantId, new());
        Requests[id] = pending;
        try
        {
            var sent = ItsDataTransporter.BroadcastDataWithResult(ItsRefresh.Subject, new { code, requestId = id, tenantId });
            if (sent.Values.Any(x => x)) await Task.Delay(TimeSpan.FromSeconds(5));
            var replies = pending.Replies.Values.OrderBy(x => x.Instance).ToList();
            return new VdsRefreshOutput { Refresh = sent, Applied = replies.Count == 0 ? null : replies.All(x => x.Applied), Services = replies };
        }
        finally { Requests.TryRemove(id, out _); }
    }

    private static void Receive(string subject, object data)
    {
        if (subject != VdsRefreshAcknowledgement.Subject) return;
        try
        {
            var receipt = System.Text.Json.JsonSerializer.Deserialize<VdsRefreshAcknowledgement>(data.ToString()!,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (receipt is not null && Requests.TryGetValue(receipt.RequestId, out var pending)
                && receipt.TenantId == pending.TenantId && !string.IsNullOrWhiteSpace(receipt.Instance))
                pending.Replies[receipt.Instance] = receipt;
        }
        catch (System.Text.Json.JsonException) { }
    }
}
