# ESHARE_CSDL

## Tổng quan

ESHARE — Tài liệu CSDL phân hệ Chia sẻ dữ liệu

Ngày lập: 20/08/2026 · Căn cứ cấu trúc và dữ liệu thật đọc từ CSDL DEV_ITS10

Tổng hợp: 5 bảng TẠO MỚI · 2 bảng DROP · 2 bảng ALTER · 3 bảng GIỮ NGUYÊN

• Mọi bảng ShareData đều rỗng, trừ ShareDataPartner (4 dòng) và ShareDataSubscription (5 dòng) ⇒ không cần script migrate.

• ShareDataPartner và ShareDataSubscription GIỮ NGUYÊN, không ALTER dòng nào — ba cột PartnerId / DatatypeId / Direction có sẵn trên Subscription chính là khoá nối sang gói tin.

• Toàn bộ CSDL này KHÔNG có FOREIGN KEY nào; quan hệ do tầng service giữ. Bảng mới cũng không khai FK, chỉ khai index.

• IsDelete kiểu datetime (xoá mềm = ghi mốc thời gian), KHÔNG phải bit. Mọi bộ lọc viết IsDelete IS NULL.

| STT | Tên bảng | Loại | Số cột | Dùng để làm gì | Tại sao cần nó |
| --- | --- | --- | --- | --- | --- |
| 1 | ShareDataPacket | TẠO MỚI | 18 | Masterdata gói tin (101–111+). Mỗi dòng là một phiên bản của một gói tin. | Thay danh mục shareData_type trong SysConfigType. Danh mục key-value chung không chứa được Version / Status / TopN và không có quan hệ với bảng nguồn. |
| 2 | ShareDataPacketTable | TẠO MỚI | 28 | Các bảng vật lý tham gia một gói tin, kèm điều kiện join và danh sách field (FieldsJson). | Mảnh thiếu chí mạng của thiết kế cũ: ShareDataDataSource chỉ khai được MỘT bảng, trong khi gói tin 101 cần ghép 3 bảng. Đây là chỗ định nghĩa gói tin gồm những gì. |
| 3 | ShareDataCodeSet | TẠO MỚI | 15 | Bộ mã quy đổi giá trị: 1 → on, 0 → off; 1 → slow, 2 → normal… | CSDL lưu mã số, đối tác cần chữ theo quy chuẩn. Tách riêng vì một bộ mã dùng lại được cho nhiều gói tin — nhét vào từng field thì khai lặp và sẽ lệch nhau. |
| 4 | ShareDataMapping | TẠO MỚI | 20 | Hồ sơ ánh xạ cho một đối tác × gói tin × chiều. Chỉ ghi phần lệch chuẩn. | Thay ShareDataMappingProfile. Khác biệt chính: bỏ DataSourceId (nguồn thuộc gói tin, không thuộc đối tác) nên thêm đối tác mới không phải khai lại nguồn và join. |
| 5 | ShareDataAlertLog | TẠO MỚI | 23 | Cảnh báo và lỗi cần người xử lý, có vòng đời xác nhận. | Màn 'Cảnh báo & lỗi' hiện đang chạy mà KHÔNG có bảng nào phía sau. |
| 6 | ShareDataActivityLog | ALTER (+3 cột) | 3 | Nhật ký hoạt động: tab Cấu hình (ai sửa gì) và tab Truyền nhận (gói tin nào đã đi qua). Gánh luôn vai trò bằng chứng xuất file. | Tab TRANSFER đã có sẵn ByteSize / RecordCount / FilePath / Hash và ActivityAction đã khai sẵn giá trị EXPORT — tạo bảng ExportLog riêng là làm hai lần một việc. ⚠ Bảng này CHỈ GHI THÊM, không sửa, không xoá. |
| 7 | ShareDataEventSource | ALTER (1 cột) | — | Danh mục nguồn sự kiện (subject NATS) để kích hoạt gửi theo sự kiện. | Nới DatatypeCode từ nvarchar(16) lên nvarchar(32) để khớp ShareDataPacket.Code. |
| 8 | ShareDataDataSource | DROP | — | (Cũ) Khai một bảng/view nguồn kèm danh sách cột. | Bỏ whitelist cột nên không còn vai trò. Ngoài ra nó chỉ khai được MỘT bảng, không đáp ứng được gói tin ghép nhiều bảng. 0 dòng dữ liệu. |
| 9 | ShareDataMappingProfile | DROP | — | (Cũ) Hồ sơ ánh xạ, có nhúng DataSourceId. | Thay bằng ShareDataMapping. Nhúng nguồn vào hồ sơ đối tác khiến 10 đối tác phải khai lại nguồn 10 lần. 0 dòng dữ liệu. |
| 10 | ShareDataPartner | GIỮ NGUYÊN | — | Đối tác chia sẻ dữ liệu và tham số kết nối C2C. | Đang có 4 dòng dữ liệu thật. Không ALTER. |
| 11 | ShareDataSubscription | GIỮ NGUYÊN | — | Đăng ký chia sẻ: đối tác × gói tin × chiều × lịch gửi. | Đang có 5 dòng. Ba cột PartnerId / DatatypeId / Direction đã đủ làm khoá nối sang ShareDataMapping nên không cần thêm cột nào. |
| 12 | ShareDataSession | GIỮ NGUYÊN | — | Phiên kết nối C2C với đối tác. | Không liên quan tới thay đổi lần này. |

## ShareDataPacket

ShareDataPacket

[TẠO MỚI]  Masterdata gói tin (101–111+). Mỗi dòng là một phiên bản của một gói tin.

Tại sao cần bảng này: Thay danh mục shareData_type trong SysConfigType. Danh mục key-value chung không chứa được Version / Status / TopN và không có quan hệ với bảng nguồn.

