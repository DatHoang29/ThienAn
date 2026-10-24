---
tier: A
read: full
source:
  - DocBusinessThienAn/Sharedata_23_10_1.m4a (06:24)
  - DocBusinessThienAn/Sharedata_23_10_2.m4a (08:41)
  - DocBusinessThienAn/Sharedata_23_10_3.m4a (01:44)
  - DocBusinessThienAn/Sharedata_23_10_4.m4a (04:26)
  - Ghi chú văn bản của Tech Lead kèm câu SQL tham khảo (bổ sung ngày 23-10-2026)
date: 23-10-2026
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData — Xử lý câu SQL đối tác lệch danh sách trường mẫu chung (thiếu trường, dư trường), quy trình chuẩn SQL mẫu → phân tích → danh sách trường, bố cục UI gom trên-dưới (thu gọn / popup), tìm theo đối tác & đánh dấu nguồn SQL (mẫu chung / riêng), cơ chế khóa nối đuôi (@LastKey) ghép chuỗi chống trùng thời gian vs quét định kỳ
participants:
  - Tech Lead
  - Dev
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ ShareData — Danh sách trường theo Mẫu chung, SQL riêng Đối tác & Khóa nối đuôi
### (Nguồn từ tệp ghi âm: Sharedata_23_10_1.m4a, Sharedata_23_10_2.m4a, Sharedata_23_10_3.m4a & Sharedata_23_10_4.m4a)

- **Ngày ghi nhận:** 23/10/2026
- **Thời lượng:** 21 phút 15 giây (Tổng cộng 4 phần ghi âm: 06:24 + 08:41 + 01:44 + 04:26)
- **Người tham gia:** Tech Lead, Dev
- **Chủ đề chính:**
  1. **Xử lý khi câu SQL của đối tác lệch so với mẫu chung (Thiếu trường / Dư trường):** Mẫu chung là chuẩn cố định (ví dụ 10 trường hoặc 19 trường). Khi câu SQL riêng của đối tác thiếu trường: cân nhắc giữa phương án skip (bỏ qua, không thêm vào bảng mapping) hoặc để trống (vẫn giữ trong cấu trúc trả về nhưng đánh dấu giá trị `null`). Khi đối tác dư trường: bổ sung thêm trường vào danh sách.
  2. **Quy trình chuẩn hóa: SQL mẫu chung → Bấm Phân tích → Cấu hình Danh sách trường:** Khắc phục quy trình ngược (nhập tay danh sách trường trước). Chuẩn là viết câu SQL mẫu chung, hệ thống phân tích các cột/alias trong `SELECT`, tự động sinh danh sách trường, sau đó người dùng mới vào cấu hình chi tiết (Tên hiển thị, Kiểu dữ liệu, Trạng thái bắt buộc `isRequired`). Giá trị và tên trường phải đọc tự động từ SQL ra chứ không nhập tay.
  3. **Cải tiến bố cục màn hình & Quản lý gói tin theo Đối tác:** Gom chung trên-dưới (khung SQL ở trên có nút thu gọn `collapse`, danh sách trường ở dưới rộng rãi hoặc mở popup bự để nhập). Bổ sung bộ lọc gói tin theo Đối tác để xem toàn bộ câu truy vấn của đối tác đó. Đánh dấu ký hiệu nhận diện gói tin đang dùng Mẫu chung hay Mẫu riêng; cho phép tạo mẫu riêng cho đối tác kế thừa trực tiếp từ mẫu chung.
  4. **Cơ chế Khóa nối đuôi (@LastKey / Composite Key) khi thời gian bị trùng:** Khi mốc thời gian trong bảng nguồn bị trùng lặp giữa nhiều bản ghi, không thể chỉ dùng mỗi trường thời gian. Bắt buộc ghép thêm trường phân định (như Mã làn `MSLANE`, Mã Zone, Thiết bị, Vị trí đầu/cuối tuyến) để tạo khóa chuỗi duy nhất. Đối chiếu 2 phương án: Khóa nối đuôi ghép chuỗi (`> @lastKey`) so với Quét định kỳ theo khoảng thời gian cố định (5 phút / 10 phút, chấp nhận trùng lặp bản ghi nếu cần). Kèm câu SQL mẫu tham khảo và hướng dẫn biến có nghĩa từ Tech Lead.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Xử lý khi câu SQL của đối tác lệch so với mẫu chung
- **Mẫu chung là chuẩn tham chiếu:** Danh sách trường của mẫu chung là tập thuộc tính chuẩn cố định (ví dụ 10 trường hoặc 19 trường). Mọi câu SQL riêng của đối tác đều lấy tập trường này làm cơ sở so sánh đối chiếu.
- **Xử lý trường thiếu (ví dụ đối tác chỉ có 9 / 10 trường):** Hệ thống đánh dấu trạng thái trường bị thiếu và cung cấp 2 phương án:
  - **Bỏ qua (Skip):** Không đưa trường này vào danh sách thuộc tính của bảng ánh xạ (Mapping), hoàn toàn không xuất hiện ở phía cấu hình ánh xạ.
  - **Để trống:** Vẫn duy trì trường trong cấu trúc trả về của gói tin, nhưng phía ánh xạ đánh dấu giá trị rỗng (`null`) hoặc gán giá trị mặc định.
  - *Định hướng kỹ thuật:* Ưu tiên dựa vào mẫu chung vì mẫu chung đã được thiết kế chuẩn mực và cố định đầy đủ các trường cần thiết của gói tin.
