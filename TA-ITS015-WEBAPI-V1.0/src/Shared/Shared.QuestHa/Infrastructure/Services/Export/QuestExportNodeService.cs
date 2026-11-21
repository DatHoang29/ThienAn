using System.Net.Http.Headers;
using System.Text;
using Shared.QuestHa.Infrastructure.Services.Export.Rules;

namespace Shared.QuestHa.Infrastructure.Services.Export;

/// <summary>
/// Author: KienLT
/// Description: Truy cập MỘT node QuestDB cho bản xuất Parquet: HTTP <c>/exp?fmt=parquet&amp;compression_codec=zstd</c> STREAM thẳng ra
/// file (không giữ cả file trong RAM), đếm dòng/<c>SHOW CREATE TABLE</c>/<c>INSERT … read_parquet</c> qua PG wire. Lệnh PG truyền dạng
/// delegate để test thay bằng giả; HTTP đi qua <see cref="HttpMessageHandler"/> (test dùng handler giả). Mật khẩu HTTP không bao giờ ghi log/lỗi.
/// Created date: 02/11/2026
/// </summary>
internal sealed class QuestExportNodeService : IDisposable
{
    private readonly HttpClient _http;
    private readonly Uri? _baseUrl;
    private readonly AuthenticationHeaderValue? _auth;
    private readonly TimeSpan _timeout;
    private readonly Func<string, CancellationToken, Task<long>> _scalarLong;
    private readonly Func<string, CancellationToken, Task<string?>> _scalarText;
    private readonly Func<string, CancellationToken, Task> _execute;