| STT | Tên cột | Kiểu dữ liệu | Null | Nhóm | Dùng để làm gì | Tại sao cần nó |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | ID | nvarchar(64) | N | Khoá chính | Khoá chính của gói tin. | Tên cột viết HOA hai chữ theo đúng ShareDataPartner / ShareDataSubscription đang có. |
| 2 | Name | nvarchar(128) | Y |  | Tên gói tin, VD 'Thông tin chung / luồng giao thông'. | Hiển thị trên danh sách gói tin và trong nhật ký cho người vận hành đọc. |
| 3 | Version | int | Y |  | Phiên bản cấu trúc gói tin. Mặc định 1. | Gói tin là hợp đồng dài hạn với đối tác. Đổi field mà không version thì mọi đối tác vỡ cùng lúc. Đổi cấu trúc = tạo bản mới, hai bản chạy song song lúc chuyển đổi. |
| 4 | Description | nvarchar(256) | Y |  | Mô tả nội dung gói tin. | Người vận hành cần biết gói tin này chứa gì mà không phải mở đặc tả Word. |
| 5 | TopN | int | Y |  | Trần số bản ghi mỗi lần xuất. | Chặn trên để một lần gửi lỗi cấu hình không kéo cả triệu dòng ra khỏi CSDL. Đặt ở cấp gói tin vì đây là thuộc tính của lần xuất, không phải của bảng nguồn. |
| 6 | MaxIntervalSec | int | Y |  | Chu kỳ gửi tối thiểu theo yêu cầu kỹ thuật (giây). | Quy chuẩn quy định tần suất tối đa cho từng loại dữ liệu. Dùng để chặn khi người dùng đặt lịch dày hơn mức cho phép. |
| 7 | Status | int | Y |  | 0 = ngừng · 1 = dùng | Trạng thái nháp cho phép khai dở dang mà chưa gửi cho ai. Cũng là chỗ móc quy trình duyệt hai bước: người khai tạo ở nháp, cấp quản lý chuyển sang dùng. |
| 8 | OrderNo | int | Y |  | Thứ tự hiển thị trong danh sách. | Cho phép sắp gói tin hay dùng lên đầu thay vì sắp theo mã. |
| 9 | Code | nvarchar(32) | Y | Khoá nghiệp vụ | Mã gói tin, VD '101'. ⚠ nvarchar(32), KHÔNG phải 64 như đuôi chuẩn. | Phải KHỚP KIỂU với ShareDataSubscription.DatatypeId nvarchar(32) — đây là khoá nối giữa đăng ký và gói tin. Lệch kiểu khi join gây implicit conversion và mất index. |
| 10 | Remark | nvarchar(256) | Y | Cột chuẩn | Ghi chú tự do của người nhập. | Cột chuẩn có ở mọi bảng ShareData hiện tại — giữ để đồng bộ. |
| 11 | TenantId | nvarchar(64) | Y | Cột chuẩn | Mã đơn vị (multi-tenant). | Khung ứng dụng đang dùng cột này để phân tách dữ liệu giữa các đơn vị. |
| 12 | Code | nvarchar(64) | Y | Cột chuẩn | Mã bản ghi do người dùng đặt. | Cột chuẩn của khung. Ở một số bảng nó mang nghĩa nghiệp vụ (xem ghi chú riêng). |
| 13 | CreateTime | datetime | Y | Cột chuẩn | Thời điểm tạo bản ghi. | Cột chuẩn; đồng thời là khoá của index index_<Bảng>_CT. |
| 14 | CreateUId | nvarchar(64) | Y | Cột chuẩn | Người tạo. | Cột chuẩn, phục vụ truy trách nhiệm. |
| 15 | UpdateTime | datetime | Y | Cột chuẩn | Thời điểm sửa gần nhất. | Cột chuẩn. Còn dùng để phát hiện hai người sửa cùng một bản ghi. |
| 16 | UpdateUId | nvarchar(64) | Y | Cột chuẩn | Người sửa gần nhất. | Cột chuẩn, phục vụ truy trách nhiệm. |
| 17 | RowStatus | nvarchar(32) | Y | Cột chuẩn | Trạng thái dòng theo quy ước khung. | Cột chuẩn có ở mọi bảng ShareData. |
| 18 | IsDelete | datetime | Y | Cột chuẩn | Xoá mềm — GHI MỐC THỜI GIAN, không phải cờ true/false. Chưa xoá = NULL. | ⚠ Khác thông lệ: kiểu datetime chứ không phải bit. Mọi bộ lọc phải viết IsDelete IS NULL, và mọi filtered index phải kèm WHERE IsDelete IS NULL. |

## ShareDataPacketTable

ShareDataPacketTable

[TẠO MỚI]  Các bảng vật lý tham gia một gói tin, kèm điều kiện join và danh sách field (FieldsJson).

Tại sao cần bảng này: Mảnh thiếu chí mạng của thiết kế cũ: ShareDataDataSource chỉ khai được MỘT bảng, trong khi gói tin 101 cần ghép 3 bảng. Đây là chỗ định nghĩa gói tin gồm những gì.

