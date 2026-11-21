using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.HaukiemTransaction;
using Module.TOLL.Core.Enums;
using Module.TOLL.Infrastructure.Service.Haukiem;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Infrastructure;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;

namespace Modules.TOLL.Controllers.HaukiemTransaction.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Truy vấn danh sách giao dịch cần hậu kiểm (xe vào / xe ra)
    /// Created date: 27/07/2026
    /// </summary>
    public class HaukiemTransactionQueryHandler : IWolverineHandler
    {
        private BaseRepository<TollTransactionIn> _dsVaoRep;
        private BaseRepository<TollTransactionOut> _dsRaRep;
        private UserManager _userManager;
        private TollService _tollService;

        public HaukiemTransactionQueryHandler(
            BaseRepository<TollTransactionIn> dsVaoRep,
            BaseRepository<TollTransactionOut> dsRaRep,
            UserManager userManager,
            TollService tollService)
        {
            _dsVaoRep = dsVaoRep;
            _dsRaRep = dsRaRep;
            _userManager = userManager;
            _tollService = tollService;
        }

        /// <summary>
        /// Danh sách giao dịch cần hậu kiểm.
        /// Direction = 1 lấy xe vào, = 2 lấy xe ra, để trống lấy cả hai
        /// Author: HieuNV
        /// Date: 27/07/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<ListTransactionOutPut>> HandleAsync(GetQueryPostTransactionInput input)
        {
            // FE gửi UTC+0 -> chuyển về giờ server trước khi lọc
            input.StartTime = BaseUtil.TimeServer.GetTimeServer(input.StartTime);
            input.EndTime = BaseUtil.TimeServer.GetTimeServer(input.EndTime);

            if (input.StartTime.HasValue && input.EndTime.HasValue)
            {
                _tollService.CheckDateForSearch(input.StartTime, input.EndTime);
            }

            // ================== CHỈ XE VÀO ==================
            if (input.Direction == TollEnums.DirectionEnum.In)
            {
                var dataVao = await BuildVaoQuery(input).ToListAsync();
                return ToPaged(dataVao, input);
            }

            // ================== CHỈ XE RA ==================
            if (input.Direction == TollEnums.DirectionEnum.Out)
            {
                var dataRa = await BuildRaQuery(input).ToListAsync();
                return ToPaged(dataRa, input);
            }

            // ================== CẢ HAI HƯỚNG ==================
            var vao = await BuildVaoQuery(input).ToListAsync();
            var ra = await BuildRaQuery(input).ToListAsync();

            var merged = vao.Concat(ra)
                .OrderByDescending(x => x.TransactionDatetime ?? x.TransactionDatetimeIn)
                .ToList();

            return ToPaged(merged, input);
        }

        /// <summary>
        /// Phân trang trên bộ dữ liệu đã gộp
        /// Author: HieuNV
        /// Date: 27/07/2026
        /// </summary>
        /// <param name="data"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private SqlSugarPagedList<ListTransactionOutPut> ToPaged(List<ListTransactionOutPut> data, GetQueryPostTransactionInput input)
        {
            var total = data.Count;

            var pageData = data
                .Skip((input.Page - 1) * input.PageSize)
                .Take(input.PageSize)
                .ToList();

            // Đánh số thứ tự theo trang hiện tại
            var seq = (input.Page - 1) * input.PageSize + 1;
            pageData.ForEach(item => item.Seq = seq++);

            return new SqlSugarPagedList<ListTransactionOutPut>
            {
                Current = input.Page,
                Size = input.PageSize,
                Total = total,
                Records = pageData
            };
        }

        /// <summary>
        /// Query dữ liệu hướng xe vào (TollTransactionIn)
        /// Author: HieuNV
        /// Date: 27/07/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private ISugarQueryable<ListTransactionOutPut> BuildVaoQuery(GetQueryPostTransactionInput input)
        {
            return _dsVaoRep.AsQueryable()
                .LeftJoin<TollStation>((a, tr) => tr.StationId == a.StationId)
                .LeftJoin<TollLane>((a, tr, ln) => ln.LaneId == a.LaneId)
                .LeftJoin<TollMasterData>((a, tr, ln, vt) => vt.SourceId == a.VehicleTypeId && vt.DataType == TollMasterDataType.VehicleType)
                .LeftJoin<TollMasterData>((a, tr, ln, vt, st) => st.SourceId == a.StatusId && st.DataType == TollMasterDataType.Status)
                .LeftJoin<TollMasterData>((a, tr, ln, vt, st, sc) => sc.SourceId == a.StatusChecked && sc.DataType == TollMasterDataType.TransactionStatus)
                .LeftJoin<TollMasterData>((a, tr, ln, vt, st, sc, rs) => rs.SourceId == a.ReasonId && rs.DataType == TollMasterDataType.Reason)
                .LeftJoin<TollMasterData>((a, tr, ln, vt, st, sc, rs, tt) => tt.SourceId == a.TicketTypeId && tt.DataType == TollMasterDataType.TicketType)

                .WhereIF(input.StartTime.HasValue, a => a.TransactionDateTime >= input.StartTime)
                .WhereIF(input.EndTime.HasValue, a => a.TransactionDateTime < input.EndTime)
                .WhereIF(!string.IsNullOrEmpty(input.TransactionId), a => a.TransactionId == input.TransactionId)
                .WhereIF(!string.IsNullOrEmpty(input.TagId), a => a.TagId == input.TagId)
                .WhereIF(!string.IsNullOrEmpty(input.Plate), a => a.Plate.Contains(input.Plate))
                .WhereIF(!string.IsNullOrEmpty(input.PlateLpr), a => a.PlateLpr.Contains(input.PlateLpr))
                .WhereIF(!string.IsNullOrEmpty(input.VehicleTypeId), a => a.VehicleTypeId == input.VehicleTypeId)
                .WhereIF(!string.IsNullOrEmpty(input.TicketId), a => a.TicketId == input.TicketId)
                .WhereIF(!string.IsNullOrEmpty(input.TicketTypeId), a => a.TicketTypeId == input.TicketTypeId)
                .WhereIF(!string.IsNullOrEmpty(input.StatusId), a => a.StatusId == input.StatusId)
                .WhereIF(!string.IsNullOrEmpty(input.StatusChecked), a => a.StatusChecked == input.StatusChecked)
                .WhereIF(!string.IsNullOrEmpty(input.StationId), a => a.StationId == input.StationId)
                .WhereIF(!string.IsNullOrEmpty(input.LaneId), a => a.LaneId == input.LaneId)
                .WhereIF(!string.IsNullOrEmpty(input.UserId), a => a.UserId == input.UserId)
                .WhereIF(!string.IsNullOrEmpty(input.ShiftId), a => a.ShiftCode == input.ShiftId)
                .WhereIF(!string.IsNullOrEmpty(input.ReasonId), a => a.ReasonId == input.ReasonId)

                .OrderByDescending(a => a.TransactionDateTime)

                .Select((a, tr, ln, vt, st, sc, rs, tt) => new ListTransactionOutPut
                {
                    ID = a.ID,
                    TransactionId = a.TransactionId,
                    Direction = TollEnums.DirectionCode.In,

                    TagId = a.TagId,
                    Plate = a.Plate,
                    PlateLpr = a.PlateLpr,
                    PlateEdit = a.PlateEdit,

                    VehicleTypeId = a.VehicleTypeId,
                    VehicleTypeName = vt.Name,

                    TicketId = a.TicketId,
                    TicketTypeId = a.TicketTypeId,
                    TicketTypeName = tt.Name,
                    Serial = a.Serial,

                    // Xe vào chưa phát sinh tiền -> trả 0
                    Price = 0,

                    StatusId = a.StatusId,
                    StatusName = st.Name,
                    StatusChecked = a.StatusChecked,
                    StatusCheckedName = sc.Name,

                    // Bảng xe vào chỉ có trạm/làn vào.
                    // Các field phía "ra" (StationId, LaneId, TransactionDatetime, ImageLane,
                    // ImagePlate) không gán -> mặc định null
                    StationIdIn = a.StationId,
                    StationInName = tr.Name,
                    LaneInId = a.LaneId,
                    LaneInName = ln.Name,

                    TransactionDatetimeIn = a.TransactionDateTime,
                    ProccessDate = a.ProcessDate,
                    SyncTime = a.SyncTime,

                    ImageLaneIn = a.ImageLane,
                    ImagePlateIn = a.ImagePlate,

                    UserId = a.UserId,
                    UserName = a.OperatorName,
                    ShiftId = a.ShiftCode,

                    ReasonId = a.ReasonId,
                    ReasonName = rs.Name
                });
        }

        /// <summary>
        /// Query dữ liệu hướng xe ra (TollTransactionOut)
        /// Author: HieuNV
        /// Date: 27/07/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private ISugarQueryable<ListTransactionOutPut> BuildRaQuery(GetQueryPostTransactionInput input)
        {
            return _dsRaRep.AsQueryable()
                .LeftJoin<TollStation>((a, tr) => tr.StationId == a.StationId)
                .LeftJoin<TollStation>((a, tr, tri) => tri.StationId == a.StationIdIn)
                .LeftJoin<TollLane>((a, tr, tri, ln) => ln.LaneId == a.LaneId)
                .LeftJoin<TollLane>((a, tr, tri, ln, lni) => lni.LaneId == a.LaneIdIn)
                .LeftJoin<TollMasterData>((a, tr, tri, ln, lni, vt) => vt.SourceId == a.VehicleTypeId && vt.DataType == TollMasterDataType.VehicleType)
                .LeftJoin<TollMasterData>((a, tr, tri, ln, lni, vt, st) => st.SourceId == a.StatusId && st.DataType == TollMasterDataType.Status)
                .LeftJoin<TollMasterData>((a, tr, tri, ln, lni, vt, st, sc) => sc.SourceId == a.StatusChecked && sc.DataType == TollMasterDataType.TransactionStatus)
                .LeftJoin<TollMasterData>((a, tr, tri, ln, lni, vt, st, sc, rs) => rs.SourceId == a.ReasonId && rs.DataType == TollMasterDataType.Reason)
                .LeftJoin<TollMasterData>((a, tr, tri, ln, lni, vt, st, sc, rs, tt) => tt.SourceId == a.TicketTypeId && tt.DataType == TollMasterDataType.TicketType)

                .WhereIF(input.StartTime.HasValue, a => a.TransactionDateTime >= input.StartTime)
                .WhereIF(input.EndTime.HasValue, a => a.TransactionDateTime < input.EndTime)
                .WhereIF(!string.IsNullOrEmpty(input.TransactionId), a => a.TransactionId == input.TransactionId)
                .WhereIF(!string.IsNullOrEmpty(input.TagId), a => a.TagId == input.TagId)
                .WhereIF(!string.IsNullOrEmpty(input.Plate), a => a.Plate.Contains(input.Plate))
                .WhereIF(!string.IsNullOrEmpty(input.PlateLpr), a => a.PlateLpr.Contains(input.PlateLpr))
                .WhereIF(!string.IsNullOrEmpty(input.VehicleTypeId), a => a.VehicleTypeId == input.VehicleTypeId)
                .WhereIF(!string.IsNullOrEmpty(input.TicketId), a => a.TicketId == input.TicketId)
                .WhereIF(!string.IsNullOrEmpty(input.TicketTypeId), a => a.TicketTypeId == input.TicketTypeId)
                .WhereIF(!string.IsNullOrEmpty(input.StatusId), a => a.StatusId == input.StatusId)
                .WhereIF(!string.IsNullOrEmpty(input.StatusChecked), a => a.StatusChecked == input.StatusChecked)
                .WhereIF(!string.IsNullOrEmpty(input.StationId), a => a.StationId == input.StationId || a.StationIdIn == input.StationId)
                .WhereIF(!string.IsNullOrEmpty(input.LaneId), a => a.LaneId == input.LaneId || a.LaneIdIn == input.LaneId)
                .WhereIF(!string.IsNullOrEmpty(input.UserId), a => a.UserId == input.UserId)
                .WhereIF(!string.IsNullOrEmpty(input.ShiftId), a => a.ShiftCode == input.ShiftId)
                .WhereIF(!string.IsNullOrEmpty(input.ReasonId), a => a.ReasonId == input.ReasonId)

                .OrderByDescending(a => a.TransactionDateTime)

                .Select((a, tr, tri, ln, lni, vt, st, sc, rs, tt) => new ListTransactionOutPut
                {
                    ID = a.ID,
                    TransactionId = a.TransactionId,
                    Direction = TollEnums.DirectionCode.Out,

                    TagId = a.TagId,
                    Plate = a.Plate,
                    PlateLpr = a.PlateLpr,
                    PlateEdit = a.PlateEdit,

                    VehicleTypeId = a.VehicleTypeId,
                    VehicleTypeName = vt.Name,

                    TicketId = a.TicketId,
                    TicketTypeId = a.TicketTypeId,
                    TicketTypeName = tt.Name,
                    Serial = a.Serial,
                    Price = a.Price,

                    StatusId = a.StatusId,
                    StatusName = st.Name,
                    StatusChecked = a.StatusChecked,
                    StatusCheckedName = sc.Name,

                    StationId = a.StationId,
                    StationName = tr.Name,
                    LaneId = a.LaneId,
                    LaneName = ln.Name,
                    StationIdIn = a.StationIdIn,
                    StationInName = tri.Name,
                    LaneInId = a.LaneIdIn,
                    LaneInName = lni.Name,

                    TransactionDatetime = a.TransactionDateTime,
                    TransactionDatetimeIn = a.TransactionDateTimeIn,
                    ProccessDate = a.ProcessDate,
                    SyncTime = a.SyncTime,

                    ImageLane = a.ImageLane,
                    ImagePlate = a.ImagePlate,
                    ImageLaneIn = a.ImageLaneIn,
                    ImagePlateIn = a.ImagePlateIn,

                    UserId = a.UserId,
                    UserName = a.OperatorName,
                    ShiftId = a.ShiftCode,

                    ReasonId = a.ReasonId,
                    ReasonName = rs.Name
                });
        }
    }
}
//                    StatusChecked = a.Status_Checked,
//                    StatusCheckedName = f.Name,
//                    TicketId = a.Ticket_ID,
//                    Note = a.Note,
//                    F0 = a.F0,
//                    ReasonId = a.Reason_ID,
//                    Direction = "2",
//                    ProccessDate = a.Proccess_Date,
//                    ImageLane = a.ImageLane,
//                    ImagePlate = a.ImagePlate,
//                    ImageLaneIn = a.ImageLane_In,
//                    ImagePlateIn = a.ImagePlate_In
//                }).Mapper(x =>
//                {
//                    var img = TcsImageHelper.GetLinkImageBike(
//                        new TcsImageReqDto
//                        {
//                            PlateInImage = x.ImagePlateIn,
//                            VehicleInImage = x.ImageLaneIn,
//                            PlateOutImage = x.ImagePlate,
//                            VehicleOutImage = x.ImageLane,
//                            LaneIn = x.LaneInId,
//                            LaneOut = x.LaneId,
//                            StationIn = x.StationIdIn,
//                            StationOut = x.StationId
//                        },
//                        x.TransactionDatetimeIn,
//                        x.TransactionDatetime
//                    );