- **Xử lý trường dư (ví dụ phát sinh thêm 2 trường ngoài mẫu):** Tự động bổ sung các trường dư vào danh sách thuộc tính trích xuất. Những trường đã khớp với mẫu chung thì giữ nguyên cấu hình.
- **Đồng bộ giữa Danh sách trường và Gói tin mẫu:** Danh sách trường và mẫu luôn đi liền nhau; nếu danh sách trường chuẩn bổ sung thêm thuộc tính mới thì mẫu gói tin cũng tương ứng có thêm trường đó.

### 1.2. Thứ tự làm đúng: SQL Mẫu chung → Phân tích cột → Danh sách trường
- **Khắc phục quy trình ngược:** Trước đây do chức năng phân tích cú pháp SQL chưa hoàn thiện nên phải nhập tay danh sách trường trước rồi mới viết câu SQL sau.
- **Quy trình chuẩn hóa:**
  1. Viết câu SQL truy vấn của gói tin mẫu chung.
  2. Hệ thống phân tích cú pháp mệnh đề `SELECT` để tự động bóc tách các cột/alias đầu ra.
  3. Tự động sinh danh sách trường tương ứng với các cột trích xuất được.
  4. Người dùng chỉ cần vào danh sách này để cấu hình thuộc tính nghiệp vụ: Tên gọi hiển thị, Kiểu dữ liệu, và Trạng thái bắt buộc (`isRequired = 1/0`).
- **Nguyên tắc dữ liệu:** Tên cột và giá trị trích xuất phải được đọc tự động từ câu lệnh SQL ra, tuyệt đối không bắt người dùng nhập tay lại từ đầu. Các trường định danh cốt lõi như `ID` là bắt buộc phải có; các trường thông tin phụ (như `Name`...) có thể cấu hình linh hoạt theo từng tình huống nghiệp vụ.

### 1.3. Cải tiến bố cục màn hình, Tìm kiếm theo Đối tác & Nhận diện Mẫu chung / Mẫu riêng
- **Bố cục màn hình trên-dưới tích hợp:**
  - Gom chung khung soạn thảo SQL và Danh sách trường vào cùng một trang theo chiều dọc (trên-dưới).
  - Khung SQL ở phía trên có nút Thu gọn (`Collapse`), cho phép người dùng ẩn bớt phần SQL khi không cần xem để nhường không gian hiển thị rộng rãi cho bảng Danh sách trường ở bên dưới.
  - Đối với cấu hình danh sách trường: Hỗ trợ bung mở rộng tại chỗ hoặc mở ra một cửa sổ popup lớn để người dùng thao tác nhập liệu thoải mái.
- **Tìm kiếm gói tin theo Đối tác:**
  - Bổ sung bộ lọc chọn Đối tác ngay trên giao diện danh mục gói tin. Khi chọn một đối tác (ví dụ Đối tác A), hệ thống hiển thị ngay toàn bộ các gói tin và câu truy vấn liên quan mà đối tác đó đang đăng ký gửi hoặc đã tùy biến SQL riêng, tránh việc quản trị viên phải dò tìm thủ công từng gói.
- **Nhận diện trực quan trạng thái Mẫu chung vs Mẫu riêng:**
  - Đối với các gói tin đối tác chưa có câu truy vấn riêng, hệ thống tự động kế thừa và hiển thị nội dung câu SQL mẫu chung.
  - Trên bảng danh sách gói tin, bổ sung cột trạng thái hoặc ký hiệu icon/tag trực quan để phân biệt rõ: Gói nào đang dùng **Mẫu riêng**, gói nào đang dùng **Mẫu chung** (ví dụ Đối tác A đăng ký gói 101, 102, 108: trong đó 108 có mẫu riêng sẽ có tag đánh dấu, 101 và 102 dùng mẫu chung).
  - Cho phép người dùng bấm vào một gói đang dùng mẫu chung và chọn **"Tạo mẫu riêng"** — hệ thống tự động nhân bản nội dung từ mẫu chung sang làm câu SQL riêng cho đối tác đó để bắt đầu chỉnh sửa.