| STT | Tên cột | Kiểu dữ liệu | Null | Nhóm | Dùng để làm gì | Tại sao cần nó |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | ID | nvarchar(64) | N | Khoá chính | Khoá chính. | Quy ước khung. |
| 2 | PacketId | nvarchar(64) | Y | Tham chiếu | Trỏ tới ShareDataPacket.ID. | Một gói tin gồm nhiều bảng. Không khai FOREIGN KEY vì toàn bộ CSDL này không có FK nào — quan hệ do tầng service giữ. |
| 3 | Name | nvarchar(128) | Y |  | Tên gợi nhớ cho bảng trong ngữ cảnh gói tin. | Tên bảng vật lý thường khó đọc; cột này để người vận hành đặt nhãn dễ hiểu. |
| 4 | DbRef | nvarchar(64) | Y |  | Tham chiếu kết nối CSDL. Mặc định 'SqlServer'. | Để sau này gói tin lấy được dữ liệu từ nhiều CSDL khác nhau mà không phải đổi cấu trúc. |
| 5 | SchemaName | nvarchar(64) | Y |  | Schema, mặc định 'dbo'. | Tách riêng thay vì gộp chuỗi 'dbo.TmsZone'. Gộp là mất khả năng truy vấn 'bảng này đang nằm trong gói tin nào' vì mỗi người viết một kiểu. |
| 6 | TableName | nvarchar(128) | Y |  | Tên bảng hoặc view vật lý, VD 'TmsZoneStatus'. | Đây là nguồn đọc dữ liệu thật. Phải đối chiếu INFORMATION_SCHEMA khi lưu. |
| 7 | ObjectType | int | Y |  | 1 = TABLE · 2 = VIEW. | Logic 'trạng thái mới nhất' hay 'tổng hợp theo khung giờ' cần hàm cửa sổ và GROUP BY, không diễn đạt được bằng JoinCondition. Đóng gói trong VIEW rồi trỏ vào, thay vì bắt người vận hành gõ logic cửa sổ. |
| 8 | Alias | nvarchar(16) | Y | Khoá nghiệp vụ | Bí danh trong câu SQL: a, b, c. | Bắt buộc vì gói tin join nhiều bảng có cột trùng tên (cả TmsZone và TmsZoneStatus đều có cột ID). Duy nhất trong phạm vi gói tin, DB đảm bảo bằng unique index. |
| 9 | IsRoot | bit | Y | Khoá nghiệp vụ | Bảng gốc của câu truy vấn (FROM). | Mỗi biến thể nguồn phải có ĐÚNG MỘT bảng gốc. DB đảm bảo bằng filtered unique index — đây là ràng buộc dễ sai nhất khi sửa cấu trúc bằng tay. |
| 10 | JoinType | nvarchar(16) | Y |  | INNER \| LEFT. | Quyết định bản ghi thiếu dữ liệu ở bảng phụ có bị loại khỏi gói tin hay không. |
| 11 | JoinCondition | nvarchar(512) | Y |  | Điều kiện nối, VD 'a.ZoneId = b.ID'. | Đây là thứ định nghĩa gói tin gồm những bảng nào ghép lại. Chỉ được trỏ vào alias đã khai PHÍA TRÊN nó để chống join vòng. ⚠ Chuỗi tự do ghép vào SQL — phải chặn ký tự nguy hiểm và luôn QUOTENAME định danh. |
| 12 | FilterExpr | nvarchar(512) | Y |  | Điều kiện WHERE riêng cho bảng này. | Lọc bớt dữ liệu ngay tại nguồn, VD chỉ lấy bản ghi còn hiệu lực. |
| 13 | VariantNo | int | Y |  | Nhóm nguồn. Mặc định 1. HIỆN CHƯA DÙNG. | Bảo hiểm rẻ cho tình huống một gói tin lấy dữ liệu từ hai hệ thống nguồn khác nhau. Thêm sẵn một cột NULL bây giờ rẻ hơn ALTER bảng đã có dữ liệu về sau. |
| 14 | ParentTableId | nvarchar(64) | Y |  | Bảng cha khi đầu ra lồng nhau. HIỆN CHƯA DÙNG. | Dự phòng cho v2 khi đối tác yêu cầu JSON nhiều tầng thay vì phẳng một tầng. |
| 15 | Cardinality | nvarchar(8) | Y |  | ONE \| MANY. HIỆN CHƯA DÙNG. | Đi kèm ParentTableId, cho biết quan hệ một–một hay một–nhiều khi dựng đầu ra lồng. |
| 16 | OrderNo | int | Y |  | Thứ tự JOIN. | Thứ tự nối có ý nghĩa: điều kiện join chỉ được trỏ vào alias đã xuất hiện trước đó. Không lưu thứ tự thì phụ thuộc thứ tự trả về của CSDL, không đảm bảo. |
| 17 | FieldsJson | nvarchar(max) | Y | JSON | Danh sách field lấy từ bảng này. Lược đồ xem sheet 'JSON - FieldsJson'. | Field không bao giờ được đọc tách khỏi bảng của nó, và không ràng buộc DB nào bao được 'fieldKey duy nhất theo gói tin' (vì field trải trên nhiều dòng). Để bảng riêng cũng không mua thêm gì, nên gộp vào JSON cho gọn. |
| 18 | SchemaValid | bit | Y |  | Khai báo còn khớp CSDL hay không. | Vì ColumnName chỉ là chuỗi, không FK, nên không có gì bảo đảm cột đó còn tồn tại. Cột này để đánh dấu khi phát hiện lệch. Backend dùng hay không tuỳ. |
| 19 | SchemaCheckedAt | datetime | Y |  | Thời điểm kiểm tra gần nhất. | Đi kèm SchemaValid. |
| 20 | Remark | nvarchar(256) | Y | Cột chuẩn | Ghi chú tự do của người nhập. | Cột chuẩn có ở mọi bảng ShareData hiện tại — giữ để đồng bộ. |
| 21 | TenantId | nvarchar(64) | Y | Cột chuẩn | Mã đơn vị (multi-tenant). | Khung ứng dụng đang dùng cột này để phân tách dữ liệu giữa các đơn vị. |
| 22 | Code | nvarchar(64) | Y | Cột chuẩn | Mã bản ghi do người dùng đặt. | Cột chuẩn của khung. Ở một số bảng nó mang nghĩa nghiệp vụ (xem ghi chú riêng). |
| 23 | CreateTime | datetime | Y | Cột chuẩn | Thời điểm tạo bản ghi. | Cột chuẩn; đồng thời là khoá của index index_<Bảng>_CT. |
| 24 | CreateUId | nvarchar(64) | Y | Cột chuẩn | Người tạo. | Cột chuẩn, phục vụ truy trách nhiệm. |
| 25 | UpdateTime | datetime | Y | Cột chuẩn | Thời điểm sửa gần nhất. | Cột chuẩn. Còn dùng để phát hiện hai người sửa cùng một bản ghi. |
| 26 | UpdateUId | nvarchar(64) | Y | Cột chuẩn | Người sửa gần nhất. | Cột chuẩn, phục vụ truy trách nhiệm. |
| 27 | RowStatus | nvarchar(32) | Y | Cột chuẩn | Trạng thái dòng theo quy ước khung. | Cột chuẩn có ở mọi bảng ShareData. |
| 28 | IsDelete | datetime | Y | Cột chuẩn | Xoá mềm — GHI MỐC THỜI GIAN, không phải cờ true/false. Chưa xoá = NULL. | ⚠ Khác thông lệ: kiểu datetime chứ không phải bit. Mọi bộ lọc phải viết IsDelete IS NULL, và mọi filtered index phải kèm WHERE IsDelete IS NULL. |

## ShareDataCodeSet

ShareDataCodeSet

[TẠO MỚI]  Bộ mã quy đổi giá trị: 1 → on, 0 → off; 1 → slow, 2 → normal…

