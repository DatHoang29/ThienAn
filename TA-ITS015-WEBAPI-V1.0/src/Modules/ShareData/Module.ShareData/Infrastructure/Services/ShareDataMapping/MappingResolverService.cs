using Furion.DependencyInjection;
using Module.ShareData.Core.Entities;
using Shared.DTO.Enums;
using Module.ShareData.Core.Entities;


namespace Module.ShareData.Infrastructure.Services.ShareDataMappingResolverService
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Suy hồ sơ ánh xạ (ShareDataMapping)
    /// Created date: 21/09/2026
    /// </summary>
    public class ShareDataMappingResolver : IScoped
    {
        private readonly BaseRepository<ShareDataMapping> _mappingRep;

        public ShareDataMappingResolver(BaseRepository<ShareDataMapping> mappingRep)
        {
            _mappingRep = mappingRep;
        }

        /// <summary>Kết quả suy hồ sơ cho một bộ ba.</summary>
        public sealed class Resolution
        {
            public BaseEnums.SubMappingState State { get; set; } = BaseEnums.SubMappingState.NoMapping;
            public string? MappingId { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }

            public static readonly Resolution None = new();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Suy hồ sơ cho MỘT bộ ba. Dùng ở cổng chặn bật đăng ký — phải tra sống tại
        ///              đúng thời điểm quyết định, không được đọc bản chụp.
        /// Created date: 21/09/2026
        /// </summary>
        public async Task<Resolution> ResolveAsync(string? partnerId, string? datatypeId, BaseEnums.Direction? direction)
        {
            if (string.IsNullOrWhiteSpace(partnerId) || string.IsNullOrWhiteSpace(datatypeId) || direction == null)
                return Resolution.None;

            var candidates = await QueryCandidatesAsync([partnerId], [datatypeId]);
            return Classify(candidates, partnerId, datatypeId, direction.Value);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Suy hồ sơ cho NHIỀU bộ ba bằng một truy vấn duy nhất — dùng khi nạp lưới
        ///              đăng ký, tránh N+1. Khoá của Dictionary lấy từ BuildKey.
        /// Created date: 21/09/2026
        /// </summary>
        public async Task<Dictionary<string, Resolution>> ResolveManyAsync(
            IEnumerable<(string PartnerId, string DatatypeId, BaseEnums.Direction Direction)> keys)
        {
            var keyList = keys
                .Where(k => !string.IsNullOrWhiteSpace(k.PartnerId) && !string.IsNullOrWhiteSpace(k.DatatypeId))
                .Distinct()
                .ToList();

            var result = new Dictionary<string, Resolution>();
            if (keyList.Count == 0) return result;

            var partnerIds = keyList.Select(k => k.PartnerId).Distinct().ToList();
            var datatypeIds = keyList.Select(k => k.DatatypeId).Distinct().ToList();

            var candidates = await QueryCandidatesAsync(partnerIds, datatypeIds);

            foreach (var key in keyList)
            {
                result[BuildKey(key.PartnerId, key.DatatypeId, key.Direction)] =
                    Classify(candidates, key.PartnerId, key.DatatypeId, key.Direction);
            }

            return result;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Khoá tra cứu của một bộ ba — dùng chung cho cả bên gọi ResolveManyAsync.
        /// Created date: 21/09/2026
        /// </summary>
        public static string BuildKey(string? partnerId, string? datatypeId, BaseEnums.Direction? direction)
            => $"{partnerId}|{datatypeId}|{(direction.HasValue ? ((int)direction.Value).ToString() : "-")}";

        #region privatefunction

        /// <summary>Bản gọn của hồ sơ ánh xạ, chỉ lấy đủ cột để phân loại và hiển thị.</summary>
        private sealed class MappingBrief
        {
            public string? ID { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? PartnerId { get; set; }
            public string? DatatypeId { get; set; }
            public BaseEnums.Direction? Direction { get; set; }
            public bool? IsActive { get; set; }
            public DateTime? UpdateTime { get; set; }
            public DateTime? CreateTime { get; set; }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy mọi hồ sơ còn sống của tập (đối tác × gói tin) — KHÔNG lọc IsActive,
        ///              vì cần phân biệt "chưa có hồ sơ" với "có hồ sơ nhưng đang tắt".
        /// Created date: 21/09/2026
        /// </summary>
        private async Task<List<MappingBrief>> QueryCandidatesAsync(List<string> partnerIds, List<string> datatypeIds)
        {
            return await _mappingRep.AsQueryable()
                .Where(m => m.IsDelete == null)
                .Where(m => partnerIds.Contains(m.PartnerId!))
                .Where(m => datatypeIds.Contains(m.DatatypeId!))
                .Select(m => new MappingBrief
                {
                    ID = m.ID,
                    Code = m.Code,
                    Name = m.Name,
                    PartnerId = m.PartnerId,
                    DatatypeId = m.DatatypeId,
                    Direction = m.Direction,
                    IsActive = m.IsActive,
                    UpdateTime = m.UpdateTime,
                    CreateTime = m.CreateTime
                })
                .ToListAsync();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Phân loại tình trạng hồ sơ cho một bộ ba.
        ///              Ưu tiên hồ sơ ĐANG BẬT, thứ tự lấy bản cập nhật gần nhất — đúng như worker.
        ///              Không có bản nào bật nhưng có hồ sơ khớp thì trả Disabled kèm hồ sơ gần nhất,
        ///              để người dùng biết chỉ cần bật lên chứ không phải tạo mới.
        /// Created date: 21/09/2026
        /// </summary>
        private static Resolution Classify(
            List<MappingBrief> candidates,
            string partnerId,
            string datatypeId,
            BaseEnums.Direction direction)
        {
            var matched = candidates
                .Where(m => m.PartnerId == partnerId
                         && m.DatatypeId == datatypeId
                         && (m.Direction == direction || m.Direction == BaseEnums.Direction.Both))
                .OrderByDescending(m => m.UpdateTime)
                .ThenByDescending(m => m.CreateTime)
                .ToList();

            if (matched.Count == 0) return Resolution.None;

            var active = matched.FirstOrDefault(m => m.IsActive == true);
            var pick = active ?? matched[0];

            return new Resolution
            {
                State = active != null ? BaseEnums.SubMappingState.Ready : BaseEnums.SubMappingState.Disabled,
                MappingId = pick.ID,
                Code = pick.Code,
                Name = pick.Name
            };
        }

        #endregion
    }
}
