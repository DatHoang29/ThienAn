---
tier: A
read: full
source: _source/pdf/cr1000x-getting-started-guide.pdf
source_pages: 1-19
extracted: 2026-09-09
---

# Campbell Scientific CR1000X/CR1000Xe — Getting Started Guide (Hướng dẫn khởi động nhanh)

> **Nguồn**: `_source/pdf/cr1000x-getting-started-guide.pdf` (CR1000X/CR1000Xe Getting Started Guide, LoggerNet/PC400 Version, Revision: 09/2024).  
> **Áp dụng cho**: Datalogger CR1000X và CR1000Xe, kết nối phần mềm LoggerNet (v4.3+), PC400 (v4.3+), hoặc Device Configuration Utility (DevConfig v2.12+).

---

## 1. Chuẩn bị ban đầu (Getting Started)

Tài liệu này hướng dẫn các bước cơ bản để kết nối máy tính với Datalogger CR1000X/CR1000Xe, tạo chương trình đo thời tiết tự động bằng công cụ **Short Cut**, nạp chương trình xuống thiết bị và thu thập dữ liệu về máy tính.

### 1.1 Thiết bị & Phụ kiện cần thiết
1. **Datalogger CR1000X** (hoặc CR1000Xe).
2. **Cáp Micro-USB** chuẩn USB 2.0 (kết nối trực tiếp từ cổng USB Type-A trên máy tính sang cổng micro-USB trên CR1000X).
3. **Nguồn cấp 12 VDC** (nguồn điện áp 10–18 VDC, tối thiểu 2 A, hoặc ắc quy trạm thời tiết).
4. **Phần mềm điều khiển trên máy tính**:
   - **LoggerNet** (bản đầy đủ cho quản lý trạm tập trung) hoặc **PC400** (bản miễn phí/rút gọn).
   - Hoặc công cụ cấu hình chuyên dụng **Device Configuration Utility** (DevConfig).

> [!NOTE]
> Khi cắm cáp USB, CR1000X có thể lấy nguồn 5 VDC từ máy tính để thực hiện cấu hình mạng, cập nhật hệ điều hành (OS) và nạp chương trình. Tuy nhiên, để cấp nguồn cho các cảm biến khí tượng (gió, mưa, nhiệt ẩm) và vận hành các cổng công nghiệp (CS I/O, 12V, SW12), **bắt buộc phải cấp nguồn 12 VDC** vào cầu đấu `POWER IN`.

---

## 2. Kết nối truyền thông qua USB (USB Communications)

### 2.1 Cài đặt Driver và nhận diện cổng
1. Cài đặt phần mềm LoggerNet, PC400 hoặc DevConfig trước khi cắm cáp USB (quá trình cài đặt phần mềm sẽ tự động nạp Campbell Scientific USB Drivers).
2. Cắm cáp micro-USB vào CR1000X và cổng USB máy tính.
3. Windows sẽ nhận diện thiết bị dưới dạng **Campbell Scientific CR1000X Datalogger** và tạo một cổng COM ảo (Virtual COM port, ví dụ: `COM3`, `COM4`...).

### 2.2 Cấu hình trực tiếp bằng Device Configuration Utility (DevConfig)
1. Mở phần mềm **Device Configuration Utility**.
2. Tại danh sách thiết bị bên trái (**Device Type**), chọn **CR1000X Series**.
3. Chọn cổng COM tương ứng (hoặc chọn kết nối qua **IP / Ethernet** nếu cắm dây mạng RJ45).
4. Nhấn **Connect**.
5. Sau khi kết nối thành công, màn hình sẽ hiển thị:
   - Thông tin hệ điều hành (OS Version).
   - Số seri thiết bị (Serial Number).
   - Địa chỉ PakBus (mặc định là `1`).
   - Cấu hình địa chỉ IP (Ethernet / RNDIS).
   - Cho phép đặt mật khẩu bảo mật (Security Code) và tài khoản truy cập Web.

---

## 3. Kiểm tra kết nối với EZSetup Wizard (Testing Communications with EZSetup)

Trình hướng dẫn **EZSetup Wizard** trong LoggerNet hoặc PC400 giúp người dùng thiết lập kết nối lần đầu một cách dễ dàng:

### Các bước thực hiện:
1. Khởi động **LoggerNet** hoặc **PC400**.
2. Trên thanh công cụ, chọn biểu tượng **EZSetup Wizard** (hoặc chọn từ menu *Tools*).
3. Nhấn **Next** tại màn hình giới thiệu.
4. **Chọn loại Datalogger**: Chọn **CR1000X** trong danh mục thiết bị, đặt tên trạm (**Station Name**, ví dụ: `WOS_HuuNghi_ChiLang`). Nhấn **Next**.
5. **Chọn kiểu kết nối (Communication Type)**:
   - Chọn **Direct Connect** nếu dùng cáp USB hoặc RS-232.
   - Chọn **IP (Ethernet)** nếu kết nối qua mạng LAN/Internet.
6. **Chọn cổng COM & Baud Rate**:
   - Chọn cổng COM của CR1000X (thường ghi kèm tên thiết bị).
   - Baud rate để mặc định **115,200 bps**. Nhấn **Next**.
