
using System;
using System.IO;
using System.Net.Http;
using Furion;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Modules.WP.Controllers.TrafficLaw.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Infrastructure;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Shared.Utility.Helpers;
using System.Linq;
using Shared.Utility.Utilities;
using SqlSugar;

namespace Modules.WP.Controllers.TrafficLaw.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các truy vấn đọc quy định giao thông hệ thống WP (TrafficLaw), bao gồm stream file PDF
    /// Created date: 03/07/2026
    /// </summary>
    public class TrafficLawQueryHandler
    {
        private readonly BaseRepository<WpTrafficLaw> _trafficLawRep;
        private readonly BaseCacheService _cache;
        private readonly IWebHostEnvironment _env;

        public TrafficLawQueryHandler(BaseRepository<WpTrafficLaw> trafficLawRep, BaseCacheService cache, IWebHostEnvironment env)
        {
            _trafficLawRep = trafficLawRep;
            _cache = cache;
            _env = env;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Traffic Law theo phân trang
        /// Created date: 14/08/2026
        /// </summary>

        public async Task<SqlSugarPagedList<WpPageTrafficLawOutput>> HandleAsync(WpPageTrafficLawInput command)
        {
            return await _trafficLawRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code)) 
                .WhereIF(!string.IsNullOrWhiteSpace(command.Title), u => u.Title.Contains(command.Title))
                .WhereIF(command.IsPublish != null, u => u.IsPublish == command.IsPublish)
                .Select((u) => new WpPageTrafficLawOutput()
                {
                }, true).OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Traffic Law theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<List<WpTrafficLawOutput>> HandleAsync(WpTrafficLawInput command)
        {
            var result = await _trafficLawRep.AsQueryable()
                 .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                 .WhereIF(!string.IsNullOrWhiteSpace(command.Title), u => u.Title.Contains(command.Title))
                 .WhereIF(command.IsPublish != null, u => u.IsPublish == command.IsPublish)
                 .Select((u) => new WpTrafficLawOutput()
                 {
                 }, true)
                 .OrderByDescending(u => u.CreateTime)
                 .ToListAsync();
            return result;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Traffic Law Detail theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>

        public async Task<WpTrafficLawDetailOutput> HandleAsync(WpTrafficLawDetailInput command)
        {
            var result = await _trafficLawRep.AsQueryable()
                .Where(u => u.IsPublish == BaseEnums.StatusEnum.Enable)
                .Select((u) => new WpTrafficLawDetailOutput()
                {
                }, true)
                .FirstAsync();

            return result;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Traffic Law Pdf Stream theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>

        public async Task<FileStreamResult> HandleAsync(WpTrafficLawPdfStreamInput input)
        {
            if (string.IsNullOrWhiteSpace(input.ID)) throw new ArgumentNullException(nameof(input.ID));

            var entity = await _trafficLawRep.AsQueryable().ClearFilter().FirstAsync(x => x.ID == input.ID)
                         ?? throw Oops.Oh("Pdf not found");

            var pdfUrl = entity.PdfUrl;
            if (string.IsNullOrWhiteSpace(pdfUrl)) throw Oops.Oh("Pdf not available");

            // Local file - resolve against known roots in order
            var relative = NormalizePath(pdfUrl);
            var candidates = new[]
            {
                Path.IsPathRooted(relative) ? relative : Path.Combine(_env.WebRootPath ?? string.Empty, relative),
                Path.Combine(_env.ContentRootPath ?? string.Empty, relative),
                !string.IsNullOrEmpty(App.Configuration["Upload:RootPath"]) ? Path.Combine(App.Configuration["Upload:RootPath"], relative) : string.Empty,
                relative
            };

            var full = candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c) && File.Exists(c));
            if (string.IsNullOrWhiteSpace(full)) throw Oops.Oh($"File not found: {pdfUrl}");

            var ext = Path.GetExtension(full).ToLowerInvariant();
            var contentTypeLocal = BaseHelper.FileHp.FileMimeHelper.GetByExtension(ext) ?? "application/pdf";
            var fs = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new FileStreamResult(fs, contentTypeLocal);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Normalize Path
        /// Created date: 14/08/2026
        /// </summary>

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path ?? string.Empty;

            var p = path.Trim();
            p = p.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            p = p.TrimStart(Path.DirectorySeparatorChar, '~');

            return p;
        }
    }
}
