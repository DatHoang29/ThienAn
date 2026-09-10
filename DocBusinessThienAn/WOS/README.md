# WOS: Trạm quan trắc thời tiết & Cảm biến khí tượng (Gió, Mưa) — Chỉ mục tài liệu

> **Single Source of Truth (SSOT)** phân hệ Trạm thời tiết khí tượng (WOS) dự án Cao tốc Hữu Nghị – Chi Lăng.  
> Quản lý toàn bộ danh mục tài liệu đặc tả, tài liệu chuyển đổi Markdown, hướng dẫn tích hợp và Tier Table theo nguyên tắc **3-Tier Context Budget**.

---

## 1. Thông tin phần cứng & Mục tiêu tích hợp

- **Bộ điều khiển trung tâm (Datalogger)**: Campbell Scientific **CR1000X** / **CR1000Xe**.
- **Cảm biến đo gió (Wind Speed & Direction)**: Model **05103** (R. M. Young / Campbell Scientific) — Đo tốc độ gió bằng phát xung AC và hướng gió qua biến trở góc quay ratiometric.
- **Cảm biến đo mưa (Rain Gauge)**: Model **TB4** (Tipping Bucket) — Đo lượng mưa tức thời và tích lũy bằng tiếp điểm thùng lật switch-closure (độ phân giải 0.2 mm hoặc 0.1 mm).
- **Cảm biến nhiệt độ & độ ẩm**: Cảm biến tương tự (0–1 V / 0–5 V / 4–20 mA) hoặc chuẩn số SDI-12.
- **Hồ sơ thiết kế thi công (BVTKTC)**: Gồm tập bản vẽ thiết kế thi công trạm WOS và hồ sơ nghiệm thu đo vẽ liên phân hệ (CCTV, VDS, VMS, DTS, WOS, TMS, PBX).
- **Mục tiêu tích hợp phần mềm**:
  - Giao thức truyền số liệu về trung tâm điều hành: **Modbus TCP** (qua cổng mạng Ethernet RJ45) hoặc **Modbus RTU** (qua cổng nối tiếp RS-485 trên chân C1–C8).
  - Thu thập dữ liệu định kỳ: Nhiệt độ không khí (°C), độ ẩm (%), lượng mưa tích lũy (mm), tốc độ gió (m/s), hướng gió (0–360°), điện áp ắc quy trạm (V).
  - Xuất bảng dữ liệu chuẩn: Định dạng chuỗi thời gian TOA5 / CSV phục vụ nạp CSDL SQL Server và hiển thị WebAPI / VideoWall.

---

## 2. Hướng dẫn định tuyến tra cứu nhanh (Developer Quick Map)

| Bạn đang cần... | Mở file tài liệu | Tóm tắt nội dung |
|---|---|---|
| **Xem thông số kỹ thuật & bảng chân tổng quát** | [`doc/cr1000x-specifications.md`](doc/cr1000x-specifications.md) | Kích thước, nguồn 12V/SW12, 16 kênh SE / 8 DIFF, 2 kênh 4-20mA, đếm xung P1/P2 |
| **Bắt đầu nhanh, kết nối máy tính, tạo code Short Cut** | [`doc/cr1000x-getting-started-guide.md`](doc/cr1000x-getting-started-guide.md) | Cắm cáp USB, dùng EZSetup, tạo file `.CR1X` bằng Short Cut, kết nối PC400/LoggerNet |
| **Tra cứu cẩm nang chi tiết (334 trang)** | [`doc/cr1000x-product-manual/00-catalog.md`](doc/cr1000x-product-manual/00-catalog.md) | Mục lục điều hướng 11 chuyên đề Tier A và 1 file toàn văn Tier B |
| **Đấu nối dây cảm biến gió 05103 & mưa TB4** | [`doc/cr1000x-product-manual/02-wiring-and-terminal-functions.md`](doc/cr1000x-product-manual/02-wiring-and-terminal-functions.md) | Sơ đồ chân đếm xung P1/P2, chân kích thích VX, chân mass tương tự/tiếp địa vỏ |
| **Lập trình đọc số liệu cảm biến (Analog, Pulse, SDI-12)** | [`doc/cr1000x-product-manual/07-measurements.md`](doc/cr1000x-product-manual/07-measurements.md) | Lệnh đo điện áp, đo xung mưa, đo tốc độ gió xoay chiều, đo vòng lặp 4-20mA |
| **Cấu hình Modbus RTU / Modbus TCP / SDI-12** | [`doc/cr1000x-product-manual/08-communications-protocols.md`](doc/cr1000x-product-manual/08-communications-protocols.md) | Khai báo Modbus Server, bảng thanh ghi Holding Registers, lệnh SDI-12 |
| **Tra cứu biến hệ thống Status & Bảng DataTableInfo** | [`doc/cr1000x-product-manual/10-settings-and-status-tables.md`](doc/cr1000x-product-manual/10-settings-and-status-tables.md) | Giám sát pin Lithium, điện áp ắc quy, lỗi Watchdog, trễ quét SkippedScan |
| **Xem hồ sơ bản vẽ thi công & vị trí cột trạm** | `_source/pdf/5. BVTKTC_WOS.pdf` | Bản vẽ CAD/Scan mặt bằng vị trí cột trạm WOS, móng cột, tủ thiết bị ngoài hiện trường |

