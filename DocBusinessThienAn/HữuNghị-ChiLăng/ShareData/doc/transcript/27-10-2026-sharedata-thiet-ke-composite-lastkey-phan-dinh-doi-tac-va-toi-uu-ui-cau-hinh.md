---
tier: A
read: full
source:
  - DocBusinessThienAn/Sharedata_27_10_01.m4a (02:40)
  - DocBusinessThienAn/Sharedata_27_10_02.m4a (10:53)
  - DocBusinessThienAn/Sharedata_27_10_03.m4a (06:39)
  - DocBusinessThienAn/Sharedata_27_10_04.m4a (04:50)
  - DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/_source/50d2fc5f676ae734be7b.jpg (Bản vẽ tay kỹ thuật 1)
  - DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/_source/b7af82ba478cc7d29e9d.jpg (Bản vẽ tay kỹ thuật 2)
date: 27-10-2026
model: Gemini Multimodal Native Audio & Vision Transcribe
status: verified
topic: ShareData — Thiết kế Khóa Nối đuôi Composite Key (@LastKey / DetectKey), Cơ chế Lưu trữ Offset theo Đối tác (Chống Xung đột Mẫu chung & Xử lý Crash), Tối ưu Hiệu năng Load Ban đầu Giao diện Nguồn Dữ liệu, Đổi tên Nút "Tạo cấu hình mới/riêng", Bố cục Khối So sánh Diff, và Chuẩn hóa Kiểu Dữ liệu Gói tin
participants:
  - Tech Lead
  - Dev
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ ShareData — Thiết kế Khóa Nối Đuôi Composite Key, Lưu Trữ Offset Phân Định Theo Đối Tác & Hoàn Thiện Giao Diện Nguồn Dữ Liệu
### (Nguồn từ chuỗi 4 tệp ghi âm Sharedata_27_10_01..04.m4a kết hợp 2 Bản vẽ tay kỹ thuật 50d2fc5f676ae734be7b.jpg và b7af82ba478cc7d29e9d.jpg)

- **Ngày ghi nhận:** 27/10/2026
- **Thời lượng tổng:** 25 phút 02 giây (toàn văn 4 tệp audio: 02:40 + 10:53 + 06:39 + 04:50)
- **Tài liệu trực quan đính kèm:**
  - Bản vẽ tay 1 (Nguyên lý `@lastkey = CONCAT(...)` & Vạch cắt nối đuôi): [50d2fc5f676ae734be7b.jpg](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/H%E1%BB%AFuNgh%E1%BB%8B-ChiL%C4%83ng/ShareData/_source/50d2fc5f676ae734be7b.jpg)
  - Bản vẽ tay 2 (So sánh 2 cách sinh `DetectKey`, `To order by`, Bảng lưu trữ theo Đối tác & Crash): [b7af82ba478cc7d29e9d.jpg](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/H%E1%BB%AFuNgh%E1%BB%8B-ChiL%C4%83ng/ShareData/_source/b7af82ba478cc7d29e9d.jpg)
