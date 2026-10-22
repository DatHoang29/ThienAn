---
tier: A
read: full
source:
  - /Users/hoangquydat/Downloads/Ghi âm 2.m4a (48:38 - toàn văn 7 chunks)
  - DocBusinessThienAn/duong_so_8.m4a (39:29)
  - DocBusinessThienAn/duong_so_8_2.m4a (19:16 - phần ShareData)
date: 22-10-2026
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData — Quản lý Gói tin Mẫu (Template Query) vs Tùy biến (Custom Query theo Đối tác), Cơ chế So sánh Đối chiếu (Diff/Compare) khi Cấu hình Gốc thay đổi, Tái cấu trúc Tab Form Cấu hình Kết nối, Scale Giao diện Ánh xạ, Xử lý Chuỗi Lỗi Gói tin
participants:
  - Tech Lead
  - Dev
corrections:
  - '22-10-2026 — [33:03] chép sai "tab thứ 3 thiết lập chạy/retry"; người tham dự xác nhận chỉ 2 tab.'
  - '22-10-2026 — gỡ phần dữ liệu sự cố (duong_so_8_2 [03:28]–[07:46], mục 1.7, chủ đề 7): nguyên nhân là script dữ liệu giả lập, đã xử lý.'
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ ShareData — Quản lý Gói tin Mẫu, Xử lý Tùy biến Đối tác, Tái cấu trúc Form Kết nối & Xử lý Lỗi Gói tin
### (Nguồn từ tệp ghi âm: duong_so_8.m4a & duong_so_8_2.m4a)

- **Ngày ghi nhận:** 22/10/2026
- **Thời lượng:** 39 phút 29 giây (`duong_so_8.m4a`) + 19 phút 16 giây (`duong_so_8_2.m4a`)
- **Người tham gia:** Tech Lead, Dev
- **Chủ đề chính:**
  1. **Quản lý câu lệnh truy vấn động (Dynamic Query) — Gói tin mẫu (Template/Default) vs Gói tin tùy biến (Custom theo đối tác):** Phân định luồng dữ liệu khi tạo mới và kế thừa cấu hình gói tin. Ban đầu hệ thống nạp câu SQL mẫu chuẩn; khi đối tác có yêu cầu riêng (lọc trạm, lọc lý trình, thêm bớt trường), câu truy vấn trở thành Custom Query độc lập của đối tác đó.
  2. **Cơ chế xử lý khi câu SQL mẫu hoặc cấu hình DB thay đổi (Bài toán xung đột & So sánh Diff/Compare):** Giải quyết tình huống câu truy vấn mẫu hoặc schema cơ sở dữ liệu thay đổi (đổi tên cột `toKmNumber`, đổi index, tối ưu câu lệnh). Đề xuất và chốt giải pháp so sánh trực quan hai bên (Compare / Diff tương tự Git Diff) để người dùng xem điểm khác biệt và chủ động chọn ghi đè (Replace/Merge) hoặc giữ nguyên câu Custom của đối tác, tránh việc tự động ghi đè làm mất cấu hình riêng.
  3. **Ánh xạ cột dữ liệu (Field & Alias Mapping) từ câu `SELECT`:** Chuẩn hóa việc phân tích danh sách cột trích xuất sau mệnh đề `SELECT ... AS ...`. Đối chiếu tự động với danh sách thuộc tính chuẩn của gói tin; nhận diện các trường đã ánh xạ, trường chưa có hoặc trường dư thừa; cho phép bổ sung hoặc điều chỉnh linh hoạt.
  4. **Cơ chế quét dữ liệu nối đuôi & Lưu vết tham số (@LastKey / Composite Key):** Làm rõ việc lưu giữ trạng thái quét ở chu kỳ trước (mốc thời gian, ZoneId, khóa tổng hợp) vào bảng quản lý trạng thái để chu kỳ sau truyền vào câu query tiếp tục lấy dữ liệu mới phát sinh, không gây trùng hoặc sót bản ghi.
  5. **Tái cấu trúc giao diện Form Cấu hình Kết nối Đối tác (`connectionConfigForm`):** Tránh dồn toàn bộ trường cấu hình vào một màn hình dài gây rối mắt và dễ thao tác nhầm. Phân tách thành 2 Tab: Thông tin chung (Mã, Tên, Ghi chú) và Cấu hình kết nối (Giao thức API/FTP/Kafka/File, Địa chỉ/Cổng/Endpoint, Xác thực, thời gian chờ).
  6. **Thu gọn và Tối ưu Scale Giao diện Ánh xạ Trường (`mappingForm`):** Xử lý lỗi giao diện khi mới mở lên chưa có dữ liệu bị bung tỷ lệ (scale) quá to, để lại khoảng trống dài bất hợp lý. Đặt mặc định giao diện co gọn, khi nạp dữ liệu thì tự động co giãn vừa vặn.
  7. **Xử lý Hiển thị Thông tin Lỗi Gói tin (Packet Error Handling):** Thông tin lỗi hệ thống dài (chứa exception/stack trace) ở Bước 1, Bước 2 làm vỡ giao diện; thống nhất cắt ngắn chuỗi hiển thị trên UI (vài chục đến tối đa vài trăm ký tự), bảo toàn toàn văn lỗi trong file log hệ thống.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Kiến trúc Gói tin Mẫu (Template Query) và Gói tin Tùy biến (Custom Query)
- **Thực trạng và Cơ chế kế thừa:**
  - Hệ thống duy trì tập danh mục gói tin chuẩn (11 gói tin chia sẻ dữ liệu từ 101 trở đi). Mỗi gói tin có một câu truy vấn SQL mẫu mặc định (`Default/Template Query`) được sinh ra từ hệ thống.
  - Khi cấu hình chia sẻ dữ liệu cho một đối tác mới, hệ thống nạp nguyên bản câu SQL mẫu của gói tin đó.
  - Nếu đối tác sử dụng nguyên bản dữ liệu chuẩn, cấu hình của đối tác vẫn tham chiếu/đồng bộ theo gói mẫu.
  - Khi đối tác có yêu cầu đặc thù (ví dụ: chỉ lấy dữ liệu của phân đoạn đường `Section A`, hoặc thêm bớt các thuộc tính đầu ra), người dùng chỉnh sửa câu SQL và bấm Lưu -> hệ thống chuyển trạng thái của gói tin đối tác này sang **Tùy biến riêng (Custom Query)** và lưu câu SQL độc lập cho đối tác.

### 1.2. Giải pháp Xung đột khi Cấu hình Gốc / DB thay đổi (Cơ chế Compare / Diff)
- **Vấn đề đặt ra:** Trong quá trình vận hành, câu truy vấn mẫu của gói tin chuẩn có thể phải cập nhật (do tối ưu hiệu năng, bổ sung điều kiện lọc hệ thống, hoặc CSDL thay đổi cấu trúc bảng/cột như `Z.toKmNumber`, đổi index...).
  - Với các đối tác đang dùng mẫu mặc định: Tự động kế thừa câu SQL mới.
  - Với các đối tác đã có `Custom Query`: Nếu tự động ghi đè sẽ làm mất toàn bộ logic tùy biến của đối tác; nếu không cập nhật thì câu Custom có nguy cơ bị lỗi thời hoặc gãy câu lệnh SQL.
