using Microsoft.Extensions.Logging;
using Modules.TMS.Controllers.VehicleFilterHistory.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Report.Controllers.TrafficAnalysis.Dto;
using Modules.TMS.Report.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine;

namespace Modules.TMS.Report.Controllers.TrafficAnalysis.Queries
{
	/// <summary>
	/// Author: Tuan HA
	/// Create Date: 10/02/2024
	/// </summary>
	public class TrafficAnalysisQueryHandler
	{
		private const long SlowQueryThresholdMilliseconds = 3000;
		private BaseRepository<TmsTrafficAnalysis> _tmsTrafficAnalysisRep;
		private BaseCacheService _cache;
		private ILogger<TrafficAnalysisQueryHandler> _logger;
	
		public TrafficAnalysisQueryHandler(
			BaseRepository<TmsTrafficAnalysis> tmsTrafficAnalysisRep,
			BaseCacheService cache,
			ILogger<TrafficAnalysisQueryHandler> logger)
		{
			_tmsTrafficAnalysisRep = tmsTrafficAnalysisRep;
			_cache = cache;
			_logger = logger;
		}

		public async Task<SqlSugarPagedList<PageTrafficAnalysisOutput>> HandleAsync(PageTrafficAnalysisInput command)
		{
			var now = BaseUtil.TimeServer.GetTimeServer();
			var startTime = BaseUtil.TimeServer.GetTimeServer(command.StartTime) ?? now.AddDays(-3);
			var endTime = BaseUtil.TimeServer.GetTimeServer(command.EndTime) ?? now;
			var licensePlate = command.LicensePlate?.Trim();
			var stopwatch = Stopwatch.StartNew();

			try
			{
				return await _tmsTrafficAnalysisRep.AsQueryable()
					.Where(t => t.DetectTime >= startTime && t.DetectTime <= endTime)
					.WhereIF(!string.IsNullOrWhiteSpace(command.Direction) && command.Direction != "0", t => t.Direction == command.Direction)
					.WhereIF(!string.IsNullOrWhiteSpace(command.VehicleType), t => t.Type == command.VehicleType)
					.WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), t => t.EquipmentId == command.EquipmentId)
					.WhereIF(command.LengthFrom > 0 && command.LengthTo > 0 && command.LengthFrom <= command.LengthTo,
							t => t.Length >= command.LengthFrom && t.Length <= command.LengthTo)
					.WhereIF(!string.IsNullOrWhiteSpace(licensePlate), t => t.LicensePlate == licensePlate)
					.Select(u => new PageTrafficAnalysisOutput
					{
						ID = u.ID,
						LicensePlate = u.LicensePlate,
						DetectTime = u.DetectTime,
						Type = u.Type,
						Direction = u.Direction,
						Location = u.Location,
						Lane = u.Lane,
						EquipmentId = u.EquipmentId,
						IpAddress = u.IpAddress,
						Speed = u.Speed,
						Height = u.Height,
						Width = u.Width,
						Length = u.Length,
						MoveType = u.MoveType
					})
					.OrderBuilder(command, "", "DetectTime", false)
					.OrderByIF(!string.Equals(command.Field, nameof(TmsTrafficAnalysis.ID), StringComparison.OrdinalIgnoreCase),
						t => t.ID, OrderByType.Desc)
					.ToPagedListAsync(command.Page, command.PageSize);
			}
			finally
			{
				stopwatch.Stop();
				if (stopwatch.ElapsedMilliseconds >= SlowQueryThresholdMilliseconds)
				{
					_logger.LogWarning(
						"Traffic analysis page query execution ended after {ElapsedMilliseconds} ms (Page={Page}, PageSize={PageSize}, StartTime={StartTime}, EndTime={EndTime}).",
						stopwatch.ElapsedMilliseconds, command.Page, command.PageSize, startTime, endTime);
				}
				else
				{
					_logger.LogDebug(
						"Traffic analysis page query execution ended after {ElapsedMilliseconds} ms (Page={Page}, PageSize={PageSize}, StartTime={StartTime}, EndTime={EndTime}).",
						stopwatch.ElapsedMilliseconds, command.Page, command.PageSize, startTime, endTime);
				}
			}
		}



		/// <summary>
		/// Author: TuanHA
		/// Description: List theo tìm kiếm
		/// Created date: 22/03/2025
		/// </summary>
		public async Task<List<TrafficAnalysisOutput>> HandleAsync(TrafficAnalysisInput command)
		{
			var list = await this._tmsTrafficAnalysisRep.AsQueryable()
					.Where(t => t.DetectTime >= command.StartTime && t.DetectTime <= command.EndTime)
					.WhereIF(!string.IsNullOrEmpty(command.Direction) && command.Direction != "0", t => t.Direction == command.Direction)
					.WhereIF(!string.IsNullOrEmpty(command.VehicleType), t => t.Type == command.VehicleType)
					.WhereIF(command.LengthFrom > 0 && command.LengthTo > 0 && command.LengthFrom <= command.LengthTo,
							t => t.Length >= command.LengthFrom && t.Length <= command.LengthTo)
					.WhereIF(!string.IsNullOrEmpty(command.LicensePlate), t => !string.IsNullOrEmpty(t.LicensePlate) && t.LicensePlate.ToLower() == command.LicensePlate.ToLower())
					.Select((u) => new TrafficAnalysisOutput { }, true)
					.ToListAsync();

			return list;
		}

	}
}