### 1.4. Cơ chế Khóa nối đuôi (@LastKey / Composite Key), Cột mốc chống trùng thời gian & Ghi chú SQL mẫu của Tech Lead
- **Vấn đề trùng mốc thời gian:** Khi quét dữ liệu tăng dần (Incremental Scan), nếu chỉ lọc theo một trường thời gian đơn lẻ (`DetectTime`, `TranTime`) thì trong thực tế CSDL có nhiều bản ghi phát sinh cùng một giây hoặc cùng một mili-giây. Nếu chu kỳ trước lưu mốc thời gian và chu kỳ sau lọc `> @lastTime`, những bản ghi còn lại có cùng mốc thời gian đó sẽ bị bỏ sót (lọt dữ liệu).
- **Giải pháp Khóa tổng hợp (Composite Key / Khóa ghép):**
  - Ghép trường thời gian định dạng cố định với một trường phân định duy nhất (như Mã làn `MSLANE`, Mã Zone, Thiết bị, hoặc Vị trí). Ví dụ: Cùng một biển số xe không thể xuất hiện đồng thời ở vị trí đầu tuyến và cuối tuyến tại cùng một thời điểm; kết hợp mốc thời gian với vị trí/mã làn sẽ tạo ra khóa định danh duy nhất cho từng bản ghi.
  - Dựng khóa nối chuỗi trong câu truy vấn: `CONVERT(varchar(23), Write_Date, 121) + '_' + MSLANE AS TAC_Key` và lọc điều kiện `> @lastKey`.
  - Khóa tổng hợp được lưu vào bảng quản lý trạng thái (`ShareDataLastSend`) cho từng cặp Đối tác × Gói tin sau mỗi lần gửi thành công để làm tham số đầu vào `@lastKey` cho chu kỳ quét tiếp theo.
- **Phương án Quét định kỳ theo khoảng thời gian cố định (Fixed Window Interval):**
  - Đối với một số bảng dữ liệu không thể ghép khóa hoặc CSDL đối tác chấp nhận nhận lại bản ghi trùng lặp: Áp dụng cơ chế quét định kỳ cố định (5 phút / 10 phút một lần) theo dải thời gian (`FromDate` – `ToDate`), không phụ thuộc vào `@lastKey`. Tùy theo nghiệp vụ cụ thể của từng gói tin và đối tác để lựa chọn phương án quét phù hợp.
- **Ghi chú bổ sung bằng văn bản và câu SQL tham khảo của Tech Lead:**

Nội dung Tech Lead đưa, chép đúng nguyên văn (giữ nguyên chữ, dấu và xuống dòng):

```sql
SELECT * FROM ( 
 SELECT TOP ({1}) 
 TID AS IDReference ,s.MSLANE AS Lane
 ,Cast(ProcessDate as Date) as ProcessDate
 
 , Ca , cast(s.GIAVE as int) AS Total,NGAYSOAT AS Date 
 , N'Tiền mặt' AS PaymentMethod ,s.MSLoaive ,RIGHT(s.MSLoaive,1) AS VehicleID ,'' as [Checker] ,SoXe_ND AS SoXe ,CONVERT(varchar(23), Write_Date , 121) +'_' +MSLANE AS TAC_Key ,'TAC_Key' AS TAC_PrimaryKey, '1' AS OPTION1,s.[Login] as SellerName 
 FROM SoatVeVangLai s
 WHERE CONVERT(varchar(23), Write_Date , 121) +'_' +MSLANE > '{0}' 
 AND GIAVE > 0
 AND NGAYSOAT >= '2025-05-10 00:00:00' AND NGAYSOAT < '2026-01-01 00:00:00.000' 
 ORDER BY NGAYSOAT 
 ) T ORDER BY TAC_Key ASC, Lane ASC
```

```sql
 WHERE CONVERT(varchar(23), Write_Date , 121) +'_' +MSLANE > '{0}'
```

```text
đó
kiểu kiểu v
r ví dụ trạm A, cần thêm là lấy DL t ừ năm 2027 thôi
thì thêm AND  NGAYSOAT >= '2027-01-01 00:00:00'
thì này là dk riêng r
hoặc lấy tự động chỉ từ đầu năm thôi
thì NGAYSOAT >= @now.year
hay kiểu gì gì đó
tùy theo thiết lập thôi
mà @.... nên là 1 tên có nghĩa
@ --->>> gán nó là cái gì
```

*(Hết nội dung Tech Lead đưa.)*

- **Phân tích kỹ thuật từ câu mẫu của Tech Lead:**
  1. *Khóa nối chuỗi chuẩn hóa:* Thời gian ghi nhận được ép kiểu định dạng cố định 23 ký tự (`yyyy-mm-dd hh:mi:ss.mmm`, chuẩn style 121 trong SQL Server) rồi nối với dấu gạch dưới `_` và mã làn `MSLANE`. Vì phần thời gian có độ dài cố định nên so sánh chuỗi theo thứ tự ASCII hoàn toàn trùng khớp với thứ tự thời gian tuyến tính; mã làn đóng vai trò phân tách khi trùng mili-giây.
  2. *Cột chỉ định khóa chính:* Trả về thêm cột hằng số `'TAC_Key' AS TAC_PrimaryKey` để tầng xử lý phía sau nhận biết chính xác cột nào đang đóng vai trò khóa quét nối đuôi.
  3. *Điều kiện riêng của đối tác:* Được viết trực tiếp vào mệnh đề `WHERE` của câu SQL riêng đối tác đó (ví dụ: `AND NGAYSOAT >= '2027-01-01 00:00:00'`), không cần tạo thêm các trường lọc rời rạc trên giao diện.
  4. *Tham số hóa có ý nghĩa:* Thay thế các placeholder kiểu cũ (`{0}`, `{1}`) bằng các biến tham số có tên định danh rõ ràng (`@lastKey`, `@pageSize`, `@yearStart`...). Các biến bắt buộc phải có quy ước rõ về nguồn gán giá trị (từ hệ thống, tính toán tự động từ ngày giờ hiện tại, hay cấu hình riêng theo đối tác).