- **Người tham gia:** Tech Lead, Dev
- **Chủ đề chính:**
  1. **Tối ưu Hiệu năng Load Ban đầu Màn hình Nguồn Dữ liệu (Lazy Load UI):** Tránh việc tải đồng loạt toàn bộ dữ liệu chi tiết của dòng đầu tiên khi người dùng vừa mở trang. Giữ khung hiển thị (skeleton layout), chỉ nạp dữ liệu chi tiết khi người dùng chủ động nhấp chọn đối tác hoặc gói tin cụ thể.
  2. **Chuẩn hóa Tên Nút & Hành Động Kế Thừa Cấu Hình Đối Tác:** Đổi tên nút "SQL riêng" thành "Tạo cấu hình mới" hoặc "Tạo cấu hình riêng" để phản ánh đúng bản chất nghiệp vụ; nút Lưu đổi thành "Lưu & phân tích". Hỗ trợ 2 phương thức tạo cấu hình cho đối tác mới: sao chép từ mẫu chung hoặc clone từ cấu hình của một đối tác đã có trước đó.
  3. **Hoàn thiện Khối So Sánh Khác Biệt (Diff / Monaco Compare):** Hạ vị trí khối so sánh Diff xuống khu vực dưới khung soạn thảo SQL/danh sách trường để người dùng dễ quan sát trực tiếp, không phải cuộn trang lên xuống liên tục. Gom các icon chỉ thị trạng thái thành một dải gọn gàng và bổ sung chức năng lọc "Chỉ hiện dòng khác biệt".
  4. **Rút Gọn & Tinh Tế Hóa Danh Mục Kiểu Dữ Liệu (Data Types):** Giảm tải danh sách kiểu dữ liệu quá rườm rà trên giao diện gói tin (hiện có `Guid`, `Float`, `Double`, `Decimal`, `Long`...). Thống nhất quy về các nhóm kiểu cốt lõi trong JSON: `string`, `number` (số), `boolean`, và `datetime` (chuỗi định dạng thời gian).
  5. **Giải Mã Bản Vẽ Kỹ Thuật b7af82ba478cc7d29e9d.jpg — Cơ Chế Khóa Nối Đuôi Composite Key (@LastKey / DetectKey):**
     - Đánh giá 2 phương án sinh khóa nối đuôi ghép chuỗi: Phương án 1 (SQL CONCAT / Select nối chuỗi) vs Phương án 2 (C# Worker xử lý ghép chuỗi `dl.Time.ToString("HHmm") + "_" + Code`).
     - Xác định mệnh đề truy vấn: Trường khóa nối đuôi bắt buộc phải xuất hiện đồng thời trong cả mệnh đề `WHERE` (`WHERE DetectKey > @LastKey`) và mệnh đề sắp xếp (`ORDER BY DetectKey ASC`).
  6. **Kiến Trúc Bảng Lưu Trữ Trạng Thái Offset ("Tên khóa lưu trữ"):**
     - Bắt buộc cấu trúc khóa trạng thái phải bao gồm cả `Mã gói` và `Đối tác` (`PartnerId`). Tuyệt đối không lưu mốc chạy thật ở cấp mẫu dùng chung vì sẽ gây xung đột dữ liệu (race condition / data collision) giữa các đối tác dùng chung mẫu.
     - Cơ chế xử lý khi Worker bị Crash hoặc dừng đột ngột giữa chừng: Giữ nguyên mốc `LastKey` cũ, chỉ commit mốc mới khi toàn bộ batch dữ liệu đã được xử lý trọn vẹn.
     - Phân định rạch ròi giữa tham số cố định (`PageSize = 100`) và tham số biến đổi theo lần chạy (`DetectKey` / `@LastKey`).

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Giải Mã Toàn Diện 2 Bản Vẽ Kỹ Thuật (50d2fc5f676ae734be7b.jpg & b7af82ba478cc7d29e9d.jpg)

Hai bản vẽ tay tại [50d2fc5f676ae734be7b.jpg](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/H%E1%BB%AFuNgh%E1%BB%8B-ChiL%C4%83ng/ShareData/_source/50d2fc5f676ae734be7b.jpg) và [b7af82ba478cc7d29e9d.jpg](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/H%E1%BB%AFuNgh%E1%BB%8B-ChiL%C4%83ng/ShareData/_source/b7af82ba478cc7d29e9d.jpg) được Tech Lead trực tiếp phác thảo (trong phần 3 và phần 4 của cuộc họp: `Sharedata_27_10_03.m4a` và `Sharedata_27_10_04.m4a`), ghi nhận trực quan toàn bộ bài toán và lời giải cho cơ chế khóa nối đuôi (Incremental Fetching / Offset Tracking).

```
      BẢN VẼ TAY 1 (50d2fc5f676ae734be7b.jpg): NGUYÊN LÝ @lastkey & VẠCH CẮT
 
   @lastkey  =  CONCAT ( ... )  --->  [ Key A ] duy nhất
 
   +----------+-----------+-------+---------------+
   | gói tin  |  đối tác  |  key  |     Value     |
   +----------+-----------+-------+---------------+
   |   ...    |    ...    |   x   |    1004_C     |
   +----------+-----------+-------+---------------+
 
   DL:
     10:00  A   1000_A
     10:30  B   1030_B          select > 1040_C
     10:40  C   1040_C
   ------------------- (Vạch cắt đợt quét 1) -------------------
     10:40  C
     10:50  D

=========================================================================

      BẢN VẼ TAY 2 (b7af82ba478cc7d29e9d.jpg): DETECTKEY, ORDER BY & LƯU TRỮ
 
 [BẢNG DỮ LIỆU NGUỒN]
  +------+-------+----------+
  |  DL  | Code  |   Time   |
  +------+-------+----------+
  |  1   |   A   |  10:00   |
  |  2   |   B   |  10:05   |
  |  3   |   C   |  10:10   |
  +------+-------+----------+
 
  Điều kiện lọc:
  Where HHmmss_Code > [trước đó]
 
 -------------------------------------------------------------------------
 
  1) Lấy DL (To order by) — Nối chuỗi dưới SQL:
  +------+-------+--------------------+
  | Code | Time  | DetectKey (LastKey)|
  +------+-------+--------------------+
  |  A   | 10:00 | 1000-A             |
  |  C   | 10:10 | 1010_C  (Khoanh tròn) ---> Đẩy sang Worker lưu trữ
  |  E   | 11:00 | 1100_E             |
  +------+-------+--------------------+
 
  2) Phương thức sinh khóa trên C# / Truy vấn phụ:
     Time.ToString("HHmm") + "-" + Code
     hoặc biết dòng cuối ID = 123 ---> SELECT ... WHERE ID = 123
     Code C# = dl.Time.ToString(...)
     Tham số phiên quét:
     {
        DetectKey: 1010_C
        PageSize : 100
     }
 
 -------------------------------------------------------------------------
 
 [TÊN KHÓA LƯU TRỮ TRẠNG THÁI OFFSET]
  +--------+---------+-----------+---------+
  | Mã gói | Đối tác |    Key    |  Value  |
  +--------+---------+-----------+---------+
  |  101   |    A    | DetectKey | 1010_C  | ---> Worker commit khi xong batch (10 -> 20 -> 1100_E)
  |  101   |    A    | PageSize  |   100   | ---> Giá trị cố định (fixed)
  +--------+---------+-----------+---------+
 
  Tình huống sự cố:
  Khi xử lý bị Crash / Dừng giữa chừng ---> Chỉ lưu Value mới sau khi xử lý xong!
  (Cột Đối tác BẮT BUỘC có mặt; nếu lưu trống ở mẫu chung thì 2 đối tác A, B
   xài chung gói 101 sẽ ghi đè 10 / 20 lên nhau gây conflict không chạy được).
```

#### Phân tích 2 phương án sinh khóa nối đuôi:
1. **Phương án 1 — Xử lý tại Database bằng câu lệnh SQL (SQL CONCAT):**
   - Trong câu truy vấn động, người cấu hình bổ sung cột ghép chuỗi: `CONCAT(FORMAT(Time, 'HHmm'), '_', Code) AS DetectKey`.
   - Câu truy vấn sử dụng: `WHERE DetectKey > @LastKey ORDER BY DetectKey ASC`.
   - **Ưu điểm:** Tầng Worker C# hoàn toàn trong suốt (agnostic), chỉ việc đọc giá trị cột `DetectKey` của bản ghi cuối cùng trong danh sách kết quả rồi lưu lại.
   - **Nhược điểm:** Phép toán ghép chuỗi và convert dữ liệu trên tập dữ liệu lớn sẽ tiêu tốn tài nguyên Database IO, không tận dụng được index có sẵn của trường `Time` và `Code`, khó debug khi có sai lệch định dạng.
2. **Phương án 2 — Xử lý tại tầng ứng dụng Worker (C# Application Processing):**
   - Câu truy vấn SQL giữ nguyên các cột riêng lẻ: `SELECT Time, Code, ... FROM Table ORDER BY Time ASC, Code ASC`.
   - Worker C# khi đọc từng bản ghi sẽ áp dụng công thức ánh xạ: `dl.Time.ToString("HHmm") + "_" + dl.Code`.
   - Bản ghi cuối cùng của đợt quét (ví dụ dòng C lúc 10:10) sẽ cho ra chuỗi `1010_C`. Chuỗi này được đóng gói làm giá trị `DetectKey` lưu vào bảng trạng thái.
   - **Ưu điểm:** Giảm tải tối đa cho CSDL, truy vấn SQL tận dụng được index cây B-Tree của các cột gốc.
   - **Nhược điểm:** Phải lưu trữ công thức hoặc quy tắc ánh xạ trường trong cấu hình để Worker biết cách thức lắp ráp chuỗi.

### 1.2. Kiến Trúc Bảng Lưu Trữ Trạng Thái Offset ("Tên khóa lưu trữ")
- **Bắt buộc phân định theo từng Đối tác cụ thể:**
  - Trong mô hình ShareData, một gói tin mẫu (ví dụ: Gói 101 - Dữ liệu VMS/Giao thông) có thể được dùng chung bởi nhiều đối tác khác nhau (Đối tác A, Đối tác B...).
  - Tuy nhiên, tiến độ quét và lấy dữ liệu của mỗi đối tác là hoàn toàn độc lập:
    - Đối tác A có thể đang quét đến mốc `10:10` (`1010_C`).
    - Đối tác B có thể vừa mới khởi động hoặc đang quét ở mốc `08:00`.
  - **Quy tắc bất biến:** Bảng lưu trữ trạng thái offset bắt buộc phải quản lý theo cặp khóa `(Mã gói, Đối tác, Key)`.
  - **Cấm kỵ:** Không bao giờ được phép lưu giá trị offset chạy thật vào cấu hình mẫu chung. Nếu lưu chung, khi Đối tác A vừa ghi mốc 10:10, Đối tác B chạy sau sẽ đọc đè mốc này, làm nhảy cóc dữ liệu của Đối tác B hoặc gây xung đột dữ liệu nghiêm trọng.
- **Phân loại 2 nhóm tham số trong bảng lưu trữ:**
  - **Tham số cố định (Static Parameter):** Ví dụ `PageSize = 100`. Giá trị này do hệ thống hoặc người quản trị thiết lập cố định, không thay đổi qua các lần quét.
  - **Tham số biến đổi theo lần chạy (Stateful / Dynamic Parameter):** Ví dụ `DetectKey` / `@LastKey`. Giá trị này được cập nhật liên tục sau mỗi mẻ quét dữ liệu thành công (lần 1: `1000_A`, lần 2: `1010_C`, lần 3: `1100_E`...).

### 1.3. Cơ Chế Xử Lý Khi Worker Gặp Sự Cố (Crash Resilience)
- Trong trường hợp Worker đang thực thi đọc một batch dữ liệu (ví dụ 100 dòng), nếu tiến trình bị crash, tắt nguồn hoặc mất kết nối mạng giữa chừng:
  - Hệ thống chỉ cập nhật giá trị `Value` mới (ví dụ `1010_C`) vào CSDL sau khi toàn bộ quy trình đóng gói và gửi/nhận dữ liệu của batch đó hoàn tất thành công.
  - Nếu gặp sự cố, transaction cập nhật offset không được thực hiện. Mốc `LastKey` cũ vẫn được bảo toàn. Khi Worker khởi động lại, nó sẽ quét lại đúng từ mốc an toàn trước đó, đảm bảo tính bền vững (zero data loss).

### 1.4. Tối Ưu Hiệu Năng Tải Ban Đầu Giao Diện (Lazy Loading UI)
- **Hiện trạng:** Khi người dùng truy cập màn hình cấu hình Nguồn dữ liệu, hệ thống tự động chọn ngay dòng đầu tiên và gửi API tải về toàn bộ dữ liệu chi tiết của dòng đó. Điều này làm trang bị đơ, giật lag nếu dữ liệu lớn hoặc kết nối mạng chậm.
- **Quyết định chốt:**
  - Áp dụng cơ chế nạp lười (Lazy Loading): Khi vừa mở trang, chỉ tải danh sách tóm tắt ở danh sách bên trái. Khung chi tiết bên phải chỉ dựng sẵn bộ khung (skeleton/placeholder layout), nội dung bên trong để trống.
  - Chỉ khi người dùng chủ động nhấp chuột vào một đối tác hoặc gói tin cụ thể, hệ thống mới kích hoạt API tải dữ liệu chi tiết tương ứng.

### 1.5. Chuẩn Hóa Nhãn Nút Bấm & Bố Cục Khối So Sánh (Wireframe W0.2)
- **Tên nút thao tác:**
  - Thay đổi nút mang nhãn "SQL riêng" thành **"Tạo cấu hình mới"** hoặc **"Tạo cấu hình riêng"** nhằm diễn đạt chính xác hành vi nghiệp vụ (bản chất là tạo cấu hình tùy biến cho đối tác).
  - Nút "Lưu" chuẩn hóa thành **"Lưu & phân tích"**.
- **Kế thừa cấu hình đối tác mới:**
  - Cho phép người dùng chọn 1 trong 2 hình thức:
    1. Tạo từ mẫu chung (sao chép câu SQL và danh sách trường của gói tin gốc).
    2. Clone từ cấu hình của một đối tác đã tồn tại trước đó.
- **Khối so sánh đối chiếu (Diff Box):**
  - Đưa khối so sánh Diff từ vị trí trên cao xuống khu vực phía dưới khung SQL/bảng trường để người dùng so sánh ngay trong tầm mắt, triệt tiêu việc phải cuộn trang lên xuống.
  - Thu gọn các icon phân loại khác biệt thành một dải hiển thị viền mỏng, đồng bộ kiểu dáng.
  - Bổ sung nút chuyển đổi chế độ lọc **"Chỉ hiện dòng khác biệt"** (khi bật, bảng ẩn toàn bộ các trường trùng khớp, chỉ làm nổi bật các trường bị lệch kiểu dữ liệu hoặc thêm/bớt).

### 1.6. Tinh Gọn Danh Mục Kiểu Dữ Liệu (Data Types)
- **Rà soát:** Danh mục kiểu dữ liệu trên form hiện tại thừa mứa các kiểu cấp thấp của hệ quản trị CSDL (`Guid`, `Float`, `Double`, `Decimal`, `Long`...).
- **Định hướng chuẩn hóa:**
  - Bản chất gói tin trao đổi là định dạng JSON. Trong chuẩn JSON chỉ có 3 nhóm kiểu dữ liệu gốc: `string` (chuỗi), `number` (số), và `boolean` (đúng/sai).
  - Tạm thời giữ nguyên mã nguồn đã chạy ở Phase 1 trên môi trường phát triển, nhưng thống nhất giao diện chỉ hiển thị và hướng dẫn người dùng sử dụng các kiểu cốt lõi: Chuỗi (`string`), Số (`number`), Luận lý (`boolean`), và Ngày giờ (`datetime`). Loại bỏ `Guid` vì bản chất trong JSON đã là chuỗi (`string`).

---

## 2. Kịch bản & Lời thoại chi tiết theo mốc thời gian (Detailed Transcript)

---

### PHẦN 1: RÀ SOÁT TỔNG THỂ GIAO DIỆN & TIỆN ÍCH GHI CHÚ
*(Trích xuất từ tệp ghi âm: `DocBusinessThienAn/Sharedata_27_10_01.m4a` — Thời lượng: 02 phút 40 giây)*

- `[00:00 - 00:06]` **Tech Lead:** Em xem lại chỗ này...
- `[00:07 - 00:15]` **Tech Lead:** Nhận hay cái gì đó... Nó dùng cái này đi về là gói tin. Gói tin thì nó sẽ tính là cái gói gửi với gói nhận.
- `[00:16 - 00:24]` **Tech Lead:** Gửi nhận hả... Gửi... Gửi nhận.
- `[00:25 - 00:34]` **Dev:** Dạ đúng rồi.
- `[00:35 - 00:44]` **Tech Lead:** Thấy kỹ quá ta... Nghe rõ quá, kẻ mạnh ta!
- `[00:45 - 00:50]` **Dev:** Để coi... Em coi lại.
- `[00:51 - 00:56]` **Tech Lead:** Trong phần chỉnh sửa, bấm Edit xem...
- `[00:57 - 01:05]` **Tech Lead:** Cái này bỏ lại dữ liệu cũ... Bung nó ra đi, bung nó ra cho nó xem...
- `[01:06 - 01:14]` **Tech Lead:** Chạy thử coi sao. Này bung lên nè... Đúng rồi, bung ra... Từ từ, từ từ, xuống dòng quá.
- `[01:15 - 01:27]` **Dev:** Dùng Obsidian hả anh?
- `[01:28 - 01:40]` **Tech Lead:** Ừ, dùng Obsidian... cái Obsidian...
- `[01:41 - 01:47]` **Dev:** Xong rồi thoát ra... Dùng nó làm công cụ ghi chú.
- `[01:48 - 01:54]` **Tech Lead:** Đúng rồi. Mấy cái kia xong rồi?
- `[01:55 - 02:05]` **Dev:** Xong rồi anh. Em đang chuẩn bị phần tiếp theo...
- `[02:06 - 02:20]` **Tech Lead:** Ừ, kiểm tra kỹ các luồng xử lý trước khi đi tiếp.
- `[02:21 - 02:40]` **Dev:** Dạ, em đang mở giao diện Nguồn dữ liệu để đối chiếu từng phần.

---

### PHẦN 2: TỐI ƯU HIỆU NĂNG LOAD UI, ĐỔI TÊN NÚT VÀ TINH GỌN KIỂU DỮ LIỆU
*(Trích xuất từ tệp ghi âm: `DocBusinessThienAn/Sharedata_27_10_02.m4a` — Thời lượng: 10 phút 53 giây)*

#### Đoạn 2.1: Tối ưu tải trang ban đầu & Chuẩn hóa nút tạo cấu hình đối tác (00:00 - 05:30)
- `[00:00 - 00:15]` **Tech Lead:** Em mở cái này ra coi. Còn cái này thì hôm qua anh cũng nói là có cái filter... mình đi theo đối tác.
- `[00:16 - 00:34]` **Tech Lead:** Rồi bây giờ em bấm vô đây... Ở bên đây nó sẽ là thông tin gói tin. Chữ viết tắt... Anh cũng có dựa vô cái file tham khảo...
- `[00:34 - 00:45]` **Tech Lead:** Nhưng mà từ từ, cái này load... load ban đầu nó nhanh không? Nó chạm không?
- `[00:46 - 00:55]` **Tech Lead:** Load á hả? Nếu mà chậm quá thì... thì thôi khỏi chọn ban đầu. Cứ để hiện bên này lên trước rồi người ta muốn coi cái nào thì người ta bấm vô coi.
- `[00:56 - 01:05]` **Dev:** Hiện bên đây trước đúng không anh? Ừm... Chứ em thấy đang load là nó tự động nó chọn cái dòng đầu đúng không? Vô tình là nó load luôn cái kia nữa.
- `[01:06 - 01:13]` **Tech Lead:** Đúng rồi, như vậy nó hơi nặng đó. Để trống đi... cứ để trống thôi.
- `[01:14 - 01:22]` **Tech Lead:** Hoặc em làm sẵn cái khung... cái khung thôi, còn nội dung ở trong để trống hết. Load cả dây như vậy nó load cũng khủng á.
- `[01:23 - 01:31]` **Tech Lead:** Rồi, một trường... Tức là ở đây gói này có 3 người dùng nè, 3 đối tác dùng. Có một thằng riêng, hai thằng dùng chung.
- `[01:32 - 01:40]` **Tech Lead:** Thì bây giờ muốn tạo cho đối tác thì mình cứ chọn nó... xong chọn đối tác của hai thằng còn lại.
- `[01:41 - 01:47]` **Tech Lead:** Thì mình sẽ có 2 ngữ cảnh: Một là mình sao chép cái mẫu chung, hai là mình clone từ một thằng đối tác trước đó.
- `[01:48 - 01:53]` **Dev:** Dạ, ví dụ clone mẫu chung thì cứ tạo mới.
- `[01:54 - 02:02]` **Tech Lead:** Ừ, nhưng mà cái chữ "SQL riêng" sửa lại... sửa lại một tí.
- `[02:03 - 02:12]` **Dev:** Chỗ này đúng không anh?
- `[02:13 - 02:22]` **Tech Lead:** Đúng rồi. Ví dụ "Tạo cấu hình" gì đó... "Tạo cấu hình riêng", hay là "Tạo cấu hình mới" gì đó. Tạo cấu hình riêng hay tạo cấu hình mới, hay là tạo gì đó... Cấu hình nói chung chung. Mình cần cái... Hiện tại đây là cấu hình của kênh này, cấu hình của gói tin.
- `[02:23 - 02:30]` **Tech Lead:** Nhưng mà cái này là đang là của... của đối tác đúng không?
- `[02:31 - 02:39]` **Dev:** Dạ của đối tác. Thì ghi ngắn gọn là "Tạo cấu hình mới" là được.
- `[02:40 - 02:48]` **Tech Lead:** Tạo mới... Tại vì bấm vô là cũng sẽ hiện đối tác rồi hiện lại mấy cái kia rồi.
- `[02:49 - 02:58]` **Tech Lead:** Cái tiêu đề cũng có rồi: "Cấu hình riêng cho đối tác" gì đó... Thì ghi "Tạo cấu hình mới" là được rồi. Để chữ "SQL riêng" nhìn nghe kỳ quá. Tạo cấu hình mới...
- `[02:59 - 03:09]` **Dev:** Rồi, khi mà nó riêng... thì mình đang có cái button... cái này em đang demo rồi em thêm mấy cái wireframe vô đây để nó so sánh với các field.
- `[03:10 - 03:22]` **Tech Lead:** Ừm... nhưng mà cái ô... cái ô kia làm cho nó thống nhất với nhau đi. Làm biểu tượng thì làm thống nhất luôn luôn cho nó dễ. Ví dụ như là cũng làm một cái dãy giống vậy nè, để nó ở trên này nè, rồi vẽ icon nó nhỏ nhỏ lại thôi. Chắc cái này làm sơ sài quá...
- `[03:23 - 03:35]` **Tech Lead:** Để cho nó... cho nó đỡ rối.
- `[03:36 - 03:45]` **Dev:** Còn cái style...
- `[03:46 - 03:55]` **Tech Lead:** Còn thằng này... thằng này bê xuống gần dưới này nè. Cao quá! Bê xuống để dưới đây nè, để người ta so cho nó dễ. Chứ giờ đã scroll xuống rồi, lại scroll ngược lên nữa thì mất công.
- `[03:56 - 04:09]` **Tech Lead:** Gom nó lại. Tại trên này như đâu có... đâu có cần đâu. Hay là nó dính cái màu?
- `[04:10 - 04:20]` **Dev:** Màu thôi anh. Nếu mà màu giống thì để ở trên cũng được, để em kiểm tra lại...
- `[04:21 - 04:30]` **Tech Lead:** Rồi, xem tiếp...
- `[04:31 - 04:45]` **Tech Lead:** Thì nó sẽ so sánh cấu hình của đối tác với mẫu chung, coi nó có so sánh đúng không.
- `[04:46 - 05:05]` **Tech Lead:** Đúng rồi... Hiện dòng khác biệt. Bấm vô hiện dòng khác biệt là nó mất đúng không?
- `[05:06 - 05:15]` **Dev:** Bấm vô là nó chỉ hiện dòng này... Đúng rồi, nó sẽ hiện các dòng lệch nhau.
- `[05:16 - 05:30]` **Tech Lead:** Rồi, ok. Tiếp tục chỗ khối tiếp theo...

#### Đoạn 2.2: Rà soát danh mục Kiểu dữ liệu & Đặt tên cột trường (05:31 - 10:53)
- `[05:31 - 05:45]` **Tech Lead:** Rồi cái chỗ này là một cái... mới. Mẫu chung ở đây dùng cho hai người...
- `[05:46 - 06:05]` **Tech Lead:** Mà lúc qua hàng nó sẽ có mấy cái chỉnh sửa. Từ từ mở cái kiểu ra coi... Kiểu nhiều quá vậy?
- `[06:06 - 06:15]` **Dev:** Đang kế thừa ở cũ anh, gồm có: `string`, `datetime`, `boolean`, `long`, `float`, `double`, `decimal`...
- `[06:16 - 06:25]` **Tech Lead:** `Float`, `double`, `decimal`... hơi nhiều quá!
- `[06:26 - 06:35]` **Dev:** Nhiều thì lấy `double` với `decimal` là được rồi.
- `[06:36 - 06:45]` **Tech Lead:** `Float`, `decimal`, `double`... ba cái. `Guid`... `Guid` cũng nhiều nữa. `Guid` nhiều khi nó cũng là kiểu `string` rồi. Tại vì về cơ bản nhận JSON thì nó cũng là `string` mà ra.
- `[06:46 - 06:55]` **Tech Lead:** Thật ra trong JSON... JSON nó chỉ có: một là `string`, hai là kiểu số (`number`). Số thì `int` hay là `float` gì đó mình không care. Kiểu số, với lại kiểu ngày tháng (`datetime`). `Datetime` thì cũng quy về `string`.
- `[06:56 - 07:05]` **Tech Lead:** Vậy `int` bỏ nè, `long` bỏ luôn, `float` bỏ luôn. Giữ cho hai cái nhiều nhất. `Guid` cũng bỏ luôn, coi như dùng là `string`.
- `[07:06 - 07:15]` **Dev:** `Guid` bỏ luôn, tại vì giờ nó là kiểu `string` thì mình gắn `Guid` vô cũng được.
- `[07:16 - 07:25]` **Tech Lead:** Bỏ luôn, giữ hai cái này thôi.
- `[07:26 - 07:35]` **Dev:** Còn cái số... số lẻ hay số chẵn anh? Số lẻ như `double` đúng không? Em nhớ vậy á. Một trong hai thôi.
- `[07:36 - 07:45]` **Tech Lead:** Giữ lại một cái, lấy cái nào nhiều nhất ở giữa á.
- `[07:46 - 07:55]` **Dev:** Nếu mình lấy số lẻ mà cần số chẵn thì sao anh? Tại số lẻ lúc mình nhập được số chẵn đúng không?
- `[07:56 - 08:05]` **Tech Lead:** Thì mới nói là... nếu mà quy về JSON thì nó chỉ là: kiểu số, kiểu `string`, với `boolean`. Thường là `boolean` thôi, `boolean`, `string`, với lại số.
- `[08:06 - 08:20]` **Tech Lead:** Xong rồi trong `string` người ta mới phân ra là `string` dạng chuỗi hay là `string` theo kiểu ngày tháng. Thì cái đó là phải thêm ngày tháng vô để biết đường mình convert cho nó dễ.
- `[08:21 - 08:35]` **Tech Lead:** Nó chia chia cái kiểu này thấy nó sẽ hơi... Thôi cứ kệ để vậy cũng được, nhưng mà thấy có vẻ hơi dư. Để lúc sử dụng thử... sử dụng thử rồi nếu mà dư quá thì bỏ bớt đi. Chứ thấy cái này này hơi dư. Giờ cứ cho trải nghiệm trước đi.
- `[08:36 - 08:45]` **Dev:** Anh chốt lại trải nghiệm hay là bỏ để em biết em bỏ luôn?
- `[08:46 - 08:55]` **Tech Lead:** Cứ để trước cũng được. Nhưng mà trong code có xử lý gì không? Nếu trong code chưa xử lý thì thôi.
- `[08:56 - 09:05]` **Dev:** Tạm thời em để lại, tại cái khúc này hiện tại nó đang nằm trên môi trường dev luôn rồi, kiểu nó có từ Phase 1 á.
- `[09:06 - 09:20]` **Tech Lead:** Ừ, để coi lại... Khóa... Cột "Khóa", "Trường", "Trường quản lý", hay "Trường dữ liệu"?
- `[09:21 - 09:35]` **Dev:** Ghi chữ "Khóa" nghe hơi kỳ... "Trường dữ liệu"...
- `[09:36 - 09:50]` **Tech Lead:** "Tên hiển thị", "Tên mô tả"... Tên mô tả hay tên hiển thị?
- `[09:51 - 10:05]` **Dev:** Tên hiển thị kiểu như lúc mình gửi qua á.
- `[10:06 - 10:20]` **Tech Lead:** Không, tên hiển thị là để... để cho cái chức năng ánh xạ nó hiện ra để biết được cái trường đó là cái gì thôi. Chứ mình... mình gửi nhận mình đâu có dùng cái trường này. Mình dùng cái trường trong cái ánh xạ mà.
- `[10:21 - 10:35]` **Dev:** Dạ đúng rồi.
- `[10:36 - 10:53]` **Tech Lead:** Rồi, ok. Gửi qua xem có đúng không...

---

### PHẦN 3: PHÂN TÍCH BẢN VẼ TAY b7af82ba478cc7d29e9d.jpg — KHÓA NỐI ĐUÔI COMPOSITE KEY (@LastKey)
*(Trích xuất từ tệp ghi âm: `DocBusinessThienAn/Sharedata_27_10_03.m4a` — Thời lượng: 06 phút 39 giây)*

- `[00:00 - 00:15]` **Tech Lead:** Quay lại chỗ này... Cái key, anh muốn là có time nó là: Hour Minute _ Code... Nó phải lớn hơn cái trước đó.
- `[00:16 - 00:23]` **Dev:** Cái trước đó này của mình chắc chắn phải lưu rồi đúng không?
- `[00:24 - 00:30]` **Tech Lead:** Đúng rồi! Nhưng mà lưu nó sẽ có 2 cách. Thứ nhất là: Làm sao để xử lý ra được thằng này? Đúng không?
- `[00:31 - 00:44]` **Tech Lead:** Thì cách thứ nhất là mình xử lý dưới cơ sở dữ liệu luôn. Là mình... cho nó tải về... ví dụ như là `DetectKey` gì đó.
- `[00:45 - 00:55]` **Tech Lead:** Đó: A, 10 giờ... Mình nối ở dưới câu truy vấn luôn! Là ra vậy luôn. *(Tech Lead cầm bút vẽ mục 1 trên tờ giấy b7af82ba478cc7d29e9d.jpg)*.
- `[00:56 - 01:03]` **Dev:** Hình như cái này mình cũng lưu luôn nè, nếu mà đi theo hướng này.
- `[01:04 - 01:15]` **Tech Lead:** Thì lúc đó là mình sẽ lưu lại là cái cột nào là cái cột làm khóa để mình lấy.
- `[01:16 - 01:28]` **Tech Lead:** Còn cách thứ hai á, là mình sẽ lưu làm sao để biến đổi ra được thằng này. Để trong code C# khi mà nó lấy dữ liệu từng cột nè, nó phải map lại được.
- `[01:29 - 01:38]` **Tech Lead:** Ví dụ như ở cột Time này, nó sẽ `ToString("HHmm")` cộng với lại cái dấu gạch ngang (hoặc gạch dưới), cộng với lại cái `Code`.
- `[01:39 - 01:48]` **Tech Lead:** Thì phải lưu được cái mã này vào, để C# nó sẽ xử lý. *(Tech Lead ghi chú mục 2 trên tờ giấy b7af82ba478cc7d29e9d.jpg)*.
- `[01:49 - 02:00]` **Tech Lead:** Thằng này thì cách này nó sẽ tốn vận hành trên C#, trên code của mình. Còn cách này nó sẽ tốn vận hành trên SQL, cái IO phép toán nối chuỗi. Nếu mà có hình ảnh nối chuỗi, convert gì đó thì nó sẽ tốn, nó sinh thêm một cục nữa.
- `[02:01 - 02:10]` **Dev:** Nó có hai cách. Nhưng mà cách nào đi nữa thì nó cũng phải lưu một cái lại là...
- `[02:11 - 02:20]` **Tech Lead:** Cái cách này nó phải lưu lại được cái cột nào để nó lấy giá trị đó ra. Ví dụ nó đến dòng C đi...
- `[02:21 - 02:30]` **Tech Lead:** Thì nó phải lấy được cái cột đó ra, lấy được cái giá trị này ra để nó lưu trữ.
- `[02:31 - 02:40]` **Tech Lead:** Còn nếu đi cách này á, nó phải lưu được cái công thức ở chỗ này. Đúng không?
- `[02:41 - 02:50]` **Dev:** Lưu được công thức... rồi xử lý C#?
- `[02:51 - 03:00]` **Tech Lead:** Đúng rồi, để convert ra được thằng này. C# hoặc là code SQL gì đó, làm sao để nó map ra được thôi. Có thể dò... ví dụ như lưu SQL thì có thể dùng truy vấn để truy vấn lại, nếu được.
- `[03:01 - 03:15]` **Tech Lead:** Ví dụ mình đã biết cái key cuối cùng là key A rồi, thì mình lại truy vấn lại, lấy cái mã này select lại cái mã này. Đúng không? Cũng được, cũng là một cách.
- `[03:16 - 03:28]` **Tech Lead:** Ví dụ như query 100 dòng, dòng cuối mình biết được ID của nó là 123, thì về cơ bản là `SELECT ... WHERE ID = ...`.
- `[03:29 - 03:40]` **Tech Lead:** Được chưa? Còn không thì lưu dưới dạng là code C#. C# là cái... lúc đó phải viết trong code, ví dụ như `Time.ToString(...)` gì đó thì nó sẽ hơi khó quá.
- `[03:41 - 03:52]` **Dev:** Nhưng mà cái IO bên dưới SQL á, theo hướng này... thì nó sẽ nặng cơ sở dữ liệu. Nó nặng do là mình kiểu lưu nhiều quá hay sao?
- `[03:53 - 04:05]` **Tech Lead:** Nó phải convert chỗ này... Select nối chuỗi! Giống như `CONCAT` vậy á. Tại vì cái chỗ nối chuỗi này nè, nhiều mấu nối phức tạp quá thì nó sẽ tăng cái tải này lên.
- `[04:06 - 04:15]` **Dev:** Tăng thời gian tìm kiếm hả anh?
- `[04:16 - 04:25]` **Tech Lead:** Tăng ở chỗ liên quan đến truy vấn. Truy vấn nó cao. Ví dụ như lấy dữ liệu của cái bảng đó nhiều đi, nó nối thêm chỗ này chắc chắn nó phải tăng lên rồi.
- `[04:26 - 04:36]` **Tech Lead:** Thì nó có hai trường hợp. Thứ hai: Nó phải có một cái chỗ để nó cấu hình, chính là cái chỗ giá trị nhập đó.
- `[04:37 - 04:45]` **Tech Lead:** Rồi, ngoài ra nữa thì nó sẽ còn một cái nữa là: Cái tên khóa lưu trữ.
- `[04:46 - 04:55]` **Tech Lead:** Để vô cái bảng cấu hình cái giá trị lần cuối đó, nó phải có: Thứ nhất là cái gì? Cái mã gói đúng không? Thứ hai là gói tin... Mã gói, đối tác, và cái key đó.
- `[04:56 - 05:05]` **Tech Lead:** Để nó lấy được cái value trước đó... nó định nghĩa là cái gì, để nó xử lý. *(Tech Lead vẽ bảng "Tên khóa lưu trữ" ở cuối tờ giấy b7af82ba478cc7d29e9d.jpg)*.
- `[05:06 - 05:15]` **Tech Lead:** Cái này mới là mức bình thường. Còn cái mức nâng cao hơn nữa là phải tối ưu nữa là: Trường hợp lấy cái dữ liệu này, vô tình xử lý nó bị crash đi!
- `[05:16 - 05:25]` **Dev:** Bị dừng giữa chừng? Bị crash app à?
- `[05:26 - 05:35]` **Tech Lead:** Ừ, thì xử lý như thế nào nữa? Cái đó tính sau. Nhưng trước mắt cứ lấy lên được đúng giá trị trước đó, lưu ngược lại giá trị mới khi mà nó xử lý xong. Đấy, là được.
- `[05:36 - 05:48]` **Tech Lead:** Đó thì nó sẽ có hai cái. Và cái key này có thể là lớn hơn (`>`), hoặc là lớn hơn hoặc bằng (`>=`). Mặc định là lớn hơn. Lớn hơn hay lớn hơn bằng... thì cũng xem xét. Nếu mà cho phép giá trị bằng thì có cần không? Nếu không thì thôi lớn hơn, mặc định là lớn hơn đi.
- `[05:49 - 06:05]` **Dev:** Cái value này là người dùng tự set vô hả anh?
- `[06:06 - 06:15]` **Tech Lead:** Không! Value này là hệ thống detect chứ! Là cái này nè... *(Chỉ vào mục DetectKey trên bản vẽ)*. Có nghĩa là lúc mà em lấy dữ liệu, em phải order theo một cái hướng gì đó.
- `[06:16 - 06:25]` **Tech Lead:** Ví dụ như ở đây, do kết hợp giữa code với time thì bắt buộc phải order dựa trên cái hàng key này.
- `[06:26 - 06:33]` **Dev:** Tại vì không order là nó không detect được.
- `[06:34 - 06:40]` **Tech Lead:** Đúng rồi! Nên là nó chính là cái hàng vừa trong cái `WHERE`, vừa trong cái `ORDER` luôn đó! Vừa `WHERE`, vừa `ORDER` vô trong thằng này luôn. Thì nó sẽ lưu ra được cái giá trị, lúc nào nó cũng sẽ tăng dần, hoặc là giảm dần.
- `[06:41 - 06:48]` **Tech Lead:** Nhưng mà thường sẽ tăng dần mà. Tại vì thường mấy cái dữ liệu này là theo thời gian rồi. Thời gian thì chỉ có thời gian tăng theo tương lai thôi chứ không thể giảm về quá khứ được. Mấy dữ liệu thời gian này mới có cái vụ nối tiếp. Chứ mấy dữ liệu mà dạng quản lý thì cơ bản là gửi lại hết rồi. Đa số là gửi lại hết. Nên xu hướng sẽ tăng dần.
- `[06:49 - 06:55]` **Dev:** Vậy thôi hả anh?
- `[06:56 - 07:05]` **Tech Lead:** Ừ, vậy thôi. Đơn giản!

---

### PHẦN 4: BẢNG LƯU TRỮ OFFSET THEO ĐỐI TÁC, XỬ LÝ CRASH & QUY CHUẨN THAM SỐ
*(Trích xuất từ tệp ghi âm: `DocBusinessThienAn/Sharedata_27_10_04.m4a` — Thời lượng: 04 phút 50 giây)*

- `[00:00 - 00:10]` **Tech Lead:** Nghĩa là làm sao để nó biến đổi ra được cái key này nè... Chứ không phải là nhập cái giá trị này vào.
- `[00:11 - 00:20]` **Dev:** Em ví dụ ha, em nhập là test đi, thì em phải biến đổi sao cho nó ra được cái này đúng không?
- `[00:21 - 00:30]` **Tech Lead:** Đúng rồi! Chính là cái này nè. Còn cái giá trị mà nhập vào á, về bản chất là load cái thằng này lên nè, để nó biết là giá trị trước đó, giá trị gần nhất đang chạy là cái gì.
- `[00:31 - 00:45]` **Tech Lead:** Ví dụ nó hiện lên đây. Nhưng mà chắc thực ra không cần hiện đâu. Do nếu mà hiện thì phải hiện theo đúng đối tác, chứ không thể nào mà hiện theo mẫu được. Ví dụ như hai đối tác này cùng xài mẫu chung đi, thì ở đây nó sẽ hiện rất là nhiều, không thể nào mà hiện được.
- `[00:46 - 00:55]` **Tech Lead:** Tại vì nếu mà hiện theo mẫu là cũng không đúng, vì nó đang chạy theo đối tác mà! Cái mẫu bao giờ cũng sẽ trống, mẫu không bao giờ hiện được.
- `[00:56 - 01:10]` **Tech Lead:** Cho nên không... chỗ này không bao giờ lưu trống được, lưu trống là không chạy được. Nếu mà lưu trống là đối tác A cũng chạy cái mã này, cũng chạy gói 101 này, cũng cái key A đó... thì lúc đó là nhiều đối tác cùng đè lên cái key này là nó sẽ sai!
- `[01:11 - 01:25]` **Dev:** Em ví dụ xong cái luồng này, giờ ví dụ đang phát triển đi, giả sử em muốn test á... kiểu kiểm tra... có cách nào kiểu mình tự self-test lại không?
- `[01:26 - 01:38]` **Tech Lead:** Thì mình tạo một cái mã mới, tạo một cái đối tác mới thôi. Tự test case đó coi sao. Tạo một cái đối tác mới, thế thôi.
- `[01:39 - 01:46]` **Dev:** Vậy cái cột này bỏ luôn ha anh, "Giá trị lần trước" nè?
- `[01:47 - 02:00]` **Tech Lead:** Bỏ cũng được nhưng mà sẽ hiện ở chỗ khác. Tại vì về cơ bản là mình đang thiếu một cái bảng là cái bảng quản lý nè, cái chức năng quản lý nè: Là cái key này nó đang chạy đến chỗ nào rồi. Hiện trên giao diện mình là mình không có.
- `[02:01 - 02:15]` **Tech Lead:** Ví dụ như gói 101, đối tác A nè, nó đang chạy đến cái dữ liệu nào. Chỗ này không có, hiện tại là không có. Thì có thể một là gom chung để ở từng đối tác này cũng được, hai là một cái trang màn hình riêng để quản lý cho nó dễ.
- `[02:16 - 02:30]` **Dev:** Vậy nếu như cái "Giá trị đã lưu" này nó đi theo đối tác, vậy mẫu chung nó không có đúng không anh?
- `[02:31 - 02:40]` **Tech Lead:** Đúng rồi! Mẫu chung sao hiện được? Nó đâu có lưu gì đâu mà nó hiện!
- `[02:41 - 02:55]` **Tech Lead:** Giờ không... không chia đối tác, không lưu đối tác rồi sao mà nó đúng? Giờ thằng A cũng xài mẫu chung, giờ nó lưu ở đây là số 10. Thằng B cũng xài mẫu chung, nó ghi vô đây là số 20 là nó ghi đè vô đây rồi! Nó ghi đè vô rồi hai thằng conflict với nhau rồi sao chạy được nữa?
- `[02:56 - 03:05]` **Tech Lead:** Bắt buộc chỗ này phải chia theo đối tác! Đấy, vậy thôi. Cho nó dễ. Chung key, chung key là dễ nhất.
- `[03:06 - 03:15]` **Tech Lead:** Cái key đó mình sẽ đặt để nó định nghĩa là cái giá trị này nó đang là cái giá trị gì đó. Ví dụ như là `DetectKey` đi, thì biết được là à thằng này là `DetectKey`. Tại vì nhiều tình huống trong cùng một cái câu nó có hai ba cái câu điều kiện khác nhau.
- `[03:16 - 03:25]` **Tech Lead:** Đúng không? Ví dụ như là cái của em là `DetectKey` đi, là cái giá trị trước đó, ví dụ như `10:10_C`. Rồi phân trang đi, `PageSize` đi, là `100`. Đó, thì nó sẽ lưu hai cái thằng này vô đây: `PageSize`, `DetectKey` cho đối tác A gói 101.
- `[03:26 - 03:35]` **Tech Lead:** Đúng không? Thì khi mình update thì mình chỉ update thằng này thôi (`DetectKey`), thằng này là fixed rồi (`PageSize`). Thì nó là giá trị cố định, cái nguồn giá trị là giá trị cố định.
- `[03:36 - 03:45]` **Dev:** Nhìn phức tạp lại nó cũng hơi rối ha, cái chỗ này nè...
- `[03:46 - 03:55]` **Tech Lead:** Nó là nó có thể tạo nhiều... tạo nhiều tham số. Tùy từng tham số: tham số là nó sẽ fixed giá trị, có tham số là nó sẽ biến đổi dựa trên lần chạy.
- `[03:56 - 04:05]` **Tech Lead:** Mỗi lần chạy lấy giá trị khác nhau. Lần sau nó chạy ví dụ nó tiếp đến E, lúc 11 giờ... thì chỗ này bắt buộc nó phải là 11 giờ gạch E (`1100_E`). Nó phải lưu được thằng này vô để xử lý tiếp.
- `[04:06 - 04:18]` **Dev:** Dạ em hiểu rồi.
- `[04:19 - 04:30]` **Tech Lead:** Rồi, xem kỹ lại rồi ráp vô.
- `[04:31 - 04:50]` **Dev:** Dạ vâng anh, em nắm rõ phần này rồi!

