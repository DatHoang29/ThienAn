using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Phễu lọc 
    /// Created date: 2026-08-21
    /// </summary>
    [SugarTable("ShareDataMapping")]
    [SugarIndex("index_{table}_CT", nameof(CreateTime), OrderByType.Desc)]
    public class ShareDataMapping : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }
        [SugarColumn(IsNullable = true)]
        public string? DatatypeId { get; set; }
        [SugarColumn(IsNullable = true)]
        public string? PacketVersion { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "1 = Inbound | 0 = Outbound")]
        public BaseEnums.Direction? Direction { get; set; }
        /// <summary>
        /// Nguyên văn bản ghi mẫu JSON do đối tác cung cấp
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "JSON mẫu đối tác cung cấp")]
        public string? PartnerSchemaJson { get; set; }

        /// <summary>
        /// BỘ KHUNG đầu ra — bản sao cấu trúc JSON của đối tác, mỗi lá thay bằng một ký hiệu.
        /// Tài liệu đầy đủ cho người viết service: docs/sharedata/TargetShapeJson.md
        ///
        /// Ký hiệu ở lá:
        ///   giá trị JSON thường               → hằng số, gửi nguyên văn
        ///   null                              → khoá chưa ánh xạ, gửi null
        ///   {"$field":"X"}                    → điền giá trị của trường gói tin X (AliasFieldKey)
        ///   {"$field":"X","$extend":{...}}    → kèm luật quy đổi
        ///   {"$meta":"Now|Serial|PacketCode|PartnerCode","$value":"..."}
        ///                                     → giá trị hệ thống, service tự sinh; $value là bản
        ///                                       chụp FE ghi sẵn cho PacketCode/PartnerCode
        ///   {"$exclude":true}                 → DI SẢN: không gửi khoá này (FE không sinh nữa)
        ///
        /// Sáu khoá của $extend, đều không bắt buộc — không có luật nào thì không ghi $extend:
        ///   codeSet             → mã ShareDataCodeSet.Code dùng để quy đổi giá trị (2 chiều)
        ///   targetType          → kiểu dữ liệu PHÍA ĐỐI TÁC
        ///   dateFormat          → khuôn ngày PHÍA ĐỐI TÁC (gửi: format ra, nhận: parse vào)
        ///   numberFormat        → khuôn số PHÍA ĐỐI TÁC, chỉ áp khi GỬI
        ///   defaultPartnerValue → gửi giá trị này khi dữ liệu nội bộ rỗng (chiều GỬI)
        ///   defaultSourceValue  → lưu giá trị này khi đối tác không gửi (chiều NHẬN)
        ///
        /// Mảng không có ký hiệu riêng: mảng chứa $field bên trong là mảng NHÂN THEO BẢN GHI và
        /// chỉ giữ MỘT phần tử làm khuôn; mảng không chứa $field là mảng hằng, giữ nguyên.
        ///
        /// Luật gắn theo VỊ TRÍ, không theo trường: cùng một trường dùng ở hai khoá khác nhau thì
        /// mỗi khoá khai luật riêng, nhờ vậy đối tác đòi cùng giá trị ở hai chỗ với hai đơn vị
        /// khác nhau vẫn làm được.
        ///
        /// Nơi đọc: ShareDataWorker — DataPublicationService (chiều gửi) và
        /// DataInboundService.Parse (chiều nhận).
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Bộ khung đầu ra dạng JSON")]
        public string? TargetShapeJson { get; set; }

        [SugarColumn(IsNullable = true)]
        public bool? IsActive { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