---

## 2. Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript)

> **Ghi chú đối thoại:** Dòng thời gian dưới đây nối liền 4 phần ghi âm (`Sharedata_23_10_1.m4a` → `Sharedata_23_10_4.m4a`) thành một mạch đối thoại liên tục từ phút 00:00 đến 21:15. Giờ in đậm đầu dòng là mốc thời gian cộng dồn của toàn bộ cuộc họp; giờ in nghiêng trong ngoặc là mốc thời gian trên tệp ghi âm gốc. Các thuật ngữ kỹ thuật, tên công nghệ và danh xưng vai trò được bảo toàn nguyên văn theo quy chuẩn Tier A.

| Chặng | Tệp gốc | Thời lượng | Mốc cộng dồn | Nội dung trọng tâm |
|---|---|---|---|---|
| 1 | `Sharedata_23_10_1.m4a` | 06:24 | 00:00 – 06:24 | Xử lý lệch cột SQL riêng vs Mẫu chung (Skip / Null); Chuẩn hóa quy trình: SQL mẫu → Phân tích cột → Danh sách trường |
| 2 | `Sharedata_23_10_2.m4a` | 08:41 | 06:24 – 15:05 | Rà soát UI gói 108; Bố cục trên-dưới, nút Thu gọn (Collapse) SQL; Lọc theo đối tác & Đánh dấu Mẫu chung / Mẫu riêng |
| 3 | `Sharedata_23_10_3.m4a` | 01:44 | 15:05 – 16:49 | Bài toán trùng mốc thời gian; Ghép trường phụ (Mã làn, Mã Zone, Thiết bị) tạo Khóa tổng hợp chống sót dữ liệu |
| 4 | `Sharedata_23_10_4.m4a` | 04:26 | 16:49 – 21:15 | Thiết kế Khóa nối đuôi (@LastKey), Nối chuỗi câu WHERE; Tranh luận 2 điều kiện vs Quét định kỳ theo thời gian (5–10 phút) |

---

### [00:00 – 03:30] Xử lý Lệch Cột giữa SQL Đối tác và Mẫu chung: Thiếu trường (Skip / Null) & Dư trường

- **[00:00]** *(tệp 1 · 00:00)* **Dev:** Cho em hỏi thêm cái... Ví dụ cái danh sách trường đây có 19 trường đi, mà ở đây á mình nếu mà mong muốn hiển thị cái trường á, mình hiển thị trường nào đang dùng và trường nào không dùng luôn hay sao? Mục tiêu hôm nay là em đang tính như anh em nói...
- **[00:20]** *(tệp 1 · 00:20)* **Tech Lead:** Thì quan trọng bây giờ dùng, hôm qua mới nói là nó sẽ có trường hợp là nó khớp, đúng không, nó khớp hoặc là nó ít hơn, hoặc nó nhiều hơn, đúng không? Thì làm sao để có một cái chỗ gì đó phân biệt được là bây giờ nếu mà ít hơn thì sao? Có xóa không? Hay là giữ? Xóa ở đây mang ý nghĩa là skip á. Có nghĩa là có thể dựa vào thứ nhất là cái trường mặc định, có nghĩa là dựa vào trường mẫu chung á. Ví dụ như mẫu chung nó ra 10 trường.
- **[00:53]** *(tệp 1 · 00:53)* **Tech Lead:** Thì qua cái thằng thứ hai, thằng E, đối tác E đi, nó tạo ra một cái cấu trúc này đúng không? Ví dụ như nó có 9 trường thôi, thì lúc đó là cái dưới này nó sẽ so sánh với cái mẫu chung nè, cái thằng mẫu chung. Ví dụ ở đây nó hiện 10 trường của mẫu chung, thì lúc đó là nó sẽ có một cái trường là đánh dấu lại trạng thái là không có.
- **[01:13]** *(tệp 1 · 01:13)* **Tech Lead:** Không có đúng không? Thì bắt đầu là mình bên này có hành động là mình sẽ skip: một là skip, hai là để trống.
- **[01:24]** *(tệp 1 · 01:24)* **Tech Lead:** Ví dụ trường hợp là skip đi, thì có nghĩa là cái thằng này, dù cái tên này đi nữa, là nó không được thêm vào trong trong kia luôn, trong cái bảng của mapping đó, là không thể thêm luôn.
- **[01:39]** *(tệp 1 · 01:39)* **Dev:** Không được thêm luôn.
- **[01:41]** *(tệp 1 · 01:41)* **Tech Lead:** Ở chỗ mapping, mấy cái chỗ mà nói cột này qua cột kia đó, không hiện. Còn ví dụ như là trống đi, thì nó vẫn hiện, nhưng mà lúc đó giá trị nó bằng trống. Thì do đó nó có thể là vẫn hiện, hiện là vì qua bên kia mình sẽ map riêng. Qua bên kia mình có thể để trống hoặc là để thành giá trị mặc định hay gì đó. Thì bên này mình sẽ để là một là skip, là bỏ qua luôn; hai là mình vẫn để trong cái cấu trúc trả về, nhưng mà lúc đó là bên kia mình sẽ map, mình sẽ đánh dấu lại là cái chỗ này là giá trị nó bằng `null` này để bên kia nó biết để nó set.
- **[02:16]** *(tệp 1 · 02:16)* **Dev:** Dạ rồi, ok anh.
- **[02:18]** *(tệp 1 · 02:18)* **Tech Lead:** Đó, còn trường dư thì tương tự vậy. Đúng không? Còn khớp thì không nói. Khớp rồi thì thôi. Đó, thì thường cái mã này... cái này thì thường sẽ dựa vào cái mẫu chung là ok nhất. Tại mẫu chung là mình đã fix cố định rồi. Mình đã tạo sẵn 10 trường rồi, chắc chắn 10 trường đó phải có. Đúng không? Ví dụ như qua cái đối tác E đó nó còn 9 trường thì chắc chắn một cái trường còn lại mình phải xác định là cái gì.
- **[02:44]** *(tệp 1 · 02:44)* **Tech Lead:** Hoặc là qua bên này nó phát sinh thêm 2 trường nữa, thì 2 trường đó làm cái gì? Sẽ bổ sung thêm 2 trường đó. Cái mẫu đó là cái quan trọng nhất.

