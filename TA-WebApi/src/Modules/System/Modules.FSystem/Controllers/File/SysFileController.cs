

using Microsoft.Extensions.Options;
using Modules.System.Core.Entities;
using Shared.Core.Interfaces;
using Shared.Core.Settings.Options;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.FSystem.Controllers.File;

/// <summary>
/// System File Service 
/// </summary>
public class SysFileController : Modules.System.Controllers.File.SysFileController
{
    public SysFileController(UserManager userManager, SqlSugarRepository<SysFile> sysFileRep, IOptions<UploadOptions> uploadOptions, IUploadService uploadService) : base(userManager, sysFileRep, uploadOptions, uploadService)
    {
    }
}