---

## 3. Tier Table (Bảng phân loại tài liệu theo 3-Tier Context Budget)

| Tier | File | Read | Size | Nội dung | Nguồn gốc |
|---|---|---|---|---|---|
| **A** | [`doc/cr1000x-specifications.md`](doc/cr1000x-specifications.md) | full | 14.2 KB | Thông số kỹ thuật chi tiết, yêu cầu nguồn, dải đo tương tự, xung, chức năng chân | `_source/pdf/s_cr1000x.pdf` |
| **A** | [`doc/cr1000x-getting-started-guide.md`](doc/cr1000x-getting-started-guide.md) | full | 12.8 KB | Hướng dẫn khởi động nhanh, nạp code Short Cut, cấu hình EZSetup, kết nối PC400/LoggerNet | `_source/pdf/cr1000x-getting-started-guide.pdf` |
| **A** | [`doc/cr1000x-product-manual/00-catalog.md`](doc/cr1000x-product-manual/00-catalog.md) | full | 4.9 KB | Mục lục và chỉ mục tra cứu toàn bộ 11 chuyên đề cẩm nang CR1000X | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/01-introduction-and-components.md`](doc/cr1000x-product-manual/01-introduction-and-components.md) | full | 9.6 KB | Chương 1–4: Giới thiệu, cảnh báo an toàn, kiểm tra thiết bị, thành phần phần cứng | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/02-wiring-and-terminal-functions.md`](doc/cr1000x-product-manual/02-wiring-and-terminal-functions.md) | full | 24.9 KB | Chương 5: Sơ đồ bảng đấu dây, chức năng chân nguồn, tiếp địa, cổng xung, tương tự, C1–C8 | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/03-setup-and-communications.md`](doc/cr1000x-product-manual/03-setup-and-communications.md) | full | 33.3 KB | Chương 6: Thiết lập cấu hình IP mạng, kết nối USB, Web Interface, RNDIS, EZSetup | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/04-pre-installation-and-security.md`](doc/cr1000x-product-manual/04-pre-installation-and-security.md) | full | 29.7 KB | Chương 7: Chuẩn bị lắp đặt, cơ chế bảo mật UID, cổng mạng HTTP/FTP/Telnet/Ping, mã hóa TLS | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/05-installation-and-cloud.md`](doc/cr1000x-product-manual/05-installation-and-cloud.md) | full | 8.7 KB | Chương 8–9: Hướng dẫn lắp đặt ngoài hiện trường, tủ trạm, chống sét lan truyền, CampbellCloud | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/06-data-tables-and-memory.md`](doc/cr1000x-product-manual/06-data-tables-and-memory.md) | full | 42.8 KB | Chương 10–11: Xử lý dữ liệu, định dạng bảng TOA5, bộ nhớ Flash CPU và thẻ MicroSD | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/07-measurements.md`](doc/cr1000x-product-manual/07-measurements.md) | full | 37.3 KB | Chương 12: Kỹ thuật đo cảm biến thời tiết, đo xung gió/mưa TB4, đo dòng loop 4-20mA, PT100 | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/08-communications-protocols.md`](doc/cr1000x-product-manual/08-communications-protocols.md) | full | 48.0 KB | Chương 13: Giao thức Modbus RTU/TCP Server/Client, truyền thông nối tiếp RS-485, SDI-12 | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/09-maintenance-and-troubleshooting.md`](doc/cr1000x-product-manual/09-maintenance-and-troubleshooting.md) | full | 92.6 KB | Chương 14–15: Bảo trì, thay pin Lithium, cập nhật OS, mã lỗi, chẩn đoán NAN/INF, lọc nhiễu | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/10-settings-and-status-tables.md`](doc/cr1000x-product-manual/10-settings-and-status-tables.md) | full | 57.3 KB | Chương 16: Bảng thông tin hệ thống Status, DataTableInfo, CPIStatus, danh mục cài đặt Settings | `_source/pdf/cr1000x-product-manual.pdf` |
| **A** | [`doc/cr1000x-product-manual/11-specifications-and-glossary.md`](doc/cr1000x-product-manual/11-specifications-and-glossary.md) | full | 115.8 KB | Chương 17–18 + Phụ lục A: Thông số chi tiết CR1000Xe/CR1000X và từ điển thuật ngữ A–Z | `_source/pdf/cr1000x-product-manual.pdf` |
| **B** | [`doc/cr1000x-product-manual/cr1000x-product-manual-full.md`](doc/cr1000x-product-manual/cr1000x-product-manual-full.md) | grep-only | 512.6 KB | Toàn văn 334 trang cẩm nang CR1000X (Chỉ dùng công cụ grep_search, không đọc nguyên file) | `_source/pdf/cr1000x-product-manual.pdf` |
| **C** | `_source/pdf/s_cr1000x.pdf` | never | 1.04 MB | File PDF gốc bảng thông số kỹ thuật (8 trang) | → bản `.md`: `doc/cr1000x-specifications.md` |
| **C** | `_source/pdf/cr1000x-getting-started-guide.pdf` | never | 1.6 MB | File PDF gốc hướng dẫn khởi động nhanh (19 trang) | → bản `.md`: `doc/cr1000x-getting-started-guide.md` |
| **C** | `_source/pdf/cr1000x-product-manual.pdf` | never | 11.8 MB | File PDF gốc cẩm nang sản phẩm đầy đủ (334 trang) | → bản `.md`: `doc/cr1000x-product-manual/` |
| **C** | `_source/pdf/5. BVTKTC_WOS.pdf` | never | 25.15 MB | Bản vẽ thiết kế thi công trạm quan trắc thời tiết WOS (33 trang bản vẽ CAD/scan ảnh, không có text kỹ thuật số) | Bản vẽ xây dựng/lắp đặt hiện trường |
| **C** | `_source/pdf/C2.TAP III.Q2.1.3 NTĐV CCTV,VDS,VMS,DTS,WOS,TMS,PBX.pdf` | never | 244.98 MB | Hồ sơ thiết kế kỹ thuật nghiệm thu đo vẽ Tập III Quyển 2.1.3 (284 trang bản vẽ scan tổng hợp 7 phân hệ) | Hồ sơ nghiệm thu hiện trường |
| **C** | `_source/pdf/C2.TAP III.Q2.2.3 NTĐV CCTV,VDS,VMS,DTS,WOS,TMS,PBX.pdf` | never | 183.89 MB | Hồ sơ thiết kế kỹ thuật nghiệm thu đo vẽ Tập III Quyển 2.2.3 (189 trang bản vẽ scan tổng hợp 7 phân hệ) | Hồ sơ nghiệm thu hiện trường |

---

## 4. Liên kết tài liệu trực tuyến (Online Resources)

- Trang chủ thiết bị: https://www.campbellsci.eu/cr1000x
- Cẩm nang trực tuyến (Campbell Scientific Web Help): https://help.campbellsci.com/CR1000X/Default.htm
- Hướng dẫn Short Cut & CRBasic: https://www.campbellsci.com/shortcut
