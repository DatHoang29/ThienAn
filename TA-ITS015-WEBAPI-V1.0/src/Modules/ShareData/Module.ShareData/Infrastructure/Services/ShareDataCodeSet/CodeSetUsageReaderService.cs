using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure.Services.ShareDataMappingReader;
using Shared.DTO.Enums;

namespace Module.ShareData.Infrastructure.Services.ShareDataCodeSetService
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Tra hồ sơ ánh xạ "đang dùng" có tham chiếu 1 bộ mã — dùng chung cho chặn sửa bảng quy đổi (ESH-2206)
    ///              và cờ khoá trả cho FE ở API chi tiết bộ mã.
    ///              "Đang dùng" theo đúng luật MappingCommandHandler.HasActiveSubscriptionAsync:
    ///              hồ sơ đang bật + có đăng ký còn sống cùng đối tác, cùng gói tin, cùng chiều (hồ sơ Both khớp mọi chiều).
    /// Created date: 2026-10-30
    /// </summary>
    internal static class ShareDataCodeSetUsageReader
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Danh sách mã hồ sơ ánh xạ đang dùng có tham chiếu bộ mã; rỗng = không khoá.
        /// Created date: 2026-10-30
        /// </summary>
        internal static async Task<List<string>> FindInUseMappingsAsync(
            BaseRepository<ShareDataMapping> mappingRep,
            BaseRepository<ShareDataSubscription> subscriptionRep,
            string? code)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(code)) return result;

            // Lọc thô bằng LIKE cho nhẹ rồi đọc đúng ký hiệu $extend.codeSet bằng ShapeReader.
            var mappings = await mappingRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.IsActive == true && u.TargetShapeJson != null && u.TargetShapeJson.Contains("codeSet"))
                .ToListAsync();

            foreach (var mapping in mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.PartnerId) || string.IsNullOrWhiteSpace(mapping.DatatypeId)) continue;
                if (!ShapeReader.ReadShape(mapping.TargetShapeJson).CodeSets
                        .Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase))) continue;

                var hasActiveSubscription = await subscriptionRep.IsAnyAsync(u => u.IsDelete == null
                    && u.PartnerId == mapping.PartnerId
                    && u.DatatypeId == mapping.DatatypeId
                    && (mapping.Direction == BaseEnums.Direction.Both || u.Direction == mapping.Direction)
                    && ShareDataConst.SubStateGroup.Alive.Contains(u.State));

                if (hasActiveSubscription)
                    result.Add(mapping.Code ?? mapping.Name ?? mapping.ID);
            }

            return result;
        }
    }
}
