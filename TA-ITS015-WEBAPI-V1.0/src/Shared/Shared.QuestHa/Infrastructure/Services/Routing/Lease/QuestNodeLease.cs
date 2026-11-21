namespace Shared.QuestHa.Infrastructure.Services.Routing.Lease;

/// <summary>
/// Author: KienLT
/// Description: Node đọc được router cấp cho 1 lượt gọi. Chỉ dùng TRONG lượt gọi đó (router đóng kết nối khi op trả về) và tuần tự
/// không chạy song song nhiều lệnh trên cùng <see cref="Db"/>, không trả về truy vấn/luồng đọc trễ ra ngoài op.
/// Created date: 09/10/2026
/// </summary>
internal sealed record QuestNodeLease(string Node, SqlSugar.ISqlSugarClient Db) : IAsyncDisposable
{
    /// <summary>: kết nối đã mở sẵn của lease (null = SqlSugar tự mở/đóng) — router dispose sau lượt gọi.</summary>
    public IAsyncDisposable? Connection { get; init; }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Connection?.DisposeAsync() ?? ValueTask.CompletedTask;
}
