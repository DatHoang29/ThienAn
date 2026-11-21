using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.QuestHa;
using Shared.QuestHa.Exceptions;

namespace Modules.VDS.Infrastructure.Services.Persistence.Quest
{
    /// <summary>
    /// Author: KienLT
    /// Description: Repository QuestDB cho WebAPI, bọc MỌI lượt gọi qua cầu dao
    /// <see cref="QuestAvailabilityGate"/> — bản WebAPI của <c>QuestRepositoryBase.GuardAsync</c> bên
    /// worker.
    ///
    /// <para><b>Vì sao bắt buộc.</b> Đo được 26/08/2026: khi máy chủ QuestDB không tới được, một lượt gọi
    /// qua SqlSugar treo <b>5 phút 16 giây</b> bất chấp <c>Timeout</c>/<c>CancellationToken</c>. Ở WebAPI,
    /// mỗi request treo như vậy giữ một thread của thread pool; vài chục request là cạn pool và kéo sập cả
    /// những API KHÔNG liên quan tới VDS. Cầu dao bỏ ngay lượt gọi khi cửa đang đóng nên request trả nhanh
    /// (lỗi 503 do handler dịch từ <see cref="QuestDbUnavailableException"/>) thay vì treo.</para>
    ///
    /// <para><b>Cách dùng.</b> Handler bọc lượt truy vấn trong <see cref="GuardAsync{T}"/>:
    /// <code>
    /// return await _repo.GuardAsync(() => _repo.AsQueryable().Where(...).ToListAsync());
    /// </code>
    /// Đừng gọi thẳng <c>AsQueryable().ToListAsync()</c> — bỏ cầu dao là quay lại đúng cái treo 5 phút.</para>
    ///
    /// <para>Cầu dao là <b>singleton</b> nên trạng thái mở/đóng dùng chung cho mọi <c>T</c>: nó mô tả một
    /// sự thật duy nhất — máy chủ QuestDB có tới được hay không.</para>
    /// </summary>
    public class VdsRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        /// <summary>
        /// Hạn nghỉ sau một lỗi kết nối (giây). Khớp mặc định của worker
        /// (<c>Service:QuestUnavailableCooldownSeconds</c> = 30). WebAPI không có section Service riêng nên
        /// dùng hằng; đủ dài để không thăm dò dồn dập (mỗi lượt thăm dò khi máy chủ chết giữ luồng vài phút).
        /// </summary>
        private const int CooldownSeconds = 30;

        private readonly QuestAvailabilityGate _gate;
        // KienLT - 30/10/2026: đọc qua router PHẢI có chứng nhận coverage (head/cursor/fence/phase/quarantine/WAL) như
        // Worker; chỉ có khi ReadMode=Router (AddQuestHa vai Reader đăng ký). Không có ⇒ giữ nguyên đường cũ (cầu dao + probe).
        // KienLT - 31/10/2026: nhận QuestHaManager (cửa vào duy nhất của Shared.QuestHa) thay QuestCoveredReadService.
        private readonly QuestHaManager? _questHa;

        /// <summary>Repository WebAPI: <paramref name="questHa"/> null (ReadMode khác Router/HA tắt) ⇒ đường cầu dao cũ.</summary>
        public VdsRepository(QuestAvailabilityGate gate, QuestHaManager? questHa = null) : base("VdsDefault")
        {
            _gate = gate;
            _questHa = questHa;
        }

        /// <summary>
        /// Chạy một lượt gọi QuestDB dưới cầu dao.
        ///
        /// <paramref name="work"/> là LAMBDA chứ không phải <see cref="Task"/> đã khởi động: cầu dao phải
        /// quyết định TRƯỚC khi câu lệnh được dựng, vì SqlSugar mở kết nối đồng bộ ngay trong thân method
        /// async — "dựng task" đã là "chạm socket".
        /// </summary>
        /// <exception cref="QuestDbUnavailableException">Cửa đang đóng — bỏ lượt này, thử lại sau.</exception>
        // KienLT - 27/10/2026: callback nhận đúng client của lease, không đổi Context dùng chung khi failover.
        public async Task<TResult> GuardAsync<TResult>(Func<ISqlSugarClient, Task<TResult>> work, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (_questHa is not null)
            {
                try
                {
                    // KienLT - 30/10/2026: chốt head một lần, chờ node phủ rồi mới chạy query trên lease đó.
                    return await _questHa.ReadAsync(work, ct).ConfigureAwait(false);
                }
                catch (QuestHaUnavailableException ex) when (ex.Code == QuestHaUnavailableException.NoNodeCode)
                {
                    // Hết node Ready (kể cả journal SQL lỗi ở bước chọn node): giữ mã/RetryAfter WebAPI cũ.
                    throw new QuestDbUnavailableException("QuestHa", TimeSpan.FromSeconds(CooldownSeconds), 1);
                }
                catch (QuestHaUnavailableException ex)
                {
                    // QuestHaNotCovered (1 s) / QuestHaJournalUnavailable (5 s): 503 + Retry-After qua VdsExceptionFilter.
                    throw new QuestDbUnavailableException(ex.Code, ex.RetryAfter, ex.ConsecutiveFailures);
                }
                catch (Exception ex) when (QuestAvailabilityGate.IsConnectivityFailure(ex) && !ct.IsCancellationRequested)
                {
                    throw new QuestDbUnavailableException("QuestHa", TimeSpan.FromSeconds(CooldownSeconds), 1);
                }
            }
            if (!_gate.TryEnter(DateTime.UtcNow, out var retryAfter, out var failures))
                throw new QuestDbUnavailableException("VdsDefault", retryAfter, failures);

            try
            {
                await QuestConnectionProbe.EnsureAsync(Context.CurrentConnectionConfig.ConnectionString).ConfigureAwait(false);
                var result = await work(Context).ConfigureAwait(false);
                _gate.ReportSuccess();
                return result;
            }
            catch (OperationCanceledException)
            {
                // Huỷ theo yêu cầu (client ngắt, hết hạn) KHÔNG phải bằng chứng máy chủ chết.
                _gate.ReportUnrelatedFailure();
                throw;
            }
            catch (Exception ex)
            {
                if (QuestAvailabilityGate.IsConnectivityFailure(ex))
                {
                    _gate.ReportConnectivityFailure(
                        DateTime.UtcNow, TimeSpan.FromSeconds(CooldownSeconds));
                    throw new QuestDbUnavailableException("VdsDefault", TimeSpan.FromSeconds(CooldownSeconds), failures + 1);
                }
                else
                {
                    _gate.ReportUnrelatedFailure();
                }

                throw;
            }
        }
    }
}