---

### [03:30 – 06:24] Chuẩn hóa Quy trình: Đi từ SQL Mẫu chung → Phân tích cột trích xuất → Danh sách trường

- **[03:33]** *(tệp 1 · 03:33)* **Dev:** Em mạn phép nha anh... Em hơi bị cấn cấn cái chỗ này. Ví dụ cái danh sách trường là đang 19 đi, thì mẫu chung này nó cũng sẽ có 19 trường đó?
- **[03:43]** *(tệp 1 · 03:43)* **Tech Lead:** Đúng rồi.
- **[03:46]** *(tệp 1 · 03:46)* **Dev:** Cho em đặt thêm câu hỏi nữa. Ví dụ như danh sách trường này nó tạo thêm một trường mới, thì bên đây mình sẽ có thêm một trường mới đó luôn?
- **[03:52]** *(tệp 1 · 03:52)* **Tech Lead:** Đúng rồi. Tại vì cơ bản á là mình đang tạo ngược, nó hơi bị ngược. Là cái thằng này nè, phải từ cái thằng này nó mới ra cái thằng này mới đúng.
- **[04:02]** *(tệp 1 · 04:02)* **Tech Lead:** Có nghĩa là từ thằng này, cái thằng chung á, là sẽ đi từ SQL. Do hôm bữa là đi hơi ngược là SQL đang skip, nên mới kêu là làm cái này trước. Nhưng mà cơ bản là thằng này phải làm trước, có nghĩa là thằng này mới ra thằng này nè, xong rồi bấm cái Phân tích, để nó ra các trường bên này. Đó.
- **[04:24]** *(tệp 1 · 04:24)* **Tech Lead:** Rồi bắt đầu là từ cái thằng này nè, danh sách này nè, là mình sẽ dựa vào để mình... mình xử lý phía sau. Còn bây giờ là mình lại đi viết trước. Là bây giờ là cái trường đó là nó sẽ đi theo đối tác, đúng không? Và cái cấu trúc mặc định. Ví dụ như cái gói này mình lúc trước là mình đã quy định là 10 trường, thì bây giờ mình viết làm sao đó để cái câu truy vấn này nó phải có 10 trường. Thì này nó phân tích được là 10 cái cột này mang ý nghĩa gì.
- **[04:52]** *(tệp 1 · 04:52)* **Tech Lead:** Còn chủ yếu là cái mục tiêu cái bên này nè, hôm bữa anh nói là điều chỉnh thiết lập mấy cái tên nè, thiết lập mấy cái kiểu. Còn cái giá trị này là phải đọc từ SQL ra, chứ không phải là mình nhập lại.
- **[05:05]** *(tệp 1 · 05:05)* **Dev:** Vậy là mấy cái này phải đọc từ SQL ra?
- **[05:07]** *(tệp 1 · 05:07)* **Tech Lead:** Đúng rồi. Chứ hôm bữa mới nói là mục tiêu là sẽ thiết lập cái tên gọi cho nó, và cái kiểu dữ liệu, cũng như là cái trạng thái dữ liệu này có bắt buộc hay không. Chứ bữa anh không có đề cập là thằng này là phải đi nhập tay. Nhưng mà do là mình chưa có làm cái chức năng này, nên thôi, tạm thời nhập tay vào thằng này, chứ không phải là mình đi khai báo thằng này trước xong rồi mình mới add câu truy vấn sau, không phải.
- **[05:35]** *(tệp 1 · 05:35)* **Dev:** Vậy nếu mà đúng quy trình nó là phải đi từ đây...
- **[05:38]** *(tệp 1 · 05:38)* **Tech Lead:** Đúng rồi! Đi từ thằng này, nó sẽ phân tích cái trường này nè để nó lưu lại. Từ cái trường này lưu lại bắt đầu mình qua đây nè, mình đi chỉnh sửa lại: cái tên nó là cái gì, cái kiểu nó là kiểu gì, rồi cái trạng thái nó có bắt buộc hay không, bắt buộc phải có hay không.
- **[05:57]** *(tệp 1 · 05:57)* **Tech Lead:** Có nghĩa là lúc có lúc không, ví dụ một số trường nó có dữ liệu, một số trường ví dụ như mấy cái trường `Name` đi nhiều tình huống là có thể có hoặc không. Nhưng mà cái trường có chắc chắn phải có, cái `ID` chắc chắn là phải có nè. Đó thì mình mới đánh dấu lại để biết.
- **[06:14]** *(tệp 1 · 06:14)* **Dev:** Dạ, ok anh. Như vậy mình đi ngược lại, sửa lại.

