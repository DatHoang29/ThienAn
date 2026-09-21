---
tier: A
read: full
source: ../../../Plan/_source/audio/16-09-2026/16-09-2026-sua-ui-sharedata.m4a
date: 16-09-2026
duration: ~06 phút 13 giây
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData (Chia sẻ Dữ liệu - ESHARE) - Tổng hợp & Thống nhất Danh mục Công việc Sửa UI/UX
participants:
  - Hiếu (Dev ShareData / Trình bày & Thống nhất checklist)
  - Anh Sơn (Tech Lead / Chốt yêu cầu kỹ thuật & Kiến trúc)
  - Đạt (Dev Backend / Tham gia đối chiếu)
---

# Kịch bản & Biên bản Cuộc họp: Tổng Hợp & Thống Nhất Các Hạng Mục Sửa Giao Diện (Sửa UI ShareData)

> **Ghi chú:** Bản ghi được bóc tách trực tiếp bằng phương thức **Gemini Multimodal Native Audio Understanding** từ tệp âm thanh cuộc họp nội bộ ngày 16/09/2026 (`16-09-2026-sua-ui-sharedata.m4a`). Đây là phiên họp recap ngắn ngay sau buổi demo tổng thể sáng 16/09/2026, trong đó Hiếu (ShareData) cùng Anh Sơn (Tech Lead) và Đạt (Backend) rà soát lại từng mục cụ thể để chốt danh sách đầu việc cần chỉnh sửa cho phân hệ **Chia sẻ dữ liệu (ShareData / ESHARE)**.

---

## 1. Tóm tắt tổng quan & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Cấu hình Đối tác (Partner Settings)
- **Bảng danh sách:** Thêm hiển thị cột **Mã đối tác (Partner Code)** rõ ràng bên cạnh Tên đối tác.
- **Modal Chỉnh sửa (Edit Modal):** Hiện trạng cơ bản ổn, giữ nguyên. Các tính năng mở rộng (như cấu hình giao thức SMTP, SSE, bảo vệ Port khi đổi tên đối tác) sẽ ghi nhận phát triển ở giai đoạn sau.

### 1.2. Cấu hình Đăng ký chia sẻ (Subscription Config)
- **Bỏ bớt trường dư thừa & Hộp cảnh báo:** Tại khu vực hiển thị *"Địa chỉ liên kết nối gần nhất / Gửi gần nhất"*, tiến hành lược bỏ bớt các trường không cần thiết và ẩn hoàn toàn box cảnh báo.
- **Cơ chế gửi theo dữ liệu mới:** Thay thế cơ chế nút radio *"Gửi theo sự kiện"* bằng lựa chọn **"Gửi ngay khi có dữ liệu mới"** (dựa vào trường `UpdateTime` / `CreateTime` trong CSDL để phát hiện bản ghi mới sinh ra).
- **Lịch gửi hàng ngày:** Bỏ cấu hình định dạng (format) và ẩn trường Độ ưu tiên (Priority) vì chưa dùng.
- **Chiều nhận (Inbound):** Bỏ toàn bộ cấu hình kiểu lịch ở chiều nhận (vì nhận dữ liệu là bị động theo request của đối tác), giữ giao diện chiều nhận tinh gọn, chỉ tiếp nhận và lưu trữ.

### 1.3. Cấu hình Gói tin (Packet Config & Data Grouping)
- **CRUD đầy đủ:** Bổ sung chức năng Thêm / Sửa / Xóa cho gói tin và các cột trường dữ liệu tương ứng (thay vì hardcode).
- **Sửa lỗi Layout & Thanh cuộn ngang (Horizontal Scroll):**
  - Căn chỉnh lại độ rộng cột (Column Width): Không để cột Mã trường quá rộng khiến các cột phía sau bị đẩy dạt ra ngoài phải cuộn ngang.
  - Sửa lỗi vùng phân trang (Pagination) ở chân bảng bị đè/lệch.
  - Ghim cố định (Freeze/Fix column): Cố định cột Số thứ tự (STT) ở đầu hoặc cột Hành động (Action) ở cuối để khi cuộn ngang không bị trôi mất ngữ cảnh.