//                    if (img == null) return;

//                    x.ImageLaneIn = img.LinkVehicleInImage ?? x.ImageLaneIn;
//                    x.ImageLane = img.LinkVehicleOutImage ?? x.ImageLane;
//                    x.ImagePlate = img.LinkPlateOutImage ?? x.ImagePlate;
//                    x.ImagePlateIn = img.LinkPlateInImage ?? x.ImagePlateIn;
//                });

//        }

//        /// <summary>
//        /// Xuất danh sách tìm kiếm hậu kiểm
//        /// Author: KhoaND
//        /// Date: 03/05/2025 
//        /// </summary>
//        /// <param name="input"></param>
//        /// <returns></returns>
//        public async Task<FileStreamResult> HandleAsync(ExportPostTransactionInput input)
//        {
//            var fileName = $"HauKiem_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
//            var folderPath = Path.Combine(App.WebHostEnvironment.WebRootPath, "Export");

//            if (!Directory.Exists(folderPath))
//                Directory.CreateDirectory(folderPath);

//            var filePath = Path.Combine(folderPath, fileName);

//            var queryInput = input.Adapt<GetQueryPostTransactionInput>();

//            var baseKey =
//                $"{CacheConst.TcsTransactionOut.TcsTransaction}_" + $"{queryInput.StationId}";

