using Shared.Utility.Counting;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Controllers.TrafficCount.Dto;
using Modules.VDS.Infrastructure.Services.TrafficCount;
using Shared.Utility.Utilities;

namespace Modules.VDS.Controllers.TrafficCount.Queries
{
    /// <summary>
    /// Author: KienLT
    /// Description: Query handler số đếm xe (QuestDB). Thay khái niệm bảng <c>TmsTrafficCount</c> — số tính
    /// lại từ bảng thô bằng SAMPLE BY, dùng builder + folder + math DÙNG CHUNG với worker để khớp số.
    ///
    /// FE gửi UTC+0 ⇒ convert qua <c>BaseUtil.TimeServer.GetTimeServer</c>. Cửa sổ bắt buộc + trần (validator).
    /// Created date: 03/09/2026
    /// </summary>
    public class TrafficCountQueryHandler : IWolverineHandler
    {
        private readonly VdsTrafficCountService _counts;

        public TrafficCountQueryHandler(VdsTrafficCountService counts)
        {
            _counts = counts;
        }

        /// <summary>Các ô đếm 5 phút trong cửa sổ.</summary>
        public async Task<List<TrafficCountSliceOutput>> HandleAsync(TrafficCountSliceInput command)
        {
            var (from, to) = ResolveWindow(command.FromDate, command.ToDate);
            var scopeType = NormalizeScope(command.ScopeType);

            var slices = await _counts.ReadSlicesAsync(command, command.PeriodMinutes, scopeType, from, to);
            return slices.Select(ToSliceOutput).ToList();
        }

        /// <summary>Gộp toàn cửa sổ về một khối.</summary>
        public async Task<TrafficCountSummaryOutput> HandleAsync(TrafficCountSummaryInput command, CancellationToken cancellationToken = default)
        {
            var (from, to) = ResolveWindow(command.FromDate, command.ToDate);
            var scopeType = NormalizeScope(command.ScopeType);

            var slices = await _counts.ReadSlicesAsync(
                command, TrafficCountMath.DefaultPeriodMinutes, scopeType, from, to, cancellationToken);
            var block = TrafficCountMath.Sum(slices, from, to);

            return new TrafficCountSummaryOutput
            {
                Short = block.Short,
                Medium = block.Medium,
                Long = block.Long,
                Other = block.Other,
                Total = block.Total,
                AvgSpeed = block.AvgSpeed,
                MinSpeed = block.MinSpeed,
                MaxSpeed = block.MaxSpeed,
            };
        }

        /// <summary>Chuỗi 24 giờ của một ngày (bù 0 giờ không có xe).</summary>
        public async Task<List<TrafficCountHourlyOutput>> HandleAsync(TrafficCountHourlyInput command)
        {
            var value = BaseUtil.TimeServer.ToUtc(command.ValueDate) ?? command.ValueDate!.Value;
            var (dayStart, dayEnd) = BaseUtil.TimeServer.GetLocalDayUtcRange(value);

            var scopeType = NormalizeScope(command.ScopeType);

            var slices = await _counts.ReadSlicesAsync(
                command, TrafficCountMath.DefaultPeriodMinutes, scopeType, dayStart, dayEnd);

            return TrafficCountMath.ByHour(slices, dayStart, dayEnd.AddHours(-1))
                .Select(h => new TrafficCountHourlyOutput
                {
                    Hour = h.Hour,
                    Short = h.Block.Short,
                    Medium = h.Block.Medium,
                    Long = h.Block.Long,
                    Other = h.Block.Other,
                    Total = h.Block.Total,
                    AvgSpeed = h.Block.AvgSpeed,
                    MinSpeed = h.Block.MinSpeed,
                    MaxSpeed = h.Block.MaxSpeed,
                })
                .ToList();
        }

        private static (DateTime From, DateTime To) ResolveWindow(DateTime? from, DateTime? to) =>
            (BaseUtil.TimeServer.ToUtc(from) ?? from!.Value,
             BaseUtil.TimeServer.ToUtc(to) ?? to!.Value);

        /// <summary>Chuẩn hoá scope và từ chối phạm vi không được hỗ trợ.</summary>
        private static string NormalizeScope(string? scopeType)
        {
            if (string.IsNullOrWhiteSpace(scopeType)) return TrafficCountScopes.Equipment;
            var scope = scopeType.Trim().ToLowerInvariant();
            if (scope is not (TrafficCountScopes.Equipment or TrafficCountScopes.Zone))
                throw Oops.Oh(BaseMsg.Vds.Exception.ScopeTypeInvalid);
            return scope;
        }

        private static TrafficCountSliceOutput ToSliceOutput(TrafficCountSlice s) => new()
        {
            ScopeType = s.Scope.ScopeType,
            ScopeId = s.Scope.ScopeId,
            Direction = s.Scope.Direction,
            ZoneId = s.ZoneId,
            From = s.From,
            To = s.To,
            Short = s.Short,
            Medium = s.Medium,
            Long = s.Long,
            Other = s.Other,
            Total = s.Total,
            AvgSpeed = s.AvgSpeed,
            MinSpeed = s.MinSpeed,
            MaxSpeed = s.MaxSpeed,
            SpeedSampleCount = s.SpeedSampleCount,
        };
    }
}
