namespace ShareDataWorker.Infrastructure.Logging
{
    /// <summary>
    /// Toàn bộ mã cảnh báo của ShareData Worker (ESH Alert Codes theo ESHARE §9).
    /// Đã chuẩn hoá liên tục (19 mã: 9 Outbound, 6 Inbound, 4 Tracking).
    /// Author: HieuNV
    /// Created date: 07/09/2026
    /// </summary>
    public static class ShareDataAlertCode
    {
        /// <summary>Mã cảnh báo LUỒNG GỬI ĐI (9 mã: ESH-1201..1204, ESH-1301..1304, ESH-1401).</summary>
        public static class Outbound
        {
            /// <summary>ESH-1201 · Bộ mã khai trong metadata không tồn tại trong CSDL (warning).</summary>
            public const string CodeSetNotFound = "ESH-1201";

            /// <summary>ESH-1202 · Trường bắt buộc bị thiếu hoặc null — huỷ kết xuất (error).</summary>
            public const string RequiredFieldMissing = "ESH-1202";

            /// <summary>ESH-1203 · Biểu thức không qua kiểm tra an toàn tĩnh hoặc lỗi khi tính — giữ giá trị nguồn (warning).</summary>
            public const string ExpressionFailed = "ESH-1203";

            /// <summary>ESH-1204 · TargetShapeJson rỗng hoặc sai cú pháp — huỷ kết xuất (error).</summary>
            public const string ShapeInvalid = "ESH-1204";

            /// <summary>ESH-1301 · Không tìm thấy cấu hình gói tin cho DatatypeId (error).</summary>
            public const string PacketNotFound = "ESH-1301";

            /// <summary>ESH-1302 · Lỗi truy vấn dữ liệu nguồn của gói tin (error).</summary>
            public const string QueryFailed = "ESH-1302";

            /// <summary>ESH-1303 · Không tìm thấy phễu lọc (ShareDataMapping) chiều gửi đang bật — huỷ kết xuất (error).</summary>
            public const string MappingNotFound = "ESH-1303";

            /// <summary>ESH-1304 · Khoá bản ghi cuối trang (__lastKey) dài hơn cột LastKey cho phép — huỷ kết xuất trước khi gửi (error).</summary>
            public const string CursorKeyTooLong = "ESH-1304";

            /// <summary>ESH-1305 · Không tìm thấy câu lệnh SQL trích xuất dữ liệu ở bất kỳ tầng nào (error).</summary>
            public const string ExtractSqlMissing = "ESH-1305";

            /// <summary>ESH-1306 · Câu truy vấn nối đuôi sai cấu trúc (thiếu cột __lastKey, ORDER BY hoặc WHERE so mốc sai): bỏ qua lượt gửi, giữ mốc (error).</summary>
            public const string CursorOrderInvalid = "ESH-1306";

            /// <summary>ESH-1307 · Mốc con trỏ gửi mới lùi lại so với mốc hiện tại trong CSDL (warning). Hiện không ghi: mốc lùi thì CommitSuccess bỏ qua, giữ mã để danh mục không đổi.</summary>
            public const string CursorRegression = "ESH-1307";

            /// <summary>
            /// ESH-1308 · Công thức cột mốc của câu đang chạy khác công thức lúc ghi mốc (câu bị sửa ngoài màn hình cấu hình):
            /// bỏ mốc cũ, đối tác nhận lại dữ liệu từ mốc khởi tạo (warning).
            /// </summary>
            public const string CursorFormulaChanged = "ESH-1308";

            /// <summary>ESH-1309 · Nhiều dòng cùng một giá trị cột mốc (JOIN nhân dòng): trang kế tiếp sẽ bỏ sót các dòng trùng nên không gửi, giữ mốc (error).</summary>
            public const string CursorKeyDuplicated = "ESH-1309";

            /// <summary>ESH-1401 · Gửi HTTP REST trực tiếp tới đối tác thất bại — file đã ghi vẫn coi là thành công (warning).</summary>
            public const string HttpSendFailed = "ESH-1401";

            /// <summary>ESH-1402 · Lưu tệp bằng chứng kết xuất thất bại — huỷ gửi và giữ nguyên mốc con trỏ (error).</summary>
            public const string ArchiveFailed = "ESH-1402";

            /// <summary>ESH-1403 · Giá trị cấu hình kết nối không hợp lệ bị bỏ qua hoặc thay bằng mặc định (warning).</summary>
            public const string ConfigValueIgnored = "ESH-1403";
        }

        /// <summary>Mã cảnh báo LUỒNG NHẬN VỀ (7 mã: ESH-1501..1507).</summary>
        public static class Inbound
        {
            /// <summary>ESH-1501 · Không đọc được nội dung gói tin nhận về (error).</summary>
            public const string ParseFailed = "ESH-1501";

            /// <summary>ESH-1502 · Lỗi ghi dữ liệu xuống CSDL nhận (error).</summary>
            public const string WriteFailed = "ESH-1502";

            /// <summary>ESH-1503 · Không tìm thấy cấu hình gói tin cho DatatypeId nhận về (error).</summary>
            public const string PacketNotFound = "ESH-1503";

            /// <summary>ESH-1504 · Đối tác của gói tin không tồn tại hoặc đang tắt (error).</summary>
            public const string PartnerInvalid = "ESH-1504";

            /// <summary>ESH-1505 · Gói tin chưa có đăng ký nhận hợp lệ (error).</summary>
            public const string NoSubscription = "ESH-1505";

            /// <summary>ESH-1506 · Gói tin chưa khai câu ghi chiều nhận (ShareDataSql.WriteSql) (error).</summary>
            public const string WriteSqlMissing = "ESH-1506";

            /// <summary>
            /// ESH-1507 · Giải mã gói tin nhận về thất bại (sai khóa, sai envelope hoặc sai tag) (error).
            /// Dành sẵn cho giải mã chiều nhận; chưa có nơi phát vì mã hóa chưa nối luồng.
            /// </summary>
            public const string DecodeFailed = "ESH-1507";
        }

        /// <summary>Mã sự cố HẠ TẦNG Change Tracking (4 mã: ESH-1601..1604) — ghi vào ShareDataActivityLog (cột Remark), dọn sau 7 ngày.</summary>
        public static class Tracking
        {
            /// <summary>ESH-1601 · Mốc Change Tracking không hợp lệ (SQL 22114/22115) — đã tự nhảy cóc để hồi phục.</summary>
            public const string SelfHeal = "ESH-1601";

            /// <summary>ESH-1602 · Truy vấn CHANGETABLE lỗi vì lý do KHÁC version — đã bỏ cache câu SQL và hẹn quét lại.</summary>
            public const string QueryFailed = "ESH-1602";

            /// <summary>ESH-1603 · CSDL hoặc bảng nguồn chưa bật Change Tracking — polling tạm dừng.</summary>
            public const string NotReady = "ESH-1603";

            /// <summary>ESH-1604 · Mốc đang giữ không còn tương thích CSDL (tracking bị tắt/bật lại, hoặc CSDL restore từ bản cũ hơn) — đã hạ mốc về -1 để đi lại đường khởi tạo.</summary>
            public const string VersionReset = "ESH-1604";

            /// <summary>Tiền tố nhận diện cả nhóm mã hạ tầng — dùng cho tác vụ dọn nhật ký quá hạn, ⛔ không gõ chuỗi "ESH-16" rời rạc ở nơi khác.</summary>
            public const string Prefix = "ESH-16";
        }
    }
}