- **Trừu tượng hóa CSDL — Gom nhóm theo Tệp dữ liệu (Data Grouping):**
  - **Quyết định quan trọng:** Không hiển thị giao diện theo từng "Tên bảng CSDL vật lý" (tránh lộ cấu trúc DB nội bộ ra ngoài).
  - Thay vào đó, gom nhóm theo **Tệp dữ liệu nghiệp vụ (Data File / Tệp dữ liệu mong muốn)**. Một tệp dữ liệu có thể tổng hợp từ nhiều bảng con thành 10–12 cột đầu ra thống nhất, giao diện chỉ hiển thị danh sách các cột thuộc tệp đó. Bỏ hẳn khái niệm "Bí danh / Alias" gây rối mắt.

### 1.4. Ánh xạ Dữ liệu (Data Mapping & CodeSet Default Value)
- **Cột tùy chọn ánh xạ:** Thêm một cột riêng thể hiện các tùy chọn đi kèm (Option/Setting) cho từng trường ánh xạ.
- **Dữ liệu gửi thử nghiệm (Test Data):** Khi bấm nút *"Gửi thử"* hoặc *"Lấy dữ liệu mẫu"*, hệ thống phải truy vấn dữ liệu thật từ dưới CSDL lên để kiểm tra mapping thay vì dùng dữ liệu giả lập.
- **Tab Thông tin chung:** Bỏ các trường không cần thiết như Định dạng, Phiên bản.
- **Bộ mã chuẩn hóa (CodeSet):**
  - Bổ sung ô cấu hình **Giá trị mặc định (`Default Value`)** để xử lý trường hợp dữ liệu nguồn không khớp với bất kỳ mã nào trong CodeSet hoặc bị `null`.
  - Phân định chiều cho `Default Value`: Cần xác định rõ giá trị mặc định áp dụng cho chiều gửi (Outbound) hay chiều nhận (Inbound).

### 1.5. Lịch sử chia sẻ & Ghi log truyền nhận 2 bước (History & 2-Step Transmission Log)
- **Chuẩn hóa bộ lọc thời gian:** Đổi sang component chọn ngày giờ chuẩn (`DateTime Picker`).
- **Lọc tìm kiếm:** Bỏ chức năng tìm kiếm toàn văn nội dung trong tab lịch sử (tránh làm chậm câu truy vấn log); chuẩn hóa lại tên các ô lọc (Đối tác, Đối tượng...).
- **Chuyển đổi Modal chi tiết:** Thay thế thanh trượt Sidebar bằng **cửa sổ Modal** khi người dùng double-click vào từng dòng lịch sử truyền nhận.
- **Chuỗi Log 2 bước (Step Log có quan hệ Cha - Con):**
  - Trong một phiên truyền nhận, hệ thống phải thể hiện rõ 2 bước con độc lập trong cùng 1 log cha:
    - *Chiều gửi (Outbound):* Step 1 (Trích xuất dữ liệu từ DB lên) $\to$ Step 2 (Xử lý, biến đổi mapping và gửi đi đối tác).
    - *Chiều nhận (Inbound):* Step 1 (Tiếp nhận payload gói tin từ đối tác) $\to$ Step 2 (Xử lý, chuyển đổi mapping và ghi vào DB).
- **Bảng mã lỗi (Error Codes):** Khai báo danh mục mã lỗi chuẩn hóa trong bảng cấu hình hệ thống để tra cứu khi gói tin thất bại.

---

## 2. Ma trận phân công công việc & Checklist thực thi (Action Items Checklist)

