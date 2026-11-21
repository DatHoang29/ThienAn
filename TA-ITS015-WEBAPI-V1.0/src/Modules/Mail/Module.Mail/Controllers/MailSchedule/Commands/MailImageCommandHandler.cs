using Modules.Mail.Core.Dto.EmailSchedule;
using Shared.Core.Interfaces;
using Shared.DTO.Upload;
using Wolverine;

namespace Modules.Mail.Controllers.EmailSchedule.Commands;

public class RptEmailImageCommandHandler : IWolverineHandler
{
    private readonly IUploadService _uploadService;

    public RptEmailImageCommandHandler(IUploadService uploadService)
    {
        _uploadService = uploadService;
    }

    public async Task<MailUploadImageOutput> HandleAsync(MailUploadImageInput command)
    {
        var file = command.File;
        if (file == null || file.Length == 0)
            throw Oops.Oh("Vui lòng chọn file ảnh không rỗng.");

        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            var count = 0;
            while (count < header.Length)
            {
                var read = await stream.ReadAsync(header.AsMemory(count));
                if (read == 0) break;
                count += read;
            }
        }

        var url = await _uploadService.UploadAsync(
            file, BaseUploadConfig.Mail.Image, file.FileName);

        return new MailUploadImageOutput
        {
            Url = url,
            Name = file.FileName
        };
    }
}
