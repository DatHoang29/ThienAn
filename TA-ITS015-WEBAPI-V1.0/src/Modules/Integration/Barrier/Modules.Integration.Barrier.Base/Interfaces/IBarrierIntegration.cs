using Modules.Integration.Barrier.Base.Enums;
using Modules.Integration.Barrier.Base.Models;

namespace Modules.Integration.Barrier.Base.Interfaces;

/// <summary>
/// Hợp đồng driver Barrier theo hãng.
/// Quy ước: KHÔNG ném exception ra ngoài — mọi lỗi (mạng, PLC, timeout) trả về trong Result
/// để dịch vụ xử lý song song nhiều thiết bị không bị 1 thiết bị lỗi làm hỏng cả mẻ.
/// Author: ChuongLP
/// Date: 05-10-2026
/// </summary>
public interface IBarrierIntegration
{
    /// <summary>Hãng mà driver này phụ trách.</summary>
    BarrierVendor Vendor { get; }

    Task<BarrierCommandResult> SendCommandAsync(BarrierConfig config, BarrierCommandType command, CancellationToken ct = default);
    Task<BarrierStatusResult> GetStatusAsync(BarrierConfig config, CancellationToken ct = default);
    Task<bool> CheckConnectionAsync(BarrierConfig config, CancellationToken ct = default);
}