| STT | Hạng mục công việc | Phân hệ / Màn hình | Người phụ trách | Chi tiết kỹ thuật cần thực hiện |
|---|---|---|---|---|
| 1 | Thêm Mã đối tác | Cấu hình Đối tác | **Hiếu (ShareData)** | Hiển thị cột Mã đối tác bên cạnh Tên đối tác trên bảng danh sách. |
| 2 | Rào port & Bảo vệ thông tin | Cấu hình Đối tác | **Hiếu / Đạt (ShareData)** | Khóa/Rào lại trường Port khi chỉnh sửa tên đối tác; chuẩn bị sẵn cấu trúc cho SMTP, SSE mở rộng sau. |
| 3 | Tối ưu ô gửi gần nhất & Ẩn cảnh báo | Cấu hình Đăng ký chia sẻ | **Hiếu (ShareData)** | Lược bỏ các trường rườm rà ở ô liên kết/gửi gần nhất; ẩn box cảnh báo. |
| 4 | Chuyển đổi "Gửi khi có data mới" | Cấu hình Đăng ký chia sẻ | **Hiếu (ShareData)** | Bỏ nút radio "Theo sự kiện", thay bằng checkbox "Gửi ngay khi có dữ liệu mới" (dựa trên `UpdateTime`/`CreateTime`). |
| 5 | Tinh giản chiều gửi & nhận | Cấu hình Đăng ký chia sẻ | **Hiếu (ShareData)** | Chiều gửi: Bỏ định dạng, ẩn độ ưu tiên. Chiều nhận: Bỏ hẳn cấu hình kiểu lịch. |
| 6 | Thêm/Sửa/Xóa Gói tin & Trường | Cấu hình Gói tin | **Hiếu / Đạt (ShareData)** | Không hardcode danh sách gói tin và cột nữa; cho phép CRUD động từ DB. |
| 7 | Căn chỉnh độ rộng cột & Sửa scroll | Cấu hình Gói tin | **Hiếu (ShareData)** | Chỉnh width hợp lý cho cột Mã trường, sửa lỗi phân trang ở chân bảng; freeze cột STT hoặc Action. |
| 8 | Hiển thị theo Tệp dữ liệu | Cấu hình Gói tin | **Hiếu / Đạt (ShareData)** | Bỏ hiển thị theo Tên bảng CSDL vật lý và bỏ cột Bí danh; gom thành Tệp dữ liệu nghiệp vụ (Data File). |
| 9 | Cải tiến UI Ánh xạ (Mapping) | Ánh xạ dữ liệu | **Hiếu (ShareData)** | Thêm cột hiển thị option đi kèm; thay icon 3 chấm bằng icon Config; mở modal chồng khi sửa trường. |
| 10 | Lấy data mẫu thật từ DB | Ánh xạ dữ liệu | **Hiếu / Đạt (ShareData)** | Nút gửi thử nghiệm sẽ gọi API lấy bản ghi thật trong CSDL lên để đối chiếu. |
| 11 | Bổ sung `Default Value` cho CodeSet | Ánh xạ dữ liệu | **Đạt / Hiếu (ShareData)** | Thêm trường Giá trị mặc định (hỗ trợ phân biệt chiều gửi/nhận) khi data nguồn không khớp bảng mã. |
| 12 | Chuẩn hóa bộ chọn thời gian | Lịch sử chia sẻ | **Hiếu (ShareData)** | Đổi sang component DateTime chuẩn; bỏ ô tìm kiếm nội dung nặng nề. |
| 13 | Đổi Sidebar thành Modal chi tiết | Lịch sử chia sẻ | **Hiếu (ShareData)** | Khi double click dòng lịch sử, mở Modal hiển thị chi tiết thay vì Sidebar. |
| 14 | Chuẩn hóa Log truyền nhận 2 bước | Lịch sử chia sẻ & Worker | **Đạt / Hiếu (ShareData)** | Thiết lập quan hệ cha-con: 1 phiên log chứa 2 step (Đọc DB $\to$ Gửi đi hoặc Nhận về $\to$ Lưu DB). |
| 15 | Khai báo bảng mã lỗi hệ thống | Cấu hình Hệ thống | **Đạt (Backend)** | Tạo bảng quản lý danh mục Error Code chuẩn phục vụ ghi log và hiển thị trạng thái lỗi. |

---

## 3. Toàn văn nội dung đối thoại (Full Verbatim Transcript)