- **Quyết định kỹ thuật chốt:**
  - **Không tự động ép đè:** Hệ thống tuyệt đối không tự động ghi đè lên các câu lệnh Custom của đối tác khi câu mẫu thay đổi.
  - **Tích hợp màn hình So sánh Đối chiếu (Compare / Diff Tool):**
    - Thiết kế giao diện trực quan 2 cột (Side-by-Side Diff) tương tự cơ chế Diff của Git:
      + Cột bên trái: Câu lệnh SQL mẫu chuẩn mới nhất.
      + Cột bên phải: Câu lệnh Custom hiện tại của đối tác.
    - Làm nổi bật (highlight) các dòng code SQL khác biệt giữa hai phiên bản.
    - Cung cấp thao tác cho phép quản trị viên xem xét và lựa chọn: thay thế toàn bộ (Replace all), chọn lọc từng dòng cần cập nhật (Merge), hoặc giữ nguyên câu Custom hiện tại nếu sự thay đổi không ảnh hưởng đến nghiệp vụ của đối tác.

### 1.3. Cơ chế Phân tích Ánh xạ Trường (Field & Alias Mapping) từ câu `SELECT`
- **Quy trình trích xuất tự động:**
  - Khi người dùng nhập hoặc chỉnh sửa câu truy vấn SQL trong trình soạn thảo, hệ thống phân tích cú pháp mệnh đề `SELECT` để trích xuất danh sách các alias (`SELECT col1 AS Alias1, col2 AS Alias2...`).
  - Hệ thống đối chiếu danh sách alias này với danh sách trường chuẩn của gói tin:
    + **Các trường đã khớp (Mapped):** Hiển thị ở bảng danh sách các trường hợp lệ đã được ánh xạ.
    + **Các trường có trong câu truy vấn nhưng chưa có trong danh mục chuẩn:** Hiển thị ở khu vực riêng để người dùng biết và có thể bấm bổ sung trường mới vào danh sách thuộc tính.
    + **Các trường trong danh mục chuẩn nhưng câu truy vấn chưa lấy ra:** Hiển thị cảnh báo để người dùng nhận biết gói tin đang thiếu dữ liệu trường nào.
  - Cho phép người dùng linh hoạt điều chỉnh tên Alias (hỗ trợ hoa/thường, chuẩn hóa mã định danh).

### 1.4. Quản lý Tham số Điều kiện Lọc Nối đuôi (@LastKey / Composite Key)
- Các điều kiện lọc nối đuôi (Incremental) được thể hiện trực tiếp trong câu lệnh SQL hoặc cấu hình tham số:
  - Hỗ trợ tham số thời gian (`DetectTime`, `TranTime`) và khóa tổng hợp (kết hợp mốc thời gian và `ZoneId`, mã làn, vị trí).
  - Giá trị quét thành công của chu kỳ trước được lưu trữ vào bảng quản lý trạng thái quét (dưới dạng chuỗi định dạng chuẩn, ví dụ `yyyyMMddHHmmss_ZoneId`) để làm tham số đầu vào cho lần quét kế tiếp.
  - Tránh tạo các cấu hình lọc rời rạc phức tạp trên giao diện; tập trung vào câu lệnh SQL và tham số hóa an toàn.

### 1.5. Tái cấu trúc Tab trong Form Cấu hình Kết nối Đối tác (`connectionConfigForm`)
- **Thực trạng:** Màn hình cấu hình đối tác trước đây dồn tất cả thông tin (thông tin chung, thông tin giao thức mạng, thông tin lập lịch) vào một form cuộn dọc, gây rối mắt, khó quan sát và tiềm ẩn rủi ro sửa nhầm tham số kỹ thuật mạng khi chỉ muốn cập nhật thông tin mô tả.
- **Quyết định cấu trúc 2 Tab (Đồng bộ theo phong cách quản lý TMS):**
  - **Tab 1 — Thông tin chung:** Mã đối tác, Tên đối tác, Ghi chú mô tả (giữ như form cũ).
  - **Tab 2 — Cấu hình kết nối:** Loại kênh, Địa chỉ/Cổng, Endpoint (API), cấu hình theo kênh (xác thực, định dạng, thời gian chờ, thư mục…).
- *Lịch chạy/chu kỳ gửi cấu hình theo từng Đăng ký, không đặt ở form đối tác.*

### 1.6. Thu gọn và Tối ưu Scale Giao diện Ánh xạ Trường (`mappingForm`)
- **Vấn đề giao diện:** Màn hình bảng ánh xạ trường khi mới mở lên chưa có dữ liệu thì các ô và khung bị scale phóng to bất thường, tạo khoảng trống trải dài rất xa trên màn hình rồi lại bung thêm thanh cuộn/scale bên dưới, gây mất cân đối giao diện.
- **Quyết định giải quyết:**
  - Thiết lập giao diện ở trạng thái mặc định nhỏ gọn, vừa vặn khung nhìn.
  - Khi người dùng nạp dữ liệu (câu query hoặc danh sách trường) thì các ô bảng mới tự động giãn nở (bung dữ liệu) tương ứng, giữ giao diện gọn gàng, chuyên nghiệp.

### 1.7. Cơ chế Xử lý & Rút gọn Thông tin Lỗi Gói tin (Packet Error Handling)
- **Vấn đề:** Trong lịch sử truyền nhận gói tin, ở các bước xử lý (Bước 1, Bước 2), khi xảy ra lỗi hệ thống, chuỗi thông báo lỗi kèm stack trace/chi tiết kỹ thuật rất dài. Nếu hiển thị toàn bộ lên bảng giao diện sẽ làm vỡ layout dòng và bảng.
- **Quyết định kỹ thuật chốt:**
  - **Trên giao diện UI:** Chỉ cắt và hiển thị chuỗi thông tin lỗi rút gọn (vài chục đến tối đa vài trăm ký tự) đủ để nhận biết nguyên nhân chính.
  - **Trong hệ thống:** Toàn văn thông tin lỗi kỹ thuật chi tiết đã được ghi đầy đủ vào file log hệ thống (Application Log), đảm bảo không làm mất mát thông tin khi cần tra cứu chuyên sâu.

---

## 2. Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript)

### [00:00 – 04:00] Khởi động, Rà soát Hiện trạng UI Cấu hình SQL và Thử nghiệm API
- **[01:17] Dev:** Em bắt đầu nha. Cái phần UI á thì em có sửa lại như cũ rồi. Giờ em sẽ đi tiếp cái phần SQL nha.
- **[01:27] Tech Lead:** Ừ, cho nó nhỏ lại tí. Thấy được chưa?
- **[01:34] Dev:** Dạ rồi.
- **[01:44] Tech Lead:** Em buid về chưa vậy?
- **[01:47] Dev:** Chưa build anh, đang sửa code rồi.
- **[01:50] Tech Lead:** Thì cứ build đi, coi nó dính cái gì.
- **[01:55] Dev:** Thôi em nghĩ góc này nó nhanh hơn, cầm máy anh qua ngồi chung với em đi cho dễ. Qua đây nè.
- **[02:36] Dev:** Còn cái vụ mà giao task hai đứa hay gì để tụi em sắp xếp lại.
- **[02:50] Dev:** Cái này để bao nhiêu anh? 50 hay 75 thì được?
- **[03:06] Dev:** Rồi, ổn rồi đó.
- **[03:32] Dev:** Cái phần cấu hình SQL á thì em mới sửa lại cái UI.
- **[03:41] Tech Lead:** Này chưa gọi API trên staging đúng không em?
- **[03:53] Dev:** Chưa, cái này em đang gọi API... trên staging đâu có cần BE đâu, em gọi thẳng lên trên dev thôi.