Tại sao cần bảng này: CSDL lưu mã số, đối tác cần chữ theo quy chuẩn. Tách riêng vì một bộ mã dùng lại được cho nhiều gói tin — nhét vào từng field thì khai lặp và sẽ lệch nhau.

| STT | Tên cột | Kiểu dữ liệu | Null | Nhóm | Dùng để làm gì | Tại sao cần nó |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | ID | nvarchar(64) | N | Khoá chính | Khoá chính. | Quy ước khung. |
| 2 | Name | nvarchar(128) | Y |  | Tên bộ mã, VD 'Tình trạng giao thông'. | Hiển thị trên màn Bộ mã và trong dropdown khi gán cho field. |
| 3 | Description | nvarchar(256) | Y |  | Mô tả bộ mã. | Giải thích bộ mã này dùng cho loại dữ liệu nào. |
| 4 | Scope | int | Y |  | 1 = bộ mã chuẩn theo TCVN · 2 = biến thể cho đối tác lệch chuẩn. | Để lọc trên màn Bộ mã. ⚠ KHÔNG có PartnerId: một bộ mã có thể dùng cho nhiều đối tác, và ràng buộc 'đối tác nào dùng bộ mã nào' đã nằm ở Mapping.ItemsJson — khai thêm ở đây là nguồn sự thật kép. |
| 5 | ValuesJson | nvarchar(max) | Y | JSON | Danh sách giá trị quy đổi. Lược đồ xem sheet 'JSON - ValuesJson'. | Luôn đọc cùng bộ mã, không bao giờ truy vấn độc lập, mỗi bộ chỉ vài dòng — để bảng con riêng không mang lại lợi ích gì. |
| 6 | Code | nvarchar(64) | Y | Khoá nghiệp vụ | Mã bộ, VD 'TRAFFIC_COND', 'DEVICE_STATE'. | Mã ngắn để tham chiếu và để người khai nhận ra nhanh. Duy nhất. |
| 7 | Remark | nvarchar(256) | Y | Cột chuẩn | Ghi chú tự do của người nhập. | Cột chuẩn có ở mọi bảng ShareData hiện tại — giữ để đồng bộ. |
| 8 | TenantId | nvarchar(64) | Y | Cột chuẩn | Mã đơn vị (multi-tenant). | Khung ứng dụng đang dùng cột này để phân tách dữ liệu giữa các đơn vị. |
| 9 | Code | nvarchar(64) | Y | Cột chuẩn | Mã bản ghi do người dùng đặt. | Cột chuẩn của khung. Ở một số bảng nó mang nghĩa nghiệp vụ (xem ghi chú riêng). |
| 10 | CreateTime | datetime | Y | Cột chuẩn | Thời điểm tạo bản ghi. | Cột chuẩn; đồng thời là khoá của index index_<Bảng>_CT. |
| 11 | CreateUId | nvarchar(64) | Y | Cột chuẩn | Người tạo. | Cột chuẩn, phục vụ truy trách nhiệm. |
| 12 | UpdateTime | datetime | Y | Cột chuẩn | Thời điểm sửa gần nhất. | Cột chuẩn. Còn dùng để phát hiện hai người sửa cùng một bản ghi. |
| 13 | UpdateUId | nvarchar(64) | Y | Cột chuẩn | Người sửa gần nhất. | Cột chuẩn, phục vụ truy trách nhiệm. |
| 14 | RowStatus | nvarchar(32) | Y | Cột chuẩn | Trạng thái dòng theo quy ước khung. | Cột chuẩn có ở mọi bảng ShareData. |
| 15 | IsDelete | datetime | Y | Cột chuẩn | Xoá mềm — GHI MỐC THỜI GIAN, không phải cờ true/false. Chưa xoá = NULL. | ⚠ Khác thông lệ: kiểu datetime chứ không phải bit. Mọi bộ lọc phải viết IsDelete IS NULL, và mọi filtered index phải kèm WHERE IsDelete IS NULL. |

## ShareDataMapping

ShareDataMapping

[TẠO MỚI]  Hồ sơ ánh xạ cho một đối tác × gói tin × chiều. Chỉ ghi phần lệch chuẩn.

Tại sao cần bảng này: Thay ShareDataMappingProfile. Khác biệt chính: bỏ DataSourceId (nguồn thuộc gói tin, không thuộc đối tác) nên thêm đối tác mới không phải khai lại nguồn và join.

