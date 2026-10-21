# Prompt: Cập nhật tài liệu 03, 06 và dữ liệu giả lập 05 để nghiệm thu trọn đợt #14–#17

> **Tệp:** `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Prompt/sharedata-cap-nhat-tai-lieu-03-06-va-du-lieu-gia-lap-05-prompt.md`
> **Ngày lập:** 20/10/2026
> **Thứ tự chạy:** **sau #17** (`sharedata-danh-sach-truong-theo-cot-sql-prompt.md`). Thứ tự cả đợt: #14 → #20 → #19 → #16 → #17 → #18. Tài liệu phải mô tả **hành vi thật sau khi các prompt trước đã chạy**.

## Bối cảnh

Người dùng yêu cầu ngày 20/10/2026: cập nhật 3 tệp để **có bước nghiệm thu tốt nhất** cho các thay đổi đợt #14–#17.

| Tệp | Vai trò |
|---|---|
| `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/doc/03-bao-cao-tong-the-entity-csdl-sharedata.md` (549 dòng) | Mô tả entity và bảng CSDL |
| `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/sql/05-sharedata-du-lieu-gia-lap.sql` (406 dòng) | Dữ liệu giả lập cho kiểm thử, **chỉ local / test** |
| `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/doc/06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md` (671 dòng) | Hướng dẫn khởi động và nghiệm thu A–J |

