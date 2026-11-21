using System.Globalization;
using System.Text.RegularExpressions;
using Mapster;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Core.Helpers;
using Module.ShareData.Infrastructure.Services.ShareDataActivityService;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using Module.ShareData.Infrastructure.Services.ShareDataWrite.Analysis;
using Module.ShareData.Infrastructure.Services.ShareDataWrite;
using Module.ShareData.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.ShareData.Controllers.Packet.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa Gói tin 
    /// Created date: 2026-09-17
    /// </summary>
    public class PacketCommandHandler : IWolverineHandler
    {
        private static readonly Regex CodePattern = new(@"^[A-Za-z][A-Za-z0-9_]{0,63}$", RegexOptions.Compiled);

        /// <summary>Mốc theo công thức PA1: ngày giờ viết liền 8 tới 17 chữ số (yyyyMMdd tới yyyyMMddHHmmssfff), hoặc đủ 17 chữ số rồi dấu _ và phần đuôi được để trống.</summary>
        private static readonly Regex CursorTimeKeyPattern = new(@"^([0-9]{8,17}|[0-9]{17}_\S*)$", RegexOptions.Compiled);

        /// <summary>Định dạng phần thời gian của mốc theo công thức PA1 (CONVERT kiểu 112 rồi kiểu 114 bỏ dấu hai chấm).</summary>
        private const string CursorTimeFormat = "yyyyMMddHHmmssfff";

        private readonly BaseRepository<ShareDataPacket> _packetRep;
        private readonly BaseRepository<ShareDataSql> _sqlRep;
        private readonly BaseRepository<ShareDataMapping> _mappingRep;
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;
        private readonly BaseCacheService _cache;
        private readonly ShareDataActivityLogger _activityLogger;
        private readonly ISqlSugarClient _db;
        private readonly PacketSqlService _packetSqlService;
        private readonly UserManager _userManager;
        private readonly WriteSqlService _writeSqlService;

        public PacketCommandHandler(
            BaseRepository<ShareDataPacket> packetRep,
            BaseRepository<ShareDataSql> sqlRep,
            BaseRepository<ShareDataMapping> mappingRep,
            BaseRepository<ShareDataSubscription> subscriptionRep,
            BaseCacheService cache,
            ShareDataActivityLogger activityLogger,
            ISqlSugarClient db,
            PacketSqlService packetSqlService,
            UserManager userManager,
            WriteSqlService writeSqlService)
        {
            _packetRep = packetRep;
            _sqlRep = sqlRep;
            _mappingRep = mappingRep;
            _subscriptionRep = subscriptionRep;
            _cache = cache;
            _activityLogger = activityLogger;
            _db = db;
            _packetSqlService = packetSqlService;
            _userManager = userManager;
            _writeSqlService = writeSqlService;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 gói tin. Xóa mốc còn sót của gói cũ cùng mã: đối tác gửi gói mới từ đầu.
        /// Created date: 2026-09-17
        /// Modified date: 05/11/2026
        /// </summary>
        public async Task HandleAsync(ShareDataAddPacketInput command)
        {
            command.Code = command.Code?.Trim();
            command.Name = command.Name?.Trim();
            command.PacketVersion = command.PacketVersion?.Trim();

            if (await _packetRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.ShareData.Entity.PacketCode]);

            command.Status ??= BaseEnums.StatusEnum.Enable;
            command.CreateUId = null;

            await ClearPacketCursors(command.Code);

            // Gói tạo theo loại (OrderNo): chép câu SQL mẫu chung (ShareDataSql) của gói cùng loại tạo sớm nhất còn dùng và đã có câu SQL.
            // ShareDataSql là nguồn câu SQL duy nhất (bỏ TemplateSql 27/10/2026).
            ShareDataSql? sourceSql = null;
            if (command.OrderNo.HasValue)
            {
                var sourcePacketIds = await _packetRep.AsQueryable()
                    .Where(u => u.IsDelete == null && u.OrderNo == command.OrderNo.Value)
                    .OrderBy(u => u.CreateTime, OrderByType.Asc)
                    .Select(u => u.ID)
                    .ToListAsync();
                if (sourcePacketIds.Count > 0)
                {
                    var sourceSqls = await _sqlRep.AsQueryable()
                        .Where(s => s.IsDelete == null && s.PartnerId == null && sourcePacketIds.Contains(s.PacketId!)
                            && s.ExtractSql != null && s.ExtractSql != "")
                        .ToListAsync();
                    sourceSql = sourcePacketIds
                        .Select(id => sourceSqls.FirstOrDefault(s => s.PacketId == id))
                        .FirstOrDefault(s => s != null);
                }
            }

            var entity = command.Adapt<ShareDataPacket>();
            await _packetRep.InsertWithDiffLogAsync(entity);

            await _activityLogger.LogCreateAsync(entity);

            // Gói tạo theo loại: câu SQL mẫu chung, thiết lập SQL, danh sách trường và bộ biến chép nguyên từ gói gốc cùng loại
            if (sourceSql != null)
            {
                var packetId = !string.IsNullOrWhiteSpace(entity.ID)
                    ? entity.ID
                    : (await _packetRep.AsQueryable().Where(u => u.IsDelete == null && u.Code == command.Code).FirstAsync())?.ID;
                var commonSql = new ShareDataSql
                {
                    PacketId = packetId,
                    ExtractSql = sourceSql.ExtractSql,
                    FieldsJson = sourceSql.FieldsJson,
                    VariablesJson = sourceSql.VariablesJson,
                    DescribedTime = sourceSql.DescribedTime
                };

                await _sqlRep.InsertWithDiffLogAsync(commonSql);
            }

            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPacket);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 gói tin
        /// Created date: 2026-09-17
        /// </summary>
        public async Task HandleAsync(ShareDataUpdatePacketInput command)
        {
            command.Code = command.Code?.Trim();
            command.Name = command.Name?.Trim();
            command.PacketVersion = command.PacketVersion?.Trim();

            var entity = await _packetRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await _packetRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.ShareData.Entity.PacketCode]);

            // Gói tin đang được phễu lọc tham chiếu (DatatypeId = Packet.ID) thì khóa đổi Mã/Tên —
            // các trường mô tả khác (PacketVersion, Description, OrderNo, Status, Remark) vẫn sửa được.
            var codeOrNameChanged = !string.Equals(entity.Code, command.Code, StringComparison.Ordinal)
                || !string.Equals(entity.Name, command.Name, StringComparison.Ordinal);

            if (codeOrNameChanged && await IsDatatypeInUseAsync(entity.ID))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNameLocked);

            // Tắt gói khi còn đăng ký bật thì worker bỏ qua im lặng, đăng ký vẫn hiện Active mà không chạy:
            // phải tắt đăng ký (cả gửi lẫn nhận) trước rồi mới tắt gói.
            if (entity.Status != BaseEnums.StatusEnum.Disable && command.Status == BaseEnums.StatusEnum.Disable)
                await EnsureNoActiveSubscriptionAsync(entity, null, null, BaseMsg.ShareData.Exception.PacketDisableInUse);

            var before = entity.Adapt<ShareDataPacket>();

            command.Adapt(entity);

            /// <summary>Loại gói (thứ tự) cố định từ khi tạo gói, bỏ qua giá trị client gửi lên khi sửa gói.</summary>
            entity.OrderNo = before.OrderNo;

            await _packetRep.UpdateWithDiffLogAsync(entity);
            await _activityLogger.LogUpdateAsync(before, entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPacket);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 gói tin (xóa mềm), xóa luôn mốc gửi của gói
        /// Created date: 2026-09-17
        /// Modified date: 05/11/2026
        /// </summary>
        public async Task HandleAsync(ShareDataDeletePacketInput command)
        {
            var entity = await _packetRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await IsDatatypeInUseAsync(entity.ID))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PacketInUse);

            await _packetRep.SoftDeleteAsync<ShareDataPacket>(entity);
            await ClearPacketCursors(entity.Code);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPacket);

            await _activityLogger.LogDeleteAsync(entity);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều gói tin (xóa mềm), xóa luôn mốc gửi của các gói đó
        /// Created date: 2026-09-17
        /// Modified date: 05/11/2026
        /// </summary>
        public async Task HandleAsync(List<ShareDataDeletePacketInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _packetRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            foreach (var item in entities)
                if (await IsDatatypeInUseAsync(item.ID))
                    throw Oops.Oh(BaseMsg.ShareData.Exception.PacketInUse);

            await _packetRep.SoftDeleteAsync<ShareDataPacket>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPacket);

            foreach (var item in entities)
            {
                await ClearPacketCursors(item.Code);
                await _activityLogger.LogDeleteAsync(item);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Xóa mọi mốc gửi theo mã gói tin. Mốc gắn theo mã nên còn sót thì gói tạo sau cùng mã đi tiếp từ mốc cũ và mất dữ liệu trước mốc đó.
        /// Created date: 05/11/2026
        /// </summary>
        private async Task ClearPacketCursors(string? packetCode)
        {
            var rows = await _db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PacketCode == packetCode)
                .ToListAsync();

            foreach (var row in rows)
            {
                await _db.Deleteable<ShareDataLastSend>().Where(x => x.ID == row.ID).ExecuteCommandAsync();
                await _activityLogger.LogDeleteAsync(row);
            }
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: ID gói tin có đang được tham chiếu hay không — tính CẢ HAI nơi:
        ///              phễu lọc (ShareDataMapping.DatatypeId) và đăng ký chia sẻ còn sống
        ///              (ShareDataSubscription.DatatypeId, trạng thái Active).
        ///              Đăng ký đã hủy/từ chối/hết hạn không tính.
        /// Created date: 17/09/2026
        /// </summary>
        private async Task<bool> IsDatatypeInUseAsync(string? datatypeId)
        {
            if (string.IsNullOrWhiteSpace(datatypeId)) return false;

            if (await _mappingRep.IsAnyAsync(u => u.IsDelete == null && u.DatatypeId == datatypeId))
                return true;

            return await _subscriptionRep.IsAnyAsync(u => u.IsDelete == null
                && u.DatatypeId == datatypeId
                && ShareDataConst.SubStateGroup.Alive.Contains(u.State));
        }

        /// <summary>
        /// Description: Cấu hình SQL của gói (câu trích xuất, biến, câu ghi) và mốc gửi được worker đọc sống ở mỗi lượt chạy,
        /// Created date: 29/10/2026
        /// </summary>
        private async Task EnsureNoActiveSubscriptionAsync(ShareDataPacket packet, string? partnerId, BaseEnums.Direction? direction,
            string message = BaseMsg.ShareData.Exception.PacketSqlInUse)
        {
            var packetId = packet.ID;
            var packetCode = packet.Code;

            var partnerIds = await _subscriptionRep.AsQueryable()
                .Where(u => u.IsDelete == null
                    && u.State == BaseEnums.SubSubscriptionState.Active
                    && (u.DatatypeId == packetId || u.DatatypeId == packetCode))
                .WhereIF(direction != null, u => u.Direction == direction || u.Direction == BaseEnums.Direction.Both)
                .WhereIF(!string.IsNullOrWhiteSpace(partnerId), u => u.PartnerId == partnerId)
                .Select(u => u.PartnerId)
                .Distinct()
                .ToListAsync();

            if (partnerIds.Count == 0) return;

            var partners = await _db.Queryable<ShareDataPartner>()
                .Where(p => partnerIds.Contains(p.ID))
                .Select(p => new { p.Name, p.Code })
                .ToListAsync();
            var partnerNames = partners.Count > 0
                ? string.Join(", ", partners.Select(p => p.Name ?? p.Code))
                : string.Join(", ", partnerIds);

            throw Oops.Oh(message, packetCode ?? string.Empty, partnerNames);
        }

        #region Câu truy vấn SQL trích xuất (mẫu chung / SQL riêng đối tác)
        /// <summary>
        /// Author: DatHQ
        /// Description: Lưu câu SQL mẫu chung của gói tin. Chỉ SuperAdmin; còn đăng ký gửi đang bật thì từ chối (ESH-2306).
        ///              Câu làm mất khóa đang được hồ sơ ánh xạ dùng hoặc đổi công thức cột mốc thì chưa lưu, trả RequiresConfirm;
        ///              FE hỏi xác nhận rồi gửi lại với Force, khi đó mốc của các đối tác bị ảnh hưởng được đặt lại trong cùng giao dịch.
        ///              Câu nối đuôi có cột mốc chỉ là ngày giờ hoặc dùng UpdateTime trơn thì từ chối (ValidateCursorTime).
        /// Created date: 19/10/2026
        /// Modified date: 05/11/2026
        /// </summary>
        public async Task<PacketExtractSqlOutput> HandleAsync(UpdatePacketExtractSqlInput command)
        {
            // 1. Phân quyền: Chỉ tài khoản SuperAdmin mới có quyền chỉnh sửa câu truy vấn nguồn
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlEdit);
            }

            command.PacketCode = command.PacketCode?.Trim() ?? string.Empty;
            command.ExtractSql = command.ExtractSql?.Trim();

            if (string.IsNullOrWhiteSpace(command.PacketCode))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PacketCodeRequired);
            }

            var packet = await _packetRep.AsQueryable()
                .Where(x => x.IsDelete == null && x.Code == command.PacketCode)
                .FirstAsync();

            if (packet == null)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNotFound, command.PacketCode);
            }

            // Câu mẫu chung dùng cho mọi đối tác chưa có SQL riêng: còn đăng ký gửi nào đang bật thì phải tắt trước
            await EnsureNoActiveSubscriptionAsync(packet, null, BaseEnums.Direction.Outbound);

            // Bộ biến gửi kèm câu SQL: kiểm câu theo đúng bộ này và ghi cùng câu trong một lần (null = dùng bộ đang lưu)
            var incomingVariables = command.Variables == null ? null : NormalizeOrdered(command.Variables);

            var db = _packetRep.Context;
            var commonSql = await SqlTextHelper.SelectCommonSql(db, packet.ID);
            var currentFields = SqlFieldHelper.Read(commonSql?.FieldsJson);

            List<string> columns = [];
            List<SqlColumnInfo> columnInfos = [];
            var variables = SqlVariableSet.Empty;
            List<SqlFieldItem> fields = [];
            List<string> lostKeys;
            if (!string.IsNullOrWhiteSpace(command.ExtractSql))
            {
                // 2. Kiểm tra an toàn câu lệnh SQL bằng AST ScriptDom (cho dùng các biến đã khai ở bảng tham số của mẫu chung)
                variables = incomingVariables != null
                    ? SqlVariableSet.From(incomingVariables)
                    : await SqlTextHelper.ResolveVariables(_db, packet.ID, null);
                var sqlValidation = await _packetSqlService.ValidateSql(_db, command.ExtractSql, variables);
                if (!sqlValidation.IsValid)
                {
                    // Ghi Audit Log cảnh báo bảo mật khi câu truy vấn vi phạm
                    var errorMsg = sqlValidation.ErrorMessage;
                    await _activityLogger.LogActionAsync(
                        BaseEnums.ActivityAction.Reject,
                        new ShareDataPacket { Code = command.PacketCode },
                        description: $"Từ chối lưu câu truy vấn SQL không an toàn cho gói tin [{command.PacketCode}]: {errorMsg}",
                        reason: errorMsg);

                    throw SqlValidatorHelper.ToException(sqlValidation.ErrorKey, sqlValidation.ErrorArgs);
                }

                columns = sqlValidation.Columns;
                columnInfos = sqlValidation.ColumnInfos;

                // 2a. Câu có cột mốc mà không dùng biến mốc thì lượt nào cũng gửi lại cùng dữ liệu
                var cursorUse = SqlValidatorHelper.ValidateCursorColumnUsed(command.ExtractSql, columns, variables);
                if (!cursorUse.IsValid)
                {
                    throw SqlValidatorHelper.ToException(cursorUse.ErrorKey, cursorUse.ErrorArgs);
                }

                // 2b. Kiểm tra cấu trúc bắt buộc nếu câu SQL là Incremental (nối đuôi)
                if (SqlValidatorHelper.IsIncrementalSql(command.ExtractSql, variables.CursorNames))
                {
                    var incValidation = SqlValidatorHelper.ValidateIncrementalStructure(
                        command.ExtractSql,
                        columns,
                        variables
                    );
                    if (!incValidation.IsValid)
                    {
                        throw SqlValidatorHelper.ToException(incValidation.ErrorKey, incValidation.ErrorArgs);
                    }

                    var timeValidation = SqlValidatorHelper.ValidateCursorTime(command.ExtractSql, columnInfos, variables);
                    if (!timeValidation.IsValid)
                    {
                        throw SqlValidatorHelper.ToException(timeValidation.ErrorKey, timeValidation.ErrorArgs);
                    }
                }

                // 3. Danh sách trường sinh từ câu SQL: gộp với bản đang lưu rồi áp phần người dùng sửa
                var merge = SqlFieldHelper.Merge(currentFields, sqlValidation.ColumnInfos);
                fields = merge.Fields;
                lostKeys = merge.Lost;
                var newlySkipped = PacketSqlService.ApplyFieldEdits(fields, command.Fields);
                await _packetSqlService.EnsureSkippedKeysNotInUse(packet, null, newlySkipped);
            }
            else
            {
                // Xoá câu SQL thì mọi trường đang có đều mất: giữ lại và đánh Lost để không mất phần người dùng đã khai
                lostKeys = currentFields.Where(f => !f.Lost).Select(f => f.Key).ToList();
                fields = SqlFieldHelper.Read(commonSql?.FieldsJson);
                fields.ForEach(f => f.Lost = true);
            }

            // 4. Câu SQL mới làm mất khoá đang được hồ sơ ánh xạ dùng, hoặc đổi công thức cột mốc khi đối tác đang có mốc:
            //    chưa lưu, trả về để người dùng xác nhận
            var cursorResets = await _packetSqlService.FindCursorResets(packet, null, commonSql?.ExtractSql, command.ExtractSql);
            if (!command.Force)
            {
                var affected = await _packetSqlService.FindMappingUsage(packet, null, lostKeys);
                if (affected.Count > 0 || cursorResets.Count > 0)
                {
                    var pending = PacketSqlService.BuildExtractOutput(packet, commonSql, currentFields);
                    pending.RequiresConfirm = true;
                    pending.Lost = affected.SelectMany(a => a.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    pending.AffectedMappings = affected;
                    pending.CursorResetPartners = cursorResets;
                    return pending;
                }
            }

            var columnNamesJoined = columns.Count > 0 ? string.Join(",", columns) : null;
            var fieldsJson = SqlFieldHelper.Write(fields);
            DateTime? describedTime = string.IsNullOrWhiteSpace(command.ExtractSql) ? null : BaseUtil.TimeServer.GetTimeServer();

            await db.AsTenant().BeginTranAsync();
            try
            {
                // Câu SQL mẫu chung và thiết lập SQL lưu ở ShareDataSql (dòng PartnerId trống); trạng thái, ghi chú vẫn ở gói tin
                if (commonSql == null)
                {
                    commonSql = new ShareDataSql
                    {
                        PacketId = packet.ID,
                        ExtractSql = command.ExtractSql,
                        FieldsJson = fieldsJson,
                        VariablesJson = incomingVariables == null ? null : SqlTextHelper.WriteVariables(incomingVariables),
                        DescribedTime = describedTime
                    };
                    await _sqlRep.InsertWithDiffLogAsync(commonSql);
                    await _activityLogger.LogCreateAsync(commonSql);
                }
                else
                {
                    var beforeSql = CloneForLog(commonSql);
                    commonSql.ExtractSql = command.ExtractSql;
                    commonSql.FieldsJson = fieldsJson;
                    commonSql.DescribedTime = describedTime;
                    await RefreshOtherColumnsAsync(commonSql);
                    if (incomingVariables != null) commonSql.VariablesJson = SqlTextHelper.WriteVariables(incomingVariables);
                    await _sqlRep.UpdateWithDiffLogAsync(commonSql, false);
                    await _activityLogger.LogUpdate(beforeSql!, commonSql);
                }

                var statusChanged = command.Status.HasValue && command.Status != packet.Status;
                var remarkChanged = !string.IsNullOrWhiteSpace(command.Remark) && command.Remark != packet.Remark;
                if (statusChanged || remarkChanged)
                {
                    var beforePacket = new ShareDataPacket
                    {
                        ID = packet.ID,
                        Code = packet.Code,
                        Status = packet.Status,
                        Remark = packet.Remark
                    };

                    if (statusChanged) packet.Status = command.Status;
                    if (remarkChanged) packet.Remark = command.Remark;

                    await _packetRep.UpdateWithDiffLogAsync(packet, false);
                    await _activityLogger.LogUpdate(beforePacket, packet);
                }

                // Câu mẫu chung đổi công thức cột mốc: mốc cũ của các đối tác đang dùng mẫu chung không so được với công thức mới
                foreach (var reset in cursorResets)
                {
                    await ResetCursor(reset.PartnerCode, packet);
                }

                await db.AsTenant().CommitTranAsync();
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }

            var output = PacketSqlService.BuildExtractOutput(packet, commonSql, fields);
            output.ColumnNames = columnNamesJoined;
            output.Lost = lostKeys;
            output.UpdateTime = BaseUtil.TimeServer.GetTimeServer();
            return output;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lưu hoặc cập nhật câu lệnh SQL trích xuất riêng của đối tác cho một gói tin.
        ///              Không phụ thuộc hồ sơ ánh xạ: soạn trước được, hồ sơ ánh xạ chỉ cần lúc gửi.
        ///              Câu nối đuôi có cột mốc chỉ là ngày giờ hoặc dùng UpdateTime trơn thì từ chối (ValidateCursorTime).
        /// Created date: 24/10/2026
        /// Modified date: 05/11/2026
        /// </summary>
        public async Task<PartnerExtractSqlOutput> HandleAsync(UpdatePartnerExtractSqlInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlEdit);
            }

            var (partner, packet) = await _packetSqlService.ResolvePartnerAndPacket(command.PartnerId, command.PacketCode);
            await EnsureNoActiveSubscriptionAsync(packet, partner.ID, BaseEnums.Direction.Outbound);
            var partnerSql = await SqlTextHelper.SelectPartnerSql(_db, partner.ID, packet);
            var before = CloneForLog(partnerSql);
            var wasCustom = SqlTextHelper.IsCustom(partnerSql);
            var commonSql = await SqlTextHelper.SelectCommonSql(_db, packet.ID);
            var commonFields = SqlFieldHelper.Read(commonSql?.FieldsJson);

            var incomingSql = command.ExtractSql?.Trim();
            if (string.IsNullOrWhiteSpace(incomingSql))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PartnerExtractSqlRequired);
            }

            // Biến đi theo câu SQL: bộ biến gửi kèm thì kiểm theo bộ đó và ghi cùng câu; không gửi thì kiểm theo bộ biến của SQL riêng,
            // lần đầu tạo SQL riêng (chưa có bộ biến) thì theo bộ biến mẫu chung sẽ được chép sang
            var incomingVariables = command.Variables == null ? null : NormalizeOrdered(command.Variables);
            var variables = SqlVariableSet.From(incomingVariables ?? await SqlTextHelper.LoadEditingVariableRows(_db, packet.ID, partner.ID));
            var sqlValidation = await _packetSqlService.ValidateSql(_db, incomingSql, variables);
            if (!sqlValidation.IsValid)
            {
                var errorMsg = sqlValidation.ErrorMessage;
                await _activityLogger.LogActionAsync(
                    BaseEnums.ActivityAction.Reject,
                    partner,
                    description: $"Từ chối lưu câu truy vấn SQL riêng không an toàn của đối tác [{partner.Name}] (gói [{packet.Code}]): {errorMsg}",
                    reason: errorMsg);

                throw SqlValidatorHelper.ToException(sqlValidation.ErrorKey, sqlValidation.ErrorArgs);
            }

            var cursorUse = SqlValidatorHelper.ValidateCursorColumnUsed(incomingSql, sqlValidation.Columns, variables);
            if (!cursorUse.IsValid)
            {
                throw SqlValidatorHelper.ToException(cursorUse.ErrorKey, cursorUse.ErrorArgs);
            }

            if (SqlValidatorHelper.IsIncrementalSql(incomingSql, variables.CursorNames))
            {
                var incValidation = SqlValidatorHelper.ValidateIncrementalStructure(
                    incomingSql,
                    sqlValidation.Columns,
                    variables
                );
                if (!incValidation.IsValid)
                {
                    throw SqlValidatorHelper.ToException(incValidation.ErrorKey, incValidation.ErrorArgs);
                }

                var timeValidation = SqlValidatorHelper.ValidateCursorTime(incomingSql, sqlValidation.ColumnInfos, variables);
                if (!timeValidation.IsValid)
                {
                    throw SqlValidatorHelper.ToException(timeValidation.ErrorKey, timeValidation.ErrorArgs);
                }
            }

            // Danh sách trường của câu SQL riêng: đối chiếu với trường riêng đang lưu nếu đã có câu riêng;
            // lần đầu tạo mới (clone độc lập) thì chỉ lấy theo cột của câu SQL mới và kế thừa nhãn/bắt buộc/nhóm từ mẫu chung
            var currentFields = wasCustom
                ? SqlFieldHelper.Read(partnerSql!.FieldsJson)
                : [];
            var merge = SqlFieldHelper.Merge(currentFields, sqlValidation.ColumnInfos, commonFields);
            var newlySkipped = PacketSqlService.ApplyFieldEdits(merge.Fields, command.Fields);
            await _packetSqlService.EnsureSkippedKeysNotInUse(packet, partner.ID, newlySkipped);

            // Mốc của đối tác ghi theo câu đang chạy (câu riêng, hoặc câu mẫu chung khi mới tách): câu mới đổi công thức cột mốc thì phải đặt lại
            var cursorResets = await _packetSqlService.FindCursorResets(
                packet,
                partner.ID,
                wasCustom ? partnerSql!.ExtractSql : commonSql?.ExtractSql,
                incomingSql
            );
            if (!command.Force)
            {
                List<SqlFieldMappingUsage> affected = wasCustom ? await _packetSqlService.FindMappingUsage(packet, partner.ID, merge.Lost) : [];
                if (affected.Count > 0 || cursorResets.Count > 0)
                {
                    var pending = BuildPartnerSqlOutput(partner, packet, commonSql, partnerSql);
                    pending.RequiresConfirm = true;
                    pending.Lost = affected.SelectMany(a => a.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    pending.AffectedMappings = affected;
                    pending.CursorResetPartners = cursorResets;
                    return pending;
                }
            }

            var fieldsJson = SqlFieldHelper.Write(merge.Fields);
            var describedTime = BaseUtil.TimeServer.GetTimeServer();

            // Cho phép tạo SQL riêng giống hệt mẫu chung (bản đóng băng theo mẫu tại thời điểm tách).
            // Muốn quay về dùng mẫu chung thì dùng thao tác Đặt lại.
            if (partnerSql == null)
            {
                partnerSql = new ShareDataSql
                {
                    PartnerId = partner.ID,
                    PacketId = packet.ID,
                    ExtractSql = incomingSql,
                    FieldsJson = fieldsJson,
                    VariablesJson = incomingVariables == null ? null : SqlTextHelper.WriteVariables(incomingVariables),
                    DescribedTime = describedTime
                };
                await _sqlRep.InsertWithDiffLogAsync(partnerSql);
                await _activityLogger.LogCreateAsync(partnerSql);
            }
            else
            {
                partnerSql.ExtractSql = incomingSql;
                partnerSql.FieldsJson = fieldsJson;
                partnerSql.DescribedTime = describedTime;
                await RefreshOtherColumnsAsync(partnerSql);
                if (incomingVariables != null) partnerSql.VariablesJson = SqlTextHelper.WriteVariables(incomingVariables);
                await _sqlRep.UpdateWithDiffLogAsync(partnerSql, false);
                await _activityLogger.LogUpdate(before!, partnerSql);
            }

            // Tạo SQL riêng lần đầu là bản chép của mẫu chung: chép luôn bộ biến (đã có bộ biến riêng thì giữ nguyên).
            // Đã có SQL riêng thì không chép lại, kể cả khi đối tác đã xóa hết biến.
            if (!wasCustom)
            {
                await SqlTextHelper.CopyCommonVariablesToPartner(_db, packet.ID, partner.ID);
            }

            foreach (var reset in cursorResets)
            {
                await ResetCursor(reset.PartnerCode, packet);
            }

            var output = BuildPartnerSqlOutput(partner, packet, commonSql, partnerSql);
            output.Lost = merge.Lost;
            return output;
        }

        /// <summary>
        /// Đặt lại câu lệnh SQL trích xuất riêng của đối tác về theo câu trích xuất chung của gói tin.
        /// Dòng riêng không còn thiết lập nào khác thì xóa; còn thì chỉ xóa câu SQL riêng.
        /// Modified date: 03/11/2026
        /// </summary>
        public async Task<PartnerExtractSqlOutput> HandleAsync(ResetPartnerExtractSqlInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlEdit);
            }

            var (partner, packet) = await _packetSqlService.ResolvePartnerAndPacket(command.PartnerId, command.PacketCode);
            await EnsureNoActiveSubscriptionAsync(packet, partner.ID, BaseEnums.Direction.Outbound);
            var partnerSql = await SqlTextHelper.SelectPartnerSql(_db, partner.ID, packet);
            var commonSql = await SqlTextHelper.SelectCommonSql(_db, packet.ID);

            // Quay về câu mẫu chung khác công thức cột mốc: mốc đang chạy theo câu riêng phải đặt lại
            List<CursorResetPartner> cursorResets = SqlTextHelper.IsCustom(partnerSql)
                ? await _packetSqlService.FindCursorResets(packet, partner.ID, partnerSql!.ExtractSql, commonSql?.ExtractSql)
                : [];

            // Khoá chỉ câu SQL riêng có sẽ mất khi quay về câu mẫu chung: hồ sơ ánh xạ của đối tác đang dùng thì hỏi xác nhận trước
            if (!command.Force && SqlTextHelper.IsCustom(partnerSql))
            {
                var commonKeys = SqlFieldHelper.Read(commonSql?.FieldsJson)
                    .Where(f => !f.Lost)
                    .Select(f => f.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var lostKeys = SqlFieldHelper.Read(partnerSql!.FieldsJson)
                    .Where(f => !f.Lost && !commonKeys.Contains(f.Key))
                    .Select(f => f.Key)
                    .ToList();
                var affected = await _packetSqlService.FindMappingUsage(packet, partner.ID, lostKeys);
                if (affected.Count > 0 || cursorResets.Count > 0)
                {
                    var pending = BuildPartnerSqlOutput(partner, packet, commonSql, partnerSql);
                    pending.RequiresConfirm = true;
                    pending.Lost = affected.Count > 0 ? lostKeys : [];
                    pending.AffectedMappings = affected;
                    pending.CursorResetPartners = cursorResets;
                    return pending;
                }
            }

            if (partnerSql != null)
            {
                var before = CloneForLog(partnerSql);
                await RefreshOtherColumnsAsync(partnerSql);
                // Dòng còn giữ câu ghi chiều nhận riêng thì chỉ xoá phần SQL trích xuất.
                if (string.IsNullOrWhiteSpace(partnerSql.WriteSql))
                {
                    await _db.Deleteable<ShareDataSql>().Where(x => x.ID == partnerSql.ID).ExecuteCommandAsync();
                    await _activityLogger.LogDeleteAsync(before!);
                    partnerSql = null;
                }
                else
                {
                    partnerSql.ExtractSql = null;
                    partnerSql.FieldsJson = null;
                    partnerSql.DescribedTime = null;
                    await RefreshOtherColumnsAsync(partnerSql);
                    await _sqlRep.UpdateWithDiffLogAsync(partnerSql, false);
                    await _activityLogger.LogUpdate(before!, partnerSql);
                }
            }

            // Dùng lại mẫu chung: bỏ luôn bộ biến của SQL riêng
            await SqlTextHelper.DeletePartnerVariables(_db, packet.ID, partner.ID);

            foreach (var reset in cursorResets)
            {
                await ResetCursor(reset.PartnerCode, packet);
            }

            return BuildPartnerSqlOutput(partner, packet, commonSql, partnerSql);
        }

        /// <summary>
        /// Dòng ShareDataSql chứa cả bộ biến và khối câu ghi chiều nhận, lưu ở màn khác: đọc lại các cột này ngay trước khi ghi cả dòng
        /// để không đè mất bộ biến / câu ghi vừa lưu trong lúc đang kiểm câu SQL.
        /// </summary>
        private async Task RefreshOtherColumnsAsync(ShareDataSql row)
        {
            var latest = await _db.Queryable<ShareDataSql>()
                .Where(s => s.ID == row.ID)
                .Select(s => new { s.VariablesJson, s.WriteSql, s.AffectedTables, s.WriteRemark })
                .FirstAsync();
            if (latest == null) return;
            row.VariablesJson = latest.VariablesJson;
            row.WriteSql = latest.WriteSql;
            row.AffectedTables = latest.AffectedTables;
            row.WriteRemark = latest.WriteRemark;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm câu SQL theo bộ biến như khi lưu câu (an toàn, biến đã khai, cột mốc có biến mốc, cấu trúc nối đuôi); không hợp lệ thì ném lỗi theo mã của trình kiểm.
        /// Created date: 01/11/2026
        /// Modified date: 05/11/2026
        /// </summary>
        private async Task EnsureSqlFitsVariables(string sql, SqlVariableSet variables)
        {
            var sqlValidation = await _packetSqlService.ValidateSql(_db, sql, variables);
            if (!sqlValidation.IsValid)
            {
                throw SqlValidatorHelper.ToException(sqlValidation.ErrorKey, sqlValidation.ErrorArgs);
            }

            var cursorUse = SqlValidatorHelper.ValidateCursorColumnUsed(sql, sqlValidation.Columns, variables);
            if (!cursorUse.IsValid)
            {
                throw SqlValidatorHelper.ToException(cursorUse.ErrorKey, cursorUse.ErrorArgs);
            }

            if (SqlValidatorHelper.IsIncrementalSql(sql, variables.CursorNames))
            {
                var incValidation = SqlValidatorHelper.ValidateIncrementalStructure(
                    sql,
                    sqlValidation.Columns,
                    variables
                );
                if (!incValidation.IsValid)
                {
                    throw SqlValidatorHelper.ToException(incValidation.ErrorKey, incValidation.ErrorArgs);
                }
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chép dòng ShareDataSql trước khi sửa để ghi nhật ký thay đổi; nguồn null trả null.
        /// Created date: 24/10/2026
        /// Modified date: 03/11/2026
        /// </summary>
        private static ShareDataSql? CloneForLog(ShareDataSql? source) => source == null
            ? null
            : new ShareDataSql
            {
                ID = source.ID,
                PartnerId = source.PartnerId,
                PacketId = source.PacketId,
                ExtractSql = source.ExtractSql,
                VariablesJson = source.VariablesJson,
                FieldsJson = source.FieldsJson,
                DescribedTime = source.DescribedTime
            };

        /// <summary>
        /// Author: HieuNV
        /// Description: Dựng kết quả câu SQL của đối tác, kèm câu và danh sách trường của mẫu chung để đối chiếu.
        /// Created date: 26/10/2026
        /// </summary>
        private static PartnerExtractSqlOutput BuildPartnerSqlOutput(ShareDataPartner partner, ShareDataPacket packet, ShareDataSql? commonSql, ShareDataSql? partnerSql)
        {
            var isCustom = SqlTextHelper.IsCustom(partnerSql);
            return new PartnerExtractSqlOutput
            {
                PartnerId = partner.ID ?? string.Empty,
                PartnerCode = partner.Code ?? string.Empty,
                PartnerName = partner.Name ?? string.Empty,
                PacketCode = packet.Code ?? string.Empty,
                StandardSql = commonSql?.ExtractSql ?? string.Empty,
                CustomSql = partnerSql?.ExtractSql,
                IsCustom = isCustom,
                HasDifferences = SqlTextHelper.HasDifferences(partnerSql, commonSql?.ExtractSql),
                Fields = isCustom ? SqlFieldHelper.Read(partnerSql!.FieldsJson) : [],
                CommonFields = SqlFieldHelper.Read(commonSql?.FieldsJson),
                DescribedTime = isCustom ? partnerSql!.DescribedTime : null,
            };
        }

        #endregion

        #region Tham số câu truy vấn (VariablesJson) và mốc đã gửi
        /// <summary>
        /// Author: DatHQ
        /// Description: Lưu bảng tham số của câu SQL (mẫu chung hoặc riêng đối tác) vào ShareDataSql.VariablesJson. Chỉ SuperAdmin; còn đăng ký gửi đang bật thì từ chối (ESH-2306).
        ///              Chỉ kiểm lại câu SQL khi đổi tên, nguồn hoặc kiểu biến (SameVariableShape, EnsureSqlFitsVariables); đổi giá trị thì bỏ qua để lưu nhanh.
        /// Created date: 26/10/2026
        /// </summary>
        public async Task<PacketSqlVariablesOutput> HandleAsync(UpdatePacketSqlVariablesInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlEdit);
            }

            var packetCode = command.PacketCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(packetCode))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PacketCodeRequired);
            }

            var packet = await _db.Queryable<ShareDataPacket>()
                .Where(x => x.IsDelete == null && x.Code == packetCode)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNotFound, packetCode);

            ShareDataPartner? partner = null;
            string? partnerId = null;
            if (!string.IsNullOrWhiteSpace(command.PartnerId))
            {
                var requestedPartnerId = command.PartnerId.Trim();
                partner = await _db.Queryable<ShareDataPartner>()
                    .Where(p => p.IsDelete == null && p.ID == requestedPartnerId)
                    .FirstAsync()
                    ?? throw Oops.Oh(BaseMsg.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.ShareData.Entity.PartnerId]);
                partnerId = partner.ID;
            }

            await EnsureNoActiveSubscriptionAsync(packet, partnerId, BaseEnums.Direction.Outbound);

            // Biến đi theo câu SQL: partnerId trống = bộ biến mẫu chung, có partnerId = bộ biến của SQL riêng đối tác
            var normalized = NormalizeOrdered(command.Items);
            var variablesJson = SqlTextHelper.WriteVariables(normalized);

            // Bộ biến nằm ở cột VariablesJson của dòng ShareDataSql cùng phạm vi; chưa có dòng thì tạo dòng chỉ chứa bộ biến
            var row = partnerId == null
                ? await SqlTextHelper.SelectCommonSql(_db, packet.ID)
                : await SqlTextHelper.SelectPartnerSql(_db, partnerId, packet);

            // Câu SQL đang lưu của phạm vi này phải còn hợp lệ với bộ biến mới (vd. đổi nguồn @lastKey sang theo thời gian
            // làm câu nối đuôi mất khóa): kiểm như khi lưu câu SQL, không hợp lệ thì không lưu bộ biến.
            // Chỉ đổi giá trị, ghi chú hoặc mốc khởi tạo (tên, nguồn, kiểu biến giữ nguyên) thì kết quả kiểm không đổi nên bỏ qua.
            if (!string.IsNullOrWhiteSpace(row?.ExtractSql) && !SameVariableShape(row.VariablesJson, normalized))
            {
                await EnsureSqlFitsVariables(row.ExtractSql, SqlVariableSet.From(normalized));
            }

            var db = _db;
            await db.AsTenant().BeginTranAsync();
            try
            {
                if (row == null)
                {
                    row = new ShareDataSql
                    {
                        PacketId = packet.ID,
                        PartnerId = partnerId,
                        VariablesJson = variablesJson
                    };
                    await _sqlRep.InsertWithDiffLogAsync(row);
                    await _activityLogger.LogCreateAsync(row);
                }
                else if (row.VariablesJson != variablesJson)
                {
                    var before = new ShareDataSql { ID = row.ID, PacketId = row.PacketId, PartnerId = row.PartnerId, VariablesJson = row.VariablesJson };
                    row.VariablesJson = variablesJson;
                    await _sqlRep.UpdateWithDiffLogAsync(row, false);
                    await _activityLogger.LogUpdate(before, row);
                }

                await db.AsTenant().CommitTranAsync();
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }

            return new PacketSqlVariablesOutput
            {
                PacketCode = packet.Code ?? packetCode,
                PartnerId = partnerId,
                Items = await _packetSqlService.LoadItems(packet.ID, partnerId, normalized, packet.Code, partner?.Code)
            };
        }

        /// <summary>
        /// Bộ biến mới có cùng tên, nguồn và kiểu với bộ đang lưu không (bỏ qua giá trị nhập, ghi chú, mốc khởi tạo, thứ tự):
        /// giống nhau thì kiểm câu SQL theo bộ biến mới cho cùng kết quả với bộ đang lưu.
        /// </summary>
        private static bool SameVariableShape(string? savedJson, IReadOnlyCollection<SqlVariableItem> items)
        {
            if (savedJson == null)
            {
                return false;
            }

            // Phần ảnh hưởng tới kết quả kiểm câu SQL: tên, loại, kiểu; nguồn chỉ tính với biến lấy từ hệ thống
            static string Shape(SqlVariableItem v) =>
                $"{v.Code?.Trim().ToLowerInvariant()}|{v.ValueKind}|{v.DataType}|{(v.ValueKind == ShareDataConst.SqlVariableKindBuiltin ? v.Value : null)}";

            var saved = SqlTextHelper.ReadVariables(savedJson).Select(Shape).Order(StringComparer.Ordinal);
            return saved.SequenceEqual(items.Select(Shape).Order(StringComparer.Ordinal));
        }

        /// <summary>
        /// Chuẩn hóa bộ biến gửi lên (Normalize) rồi đánh số thứ tự theo vị trí: dùng chung cho lưu bộ biến riêng và bộ biến gửi kèm câu SQL.
        /// </summary>
        private static List<SqlVariableItem> NormalizeOrdered(List<PacketSqlVariableItemInput>? items)
        {
            var normalized = Normalize(items);
            for (var i = 0; i < normalized.Count; i++)
            {
                normalized[i].OrderNo = i + 1;
            }
            return normalized;
        }

        /// <summary>
        /// Kiểm tra và chuẩn hóa các dòng gửi lên: tên hợp lệ và không trùng, hỗ trợ nguồn hệ thống (Builtin) hoặc giá trị nhập (Const).
        /// Mốc bắt đầu (DefaultValue của biến mốc) phải đúng dạng công thức PA1 với ngày giờ có thật như Đặt mốc.
        /// Modified date: 17/11/2026
        /// </summary>
        private static List<SqlVariableItem> Normalize(List<PacketSqlVariableItemInput>? items)
        {
            var result = new List<SqlVariableItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items ?? [])
            {
                var code = item.Code?.Trim().TrimStart('@') ?? string.Empty;
                if (!CodePattern.IsMatch(code))
                {
                    throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableCodeInvalid, code);
                }
                if (!seen.Add(code))
                {
                    throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableCodeDuplicate, code);
                }

                var valueKind = string.IsNullOrWhiteSpace(item.ValueKind)
                    ? ShareDataConst.SqlVariableKindBuiltin
                    : MatchOption(item.ValueKind, [ShareDataConst.SqlVariableKindBuiltin, ShareDataConst.SqlVariableKindConst])
                        ?? throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableSourceInvalid, code, item.ValueKind);

                string dataType;
                string value;
                string? defaultValue = null;
                if (valueKind == ShareDataConst.SqlVariableKindConst)
                {
                    value = item.Value?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(item.DataType))
                    {
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                        {
                            dataType = ShareDataConst.SqlVariableTypeInt;
                        }
                        else if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                        {
                            dataType = ShareDataConst.SqlVariableTypeDecimal;
                        }
                        else
                        {
                            dataType = ShareDataConst.SqlVariableTypeString;
                        }
                    }
                    else
                    {
                        dataType = MatchOption(item.DataType, ShareDataConst.SqlVariableConstTypes)
                            ?? throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableSourceInvalid, code, item.DataType ?? string.Empty);
                    }

                    if (string.IsNullOrEmpty(value) || value.Length > EntityConst.Length256)
                    {
                        throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableValueInvalid, code, dataType);
                    }
                    if (dataType == ShareDataConst.SqlVariableTypeInt &&
                        !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    {
                        throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableValueInvalid, code, dataType);
                    }
                    if (dataType == ShareDataConst.SqlVariableTypeDecimal &&
                        !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                    {
                        throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableValueInvalid, code, dataType);
                    }
                }
                else
                {
                    value = MatchOption(item.Value, ShareDataConst.SqlVariableBuiltinTypes.Keys)
                        ?? throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableSourceInvalid, code, item.Value ?? string.Empty);
                    dataType = ShareDataConst.SqlVariableBuiltinTypes[value];
                    if (string.Equals(value, ShareDataConst.SqlVariableSourceLastKey, StringComparison.OrdinalIgnoreCase))
                    {
                        var trimmedDefault = item.DefaultValue?.Trim();
                        if (!string.IsNullOrEmpty(trimmedDefault))
                        {
                            // Mốc bắt đầu so chuỗi với cột mốc như mốc đang chạy: ngày giờ viết liền 8 tới 17 số, hoặc đủ 17 số kèm _<đuôi> theo dạng PA1
                            if (trimmedDefault.Length > EntityConst.Length256 || !IsCursorTimeKey(trimmedDefault))
                            {
                                throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableValueInvalid, code, dataType);
                            }
                            defaultValue = trimmedDefault;
                        }
                    }
                }

                if (string.Equals(code, ShareDataConst.SqlVariablePageSize, StringComparison.OrdinalIgnoreCase))
                {
                    if (valueKind != ShareDataConst.SqlVariableKindConst || dataType != ShareDataConst.SqlVariableTypeInt)
                    {
                        throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableSourceInvalid, code, item.Value ?? item.ValueKind);
                    }
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ps) && ps <= 0)
                    {
                        throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableValueInvalid, code, dataType);
                    }
                }

                result.Add(new SqlVariableItem
                {
                    Code = code,
                    Name = string.IsNullOrWhiteSpace(item.Name) ? null : item.Name.Trim(),
                    DataType = dataType,
                    ValueKind = valueKind,
                    Value = value,
                    DefaultValue = defaultValue,
                    Remark = string.IsNullOrWhiteSpace(item.Remark) ? null : item.Remark.Trim()
                });
            }
            return result;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đặt lại mốc gửi của một đối tác theo gói tin (nút Đặt lại). Chỉ SuperAdmin; phải tắt đăng ký gửi trước (ESH-2306)
        ///              vì Worker đang chạy sẽ ghi lại mốc ngay sau khi bị xóa. Lượt gửi sau quét lại từ mốc khởi tạo.
        /// Created date: 27/10/2026
        /// </summary>
        public async Task HandleAsync(ResetLastSendInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlEdit);
            }

            var packetCode = command.PacketCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(packetCode))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PacketCodeRequired);
            }

            var packet = await _db.Queryable<ShareDataPacket>()
                .Where(x => x.IsDelete == null && x.Code == packetCode)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNotFound, packetCode);

            var requestedPartnerId = command.PartnerId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestedPartnerId))
            {
                throw Oops.Oh(BaseMsg.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.ShareData.Entity.PartnerId]);
            }

            var partner = await _db.Queryable<ShareDataPartner>()
                .Where(p => p.IsDelete == null && p.ID == requestedPartnerId)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.ShareData.Entity.PartnerId]);

            if (string.IsNullOrWhiteSpace(partner.Code) || string.IsNullOrWhiteSpace(packet.Code))
            {
                return;
            }

            // Worker đang chạy sẽ ghi lại mốc ngay sau khi bị xóa: phải tắt đăng ký trước khi đặt lại (ESH-2306)
            await EnsureNoActiveSubscriptionAsync(packet, partner.ID, BaseEnums.Direction.Outbound);

            await ResetCursor(partner.Code, packet);
        }

        /// <summary>
        /// Đặt mốc nối đuôi của gói tin cho một đối tác (chỉ SuperAdmin): đối tác chỉ muốn nhận dữ liệu từ một thời điểm thì đặt mốc
        /// thay vì tách cấu hình riêng chỉ để đổi mốc khởi tạo. Ghi kèm chữ ký công thức cột mốc của câu SQL đối tác đang chạy
        /// để Worker coi là mốc hợp lệ. Mốc trống bị từ chối: bỏ mốc thì dùng Đặt lại.
        /// Mốc phải đúng dạng công thức PA1 với ngày giờ có thật: viết liền 8 tới 17 số (như 20260101), hoặc đủ 17 số kèm _&lt;đuôi&gt; (đuôi được để trống), sai dạng bị từ chối.
        /// Modified date: 17/11/2026
        /// </summary>
        public async Task HandleAsync(SetLastSendInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlEdit);
            }

            if (string.IsNullOrWhiteSpace(command.LastKey))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableValueInvalid, ShareDataConst.SqlVariableSourceLastKey, ShareDataConst.SqlVariableTypeString);
            }

            var lastKey = command.LastKey.Trim();

            // Mốc sai dạng công thức PA1 thì phép so chuỗi lệch thứ tự thời gian, lượt gửi bỏ sót hoặc gửi lại dữ liệu
            if (!IsCursorTimeKey(lastKey))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.SqlVariableValueInvalid, ShareDataConst.SqlVariableSourceLastKey, ShareDataConst.SqlVariableTypeString);
            }

            var (partner, packet) = await _packetSqlService.ResolvePartnerAndPacket(command.PartnerId, command.PacketCode);

            // Khóa như Đặt lại: phải tắt đăng ký trước khi đặt mốc (ESH-2306)
            await EnsureNoActiveSubscriptionAsync(packet, partner.ID, BaseEnums.Direction.Outbound);

            var sqlConfig = await SqlTextHelper.ResolveSqlConfig(_db, packet, partner.ID);

            string? cursorKey = null;
            string? signature;
            if (!string.IsNullOrWhiteSpace(command.CursorKey))
            {
                var variables = await SqlTextHelper.ResolveVariables(_db, packet.ID, partner.ID);
                var group = variables.CursorGroups.FirstOrDefault(g => string.Equals(g, command.CursorKey.Trim(), StringComparison.OrdinalIgnoreCase))
                    ?? throw Oops.Oh(BaseMsg.ShareData.Validation.SqlIncrementalGroupNotFound, command.CursorKey.Trim());
                cursorKey = group;
                signature = SqlValidatorHelper.GetCursorSignature(sqlConfig.ExtractSql, group);
            }
            else
            {
                signature = SqlValidatorHelper.GetCursorSignature(sqlConfig.ExtractSql);
            }

            var row = await _db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packet.Code)
                .WhereIF(cursorKey == null, ls => ls.CursorKey == null)
                .WhereIF(cursorKey != null, ls => ls.CursorKey == cursorKey)
                .FirstAsync();
            if (row == null)
            {
                row = new ShareDataLastSend
                {
                    PartnerCode = partner.Code,
                    PacketCode = packet.Code,
                    CursorKey = cursorKey,
                    LastKey = lastKey,
                    CursorSignature = signature
                };
                await _db.Insertable(row).ExecuteCommandAsync();
                await _activityLogger.LogCreateAsync(row);
            }
            else
            {
                var before = new ShareDataLastSend
                {
                    ID = row.ID,
                    PartnerCode = row.PartnerCode,
                    PacketCode = row.PacketCode,
                    CursorKey = row.CursorKey,
                    LastTime = row.LastTime,
                    LastKey = row.LastKey,
                    CursorSignature = row.CursorSignature
                };
                row.LastTime = null;
                row.LastKey = lastKey;
                row.CursorSignature = signature;
                await _db.Updateable(row).ExecuteCommandAsync();
                await _activityLogger.LogUpdate(before, row);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Mốc đúng dạng công thức PA1: ngày giờ có thật viết liền từ 8 chữ số yyyyMMdd tới đủ 17 chữ số yyyyMMddHHmmssfff;
        /// muốn ghép dấu _ và phần đuôi (được để trống) thì phần ngày giờ phải đủ 17 chữ số.
        /// Created date: 16/11/2026
        /// Modified date: 17/11/2026
        /// </summary>
        private static bool IsCursorTimeKey(string lastKey)
        {
            if (!CursorTimeKeyPattern.IsMatch(lastKey))
            {
                return false;
            }

            // Mốc chỉ có ngày giờ ngắn (8 tới 16 số) thì đệm 0 cho đủ 17 số rồi kiểm ngày giờ có thật
            var timePart = lastKey.Length < CursorTimeFormat.Length
                ? lastKey.PadRight(CursorTimeFormat.Length, '0')
                : lastKey[..CursorTimeFormat.Length];
            return DateTime.TryParseExact(timePart, CursorTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        /// <summary>
        /// Xóa mốc nối đuôi (ShareDataLastSend) của đối tác cho gói tin: lượt gửi kế tiếp quét lại từ mốc khởi tạo.
        /// Dùng cho nút Đặt lại và khi lưu câu SQL đổi công thức cột mốc.
        /// </summary>
        private async Task ResetCursor(string? partnerCode, ShareDataPacket packet)
        {
            var rows = await _db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PartnerCode == partnerCode && ls.PacketCode == packet.Code)
                .ToListAsync();

            foreach (var row in rows)
            {
                await _db.Deleteable<ShareDataLastSend>().Where(x => x.ID == row.ID).ExecuteCommandAsync();
                await _activityLogger.LogDeleteAsync(row);
            }
        }

        /// <summary>Trả về đúng chữ của lựa chọn khớp (không phân biệt hoa thường), không khớp thì null.</summary>
        private static string? MatchOption(string? input, IEnumerable<string> options)
        {
            var text = input?.Trim();
            return string.IsNullOrEmpty(text) ? null : options.FirstOrDefault(o => string.Equals(o, text, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Khối câu ghi chiều nhận (WriteSql)
        /// <summary>
        /// Author: DatHQ
        /// Description: Lưu câu ghi mẫu chung của chiều nhận. Chỉ SuperAdmin; còn đăng ký nhận đang bật thì từ chối.
        /// Created date: 27/10/2026
        /// </summary>
        public async Task<PacketWriteSqlsOutput> HandleAsync(UpdatePacketWriteSqlsInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlEdit);
            }

            var packet = await _writeSqlService.FindPacket(command.PacketCode);
            // Câu ghi mẫu chung: còn đăng ký nhận nào của gói đang bật thì phải tắt trước
            await EnsureNoActiveSubscriptionAsync(packet, null, BaseEnums.Direction.Inbound);
            var writeSql = command.WriteSql?.Trim();
            if (string.IsNullOrWhiteSpace(writeSql))
            {
                // Mẫu chung trống = gói chỉ gửi
                await SaveWriteColumnsAsync(packet, null, null, null, null);
                return await _writeSqlService.LoadOutput(packet);
            }

            var affectedCsv = await ValidateWriteSqlForSaveAsync(packet, null, writeSql, command.AffectedTables);
            var remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();

            await SaveWriteColumnsAsync(packet, null, writeSql, affectedCsv, remark);
            return await _writeSqlService.LoadOutput(packet);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật câu ghi chiều nhận riêng của đối tác (giống UpdatePartnerExtractSql):
        ///              câu ghi rỗng bị từ chối — muốn dùng lại mẫu chung thì gọi ResetPartnerWriteSql.
        /// Created date: 30/10/2026
        /// </summary>
        public async Task<PartnerWriteSqlOutput> HandleAsync(UpdatePartnerWriteSqlInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlEdit);
            }

            var (partner, packet) = await _packetSqlService.ResolvePartnerAndPacket(command.PartnerId, command.PacketCode);
            // Câu ghi riêng chỉ ảnh hưởng đăng ký nhận của đúng đối tác này
            await EnsureNoActiveSubscriptionAsync(packet, partner.ID, BaseEnums.Direction.Inbound);

            var writeSql = command.WriteSql?.Trim();
            if (string.IsNullOrWhiteSpace(writeSql))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PartnerWriteSqlRequired);
            }

            var affectedCsv = await ValidateWriteSqlForSaveAsync(packet, partner.ID, writeSql, command.AffectedTables);
            var remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();

            await SaveWriteColumnsAsync(packet, partner.ID, writeSql, affectedCsv, remark);
            return await _writeSqlService.LoadPartnerOutput(packet, partner);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đặt lại câu ghi chiều nhận của đối tác về câu ghi mẫu chung (giống ResetPartnerExtractSql): xoá câu ghi riêng.
        /// Created date: 30/10/2026
        /// </summary>
        public async Task<PartnerWriteSqlOutput> HandleAsync(ResetPartnerWriteSqlInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlEdit);
            }

            var (partner, packet) = await _packetSqlService.ResolvePartnerAndPacket(command.PartnerId, command.PacketCode);
            await EnsureNoActiveSubscriptionAsync(packet, partner.ID, BaseEnums.Direction.Inbound);

            await SaveWriteColumnsAsync(packet, partner.ID, null, null, null);
            return await _writeSqlService.LoadPartnerOutput(packet, partner);
        }

        /// <summary>
        /// Kiểm an toàn câu ghi trước khi lưu (theo danh sách trường của đối tác nếu có partnerId); có lỗi thì ném.
        /// Trả danh sách bảng bị ảnh hưởng (tự dò + khai thêm) dạng chuỗi phân tách dấu phẩy để lưu.
        /// </summary>
        private async Task<string> ValidateWriteSqlForSaveAsync(ShareDataPacket packet, string? partnerId, string writeSql, List<string>? tables)
        {
            var affectedTables = (tables ?? [])
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .DistinctBy(WriteSqlAnalyzer.NormalizeTable, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var check = await _writeSqlService.Validate(packet, partnerId, affectedTables, writeSql);
            var firstError = check.Checks.FirstOrDefault(c => c.Level == ShareDataConst.WriteSqlCheckError);
            if (firstError != null)
            {
                throw Oops.Oh(
                    BaseMsg.ShareData.Validation.WriteSqlItemInvalid,
                    1,
                    string.Join(", ", affectedTables),
                    firstError.Message
                );
            }

            var mergedTables = check.DetectedTables
                .Concat(affectedTables)
                .DistinctBy(WriteSqlAnalyzer.NormalizeTable, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return string.Join(",", mergedTables);
        }

        /// <summary>
        /// Ghi các cột câu ghi chiều nhận vào dòng ShareDataSql của gói: partnerId trống = dòng mẫu chung, có partnerId = dòng riêng đối tác.
        /// Đọc dòng ngay trong giao dịch để giữ nguyên câu SQL gửi đang lưu cùng dòng (chỉ đổi WriteSql, AffectedTables, WriteRemark).
        /// Dòng riêng đối tác bỏ hết câu ghi mà không còn cấu hình nào khác (SQL riêng, biến) thì xoá luôn dòng.
        /// Modified date: 03/11/2026
        /// </summary>
        private async Task SaveWriteColumnsAsync(ShareDataPacket packet, string? partnerId, string? writeSql, string? affectedTables, string? remark)
        {
            await _db.AsTenant().BeginTranAsync();
            try
            {
                var row = string.IsNullOrWhiteSpace(partnerId)
                    ? await SqlTextHelper.SelectCommonSql(_db, packet.ID)
                    : await SqlTextHelper.SelectPartnerSql(_db, partnerId, packet);
                if (row == null)
                {
                    if (writeSql != null)
                    {
                        row = new ShareDataSql
                        {
                            PacketId = packet.ID,
                            PartnerId = string.IsNullOrWhiteSpace(partnerId) ? null : partnerId,
                            WriteSql = writeSql,
                            AffectedTables = affectedTables,
                            WriteRemark = remark
                        };
                        await _sqlRep.InsertWithDiffLogAsync(row);
                        await _activityLogger.LogCreateAsync(row);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(partnerId) && writeSql == null
                    && !SqlTextHelper.IsCustom(row) && row.VariablesJson == null)
                {
                    var before = Clone(row);
                    await _db.Deleteable<ShareDataSql>().Where(x => x.ID == row.ID).ExecuteCommandAsync();
                    await _activityLogger.LogDeleteAsync(before);
                }
                else if (row.WriteSql != writeSql || row.AffectedTables != affectedTables || row.WriteRemark != remark)
                {
                    var before = Clone(row);
                    row.WriteSql = writeSql;
                    row.AffectedTables = affectedTables;
                    row.WriteRemark = remark;
                    await _sqlRep.UpdateWithDiffLogAsync(row, false);
                    await _activityLogger.LogUpdate(before, row);
                }

                await _db.AsTenant().CommitTranAsync();
            }
            catch
            {
                await _db.AsTenant().RollbackTranAsync();
                throw;
            }
        }

        /// <summary>
        /// Chạy thử khối câu ghi trên CSDL nhận rồi quay lui. Không gửi WriteSql thì chạy khối câu ghi đang lưu của gói.
        /// </summary>
        public async Task<TryWriteSqlOutput> HandleAsync(TryWriteSqlInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlPreview);
            }

            var packet = await _writeSqlService.FindPacket(command.PacketCode);
            var partner = await _writeSqlService.FindPartner(command.PartnerId);
            if (!string.IsNullOrWhiteSpace(command.WriteSql))
            {
                return await _writeSqlService.TryRun(packet, partner, command.WriteSql, command.AffectedTables, command.RecordsJson);
            }

            // Không gửi câu ghi thì chạy câu ghi hiệu lực của đối tác (riêng, hoặc mẫu chung nếu đối tác chưa có riêng)
            if (partner != null)
            {
                var partnerSaved = await _writeSqlService.LoadPartnerOutput(packet, partner);
                var effectiveSql = partnerSaved.IsCustom ? partnerSaved.CustomWriteSql : partnerSaved.StandardWriteSql;
                return await _writeSqlService.TryRun(packet, partner, effectiveSql, partnerSaved.AffectedTables, command.RecordsJson);
            }

            var saved = await _writeSqlService.LoadOutput(packet);
            return await _writeSqlService.TryRun(packet, null, saved.WriteSql, saved.AffectedTables, command.RecordsJson);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chép các cột câu ghi chiều nhận trước khi sửa để ghi nhật ký thay đổi.
        /// Created date: 27/10/2026
        /// </summary>
        private static ShareDataSql Clone(ShareDataSql source) => new()
        {
            ID = source.ID,
            PacketId = source.PacketId,
            PartnerId = source.PartnerId,
            WriteSql = source.WriteSql,
            AffectedTables = source.AffectedTables,
            WriteRemark = source.WriteRemark
        };

        #endregion
    }
}