//            List<ListTransactionOutPut> list;

//            // ================== VAO ==================
//            if (queryInput.Direction == 1)
//            {
//                var cacheKey = $"VAO_{baseKey}";

//                list = _cache.GetOrAdd(cacheKey, _ =>
//                {
//                    return BuildVaoQuery(queryInput)
//                        .OrderByDescending(x => x.TransactionDatetimeIn)
//                        .ToList();
//                });
//            }

//            // ================== RA ==================
//            else if (queryInput.Direction == 2)
//            {
//                var cacheKey = $"RA_{baseKey}";

//                list = _cache.GetOrAdd(cacheKey, _ =>
//                {
//                    return BuildRaQuery(queryInput).ToList();
//                });
//            }

//            // ================== OFFLINE ==================
//            else if (queryInput.Direction == 3) // hoặc enum Offline
//            {
//                var cacheKey = $"OFFLINE_{baseKey}";

//                list = _cache.GetOrAdd(cacheKey, _ =>
//                {
//                    var source = BuildRaQuery(input).ToList();

//                    // filter ngay trong cache (QUAN TRỌNG)
//                    if (!string.IsNullOrEmpty(input.StatusId))
//                    {
//                        source = source
//                            .Where(x => x.StatusId == input.StatusId)
//                            .ToList();
//                    }

