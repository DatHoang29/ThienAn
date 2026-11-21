using Modules.CfgSystem.Core.Entities;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Services;

namespace Modules.Media.Infrastructure.Config;

/// <summary>
/// Description: Nạp <see cref="MediaConfig"/> từ SysOpConfig (GroupCode = media_config, kèm wp_config/baseUrl_its).
/// - Đọc bằng ClearFilter + tự lọc IsDelete/Status: request ảnh thường là ẨN DANH (img src, WP gọi ITS)
///   nên filter tenant toàn cục sẽ lọc ra rỗng nếu đi qua handler SysOpConfig thông thường.
/// - Cache qua BaseCacheService, khoá "sysOpConfig:media_config" — cùng tiền tố với cache SysOpConfig
///   nên sửa cấu hình trên màn hình (RemoveByPrefixKey("sysOpConfig")) là cache này cũng được xoá.
/// Created date: 08/10/2026
/// </summary>
public sealed class MediaConfigService
{
    private const string WpConfigGroup = "wp_config";
    private const string WpItsBaseUrlCode = "baseUrl_its";

    private readonly BaseRepository<SysOpConfig> _opConfigRep;
    private readonly BaseCacheService _cache;
    private MediaConfig? _current;

    public MediaConfigService(BaseRepository<SysOpConfig> opConfigRep, BaseCacheService cache)
    {
        _opConfigRep = opConfigRep;
        _cache = cache;
    }

    public async Task<MediaConfig> GetAsync(CancellationToken ct = default)
    {
        if (_current != null) return _current;

        var rows = await _cache.GetOrAdd($"{CacheConst.Vds.OpConfig}:{MediaConst.ConfigGroup}", async _ =>
            await _opConfigRep.AsQueryable()
                .ClearFilter()
                .Where(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable)
                .Where(x => x.GroupCode == MediaConst.ConfigGroup
                            || (x.GroupCode == WpConfigGroup && x.Code == WpItsBaseUrlCode))
                .Select(x => new MediaConfigRow { GroupCode = x.GroupCode, Code = x.Code, Value = x.Value })
                .ToListAsync()) ?? new List<MediaConfigRow>();

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.Where(r => r.GroupCode == MediaConst.ConfigGroup && !string.IsNullOrWhiteSpace(r.Code)))
            values[row.Code!.Trim()] = row.Value ?? string.Empty;

        var itsBaseUrl = rows.FirstOrDefault(r => r.GroupCode == WpConfigGroup && r.Code == WpItsBaseUrlCode)?.Value;

        _current = new MediaConfig(values, itsBaseUrl);
        return _current;
    }

    /// <summary>Dòng cấu hình rút gọn để cache.</summary>
    public sealed class MediaConfigRow
    {
        public string? GroupCode { get; set; }
        public string? Code { get; set; }
        public string? Value { get; set; }
    }
}
