using Services.Sample.Configuration;
using Services.Sample.Features.DataFlow.Models;
using Services.Shared.Persistence;
using Shared.Utility.Utilities;
using SqlSugar;

namespace Services.Sample.Features.DataFlow.Persistence;

/// <summary>
/// Author: TAC Team
/// Description: Repository nghiệp vụ cho bảng ServiceData. Kế thừa SqlSugarAccess (chỉ lo connection)
/// và tự viết query domain: lưu bản ghi đã chuẩn hoá + chống trùng theo Source/MessageId.
/// SqlSugar chỉ xuất hiện trong class này, không leak ra DataWorker. Connection chọn động theo
/// config runtime (DataConnectionId).
/// Created date: 16/07/2026
/// </summary>
public sealed class ServiceDataRepository(
    DatabaseManager databaseManager,
    IConfigManager configService) : SqlSugarAccess(databaseManager, () => configService.Current.DataConnectionId)
{
    /// <summary>Lưu bản ghi nếu chưa tồn tại (chống trùng theo Source/MessageId). Trả true nếu mới lưu.</summary>
    public async Task<bool> InsertIfNewAsync(DataRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await ExistsAsync(record))
            return false;

        var entity = new DataEntity
        {
            Source = record.Source,
            MessageId = record.MessageId,
            OccurredAt = record.OccurredAt,
            DataJson = record.NormalizedJson,
            CreateTime = BaseUtil.TimeServer.GetTimeServer(),
        };

        try
        {
            await Client.Insertable(entity).ExecuteCommandAsync(cancellationToken);
            return true;
        }
        catch (SqlSugarException)
        {
            // Race-safe: kiểm tra lại sau khi lỗi — có thể bản ghi vừa được insert bởi worker khác.
            if (await ExistsAsync(record))
                return false;

            throw;
        }
    }

    private Task<bool> ExistsAsync(DataRecord record) =>
        Client.Queryable<DataEntity>()
            .AnyAsync(x => x.Source == record.Source && x.MessageId == record.MessageId);
}