7. **Địa chỉ PakBus**: Giữ giá trị mặc định là `1` (trừ khi có yêu cầu cấu hình mạng đa điểm PakBus).
8. **Mật khẩu bảo mật (Security Code)**: Để trống (`0`) nếu chưa thiết lập bảo mật.
9. **Kiểm tra truyền thông (Communication Test)**:
   - Nhấn nút **Test Communication**.
   - Nếu hiển thị **Communication test successful**, chứng tỏ đường truyền đã sẵn sàng.
10. Nhấn **Next**, kiểm tra đồng bộ xung nhịp (**Set Datalogger Clock**) để đồng bộ giờ thiết bị theo giờ máy tính.
11. Thiết bị đi kèm một chương trình mặc định `GettingStarted.CR1X`. Nhấn **Finish** để hoàn tất cấu hình.

---

## 4. Kết nối phần mềm & Giám sát trực tiếp (Making the Software Connection)

### 4.1 Màn hình kết nối (Connect Screen)
1. Trong LoggerNet: Nhấn biểu tượng **Connect** (hoặc mở màn hình điều khiển chính trong PC400).
2. Danh sách các trạm bên trái hiển thị tên trạm đã tạo (ví dụ: `WOS_HuuNghi_ChiLang`).
3. Chọn trạm và nhấn nút **Connect**:
   - Biểu tượng trạng thái chuyển sang màu xanh lá cây.
   - Hiển thị thời gian đồng hồ của Datalogger (**Datalogger Clock**) và tên chương trình hiện hành.

### 4.2 Giám sát dữ liệu thời gian thực (Numeric Display & Graphs)
- **Bảng Public (Public Table)**: Chứa tất cả các biến đo kiểm tức thời (nhiệt độ, tốc độ gió, hướng gió, điện áp ắc quy).
- **Mở màn hình số liệu (Numeric Display)**: Nhấn *Numeric Display*, chọn các biến quan sát để hiển thị số đo cập nhật liên tục theo chu kỳ quét (scan rate).
- **Xem đồ thị trực tiếp**: Nhấn *Strip Chart* hoặc *Graph* để vẽ biểu đồ trực quan biến thiên của hướng gió, tốc độ gió và lượng mưa theo thời gian thực.

---

## 5. Tạo chương trình trạm thời tiết bằng Short Cut (Creating a Short Cut Program)

**Short Cut** (SCWin) là trình tạo mã nguồn CRBasic tự động của Campbell Scientific, cho phép cấu hình cảm biến thời tiết mà không cần lập trình thủ công từ đầu.

### 5.1 Quy trình tạo chương trình gồm 5 bước:

#### Bước 1: Khởi động Short Cut và chọn thiết bị
1. Mở LoggerNet / PC400 $ightarrow$ chọn **Short Cut** (biểu tượng chiếc kéo).
2. Chọn **New Program** (Tạo chương trình mới).
3. Chọn dòng Datalogger: **CR1000X**.
4. Chọn chu kỳ quét chính (**Scan Interval**): Đặt là **1 giây** (1 Sec) hoặc **5 giây** tùy theo yêu cầu của trạm quan trắc.

#### Bước 2: Chọn cảm biến thời tiết (Sensors)
Tại cây thư mục cảm biến bên trái:
- **Cảm biến gió (Wind Speed & Direction)**:
  - Chọn nhóm *Meteorological* $ightarrow$ *Wind Speed & Direction*.
  - Chọn mã cảm biến **05103 Wind Monitor** (R. M. Young / Campbell).
  - Chọn đơn vị đo: m/s cho tốc độ gió, độ (Degrees 0–360°) cho hướng gió.
  - Khai báo chân đấu nối theo sơ đồ gợi ý (chân đếm xung P1/P2 cho tốc độ gió, chân tương tự SE/DIFF cho hướng gió, chân kích thích VX).
- **Cảm biến mưa (Precipitation / Rain)**:
  - Chọn nhóm *Meteorological* $ightarrow$ *Precipitation*.
  - Chọn mã cảm biến **TB4 Rain Gauge** (hoặc Tipping Bucket Rain Gauge).
  - Khai báo dung tích thùng lật (0.2 mm hoặc 0.1 mm mỗi xung lật).
  - Chọn chân đếm xung: P1, P2 hoặc C1–C8.
- **Cảm biến nhiệt độ & độ ẩm không khí**:
  - Chọn cảm biến tương tự (0–1 V / 0–5 V) hoặc cảm biến số chuẩn SDI-12.

#### Bước 3: Xem sơ đồ đấu dây tự động (Wiring Diagram)
- Nhấn tab **Wiring Diagram**: Short Cut sẽ vẽ sơ đồ đấu nối chi tiết từng sợi dây màu của cảm biến vào đúng các cọc chân trên thanh đấu dây của CR1000X.
- Có thể in hoặc lưu sơ đồ này dưới dạng tài liệu kỹ thuật hiện trường.

