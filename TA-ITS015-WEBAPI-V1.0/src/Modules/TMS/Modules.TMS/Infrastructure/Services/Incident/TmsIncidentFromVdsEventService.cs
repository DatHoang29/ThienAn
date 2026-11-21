using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Modules.CfgSystem.Core.Entities;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Core.Helper;
using Modules.TMS.Infrastructure.Services.Incident.Messaging;
using Modules.TMS.Infrastructure.Services.Incident.Rules;
using Modules.TMS.Infrastructure.Services.Incident.Snapshot;
using Modules.TMS.Infrastructure.Services.IncidentAutomation;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Dto;
using Modules.VDS.Core.Entities;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Infrastructure.Services.Incident
{
    /// <summary>
    /// Author: KienLT
    /// Description: Người vận hành tạo sự cố từ một bản tin VDS đã có (tab Sự kiện → "Tạo sự cố").
    ///
    ///              Dựng sự cố GIỐNG sự cố VDS tự tạo (Services.Vds IncidentPromoter): mã/tên theo loại + tuyến +
    ///              hướng + lý trình + giờ phát hiện, tuyến/hướng lấy theo camera phát hiện, thiết bị là camera CCTV
    ///              xem được cùng vùng, <c>Source = vds</c> và <c>EventId</c> = id bản tin. Nhờ Source = vds, ảnh
    ///              đăng ký lấy từ ẢNH GỐC VDS lúc xảy ra sự kiện (TmsVdsRegistrationImageService), không chụp camera
    ///              hiện tại. Khác sự cố tự động: CreatedBy là người thao tác, CreatedDate là lúc bấm tạo.
    ///
    ///              Chống trùng: chỉ mục UNIQUE (TenantId, EventId) — tính cả sự cố đã xoá — nên một bản tin chỉ có
    ///              một sự cố. Đang có sự cố MỞ cùng loại, cùng vị trí ⇒ trả về để FE hỏi lại (Force).
    ///
    ///              26/10/2026: loại sự cố cố định theo loại đã ánh xạ của bản tin (chỉ nhóm sự cố); hai hình thức —
    ///              Xử lý (Đăng ký + luồng tự động/thủ công cũ) hoặc Ghi nhận (tạo thẳng Hoàn thành/Bỏ qua/Huỷ).
    /// Created date: 23/10/2026
    /// </summary>
    public class TmsIncidentFromVdsEventService : IScoped
    {
        /// <summary>Trạng thái tính là ĐANG MỞ (lưu dạng chuỗi số), giống cửa chống trùng của worker.</summary>
        private static readonly string[] OpenStates =
        {
            ((int)TmsIncidentStateEnum.Registration).ToString(CultureInfo.InvariantCulture),
            ((int)TmsIncidentStateEnum.Verified).ToString(CultureInfo.InvariantCulture),
            ((int)TmsIncidentStateEnum.InProgress).ToString(CultureInfo.InvariantCulture),
        };

        private readonly BaseRepository<TmsIncident> _incidents;
        private readonly BaseRepository<TmsEventType> _eventTypes;
        private readonly BaseRepository<TmsEquipment> _equipment;
        private readonly BaseRepository<TmsModule> _modules;
        private readonly BaseRepository<SysConfigData> _configData;
        private readonly IMessageBus _messageBus;
        private readonly BaseCacheService _cache;
        private readonly IncidentBroadcaster _broadcaster;
        private readonly TmsIncidentAutomationService _automation;
        private readonly IncidentSnapshotQueue _snapshotQueue;
        private readonly ILogger<TmsIncidentFromVdsEventService> _logger;

        public TmsIncidentFromVdsEventService(BaseRepository<TmsIncident> incidents, BaseRepository<TmsEventType> eventTypes,
            BaseRepository<TmsEquipment> equipment, BaseRepository<TmsModule> modules, BaseRepository<SysConfigData> configData,
            IMessageBus messageBus, BaseCacheService cache, IncidentBroadcaster broadcaster,
            TmsIncidentAutomationService automation, IncidentSnapshotQueue snapshotQueue,
            ILogger<TmsIncidentFromVdsEventService> logger)
        {
            _incidents = incidents;
            _eventTypes = eventTypes;
            _equipment = equipment;
            _modules = modules;
            _configData = configData;
            _messageBus = messageBus;
            _cache = cache;
            _broadcaster = broadcaster;
            _automation = automation;
            _snapshotQueue = snapshotQueue;
            _logger = logger;
        }

        public async Task<IncidentFromVdsEventOutput> CreateAsync(CreateIncidentFromVdsEventInput input)
        {
            var eventKey = input.VendorEventId.ToString(CultureInfo.InvariantCulture);
            var tenant = App.User?.FindFirst(ClaimConst.TenantId)?.Value;

            // KienLT - 26/10/2026: process ⇒ Đăng ký rồi theo luồng xử lý cũ; record ⇒ tạo thẳng ở trạng thái kết thúc.
            var initialState = TmsIncidentFromVdsEventRules.InitialState(input.Mode, input.RecordState)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);
            var isRecord = initialState != (int)TmsIncidentStateEnum.Registration;

            // Bản tin đã có sự cố ⇒ trả sự cố đó (FE mở luôn), không coi là lỗi.
            if (await FindByEventIdAsync(eventKey, tenant) is { } existing) return Existing(existing);

            // Bản tin gốc do module VDS đọc (kiểm miền khóa + "không tìm thấy" ở VdsRecordReader).
            var detectUtc = BaseUtil.TimeServer.ToUtc(input.DetectTime);
            var row = await _messageBus.InvokeAsync<VdsTrafficData>(new VdsTrafficRecordLookupInput(input.VendorEventId, detectUtc));

            // KienLT - 26/10/2026: loại sự cố = LOẠI ĐÃ ÁNH XẠ CỦA CHÍNH BẢN TIN (cột EventType = mã TmsEventType), người
            // vận hành không chọn loại khác. Chỉ loại thuộc nhóm sự cố mới tạo được (nhóm sự kiện / 'unknown' thì không).
            var typeCode = row.EventType?.Trim();
            var type = string.IsNullOrEmpty(typeCode) ? null
                : await _eventTypes.GetFirstAsync(x => x.Code == typeCode && x.IsDelete == null);
            if (type is null || !TmsIncidentFromVdsEventRules.IsIncidentType(type.Type, type.Code))
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.FromEventTypeNotIncident);
            var group = IncidentEventGroupConst.Normalize(type.Type);

            // Tuyến (Location) và hướng (InfluenceScope) theo DANH MỤC CAMERA như worker; payload chỉ có toạ độ.
            var camera = string.IsNullOrWhiteSpace(row.EquipmentId) ? null
                : await _equipment.GetFirstAsync(x => x.ID == row.EquipmentId && x.IsDelete == null);
            var location = string.IsNullOrWhiteSpace(camera?.Location) ? null : camera!.Location!.Trim();
            var scope = camera?.DirectionId?.ToString(CultureInfo.InvariantCulture);
            var km = row.KmNumber ?? camera?.KmNumber;
            var met = row.MetNumber ?? camera?.MetNumber;

            // StartDate là GIỜ SERVER của thời điểm phát hiện (mã/tên sự cố cũng dùng mốc này).
            var startLocal = BaseUtil.TimeServer.GetTimeServer(detectUtc)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            if (!input.Force && await FindOpenAtSameLocationAsync(type.ID, km, met, location, scope) is { } open)
            {
                return new IncidentFromVdsEventOutput
                {
                    ConflictIncidentId = open.ID,
                    ConflictCode = open.Code,
                    ConflictName = open.Name,
                    ConflictStartDate = open.StartDate,
                };
            }

            var names = await LoadConfigNamesAsync();
            var code = IncidentNamingHelper.Code(type.Code, location, scope, km, met, startLocal);
            // Hai xe khác nhau có thể trùng giây phát hiện: giữ định dạng mã, thêm hậu tố theo id bản tin.
            if (await _incidents.IsAnyAsync(x => x.IsDelete == null && x.Code == code))
                code = IncidentNamingHelper.Disambiguate(code, eventKey);
            var name = IncidentNamingHelper.Name(type.Name,
                NameOf(names, VdsConfigConst.ZoneLocationTypeCode, location),
                NameOf(names, SysConfigTypeCodeEnum.TmsDirectionConfigCode, scope), km, met, startLocal);

            var cctvModuleId = (await _modules.GetFirstAsync(x => x.KeyName == IncidentConst.FromVdsEvent.CctvModuleKey
                && x.IsDelete == null))?.ID;
            var position = await IncidentPositionHelper.QueryAsync(_incidents.Context, km, met, location, scope, cctvModuleId);

            var aid = row.AidEventType ?? row.EventType;
            var account = App.User?.FindFirst(ClaimConst.Account)?.Value;
            var now = BaseUtil.TimeServer.GetTimeServer();
            var incident = new TmsIncident
            {
                Code = code,
                Name = name,
                EventTypeId = type.ID,
                EventGroup = group,
                EventId = eventKey,
                StartDate = startLocal,
                KmNumber = km,
                MetNumber = met,
                Location = location ?? "0",
                InfluenceScope = scope ?? "0",
                Source = IncidentAutomationSourceConst.Vds,
                ZoneId = position.ZoneIds,
                EquipmentId = position.EquipmentIds,
                Description = string.IsNullOrWhiteSpace(input.Description)
                    ? IncidentVdsDetailHelper.Description(row.LicensePlate, row.Speed, row.Lane)
                    : input.Description.Trim(),
                Remark = string.IsNullOrWhiteSpace(input.Remark) ? null : input.Remark.Trim(),
                Metadata = IncidentVdsDetailHelper.Metadata(aid, row.SpeedMin, row.SpeedMax, row.LicensePlate, row.Speed,
                    row.LengthMm, row.VehicleType, row.VehicleColor, row.VehicleSize, row.Lane),
                State = initialState.ToString(CultureInfo.InvariantCulture),
                CreatedBy = account,
                CreatedDate = now,
                // Ghi nhận: audit kết thúc giống BuildStateColumns của luồng đổi trạng thái (FinishedBy/FinishedDate).
                FinishedBy = isRecord ? account : null,
                FinishedDate = isRecord ? now : null,
            };
            new Core.Validators.IncidentEntityValidator().ValidateAndThrow(incident);

            try
            {
                await _incidents.InsertWithDiffLogAsync(incident);
            }
            catch (Exception ex) when (ex is SqlSugarException or System.Data.Common.DbException)
            {
                // Hai người bấm cùng lúc: chỉ mục UNIQUE EventId chặn bên thua ⇒ trả sự cố bên thắng.
                if (await FindByEventIdAsync(eventKey, tenant) is { } raced) return Existing(raced);
                throw;
            }
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncident);

            var output = new IncidentFromVdsEventOutput
            {
                IncidentId = incident.ID,
                Code = incident.Code,
                StartDate = incident.StartDate,
                State = incident.State,
                Created = true,
            };

            // Xử lý: đúng luồng sự cố VDS tự tạo — thử lập lịch tự động ở mốc Đăng ký; loại có cấu hình (trigger Đăng
            // ký, nguồn vds được phép) thì đếm ngược, không thì để thủ công (cấu hình trigger Xác minh sẽ chạy khi người
            // trực xác minh, qua luồng đổi trạng thái). Ghi nhận: sự việc đã kết thúc ⇒ không tự động, không điều khiển
            // thiết bị/video wall. Lập lịch TRƯỚC broadcast (FE nhận broadcast sẽ tải lại danh sách run ngay); tự động
            // là luồng phụ: lỗi ở đây không làm hỏng việc tạo sự cố.
            if (!isRecord)
            {
                try
                {
                    var run = await _automation.TryScheduleAsync(incident.ID, IncidentAutomationConst.TriggerState.Registration);
                    output.AutomationScheduled = run != null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[IncidentFromVdsEvent] Lập lịch tự động cho sự cố {IncidentId} thất bại.", incident.ID);
                }
            }

            // Source = vds ⇒ hàng đợi ảnh nhập ảnh gốc VDS theo EventId + StartDate (ảnh Đăng ký), không chụp camera.
            _snapshotQueue.Enqueue(incident.ID, (int)TmsIncidentStateEnum.Registration);
            await _broadcaster.NotifyAsync(incident, initialState);

            _logger.LogInformation("[IncidentFromVdsEvent] {User} tạo sự cố {Code} ({Mode}, trạng thái {State}) từ bản tin {EventId} ({DetectTime:O}).",
                incident.CreatedBy, incident.Code, isRecord ? IncidentConst.FromVdsEvent.Mode.Record : IncidentConst.FromVdsEvent.Mode.Process,
                incident.State, eventKey, detectUtc);
            return output;
        }

        /// <summary>Sự cố đã sinh từ bản tin, KỂ CẢ đã xoá (chỉ mục UNIQUE EventId không bỏ bản ghi đã xoá).</summary>
        private Task<TmsIncident?> FindByEventIdAsync(string eventKey, string? tenant) =>
            _incidents.Context.Queryable<TmsIncident>().ClearFilter()
                .Where(x => x.EventId == eventKey)
                .WhereIF(!string.IsNullOrWhiteSpace(tenant), x => x.TenantId == tenant)
                .FirstAsync()!;

        private static IncidentFromVdsEventOutput Existing(TmsIncident incident)
        {
            if (incident.IsDelete != null)
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.FromEventIncidentDeleted, incident.Code ?? incident.ID);
            return new IncidentFromVdsEventOutput
            {
                IncidentId = incident.ID,
                Code = incident.Code,
                StartDate = incident.StartDate,
                AlreadyExists = true,
            };
        }

        /// <summary>
        /// Sự cố ĐANG MỞ cùng loại, cùng lý trình (km+met), cùng tuyến/hướng — cùng điều kiện với cửa vị trí của
        /// worker nhưng không giới hạn cửa sổ thời gian: người vận hành chủ động tạo nên cần biết mọi sự cố còn mở.
        /// Thiếu lý trình ⇒ không có căn cứ so vị trí.
        /// </summary>
        private Task<TmsIncident?> FindOpenAtSameLocationAsync(string typeId, int? km, int? met, string? location, string? scope)
        {
            if (km is null && met is null) return Task.FromResult<TmsIncident?>(null);
            var openStates = OpenStates;
            var hasLocation = !string.IsNullOrWhiteSpace(location);
            var hasScope = !string.IsNullOrWhiteSpace(scope);
            return _incidents.AsQueryable()
                .Where(x => x.IsDelete == null && x.EventTypeId == typeId && x.KmNumber == km && x.MetNumber == met)
                .Where(x => openStates.Contains(x.State))
                .Where(x => SqlFunc.IsNull(x.EventGroup, IncidentEventGroupConst.Incident) != IncidentEventGroupConst.Event)
                .WhereIF(hasLocation, x => x.Location == location)
                .WhereIF(hasScope, x => x.InfluenceScope == scope)
                .OrderBy(x => x.StartDate, OrderByType.Desc)
                .FirstAsync()!;
        }

        /// <summary>Tên hiển thị của tuyến và hướng, đọc thẳng DB (vài chục dòng) như TmsTrafficMatrixService.</summary>
        private async Task<Dictionary<(string Type, string Code), string>> LoadConfigNamesAsync()
        {
            var typeCodes = new List<string> { VdsConfigConst.ZoneLocationTypeCode, SysConfigTypeCodeEnum.TmsDirectionConfigCode };
            var rows = await _configData.AsQueryable()
                .ClearFilter()
                .InnerJoin<SysConfigType>((d, t) => d.ConfigTypeId == t.ID)
                .Where((d, t) => d.IsDelete == null && t.IsDelete == null)
                .Where((d, t) => typeCodes.Contains(t.Code))
                .Select((d, t) => new { TypeCode = t.Code, d.Code, d.Name })
                .ToListAsync();
            var map = new Dictionary<(string Type, string Code), string>();
            foreach (var r in rows)
                if (!string.IsNullOrWhiteSpace(r.Code) && !string.IsNullOrWhiteSpace(r.Name))
                    map.TryAdd((r.TypeCode, r.Code.Trim()), r.Name.Trim());
            return map;
        }

        private static string? NameOf(Dictionary<(string Type, string Code), string> names, string type, string? code) =>
            string.IsNullOrWhiteSpace(code) ? null : names.TryGetValue((type, code.Trim()), out var name) ? name : code.Trim();
    }
}