### [04:00 – 08:00] Bố cục Màn hình Ánh xạ Trường & Trích xuất Alias từ câu Query
- **[04:01] Tech Lead:** Staging như dev đúng không?
- **[04:03] Dev:** Server dev rồi, gọi trên staging làm gì.
- **[07:07] Dev:** Cái phần mà hôm qua anh có review á thì em có sửa lại là em bỏ mấy cái cột rồi. Thì bây giờ mình sẽ thuần test luôn. Thì nó sửa trên đây xong mình lưu.
- **[07:18] Dev:** Thì trước đó nếu mình chưa có câu query truy vấn á, lúc mà nó cấu hình gói tin á thì nó sẽ cấu hình mấy cái danh sách trường này.
- **[07:31] Dev:** Phía server là mình đang hardcode query rồi xong mình sẽ map tương ứng lại những cái field trường này. Thì khi mà làm cấu hình động á, thì những cái alias này nè, nó sẽ tương đương với lại danh sách trường bên đây.
- **[07:51] Dev:** Thì khi mà một cái câu query nó có một cái câu query như vậy nè, thì nó sẽ đối chiếu là những cái trường nào...

### [08:00 – 12:00] Hiển thị Trường Chưa Ánh xạ & Bài toán Gửi Toàn bộ vs Nối đuôi
- **[08:00] Dev:** Đã map rồi á thì nó sẽ hiện ở đây, còn những cái nào mà có trường nhưng chưa nằm trong câu truy vấn thì nó sẽ nằm ở đây, kiểu nó hiển thị cho người dùng biết thôi. Và họ có thể tạo thêm cái trường tương ứng để map thêm ra.
- **[08:22] Dev:** Thì cái phần này trước tiên về mặt UI/UX đi, thì đây là cái mong muốn của mình rồi đúng không? Có thêm gì không anh?
- **[08:33] Dev:** Cho em hỏi thêm cái, cái phần mà trước đó mình có bàn về cái phần gửi toàn bộ và gửi nối đuôi á, thì em đang phân vân là cái này mình có cần phải tách... à không cần tách, kiểu nó viết trong query rồi đúng không?
- **[08:51] Tech Lead:** Viết thêm điều kiện thôi.
- **[08:55] Dev:** Thì cái điều kiện đó cũng sẽ viết trong đây thôi chứ không có UI gì nữa luôn hả anh?
- **[09:00] Tech Lead:** Ví dụ như rồi nó sẽ có cái phần ở dưới người ta chọn, là ví dụ như cái trường... cái giá trị mà mình so sánh á, nó mang ý nghĩa là cái gì.
- **[09:15] Dev:** Giống như trường so sánh đi, ví dụ cái LastTime đi, thì cái mang ý nghĩa ở đây là mình chỉ cần hiện tooltip vậy thôi hay là cho người ta thao tác luôn?
- **[09:24] Tech Lead:** Cho thao tác chứ.
- **[09:30] Dev:** Thì hiện tại là chỉ có những cái biến này thôi để setup thôi, anh coi có thêm cái gì không?
- **[09:39] Tech Lead:** Nhưng mà em xem coi đặt cái câu điều kiện này ở đây có hợp lý không, tại vì anh thấy cái này đặt ở mapping thì ổn hơn á. Tại nhiều khi giữa đối tác người ta muốn gói này gửi toàn bộ, đối tác kia lại muốn gói này gửi nối đuôi, hiểu không?
- **[09:53] Tech Lead:** Ví dụ có 2 đối tác cùng trỏ về 1 gói, một ông muốn gửi nối đuôi, một ông muốn gửi toàn bộ thì làm sao cấu hình ở đây được?
- **[10:00] Dev:** Thì hôm bữa em nói này là tạo một cái thằng chung, sau đó là mỗi đối tác sẽ chỉnh sửa lại riêng cho đối tác đó. Lúc đó mới có phần quản lý chung và quản lý riêng.
- **[10:16] Dev:** Chứ bây giờ cái gói gốc mà không có để dùng rồi làm sao mấy thằng kia dùng được? Chẳng lẽ mỗi lần tạo đối tác mới là em phải đi viết lại câu mới từ đầu?
- **[10:29] Dev:** Nên em mới nói là tạo một cái thằng chung tổng quát trước, sau đó đối tác nào chọn gói tin đó nếu cần sửa cái gì đó thì mới sửa lại theo cái thằng mới, ví dụ như thằng mới yêu cầu thêm trường A, trường B, trường C gì đó thì sửa riêng lại. Chứ chẳng lẽ tạo thằng mới xong rồi trống trơn hết, rồi người ta phải ngồi viết lại từ đầu hả?
- **[10:53] Dev:** Anh coi thử có phải không, tại vì hôm bữa chỗ này đã nói một lần rồi mà. Lúc mà mình tạo một cái gói mới, ví dụ gói 101 đi, thì hiện tại là em đang có một câu query kiểu câu mẫu sẵn cho nó.
- **[11:18] Tech Lead:** Giờ cái này ví dụ thằng này là thằng quản lý con mẫu nè, thì ra cái câu mẫu đó cũng sẽ đi tới là ví dụ những cái trường nào cần thiết, có nghĩa là những cái trường chuẩn á, hôm bữa mình làm là những trường chuẩn hỗ trợ.
- **[11:34] Tech Lead:** Cái thứ hai nữa là cái điều kiện hôm bữa mình đã chốt, cái phần nào gửi tuần tự, cái nào gửi hết rồi đúng không? Thì mình đã có cái trường đó mình quy định rồi, cái đó mình sẽ làm được. Có nghĩa điều kiện theo cái trường đó.
- **[11:50] Tech Lead:** Rồi tương ứng ví dụ như khi cấu hình cho một cái thằng đối tác A đi, đối tác A đó nó cần dữ liệu chỉ thuộc cái mã section A gạch gì đó, thì bắt đầu mình mới viết thêm cho nó trong cái điều kiện query đó.