#### Bước 4: Thiết lập bảng dữ liệu xuất ra (Output Tables)
1. Short Cut hỗ trợ cấu hình các bảng dữ liệu định kỳ:
   - **Bảng 1 phút (Table 1 Min)**: Lấy giá trị trung bình tốc độ gió, hướng gió trung bình vector, tổng lượng mưa trong 1 phút.
   - **Bảng 10 phút hoặc 1 giờ (Table 60 Min)**: Lưu trữ thống kê phục vụ báo cáo.
2. Với mỗi biến, chọn phép toán thống kê tương ứng:
   - **Sample**: Lấy mẫu tức thời.
   - **Average**: Giá trị trung bình.
   - **Maximum / Minimum**: Giá trị cực đại / cực tiểu kèm thời điểm xuất hiện (Time of Max/Min).
   - **Total**: Tính tổng (dùng cho đo lượng mưa tích lũy).
   - **WindVector**: Tính toán vector gió chuẩn WMO (Mean Speed, Resultant Speed, Resultant Direction, Standard Deviation of Direction).

#### Bước 5: Biên dịch và xuất file chương trình
- Nhấn **Finish**: Short Cut sẽ tự động tạo file mã nguồn CRBasic có đuôi mở rộng là `.CR1X` (ví dụ: `WOS_Station.CR1X`).

---

## 6. Nạp chương trình xuống Datalogger (Sending a Program)

### Các bước nạp chương trình:
1. Mở màn hình **Connect Screen** trong LoggerNet / PC400.
2. Đảm bảo trạng thái đã kết nối (**Connected**) với CR1000X.
3. Nhấn nút **Send New...** (hoặc *File Control* $ightarrow$ *Send*).
4. Duyệt tìm file chương trình vừa tạo (`.CR1X`).
5. Phần mềm sẽ hỏi xác nhận ghi đè chương trình:
   - **Run Now**: Chạy chương trình ngay lập tức.
   - **Run on Power-up**: Tự động chạy lại chương trình mỗi khi mất điện khởi động lại thiết bị.
6. Quá trình nạp:
   - File code được truyền qua cổng USB/mạng vào bộ nhớ `CPU:`.
   - Datalogger thực hiện biên dịch mã nguồn (**Compiling**).
   - Nếu biên dịch thành công, thông báo hiển thị: `Program compiled successfully`.
7. Kiểm tra thẻ **Table Fill Times**:
   - Xem dung lượng bộ nhớ cấp phát cho từng bảng dữ liệu.
   - Xem số ngày có thể lưu trữ dữ liệu trước khi bị ghi đè vòng tròn (ví dụ: 120 ngày, 365 ngày).

---

## 7. Quản lý & Thu thập dữ liệu (Working with Data)

### 7.1 Cấu trúc các bảng dữ liệu mặc định
Khi chạy chương trình, CR1000X tự động duy trì các bảng hệ thống:
- **`Status` Table**: Chứa hơn 50 thông số giám sát hệ thống (điện áp ắc quy, nhiệt độ bảng mạch, lỗi watchdog, số lần quét bị trễ, dung lượng bộ nhớ còn trống).
- **`Public` Table**: Chứa các biến động đang chạy trong chu kỳ quét hiện tại.
- **`DataTableInfo` Table**: Chứa siêu dữ liệu về dung lượng và số bản ghi của từng bảng dữ liệu.
- **Các bảng dữ liệu do người dùng định nghĩa**: Ví dụ `WOS_Data_1Min`, `WOS_Data_Daily` chứa chuỗi thời gian quan trắc khí tượng.

### 7.2 Thu thập dữ liệu (Collecting Data)
1. Trong màn hình **Connect Screen**, nhấn nút **Collect Data** (hoặc thiết lập tự động thu thập theo lịch trình qua *Schedule Data Collection* trong LoggerNet).
2. **Các tùy chọn thu thập**:
   - **Append to existing file**: Ghi nối tiếp dữ liệu mới vào cuối file hiện có trên máy tính (khuyến nghị).
   - **Create new file**: Tạo file mới mỗi lần tải.
3. **Định dạng file lưu trữ**:
   - **TOA5** (Table Oriented ASCII format 5): Định dạng chuẩn CSV của Campbell Scientific, có 4 dòng tiêu đề (Header rows) chứa thông tin trạm, tên biến, đơn vị đo và kiểu tính toán. Dễ dàng đọc trực tiếp bằng Excel, Python, hoặc nạp vào CSDL SQL Server.
   - **TOB1**: Định dạng nhị phân dung lượng nhẹ.

### 7.3 Xem và phân tích dữ liệu lịch sử (Viewing Historic Data)
1. Mở công cụ **View Pro** trong LoggerNet/PC400.
2. Chọn file `.dat` hoặc `.csv` đã tải về.
3. Chức năng chính:
   - Xem bảng dữ liệu chi tiết theo từng mốc thời gian (Timestamp chuẩn UTC hoặc Local time).
   - Vẽ đồ thị nhiều trục (Multi-axis charts) kết hợp giữa tốc độ gió, nhiệt độ và vũ lượng mưa.
   - Xuất dữ liệu sang các định dạng chuẩn khác để phục vụ tích hợp hệ thống WebAPI / GIS / ITS.