| STT | Tên cột | Kiểu dữ liệu | Null | Nhóm | Dùng để làm gì | Tại sao cần nó |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | ID | nvarchar(64) | N | Khoá chính | Khoá chính. | Quy ước khung. |
| 2 | PartnerId | nvarchar(64) | Y | Khoá nghiệp vụ | Trỏ tới ShareDataPartner.ID. | ⚠ Đây là bảng DUY NHẤT trong nhóm mới có cột đối tác. Nguyên tắc thiết kế: chỉ một nơi được phép nhắc tới đối tác, mọi thứ riêng theo đối tác đều nằm ở đây. |
| 3 | DatatypeId | nvarchar(32) | Y | Khoá nghiệp vụ | Mã gói tin — bằng ShareDataPacket.Code. | Cùng kiểu với ShareDataSubscription.DatatypeId. Khoá tra cứu lúc chạy là bộ ba (PartnerId, DatatypeId, Direction) — cả ba cột này đều đã có sẵn trên Subscription nên không phải ALTER bảng đó. |
| 4 | PacketId | nvarchar(64) | Y | Tham chiếu | Trỏ tới bản gói tin cụ thể đang áp dụng. | DatatypeId chỉ ra loại gói tin; PacketId chỉ ra đúng bản ghi nào (vì có nhiều phiên bản). |
| 5 | PacketVersion | int | Y |  | Ghim phiên bản cấu trúc mà hồ sơ này được viết cho. | Cho phép đối tác chưa sẵn sàng tiếp tục chạy bản cũ trong khi đối tác khác đã chuyển sang bản mới. Đặt ở đây thay vì trên Subscription vì Subscription không được sửa. |
| 6 | Direction | nvarchar(16) | Y | Khoá nghiệp vụ | OUTBOUND \| INBOUND. | ⚠ Phải cùng kiểu và cùng tập giá trị với ShareDataSubscription.Direction nvarchar(16). Bảng MappingProfile cũ dùng nvarchar(8) với OUT/IN — lệch, phải thống nhất. |
| 7 | Format | nvarchar(8) | Y |  | DATA \| FILE. | Khớp ShareDataSubscription.Format. Quyết định gửi dữ liệu trực tiếp hay đóng gói file. |
| 8 | TargetRootEntity | nvarchar(128) | Y |  | Tên thực thể gốc phía đối tác, VD 'BB'. | Đối tác không chỉ gọi field khác tên mà còn gọi BẢNG khác tên. Bản thiết kế cũ chỉ đổi được tên field, thiếu tầng này. |
| 9 | ItemsJson | nvarchar(max) | Y | JSON | CHỈ ghi field lệch chuẩn. Lược đồ xem sheet 'JSON - ItemsJson'. | Gói tin theo quy chuẩn nên đa số đối tác nhận đúng tên chuẩn. Field không có dòng thì gửi theo chuẩn ⇒ đối tác tuân thủ chuẩn có ItemsJson = [], giảm hẳn khối lượng khai báo. |
| 10 | IsActive | bit | Y | Khoá nghiệp vụ | Hồ sơ đang được áp dụng. | Duy nhất một hồ sơ đang dùng cho mỗi (đối tác × gói tin × chiều). DB đảm bảo bằng filtered unique index — nếu không sẽ có hai hồ sơ cùng hiệu lực và không biết dùng cái nào. |
| 11 | Name | nvarchar(128) | Y |  | Tên hồ sơ ánh xạ. | Hiển thị trên danh sách để phân biệt các bản nháp/lịch sử. |
| 12 | Remark | nvarchar(256) | Y | Cột chuẩn | Ghi chú tự do của người nhập. | Cột chuẩn có ở mọi bảng ShareData hiện tại — giữ để đồng bộ. |
| 13 | TenantId | nvarchar(64) | Y | Cột chuẩn | Mã đơn vị (multi-tenant). | Khung ứng dụng đang dùng cột này để phân tách dữ liệu giữa các đơn vị. |
| 14 | Code | nvarchar(64) | Y | Cột chuẩn | Mã bản ghi do người dùng đặt. | Cột chuẩn của khung. Ở một số bảng nó mang nghĩa nghiệp vụ (xem ghi chú riêng). |
| 15 | CreateTime | datetime | Y | Cột chuẩn | Thời điểm tạo bản ghi. | Cột chuẩn; đồng thời là khoá của index index_<Bảng>_CT. |
| 16 | CreateUId | nvarchar(64) | Y | Cột chuẩn | Người tạo. | Cột chuẩn, phục vụ truy trách nhiệm. |
| 17 | UpdateTime | datetime | Y | Cột chuẩn | Thời điểm sửa gần nhất. | Cột chuẩn. Còn dùng để phát hiện hai người sửa cùng một bản ghi. |
| 18 | UpdateUId | nvarchar(64) | Y | Cột chuẩn | Người sửa gần nhất. | Cột chuẩn, phục vụ truy trách nhiệm. |
| 19 | RowStatus | nvarchar(32) | Y | Cột chuẩn | Trạng thái dòng theo quy ước khung. | Cột chuẩn có ở mọi bảng ShareData. |
| 20 | IsDelete | datetime | Y | Cột chuẩn | Xoá mềm — GHI MỐC THỜI GIAN, không phải cờ true/false. Chưa xoá = NULL. | ⚠ Khác thông lệ: kiểu datetime chứ không phải bit. Mọi bộ lọc phải viết IsDelete IS NULL, và mọi filtered index phải kèm WHERE IsDelete IS NULL. |

## ShareDataAlertLog

ShareDataAlertLog

[TẠO MỚI]  Cảnh báo và lỗi cần người xử lý, có vòng đời xác nhận.

Tại sao cần bảng này: Màn 'Cảnh báo & lỗi' hiện đang chạy mà KHÔNG có bảng nào phía sau.

| STT | Tên cột | Kiểu dữ liệu | Null | Nhóm | Dùng để làm gì | Tại sao cần nó |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | ID | nvarchar(64) | N | Khoá chính | Khoá chính. | Quy ước khung. |
| 2 | OccurredAt | datetime | Y |  | Thời điểm phát sinh. | Khác CreateTime: sự cố có thể xảy ra trước lúc ghi nhận. |
| 3 | Severity | nvarchar(16) | Y |  | warning \| error. | Phân biệt việc cần xử lý ngay với việc chỉ cần biết. |
| 4 | AlertSource | nvarchar(32) | Y |  | session \| packet \| subscription \| protocol \| schema. | ⚠ Đặt tên AlertSource chứ không phải Source — tránh nhầm với ActivityLog.TargetType khi đọc code. |
| 5 | AlertCode | nvarchar(32) | Y |  | ESH-xxxx hoặc reason-cd giao thức ISO 14827-2. | Mã hoá lỗi để tra cứu và thống kê, thay vì so khớp chuỗi thông báo. |
| 6 | PartnerId | nvarchar(64) | Y |  | Đối tác liên quan. | Lọc cảnh báo theo đối tác trên giao diện. |
| 7 | SessionId | nvarchar(64) | Y |  | Phiên kết nối liên quan. | Truy ngược về phiên khi lỗi thuộc tầng kết nối. |
| 8 | SubscriptionId | nvarchar(64) | Y |  | Đăng ký liên quan. | Truy ngược về đăng ký khi lỗi thuộc tầng gửi/nhận. |
| 9 | DatatypeId | nvarchar(32) | Y |  | Mã gói tin liên quan. | Lọc cảnh báo theo gói tin. |
| 10 | Message | nvarchar(1000) | Y |  | Nội dung mô tả tiếng Việt. | Thứ người vận hành đọc trực tiếp. |
| 11 | DetailJson | nvarchar(max) | Y | JSON | Chi tiết bổ sung dạng JSON. | Chứa ngữ cảnh kỹ thuật (câu SQL lỗi, field thiếu…) mà không phải thêm cột mới cho từng loại lỗi. |
| 12 | Acknowledged | bit | Y | Trạng thái | Đã có người xác nhận xử lý. | ⚠ Chính ba cột Ack* này khiến bảng cảnh báo KHÔNG gộp được vào ShareDataActivityLog: cảnh báo phải UPDATE được, còn nhật ký truyền nhận là bằng chứng chỉ ghi thêm. |
| 13 | AckBy | nvarchar(64) | Y | Trạng thái | Người xác nhận. | Truy trách nhiệm xử lý sự cố. |
| 14 | AckAt | datetime | Y | Trạng thái | Thời điểm xác nhận. | Đo thời gian phản ứng với sự cố. |
| 15 | Remark | nvarchar(256) | Y | Cột chuẩn | Ghi chú tự do của người nhập. | Cột chuẩn có ở mọi bảng ShareData hiện tại — giữ để đồng bộ. |
| 16 | TenantId | nvarchar(64) | Y | Cột chuẩn | Mã đơn vị (multi-tenant). | Khung ứng dụng đang dùng cột này để phân tách dữ liệu giữa các đơn vị. |
| 17 | Code | nvarchar(64) | Y | Cột chuẩn | Mã bản ghi do người dùng đặt. | Cột chuẩn của khung. Ở một số bảng nó mang nghĩa nghiệp vụ (xem ghi chú riêng). |
| 18 | CreateTime | datetime | Y | Cột chuẩn | Thời điểm tạo bản ghi. | Cột chuẩn; đồng thời là khoá của index index_<Bảng>_CT. |
| 19 | CreateUId | nvarchar(64) | Y | Cột chuẩn | Người tạo. | Cột chuẩn, phục vụ truy trách nhiệm. |
| 20 | UpdateTime | datetime | Y | Cột chuẩn | Thời điểm sửa gần nhất. | Cột chuẩn. Còn dùng để phát hiện hai người sửa cùng một bản ghi. |
| 21 | UpdateUId | nvarchar(64) | Y | Cột chuẩn | Người sửa gần nhất. | Cột chuẩn, phục vụ truy trách nhiệm. |
| 22 | RowStatus | nvarchar(32) | Y | Cột chuẩn | Trạng thái dòng theo quy ước khung. | Cột chuẩn có ở mọi bảng ShareData. |
| 23 | IsDelete | datetime | Y | Cột chuẩn | Xoá mềm — GHI MỐC THỜI GIAN, không phải cờ true/false. Chưa xoá = NULL. | ⚠ Khác thông lệ: kiểu datetime chứ không phải bit. Mọi bộ lọc phải viết IsDelete IS NULL, và mọi filtered index phải kèm WHERE IsDelete IS NULL. |