### [12:00 – 16:00] Tùy biến Điều kiện theo Đối tác & Thảo luận Cấu trúc Tab Gói tin
- **[12:05] Tech Lead:** Mình thêm cái section là `AND SectionId = 'SectionA%'` đúng không? Đó thì lúc đó mới custom thêm cho thằng A đó, là ở một cái phần khác nữa. Hiện tại là mình đang cần một cái thằng quản lý chung.
- **[12:33] Dev:** Quản lý chung là thằng này nè.
- **[12:36] Tech Lead:** Quản lý chung để nó có cái câu điều kiện chuẩn. Chứ bây giờ tạo thằng A không có câu quản lý chung thì chẳng lẽ thằng A phải đi copy lại? Đi mở thằng này ra copy lại à, hay mở thằng khác ra copy lại?
- **[12:48] Dev:** Vậy là cho một cái chức năng riêng nữa?
- **[12:57] Tech Lead:** Cũng không hẳn. Ví dụ như thằng này đi, cũng là thằng này luôn, nhưng mà ở đây sẽ chia thành nhiều tab hay gì đó. Ví dụ như tab đầu là mẫu chung nè, tab 2 là của thằng A đang có, tab B đang có, tab C đang có. Bao nhiêu đối tác là tạo bấy nhiêu tab.
- **[13:15] Dev:** Ý anh nói cái mẫu chung này có nghĩa là cái gói này nó sẽ tạo mẫu cho nó luôn đúng không?
- **[13:21] Tech Lead:** Cái gói đó cơ bản đang là mẫu rồi đó.
- **[13:23] Dev:** Ừ thì cái đó em đang có nè. Giống như khi mà có đối tác A đi nó sẽ tạo ra thêm một tab nữa cho đối tác A. Nó sẽ có câu truy vấn dành riêng cho đối tác A. Tuy nhiên cái chung của chức năng này nó sẽ chia làm nhiều đối tác.
- **[13:37] Tech Lead:** Rồi hiểu rồi.
- **[13:39] Tech Lead:** Bên kia sẽ fix cố định là chỉ 11 gói tin thôi, không phải tạo ra nhiều.
- **[13:45] Dev:** Đúng rồi, cái case mà 11 gói thì em đang gom lại.
- **[13:51] Tech Lead:** Đừng có tạo theo kiểu 101 của A, 101 của B nữa, sau này nó sẽ lên bốn mươi mấy gói không tìm thấy gói nào đâu. Mình sẽ gom lại bên kia là 11 gói, rồi ở đây mình sẽ chia nhỏ nó ra.
- **[14:03] Tech Lead:** Ví dụ như chữ số xài tất cả đều nằm hàng chung hết thì có đúng 8 hàng chung. Rồi trường hợp dùng có thể đối tác A nó hiện ra cho đối tác A. Đấy kiểu vậy.
- **[14:19] Dev:** Còn không thì làm theo kiểu chia đều theo cục tổng đó, giống như ở bên thằng user đó. Nó có nhiều cách để chia.
- **[14:51] Dev:** Đó ví dụ như bên trái đi là nó sẽ chia chung, đối tác A, đối tác B, đối tác C gì đó. Bấm thằng nào thì tương ứng ví dụ bấm vô thằng đối tác A đi thì nó sẽ hiện ra gói 101, 102. Đối tác B cần customize lại gói 103, 104 thì bấm vào đó. Nhưng nếu làm kiểu này thì sẽ hơi khó quản lý tại vì ví dụ mấy thằng dùng chung nó sẽ không thấy, bắt buộc phải vào tab dùng chung. Tích vô gói nào thì nó hiển thị gói đó.
- **[15:38] Tech Lead:** Gợi ý 2 cách đó xem thằng kia nó hỗ trợ làm sao, coi công cụ nào dễ thì áp dụng đi. Khó quá thì cứ xuất dễ ra.

### [16:00 – 20:00] Kịch bản Khi Sửa Câu Mẫu — Bản Custom Sẽ Xử lý Thế nào?
- **[16:01] Dev:** Mấy cái phần ánh xạ bộ này nọ là em chưa... vẫn như cũ, không có gì hết, chỉ có sửa phần câu query thôi.
- **[16:09] Tech Lead:** Nếu mà như vậy, nếu mà theo anh Sơn nói thì dữ liệu ánh xạ là nó phải lấy từ đối tác, chứ không lấy từ đây nữa.
- **[16:20] Dev:** Rồi hả nó mà không hiệu chỉnh cái gì thì nó là thằng chính chứ gì nữa.
- **[16:29] Tech Lead:** Còn nếu mà vậy á, mỗi lần thằng chung đúng không, mà mỗi lần tụi em load thằng chung ra thằng mới á, thì gặp tình huống bây giờ sửa... Sửa thằng chung thì mấy thằng kia sao?
- **[16:48] Dev:** Sửa thằng chung với mấy thằng kia thì sao á?
- **[16:50] Tech Lead:** Ví dụ như thằng chung nó bị sai một cái logic gì đó đi, hoặc update logic đi, mình sửa lại câu truy vấn mẫu đi, thì mấy thằng kia giải quyết thế nào? Chẳng lẽ phải copy tay lại hết, giờ 10 thằng cùng xài, 10 thằng cùng đụng ra thì sao? Có cách nào sửa nhanh không?
- **[17:19] Dev:** Hiện tại cái câu truy vấn mẫu ở đây đang đi theo gói tin.
- **[17:25] Tech Lead:** Đó là tình huống giờ mỗi thằng load một mảng riêng, giờ sửa lại cái gốc mẫu thì mấy thằng kia sửa như thế nào? Bắt buộc phải sửa đúng không?
- **[17:35] Dev:** Mấy thằng kia thì nó đang... từ từ nha. Câu query mẫu đang đi theo gói tin, mà đối tác á thì phải chọn gói tin. Vậy suy ra nếu mà chỉ cần sửa mã SQL trong gói tin là mấy thằng kia được hưởng hết!
- **[17:48] Tech Lead:** Đang nói tới cái... Không anh, nếu mà anh nói trường hợp đó là câu query mẫu đi theo gói tin đúng không, gói tin đó là gói tin chuẩn. Bây giờ trường hợp khác là đối tác sẽ có một thằng nó dùng gói tin chuẩn, còn một thằng nó dùng cái gói custom. Thì khi mà thay đổi gói tin chuẩn thì những cái gói custom của gói tin chuẩn đó xử lý như thế nào?
- **[18:09] Dev:** Cái mã SQL custom á hả?
- **[18:12] Tech Lead:** Ừ, ví dụ mấy cái gửi nối đuôi này nọ nè, thằng này nó cần cái này thằng kia nó không cần cái kia, hoặc là gửi từ ngày nào tới ngày nào á.
- **[18:19] Dev:** Rồi từ từ, chia 2 trường hợp. Lúc mình tạo gói mới, khi nó chưa có câu SQL của nó thì nó đang dùng là template mẫu là chung nè. Trường hợp ví dụ thằng này nó có custom đi, thì nhận lệnh custom là lấy cái template ha. Thì khi mà bấm Lưu á thì lúc này là cái câu này nó đã độc lập riêng rồi!
- **[18:46] Tech Lead:** Đúng! Thì bây giờ ví dụ thay đổi một thằng chuẩn chung thì những thằng này...
- **[18:50] Dev:** Thằng này nó sẽ dùng cái custom.
- **[18:52] Tech Lead:** Những thằng custom nó sẽ thay đổi như thế nào so với những thằng chuẩn chung? Ví dụ bây giờ anh vô gói tin số 3 anh thay đổi đúng không? Thì bây giờ những thằng custom của gói số 3 đó nó sẽ thay đổi như thế nào?
- **[19:15] Dev:** Giờ lấy ví dụ nè, này đang gói nhiêu, 101 đúng không?
- **[19:21] Tech Lead:** Đó bây giờ ví dụ anh có gói 101 đúng không? Gói 101 phẩy anh build từ gói 101 lên. Bây giờ anh có đối tác A đang trỏ vô gói 101 đúng không? Rồi bây giờ em cũng trỏ vô gói 101, nhưng mà em muốn thay đổi cách lấy dữ liệu của nó thì em đẻ ra là gói 101 phẩy. Thì gói 101 phẩy đó build từ gói 101.
- **[19:49] Tech Lead:** Thì bây giờ anh cũng sẽ làm tương tự như em nhưng anh sẽ thay đổi theo một cách khác để anh có gói 101 phẩy phẩy. Vậy thì bây giờ gói 101 đó thay đổi thì gói của em và gói của anh action nó sẽ như thế nào?

