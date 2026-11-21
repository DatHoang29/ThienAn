using Modules.VMS.Controllers.Message.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.VMS.Controllers.Message.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    public class VmsMessageQueryHandler : IWolverineHandler
    {
        private BaseRepository<VmsMessage> _messageRepo;
        private BaseCacheService _cache;

        public VmsMessageQueryHandler(BaseRepository<VmsMessage> messageRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._messageRepo = messageRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy danh sách theo phân trang
        /// Created date: 01/12/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageVmsMessageOutput>> HandleAsync(PageVmsMessageInput command)
        {
            var msgList = await this._messageRepo.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Code), msg => msg.Code != null && msg.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrEmpty(command.Name), msg => msg.Name != null && msg.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Type), msg => msg.Type != null && msg.Type.Equals(command.Type, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Lang), msg => msg.Lang != null && msg.Lang.Equals(command.Lang, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Content1), msg => msg.Content1 != null && msg.Content1.Contains(command.Content1, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Content2), msg => msg.Content2 != null && msg.Content2.Contains(command.Content2, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((msg) => new PageVmsMessageOutput() { }, true)
                .OrderBuilder(command, "", "CreateTime", true)
                .ToPagedListAsync(command.Page, command.PageSize);

            return msgList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy danh sách theo tham số
        /// Created date: 01/12/2024
        /// </summary>
        public async Task<List<VmsMessageOutput>> HandleAsync(VmsMessageInput command)
        {
            var msgList = await this._cache.GetOrAdd(CacheConst.Vms.VmsMessage, _ =>
            {
                return this._messageRepo.AsQueryable()
                    .Select(msg => new VmsMessageOutput() { }, true)
                    .OrderBy(msg => msg.OrderNo)
                    .ToListAsync();
            });

            var result = msgList?
                .WhereIF(!string.IsNullOrEmpty(command.Code), msg => msg.Code != null && msg.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrEmpty(command.Name), msg => msg.Name != null && msg.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Type), msg => msg.Type != null && msg.Type.Equals(command.Type, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Lang), msg => msg.Lang != null && msg.Lang.Equals(command.Lang, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Content1), msg => msg.Content1 != null && msg.Content1.Contains(command.Content1, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Content2), msg => msg.Content2 != null && msg.Content2.Contains(command.Content2, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Where(msg => msg.IsDelete == null)
                .ToList();

            return result;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin theo id hoặc code
        /// Created date: 01/12/2024
        /// </summary>
        public async Task<VmsMessageOutput> HandleAsync(VmsMessageSingleInput command)
        {
            var msgList = await this._cache.GetOrAdd(CacheConst.Vms.VmsMessage, _ =>
            {
                return this._messageRepo.AsQueryable()
                    .Select(msg => new VmsMessageOutput() { }, true)
                    .OrderBy(msg => msg.OrderNo)
                    .ToListAsync();
            });

            var result = msgList?
                .WhereIF(!string.IsNullOrEmpty(command.Id), msg => msg.ID == command.Id)
                .WhereIF(!string.IsNullOrEmpty(command.Code), msg => msg.Code == command.Code)
                .FirstOrDefault();

            return result;
        }
    }
}
