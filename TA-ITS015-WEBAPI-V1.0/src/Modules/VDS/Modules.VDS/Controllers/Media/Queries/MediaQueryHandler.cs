using Microsoft.Extensions.Configuration;
using Modules.VDS.Controllers.Media.Dto;
using Modules.VDS.Core.Entities;
using Modules.VDS.Infrastructure;
using Modules.VDS.Infrastructure.Services.Media.Paths;
using Modules.VDS.Infrastructure.Services.TrafficData;
using Modules.VDS.Core.Constants;
using Modules.VDS.Infrastructure.Services.TrafficData.Rules;

namespace Modules.VDS.Controllers.Media.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler tra file media của một bản tin VDS.
///
/// <para>Không có bảng media: worker ghi file theo đường dẫn TẤT ĐỊNH dựng từ mốc UTC + camera + loại bản
/// tin, bên đọc dựng lại đúng đường dẫn đó (<see cref="VdsMediaLocator"/>). Thư mục gốc lấy từ cấu hình DB
/// trước, thiếu thì mới rơi về appsettings.</para>
/// Created date: 21/09/2026
/// </summary>
public class MediaQueryHandler : IWolverineHandler
{
    private readonly VdsRecordReaderService _records;
    private readonly BaseRepository<VdsConfig> _settings;
    private readonly IConfiguration _config;
    private readonly Modules.VDS.Core.Services.Media.VdsImageShareAccess _shareAccess;

    public MediaQueryHandler(VdsRecordReaderService records, BaseRepository<VdsConfig> settings, IConfiguration config,
        Modules.VDS.Core.Services.Media.VdsImageShareAccess shareAccess)
    {
        _records = records;
        _settings = settings;
        _config = config;
        _shareAccess = shareAccess;
    }

    /// <summary>Đường dẫn file media, hoặc lý do không trả được file.</summary>
    public async Task<MediaFileOutput> HandleAsync(MediaFileInput command)
    {
        // Chặn kind trước khi đọc DB: kind sai có thể là mưu toan thoát thư mục (../), không đáng tra tiếp.
        try { VdsMediaLocator.Suffix(command.Kind); }
        catch (ArgumentException) { return new MediaFileOutput { Status = MediaFileStatus.KindInvalid }; }

        // Kiểm khóa TRƯỚC khi gọi reader: reader ném Oops.Oh cho khóa sai, còn endpoint này phải trả
        // ProblemDetails 400 giống nhánh kind sai. Miền hợp lệ ở VdsEventKeyRules — dùng chung với hai API
        // chi tiết kia nên không có chuyện ba nơi kiểm ba kiểu.
        if (VdsEventKeyRules.Validate(command.EventId, command.DetectTime) is { } keyError)
            return new MediaFileOutput { Status = MediaFileStatus.KeyInvalid, MessageKey = keyError };

        var row = await _records.ReadAsync(command.EventId, command.DetectTime);
        var setting = await _settings.AsQueryable()
            .Where(x => x.IsDelete == null && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable
                && x.GroupCode == VdsConfigConst.Group.Config && x.Code == VdsConfigConst.Code.MediaStorageDirectory)
            .FirstAsync();
        var root = setting?.Value ?? _config["Vds:MediaStorageDirectory"];
        if (string.IsNullOrWhiteSpace(root))
            return new MediaFileOutput { Status = MediaFileStatus.StorageNotConfigured };

        return await _shareAccess.RunAsync(root, () =>
        {
            var file = VdsMediaLocator.Find(root, row.DetectTime, row.CameraIp, row.EventType, command.Kind);
            if (file == null) return Task.FromResult(new MediaFileOutput { Status = MediaFileStatus.NotFound });
            // Open while authenticated; the response owns this handle after the impersonation scope ends.
            return Task.FromResult(new MediaFileOutput
            {
                Status = MediaFileStatus.Ok, FilePath = file,
                ContentType = VdsMediaLocator.Extensions[Path.GetExtension(file).ToLowerInvariant()],
                Content = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                    64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan)
            });
        }, default);
    }
}
