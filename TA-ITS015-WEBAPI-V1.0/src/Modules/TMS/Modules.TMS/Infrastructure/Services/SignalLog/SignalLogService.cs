using Modules.TMS.Core.Entities;

namespace Modules.TMS.Infrastructure.Services.SignalLog
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created Date: 19/08/2024
    /// </summary>
    public class SignalLogService
    {
        public IMessageBus _messBus;

        private BaseRepository<TmsSignalLog> _tmsSignalLogRep;

        public SignalLogService(IMessageBus messBus, BaseRepository<TmsSignalLog> tmsSignalLogRep)
        {
            this._messBus = messBus;
            this._tmsSignalLogRep = tmsSignalLogRep;
        }

        public async Task<TmsSignalLog> GetLatestSignalLogByEquipmentIDAsync(string vmsId)
        {
            try
            {

                return await _tmsSignalLogRep.AsQueryable().Where(log => log.EquipmentId == vmsId)
                    .OrderByDescending(log => log.ExecuteTime)
                    .FirstAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving latest signal log by Equipment ID: {ex.Message}");
                return null;
            }
        }

        public async Task<List<TmsSignalLog>> GetIncident(string incidentId)
        {
            try
            {
                // Truy vấn tmsSignalLog theo IncidentId và sắp xếp giảm dần theo ExecuteTime
                return await _tmsSignalLogRep.AsQueryable()
                    .Where(log => log.IncidentId == incidentId)
                    .OrderByDescending(log => log.ExecuteTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving signal log by Incident ID: {ex.Message}");
                return new List<TmsSignalLog>(); // Trả về danh sách rỗng thay vì null
            }
        }

    }
}