## ActivityLog (cột thêm)

ActivityLog (cột thêm)

[ALTER]  Ba cột bổ sung vào ShareDataActivityLog đã có.

Tại sao cần bảng này: Bảng này gánh luôn vai trò bằng chứng xuất file — tab TRANSFER đã có sẵn ByteSize / RecordCount / FilePath / Hash, và ActivityAction đã khai sẵn giá trị EXPORT.

| STT | Tên cột | Kiểu dữ liệu | Null | Nhóm | Dùng để làm gì | Tại sao cần nó |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | MappingId | nvarchar(64) | Y | Cột mới | Hồ sơ ánh xạ đã dùng để gửi. | Đối soát về sau: gói tin này được dịch theo hồ sơ nào. |
| 2 | PacketId | nvarchar(64) | Y | Cột mới | Gói tin đã gửi. | Bảng đã có DatatypeId (mã loại) nhưng chưa có bản ghi gói tin cụ thể. |
| 3 | PacketVersion | int | Y | Cột mới | Phiên bản cấu trúc thực sự đã dùng. | Khi đối tác khiếu nại 'dữ liệu thiếu field', cột này cho biết lúc đó đang chạy bản nào. |

## JSON - FieldsJson

ShareDataPacketTable.FieldsJson

Danh sách field lấy từ bảng này. Mỗi phần tử là một cột sẽ xuất hiện trong gói tin.

Tại sao để JSON thay vì bảng con: Field không bao giờ được đọc tách khỏi bảng của nó. Ngoài ra không ràng buộc DB nào bao được 'fieldKey duy nhất theo gói tin' vì field trải trên nhiều dòng PacketTable — nên để bảng riêng cũng không mua thêm gì.

| STT | Khoá JSON | Kiểu | Dùng để làm gì | Tại sao cần nó |
| --- | --- | --- | --- | --- |
| 1 | fieldKey | string | Tên field theo đặc tả gói tin, VD 'averageSpeed'. Tự sinh từ tên cột. | ⚠ Đây là khoá mà ItemsJson của Mapping trỏ vào. Phải duy nhất trong TOÀN gói tin (không phải chỉ trong một bảng) vì hai bảng có thể cùng có cột tên Name. KHÔNG được đổi khi đã có ánh xạ trỏ vào — đổi là hỏng âm thầm mọi đối tác. |
| 2 | columnName | string | Cột thật trong CSDL, VD 'AverageSpeed'. NULL nếu field tính toán hoặc chưa có nguồn. | Là thứ ghép vào câu SELECT. Phải đối chiếu INFORMATION_SCHEMA khi lưu. |
| 3 | dbType | string | Kiểu CSDL chụp lúc khai, VD 'decimal(9,2)'. | Để đối chiếu về sau khi nghi ngờ cấu trúc nguồn đã đổi. |
| 4 | name | string | Tên hiển thị tiếng Việt, VD 'Tốc độ trung bình'. | Hiển thị trên lưới và trong bản xem trước; CSDL không cung cấp thông tin này. |
| 5 | dataType | string | string \| number \| datetime \| bool. | Quyết định cách định dạng khi sinh JSON gửi đi và cách hiển thị trên lưới. |
| 6 | unit | string | Đơn vị NGUỒN, VD 'km/h'. | Là GỐC để quy đổi sang đơn vị đối tác yêu cầu. Không khai đơn vị nguồn thì không có căn cứ để đổi 60 km/h thành 16.67 m/s. |
| 7 | expression | string | Biến đổi lúc đọc, VD 'FromKmNumber*1000 + FromMetNumber'. | Áp CHUNG cho mọi đối tác — dùng khi quy chuẩn đòi dạng khác với cách CSDL lưu. |
| 8 | codeSetId | string | Trỏ tới ShareDataCodeSet.ID. | Quy đổi giá trị về mã chuẩn, áp chung mọi đối tác. NULL với field số, chuỗi tự do, thời gian. |
| 9 | defaultValue | string | Giá trị thay thế khi nguồn rỗng. | Tránh gửi đi field null với những trường đối tác không chấp nhận rỗng. |
| 10 | isRequired | bool | Bắt buộc có giá trị. | Thiếu thì KHÔNG gửi và ghi cảnh báo, thay vì gửi gói tin khuyết cho đối tác. |
| 11 | isKey | bool | Là khoá dùng để join hoặc định danh bản ghi. | Đánh dấu để người khai biết cột nào không nên bỏ khi chọn field. |
| 12 | orderNo | int | Thứ tự cột trong SELECT và trong JSON đầu ra. | Đầu ra phải ổn định thứ tự; không lưu thì phụ thuộc thứ tự trả về của CSDL. |
| 13 | status | int | 0 = đặc tả quy định nhưng CSDL CHƯA có cột · 1 = dùng thật. | Đặc tả gói tin 101 có sẵn 2 dòng '— Chưa có' (roadType, pavementType…). Cần khai để không quên, nhưng không đưa vào SELECT. Đây là lý do BỎ ràng buộc CHECK 'phải có columnName hoặc expression'. |

