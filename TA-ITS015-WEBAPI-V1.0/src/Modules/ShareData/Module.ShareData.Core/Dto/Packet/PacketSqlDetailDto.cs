namespace Module.ShareData.Core.Dto.Packet
{
    /// <summary>
    /// Author: DatHQ
    /// Description: DTO lấy gọn cấu hình SQL mẫu chung của gói tin trong một lần gọi (câu truy vấn kèm trường, bộ biến, khối câu ghi).
    /// Created date: 27/10/2026
    /// </summary>
    public class GetPacketSqlDetailInput
    {
        public string PacketCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Cấu hình SQL mẫu chung của gói tin đọc từ một dòng ShareDataSql: giống hệt kết quả của GetExtractSql,
    ///              GetSqlVariables (mẫu chung) và GetWriteSqls.
    /// Created date: 27/10/2026
    /// </summary>
    public class PacketSqlDetailOutput
    {
        public PacketExtractSqlOutput Extract { get; set; } = new();

        public PacketSqlVariablesOutput Variables { get; set; } = new();

        public PacketWriteSqlsOutput Write { get; set; } = new();
    }
}