| Mốc thời gian | Người nói | Lời thoại chi tiết |
|---|---|---|
| `00:00` | **Hiếu** | Từ từ... Thứ nhất là phần cấu hình đối tác. |
| `00:03` | **Anh Sơn** | Nghe là đói bụng quá à. |
| `00:05` | **Hiếu** | Thứ nhất là phần cấu hình đối tác thì ở list danh sách là sẽ thêm mã đối tác thể hiện. Thêm mã đối tác. Ở Edit modal... Edit modal tạm ổn, không cần sửa gì, đúng không? |
| `00:20` | **Anh Sơn** | Comment... Ờ, rồi. |
| `00:23` | **Hiếu** | Tiếp theo... |
| `00:24` | **Anh Sơn** | Nhưng mà nhớ mở rộng nha, cái mở rộng này có nói rồi đó. |
| `00:27` | **Hiếu** | Rồi, mở rộng để sau. Mở rộng để sau. Rồi tiếp theo tới phần cấu hình... Cấu hình subscription thì ở cái ô địa chỉ lần kết nối gần nhất là gửi gần nhất thì xem cấu hình lại sao cho nó hợp lý, bỏ bớt trường đi. Bỏ luôn cả box cảnh báo. |
| `00:48` | **Hiếu** | Rồi còn ở trong Edit cấu hình subscription thì... Thì sao anh Sơn, quên mất tiêu rồi? |
| `00:58` | **Hiếu** | À... Button gửi ngay khi có dữ liệu mới chứ không phải là theo sự kiện. Hằng ngày thì thêm một cái chu kỳ... |
| `01:13` | **Anh Sơn** | Cái gì cái gì? Chu kỳ gì? |
| `01:16` | **Hiếu** | Ý là... |
| `01:17` | **Anh Sơn** | Nói hồi không biết đang nói cái gì luôn! |
| `01:19` | **Hiếu** | Đi vòng vòng một hồi em quay lại cái tự nhiên em bị mất trí nhớ tạm thời! |
| `01:23` | **Hiếu** | Rồi, liên tục là chuẩn rồi nè đúng không? Ở kiểu lịch á thì liên tục là chuẩn rồi. |
| `01:30` | **Anh Sơn** | Thì hiện tại là chuẩn rồi, nhưng mà mốt thêm cái thời gian này kia... |
| `01:34` | **Hiếu** | Thôi giờ nói hiện tại thôi, để mốt... |
| `01:37` | **Anh Sơn** | Nói chung là vậy, nhưng mà chỉnh lại cái giao diện cho nó phù hợp. |
| `01:42` | **Hiếu** | Rồi, hằng ngày thì... |
| `01:45` | **Anh Sơn** | Trời ơi, nữa! |
| `01:47` | **Hiếu** | Bỏ đi định dạng, bỏ ưu tiên... bỏ định dạng, bỏ ưu tiên đi. Còn chiều nhận về thì sẽ bỏ đi kiểu lịch, hiện tại là cứ để nhận về thôi. |
| `01:58` | **Anh Sơn** | Đúng, cho nó gọn. Gọn là nhận chứ không có để... không có cấu hình kiểu lịch. |
| `02:05` | **Hiếu** | Rồi. |
| `02:07` | **Anh Sơn** | Ghi nãy giờ nhiều quá hả? |
| `02:09` | **Hiếu** | Không, em thấy có chút xíu à. |
| `02:11` | **Anh Sơn** | Nhiều quá! |
| `02:12` | **Hiếu** | Ở phần cấu hình gói tin thì cho thêm sửa xóa... Thêm sửa xóa gói tin, thêm sửa xóa các cột dữ liệu. |
| `02:22` | **Anh Sơn** | Rồi chỉnh lại cái... cái dòng đầu nữa kìa. Chỉnh lại cái giao diện kìa. |
| `02:27` | **Hiếu** | Chỉnh lại cái giao diện của cấu hình gói tin. Chỉnh lại cái phần phân trang ở dưới bị lỗi. |
| `02:37` | **Anh Sơn** | Rồi thấy cũng cũng rồi đó. Còn bên kia cái cái đó... cái số 1 2 3 bên kia nữa kìa. Thấy to ở trong đó. Cột đầu chỉnh nó lại. |
| `02:49` | **Hiếu** | Cột đầu nó đang bị rác, bị gác cái gì đó... |
| `02:53` | **Anh Sơn** | Căn chỉnh lại cái độ rộng của cột thôi. Tự nhiên cái mã trường đây cho nó dài vô, xong rồi mấy cột phía sau phải scroll nó qua, thấy muốn đơ ghê! Với cái nữa là cái bảng ở trên... không freeze cái cột cuối hay cột gì đi. |
| `03:13` | **Hiếu** | Không, fix... fix fix sườn... fix sườn... |
| `03:16` | **Anh Sơn** | Nó lại một hai cột gì đó. Rồi bí danh gì đó không có, bỏ mẹ nó đi! Với lại cái này đâu có chia theo bảng dữ liệu, cái này chia theo cái gì? |
| `03:28` | **Hiếu** | Theo field... |
| `03:30` | **Anh Sơn** | Không phải theo field. Theo alias? Không phải theo alias. Bảng mà, người ta nói bảng dữ liệu. Em nói cái này mà! |
| `03:40` | **Hiếu** | Theo gói tin? |
| `03:41` | **Anh Sơn** | Không theo gói tin luôn! Nó gom dữ liệu mình mong muốn. Ví dụ trong này nó có hai cái dữ liệu đi, một cái cụm ví dụ dữ liệu A đi. Thì trong đó nó sẽ có truy vấn của cái dữ liệu A đó, thì trong dữ liệu A đó nó sẽ ra mấy cái cột của dữ liệu A. Nó ra một cái dữ liệu là dữ liệu liên quan đến data đi, thì mình sẽ gom nó lại một cái bảng data thôi. Rồi trong trỏng ví dụ nó có năm năm bảng gì đó kệ mẹ nó, tại vì cái đó nó không quan trọng, miễn sao là năm bảng đó nó ra mười cột và mình lưu lại mười cột đó thôi, liên quan đến cái tệp đó. |
| `04:18` | **Hiếu** | Cấu hình gói tin... Cấu hình gói tin hiển thị theo từng tệp data, không hiển thị theo tên bảng. Mới hiểu luôn á, mới biết luôn á! |
| `04:26` | **Hiếu** | Rồi, Mapping nè. Mapping ở phần cấu hình ánh xạ thì thêm một cột thể hiện những cái option đi kèm. Thiết kế lại UI/UX của phần ánh xạ gói tin. |
| `04:40` | **Anh Sơn** | Ừ. |
| `04:41` | **Hiếu** | Dữ liệu gửi thử thì lấy data từ dưới DB lên. |
| `04:46` | **Anh Sơn** | Ừ, bấm nút gửi hay là bấm nút lấy gì đó... lấy dữ liệu mẫu gì đó. |
| `04:52` | **Hiếu** | Thông tin chung ổn, bỏ đi định dạng, bỏ bỏ phiên bản. Ở phần bộ mã chuẩn hóa thì thêm một cái... thì thêm một một một ô Default là... |
| `05:12` | **Anh Sơn** | Là... là... là giá trị... |
| `05:15` | **Hiếu** | Là giá trị mặc định. Nếu mà dữ liệu không có... |
| `05:19` | **Hiếu** | Nếu dữ liệu không có thì thêm một... thì luôn luôn có một ô Default là dữ liệu... là dữ liệu mặc định. |
| `05:25` | **Anh Sơn** | Nhưng mà dữ liệu mặc định thì xem coi là dữ liệu chiều nào. |
| `05:32` | **Hiếu** | Default Value sẽ thêm chiều của CodeSet, cấu hình gói tin CodeSet sẽ thêm chiều cho Default Value. Ở phần lịch sử thì sẽ chỉnh sửa lại khung thời gian, chỉnh sửa lại tên tên của các ô lựa chọn tìm kiếm: đối tác, đối tượng này. Ở tab truyền nhận thì... ở phần chi tiết cân nhắc thể hiện panel. Thêm các step cho từng bước nhận đi, gửi về. |
| `06:01` | **Anh Sơn** | Step đó là cái gì? |
| `06:03` | **Hiếu** | Ví dụ step gửi đi thì sẽ có là lấy data từ dưới DB lên xử lý rồi gửi đi. Step nhận về là tiếp nhận gói tin, xử lý rồi lưu DB. Khai báo cấu hình... cấu hình mã lỗi ở... khai báo mã lỗi, khai báo bảng mã lỗi ở trong bảng cấu hình hệ thống. |