### [20:00 – 24:00] Đề xuất Cơ chế So sánh Diff Giữa Bản Gốc và Bản Custom
- **[20:02] Dev:** Nếu mà em thay đổi đúng không?
- **[20:04] Tech Lead:** Nếu mà cái gói 101 gốc thay đổi á, thì gói của em với anh thay đổi như thế nào?
- **[20:09] Dev:** Một thằng đang dùng cái mẫu, còn của anh thì đang dùng câu custom mới. Hiện tại là như vậy.
- **[20:17] Tech Lead:** Thì đó, anh đặt trường hợp là ví dụ giờ thay đổi đó thì giờ sẽ đi copy tay lại hết hả hay là sẽ như thế nào đó?
- **[20:21] Dev:** Thí dụ hai em với anh đều là tạo một gói 101 đúng không?
- **[20:26] Tech Lead:** Hai gói khác nhau nhưng cùng build từ 101.
- **[20:30] Dev:** Ừ thì hai gói khác nhau 101, anh cũng 101 luôn. Anh có sửa thì anh sẽ dùng cái sửa của anh, còn hai người sẽ dùng cái mẫu nếu mà không sửa gì hết. Nếu mà không sửa câu query đây nè.
- **[20:46] Tech Lead:** Không sửa câu query ở đây hả?
- **[20:47] Dev:** Thì không sửa ở đây, chưa từng sửa thì nó sẽ đang dùng cái mẫu.
- **[20:58] Tech Lead:** Bây giờ nha, bây giờ anh bỏ hết tất cả những cái toàn bộ với nối đuôi của cái gói này đi nè. Anh gạt nó qua một bên đi. Bây giờ ở trong cái gói 101 anh trỏ vô gói 101 đi. Ở đây là nó chỉ là một câu query bình thường thôi, không có nối đuôi không có toàn bộ gì hết. Nhưng mà ví dụ em đi chẳng hạn, em muốn cái 101 có nối đuôi, thì tại bây giờ là mình sẽ build ra một câu 101 riêng và sẽ truyền cái nối đuôi vô là kiểu custom á. Anh cũng có nối đuôi theo kiểu khác nữa là custom kiểu khác nữa. Đúng không?
- **[21:33] Tech Lead:** Rồi, vậy bây giờ ví dụ như cái 101 này của anh thay đổi đi chẳng hạn, thì hai cái này của em với anh có thay đổi không?
- **[21:40] Dev:** Không! Cái này độc lập.
- **[21:42] Tech Lead:** Bây giờ rồi, ví dụ thêm một case nữa đó là khi mà nó thay đổi nó không thay đổi alias key mà nó thay đổi thẳng luôn cái field DB á, là cái `Z.toKmNumber` á. Ừ, nó không thay đổi alias key nha, mà nó thay đổi `Z.toKmNumber` á thì của em với của anh action như thế nào?
- **[22:03] Dev:** Thay đổi trong DB đúng không?
- **[22:05] Tech Lead:** Ừ, ví dụ giờ đổi trong DB thì giờ lên đây thì sao? Thì đặt trường hợp là phải đi copy tay lại hết từng gói hay là như thế nào đó?
- **[22:13] Dev:** Tại vì dữ liệu đang đi tuần tự từ DB ra, nếu mà em đổi DB thì chắc trên đây cũng đổi luôn, mà cái đó để coi lại.
- **[22:23] Tech Lead:** Thì cái đó để cho nghiên cứu hướng giải quyết.
- **[22:25] Dev:** Chịu khó làm cái hướng nào cho người ta dễ hình dung, ví dụ như mấy cái mà tụi em hay cãi nhau đó... Giờ làm sao? Chèn cái nào? Bắt chước trang nào?
- **[22:38] Tech Lead:** Dùng Git á! Bên trái bên phải, phần nào khác nhau. Đúng không?
- **[22:51] Dev:** Ừ, ví dụ như ờ có cái nút Compare đi! Compare bản gốc với lại bản alias, ví dụ bản gốc với các bản đối tác. Mở lên nó hiện ra danh sách đối tác theo bản gốc, chỗ nào cần replace, chỗ nào để nguyên! Làm y chang như Git chụp lại anh nói chứ gì đâu!
- **[23:22] Tech Lead:** Đúng rồi!
- **[23:24] Dev:** Nói nó... chỉ nó làm kiểu... làm theo cái Compare hợp lý hơn! Có cách nào tối ưu không?
- **[23:38] Dev:** Mình làm bằng tay thì khó nhưng mà nó làm thì nó sẽ dễ, tập làm... viết code tới.

### [24:00 – 28:00] Định hình Hướng Xử lý Quản lý Gói tin & Phân loại Danh mục
- **[24:01] Tech Lead:** Đi sai là ngay ở đây rồi đó, kiểm tra kỹ lại tí. Mấy đứa kia hôm qua có nhưng mấy đứa đó đâu có xài gì đâu. Có hai thằng tự nhiên sai tè le hết luôn. Mấy cái kỹ kỹ mình phải coi kỹ kỹ lại tí.
- **[24:22] Dev:** Nó có 2-3 hướng đó, xử lý thử. Thứ nhất là cái hướng liên quan đến quản lý cái gói tin nè: Một là quản lý theo cái hướng là đối tác hình chuẩn là fix cố định luôn, 11 cái. Qua bên này mình sẽ quản lý riêng theo đối tác. Nhưng mà đối tác như đó thì gặp tình huống nữa là tìm kiếm theo đối tác nó có dễ tìm kiếm hay không. Ví dụ bây giờ tìm đối tác, giờ muốn biết được đối tác A đang xài bao nhiêu gói tin thì tìm như thế nào, có tiện lợi hay không?
- **[25:01] Dev:** Cách thứ hai là quản lý theo dạng đối tác: Đối tác click vào sẽ ra cái gói tin. Nhưng mà gói tin đó thì làm sao biết được nó là gói tin mẫu hay là gói tin chỉnh sửa? Thì cũng phải đặt tình huống như đó, chứ không phải là làm không có tình huống trỏng.
- **[25:21] Dev:** Nói chung mục tiêu là quản lý sao cho nó dễ dàng, phân biệt được là gói tin nào là gói tin chung và gói tin nào là gói tin dành riêng cho đối tác. Chứ mình không phân biệt được trong này là sửa trùm lum hết á, không biết đang sửa cho đối tác hay đang sửa cho thằng chung.
- **[25:48] Dev:** Rồi qua bên này khi mà chỉnh sửa cho ông chung hay chỉnh sửa cho đối tác gì đó thì nó sẽ có tình huống là thay đổi tổng thể. Phải không? Chứ không thể nào mà cái gói tin nó giữ nguyên nhiều tình huống mà nó xuất hiện theo điều kiện A, điều kiện B, điều kiện C lắm. Ví dụ như trong quá trình mình đang chạy đi, mình thấy lấy dữ liệu kiểu đó không tối ưu quá, mình sửa lại một cái, cái cách viết khác. Có gặp tình huống đó rồi, đúng không nè?
- **[26:25] Dev:** Ví dụ chạy kiểu này nó không tối ưu, xong rồi mình đặt thêm cái index hay là partition gì đó là chắc chắn trong này mình phải sửa lại một tí rồi. Mấy lần sửa rồi. Đó thì gặp tình huống phải sửa như vậy bây giờ muốn sửa hàng loạt thì sửa như thế nào? Tổng thể làm sao?
- **[26:48] Dev:** Nó ít thì copy tay thì ok, chứ nó mà nhiều, mười mấy cái đối tác một lúc thì sao mà mình sửa nổi, đúng không? Mấy đối tác mà mỗi đối tác 10 gói tin đi, nhân lên thấy bao nhiêu rồi! Một đối tác mấy gói tin đó mà nhân lên thấy bao nhiêu rồi, đi copy tay từng cái thì sao.

