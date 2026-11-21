namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Message liên module: đọc MỘT bản tin VDS thô theo khóa kép (vendorEventId + detectTime UTC giữ
///              microsecond). TMS gửi qua message bus khi người vận hành tạo sự cố từ sự kiện cũ; handler ở
///              Modules.VDS trả <see cref="Entities.VdsTrafficData"/> hoặc ném lỗi khóa sai / không tìm thấy.
/// Created date: 23/10/2026
/// </summary>
public sealed record VdsTrafficRecordLookupInput(long EventId, DateTime DetectTimeUtc);
