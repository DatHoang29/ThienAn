---
tier: A
read: full
source:
  - DocBusinessThienAn/Sharedata_28_10.m4a (15:25)
  - DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/_source/854760f3b1c0319e68d15.jpg (Bản vẽ tay kỹ thuật 1 - @lastKey, __lastKey, cấu trúc key gói 101/102, lấy dòng cuối)
  - DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/_source/ad85cb3a1a099a57c3184.jpg (Bản vẽ tay kỹ thuật 2 - Trục thời gian cũ -> mới, ORDER BY, WHERE > @lastKey, lưu trữ offset)
  - DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/_source/eca33912e821687f31306.jpg (Bản vẽ tay kỹ thuật 3 - Công thức xử lý SQL dòng cuối vs C# Worker)
date: 28-10-2026
model: Gemini Multimodal Native Audio & Vision Transcribe
status: verified
topic: ShareData — Xử lý Khóa Nối Đuôi (@lastKey / __lastKey), 3 Phương Án Kỹ Thuật (SQL Full Projection vs. Single Row Evaluation vs. C# Worker), Tối Ưu Hiệu Năng Truy Vấn Dữ Liệu Lớn & Chuẩn Hóa Bảng Lưu Trữ Offset Phân Định Theo Đối Tác
participants:
  - Tech Lead
  - Dev
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ ShareData — Xử Lý Khóa Nối Đuôi, 3 Phương Án Kỹ Thuật (@lastKey / __lastKey), Tối Ưu Hiệu Năng Truy Vấn Dữ Liệu Lớn & Bảng Lưu Trữ Offset Theo Đối Tác
### (Nguồn từ tệp ghi âm Sharedata_28_10.m4a kết hợp 3 Bản vẽ tay kỹ thuật 854760f3b1c0319e68d15.jpg, ad85cb3a1a099a57c3184.jpg và eca33912e821687f31306.jpg)

- **Ngày ghi nhận:** 28/10/2026
- **Thời lượng:** 15 phút 25 giây (toàn văn tệp audio `DocBusinessThienAn/Sharedata_28_10.m4a`)
- **Tài liệu trực quan đính kèm:**
  - Bản vẽ tay 1 (Mối liên hệ `@lastKey -> __lastKey`, Gói 101/102, Lấy dòng cuối cùng): [854760f3b1c0319e68d15.jpg](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/H%E1%BB%AFuNgh%E1%BB%8B-ChiL%C4%83ng/ShareData/_source/854760f3b1c0319e68d15.jpg)
  - Bản vẽ tay 2 (Trục thời gian `cũ -> mới`, Mệnh đề `WHERE / ORDER BY`, Giá trị mặc định & Lưu trữ theo Đối tác): [ad85cb3a1a099a57c3184.jpg](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/H%E1%BB%AFuNgh%E1%BB%8B-ChiL%C4%83ng/ShareData/_source/ad85cb3a1a099a57c3184.jpg)
  - Bản vẽ tay 3 (Công thức xử lý SQL trên dòng cuối vs. Xử lý C# Worker): [eca33912e821687f31306.jpg](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/H%E1%BB%AFuNgh%E1%BB%8B-ChiL%C4%83ng/ShareData/_source/eca33912e821687f31306.jpg)
- **Người tham gia:** Tech Lead, Dev
- **Chủ đề chính:**
  1. **Bản chất của Khóa Nối Đuôi (`@lastKey` và `__lastKey`):** Tham số truyền vào câu truy vấn (`@lastKey`) tương ứng với cột mốc đầu ra (`__lastKey`) được trích xuất từ dòng cuối cùng của danh sách kết quả (`last list`) để lưu trữ lại làm mốc quét cho lần tiếp theo.
  2. **Đặc thù cấu trúc khóa theo từng gói tin:** Gói 101 (sự kiện, thời gian cập nhật liên tục) cần độ chi tiết cao kết hợp mã thiết bị (`UpdateTime yyyyMMddHHmmssfff_Code`); Gói 102 (thời tiết, chu kỳ chậm 1-2 tiếng) không cần mili-giây `fff` mà chỉ cần thời gian bản tin (`PacketTime yyyyMMddHHmmss_Code`).
  3. **Đánh giá & So sánh 3 Phương án Kỹ thuật Xử lý Khóa Nối Đuôi:**
     - **Phương án 1 (Đẩy xuống SQL làm - Full Projection):** Câu SQL chính trực tiếp ghép chuỗi cột `__lastKey` cho toàn bộ các dòng kết quả. Dễ cấu hình, trong suốt với Worker nhưng tốn CPU CSDL và lãng phí phép tính ghép chuỗi trên 99 dòng đầu bị bỏ đi.
     - **Phương án 2 (Lưu công thức SQL xử lý dòng cuối cùng - Single Row Evaluation):** Câu SQL chính lấy dữ liệu nguyên bản theo index. Worker sau khi đọc xong batch chỉ gọi một truy vấn phụ siêu nhẹ áp dụng công thức trên duy nhất dòng cuối cùng (`SELECT công_thức WHERE ID = @idCuối`). Tối ưu vượt trội khi quét bảng hàng triệu dòng.
     - **Phương án 3 (Xử lý ghép chuỗi tại C# Worker):** Tối ưu nhất về mặt CSDL nhưng phụ thuộc cứng vào mã nguồn C#, khó cấu hình động trên giao diện, nên thống nhất tạm thời gác lại.
  4. **Quy tắc sắp xếp (`ORDER BY`) và lọc (`WHERE`):** Dữ liệu luôn sắp xếp tăng dần theo thời gian/mã (`cũ -> mới`), đảm bảo dòng cuối cùng luôn là dữ liệu mới nhất (`A -> dữ liệu mới nhất`). Cung cấp giá trị khởi tạo mặc định (`default value` / `min khởi tạo`) để kiểm soát mốc bắt đầu, tránh gửi dữ liệu thử nghiệm cũ.
  5. **Kiến trúc Bảng Lưu Trữ Offset:** Bắt buộc lưu trữ phân định theo bộ 3 `(Mã gói, Đối tác, Key) -> Value`. Cấm lưu mốc chạy thật vào mẫu dùng chung để triệt tiêu nguy cơ xung đột tương tranh (race condition) giữa các đối tác.
  6. **Định hướng triển khai & Tối giản giao diện (Wireframe W0.2):** Thống nhất tập trung triển khai Phương án 1 và kiểm thử hiệu năng Phương án 2; giữ giao diện Khối 3 tối giản (bảng 3 cột sạch sẽ), để lập trình viên chủ động viết câu SQL linh hoạt mà không cần các wizard phức tạp.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Giải Mã Trực Quan 3 Bản Vẽ Kỹ Thuật

Ba bản vẽ tay kỹ thuật được Tech Lead phác thảo trực tiếp trong cuộc họp ngày 28/10/2026 ghi nhận toàn diện cơ chế khóa nối đuôi (Cursor-based Pagination / Incremental Offset Tracking):

#### Bản vẽ tay 1: Mối quan hệ `@lastKey` ↔ `__lastKey` & Cấu trúc khóa từng gói tin ([854760f3b1c0319e68d15.jpg](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/H%E1%BB%AFuNgh%E1%BB%8B-ChiL%C4%83ng/ShareData/_source/854760f3b1c0319e68d15.jpg))

```
   BẢN VẼ TAY 1 (854760f3b1c0319e68d15.jpg): NGUYÊN TẮC CỘT MỐC & DÒNG CUỐI CÙNG
 
   @lastKey  --->  __lastKey  (Cột mốc trong câu SQL)
       |
       +---> Lấy từ DÒNG CUỐI CÙNG của danh sách kết quả (last row of list)
             và LƯU LẠI vào bảng trạng thái offset.
 
   [CẤU TRÚC KHÓA THEO ĐẶC THÙ GÓI TIN]:
   - Gói 101 (Trạng thái/Sự kiện): 
       UpdateTime yyyyMMddHHmmssfff_Code  (Độ chính xác cao tới mili-giây fff + Mã thiết bị)
   - Gói 102 (Dữ liệu thời tiết/Môi trường):
       PacketTime yyyyMMddHHmmss_Code     (Không cần fff, chu kỳ phát sinh 1-2 tiếng/lần)
 
   [PHƯƠNG ÁN 1 & 2 - ĐẨY SQL LÀM]:
   - Trường dữ liệu nào làm key thì chỉ định trường đó.
   - lastKey -> lưu 1 đoạn mốc [A] để lấy cho lần thứ 2.
```

#### Bản vẽ tay 2: Trục thời gian tăng dần, Mệnh đề WHERE / ORDER BY & Phân định Đối tác ([ad85cb3a1a099a57c3184.jpg](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/H%E1%BB%AFuNgh%E1%BB%8B-ChiL%C4%83ng/ShareData/_source/ad85cb3a1a099a57c3184.jpg))

```
   BẢN VẼ TAY 2 (ad85cb3a1a099a57c3184.jpg): CHIỀU DỮ LIỆU & BẢNG OFFSET THEO ĐỐI TÁC
 
   Trục thời gian:  [ CŨ ]  ------------------------>  [ MỚI ]
                     |                                   |
                     v                                   v
             Dữ liệu cũ đứng trước            Dòng cuối cùng = Dữ liệu MỚI NHẤT (A)
 
   Mệnh đề truy vấn:
     WHERE  [Cột điều kiện]  >  @lastKey
     ORDER BY  Date ASC, HHmm ASC, Code ASC   (Hoặc ORDER BY __lastKey ASC)
 
   Min khởi tạo (Default Value):
     Nếu chưa có mốc chạy trước đó -> Dùng giá trị mặc định (vd: '20261028000000_A')
     để bỏ qua dữ liệu cũ từ giai đoạn chạy thử nghiệm.
 
   Bảng minh họa duyệt dữ liệu:
   +-------+-------+--------------------+
   | Date  | Code  | __lastKey          |
   +-------+-------+--------------------+
   | 10:00 |   A   | 1000_A             |
   | 10:10 |   B   | 1010_B (Last item) | ---> Cầm giá trị '1010_B' lưu vào CSDL
   +-------+-------+--------------------+
 
   Lần quét kế tiếp:
     WHERE __lastKey > '1010_B'
     -> Trích xuất các bản ghi tiếp theo (11:10_D...)
 
   BẢNG LƯU TRỮ OFFSET:
   Phân định theo 3 khóa độc lập: [Mã gói] + [Đối tác] + [Key] -> [Value]
   (Ví dụ: Gói 101, Đối tác B, Key '__lastKey' -> Value '1010_B')
```

#### Bản vẽ tay 3: So sánh 3 phương án xử lý — SQL Full vs. SQL Single Row vs. C# Worker ([eca33912e821687f31306.jpg](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/H%E1%BB%AFuNgh%E1%BB%8B-ChiL%C4%83ng/ShareData/_source/eca33912e821687f31306.jpg))

```
   BẢN VẼ TAY 3 (eca33912e821687f31306.jpg): CÔNG THỨC XỬ LÝ DÒNG CUỐI VS C# WORKER
 
   [CƠ CHẾ LƯU KHÓA]:
 
   Phương án 1 (SQL Full Projection):
     - SELECT CONCAT(...) AS __lastKey cho 100 dòng.
     - Vấn đề: 99 dòng đầu bị bỏ đi, gây lãng phí IO/CPU trên bảng lớn hàng triệu dòng.
 
   Phương án 2 (Công thức xử lý SQL trên dòng cuối - Single Row Evaluation):
     - Câu SQL chính: SELECT bình thường theo index gốc (nhẹ, nhanh).
     - Khi Worker chạy tới dòng cuối cùng (đã xác định được ID dòng cuối):
         SELECT [Công thức ghép chuỗi] FROM Bảng WHERE ID = @lastRowId
     - Chỉ tính toán DUY NHẤT 1 dòng cuối cùng! Tiết kiệm tài nguyên tối đa.
 
   Phương án 3 (C# Worker Code):
     - var d = lastRow;
     - string key = d.UpdateTime.ToString("yyyyMMddHHmmss") + "_" + d.Code;
     - Đánh giá: Tối ưu CSDL nhưng phụ thuộc cứng vào code C#, khó cấu hình động.
```

---

### 1.2. Phân Tích So Sánh Chi Tiết 3 Phương Án Trong Trao Đổi

#### A. Phân tích chi tiết Phương án 1 (SQL Full Projection):
- **Cơ chế:** Viết trực tiếp phép ghép chuỗi trong câu truy vấn chính:
  `SELECT TOP (@pageSize) CONVERT(varchar(23), UpdateTime, 121) + '_' + CAST(ID AS varchar(64)) AS __lastKey, ...`
  `WHERE (CONVERT(varchar(23), UpdateTime, 121) + '_' + CAST(ID AS varchar(64))) > @lastKey ORDER BY __lastKey ASC`.
- **Ưu điểm:** Rất đơn giản, toàn bộ logic gói gọn trong 1 câu truy vấn SQL duy nhất, tầng Worker hoàn toàn trong suốt chỉ đọc cột `__lastKey` của dòng cuối cùng.
- **Rủi ro khi mở rộng (Scale Bottleneck):**
  - Mệnh đề `WHERE` chứa hàm `CONVERT` và phép cộng chuỗi `+` là biểu thức **Non-Sargable** kinh điển trong thiết kế CSDL.
  - SQL Server không thể sử dụng cấu trúc cây B-Tree Index của cột `UpdateTime` hay `ID`. CSDL buộc phải thực hiện quét toàn bộ bảng (Table Scan / Clustered Index Scan) qua hàng triệu bản ghi, tính toán ghép chuỗi cho từng dòng rồi mới so sánh với `@lastKey`.
  - Hậu quả: Tiêu tốn Disk IO, CPU nhảy vọt lên 100%, tăng nguy cơ khóa tương tranh (lock contention/deadlock) và gây timeout khi dữ liệu tăng trưởng lớn.

#### B. Nút thắt cổ chai của Phương án 2 (SQL Single Row Evaluation / Trì hoãn tính toán):
- **Cơ chế đề xuất trong trao đổi:** Câu SQL chính chỉ lấy các cột nghiệp vụ nguyên bản. Khi Worker duyệt tới dòng cuối cùng (đã có `ID` của dòng cuối), Worker thực thi thêm một câu truy vấn phụ: `SELECT [công_thức_ghép_chuỗi] WHERE ID = @lastRowId` để lấy chuỗi mốc lưu lại vào CSDL.
- **Nút thắt cổ chai cốt lõi:**
  1. *Tiết kiệm nhầm chỗ:* Việc bỏ phép ghép chuỗi ở `SELECT` cho 99 dòng đầu chỉ tiết kiệm được vài micro-giây CPU trên RAM, không mang lại ý nghĩa đáng kể cho hệ thống.
  2. *Mệnh đề WHERE ở chu kỳ sau vẫn bị nghẽn:* Lần quét kế tiếp, Worker truyền chuỗi mốc vừa tính vào `@lastKey`. Câu SQL chính vẫn phải so sánh `WHERE (CONVERT(...) + '_' + ID) > @lastKey` nên CSDL **vẫn bị Table Scan toàn bộ bảng 100%**, nút thắt I/O nghiêm trọng nhất vẫn còn nguyên.
  3. *Tăng độ trễ mạng (Network Latency):* Mỗi đợt quét Worker phải gửi 2 round-trips mạng xuống CSDL (Query 1 lấy dữ liệu + Query 2 tính mốc), làm tăng gấp đôi độ trễ mạng so với 1 câu truy vấn đơn.

#### C. Phân tích Phương án 3 (C# Worker Mapping):
- **Cơ chế:** CSDL chỉ trả về các trường gốc (`UpdateTime`, `Code/ID`). Tầng Worker dùng C# ghép chuỗi `d.UpdateTime.ToString(...) + "_" + d.Code`.
- **Đánh giá:** Rất tối ưu cho CSDL nhưng bị phụ thuộc cứng vào mã nguồn C# (hardcoded logic), phải mở code ra tra cứu tên thuộc tính, biên dịch lại service khi có thay đổi, triệt tiêu tính tùy biến động trên giao diện UI cho SuperAdmin. Do đó cuộc họp đã thống nhất gác lại.

| Tiêu chí | PA1: SQL Full Projection | PA2: SQL Single Row Formula | PA3: C# Worker Mapping |
| :--- | :--- | :--- | :--- |
| **Bản chất mốc** | Chuỗi ghép 1 biến `@lastKey` | Chuỗi ghép 1 biến `@lastKey` | Chuỗi ghép 1 biến `@lastKey` |
| **Mệnh đề WHERE** | `WHERE CONCAT(...) > @lastKey` | `WHERE CONCAT(...) > @lastKey` | `WHERE CONCAT(...) > @lastKey` |
| **Sử dụng Index** | ❌ Không (Non-Sargable) | ❌ Không (Non-Sargable) | ❌ Không (Non-Sargable) |
| **Tải trên CSDL** | 🔴 Rất nặng (Table Scan + CPU) | 🔴 Rất nặng (Table Scan) + 2 query | 🟡 Vẫn Table Scan ở WHERE |
| **Số Round-trip** | 1 round-trip | **2 round-trips** | 1 round-trip |
| **Tính linh hoạt UI** | Cao (1 câu SQL) | Trung bình (cần thêm ô công thức) | Thấp (phụ thuộc code C#) |
| **Đánh giá hệ thống** | Làm trước vì đơn giản; dữ liệu lớn hoặc chạy lại từ đầu thì hay nghẽn ở nối chuỗi [13:49 – 14:09] | Cân nhắc cho bảng vài triệu dòng; làm lên rồi đo hiệu năng để chọn giữa PA1 và PA2 [13:31 – 14:25] | Gác lại: khó, phụ thuộc mã C# [14:28 – 15:23] |

> 📌 **Ghi chú bổ sung 29/10/2026 — kết quả đo, không phải lời họp.**
> Đo bằng test `tests/BE/ITS/ShareData/Performance/CursorQueryPerformanceTests.cs` trên bảng 1.000.000 dòng có chỉ mục `(UpdateTime, ID)`, lấy trang 100 dòng khi mốc ở gần cuối bảng (trạng thái chạy định kỳ):
>
> | Cách | Trang đọc mỗi lượt | Ghi chú |
> |---|---:|---|
> | PA1 | 31.588 | Quét cả bảng rồi sắp xếp |
> | PA2 | 11.790 | Quét chỉ mục từ đầu tới mốc, thêm 1 lượt gọi DB |
> | PA3 | 11.786 | Quét chỉ mục từ đầu tới mốc |
> | Câu mẫu gói 101 trước khi sửa | 11.786 | Dùng lại kế hoạch dựng lúc chưa có mốc: 4.072.255 |
> | Câu mẫu gói 103–111 (`COALESCE`) | 31.588 | Viết lại kiểu `>=` vẫn quét vì so biểu thức |
> | Câu 2 cột viết lại (dưới đây) | 421 | Index Seek ở mọi vị trí mốc, kể cả khi dùng lại kế hoạch |
>
> Mọi cách trả cùng một trang và cùng mốc kế tiếp. PA2 không giảm được chi phí vì điều kiện `WHERE` vẫn so chuỗi ghép. Câu mẫu gói 101 đã sửa theo dạng:
>
> ```sql
> WHERE zs.UpdateTime >= ISNULL(@lastTime, '17530101')
>   AND (@lastTime IS NULL OR zs.UpdateTime > @lastTime OR zs.ID > @lastKey)
> ORDER BY zs.UpdateTime ASC, zs.ID ASC
> ```

---

### 1.3. Kiến Trúc Lưu Trữ Trạng Thái Offset & Chống Xung Đột Tương Tranh

1. **Bộ 3 định danh bắt buộc: `(Mã gói, Đối tác, Key) -> Value`:**
   - Mỗi đối tác có tốc độ tiếp nhận và tiến độ xử lý dữ liệu khác nhau. Một gói tin mẫu (ví dụ gói 101) phục vụ đồng thời nhiều đối tác (Đối tác A, Đối tác B...).
   - **Quy tắc bất biến:** Giá trị `Value` của mốc chạy thật bắt buộc phải gắn liền với `Mã gói` và `Mã đối tác` (`PartnerId`).
   - ⛔ **CẤM:** Tuyệt đối không được lưu mốc chạy thật vào cấu hình mẫu chung. Nếu lưu chung, khi Đối tác A vừa chạy tới mốc `10:10`, Đối tác B chạy sau sẽ đọc đè mốc này làm mất dữ liệu hoặc gây xung đột nghiêm trọng.
2. **Tách biệt 2 loại tham số:**
   - **Tham số cố định (Static Parameter):** `@pageSize = 100` (giá trị cố định, do cấu hình hệ thống quy định).
   - **Tham số trạng thái động (Stateful Parameter):** `@lastKey` (mốc giá trị tăng dần, cập nhật liên tục sau mỗi mẻ quét thành công).
3. **Giá trị khởi tạo mặc định (`default value`):**
   - Hỗ trợ thiết lập mốc bắt đầu (ví dụ: ngày giờ cụ thể `20261028000000_A`) cho lần chạy đầu tiên để chủ động bỏ qua dữ liệu rác/thử nghiệm phát sinh trước đó.
   - Nếu không cấu hình mốc bắt đầu, hệ thống quét từ đầu lịch sử (`WHERE ... > ''` hoặc bỏ mệnh đề WHERE).

---

### 1.4. Trục Thời Gian Tăng Dần & Cơ Chế Lấy Bản Ghi Mới Nhất

- **Nguyên lý sắp xếp tăng dần (`cũ -> mới`):**
  - Mọi câu truy vấn dữ liệu nối đuôi bắt buộc phải có mệnh đề sắp xếp tăng dần: `ORDER BY Date ASC, Time ASC, Code ASC` (hoặc `ORDER BY __lastKey ASC`).
  - Khi duyệt danh sách kết quả từ trên xuống dưới:
    - Các bản ghi cũ hơn nằm ở đầu danh sách.
    - Bản ghi mới nhất luôn nằm ở vị trí cuối cùng của danh sách (`last item of list`).
- **Cơ chế ghi nhận mốc:**
  - Worker xử lý xong mẻ dữ liệu, đọc giá trị của cột `__lastKey` tại dòng cuối cùng, rồi commit giá trị này vào bảng offset với `Key = '__lastKey'`.
  - Ở chu kỳ quét tiếp theo, Worker đọc giá trị đã lưu truyền vào tham số `@lastKey` để tiếp tục trích xuất các bản ghi kế tiếp (`WHERE __lastKey > @lastKey`).

---

### 1.5. Thống Nhất Giao Diện Tối Giản (Wireframe W0.2)

- **Triệt tiêu phức tạp thừa trên UI:**
  - Không xây dựng các nút bấm "⚡ Thiết lập mốc khóa" hay popup wizard hướng dẫn tự động chèn SQL gây rối mắt người dùng.
  - Khối 3 (Tham số điều kiện) giữ đúng tinh thần Wireframe W0.2 tối giản: hiển thị bảng 3 cột gọn gàng:
    - Cột 1: `Tham số` (`@pageSize`, `@lastKey`).
    - Cột 2: `Nguồn giá trị` (`Giá trị cố định`, `Tự động nối tiếp lần trước (theo key)`).
    - Cột 3: `Giá trị cấu hình` (`100`, tùy chọn mốc bắt đầu).
  - Lập trình viên trực tiếp viết câu SQL có `@lastKey` và `__lastKey` trong khung soạn thảo SQL, hệ thống tự động phân tích và ánh xạ tham số chuẩn xác.

---

## 2. Kịch bản & Lời thoại chi tiết theo mốc thời gian (Detailed Transcript)

*(Toàn văn tệp ghi âm: `DocBusinessThienAn/Sharedata_28_10.m4a` — Thời lượng: 15 phút 25 giây)*

---

### PHẦN 1: CẤU TRÚC KHÓA NỐI ĐUÔI THEO GÓI TIN & NGUYÊN LÝ LẤY DÒNG CUỐI CÙNG
*(Thời gian: 00:00 – 04:08)*

- `[00:00 - 00:15]` **Tech Lead:** Thường kỳ thì phải... phải theo một cái cấu trúc gì đó... theo cấu trúc tức là theo... theo cái giá trị mình nhập. Cái giá trị này nó đang nói dưới server kiểu... mã đối tác, mã gói... hay mã gói, mã đối tác gì đó.
- `[00:16 - 00:30]` **Tech Lead:** Nhưng mà cái dữ liệu, cái dữ liệu này nó đi theo từng dữ liệu chứ không phải là nó... nó fix, nên là mình phải có cấu... cấu trúc.
- `[00:31 - 00:52]` **Tech Lead:** Ví dụ gói 101 đi, thì anh muốn nó là... cái cột UpdateTime chẳng hạn... Anh muốn nó là yyyy... MMddHHmmssfff... gạch Code.
- `[00:53 - 01:10]` **Tech Lead:** Nhưng mà gói 102 đi ha, thì muốn nó là cái trường PacketTime... yyyyMMddHHmmss... Cùng thời... Không cần fff, vì cơ bản là nó không chạy được đến fff.
- `[01:11 - 01:30]` **Tech Lead:** Ví dụ như dữ liệu về thời tiết chẳng hạn, hay gì đó, thì cơ bản là không thể nào mà nó... một cái dữ liệu nó chạy lặp lại mấy giây, trong vòng mấy mili-giây. Thường ví dụ một tiếng, hai tiếng mới có dữ liệu... hoặc có thể nó đến, nhiều khi đến phút thôi. Nên mỗi cái nó không... Mỗi cái nó có một cách.
- `[01:31 - 01:42]` **Dev:** Mỗi cái nó có một cách, mà cái cách này là mình tự... xử lý dưới... cái hạ tầng của mình đây nè.
- `[01:43 - 02:03]` **Tech Lead:** Đúng rồi. Có nghĩa là mình phải nhập cái name, làm sao để trong code mình hiểu được là nó sẽ xử lý như thế nào. Đó, làm sao để nó nối được ra thành cái chuỗi này nè. Nó sẽ dùng cái chương trình nào để nó ra được cái này. Rồi nó kết hợp với cái thằng nào đó. Tức là phải có một cái công thức.
- `[02:04 - 02:15]` **Tech Lead:** Hôm đó anh nói là có 2 cách. Như hôm bữa 2 cách đó. Một là đẩy xuống SQL làm.
- `[02:16 - 02:26]` **Dev:** Ừ.
- `[02:27 - 02:45]` **Tech Lead:** Đẩy xuống SQL làm thì nó sẽ dễ. Có cái là nó nối lại hết rồi. Mình chỉ còn khai báo cái... trường dữ liệu nào làm key đó. Ví dụ như mình nối sẵn ở SQL cái này, cột của nó là cột A đi. Thì mình sẽ khai báo cái thằng lastKey này... nó sẽ dựa theo cái column A để nó lấy.
- `[02:46 - 02:48]` **Dev:** Đang gắn này...
- `[02:49 - 03:06]` **Tech Lead:** Đúng không? Thì về cơ bản khi mà nó chạy, thì cái thời gian chạy theo nguyên tắc thường nó sẽ tăng dần. Đúng không em? Nó sẽ tăng dần. Tăng dần thì dữ liệu cũ nó sẽ đứng trước, dữ liệu mới nó sẽ đứng cuối cùng.
- `[03:07 - 03:15]` **Tech Lead:** Thì kiểu gì khi mà em duyệt đến cái dòng cuối cùng, em lấy được cái cột A này, bao giờ nó cũng là cái dữ liệu... mới nhất trong cái danh sách đó.
- `[03:16 - 03:17]` **Dev:** Mới nhất.
- `[03:18 - 03:32]` **Tech Lead:** Đó, thì lấy cái cột đó ra, giá trị nó là X, mình sẽ lưu cái X đó lại vào một cái key gì đó. Đúng không? Thì key này nó sẽ bao gồm là cái gì? Cái gói tin, đối tác, và cái key đó.
- `[03:33 - 03:46]` **Tech Lead:** Thì mình sẽ lưu cái giá trị value này vào trong cái value này. Đó, rồi lần sau nó lấy, nó WHERE, điều kiện...
- `[03:47 - 04:08]` **Tech Lead:** Nó đi tiếp thì nó làm gì? Nó lấy cái lastKey... nó sẽ móc cái value kia ra.

---

### PHẦN 2: THIẾT LẬP MỆNH ĐỀ TRUY VẤN, GIÁ TRỊ KHỞI TẠO VÀ BẢNG LƯU TRỮ OFFSET
*(Thời gian: 04:09 – 10:14)*

- `[04:09 - 04:26]` **Tech Lead:** Đó là cách đầu tiên là sẽ đẩy xuống SQL làm. Thì đẩy xuống SQL làm cái dễ á là không... không phải nhớ công thức dài dòng. Không cần cài công thức, mình chỉ lưu cái tên trường này... mình lấy.
- `[04:27 - 04:37]` **Tech Lead:** Về cơ bản nó chính là cái hàng... hàng này luôn nè. Nó sẽ nằm ở khúc chỗ này nè.
- `[04:38 - 04:40]` **Dev:** Mình coi khoảng nha.
- `[04:41 - 05:07]` **Tech Lead:** Đó, nó cũng gợi ý ra vậy. Nó giống như cái cột lastTime á em. Có nghĩa là một cái trường... một cái trường giống như vầy nè. Nó giống kiểu như là một cái trường nội bộ để mình quản lý. Giống như là em SELECT ra, em SELECT em tự động em nối để nó ra một cái key gì đó. Thì khi mà em SELECT nó sẽ ra một cái trường này. Mình làm cái trường này mình đi lưu luôn nó, kiểu vậy đó. Cầm cái trường đó mình đi lưu luôn.
- `[05:08 - 05:16]` **Tech Lead:** Thì lúc đó là cái `@lastKey` đó, mình sẽ gán...
- `[05:17 - 05:30]` **Tech Lead:** Thì khi mà nó duyệt đến dòng cuối cùng, dòng cuối cùng của danh sách, thì nó sẽ phải lấy được cái thằng này ra để nó lưu lại. Khi mà nó thiết lập theo cái lần... theo cái giá trị trước đó... lần trước đó.
- `[05:31 - 05:46]` **Tech Lead:** Nếu mà ở đây thiết lập giá trị lần trước, thì nó phải lấy được cái thằng thiết lập ở đây. Để nó lấy cái giá trị... nó đưa vô trỏng. Lấy giá trị ra nó đưa vô trỏng. Rất là đơn giản thôi.
- `[05:47 - 06:05]` **Tech Lead:** Còn ở dưới đây là... cái bảng này là... cái bảng này làm cũng mấy lần rồi nè. Code, đặt cho nó một cái trường giống như là... Key đi. Rồi Value. Thì cái lastKey này mình cũng nối nó thành...
- `[06:06 - 06:24]` **Tech Lead:** Khi mà nó duyệt xuống, chắc chắn phải ORDER rồi, không thể nào không ORDER được. ORDER dựa trên cái cặp này luôn: ORDER BY Ngày, Date, Time, Code... ORDER BY ba thằng này luôn.
- `[06:25 - 06:45]` **Tech Lead:** Và WHERE cũng y chang vậy. Nếu mà trước đó nó không có giá trị gì đó thì để... nó bằng một cái giá trị mặc định gì đó. Ví dụ như là để bằng rỗng hay gì đó để nó lấy từ đầu. Hoặc nếu mà không có giá trị gì, khỏi WHERE này luôn để lấy từ đầu.
- `[06:46 - 07:10]` **Tech Lead:** Hoặc là mình gán một cái giá trị min... khởi tạo, bắt đầu. Ví dụ như cái giao dịch dữ liệu này mình muốn... nó phát sinh dữ liệu, cái dữ liệu này có thể nó phát sinh từ lúc chạy thử rồi, thì nhiều tình huống mình không muốn nó gửi dữ liệu chạy thử, thì mình sẽ cố định luôn là nếu mà không có giá trị thì set cái default value... bằng một cái giá trị gì đó. Ví dụ như set trước cái key này luôn, nhập tay vô: 2026...
- `[07:11 - 07:40]` **Tech Lead:** Thì lúc đó nó sẽ lấy cái thằng này. Còn khi mà nó đã có giá trị mới nhất nó lấy rồi, khi mà chạy đúng không, đứng gần chắc chắn 2 thằng này sẽ lấy được. Có thằng A này, xử lý không có vấn đề gì. Đến thằng B, thì nó là giá trị cuối rồi, last list rồi, giá trị cuối của list rồi.
- `[07:41 - 08:05]` **Tech Lead:** Thì lúc đó là mới xét tới là bây giờ... cái lastKey tiếp theo... à nó đang... nó đang sử dụng nguồn giá trị là lần trước đó, thì suy ra là phải lưu lại. Lưu lại giá trị trước đó. Mà lưu lại giá trị trước đó đang đặt là cái cột lastKey. Thì suy ra là phải lấy được cái lastKey của thằng B này, last của thằng B này luôn. Cầm cái giá trị này đi lưu lại vào trong cái bảng.
- `[08:06 - 08:24]` **Tech Lead:** Để làm sao để lúc sau nó chạy nó lấy lại được thằng này. Nhưng mà khi lưu thì không có lưu một mình nó, mà khi lưu thì nó sẽ có cái cặp này: gói tin, đối tác, và cái key. Phải đặt cho nó cái key gì đó để nó lưu lại thì mới đúng.
- `[08:25 - 08:35]` **Tech Lead:** Chứ cái nào cũng để lastKey, lastKey, lastKey... một đống lastKey là nó trùng hết. Mình phải đặt cho nó một cái tên định danh cho nó.
- `[08:36 - 08:42]` **Dev:** Đặt theo ID của data...
- `[08:43 - 09:05]` **Tech Lead:** Đúng rồi, ví dụ như là cái này gói 1.1 đi ha, 101... key gì đó, key data gì đó, để lúc đó nó sẽ hiểu được trên thằng này. Hoặc cái gì đó. Mỗi cái key tại vì cơ bản là nó sẽ dựa trên gói tin với đối tác nó không bao giờ trùng được.
- `[09:06 - 09:15]` **Dev:** Đúng rồi, nó đâu có trùng được. Tại nó đi theo cái mã đối tác... mã gói, mã đối tác...
- `[09:16 - 09:40]` **Tech Lead:** Lúc đầu là tính là nối, nối chuỗi cái key này nè. Nhưng mà thôi mắc công á, chia làm ba luôn cho nó dễ: Đối tác xác định luôn, gói tin xác định luôn, và cái key nó ném nó vô. Lưu lại cái mục đích này.
- `[09:41 - 10:00]` **Tech Lead:** Lúc trước thì chỉ lưu cái key thôi, lúc trước chỉ định danh cái key thôi. Thì cái key đó nhiều tình huống là khác đối tác là phải đi nối chuỗi á. Tự động nối chuỗi chèn cái đối tác vô... để cho khác cái key. Nhưng mà làm mất công á, nên thôi chia nó ra. Lưu một ô luôn cho nó dễ.
- `[10:01 - 10:14]` **Tech Lead:** Đó, mục tiêu là lấy lần sau nó chạy nó lấy thằng này mới. Ví dụ đây chạy tiếp đến thằng B nè, thằng C, thằng D gì đó... Ví dụ lấy được hai dòng B, C thì cái lần tiếp theo nó lưu lại thằng D...

---

### PHẦN 3: ĐÁNH GIÁ 3 PHƯƠNG ÁN KỸ THUẬT & TỐI ƯU TRUY VẤN DỮ LIỆU LỚN
*(Thời gian: 10:15 – 13:08)*

- `[10:15 - 10:25]` **Tech Lead:** Và lần sau nó chạy nó sẽ lấy lớn hơn thằng này tiếp. Nó quay lại quy trình. Thì đó là cách đầu là cái... cách đẩy xuống cho SQL làm. Một cách ha.
- `[10:26 - 10:27]` **Dev:** Một cách.
- `[10:28 - 10:35]` **Tech Lead:** Thì nó sẽ có cái khuyết là nó sẽ bị vướng vào chỗ mà... tính toán nó xuống truy vấn á.
- `[10:36 - 10:39]` **Dev:** Truy vấn... hiệu năng lùi đúng không?
- `[10:40 - 10:45]` **Tech Lead:** Đúng rồi, nối chuỗi. Cứ cấp nào cứ... cho nó xong. Còn cách kia... Cách kia là đẩy xử lý lên code hả?
- `[10:46 - 10:49]` **Dev:** Qua C# gì?
- `[10:50 - 10:55]` **Tech Lead:** Ừ. Mình có tới 2 cách để đẩy lên.
- `[10:56 - 11:15]` **Dev:** Tức là làm một cách, rồi một cách kia nữa, rồi xong rồi phân tích nọ kia. Đưa AI nó chuyển qua nhanh lắm.
- `[11:16 - 11:35]` **Tech Lead:** Có cách này... anh thấy nó sẽ dễ hơn, mà anh không biết nó tối ưu không. Đó là: Cũng y chang này luôn, cũng y chang như này, nhưng mà cái chỗ này nè, chắc chắn chỗ này nó phải có cái hàm nối chuỗi rồi. Đúng không? Ví dụ như cái hàm nối chuỗi là cái câu SQL đó.
- `[11:36 - 11:55]` **Tech Lead:** Thay vì như này, anh sẽ cầm nguyên cái cụm này nè, lưu... lưu xuống dưới này. Thấy chưa? Lưu xuống. Thì về bản chất là tới cái lúc... cái lần cuối nó chạy á, nó giống như thằng này nè: Thằng A xong đến thằng B nó chạy, đã biết được nó là thằng đứng cuối danh sách rồi.
- `[11:56 - 12:12]` **Tech Lead:** Chắc chắn phải xác định được, cuối danh sách. Thì bắt đầu nó mới lấy cái thằng đó ra. Lấy thằng đó ra nó sẽ thấy được là cái... cái gì ta? Cái nó gọi là cái gì ta? Cái công thức... Công thức xử lý SQL.
- `[12:13 - 12:35]` **Tech Lead:** Về cơ bản nó gọi lại cái hàm này: `SELECT` cái công thức này `WHERE ID = ...` gì đó. Hiểu không? Thì lúc đó nó chỉ ảnh hưởng một dòng đó thôi. Chứ nó không có phải nó đi nó... nó phải nó gán hết tất cả.
- `[12:36 - 12:50]` **Tech Lead:** Tại vì cơ bản nó gán hết thì thằng này cũng bỏ. Ví dụ như 100 dòng thì tất cả bỏ hết 99 dòng đầu rồi. Chỉ có cái dòng cuối là nó lấy được thôi. Còn thằng này là nó chỉ tác động đúng một dòng cuối cùng. SELECT ra đúng một dòng cuối cùng.
- `[12:51 - 12:55]` **Dev:** Ok...
- `[12:56 - 13:08]` **Tech Lead:** Đó, thì là cách thứ hai. Còn cách thứ ba là cái công thức này là biến thành công thức của C#. Biến thành code C# luôn. Nhưng cách 3 chắc sẽ khó. Mình ưu tiên cách 1 với cách 2 thôi.

---

### PHẦN 4: KẾT LUẬN ĐỊNH HƯỚNG TRIỂN KHAI & PHÂN CÔNG THỰC HIỆN
*(Thời gian: 13:09 – 15:25)*

- `[13:09 - 13:12]` **Dev:** Nhưng cho em hỏi...
- `[13:13 - 01:14]` **Tech Lead:** Nói chung vòng vòng vậy thôi.
- `[13:15 - 13:16]` **Dev:** Đó cũng một vấn đề.
- `[13:17 - 13:18]` **Tech Lead:** Hai cái thôi. Hiểu là được.
- `[13:19 - 13:23]` **Dev:** Không, nhưng mà tính ra nó nghe xong á, nó cũng phát triển lên hòm hòm lên được.
- `[13:24 - 13:30]` **Tech Lead:** Hiểu được là... hình dung rồi.
- `[13:31 - 13:43]` **Tech Lead:** Mấy cái... mai đi em báo cáo lại ha. Mai chắc lên được á. Còn cái vụ xử lý thì tùy. Hai cái anh gợi ý vậy đó, xem cái nào tối ưu hơn. Anh thì anh chắc thiên về...
- `[13:44 - 13:48]` **Dev:** Bây giờ thà tối ưu không, thì mình cứ lên đi rồi xong mình... Mình chạy hiệu năng đồ này nọ.
- `[13:49 - 14:04]` **Tech Lead:** Hồi trước, trước là hay dùng kiểu này nè. Trước là bên mình hay dùng kiểu này. Nhưng mà dùng kiểu này thì thời gian á, dữ liệu nó lớn á, ví dụ như là tình huống là nó phải chạy lại hết dữ liệu á, là nó hay bị nghẽn chỗ này nè. Tại vì nó phải đi xử lý câu lệnh nhiều.
- `[14:05 - 14:09]` **Dev:** Nối chuỗi, ờ đúng rồi nối chuỗi nó nặng á, nối chuỗi...
- `[14:10 - 14:25]` **Tech Lead:** Bảng tới bảng mấy... mấy triệu dòng. Thì có thể xem xét là cái kia, cái kia là chỉ lấy cái cuối. Có dòng nào là lấy dòng đó. Tác động đúng cái dòng đó thôi.
- `[14:26 - 14:27]` **Dev:** Ghê ha...
- `[14:28 - 14:36]` **Tech Lead:** Còn cái C# thì thôi. C# thì... thực ra C# là tối ưu nhất á, nhưng mà C#... C# lại khó.
- `[14:37 - 14:43]` **Dev:** C# nếu mà xử lý thì nó cũng nặng trên RAM... Nó bị phụ thuộc code quá.
- `[14:44 - 15:00]` **Tech Lead:** Tại vì C# thì em mới khi khai báo... Cái này ví dụ như cái này em còn truyền... còn truyền cái cột được. Còn C# là em phải mở code ra em coi cái biến đó là cái biến gì. Em mới chấm được.
- `[15:01 - 15:15]` **Tech Lead:** Đấy, là phải biết được, ví dụ như mình đặt cái biến qua `d`, thì phải truyền đúng cái `d` là cái gì vào. Còn cái này á, mở cái bảng ra là biết rồi. Mở cái câu truy vấn ra là thấy rồi. Nên là cái C# là cái cuối cùng. C# thì nó hơi khó.
- `[15:16 - 15:18]` **Dev:** Khó thì khỏi bàn.
- `[15:19 - 15:23]` **Tech Lead:** Bỏ qua. Thì hai cái đầu thôi.
- `[15:24 - 15:25]` **Dev:** Ok anh.