### [28:00 – 32:00] Bóc tách Mệnh đề `SELECT` & Phân định Cột SQL vs Danh sách Trường Chuẩn
- **[28:00] Tech Lead:** Mục tiêu là những cái trường, cái trường phía sau alias á, là cái gì thì mới đối chiếu với lại cái mình lưu. Là phải tách, trích xuất ra được cái thằng phía sau alias á, cho nó ra một cái danh sách. Thì danh sách đó có thể là nó bằng, không nói, nhưng mà trường hợp nó lệch, nó lệch thì nó lệch sao? Ít hơn, hay nhiều hơn? Ít hơn thì xử lý như thế nào, nhiều hơn xử lý như thế nào?
- **[28:34] Tech Lead:** Rồi cái thứ hai nữa là các điều kiện. Điều kiện đúng không? Điều kiện thứ nhất là mấy cái điều kiện mà nó cứng thì không nói rồi, điều kiện cứng thì nó sẽ nằm trong cơ sở dữ liệu rồi không nói. Nhưng mà mấy cái điều kiện mình sẽ bổ sung thêm để phục vụ mục đích của mình, ví dụ như là cái phần liên quan đến gửi theo điều kiện đi. Gửi nối đuôi hay là gửi dữ liệu trước đó, gửi từ thời điểm trước đó thì tìm cái trường hợp đó.
- **[29:05] Tech Lead:** Ví dụ như là cái trường, cái trường mình truyền vào đó, mình sẽ có cái rule gì đó bên nào. Đúng không? Thì nó sẽ hiện hết tất cả các trường đó ra giống y chang như là cái danh sách trường đó. Và mỗi trường đó mình sẽ gán cho nó một cái điều kiện gì đó, nếu có.
- **[29:21] Tech Lead:** Ví dụ như lấy trường này thì nó là cái giá trị gì đó, còn cái trường kia giá trị nó là cái gì đó. Để khi mà bên trong code nó xử lý cho nó đúng.
- **[29:40] Tech Lead:** Và mấy cái thông số mấy cấu hình đó thì lưu nó lại trong một cái bảng nào đó, thường là sẽ lưu trong cái bảng nào đó. Ví dụ như là cái bảng liên quan đến story gì đó. Chưa nghĩ ra tên thế nào. Mục tiêu là sẽ lưu lại cái giá trị mình xử lý được ở lần trước đó để mình lưu vào, để lần sau nó sẽ gọi, lọc cái trường đó ra để nó lấy.
- **[30:05] Tech Lead:** Đó kiểu dạng ví dụ như cái trường đó là, ví dụ cái điều kiện này đi, lớn hơn cái cột thời gian đi, `DetectTime` đi, `DetectTime` hay `TranTime` gì đó. Đúng không? Mà được chuyển đổi nó lại dưới dạng đọc được, hay là dưới dạng key của mình đi, ví dụ như là `yyyyMMddHHmmss` viết liền, đúng không? Rồi kết hợp với lại cái `ZoneId` đi.
- **[30:42] Tech Lead:** Thì phải lưu được cái giá trị đó vào trong một cái chỗ nào đó, để lần sau nó bắt lên nó check vào. Tiêu chí là như vậy.
- **[31:18] Dev:** Có nhiều cái để làm...
- **[31:33] Dev:** Cái cấu hình này theo nội bộ trao đổi thì có bổ sung thêm mấy cái loại kết nối. Thì hiện tại hướng xử lý á thì cứ cho nó cấu hình thôi, rồi cái phần service á...
- **[31:43] Tech Lead:** Cái cấu hình kết nối hôm bữa chia cái tab nghe sai sai đúng không? Bữa đã nói là chia cái tab nó giống như cái bảng của mình. Thông tin ở một cái tab riêng, mấy cái thông tin chung ở một cái tab, để tránh tình huống là nó xẹp...

### [32:00 – 36:00] Tái cấu trúc Tab Giao diện Kết nối Đối tác (`connectionConfigForm`)
- **[32:00] Tech Lead:** Mấy cái thông tin kết nối nó hiển thị luôn ngay cái tab đầu. Hôm bữa chỗ này đã nói một lần, nhắc cái chỗ này rồi á, chứ không phải không nhắc đâu.
- **[32:16] Tech Lead:** Chia nó ra làm hai cái tab, một cái tab là thông tin chung là mấy vụ như cái mã, mấy cái tên đối tác rồi ghi chú gì đó. Cái tab thứ hai mới là cái tab kết nối. Để tình huống người ta vào người ta đổi cái tên thôi, người ta không ảnh hưởng. Còn ví dụ như người ta đổi liên quan thông tin kết nối gì đó người ta qua tab kết nối người ta lèo vào trỏng.
- **[32:37] Tech Lead:** Nhưng mà người ta bấm người ta thấy người ta mở lên người ta bấm cái gì đó rồi sao? Phải chia ra các tab thông tin để cho nó dễ quản lý. Thứ nhất là nó dễ quản lý, nó đi theo cụm rồi, theo tab rồi. Ví dụ như sửa kết nối gì đó thì vào đúng cái tab kết nối là được.
- **[32:53] Tech Lead:** Thứ hai là đỡ rối thông tin cho người ta nhìn. Chứ mở lên đập vô mắt đóng một cục như vậy sao người ta biết đường xử lý thế nào. Đúng không?
- **[33:03] Tech Lead:** Ví dụ như thông tin đầu cái tab đầu là thông tin về chung đi, đặt tên đặt mã, đúng không? Còn một cái tab nữa là tab liên quan đến kết nối, đúng không? Mấy thông tin thông số kết nối gì đó. Với một cái tab nữa là cái tab liên quan đến thiết lập: thời gian chạy, có khởi tạo lại có gì có chạy lại hay không, ví dụ có retry mấy lần gì đó... Chứ gom vô một đống thông tin đầu nó vừa dài nó vừa rối.

> 📌 **Đính chính 22/10/2026:** Đoạn "một cái tab nữa… thời gian chạy… retry" là chép sai từ bản ghi âm tự động. Người tham dự xác nhận cuộc họp chốt **2 tab** như [32:16]; lịch chạy nằm ở Đăng ký.
- **[33:41] Dev:** Như là cái kênh kết nối này mình sẽ tạo trong cái tab hả?
- **[33:44] Tech Lead:** Không, tạo cái tab trong đây luôn mà!
- **[33:46] Dev:** Tab trong đây là ở đâu? Tab pane á hả?
- **[33:50] Tech Lead:** Mở... Mở lại cái... Mở lại cái chức năng giùm anh cái.
- **[33:56] Dev:** Chức năng nào?
- **[33:57] Tech Lead:** Chức năng nào cũng được. TMS quản lý á.
- **[34:11] Dev:** Không phải cái này đâu.
- **[34:13] Dev:** Vào trong User cũng được kìa.
- **[34:17] Tech Lead:** Hệ thống... hệ thống... Quản lý người dùng. Đó, tạo mới.
- **[34:31] Tech Lead:** Đó! Chia cái tab ra! Cô đọng những cái thông tin nào liên quan để ở một tab thôi.
- **[34:43] Dev:** Ok, rồi tiếp...