**Thay đổi cần phản ánh** (đọc báo cáo #14, #19, #16, #17 trong `Prompt/README.md` để biết hành vi thật):

- **#14 — tab Cấu hình SQL:**
  - nút "Gửi toàn bộ | Gửi nối đuôi";
  - bỏ menu "Chèn biến hệ thống" và ô "Giới hạn TOP".
- **#19 — thu gọn thẻ nối đuôi / gửi toàn bộ (chỉ FE; kiến trúc biến mốc trong câu SQL giữ nguyên):**
  - thẻ nối đuôi chỉ còn một hàng "Nối đuôi theo cột thời gian / khóa phụ (mặc định ID) / bỏ dữ liệu cũ hơn N ngày";
  - chọn cột là **tự áp** phần nối đuôi vào câu SQL (không còn nút "Áp vào câu SQL");
  - 4 mục kiểm tra gộp thành **một dòng trạng thái** ✓ / ✗ kèm [Sửa tự động];
  - thẻ gửi toàn bộ: số ngày + cột thời gian một hàng, tự áp `@fromDate`.
- **#16 — SQL mẫu và chế độ Trực quan:**
  - `TemplateSql` chỉ ghi khi tạo gói (`IsOnlyIgnoreUpdate`). Tạo gói ghi `TemplateSql` = `ExtractSql`, chép từ gói chuẩn cùng `OrderNo`;
  - bỏ `PacketDefaultTemplate.cs` và chức năng "Lấy SQL đã lưu làm mẫu"; menu "Mẫu SQL ▾" chỉ còn "Khôi phục SQL mẫu";
  - gói 111 có mẫu (đọc `TmsSignalLog`), vẫn tắt;
  - form tạo gói: ô "Loại gói (thứ tự)" 101–111, mặc định 101, khóa khi sửa;
  - chế độ Trực quan mở được cả 11 gói ("Nối bảng": LEFT / INNER JOIN, "Lấy 1 dòng mới nhất" cho gói 110);
  - script 02 nạp mẫu chỉ khi trống, chuẩn hóa `OrderNo`; `@GhiDeMauGoiTin = 1` là cách duy nhất làm mới `TemplateSql`.
- **#17 — danh sách trường:**
  - tạo gói không tự tạo trường; khóa trường chỉ chọn từ cột SQL (chọn nhiều, "Chọn tất cả"), sửa thì khóa chỉ đọc;
  - cột không khai trường thì **không gửi**; trường có khai mà SQL không trả thì gửi **null**, có thẻ "Không có trong SQL, gửi đi sẽ rỗng" (đỏ nếu bắt buộc);
  - bỏ dải đối chiếu và API `SyncExtractColumns`;
  - tab Cấu hình SQL có dòng ⚠ / ⓘ khi lệch; màn Ánh xạ có hướng dẫn khi gói chưa có trường;
  - API mới trả cột SQL cho tab Danh sách trường (không cần SuperAdmin).

## Hiện trạng 3 tệp (đọc ngày 20/10/2026)

### `doc/03` — chỗ đã cũ hoặc lệch

- **Mục III.1 `ShareDataPacket`** (dòng 110–132):
  - bảng cột **không có `TemplateSql`**;
  - `OrderNo` ghi "Thứ tự sắp xếp" (nay là **loại gói 101–111**);
  - `ExtractSql` ghi "Câu truy vấn… mẫu chuẩn" (nay là **SQL đang chạy**);
  - vai trò ghi gói 111 là `111_regionalCommand`, trong khi script 02 tạo gói 111 với mã `111`.
- **Mục III.2 `ShareDataPacketField`** (dòng 134–150):
  - vai trò ghi "hiển thị danh sách trường nguồn trên màn Ánh xạ và Query Builder", chưa có quy tắc khóa lấy từ cột SQL;
  - `DatatypeId` ghi "`ShareDataPacket.Code`", trong khi mã tra theo cả `ID` lẫn `Code`, và trường mới lưu `packet.ID`.
- **Mục II** (bảng đối chiếu, khoảng dòng 84): dòng `ShareDataPacket` chưa nói `TemplateSql`.
- **Mục IV.3** (dòng 417): "Source Tables 101–110", chưa có `TmsSignalLog` cho gói 111.
- **Mục IV.4** (dòng 430–531): danh mục `ShareDataSourceTable`. Nếu #16 có thêm `TmsSignalLog` vào script 03 thì phải bổ sung dòng tương ứng.
- **Mục V** (dòng 532–549): 5 quy tắc, chưa có quy tắc về `TemplateSql` và khóa trường.

### `sql/05` — dữ liệu giả lập hiện có

- **Khối 1:** gói 104 (`TmsWeather`).
- **Khối 2:** gói 106 (`TmsTrafficData` WIM).
- **Khối 3:** gói 110 (`TmsIncident`, `TmsEquipment`, `VmsCurrent`, `TmsWeather`).
- **Khối 4:** chiều nhận E0. Bảng đích `InboundTrafficFlow` trên CSDL Inbound; gói **`E2E_INBOUND_TEST`** (dòng 303–345) với **`OrderNo = 900`**, `ExtractSql = NULL`; câu ghi MERGE.
- Mọi dòng tạo ra mang `CreateUId = N'SHAREDATA-SEED-GIA-LAP'`; chạy lại nhiều lần an toàn.
- **Thiếu:**
  - dữ liệu cho gói 111;
  - dữ liệu để kiểm "nhiều dòng cùng thời điểm vượt qua ranh giới trang" (vai trò của `@lastKey`);
  - dữ liệu để kiểm "dữ liệu cũ hơn số ngày quét lùi không gửi";
  - cách dọn dữ liệu giả lập.
- **Mâu thuẫn với #16:**
  - `OrderNo = 900` nằm ngoài 101–111;
  - `ExtractSql = NULL` khiến màn hình hiện cảnh báo "Gói chưa có câu SQL".

### `doc/06` — chỗ đã cũ

- **Mục 3.1 Bước 2** (dòng 146–167):
  - hướng dẫn nhập mã chuẩn và tự gõ "Số thứ tự 101..111";
  - nhắc `PacketDefaultTemplate.TryGet`, nút "Mẫu mặc định", nút "Đồng bộ vào Gói tin" (`syncToFields`).
  - **Chưa có bước khai danh sách trường**, trong khi Ánh xạ cần trường.
- **Mục 3.1 Bước 3** (dòng 169–187): Ánh xạ, chưa nói phải có trường trước.
- **Mục A** (dòng 428–440):
  - A3 tự gõ `TOP (@pageSize)`, `__lastTime`, `__rowid`;
  - A5 dùng "Mẫu mặc định";
  - A7 "tạo gói mã chuẩn" theo tiền tố mã.
- **Mục B** (dòng 442–459): B6 "Mẫu mặc định" khôi phục "theo mã gói từ Catalog"; chưa có bước cho chế độ Trực quan với JOIN / gói 110.
- **Mục D** (dòng 476–497): chưa có bước kiểm `@lastKey` (nhiều dòng cùng thời điểm) và số ngày quét lùi.
- **Mục G** (dòng 533–544): G4 "Xem gói 111: tắt, không chạy". Chưa nói gói 111 đã có SQL mẫu.
- **Chưa có nhóm nghiệm thu cho danh sách trường ↔ SQL (#17).**
- **Lộ trình nhanh** (dòng 11–20), mục 6.3 (dòng 648–660), 6.4 (dòng 664–671): còn ghi A–J.

## Phần chung (bắt buộc đọc)

- **Repo:** cha `ThienAn` ở `main`. Chỉ sửa 3 tệp trên. Không sửa mã FE / BE.
- **Điều kiện bắt đầu:** dòng #14, #19, #16, #17 trong `Prompt/README.md` đều ✅. Chưa đủ thì **dừng**.
- **Đọc trước:**
  - báo cáo #14, #16, #17 trong README;
  - `.agents/rules/documentation-standards_thienan.md`;
  - `.agents/rules/thienan_rules.md`;
  - `.agents/rules/database-sql_thienan.md` nếu có;
  - `doc/02-mapping-goi-tin-101-111.md`;
  - `doc/05-chay-sql-tu-dau-sharedata.md` (Phụ lục C);
  - `sql/02-sharedata-seed-goi-tin.sql`, `sql/03-sharedata-seed-cau-hinh.sql` **bản sau #16**.
- **Nguyên tắc viết:**
  - **Mọi câu mô tả phải khớp mã thật sau #14–#17.** Đối chiếu bằng Gortex `search_text` / `read_file` trên mã và i18n `vi-vn.json`. Nhãn nút, tiêu đề tab viết **đúng chữ hiện trên giao diện**. Không chép nguyên văn từ prompt nếu mã đã làm khác; chỗ khác thì ghi vào báo cáo.
  - #16, #17 có mục "Việc cuối" đã sửa một phần doc 03 / 06. **Đọc lại trước, gộp cho thống nhất**, không viết trùng hai đoạn cùng ý.
  - Giữ khuôn trình bày hiện có của từng tệp: bảng "# | Bước | Kết quả mong đợi | Kết quả", ô "☐ Đạt ☐ Không đạt", tiêu đề có biểu tượng.
  - Hạn chế thay đổi: chỉ sửa đoạn liên quan, không viết lại phần không đổi.
  - Ghi ngày cập nhật "20/10/2026" ở các mục sửa, theo cách tệp đang ghi.
- **Không làm:**
  - ⛔ **Không chạy script lên CSDL**; người dùng tự chạy `05`.
  - Không commit.
  - Không ghi tên prompt vào tài liệu (Rule 19.80).

## A. `sql/05-sharedata-du-lieu-gia-lap.sql`

**Quy ước chung cho các khối mới:**
- Giữ khuôn hiện có: `@SeedUser = N'SHAREDATA-SEED-GIA-LAP'`; kiểm bảng tồn tại bằng `OBJECT_ID`; chống trùng bằng `IF NOT EXISTS` theo khóa nghiệp vụ hoặc `CreateUId`; `PRINT` kết quả từng khối.
- **Kiểm cấu trúc thật** của từng bảng trước khi viết `INSERT` (entity trong `Modules.TMS.Core/Entities/…` và `01-sharedata-cau-truc.sql` / CSDL local): kiểu `ID`, cột bắt buộc. Không đoán.
- Mỗi khối mới ghi rõ trong chú thích: phục vụ bước nghiệm thu nào trong doc 06 (theo mã ở mục C).

1. **Sửa khối 4.2.b, gói `E2E_INBOUND_TEST`:**
   - `OrderNo` từ `900` thành **`101`**, vì gói nhận dữ liệu cùng cấu trúc luồng giao thông của gói 101;
   - khi thêm mới: `TemplateSql` = `ExtractSql` = `TemplateSql` của gói chuẩn `OrderNo = 101` (`IsDelete IS NULL`, tạo sớm nhất), đúng mô hình #16;
   - **thêm đoạn sửa bản ghi đã có**: `OrderNo <> 101` thì gán 101; `TemplateSql` / `ExtractSql` đang trống thì chép như trên. Đây là SQL thuần nên được ghi `TemplateSql` (ngoại lệ có chủ đích, ghi chú rõ: chỉ điền khi trống);
   - giữ chú thích "gói kiểm thử chiều nhận, không đăng ký chiều gửi".
2. **Khối mới 5 — gói 111 (`TmsSignalLog`):**
   - 3 dòng lệnh điều phối, thời điểm trong vòng 1 ngày gần nhất, `NewData` dạng JSON ngắn (ví dụ `{"speedLimit":60}`), `State` khác nhau (ví dụ `executed`, `pending`, `failed`), `IsDelete = NULL`;
   - phục vụ: "Chạy thử" SQL gói 111 ra dữ liệu; chế độ Trực quan gói 111.
3. **Khối mới 6 — gói 103, nhiều dòng cùng thời điểm vượt ranh giới trang** (kiểm `@lastKey`):
   - **150 dòng** `TmsTrafficData` cùng `DetectTime` / `CreateTime` / `UpdateTime` = một mốc cố định, làm tròn tới giây, nằm trong cửa sổ quét lùi (ví dụ `DATEADD(MINUTE, -30, GETDATE())`);
   - `LicensePlate` = `LKSAME-001` … `LKSAME-150`;
   - `ID` sao cho thứ tự `ID` tăng dần trùng thứ tự biển số (ví dụ tiền tố cố định + số đệm 3 chữ số), vì câu mẫu 103 so `td.ID > @lastKey`;
   - `EquipmentId` lấy thiết bị VDS có sẵn hoặc thiết bị giả lập ở khối 2;
   - biến đầu khối `@SoDongCungMoc INT = 150`, chú thích: "phải lớn hơn cỡ trang của Worker (mặc định 100, cấu hình ở `SysOpConfig` nhóm `sharedata_config`)".
4. **Khối mới 7 — gói 103, dữ liệu cũ hơn số ngày quét lùi:**
   - 3 dòng `TmsTrafficData`, thời điểm = `DATEADD(DAY, -3, GETDATE())`, `LicensePlate` = `LKOLD-001` … `LKOLD-003`;
   - chú thích: "gói 103 mặc định quét lùi 1 ngày; đăng ký mới không được gửi 3 dòng này".
5. **Khối mới 8 — dọn dữ liệu giả lập:**
   - cờ `@XoaDuLieuGiaLap BIT = 0` ở đầu khối. `= 1` thì **xoá cứng** các dòng do script này tạo (`CreateUId = @SeedUser`) trong các bảng nguồn TMS / VMS của khối 1, 2, 3, 5, 6, 7 và bảng đích `InboundTrafficFlow`;
   - **không đụng** `ShareDataPacket`, `ShareDataPacketSql`, đăng ký, nhật ký;
   - in số dòng xoá từng bảng;
   - chú thích cảnh báo: chỉ local / test.
6. **Đầu tệp:**
   - cập nhật phần mô tả liệt kê khối 1–8;
   - "CẬP NHẬT LẦN CUỐI: 20/10/2026";
   - nhắc: khi Worker đang chạy, chèn vào bảng đang bật Change Tracking sẽ kích hoạt gửi ngay.
7. **Kiểm cú pháp không cần chạy lên CSDL:**
   - viết một test xUnit **tạm** (hoặc dùng sẵn `SeedScriptReader` / ScriptDom như test script 02 đang làm) parse `05` bằng `TSql160Parser`, 0 lỗi;
   - **gỡ test tạm** trước khi xong nếu không giữ được theo quy ước test. Nếu repo đã có test parse các script SQL thì thêm `05` vào đó và giữ lại.

## B. `doc/03-bao-cao-tong-the-entity-csdl-sharedata.md`

1. **Mục III.1 `ShareDataPacket`:**
   - thêm dòng `TemplateSql` (`nvarchar(max)`, Nullable): "SQL mẫu của gói. Chỉ ghi khi tạo gói (`IsOnlyIgnoreUpdate`), dùng cho 'Khôi phục SQL mẫu'. Không đổi qua ứng dụng; chỉ script 02 với `@GhiDeMauGoiTin = 1` làm mới được.";
   - sửa `OrderNo`: "Loại gói 101–111 theo đặc tả 02; chọn khi tạo, không đổi sau đó; quyết định SQL mẫu";
   - sửa `ExtractSql`: "SQL đang chạy, Worker dùng; ghi đè khi Lưu trên tab Cấu hình SQL";
   - mục "Lưu ý kỹ thuật" thêm:
     - tạo gói thì `TemplateSql` = `ExtractSql`, chép từ gói chuẩn cùng `OrderNo`;
     - không còn mẫu viết cứng trong mã;
   - sửa tên gói 111 theo đúng mã trong script 02 (đối chiếu Phụ lục C doc 05; lệch thì theo script và ghi vào báo cáo).
2. **Mục III.2 `ShareDataPacketField`**, viết lại phần vai trò:
   - khóa (`AliasFieldKey`) **chỉ chọn từ cột của câu SQL**, không đổi sau khi tạo;
   - bảng **chỉ dùng lúc cấu hình** (màn Ánh xạ, tab Danh sách trường); **Worker không đọc**;
   - cột SQL không khai trường thì **không gửi**; trường không có cột thì gửi **null** (hoặc giá trị mặc định ở Ánh xạ), trường bắt buộc rỗng thì báo ESH-1202;
   - `DatatypeId`: ghi đúng thực tế (trường mới lưu `ShareDataPacket.ID`; mã tra theo cả `ID` và `Code`).
3. **Mục II:** dòng `ShareDataPacket` bổ sung `TemplateSql`.
4. **Mục IV.3:** "101–111"; thêm `TmsSignalLog` cho gói 111. **Mục IV.4:** thêm dòng `TmsSignalLog` nếu script 03 sau #16 có.
5. **Mục V:** thêm 2 quy tắc:
   - **6. SQL mẫu bất biến:** chỉ ghi khi tạo gói; ⛔ không cập nhật `TemplateSql` bằng `SetColumns` hay SQL thuần trong mã ứng dụng.
   - **7. Khóa trường theo cột SQL:** khóa chọn từ cột SQL, không sửa; muốn đổi khóa thì sửa bí danh trong SQL rồi khai trường mới.

## C. `doc/06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md`

1. **Mục 1 / 3** (điều kiện tiên quyết): thứ tự script `00 → 01 → 02 → 03 → 04 → 05`. Sau script 02 đã có sẵn 11 gói chuẩn kèm SQL.
2. **Mục 3.1 Bước 2**, viết lại:
   - 11 gói chuẩn do script 02 tạo. Bước này là **kiểm** 11 gói có trong danh sách, mỗi gói mở tab Cấu hình SQL thấy ngay câu SQL; gói 111 ở trạng thái tắt;
   - tạo gói mới (chỉ khi cần): ô "Loại gói (thứ tự)" chọn 101–111, mặc định 101 → gói có SQL ngay;
   - bỏ mọi nhắc tới `PacketDefaultTemplate`, nút "Mẫu mặc định", "Đồng bộ vào Gói tin";
   - nút khôi phục ghi đúng tên mới.
3. **Thêm Bước 2b — Khai danh sách trường** (bắt buộc trước Bước 3):
   - tab Danh sách trường → Thêm → chọn cột (chọn nhiều, "Chọn tất cả") → Xác nhận → sửa tên hiển thị, kiểu, bắt buộc;
   - **liệt kê đúng các khóa mà ánh xạ D0 (gói 101, 103) và E0 cần**, đọc từ khuôn JSON ở mục 3.2;
   - nhắc: cột không khai trường thì không gửi.
4. **Mục 3.1 Bước 3:** thêm "gói phải có trường; chưa có thì màn Ánh xạ hiện hướng dẫn".
5. **Mục A**, sửa và thêm, giữ đánh số liền mạch:
   - **A3:** đổi gói 105 sang "Gửi nối đuôi" → nếu xác định được cột thì tự áp, nếu không thì dòng ✗ → chọn cột thời gian (khóa phụ tự là ID) → ô SQL tự có `TOP (@pageSize)`, điều kiện con trỏ, `ORDER BY` → dòng ✓ "Sẵn sàng gửi nối đuôi" → Lưu được.
   - **A5:** về "Gửi toàn bộ" → cảnh báo phần nối đuôi → "Gỡ phần nối đuôi" (hoặc "Mẫu SQL ▾ → Khôi phục SQL mẫu") → Lưu.
   - **A7:** tạo gói chọn loại 105 → mở tab SQL thấy câu mẫu 105, chế độ và số ngày quét lùi theo gói chuẩn.
   - **A8 (mới):** sửa SQL gói vừa tạo → Lưu → "Khôi phục SQL mẫu" vẫn đưa về câu lúc tạo.
   - **A9 (mới):** mở form sửa gói → ô "Loại gói" bị khóa.
   - **A10 (mới, tùy chọn, SQL người dùng tự chạy):** so `TemplateSql` trước / sau khi sửa gói và Lưu SQL → không đổi.
   - **A11 (mới):** xoá `ORDER BY` trong ô SQL của gói nối đuôi → dòng ✗ nêu "thiếu sắp xếp theo thời gian" → [Sửa tự động] → dòng ✓.
6. **Mục B:**
   - **B3:** giữ (bỏ `__lastTime` hoặc `__rowid` → bị từ chối), nói rõ thao tác ở chế độ SQL;
   - **B4:** giữ (đổi `ORDER BY … DESC` → Lưu bị từ chối); thêm: thẻ nối đuôi hiện ✗ và [Sửa tự động] đưa về tăng dần;
   - **B6:** "Khôi phục SQL mẫu" đọc `TemplateSql` của chính gói;
   - **thêm:**
     - **B13:** mở lần lượt 11 gói ở chế độ Trực quan, không gói nào bị khóa; gói 101 thấy 2 dòng "Nối bảng", gói 110 thấy 3 dòng "Lấy 1 dòng mới nhất";
     - **B14:** thêm / xoá một bảng nối, "SQL được tạo" đổi theo;
     - **B15:** dán câu tự viết có `UNION` → không chuyển được sang Trực quan, thông báo dễ hiểu;
     - **B16:** không còn menu "Chèn biến hệ thống"; dải "Biến đang dùng": tooltip ghi đúng giá trị khi chạy thử (số dòng mỗi lượt = `SqlSampleTop`) và khi gửi thật.
7. **Mục D, thêm** (dùng dữ liệu khối 6, 7 của script 05):
   - **D15:** đăng ký mới gói 103 → toàn bộ `LKSAME-001…150` được gửi **đúng một lần**, không sót, không trùng (kiểm `received-*.json` của receiver hoặc câu SQL đếm theo nhật ký). Ghi rõ cách kiểm.
   - **D16:** trong cùng đăng ký, `LKOLD-001…003` **không** được gửi.
8. **Mục G:** **G4** đổi thành "gói 111 đã có SQL mẫu, vẫn tắt: Chạy thử ra dữ liệu `TmsSignalLog` (khối 5 script 05); không gửi, không cảnh báo".
9. **Thêm mục K — Danh sách trường và câu SQL** (đặt sau J, cùng khuôn bảng):

   | # | Bước | Kết quả mong đợi |
   |---|---|---|
   | K1 | Gói 101 khai đúng 1 trường (ví dụ `zoneName`) → Ánh xạ | Chỉ chọn được `zoneName`; dữ liệu gửi đi chỉ có khóa này (ngoài `$meta`) |
   | K2 | Sửa SQL bỏ cột của một trường đã khai → Lưu | Tab SQL hiện ⚠; tab Danh sách trường có thẻ "Không có trong SQL…"; lượt gửi kế tiếp khóa đó **null** |
   | K3 | Như K2 với trường bắt buộc | Thẻ / thông báo đỏ; lượt gửi báo thiếu trường bắt buộc (ESH-1202), mốc không tiến |
   | K4 | Thêm trường | Ô khóa chỉ chọn được cột SQL chưa khai; "Chọn tất cả" chọn đủ; mở sửa thì ô khóa chỉ đọc |
   | K5 | Gói chưa có trường → mở Ánh xạ | Hiện hướng dẫn khai trường |
   | K6 | Tài khoản không phải SuperAdmin vào tab Danh sách trường | Thêm trường bằng chọn cột được; không thấy câu SQL |
   | K7 | Tab SQL có cột chưa khai trường | Dòng ⓘ xám (không phải cảnh báo); [Khai thêm trường] mở form với cột chọn sẵn |

   Gói thử K2 / K3 dùng gói tự tạo, ví dụ `101_k_test`. Ghi bước dọn ở mục 6.
10. **Lộ trình nhanh, mục 6.3, 6.4:**
    - "A–J" thành "A–K";
    - 6.4 thêm tiêu chí nhóm K;
    - 6.3 cập nhật nếu có kịch bản tùy chọn mới;
    - **mục 6 dọn dẹp:** thêm bước chạy khối 8 script 05 với `@XoaDuLieuGiaLap = 1` để dọn dữ liệu giả lập.

## D. Điểm dừng

1. #14, #16 hoặc #17 chưa ✅ → dừng.
2. Hành vi thật trong mã khác với mô tả ở phần Bối cảnh (ví dụ nhãn nút khác, chức năng chưa làm) → **viết theo mã thật**, ghi chỗ lệch vào báo cáo. Chức năng chưa làm thì **không** viết bước nghiệm thu cho nó; ghi vào báo cáo.
3. Cấu trúc `TmsSignalLog` / `TmsTrafficData` không cho chèn như mục A (cột bắt buộc lạ, `ID` kiểu khác) → dừng và hỏi.
4. Không xác định được các khóa trường mà D0 / E0 cần → dừng và hỏi.

## Báo cáo

- `git diff --stat` repo cha (chỉ 3 tệp).
- Danh sách mục đã sửa / thêm ở từng tệp.
- Chỗ lệch giữa phần Bối cảnh và mã thật (nếu có).
- Kết quả parse script 05 (0 lỗi), kèm lời nhắc **người dùng tự chạy** script 05.
- Danh sách khóa trường cần khai cho D0 / E0 (mục C.3).

## Việc cuối — Cập nhật lại tài liệu gốc

- `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/sql/README.md`: mô tả script 05 có thêm khối 5–8 (gói 111, nhiều dòng cùng mốc, dữ liệu cũ hơn số ngày quét lùi, cờ dọn dữ liệu) và gói `E2E_INBOUND_TEST` dùng `OrderNo = 101`.
- `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Plan/Sharedata_MasterPlan.md`: mục kết quả nghiệm thu (`DQ-TEST-3`) ghi nhóm K mới.
- `Prompt/README.md`: cập nhật trạng thái dòng của prompt này.
