using System.Text.RegularExpressions;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Newtonsoft.Json;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.ShareData.Core.Helpers
{
    /// <summary>
    /// Cấu hình SQL hiệu lực của một gói tin cho một đối tác: dòng mẫu chung và dòng riêng của đối tác (nếu có) trong ShareDataSql.
    /// Cột nào ở dòng đối tác để trống thì lấy theo dòng mẫu chung.
    /// </summary>
    public sealed record SqlConfig(ShareDataSql? Common, ShareDataSql? Partner)
    {
        /// <summary>Đối tác có câu SQL riêng (ExtractSql ở dòng đối tác có nội dung).</summary>
        public bool IsCustom => SqlTextHelper.IsCustom(Partner);

        /// <summary>Câu SQL dùng để chạy: câu riêng nếu có, không thì câu mẫu chung.</summary>
        public string? ExtractSql => IsCustom ? Partner!.ExtractSql!.Trim() : Common?.ExtractSql?.Trim();
    }

    /// <summary>
    /// Một biến SQL hiệu lực của gói tin × đối tác sau khi gộp: kiểu theo dòng khai báo, cách gán và giá trị theo dòng ghi đè (nếu có).
    /// </summary>
    public sealed record EffectiveSqlVariable(string Code, string DataType, string ValueKind, string? Value, string? DefaultValue = null);

    /// <summary>
    /// Tập biến SQL hiệu lực của gói tin × đối tác (mẫu chung, giá trị ghi đè của đối tác và biến riêng của đối tác).
    /// </summary>
    public sealed record SqlVariableSet(IReadOnlyList<EffectiveSqlVariable> Items)
    {
        public static SqlVariableSet Empty { get; } = new([]);

        /// <summary>
        /// Dựng tập biến từ bộ biến (VariablesJson) của một câu SQL. Dòng lấy từ nguồn hệ thống không còn hỗ trợ bị bỏ,
        /// nên câu SQL dùng biến đó bị chặn như dùng biến chưa khai (chạy thử, dò cột, Worker).
        /// </summary>
        public static SqlVariableSet From(IEnumerable<SqlVariableItem> rows) => new(rows
            .Where(v => !string.IsNullOrWhiteSpace(v.Code))
            .Where(v => (v.ValueKind ?? ShareDataConst.SqlVariableKindBuiltin) != ShareDataConst.SqlVariableKindBuiltin
                || ShareDataConst.SqlVariableBuiltinTypes.ContainsKey(v.Value ?? string.Empty))
            .Select(v => new EffectiveSqlVariable(
                v.Code!,
                v.DataType ?? ShareDataConst.SqlVariableTypeString,
                v.ValueKind ?? ShareDataConst.SqlVariableKindBuiltin,
                v.Value,
                v.DefaultValue))
            .ToList());

        /// <summary>Tên các biến đã khai (không có @).</summary>
        public IReadOnlyCollection<string> Names => Items.Select(v => v.Code).ToList();

        /// <summary>Tên các biến lấy giá trị từ mốc lần gửi trước (lastKey): câu SQL dùng biến này là truy vấn nối đuôi.</summary>
        public IReadOnlyCollection<string> CursorNames => Items
            .Where(v => v.ValueKind == ShareDataConst.SqlVariableKindBuiltin
                && string.Equals(v.Value, ShareDataConst.SqlVariableSourceLastKey, StringComparison.OrdinalIgnoreCase))
            .Select(v => v.Code)
            .ToList();

        /// <summary>Tên các biến lấy giá trị từ mốc khóa lần gửi trước (lastKey) của nhóm mốc mặc định (cột __lastKey).</summary>
        public IReadOnlyCollection<string> LastKeyNames => DefaultSourceNames(ShareDataConst.SqlVariableSourceLastKey);

        /// <summary>Biến mốc của nhóm mốc mặc định (cột __lastKey không hậu tố): câu SQL dùng biến này thì dùng nhóm mặc định.</summary>
        public IReadOnlyCollection<string> DefaultCursorNames => CursorNames.Where(code => CursorGroupOf(code) == null).ToList();

        /// <summary>
        /// Các nhóm mốc có tên của câu gộp nhiều nguồn (bảng (gói, đối tác, key) → value): mỗi nguồn khai biến @lastKey_&lt;nhóm&gt;,
        /// chiếu cột __lastKey_&lt;nhóm&gt;; mốc lưu riêng từng nhóm ở ShareDataLastSend (cột CursorKey).
        /// </summary>
        public IReadOnlyCollection<string> CursorGroups => CursorNames
            .Select(CursorGroupOf)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        /// <summary>Nhóm mốc của biến: biến tên lastKey_&lt;nhóm&gt; thuộc nhóm đó, biến khác thuộc nhóm mặc định (null).</summary>
        public static string? CursorGroupOf(string? code)
        {
            var match = CursorGroupRegex.Match(code ?? string.Empty);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static readonly Regex CursorGroupRegex = new(@"^lastKey_([A-Za-z0-9_]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Author: DatHQ
        /// Description: Tên các biến lấy theo nguồn source và thuộc nhóm mốc mặc định.
        /// Created date: 30/10/2026
        /// </summary>
        private IReadOnlyCollection<string> DefaultSourceNames(string source) => Items
            .Where(v => v.ValueKind == ShareDataConst.SqlVariableKindBuiltin
                && string.Equals(v.Value, source, StringComparison.OrdinalIgnoreCase)
                && CursorGroupOf(v.Code) == null)
            .Select(v => v.Code)
            .ToList();
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Chuẩn hóa câu lệnh SQL trích xuất, đọc cấu hình SQL (ShareDataSql) và biến SQL (ShareDataSql.VariablesJson) hiệu lực
    ///              theo gói tin × đối tác, tính cờ trạng thái đối chiếu và chọn hồ sơ ánh xạ chiều gửi đang bật.
    /// Created date: 22/10/2026
    /// Modified date: 24/10/2026 (Đọc SQL từ ShareDataSql, thêm biến @pageSize theo bảng tham số)
    /// Modified date: 27/10/2026 (Bộ biến lưu ở cột ShareDataSql.VariablesJson, bỏ bảng ShareDataSqlVariable)
    /// Modified date: 12/11/2026
    /// </summary>
    public static class SqlTextHelper
    {
        private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Chuẩn hóa chuỗi SQL để so sánh: cắt khoảng trắng hai đầu, bỏ dấu chấm phẩy cuối câu và gom khoảng trắng liên tiếp thành 1 dấu cách.
        /// </summary>
        public static string Normalize(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return string.Empty;
            }

            var trimmed = sql.Trim();
            while (trimmed.EndsWith(';'))
            {
                trimmed = trimmed[..^1].TrimEnd();
            }

            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            return WhitespaceRegex.Replace(trimmed, " ");
        }

        /// <summary>
        /// Đối tác có đang dùng câu lệnh SQL trích xuất riêng cho gói tin hay không.
        /// </summary>
        public static bool IsCustom(ShareDataSql? partnerSql)
        {
            return !string.IsNullOrWhiteSpace(partnerSql?.ExtractSql);
        }

        /// <summary>
        /// Nội dung câu riêng hiện tại có khác câu trích xuất chung của gói tin (sau chuẩn hóa khoảng trắng) hay không.
        /// </summary>
        public static bool HasDifferences(ShareDataSql? partnerSql, string? commonSql)
        {
            if (!IsCustom(partnerSql))
            {
                return false;
            }

            return !string.Equals(
                Normalize(partnerSql!.ExtractSql),
                Normalize(commonSql),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Lấy dòng câu SQL mẫu chung của gói tin (PartnerId trống); null khi gói chưa có câu SQL.
        /// </summary>
        public static async Task<ShareDataSql?> SelectCommonSql(
            ISqlSugarClient db,
            string? packetId,
            CancellationToken cancellationToken = default)
        {
            if (db == null || string.IsNullOrWhiteSpace(packetId))
            {
                return null;
            }

            return await db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PacketId == packetId && s.PartnerId == null)
                .FirstAsync(cancellationToken);
        }

        /// <summary>
        /// Lấy dòng cấu hình SQL riêng của đối tác cho gói tin; null khi đối tác không có dòng riêng.
        /// </summary>
        public static async Task<ShareDataSql?> SelectPartnerSql(
            ISqlSugarClient db,
            string? partnerId,
            ShareDataPacket? packet,
            CancellationToken cancellationToken = default)
        {
            if (db == null || string.IsNullOrWhiteSpace(partnerId) || string.IsNullOrWhiteSpace(packet?.ID))
            {
                return null;
            }

            var packetId = packet.ID;
            return await db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PartnerId == partnerId && s.PacketId == packetId)
                .FirstAsync(cancellationToken);
        }

        /// <summary>
        /// Đọc cấu hình SQL hiệu lực của gói tin cho đối tác (partnerId trống = chỉ mẫu chung) trong một truy vấn.
        /// </summary>
        public static async Task<SqlConfig> ResolveSqlConfig(
            ISqlSugarClient db,
            ShareDataPacket? packet,
            string? partnerId,
            CancellationToken cancellationToken = default)
        {
            if (db == null || string.IsNullOrWhiteSpace(packet?.ID))
            {
                return new SqlConfig(null, null);
            }

            var packetId = packet.ID;
            var hasPartner = !string.IsNullOrWhiteSpace(partnerId);
            var rows = await db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PacketId == packetId)
                .WhereIF(hasPartner, s => s.PartnerId == null || s.PartnerId == partnerId)
                .WhereIF(!hasPartner, s => s.PartnerId == null)
                .ToListAsync(cancellationToken);

            return PickSqlConfig(rows, packet, partnerId);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chọn cấu hình SQL hiệu lực của gói tin cho đối tác từ các dòng ShareDataSql đã nạp: dòng mẫu chung (PartnerId trống)
        ///              và dòng riêng đúng đối tác (partnerId trống = chỉ mẫu chung). Dùng chung cho đường tra CSDL và đường nạp trước.
        /// Created date: 12/11/2026
        /// </summary>
        public static SqlConfig PickSqlConfig(IEnumerable<ShareDataSql> rows, ShareDataPacket? packet, string? partnerId)
        {
            if (string.IsNullOrWhiteSpace(packet?.ID))
            {
                return new SqlConfig(null, null);
            }

            var packetId = packet.ID;
            var hasPartner = !string.IsNullOrWhiteSpace(partnerId);
            var scoped = rows
                .Where(s => s.IsDelete == null && string.Equals(s.PacketId, packetId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var common = scoped.FirstOrDefault(s => s.PartnerId == null);
            var partner = hasPartner
                ? scoped.FirstOrDefault(s => string.Equals(s.PartnerId, partnerId, StringComparison.OrdinalIgnoreCase))
                : null;

            return new SqlConfig(common, partner);
        }

        /// <summary>
        /// Đọc các dòng biến của đúng một câu SQL: partnerId trống = bộ biến của mẫu chung, có partnerId = bộ biến của SQL riêng đối tác đó.
        /// </summary>
        public static async Task<List<SqlVariableItem>> LoadVariableRows(
            ISqlSugarClient db,
            string? packetId,
            string? partnerId,
            CancellationToken cancellationToken = default)
        {
            return ReadVariables(await LoadVariablesJson(db, packetId, partnerId, cancellationToken));
        }

        /// <summary>Đọc cột VariablesJson của đúng một dòng ShareDataSql (partnerId trống = dòng mẫu chung); null khi chưa có dòng hoặc chưa lưu biến.</summary>
        private static async Task<string?> LoadVariablesJson(
            ISqlSugarClient db,
            string? packetId,
            string? partnerId,
            CancellationToken cancellationToken = default)
        {
            if (db == null || string.IsNullOrWhiteSpace(packetId))
            {
                return null;
            }

            var hasPartner = !string.IsNullOrWhiteSpace(partnerId);
            return await db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PacketId == packetId)
                .WhereIF(hasPartner, s => s.PartnerId == partnerId)
                .WhereIF(!hasPartner, s => s.PartnerId == null)
                .Select(s => s.VariablesJson)
                .FirstAsync(cancellationToken);
        }

        /// <summary>Đọc mảng biến từ JSON: trống, null hoặc JSON lỗi trả về rỗng; bỏ phần tử không có Code, sắp theo OrderNo.</summary>
        public static List<SqlVariableItem> ReadVariables(string? variablesJson)
        {
            if (string.IsNullOrWhiteSpace(variablesJson))
            {
                return [];
            }

            try
            {
                return (JsonConvert.DeserializeObject<List<SqlVariableItem>>(variablesJson) ?? [])
                    .Where(v => !string.IsNullOrWhiteSpace(v.Code))
                    .OrderBy(v => v.OrderNo ?? int.MaxValue)
                    .ToList();
            }
            catch (JsonException)
            {
                return [];
            }
        }

        /// <summary>Ghi mảng biến ra JSON; danh sách rỗng ghi "[]" (đã lưu rỗng), khác null (dòng đối tác chưa tách riêng).</summary>
        public static string WriteVariables(IReadOnlyCollection<SqlVariableItem>? items)
            => JsonConvert.SerializeObject(items ?? []);

        /// <summary>
        /// Biến của câu SQL đang chạy cho đối tác: đối tác có SQL riêng thì dùng bộ biến của SQL riêng, không thì dùng bộ biến của mẫu chung
        /// (biến đi theo câu SQL; SQL riêng và bộ biến của nó là bản chép tách khỏi mẫu chung).
        /// </summary>
        public static async Task<SqlVariableSet> ResolveVariables(
            ISqlSugarClient db,
            string? packetId,
            string? partnerId,
            CancellationToken cancellationToken = default)
        {
            if (db == null || string.IsNullOrWhiteSpace(packetId))
            {
                return SqlVariableSet.Empty;
            }

            var hasPartner = !string.IsNullOrWhiteSpace(partnerId);
            var rows = await db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PacketId == packetId)
                .WhereIF(hasPartner, s => s.PartnerId == null || s.PartnerId == partnerId)
                .WhereIF(!hasPartner, s => s.PartnerId == null)
                .ToListAsync(cancellationToken);

            return PickVariables(rows, packetId, partnerId);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chọn bộ biến của câu SQL đang chạy cho đối tác từ các dòng ShareDataSql đã nạp: đối tác có SQL riêng (ExtractSql có nội dung)
        ///              thì lấy bộ biến ở dòng của đối tác, không thì lấy bộ biến ở dòng mẫu chung. Dùng chung cho đường tra CSDL và đường nạp trước.
        /// Created date: 12/11/2026
        /// </summary>
        public static SqlVariableSet PickVariables(IEnumerable<ShareDataSql> rows, string? packetId, string? partnerId)
        {
            if (string.IsNullOrWhiteSpace(packetId))
            {
                return SqlVariableSet.Empty;
            }

            var scoped = rows
                .Where(s => s.IsDelete == null && string.Equals(s.PacketId, packetId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var hasPartner = !string.IsNullOrWhiteSpace(partnerId);
            var usePartner = hasPartner && scoped.Any(s => string.Equals(s.PartnerId, partnerId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(s.ExtractSql));
            var source = usePartner
                ? scoped.FirstOrDefault(s => string.Equals(s.PartnerId, partnerId, StringComparison.OrdinalIgnoreCase))
                : scoped.FirstOrDefault(s => s.PartnerId == null);
            var items = ReadVariables(source?.VariablesJson);

            return SqlVariableSet.From(items);
        }

        /// <summary>
        /// Bộ biến khi đang soạn / lưu SQL của đối tác: đối tác đã có SQL riêng hoặc đã có dòng biến thì dùng đúng bộ của đối tác (kể cả rỗng);
        /// chưa có gì (sắp tạo SQL riêng) thì dùng bộ của mẫu chung làm điểm xuất phát. partnerId trống = bộ của mẫu chung.
        /// </summary>
        public static async Task<List<SqlVariableItem>> LoadEditingVariableRows(
            ISqlSugarClient db,
            string? packetId,
            string? partnerId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(partnerId))
            {
                return await LoadVariableRows(db, packetId, null, cancellationToken);
            }

            var partnerRow = await db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PacketId == packetId && s.PartnerId == partnerId)
                .FirstAsync(cancellationToken);
            // Đối tác đã lưu bộ biến riêng (kể cả "[]") hoặc đã có SQL riêng thì dùng đúng bộ của đối tác
            if (partnerRow?.VariablesJson != null || IsCustom(partnerRow))
            {
                return ReadVariables(partnerRow?.VariablesJson);
            }

            return await LoadVariableRows(db, packetId, null, cancellationToken);
        }

        /// <summary>
        /// Tạo SQL riêng: chép bộ biến của mẫu chung sang làm bộ biến của đối tác nếu đối tác chưa có dòng biến nào.
        /// </summary>
        public static async Task CopyCommonVariablesToPartner(
            ISqlSugarClient db,
            string? packetId,
            string? partnerId,
            CancellationToken cancellationToken = default)
        {
            if (db == null || string.IsNullOrWhiteSpace(packetId) || string.IsNullOrWhiteSpace(partnerId))
            {
                return;
            }

            // Đối tác đã lưu bộ biến riêng (VariablesJson khác null) thì giữ nguyên
            var commonJson = await LoadVariablesJson(db, packetId, null, cancellationToken);
            await db.Updateable<ShareDataSql>()
                .SetColumns(s => s.VariablesJson == commonJson)
                .Where(s => s.IsDelete == null && s.PacketId == packetId && s.PartnerId == partnerId && s.VariablesJson == null)
                .ExecuteCommandAsync(cancellationToken);
        }

        /// <summary>
        /// Dùng lại mẫu chung: bỏ bộ biến riêng của đối tác (VariablesJson = null, kế thừa lại mẫu chung).
        /// </summary>
        public static async Task DeletePartnerVariables(
            ISqlSugarClient db,
            string? packetId,
            string? partnerId,
            CancellationToken cancellationToken = default)
        {
            if (db == null || string.IsNullOrWhiteSpace(packetId) || string.IsNullOrWhiteSpace(partnerId))
            {
                return;
            }

            await db.Updateable<ShareDataSql>()
                .SetColumns(s => s.VariablesJson == null)
                .Where(s => s.PacketId == packetId && s.PartnerId == partnerId)
                .ExecuteCommandAsync(cancellationToken);
        }

        /// <summary>
        /// Chọn đúng 1 hồ sơ ánh xạ chiều gửi (Outbound hoặc Both) đang bật cho cặp (PartnerId, Packet) theo thứ tự ưu tiên UpdateTime DESC, CreateTime DESC.
        /// </summary>
        public static async Task<ShareDataMapping?> SelectActiveOutboundMapping(
            ISqlSugarClient db,
            string? partnerId,
            ShareDataPacket? packet,
            CancellationToken cancellationToken = default)
        {
            if (db == null || string.IsNullOrWhiteSpace(partnerId) || packet == null)
            {
                return null;
            }

            var packetId = packet.ID ?? string.Empty;
            var packetCode = packet.Code ?? string.Empty;

            var rows = await db.Queryable<ShareDataMapping>()
                .Where(m => m.IsDelete == null
                    && m.IsActive == true
                    && m.PartnerId == partnerId
                    && (m.DatatypeId == packetId || m.DatatypeId == packetCode)
                    && (m.Direction == BaseEnums.Direction.Outbound || m.Direction == BaseEnums.Direction.Both))
                .ToListAsync(cancellationToken);

            return PickActiveOutboundMapping(rows, partnerId, packet);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chọn đúng 1 hồ sơ ánh xạ chiều gửi (Outbound hoặc Both) đang bật của cặp (PartnerId, Packet) từ danh sách đã nạp,
        ///              theo thứ tự ưu tiên UpdateTime DESC, CreateTime DESC. Dùng chung cho đường tra CSDL và đường nạp trước.
        /// Created date: 12/11/2026
        /// </summary>
        public static ShareDataMapping? PickActiveOutboundMapping(IEnumerable<ShareDataMapping> mappings, string? partnerId, ShareDataPacket? packet)
        {
            if (string.IsNullOrWhiteSpace(partnerId) || packet == null)
            {
                return null;
            }

            var packetId = packet.ID ?? string.Empty;
            var packetCode = packet.Code ?? string.Empty;

            return mappings
                .Where(m => m.IsDelete == null
                    && m.IsActive == true
                    && string.Equals(m.PartnerId, partnerId, StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(m.DatatypeId, packetId, StringComparison.OrdinalIgnoreCase) || string.Equals(m.DatatypeId, packetCode, StringComparison.OrdinalIgnoreCase))
                    && (m.Direction == BaseEnums.Direction.Outbound || m.Direction == BaseEnums.Direction.Both))
                .OrderByDescending(m => m.UpdateTime)
                .ThenByDescending(m => m.CreateTime)
                .FirstOrDefault();
        }
    }
}
