using Module.ShareData.Core.Dto.Metadata;
using Module.ShareData.Core.Dto.Packet;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.ShareData.Controllers.Packet;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý Gói tin ESHARE (ShareDataPacket).

/// Created date: 2026-08-21
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 650)]
public class ShareDataPacketController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách gói tin theo phân trang
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get ShareDataPacket page list")]
    public virtual async Task<SqlSugarPagedList<ShareDataPagePacketOutput>> Page(ShareDataPagePacketInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách gói tin (không phân trang) — dùng cho dropdown chọn gói tin
    /// Created date: 2026-08-21
    /// </summary>
    [DisplayName("Get ShareDataPacket list")]
    public async Task<List<ShareDataPagePacketOutput>> GetList([FromQuery] ShareDataPacketInput input)
    {
        return await MessBus.InvokeAsync<List<ShareDataPagePacketOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 gói tin theo ID
    /// Created date: 2026-08-21
    /// </summary>
    [DisplayName("Get ShareDataPacket by id")]
    public async Task<ShareDataPacketOutput> GetById([FromQuery] ShareDataIdPacketInput input)
    {
        return await MessBus.InvokeAsync<ShareDataPacketOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 gói tin
    /// Created date: 2026-09-17
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ShareDataPacket")]
    public async Task<ShareDataPacketOutput> AddShareDataPacket(ShareDataAddPacketInput input)
    {
        return await MessBus.InvokeAsync<ShareDataPacketOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 gói tin
    /// Created date: 2026-09-17
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ShareDataPacket")]
    public async Task<ShareDataPacketOutput> UpdateShareDataPacket(ShareDataUpdatePacketInput input)
    {
        return await MessBus.InvokeAsync<ShareDataPacketOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 gói tin (xóa mềm)
    /// Created date: 2026-09-17
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ShareDataPacket")]
    public async Task DeleteShareDataPacket(ShareDataDeletePacketInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều gói tin (xóa mềm)
    /// Created date: 2026-09-17
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple ShareDataPacket")]
    public async Task BatchDeleteShareDataPacket(List<ShareDataDeletePacketInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy dữ liệu mẫu thật của 1 gói tin — chạy đúng câu truy vấn nguồn của gói tin
    /// Created date: 2026-09-22
    /// </summary>
    [ApiDescriptionSettings(Name = "SampleData"), HttpPost]
    [DisplayName("Get ShareDataPacket sample data")]
    public async Task<ShareDataPacketSampleDataOutput> GetSampleData(ShareDataPacketSampleDataInput input)
    {
        return await MessBus.InvokeAsync<ShareDataPacketSampleDataOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Lấy cấu hình câu truy vấn trích xuất động của gói tin
    /// Created date: 13/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetExtractSql"), HttpGet]
    [DisplayName("Get packet dynamic extract sql config")]
    public async Task<PacketExtractSqlOutput> GetExtractSql([FromQuery] GetPacketExtractSqlInput input)
    {
        return await MessBus.InvokeAsync<PacketExtractSqlOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Cập nhật câu truy vấn trích xuất động của gói tin
    /// Created date: 13/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "UpdateExtractSql"), HttpPost]
    [DisplayName("Update packet dynamic extract sql config")]
    public async Task<PacketExtractSqlOutput> UpdateExtractSql(UpdatePacketExtractSqlInput input)
    {
        return await MessBus.InvokeAsync<PacketExtractSqlOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Kiểm tra cú pháp SQL và lấy danh sách cột kết quả
    /// Created date: 13/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "ParseExtractColumns"), HttpPost]
    [DisplayName("Parse columns from dynamic extract sql")]
    public async Task<ParsePacketColumnsOutput> ParseExtractColumns(ParsePacketColumnsInput input)
    {
        return await MessBus.InvokeAsync<ParsePacketColumnsOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Dò cột trả về (khoá, kiểu, nguồn) của câu SQL đang soạn và đối chiếu với danh sách trường đang lưu, không ghi dữ liệu
    /// Created date: 24/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "DescribeColumns"), HttpPost]
    [DisplayName("Describe result columns of extract sql")]
    public async Task<DescribeColumnsOutput> DescribeColumns(DescribeColumnsInput input)
    {
        return await MessBus.InvokeAsync<DescribeColumnsOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Chạy thử câu SQL trích xuất và lấy dữ liệu mẫu tối đa 10 dòng
    /// Created date: 13/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "ExecuteExtractSqlSample"), HttpPost]
    [DisplayName("Execute dynamic extract sql sample")]
    public async Task<ExecutePacketSqlSampleOutput> ExecuteExtractSqlSample(ExecutePacketSqlSampleInput input)
    {
        return await MessBus.InvokeAsync<ExecutePacketSqlSampleOutput>(input);
    }


    /// <summary>
    /// Author: DatHQ
    /// Description: Lấy danh mục các bảng nguồn CSDL an toàn được phép chia sẻ cho bộ dựng truy vấn
    /// Created date: 17/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetMetadataTables"), HttpGet]
    [DisplayName("Get sharedata allowed source tables")]
    public async Task<List<MetadataTableDto>> GetMetadataTables()
    {
        return await MessBus.InvokeAsync<List<MetadataTableDto>>(new GetMetadataTablesInput());
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Lấy danh sách cột của một bảng nguồn cụ thể kèm kiểu dữ liệu, gợi ý alias và mô tả tiếng Việt
    /// Created date: 17/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetMetadataFields"), HttpGet]
    [DisplayName("Get sharedata source table columns")]
    public async Task<List<MetadataFieldDto>> GetMetadataFields([FromQuery] GetMetadataFieldsInput input)
    {
        return await MessBus.InvokeAsync<List<MetadataFieldDto>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách trường dùng được của gói tin (từ câu SQL riêng của đối tác hoặc câu mẫu chung) cho màn ánh xạ
    /// Created date: 26/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetFields"), HttpGet]
    [DisplayName("Get packet fields for mapping")]
    public async Task<List<SqlFieldItem>> GetFields([FromQuery] GetPacketFieldsInput input)
    {
        return await MessBus.InvokeAsync<List<SqlFieldItem>>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Lấy danh sách hồ sơ ánh xạ chiều gửi đi của một gói tin để cấu hình câu truy vấn riêng đối tác
    /// Created date: 22/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetPartnerSqlList"), HttpGet]
    [DisplayName("Get partner extract sql list by packet")]
    public async Task<List<PartnerSqlListItemOutput>> GetPartnerSqlList([FromQuery] GetPartnerSqlListInput input)
    {
        return await MessBus.InvokeAsync<List<PartnerSqlListItemOutput>>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Lấy chi tiết câu truy vấn trích xuất riêng của đối tác kèm đối chiếu với câu chuẩn của gói tin
    /// Created date: 22/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetPartnerExtractSql"), HttpGet]
    [DisplayName("Get partner extract sql detail and diff")]
    public async Task<PartnerExtractSqlOutput> GetPartnerExtractSql([FromQuery] GetPartnerExtractSqlInput input)
    {
        return await MessBus.InvokeAsync<PartnerExtractSqlOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Cập nhật câu truy vấn trích xuất riêng của đối tác
    /// Created date: 22/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "UpdatePartnerExtractSql"), HttpPost]
    [DisplayName("Update partner custom extract sql")]
    public async Task<PartnerExtractSqlOutput> UpdatePartnerExtractSql(UpdatePartnerExtractSqlInput input)
    {
        return await MessBus.InvokeAsync<PartnerExtractSqlOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đặt lại câu truy vấn trích xuất riêng của đối tác về theo gói tin chuẩn
    /// Created date: 22/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "ResetPartnerExtractSql"), HttpPost]
    [DisplayName("Reset partner custom extract sql")]
    public async Task<PartnerExtractSqlOutput> ResetPartnerExtractSql(ResetPartnerExtractSqlInput input)
    {
        return await MessBus.InvokeAsync<PartnerExtractSqlOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Lấy bảng tham số câu truy vấn của gói tin (mẫu chung, hoặc theo đối tác kèm giá trị ghi đè và biến riêng)
    /// Created date: 24/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetSqlVariables"), HttpGet]
    [DisplayName("Get packet sql variables")]
    public async Task<PacketSqlVariablesOutput> GetSqlVariables([FromQuery] GetPacketSqlVariablesInput input)
    {
        return await MessBus.InvokeAsync<PacketSqlVariablesOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Lưu bảng tham số câu truy vấn của gói tin cho mẫu chung hoặc cho một đối tác
    /// Created date: 24/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "UpdateSqlVariables"), HttpPost]
    [DisplayName("Update packet sql variables")]
    public async Task<PacketSqlVariablesOutput> UpdateSqlVariables(UpdatePacketSqlVariablesInput input)
    {
        return await MessBus.InvokeAsync<PacketSqlVariablesOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đặt lại mốc đã gửi (ShareDataLastSend) của gói tin cho đối tác
    /// Created date: 27/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "ResetLastSend"), HttpPost]
    [DisplayName("Reset partner packet last send cursor")]
    public async Task ResetLastSend(ResetLastSendInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đặt mốc nối đuôi (ShareDataLastSend) của gói tin cho một đối tác
    /// Created date: 29/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "SetLastSend"), HttpPost]
    [DisplayName("Set partner packet last send cursor")]
    public async Task SetLastSend(SetLastSendInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Lấy danh sách câu ghi chiều nhận (ghi ngược) của gói tin theo thứ tự chạy
    /// Created date: 26/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetWriteSqls"), HttpGet]
    [DisplayName("Get packet inbound write sqls")]
    public async Task<PacketWriteSqlsOutput> GetWriteSqls([FromQuery] GetPacketWriteSqlsInput input)
    {
        return await MessBus.InvokeAsync<PacketWriteSqlsOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Lưu toàn bộ danh sách câu ghi chiều nhận của gói tin (câu không gửi lên là xoá)
    /// Created date: 26/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "UpdateWriteSqls"), HttpPost]
    [DisplayName("Update packet inbound write sqls")]
    public async Task<PacketWriteSqlsOutput> UpdateWriteSqls(UpdatePacketWriteSqlsInput input)
    {
        return await MessBus.InvokeAsync<PacketWriteSqlsOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách đối tác có thể cấu hình câu ghi chiều nhận riêng của một gói tin (giống GetPartnerSqlList của chiều gửi)
    /// Created date: 30/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetPartnerWriteSqlList"), HttpGet]
    [DisplayName("Get partner inbound write sql list by packet")]
    public async Task<List<PartnerSqlListItemOutput>> GetPartnerWriteSqlList([FromQuery] GetPartnerWriteSqlListInput input)
    {
        return await MessBus.InvokeAsync<List<PartnerSqlListItemOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy câu ghi chiều nhận riêng của đối tác kèm câu ghi mẫu chung để đối chiếu
    /// Created date: 30/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetPartnerWriteSql"), HttpGet]
    [DisplayName("Get partner inbound write sql")]
    public async Task<PartnerWriteSqlOutput> GetPartnerWriteSql([FromQuery] GetPartnerWriteSqlInput input)
    {
        return await MessBus.InvokeAsync<PartnerWriteSqlOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật câu ghi chiều nhận riêng của đối tác
    /// Created date: 30/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "UpdatePartnerWriteSql"), HttpPost]
    [DisplayName("Update partner inbound write sql")]
    public async Task<PartnerWriteSqlOutput> UpdatePartnerWriteSql(UpdatePartnerWriteSqlInput input)
    {
        return await MessBus.InvokeAsync<PartnerWriteSqlOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Đặt lại câu ghi chiều nhận của đối tác về câu ghi mẫu chung
    /// Created date: 30/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "ResetPartnerWriteSql"), HttpPost]
    [DisplayName("Reset partner inbound write sql")]
    public async Task<PartnerWriteSqlOutput> ResetPartnerWriteSql(ResetPartnerWriteSqlInput input)
    {
        return await MessBus.InvokeAsync<PartnerWriteSqlOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Kiểm an toàn một câu ghi chiều nhận (không ghi dữ liệu)
    /// Created date: 26/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "ValidateWriteSql"), HttpPost]
    [DisplayName("Validate packet inbound write sql")]
    public async Task<ValidateWriteSqlOutput> ValidateWriteSql(ValidateWriteSqlInput input)
    {
        return await MessBus.InvokeAsync<ValidateWriteSqlOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Chạy thử câu ghi chiều nhận trên CSDL nhận với dữ liệu mẫu rồi quay lui (không ghi dữ liệu)
    /// Created date: 26/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "TryWriteSql"), HttpPost]
    [DisplayName("Try packet inbound write sql")]
    public async Task<TryWriteSqlOutput> TryWriteSql(TryWriteSqlInput input)
    {
        return await MessBus.InvokeAsync<TryWriteSqlOutput>(input);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Lấy gọn cấu hình SQL mẫu chung của gói tin trong một lần gọi: câu truy vấn kèm trường, bộ biến, khối câu ghi
    /// Created date: 27/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "GetSqlDetail"), HttpGet]
    [DisplayName("Get packet common sql detail")]
    public async Task<PacketSqlDetailOutput> GetSqlDetail([FromQuery] GetPacketSqlDetailInput input)
    {
        return await MessBus.InvokeAsync<PacketSqlDetailOutput>(input);
    }
}
