using Modules.VDS.Core.Dto;
using Modules.VDS.Core.Entities;
using Modules.VDS.Infrastructure.Services.TrafficData;

namespace Modules.VDS.Controllers.TrafficData.Queries;

/// <summary>
/// Author: KienLT
/// Description: Đọc một bản tin VDS thô cho module khác (TMS tạo sự cố từ sự kiện cũ). Đi qua
///              <see cref="VdsRecordReaderService"/> để dùng chung cửa kiểm miền khóa và lỗi "không tìm thấy".
/// Created date: 23/10/2026
/// </summary>
public sealed class VdsTrafficRecordLookupQueryHandler(VdsRecordReaderService reader) : IWolverineHandler
{
    public Task<VdsTrafficData> HandleAsync(VdsTrafficRecordLookupInput input) =>
        reader.ReadAsync(input.EventId, DateTime.SpecifyKind(input.DetectTimeUtc, DateTimeKind.Utc));
}