## JSON - ValuesJson

ShareDataCodeSet.ValuesJson

Danh sách cặp quy đổi giá trị của một bộ mã.

Tại sao để JSON thay vì bảng con: Luôn đọc cùng bộ mã, không truy vấn độc lập, mỗi bộ chỉ vài dòng.

| STT | Khoá JSON | Kiểu | Dùng để làm gì | Tại sao cần nó |
| --- | --- | --- | --- | --- |
| 1 | sourceValue | string | Giá trị trong CSDL, VD '1'. | Vế trái của phép quy đổi. Chiều INBOUND dùng NGƯỢC LẠI cặp này. |
| 2 | standardValue | string | Giá trị gửi ra, VD 'slow'. | Vế phải — giá trị theo quy chuẩn mà đối tác nhận được. |
| 3 | displayName | string | Nhãn tiếng Việt, VD 'Chậm'. | Chỉ để hiển thị trên giao diện quản trị, không gửi ra ngoài. |
| 4 | isDefault | bool | Dùng khi giá trị nguồn không khớp dòng nào. | Tránh gửi ra giá trị lạ khi CSDL xuất hiện mã mới chưa kịp khai. Chỉ nên có một dòng đặt cờ này. |
| 5 | orderNo | int | Thứ tự hiển thị. | Cho phép sắp theo nghĩa thay vì theo mã. |

## JSON - ItemsJson

ShareDataMapping.ItemsJson

CHỈ ghi những field mà đối tác này yêu cầu khác chuẩn. Field không có dòng thì gửi theo chuẩn.

Tại sao để JSON thay vì bảng con: Gói tin theo quy chuẩn nên đa số đối tác nhận đúng tên chuẩn ⇒ đối tác tuân thủ chuẩn có ItemsJson = [], giảm hẳn khối lượng khai báo và số chỗ phải sửa khi đổi chuẩn.

| STT | Khoá JSON | Kiểu | Dùng để làm gì | Tại sao cần nó |
| --- | --- | --- | --- | --- |
| 1 | fieldKey | string | Khoá trỏ vào fieldKey trong FieldsJson của gói tin. | Là sợi dây nối giữa hồ sơ đối tác và cấu trúc gói tin. Vì trỏ theo chuỗi nên phải chặn đổi tên fieldKey khi đã có ánh xạ. |
| 2 | targetEntity | string | Lệch tên thực thể phía đối tác (b → BB). NULL = dùng TargetRootEntity. | Đối tác gọi bảng khác tên, không chỉ gọi field khác tên. |
| 3 | targetKey | string | Lệch tên field (averageSpeed → vanToc). NULL = dùng fieldKey. | Đây là việc chính của tầng ánh xạ: đổi tên theo yêu cầu từng đối tác. |
| 4 | targetUnit | string | Lệch đơn vị (km/h → m/s). | Gốc quy đổi lấy từ field.unit của gói tin. Đây là 'đổi giá trị riêng theo đối tác' — không đặt được ở cấp gói tin vì mỗi đối tác đòi một đơn vị khác. |
| 5 | codeSetId | string | Lệch bộ mã — trỏ vào một ShareDataCodeSet khác bộ mã chuẩn của field. | VD chuẩn gửi 'slow' nhưng đối tác đòi 'Chậm'. Các đối tác khác không bị ảnh hưởng. |
| 6 | expression | string | Biến đổi riêng cho đối tác này. | Lối thoát cho các yêu cầu biến đổi không nằm trong đơn vị hay bộ mã. |
| 7 | defaultValue | string | Giá trị thay thế riêng cho đối tác này. | Một số đối tác chấp nhận rỗng, số khác đòi giá trị thay thế. |
| 8 | isExcluded | bool | true = đối tác này KHÔNG nhận field đó. | Không phải đối tác nào cũng được nhận đủ mọi field của gói tin. Không có cột này thì phải tạo gói tin riêng cho từng mức độ chia sẻ. |

## Index & ràng buộc

Index và ràng buộc do CSDL đảm bảo

Lưu ý: mọi filtered index đều kèm WHERE IsDelete IS NULL vì IsDelete là datetime, không phải bit.