    /// <summary>Dựng cho <paramref name="node"/>; <paramref name="handler"/> null ⇒ SocketsHttpHandler thật.</summary>
    public QuestExportNodeService(QuestNodeOptions node, IQuestNodeClient client, TimeSpan httpTimeout, HttpMessageHandler? handler = null,
        Func<string, CancellationToken, Task<long>>? scalarLong = null, Func<string, CancellationToken, Task<string?>>? scalarText = null,
        Func<string, CancellationToken, Task>? execute = null)
    {
        Node = node.Name;
        Options = node;
        Client = client;
        _timeout = httpTimeout;
        _baseUrl = Uri.TryCreate(node.HttpUrl?.TrimEnd('/') + "/", UriKind.Absolute, out var url) ? url : null;
        if (!string.IsNullOrEmpty(node.HttpUser))
            _auth = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(node.HttpUser + ":" + node.HttpPassword)));
        // Timeout từng lệnh do CancellationToken; HttpClient không tự cắt luồng dài.
        _http = new HttpClient(handler ?? new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }, disposeHandler: true)
        { Timeout = Timeout.InfiniteTimeSpan };
        _scalarLong = scalarLong ?? (async (sql, ct) =>
        {
            await using var lease = await client.OpenLeaseAsync(ct).ConfigureAwait(false);
            lease.Db.Ado.CancellationToken = ct;
            return await lease.Db.Ado.GetLongAsync(sql).ConfigureAwait(false);
        });
        _scalarText = scalarText ?? (async (sql, ct) =>
        {
            await using var lease = await client.OpenLeaseAsync(ct).ConfigureAwait(false);
            lease.Db.Ado.CancellationToken = ct;
            return await lease.Db.Ado.GetStringAsync(sql).ConfigureAwait(false);
        });
        _execute = execute ?? (async (sql, ct) =>
        {
            await using var lease = await client.OpenLeaseAsync(ct).ConfigureAwait(false);
            lease.Db.Ado.CancellationToken = ct;
            // INSERT … read_parquet của một giờ đỉnh mất vài chục giây: không dùng CommandTimeout ngắn của đường đọc.
            lease.Db.Ado.CommandTimeOut = (int)Math.Clamp(_timeout.TotalSeconds, 30, int.MaxValue);
            await lease.Db.Ado.ExecuteCommandAsync(sql).ConfigureAwait(false);
        });
    }

    /// <summary>Tên node.</summary>
    public string Node { get; }

    /// <summary>Cấu hình node (HttpUrl, ParquetCopyRoot…).</summary>
    public QuestNodeOptions Options { get; }

    /// <summary>Client PG của node (chờ WAL).</summary>
    public IQuestNodeClient Client { get; }

    /// <summary>Số dòng của bảng trong [from, to) — cùng điều kiện với câu <c>/exp</c>.</summary>
    public Task<long> CountAsync(QuestTableSpec table, DateTime fromUtc, DateTime toUtc, CancellationToken ct) =>
        _scalarLong(QuestExportDayRules.CountQuery(table.Name, table.Designated.Name, fromUtc, toUtc), ct);

    /// <summary>Số dòng của bảng <paramref name="tableName"/> trong [from, to) theo cột <paramref name="designated"/>.</summary>
    public Task<long> CountAsync(string tableName, string designated, DateTime fromUtc, DateTime toUtc, CancellationToken ct) =>
        _scalarLong(QuestExportDayRules.CountQuery(tableName, designated, fromUtc, toUtc), ct);

    /// <summary>DDL đầy đủ của bảng (<c>SHOW CREATE TABLE</c>).</summary>
    public async Task<string> ShowCreateTableAsync(QuestTableSpec table, CancellationToken ct) =>
        await _scalarText("SHOW CREATE TABLE " + QuestExportDayRules.Quote(table.Name), ct).ConfigureAwait(false)
        ?? throw new InvalidDataException($"SHOW CREATE TABLE {table.Name} không trả DDL.");

    /// <summary>
    /// Stream kết quả <c>/exp</c> Parquet zstd của một giờ vào <paramref name="destinationPath"/> (tạo mới, flush xuống đĩa). Trả số byte.
    /// HTTP lỗi ⇒ <see cref="IOException"/> chỉ kèm mã trạng thái (không kèm URL có thông tin xác thực); hết HttpTimeout ⇒ <see cref="TimeoutException"/>.
    /// </summary>
    public async Task<long> ExportAsync(QuestTableSpec table, DateTime fromUtc, DateTime toUtc, string destinationPath, CancellationToken ct)
    {
        if (_baseUrl is null) throw new InvalidOperationException($"Node {Node} chưa khai HttpUrl.");
        var query = QuestExportDayRules.ExportQuery(table.Name, table.Designated.Name, fromUtc, toUtc);
        var uri = new Uri(_baseUrl, "exp?query=" + Uri.EscapeDataString(query) + "&fmt=parquet&compression_codec=zstd");
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(_timeout);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (_auth is not null) request.Headers.Authorization = _auth;
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, limit.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new IOException($"/exp node {Node} trả HTTP {(int)response.StatusCode}.");
            await using var body = await response.Content.ReadAsStreamAsync(limit.Token).ConfigureAwait(false);
            await using var file = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, FileOptions.Asynchronous);
            await body.CopyToAsync(file, 1 << 20, limit.Token).ConfigureAwait(false);
            await file.FlushAsync(limit.Token).ConfigureAwait(false);
            file.Flush(flushToDisk: true);
            return file.Length;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"/exp node {Node} quá {_timeout.TotalSeconds:0} s.");
        }
    }

    /// <summary>
    /// Nạp một file Parquet vào <paramref name="intoTable"/>: <c>INSERT INTO … SELECT * FROM read_parquet('<paramref name="relativePath"/>')</c>
    /// (đường dẫn tương đối so với <c>cairo.sql.copy.root</c> của node). DEDUP của bảng chống trùng khi nạp lại.
    /// </summary>
    public Task ImportAsync(string intoTable, string relativePath, CancellationToken ct)
    {
        if (!QuestExportDayRules.IsSafeTableName(intoTable)) throw new ArgumentException("Tên bảng nạp không hợp lệ.", nameof(intoTable));
        var rel = relativePath.Replace('\\', '/');
        if (rel.Contains("..", StringComparison.Ordinal) || rel.Contains('\'', StringComparison.Ordinal) || Path.IsPathRooted(rel))
            throw new ArgumentException("Đường dẫn read_parquet phải tương đối trong copy root.", nameof(relativePath));
        return _execute($"INSERT INTO {QuestExportDayRules.Quote(intoTable)} SELECT * FROM read_parquet('{rel}')", ct);
    }

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();
}