---

### [06:24 – 11:20] Rà soát UI: Gom bố cục trên-dưới, Thu gọn (Collapse) SQL, Popup danh sách trường

- **[06:45]** *(tệp 2 · 00:21)* **Dev:** Đang nhìn gói tin 108 nè anh... Thì cái phần cấu hình gói tin á... Ví dụ như gói 108, đối tác hiện tại nếu mà nó chưa custom mẫu riêng thì nó sẽ dùng mẫu chung. Còn ví dụ mà nó mẫu riêng... Bây giờ ví dụ như anh cần tìm đối tác A đi, nó đang xài cái nào...
- **[07:15]** *(tệp 2 · 00:51)* **Dev:** Thay vì hồi nãy em nói cần bung ra mấy cái tiêu chí... Mình chọn đối tác... Thì cái đó để em bổ sung thêm nha. Rồi ví dụ mà thằng Hiếu này nó có SQL riêng á, hôm qua mình bàn về so sánh giống như Git Change á... Đối chiếu lại... Tự lưu của nó đối với mẫu chung...
- **[07:34]** *(tệp 2 · 01:10)* **Tech Lead:** Nó có nút sao chép qua lại chưa?
- **[07:37]** *(tệp 2 · 01:13)* **Dev:** Sao chép thì chưa có nhưng mà nó chỉ có button là dùng lại mẫu chung thôi. Bây giờ ví dụ như click á... Nó bị sai rồi. Thì cái này thường cái mẫu sẽ cố định rồi, dùng cho toàn tuyến... Đây là custom riêng của nó, đây mẫu chung, bây giờ nó muốn về mẫu chung thì bấm qua đây.
- **[08:48]** *(tệp 2 · 02:24)* **Dev:** Còn cái chỗ SQL riêng này á hiện tại nếu mà có thì ví dụ gói này xài nhiều trạm... Xài nhiều trạm khác nhau...
- **[10:29]** *(tệp 2 · 04:05)* **Dev:** Không khớp thì hiện tại nó đang chỉ cảnh báo đây tạo thôi. Nó không khớp là lúc đó mình cũng không biết được là có thêm một cái cột của thằng A hay không... Không hiện ra được.
- **[10:54]** *(tệp 2 · 04:30)* **Tech Lead:** Tại vì cơ bản là thằng này nó ra là mẫu... Thì có thể xem lại cái cách bố trí của cái chỗ này nè, cho phù hợp. Ví dụ như là một cái tab lớn ở ngoài này luôn. Trong mẫu này nó có danh sách trường với lại SQL. Rồi ví dụ như đối tác A, đối tác B gì đó hiện ra tương ứng luôn... Hoặc là gom chung cũng được. Thường thì gom chung nó sẽ dễ nhìn hơn.
- **[11:23]** *(tệp 2 · 04:59)* **Tech Lead:** Gom chung trên-dưới: cái SQL với cái trường. SQL này có thể gom nhỏ lại, ví dụ như người ta không cần thì người ta bấm cái nút thu gọn á, thu nó lại, collapse lại. Thì lúc đó mình chỉ cần care chủ yếu coi ở dưới này thôi. Ở dưới này nó đỡ, nó rộng cái phần này cho mình.
- **[11:42]** *(tệp 2 · 05:18)* **Tech Lead:** Bây giờ là bê thằng này qua bên này, cấu hình nó lại cho nó khớp. Nghĩa là bê danh sách trường qua đây, có thể cấu hình dưới đây luôn đúng không? Cho nó rộng rãi.
- **[11:58]** *(tệp 2 · 05:34)* **Dev:** Dạ rồi.
- **[12:00]** *(tệp 2 · 05:36)* **Tech Lead:** Thì cái chỗ này một là bung vô hay là bung mở rộng ra. Bung mở rộng ra thì nếu mà dễ á thì làm cái nút mở popup á. Mở ra nó sẽ hiện một cái popup bự để người ta nhập. Người ta nhập làm cái gì ta? Ok thì nó fill lại vô đây. Cái này có thể làm cái nút nhỏ hơn.

---

### [11:20 – 15:05] Quản lý Gói tin theo Đối tác: Bộ lọc đối tác, Ký hiệu nhận diện Mẫu chung / Mẫu riêng & Tạo SQL riêng

