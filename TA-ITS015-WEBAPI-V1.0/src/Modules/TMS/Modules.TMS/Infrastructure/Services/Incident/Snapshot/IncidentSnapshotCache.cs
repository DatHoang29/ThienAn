using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot
{
    /// <summary>
    /// Author: KienLT
    /// Description: Bộ đệm ngắn hạn cho snapshot SSE của sự cố, kèm khoá chống đọc chồng (single-flight).
    ///
    /// <para><b>Vì sao tách ra khỏi controller.</b> Trước 22/09/2026 <c>MemoryCache</c>, mảng
    /// <c>SemaphoreSlim</c>, việc băm khoá và cả việc dựng snapshot đều nằm ngay trong
    /// <c>TmsIncidentSnapshotController</c>. Controller theo mẫu TMS chỉ được dispatch; hạ tầng đệm là việc
    /// của <c>Infrastructure/Services</c>.</para>
    ///
    /// <para><b>Singleton là cố ý:</b> bộ đệm và các cổng phải dùng chung giữa mọi request, nếu theo scope thì
    /// mỗi kết nối lại có bộ đệm riêng và việc gộp truy vấn mất tác dụng. Đăng ký ở
    /// <c>ServiceCollectionExtensions</c> cạnh <c>IncidentSnapshotQueue</c> — cùng lý do, cùng kiểu.</para>
    ///
    /// <para><b>Khoá LUÔN chứa danh tính người gọi.</b> Snapshot sự cố bị lọc theo tenant và theo tổ chức, nên
    /// dùng chung một khoá cho hai người dùng khác phạm vi là phát dữ liệu chéo. Vì vậy
    /// <see cref="KeyFor"/> băm toàn bộ claim, không chỉ user id.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public sealed class IncidentSnapshotCache
    {
        /// <summary>Thời gian sống của một snapshot; ngắn vì stream poll mỗi vài giây.</summary>
        private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(3);

        /// <summary>Số cổng chống đọc chồng; chia theo khoá nên hai nhóm khác nhau không chờ nhau.</summary>
        private const int GateCount = 16;

        private readonly MemoryCache _snapshots = new(new MemoryCacheOptions { SizeLimit = 512 });

        private readonly SemaphoreSlim[] _gates =
            Enumerable.Range(0, GateCount).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

        /// <summary>
        /// Khoá đệm cho một người gọi và một loại snapshot.
        /// </summary>
        /// <param name="user">Người gọi; toàn bộ claim đi vào khoá để không dùng chung giữa các phạm vi quyền.</param>
        /// <param name="discriminator">Phân biệt loại snapshot, ví dụ <c>page</c> và <c>icons</c>.</param>
        /// <param name="query">Chuỗi mô tả bộ lọc; bỏ trống nếu snapshot không có tham số.</param>
        public static string KeyFor(ClaimsPrincipal? user, string discriminator, string? query = null)
        {
            var principal = user is null
                ? "<anonymous>"
                : string.Join("|", user.Claims.Select(c => c.Type + "=" + c.Value).Order());
            var material = principal + "\n" + discriminator + "\n" + (query ?? "");
            return discriminator + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
        }

        /// <summary>
        /// Lấy snapshot từ đệm, hoặc dựng mới đúng MỘT lần cho các lời gọi trùng khoá.
        /// </summary>
        public async Task<object> GetOrAddAsync(string key, Func<CancellationToken, Task<object>> factory,
            CancellationToken cancellationToken)
        {
            var gate = _gates[GateIndex(key)];
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (_snapshots.TryGetValue(key, out object? cached) && cached != null) return cached;

                var snapshot = await factory(cancellationToken);
                _snapshots.Set(key, snapshot, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = Ttl,
                    Size = 1,
                });
                return snapshot;
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>Chọn cổng theo hai ký tự hex đầu của khoá; ổn định và rải đều.</summary>
        private static int GateIndex(string key)
        {
            var hex = key.AsSpan(key.IndexOf(':') + 1);
            return hex.Length >= 2 ? Convert.ToByte(hex[..2].ToString(), 16) % GateCount : 0;
        }
    }
}
