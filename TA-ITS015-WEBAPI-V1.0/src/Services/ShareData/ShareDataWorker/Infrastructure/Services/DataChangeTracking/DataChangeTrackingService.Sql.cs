
namespace ShareDataWorker.Infrastructure.Services.DataChangeTracking
{
    public partial class DataChangeTrackingService
    {
        public const string SqlCurrentVersion = "SELECT CHANGE_TRACKING_CURRENT_VERSION()";

        public const string SqlGetAllTrackedTables =
            "SELECT OBJECT_NAME(object_id) AS TableName, min_valid_version AS MinValidVersion FROM sys.change_tracking_tables";

        private const string SqlCheckDbTracking =
            "SELECT COUNT(1) FROM sys.change_tracking_databases WHERE database_id = DB_ID()";

        private const string SqlGetDbName = "SELECT DB_NAME()";

        private const string SqlCheckTableTracking =
            "SELECT COUNT(1) FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(@table)";


        /// <summary>
        /// Author: DatHQ
        /// Description: Tên bảng được phép ghép vào câu CHANGETABLE: chỉ gồm chữ, số, gạch dưới và không bắt đầu bằng số.
        /// Created date: 19/10/2026
        /// </summary>
        [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
        private static partial Regex SqlIdentifierRegex();

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu bật Change Tracking cho CSDL: thời gian giữ thay đổi kẹp trong 1 đến 30 ngày, bật AUTO_CLEANUP. Worker cần quyền ALTER DATABASE.
        /// Created date: 05/10/2026
        /// </summary>
        private static string SqlEnableDbTracking(
            string dbName,
            int retentionDays) =>
            $"ALTER DATABASE [{dbName.Replace("]", "]]")}] SET CHANGE_TRACKING = ON (CHANGE_RETENTION = {Math.Clamp(retentionDays, 1, 30)} DAYS, AUTO_CLEANUP = ON)";

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu bật Change Tracking cho một bảng nguồn, không theo dõi cột đã đổi vì chỉ cần biết bảng có thay đổi. Worker cần quyền ALTER TABLE.
        /// Created date: 05/10/2026
        /// </summary>
        private static string SqlEnableTableTracking(string table) =>
            $"ALTER TABLE [{table.Replace("]", "]]")}] ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF)";

        /// <summary>
        /// Description: Sinh câu truy vấn UNION ALL kiểm tra thay đổi (chỉ tính thao tác Thêm mới 'I' và Cập nhật 'U', bỏ qua Xóa 'D')
        ///              từ danh sách các bảng nguồn đang được theo dõi.
        /// Created date: 25/09/2026
        /// </summary>
        public static string BuildChangesSql(IReadOnlyList<string> trackedTables)
        {
            if (trackedTables == null || trackedTables.Count == 0)
                return string.Empty;

            var validTables = trackedTables
                .Where(t => !string.IsNullOrWhiteSpace(t) && SqlIdentifierRegex().IsMatch(t))
                .ToList();

            if (validTables.Count == 0)
                return string.Empty;

            var selects = validTables.Select(t =>
                $"SELECT '{t}' AS TableName WHERE EXISTS (SELECT 1 FROM CHANGETABLE(CHANGES [{t}], @lastVer) AS CT WHERE CT.SYS_CHANGE_OPERATION IN ('I', 'U'))");

            return string.Join("\nUNION ALL\n", selects);
        }

        /// <summary>
        /// Description: Sinh câu truy vấn UNION ALL theo từng bảng đã nhận xử lý, mỗi bảng dùng mốc OldVersion riêng của chính nó.
        /// Created date: 17/10/2026
        /// </summary>
        private static string BuildPerTableChangesSql(
            IReadOnlyList<ShareDataSourceTable> tablesToCheck,
            out SugarParameter[] parameters)
        {
            if (tablesToCheck == null || tablesToCheck.Count == 0)
            {
                parameters = [];
                return string.Empty;
            }

            var selects = new List<string>(tablesToCheck.Count);
            var paramList = new List<SugarParameter>(tablesToCheck.Count + 1);
            var minVer = long.MaxValue;

            for (var i = 0; i < tablesToCheck.Count; i++)
            {
                var row = tablesToCheck[i];
                var tableName = row.TableName?.Trim();
                if (string.IsNullOrEmpty(tableName) || !SqlIdentifierRegex().IsMatch(tableName))
                    continue;

                var oldVer = row.LastVersion ?? 0L;
                if (oldVer < minVer)
                    minVer = oldVer;

                var paramName = $"@lastVer_{i}";
                selects.Add(
                    $"SELECT '{tableName}' AS TableName WHERE EXISTS (SELECT 1 FROM CHANGETABLE(CHANGES [{tableName}], {paramName}) AS CT WHERE CT.SYS_CHANGE_OPERATION IN ('I', 'U'))");
                paramList.Add(new SugarParameter(paramName, oldVer));
            }

            if (selects.Count == 0)
            {
                parameters = [];
                return string.Empty;
            }

            paramList.Add(new SugarParameter("@lastVer", minVer == long.MaxValue ? 0L : minVer));
            parameters = paramList.ToArray();
            return string.Join("\nUNION ALL\n", selects);
        }
    }
}
