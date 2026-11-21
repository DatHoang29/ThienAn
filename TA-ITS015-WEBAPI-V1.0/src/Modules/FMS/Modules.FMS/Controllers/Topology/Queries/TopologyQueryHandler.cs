using Furion.FriendlyException;
using Modules.Fms.Core.Entities;
using Modules.FMS.Controllers.Topology.Dto;
using Modules.FMS.Core.Exceptions;
using Modules.FMS.Infrastructure;
using Modules.FMS.Infrastructure.Services.Topology;
using Wolverine;

namespace Modules.FMS.Controllers.Topology.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Xử lý các truy vấn (query) của Topology - lấy danh sách và đọc nội dung file config JSON.
    /// Created date: 08/07/2026
    /// </summary>
    public class TopologyQueryHandler : IWolverineHandler
    {
        private BaseRepository<FmsTopology> _topologyRep;
        private TopologyService _topologyService;

        public TopologyQueryHandler(BaseRepository<FmsTopology> topologyRep, TopologyService topologyService)
        {
            _topologyRep = topologyRep;
            _topologyService = topologyService;
        }

        /// <summary>
        /// Lấy danh sách bản ghi Topology từ DB
        /// Author: ChuongLP 
        /// Date: 2026-07-08
        /// </summary> 
        public async Task<List<TopologyOutput>> HandleAsync(TopologyInput command)
        {
            return await _topologyRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .Select((u) => new TopologyOutput() { }, true)
                .OrderBy(u => u.CreateTime)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy nội dung file config JSON (Nagios parent-child) theo ID bản ghi Topology.
        /// Đường dẫn thư mục config lấy từ cột Value.
        /// Author: ChuongLP 
        /// Date: 2026-07-08
        /// </summary>
        public async Task<List<TopologyConfigOutput>> HandleAsync(TopologyConfigInput command)
        {
            var entity = await _topologyRep.AsQueryable().OrderBy(u => u.CreateTime).FirstAsync() 
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            return await _topologyService.ReadConfigFilesAsync(entity.Value);
        }
    }
}