//                    return source;
//                });
//            }

//            // ================== ALL ==================
//            else
//            {
//                var cacheKey = $"ALL_{baseKey}";

//                list = _cache.GetOrAdd(cacheKey, _ =>
//                {
//                    var vao = BuildVaoQuery(queryInput).ToList();
//                    var ra = BuildRaQuery(queryInput).ToList();

//                    return vao.Concat(ra)
//                        .OrderByDescending(x =>
//                            x.TransactionDatetime == DateTime.MinValue
//                                ? x.TransactionDatetimeIn
//                                : x.TransactionDatetime)
//                        .ToList();
//                });
//            }
//            // Config mapping
//            TypeAdapterConfig<ListTransactionOutPut, ExportPostTransactionOutput>.NewConfig().Map(dest => dest.Seq, src => src.Seq ?? 0);
//            for (int i = 0; i < list.Count; i++)
//            {
//                list[i].Seq = i + 1;
//            }

//            var listExcel = list.Adapt<List<ExportPostTransactionOutput>>();



//            await BaseHelper.ExcelHp.ExportList(listExcel, filePath);

//            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

//            return new FileStreamResult(
//                fileStream,
//                BaseHelper.FileHp.FileMimeHelper.GetByExtension("xlsx")
//            )
//            {
//                FileDownloadName = fileName
//            };
//        }