| Bảng | Tên index / ràng buộc | Định nghĩa | Bảo đảm điều gì | Tại sao cần nó |
| --- | --- | --- | --- | --- |
| ShareDataPacket | UX_ShareDataPacket_CodeVer | UNIQUE (Code, Version) WHERE IsDelete IS NULL | Không cho tồn tại hai bản ghi cùng mã và cùng phiên bản. | Mã gói tin + phiên bản là định danh nghiệp vụ; trùng là không biết bản nào đang hiệu lực. |
| ShareDataPacketTable | UX_ShareDataPacketTable_Alias | UNIQUE (PacketId, Alias) WHERE IsDelete IS NULL | Bí danh duy nhất trong một gói tin. | Trùng alias thì câu SQL sinh ra sai hoặc mơ hồ. Để DB chặn thay vì tin vào code. |
| ShareDataPacketTable | UX_ShareDataPacketTable_Root | UNIQUE (PacketId, VariantNo) WHERE IsRoot = 1 AND IsDelete IS NULL | Đúng một bảng gốc cho mỗi (gói tin × biến thể). | Không có bảng gốc thì không dựng được FROM; có hai thì không biết chọn cái nào. Đây là ràng buộc dễ vi phạm nhất khi sửa cấu trúc. |
| ShareDataPacketTable | IX_ShareDataPacketTable_Object | INDEX (DbRef, SchemaName, TableName) | Trả lời 'bảng này đang nằm trong gói tin nào'. | Khi DBA muốn đổi hoặc xoá một bảng, cần biết trước ảnh hưởng. Chạy được nhờ đã tách 3 cột riêng thay vì gộp chuỗi. |
| ShareDataCodeSet | UX_ShareDataCodeSet_Code | UNIQUE (Code) WHERE IsDelete IS NULL | Mã bộ mã duy nhất. | Mã là thứ người khai dùng để nhận diện; trùng là nhầm. |
| ShareDataMapping | UX_ShareDataMapping_Active | UNIQUE (PartnerId, DatatypeId, Direction) WHERE IsActive = 1 AND IsDelete IS NULL | Duy nhất một hồ sơ đang dùng cho mỗi đối tác × gói tin × chiều. | Đây là khoá tra cứu lúc chạy. Hai hồ sơ cùng hiệu lực thì hệ thống không biết dùng cái nào. |
| ShareDataAlertLog | IX_ShareDataAlertLog_Unacked | INDEX (OccurredAt DESC) WHERE Acknowledged = 0 AND IsDelete IS NULL | Lấy nhanh danh sách cảnh báo chưa xử lý. | Đây là truy vấn mặc định của màn Cảnh báo & lỗi, chạy mỗi lần mở màn hình. |
| ShareDataActivityLog | IX_ShareDataActivityLog_Transfer | INDEX (PartnerId, DatatypeId, OccurredAt DESC) WHERE LogType = 'TRANSFER' | Tra nhật ký truyền nhận theo đối tác và gói tin. | Bảng này chứa cả nhật ký cấu hình lẫn truyền nhận; filtered index tách phần lớn ra. |
| (mọi bảng) | PK_<Bảng>_ID / index_<Bảng>_CT | PRIMARY KEY CLUSTERED (ID) / INDEX (CreateTime) | Khoá chính và index theo thời điểm tạo. | Đúng quy ước đặt tên của các bảng ShareData đang có. |

## Kiểm tra ở tầng code

Ràng buộc CSDL KHÔNG đảm bảo được — service phải tự kiểm

Gom vào một hàm ValidatePacketStructure() chạy trước mỗi lần lưu; nút 'Kiểm tra' trên màn Gói tin dùng lại chính hàm đó.

| Nội dung kiểm | Mức | Quy tắc | Tại sao CSDL không lo được |
| --- | --- | --- | --- |
| fieldKey duy nhất trong gói tin | error | fieldKey không trùng nhau giữa TẤT CẢ các bảng của cùng một gói tin. | Field nằm trong FieldsJson của nhiều dòng PacketTable khác nhau nên không index nào bao được cả. Trùng thì câu SELECT sinh ra có hai cột cùng alias. |
| Điều kiện join hợp lệ | error | Bảng không phải ROOT bắt buộc có joinType + joinCondition; điều kiện chỉ được trỏ vào alias đã khai PHÍA TRÊN nó. | Chống join vòng và chống trỏ vào alias chưa tồn tại. CHECK constraint chỉ kiểm được vế đầu, không kiểm được vế sau. |
| Field có nguồn | error | status = 1 thì bắt buộc có columnName hoặc expression. | Ràng buộc CHECK cũ bị bỏ vì đặc tả cho phép field 'chưa có nguồn' (status = 0). Phần kiểm còn lại chuyển sang code. |
| Đối tượng có thật | error | SchemaName / TableName / columnName phải khớp INFORMATION_SCHEMA. | Không có FK nên không gì đảm bảo bảng và cột còn tồn tại. Kiểm tại thời điểm lưu, không đợi tới lúc gửi thật. |
| Chặn đổi fieldKey | error | Không cho đổi fieldKey khi đã có ShareDataMapping tham chiếu tới nó. | ItemsJson trỏ theo chuỗi. Đổi tên là hỏng ánh xạ của MỌI đối tác — âm thầm, không báo lỗi. Cần đổi thật thì xoá field rồi tạo lại. |
| Kiểm UpdateTime khi lưu | warning | So UpdateTime client gửi lên với bản trong CSDL trước khi ghi đè. | FieldsJson là cả cụm field trong một cột; hai người sửa cùng một bảng thì người lưu sau ghi đè người trước. |
| Sinh SQL an toàn | error | Luôn QUOTENAME mọi định danh; chặn ';', '--', '/*', 'xp_', 'EXEC', 'UNION' trong joinCondition / filterExpr / expression. | Tên bảng, tên cột và biểu thức đều là chuỗi tự do được ghép vào câu lệnh. |

## Việc cần chốt

Ba việc phải chốt trước khi backend bắt tay

| Việc | Thời điểm | Vấn đề | Vì sao quan trọng / đề nghị |
| --- | --- | --- | --- |
| Nghĩa của OUTBOUND | GẤP — chặn mọi việc khác | subscription.types.ts ghi 'OUTBOUND = mình đăng ký NHẬN'; editSubscription.vue hiển thị 'OUTBOUND = Gửi đi'. Hai chỗ ngược nhau. | Chiều gửi thì đọc nguồn nội bộ, chiều nhận thì không — chọn nhầm là sai toàn bộ luồng. Cả 5 dòng dữ liệu hiện có đều OUTBOUND nên không suy ra được. Đề nghị lấy nghĩa của giao diện rồi sửa chú thích type. |
| Danh mục shareData_type | Trước khi dựng màn Gói tin | Gỡ khỏi SysConfigType hay để chỉ đọc, sau khi có ShareDataPacket? | Để cả hai cùng ghi được thì sẽ có hai danh sách gói tin lệch nhau. FE hiện gọi getConfigDataByCode(SharedataDatatype) ở nhiều chỗ, phải đổi sang API gói tin. |
| Mã DatatypeId = '983' | Trước khi seed dữ liệu | Cả 5 đăng ký hiện có đều mang mã này, không thuộc dải 101–111. | Nếu là mã thật thì phải tạo ShareDataPacket.Code = '983', nếu không 5 đăng ký đó thành mồ côi. Nếu là dữ liệu test thì dọn. |