### [36:00 – 39:29] Tổng kết Phân hệ ShareData & Chuẩn bị Kế hoạch Tiếp theo
- **[36:57] Dev:** Chia sẻ dữ liệu có hai cái đó mới á. Còn ba mấy thằng khác đâu có ảnh hưởng kết nối là hôm qua em lọc ối tè le hết.
- **[37:05] Tech Lead:** Đưa đây để em đổi nhánh em xử lý kia.
- **[37:09] Dev:** Rồi xuống hả? Đi xài trước... Nhớ là dò kỹ lại nha. Đi tí xuống.
- **[37:24] Tech Lead:** Giờ xuống hả? Từ từ coi...
- **[37:30] Tech Lead:** (Kết thúc cuộc họp trao đổi kỹ thuật phân hệ ShareData).

---

## 3. Kịch bản đối thoại tệp ghi âm bổ sung: duong_so_8_2.m4a (Phần Rà soát ShareData)

### [00:00 – 04:00] Rà soát Scale Giao diện Ánh xạ
- **[00:10] Tech Lead:** Được chưa?
- **[00:15] Dev:** Cái gì? Chưa xong cái chia sẻ dữ liệu mà.
- **[00:18] Tech Lead:** À được... chia sẻ dữ liệu...
- **[00:20] Dev:** Mở thêm cái năng lượng nữa rồi. Còn 3 cái...
- **[00:24] Tech Lead:** Rồi, có cái khi nãy thay đổi rồi.
- **[00:28] Dev:** Cái nào anh? Không cái... không phải đâu. Cái nào anh?
- **[00:30] Tech Lead:** Chia sẻ dữ liệu.
- **[00:31] Dev:** Chỗ nào vậy?
- **[00:33] Tech Lead:** Cái bảng hôm qua đó.
- **[00:35] Dev:** Ánh xạ đúng không? Cái đó đâu lỗi, em làm mới sợ lỗi.
- **[00:39] Tech Lead:** Giao diện đó!
- **[00:41] Dev:** Cái này có trước nó tự scale chứ.
- **[00:43] Tech Lead:** Thì phải chỉnh lại chứ! Mới vô chưa có gì hết scale chà bá, nhìn... nhìn ghê quá trời ghê à.
- **[00:51] Dev:** Đặt lại cái này hả? Đặt lại cái này để kéo qua kéo vô...
- **[00:57] Tech Lead:** Chỉnh lại cho nó phù hợp chứ.
- **[00:58] Dev:** Là sao? Là như vầy hay là khác nữa?
- **[01:01] Tech Lead:** Cái tab đầu này nè, tab đầu này nè, mới bật lên dư một khoảng rất là xa luôn. Đâu cần thiết cái tab này. Thì cái tab sau scale nó lại một tí. Cho nó phù hợp chứ để vậy nó trống một khoảng rất là xa rồi, xong rồi nó xuất hiện cái thằng... cái thằng scale ở dưới nữa kìa.
- **[01:28] Dev:** Ở đâu vậy?
- **[01:34] Tech Lead:** Ù...
- **[01:38] Dev:** Thôi để sửa lại, 5 phút.
- **[01:42] Tech Lead:** Chỉnh lại cho nó thu dữ liệu lại chứ. Chứ chưa có gì hết bung chà bá ra. Ví dụ như vô thì nó nhỏ nhỏ thôi, ví dụ nó có cái gì bự thì mới bung ra.
- **[01:55] Dev:** Là sao? Ý có nghĩa là hiện tại cái này em đang làm cố định nè, bây giờ ví dụ mình fill dữ liệu vô làm cố định lại lúc này nó mới bự ra đúng không?
- **[02:01] Tech Lead:** Ừ. Mới vô để chà bá không có fill cái gì hết nhìn nó kỳ. Rồi.
- **[02:10] Dev:** Còn ShareData dính có cái đó hả?
- **[02:12] Tech Lead:** Chủ yếu hôm qua chỉnh video wall nhiều, hôm qua chỉnh rồi. Còn mấy cái ở dưới nữa, chà lại hết mấy chức năng ở dưới nha.
- **[02:18] Dev:** Đang dò lại... đang đang dò lại đó, đang gần xong...
- **[02:22] Tech Lead:** Cứ để đâu có ai vào đâu. Mấy cái nào mà nó rối rắm quá mà nó chưa có ổn thì sửa lại giùm anh. Khi nào mấy dữ liệu thêm... mấy dữ liệu đã học là đúng rồi lên luôn. Mấy cái nào mình thấy ok rồi thì mình để lại dữ liệu cho nó cấp dữ liệu cho nó chuẩn.
- **[03:04] Tech Lead:** Chứ clip chưa có đó, hiện tại người dùng chưa có test cái này. Dữ liệu này chưa test. Giờ coi lại thấy chưa có một admin nào nghĩa là chưa có test.
- **[03:16] Dev:** Rồi ok anh.
- **[03:17] Dev:** Này mới code cho nó có thôi chứ chưa có test, chưa có chạy gói tin nữa.

> 📌 *[03:28]–[07:46] (đã gỡ 22/10/2026): trao đổi về bản ghi sự cố "va quẹt xe tải" hiển thị sai trên TMS — nguyên nhân là script dữ liệu giả lập `sharedata-du-lieu-gia-lap_local.sql` (Khối 3), đã sửa.*

### [08:00 – 12:00] Rà soát Tab Chia sẻ Dữ liệu, Kiểm tra Gói Thất bại & Bộ lọc Ngày tháng
- **[08:16] Tech Lead:** Rồi tiếp, mấy chức năng ở dưới chỗ chia sẻ dữ liệu, còn mấy cái tab ở dưới nữa kìa. Rà lại cho nó đầy đủ hết đi.
- **[08:26] Dev:** Chỗ cảnh báo lỗi này khả năng sẽ bỏ đi anh Sơn?
- **[08:30] Tech Lead:** Đó mấy cái ngày tháng năm hôm bữa cũng kêu chỉnh lại, hôm nay cũng chưa chuyển nữa kìa.
- **[08:35] Dev:** Xíu nó chuyển.
- **[08:37] Tech Lead:** Xíu cái gì mà hai ba xíu rồi đó!
- **[08:39] Dev:** Xem... lát nữa mình chia ra.
- **[08:42] Tech Lead:** Đã nói là tập trung làm. Cái nào sửa được thì sửa. Bữa đó mấy cái chỗ này là đã nói qua rồi chứ mình không nói đâu. Lại treo hoài đến cuối tháng luôn rồi đó. Mở cái lịch sử, bấm vào cái lịch sử coi. Cái nội dung coi đúng chưa?
- **[09:50] Dev:** Lên... Tây Ninh rồi... rồi sao?
- **[10:11] Tech Lead:** Rồi, coi thử mấy gói thất bại chưa?
- **[10:15] Dev:** Kiếm mấy cái lịch sử cũ á, nếu có mấy cái thất bại... Đọc kết quả này nè.
- **[10:25] Tech Lead:** Nghĩ chứ... này chưa test được hả?
- **[10:28] Dev:** Coi ngày tháng kìa, filter coi chừng không đúng. Đẩy ngày dài dài ra, đẩy luôn cho đến cuối tháng 9 luôn.
- **[10:37] Tech Lead:** Đó cái thời gian nhớ sửa nó lại. Bữa đã nói hôm qua sửa lại cái đó rồi mà.