- **[12:35]** *(tệp 2 · 06:11)* **Tech Lead:** Nhưng mà quan trọng là mục tiêu á là trong này phải kiếm được theo cái đối tác. Ví dụ bây giờ đang cần sửa những gói tin của đối tác A đi... Giả sử đối tác A bây giờ tất cả gói tin nó đều phải thêm một cái điều kiện gì đó, thì không thể nào mà đi ngồi mò hết mấy cái gói tin này được. Mình sẽ tìm theo đối tác A, mình bấm một cái là nó sẽ ra danh sách của đối tác A.
- **[13:00]** *(tệp 2 · 06:36)* **Dev:** Danh sách câu truy vấn của đối tác A.
- **[13:02]** *(tệp 2 · 06:38)* **Tech Lead:** Đúng rồi. Để mình điều chỉnh. Ví dụ như giờ đối tác A nó chưa có mẫu riêng đi, thì nó sẽ hiện ra cái mẫu chung, để người ta biết người ta bấm vào người ta hiển thị. Nhưng mà sẽ có thêm một cái chỗ nào đó đánh dấu lại là có sử dụng mẫu riêng hay là đang sử dụng mẫu chung.
- **[13:24]** *(tệp 2 · 07:00)* **Tech Lead:** Nghĩa là cái chỗ này thiết kế lại làm sao cho nó phù hợp một tí. Đẩy cái này qua một tí nè. Ví dụ này nó thêm một khoảng trống gì đó hay một icon biểu tượng gì đó để đánh dấu lại.
- **[13:35]** *(tệp 2 · 07:11)* **Dev:** Để có một cái trạng thái để biết nó đang dùng chung hay riêng đúng không anh?
- **[13:38]** *(tệp 2 · 07:14)* **Tech Lead:** Đúng rồi, có trạng thái để biết nó dùng chung hay riêng. Ví dụ như gói 108 có đối tác A và đối tác B đi. Thằng A nó xài mẫu riêng thì nó sẽ đánh dấu lại ở chỗ này một cái ký hiệu gì đó để biết là đang xài cái mẫu riêng rồi.
- **[13:58]** *(tệp 2 · 07:34)* **Tech Lead:** Còn ví dụ như đối tác A có sử dụng 101, 102, 108 đi. 108 có mẫu riêng, 101, 102 thì xài mẫu chung. Thì khi search cái đối tác A thì nó sẽ ra 101, 102, 108; 108 sẽ có ký hiệu đánh dấu lại là đang xài mẫu riêng. Còn 2 cái này là đang dùng mẫu chung.
- **[14:20]** *(tệp 2 · 07:56)* **Tech Lead:** Thì lúc đó ví dụ như người ta cần điều chỉnh mẫu A thì người ta có thể bấm vào 101 gì đó, người ta thiết lập, tạo mẫu riêng ví dụ như là từ mẫu chung nè, tạo mẫu riêng cho đối tác A. Bấm một cái là sảy ra một cái riêng mới. Ví dụ như vậy. Rồi sau đó chỉnh sửa ở trên cho đối tác A đó.

---

### [15:05 – 16:49] Bài toán Trùng mốc Thời gian: Ghép trường phụ (Mã làn, Mã Zone, Thiết bị) tạo Khóa tổng hợp

- **[15:05]** *(tệp 3 · 00:00)* **Dev:** Là một cái trường mình quy định...
- **[15:08]** *(tệp 3 · 00:03)* **Tech Lead:** Từ giá trị trước đó.
- **[15:10]** *(tệp 3 · 00:05)* **Dev:** Có thể là một field hoặc nhiều field kết hợp, thì theo cái cách mình kết hợp thôi. Ví dụ như ở cái trường thời gian đi... Nhưng mà một số cái dựa vào thời gian không không đủ, có trường hợp bị trùng.
- **[15:30]** *(tệp 3 · 00:25)* **Dev:** Có trường hợp là cái bảng đó, cùng thời gian đó, 2 dòng dữ liệu giống y chang nhau. Thì phải dựa theo một cái trường nào đó nữa. Ví dụ như là dùng mã làn đi. Thì tình huống là mã làn nó không bị trùng. Hoặc là mã Zone, hoặc là cái gì đó hoặc là thiết bị gì đó.
- **[15:45]** *(tệp 3 · 00:40)* **Dev:** Một cái trường gì đó để biết được là tại một thời điểm không thể nào mà 2 cái dữ liệu đó tại 2 vị trí khác nhau mà lại xảy ra được. Ví dụ như cùng biển số đó đi, không thể nào mà cùng biển số mà một chiếc xe ở vị trí đầu tuyến, vị trí cuối tuyến được, đúng không?
- **[16:03]** *(tệp 3 · 00:58)* **Tech Lead:** Ừ.
- **[16:05]** *(tệp 3 · 01:00)* **Dev:** Thì lúc đó mình sẽ biết được là 2 dữ liệu nó sẽ khác nhau. Để đảm bảo là khi mà vô tình mình lấy một cái dữ liệu, tới thằng A đó đi, thằng A thằng B nó trùng nhau, mình lấy thằng A. Tới thằng A nó lấy, nó lưu cái key thằng A. Lúc sau nó sẽ lớn hơn cái thằng A đó thì nó lấy thằng B. Chỉ lấy thằng B tiếp theo, chứ không có lấy lại thằng A.

