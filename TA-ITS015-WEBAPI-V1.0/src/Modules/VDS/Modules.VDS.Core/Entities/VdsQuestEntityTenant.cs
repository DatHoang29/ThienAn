using Shared.Core.Domain;
using SqlSugar;

namespace Modules.VDS.Core.Entities;

/// <summary>Hợp đồng tenant/audit cho QuestDB. Mốc thời gian do bên gọi gán,
/// còn xoá mềm lưu một mốc thời gian (null nghĩa là đang hoạt động).
///
/// <para><b>Vì sao khai <c>ColumnDataType</c>.</b> Không khai thì SqlSugar coi <c>string</c> là kiểu
/// <c>STRING</c> (kiểu chuỗi CŨ của QuestDB) — chậm và tốn đĩa hơn <c>VARCHAR</c>/<c>SYMBOL</c>. Khai vào
/// đây thì entity là NGUỒN DUY NHẤT của kiểu cột: DDL sinh từ entity ra đúng kiểu, và
/// <c>QuestSchemaContractTests</c> so được kiểu trong entity với kiểu trong
/// <c>Config/questdb_vds_tables.sql</c> nên hai bên không lệch âm thầm.</para>
///
/// <para>Đã đo 10/09/2026 trên QuestDB 11.3 + SqlSugar 5.1.4.214: <c>ColumnDataType</c> CHỈ ảnh hưởng DDL.
/// Tham số vẫn bind <c>DbType=String</c> nên INSERT/UPDATE/SELECT không đổi hành vi — lo ngại "khai
/// SYMBOL làm lỗi bind tham số Npgsql" ghi ở tài liệu cũ là KHÔNG đúng. <c>Length</c> bị bỏ qua khi có
/// <c>ColumnDataType</c> (QuestDB không nhận độ dài cho hai kiểu này); vẫn giữ <c>Length</c> để mô tả
/// giới hạn nghiệp vụ cho validator.</para></summary>
public abstract class VdsQuestEntityTenant : EntityTenant
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, Length = 64, ColumnDataType = "varchar")]
    public override string ID { get; set; } = "";

    [SugarColumn(ColumnName = "tenantId", IsNullable = true, Length = 64, IsOnlyIgnoreUpdate = true, ColumnDataType = "varchar")]
    public override string? TenantId { get; set; }

    [SugarColumn(ColumnName = "createTime", IsNullable = true, IsOnlyIgnoreUpdate = true)]
    public override DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "createUId", IsNullable = true, Length = 64, IsOnlyIgnoreUpdate = true, ColumnDataType = "varchar")]
    public override string? CreateUId { get; set; }

    [SugarColumn(ColumnName = "updateTime", IsNullable = true)]
    public override DateTime? UpdateTime { get; set; }

    [SugarColumn(ColumnName = "updateUId", IsNullable = true, Length = 64, ColumnDataType = "varchar")]
    public override string? UpdateUId { get; set; }

    [SugarColumn(ColumnName = "isDelete", IsNullable = true)]
    public override DateTime? IsDelete { get; set; }

    [SugarColumn(IsIgnore = true)]
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public override string? Code { get; set; }

    [SugarColumn(IsIgnore = true)]
    [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore]
    public override string? RowStatus { get; set; }
}