### [12:00 – 16:00] Xử lý Chuỗi Thông tin Lỗi Dài trên UI & Quyết định Ghi Chi tiết vào File Log
- **[12:01] Dev:** Cái này có sửa thiết bị không anh? Hay giờ gì không? Để đây nè.
- **[12:04] Tech Lead:** Lỗi đó sao sửa thiết bị được. Cứ cắt vừa vừa thôi, nhưng mà cố... cẩn thận cái cách cắt cái chỗ... chỗ truy vấn dữ liệu á nha. Nhiều tình huống nó sẽ hiện cái thông tin bổ trợ lên á thì mình sẽ cắt bỏ bớt nó đi nếu có.
- **[12:36] Tech Lead:** Chứ cái gói... thất bại sao có? Sao không đổ nét bóng nè?
- **[13:08] Dev:** Mấy... mấy cái kiểu mà như Bước 2 chưa xử lý như vậy có ẩn luôn không hay là cứ treo để vậy là chưa?
- **[13:17] Tech Lead:** Nếu đúng thì nó phải là màu đỏ chứ. Chứ nếu mà treo á thì... nó là bị treo. Một là để cái chỗ đó màu đỏ hover đề cái thanh pop-up nổi xuống là chèn cái gì đó.
- **[13:35] Dev:** Vẫn để số 0?
- **[13:37] Tech Lead:** Đúng rồi, để số 0 màu đỏ luôn. Chứ nếu vậy thì kiểu nhìn nó bị cái gì đó.
- **[13:56] Dev:** Mà nếu ví dụ dữ liệu thông số nó dài quá thì sao? Nó có ảnh hưởng gì không? Cái Bước 1, Bước 2 á nó dài á. Nó có trường hợp nào nó dài quá không?
- **[14:06] Tech Lead:** Danh sách nhiều quá đúng không? Query ra nhiều quá.
- **[14:10] Dev:** Thông số á... thông số ghi á, nó có đoạn quá dài không? Ví dụ như cái mã lỗi nè. Cái mã lỗi này có cắt không?
- **[14:42] Tech Lead:** Ý em nói cái chỗ xuất sắc này nhiều quá hả? Có cắt bớt không hay là... hay là nó bị lỗi giao diện hết? Nhìn vô giao diện á, nhiều khi con số lỗi nó sẽ dài lắm á. Cũng phải xem lại thử.
- **[14:56] Dev:** Thì nếu mà cắt bớt thì...
- **[14:58] Tech Lead:** Ví dụ mình cắt chỉ hiển thị dạng như vài chục hay một vài trăm ký tự là được rồi. Chung mình canh nó vừa đủ á.
- **[15:07] Tech Lead:** Do cơ bản là cái lỗi chi tiết như thế nào là nó ghi vô log rồi! Nó ghi trong file log rồi nên cũng không sợ mất. Cái chỗ này chủ yếu là hiển thị cho người ta hình dung thôi.
- **[15:20] Dev:** Thì ý anh nói cái case này xử lý UI thôi chứ gì?
- **[15:23] Tech Lead:** Đúng rồi, xử lý UI thôi. Ví dụ như nó báo lỗi gì đó thì cho người ta nhìn... Chứ để cái lỗi dài ngoằng sao được.
- **[15:36] Dev:** Rồi rồi ok.

### [16:00 – 19:16] Giao thoa Chuyển tiếp sang Phân hệ VideoWall (Tiền tố Module 'vw_' & Cấu hình Dải IP)
- **[16:42] Tech Lead:** Giờ anh qua VideoWall với em đây. Rồi xong rồi hai anh em sẽ chia lại. Em còn... em còn block đúng không? Xử lý hết đi.
- **[16:56] Dev:** Cái gì sửa lại dịch thuật luôn, đưa vô cái folder đúng phân hệ.
- **[17:08] Dev:** Ủa anh Sơn share... anh Sơn share rồi là anh sửa rồi đúng không hay là tụi em phải sửa lại? Cái chỗ dịch thuật á.
- **[17:15] Tech Lead:** Chưa chia folder là anh sửa rồi... sửa rồi nhưng mà nội dung bên trong nội dung check lại, sửa lại. Nội dung nó sai, mấy cái chỗ nó bị sai đó, mấy cái khóa không có quy định nè, còn mấy cái thiết lập. Thấy nãy có mấy cái gì ấy, tống hết vô một cái ghi là VideoWall hết. Cái mã VideoWall cái tiền tố đặt bị đồng nhất một cái. Đừng có cái thì `VideoWall` viết nguyên cái dài, cái thì viết `vw`. Nó không có map với nhau!
- **[17:53] Tech Lead:** Ví dụ một số cái thấy đang đặt `vw` thì tất cả module đều đặt là `vw` hết. Chứ cái thì đặt `VideoWall`, cái thì đặt `vw`. Nó không có đồng nhất với nhau.
- **[18:11] Dev:** Thấy dài với ngắn...
- **[18:13] Tech Lead:** Thôi lại VideoWall này hôm qua nói sửa mới rồi đó. Xong. Rồi ok.
- **[18:33] Dev:** Gì nữa?
- **[18:36] Tech Lead:** Kiểu nước... bình thường.
- **[18:42] Dev:** Kiểu nước qua? Kiểu nước của Kiên... Kiên hả? Kiên phòng mình á hả?
- **[18:47] Tech Lead:** Ừ, bữa nói mấy đứa kêu... nói là chiều nước đi...
- **[18:52] Dev:** Kiên KM hả?
- **[18:54] Tech Lead:** Nói chung là... còn dư tí...
- **[19:03] Dev:** Nhiều que ấy hả?
- **[19:09] Tech Lead:** 28, 29 gì đó nè, nó dư một tí. Còn IP share xài á không phải là cố định một mảng một hàng đâu, cái nào chưa xài hết thì share cho người khác xài.
- **[19:23] Dev:** Cái mask của WAN á, là cũng share chung luôn rồi đúng không? Hay sao? Cái vùng mask á?
- **[19:33] Tech Lead:** Thì bữa nói là mấy acc kia xài... xài nó hết đi rồi nếu mà cần thêm thì share ngày mai xài.

---

> 📌 **Ghi chú liên kết chuyển giao phân hệ:**  
> Toàn bộ phần trao đổi tiếp theo từ mốc [16:00] của `duong_so_8_2.m4a` và toàn bộ tệp ghi âm `duong_so_8_3.m4a` (06:12) tập trung hoàn toàn vào phân hệ **VideoWall** (Chuẩn hóa tiền tố `vw_`, Cấu hình mạng dải IP LAN/WAN Subnet Mask `/28`, `/29`, Layout hiển thị ma trận màn hình từ Display, Bảng màu tương phản UI, và Cơ chế phân quyền gán Group/Multi-select cho ma trận màn hình). Nội dung này được tách độc lập thành biên bản Tier A tại:  
> [`../../../VideoWall/doc/transcript/22-10-2026-videowall-phan-quyen-multi-user-va-giao-dien-ma-tran.md`](../../../VideoWall/doc/transcript/22-10-2026-videowall-phan-quyen-multi-user-va-giao-dien-ma-tran.md).