//        /// <summary>
//        /// Lấy lịch sử 
//        /// Author: KhoaND
//        /// Date: 17/04/2025 
//        /// </summary>
//        /// <param name="input"></param>
//        /// <returns></returns>
//        public async Task<List<AuditLogPostTransactionOutPut>> HandleAsync(AuditLogPostTransactionInput input)
//        {
//            try
//            {
//                var result = await _zAuditLogRep.AsQueryable()
//                    .LeftJoin<SysUser>((a, u) => u.ID == a.Create_Uid)
//                    .WhereIF(!string.IsNullOrEmpty(input.Id), a => a.ID_Ref == input.Id)
//                    .Select((a, u) => new AuditLogPostTransactionOutPut
//                    {
//                        Id = a.ID,
//                        Name = a.Name,
//                        NOTE = a.Note,
//                        UserName = u.Username,
//                        CreateDate = a.Create_Date
//                    })
//                    .ToListAsync();

//                return result;
//            }
//            catch (Exception ex)
//            {
//                throw Oops.Oh(BaseMsg.TcsTransaction.Exception.PlateUpdateError, ex.Message);
//            }
//        }

//        /// <summary>
//        /// KhoaND 
//        /// lấy danh sách xe tạo offline
//        /// </summary>
//        /// <param name="input"></param>
//        /// <returns></returns>
//        public async Task<SqlSugarPagedList<ListTransactionOutPut>> HandleAsync(GetTransactionOffline input)
//        {

//            input.StartTime = BaseUtil.TimeServer.GetTimeServer(input.StartTime);
//            input.EndTime = BaseUtil.TimeServer.GetTimeServer(input.EndTime);

//            var reasonMap = (await _reasonRep.AsQueryable()
//                .Select(x => new { x.ID, x.Name })
//                .ToListAsync())
//                .ToDictionary(x => x.ID, x => x.Name ?? string.Empty);

//            // ================== CACHE KEY ==================

//            var baseKey = $"{CacheConst.TcsTransactionOut.TcsTransaction}_" + $"{input.StationId}";

//            var cacheKey =
//                $"OFFLINE_{baseKey}";
//            _cache.RemoveByPrefixKey(cacheKey);

//            // ================== CACHE ==================
//            var filtered = _cache.GetOrAdd(cacheKey, _ =>
//            {
//                var source = BuildRaQuery(input).ToList();

//                // filter ngay trong cache (QUAN TRỌNG)
//                if (!string.IsNullOrEmpty(input.StatusId))
//                {
//                    source = source
//                        .Where(x => x.StatusId == input.StatusId)
//                        .ToList();
//                }

//                return source;
//            });

//            // ================== MAP REASON ==================
//            foreach (var item in filtered)
//            {
//                if (!string.IsNullOrEmpty(item.StatusId) &&
//                    reasonMap.TryGetValue(item.StatusId, out var reasonName))
//                {
//                    item.ReasonName = reasonName;
//                }
//            }

//            // ================== PAGING ==================
//            return ToPaged(filtered, input);
//        }
//    }
//}
