namespace Shared.Utility.Counting
{
    /// <summary>
    /// Author: KienLT
    /// Description: Bộ mã CỠ XE của cột <c>vehicleSize</c> (bảng thô VDS trên QuestDB). Đặt ở thư viện dùng
    /// chung để VDS và WebAPI không mỗi bên gõ một chuỗi — gõ lệch là số rơi hết vào nhóm "khác".
    ///
    /// <para>Cùng bộ mã với <c>TmsTrafficData.Type</c> của hệ cũ và với tên trường trên payload NATS
    /// <c>vds.count/v1</c>, nên đổi tên ở đây là đổi cả hợp đồng ra ngoài.</para>
    /// Created date: 04/08/2026
    /// </summary>
    public static class TrafficVehicleSizes
    {
        public const string Short = "short";
        public const string Medium = "medium";
        public const string Long = "long";
    }
}