---

### [16:49 – 21:15] Thiết kế Khóa nối đuôi (@LastKey), Nối chuỗi câu WHERE vs Phương án Quét định kỳ theo mốc thời gian

- **[16:49]** *(tệp 4 · 00:00)* **Dev:** Cái này nói á, thì em nghĩ mấy cái biến á, người dùng á nó có thể giống như mình có thể chỉnh sửa, tạo ở đây luôn hả anh?
- **[16:56]** *(tệp 4 · 00:07)* **Tech Lead:** Đúng rồi, là tự tạo ra được luôn. Còn nếu mà khó quá thì fix trên code. Cho nó fix trên code. Nhưng mà nếu mà thiết lập được thì thiết lập luôn. Ở đây giống như kiểu là kết hợp nối chuỗi á. Ví dụ như là thằng này kết hợp... thì cỡ nào thì phía trước em cũng phải thiết kế như vậy. Chứ không thể nào mà nó khác được đúng không? Em cũng phải nối chuỗi phía trước á, chứ không thể nào không nối được.
- **[17:35]** *(tệp 4 · 00:46)* **Dev:** Có thể một nhiều cách, trước em có chơi kiểu nối chuỗi... Thôi cái phần này để em nghiên cứu em làm tiếp nha anh Sơn.
- **[17:49]** *(tệp 4 · 01:00)* **Tech Lead:** Ví dụ như cái key, nó là hàm... Đó, gán vào... Thì kiểu gì là phía trước cái câu WHERE của em em cũng phải... em cũng phải nối lại chỗ này. Rồi sau đó em sinh ra... sau đó là `LastKey`. Thì về cơ bản trong `LastKey` này chính là cái thằng đó, nó cũng phải từng này format qua.
- **[18:30]** *(tệp 4 · 01:41)* **Tech Lead:** Giờ cơ bản là cái hàm bên này nè, cái hàm xử lý bên này nè, về bản chất là sẽ gán vào trong thằng này. Nhưng mà sẽ đặt nó vào trong cái bảng của mình là một cái giá trị gì đó. Cái bảng này là 101, `LastKey` gì đó, đặt một cái tên gì đó cho nó. Để khi mà nó lưu rồi nó chạy đầy đủ, chắc chắn cái giá trị này nó phải lưu lại chỗ nào đó đúng không?
- **[19:08]** *(tệp 4 · 02:19)* **Tech Lead:** Thì trong cái bảng nó sẽ lưu cái key này lại, lưu cái giá trị trước đó. Rồi lúc xử lý em lấy cái này ra.
- **[19:20]** *(tệp 4 · 02:31)* **Dev:** Hoặc là nhiều tình huống là dùng 2 điều kiện anh?
- **[19:24]** *(tệp 4 · 02:35)* **Tech Lead:** Nhưng mà dùng 2 điều kiện thường sẽ bị sai, không đúng.
- **[19:28]** *(tệp 4 · 02:39)* **Dev:** Dùng 2 điều kiện đúng mà?
- **[19:30]** *(tệp 4 · 02:41)* **Tech Lead:** Sao sai? Đúng đó... Rồi để vậy cũng đúng...
- **[19:35]** *(tệp 4 · 02:46)* **Dev:** Nếu mà xét vậy chắc chắn nó sẽ bị lệch liền...
- **[19:42]** *(tệp 4 · 02:53)* **Tech Lead:** Đó, còn có thể xem xét một số cái thì có thể... còn một số cái là fix cố định luôn, là cứ định kỳ lấy khoảng 5 phút đó. Cứ canh 5 phút là lấy thì đảm bảo là không bị lệch. Là tính theo thời gian luôn, khỏi phải tính theo cái `LastKey` này, tính theo thời gian.
- **[20:05]** *(tệp 4 · 03:16)* **Tech Lead:** Thì lúc đó mình chỉ dựa theo cái thời gian đó mình vào. Ví dụ như nói trường hợp là mình biết cái bảng đó nó không thể nào mà nó trùng được á, trùng thời gian được thì mình hãy lấy cái này. Hoặc là trong tình trạng là gửi lại được giao dịch trùng, thì mình sẽ làm cách nhanh nhất là mình cứ... ngắt định kỳ lấy theo thời gian, 5 phút là 5 phút hoặc là 10 phút là 10 phút.
- **[20:28]** *(tệp 4 · 03:39)* **Tech Lead:** Thì vô tình ví dụ như là... nó lưu cái key cuối là 101 đi. Thì lần sau nó lấy nó sẽ lớn hơn 101, thì đó, cái trường hợp này nó sẽ bị dính nè. Là 101 này nó sẽ bị mất nè, giá trị này bị mất nè, đúng không? Lấy lớn hơn key này lúc đó mình tăng, lúc đó là lấy lại cái key cũ. Thì nếu mà nó hỗ trợ lấy lại key cũ, nó chấp nhận lấy key cũ thì... đó, mình vẫn lấy được cái này. Cho nên tùy theo tình huống là nó sẽ phát sinh là... độc đắc.
