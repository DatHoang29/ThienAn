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

## 2. Trạng thái tích hợp phần mềm backend (C#/.NET) — dùng để báo cáo

> Plan chi tiết: [`doc/plan/wos-cr1000x-integration-plan.md`](doc/plan/wos-cr1000x-integration-plan.md).

- **Hiện trạng**: mới có **plan**, backend TA-ITS015 **chưa có code** đọc dữ liệu từ CR1000X (đã rà repo, không có driver/entity/worker nào tồn tại; bảng `TmsWeather` hiện chỉ nhận dữ liệu từ OpenWeatherMap, chưa từng chạy thật).
- **Hướng đi đã chốt**: service C# .NET độc lập (`Services.Wos`), đọc **Modbus TCP** từ CR1000X (CR1000X đóng vai trò server/slave, backend là client), ghi vào `TmsWeather` mở rộng — theo đúng pattern device-driver đã có sẵn trong repo (`Services/Sample`, `Services/VDS`).
- **Ngôn ngữ C# — đã kiểm tra kỹ, xác nhận hỗ trợ đầy đủ** (kiểm tra trực tiếp qua NuGet/GitHub/tài liệu chính thức Campbell Scientific, 10/09/2026):
  - Thư viện Modbus TCP client chọn dùng: **FluentModbus** — thư viện C#/.NET Standard 2.0 & 2.1 thuần, tương thích .NET Framework 4.6.1 → .NET 10.
  - Phiên bản mới nhất `5.3.2`, **MIT license** (tự do dùng nội bộ/thương mại), vẫn đang được bảo trì tích cực (cập nhật gần nhất 01/09/2026).
  - API đúng nhu cầu: `client.Connect(ip, port, ModbusEndianness.BigEndian)` + `client.ReadHoldingRegisters<float>(unitId, start, count)` — khớp layout số thực big-endian 32-bit của CR1000X.
  - Lưu ý implementation: API là **đồng bộ**, không có `ReadHoldingRegistersAsync` sẵn — phải tự bọc `Task.Run(...)`.
  - Phát hiện phụ: thứ tự byte (ABCD/BADC/CDAB/DCBA) là tham số cấu hình trong CRBasic `ModbusServer()` của **từng trạm**, không cố định — plan đã thêm field `byteOrder` vào schema cấu hình thay vì hard-code.
- **Chưa làm**: chưa viết code, chưa tạo project `.csproj`, chưa cài `FluentModbus` NuGet — mới dừng ở bước lập kế hoạch + xác minh khả thi công nghệ.

---

## 3. Vai trò từng cặp tài liệu (PDF gốc ↔ bản Markdown)

Mỗi tài liệu trong hệ thống này tồn tại dưới **2 dạng**: file PDF gốc trong `_source/pdf/` (Tier C — con người mở khi cần, AI không tự đọc) và bản chuyển thể Markdown trong `doc/` (Tier A/B — AI đọc trực tiếp để trả lời). Bảng dưới đây giải thích **vai trò cụ thể** của từng cặp, để biết file nào nói về gì và khi nào nên dùng file nào.

| Cặp tài liệu | Vai trò bản PDF gốc (`_source/pdf/`) | Vai trò bản Markdown (`doc/`) |
|---|---|---|
| `s_cr1000x.pdf` ↔ [`cr1000x-specifications.md`](doc/cr1000x-specifications.md) | Datasheet thông số kỹ thuật chính thức của Campbell Scientific, **Revision mới nhất (06/05/2026)**. Chỉ mở khi cần đối chiếu số liệu gốc hoặc bảng/hình không chuyển thể được. | **Nguồn tham chiếu chính (SSOT)** cho mọi câu hỏi về thông số kỹ thuật CR1000X (nguồn điện, kênh đo tương tự/xung, chức năng chân...) — luôn tra ở đây trước. |
| `CR1000x_datalogger_PW.pdf` ↔ [`cr1000x-datalogger-pw-2022.md`](doc/cr1000x-datalogger-pw-2022.md) | Cùng datasheet trên nhưng **Revision cũ hơn (22/04/2022)**, do nhà phân phối PowerWise Systems (Mỹ) phát hành lại. Giá trị chỉ mang tính lịch sử/đối chiếu nhà cung cấp. | Ghi lại phần **chênh lệch** giữa bản 2022 và bản 2026 đang dùng (ví dụ: bộ đếm xung 24-bit vs 32-bit, cảnh báo nguồn 16V). **Không phải nguồn chính** — không tra cứu thông số kỹ thuật ở đây, chỉ đọc khi cần biết lịch sử revision. |
| `cr1000x-getting-started-guide.pdf` ↔ [`cr1000x-getting-started-guide.md`](doc/cr1000x-getting-started-guide.md) | Hướng dẫn khởi động nhanh gốc của Campbell Scientific (19 trang). | Vai trò: **hướng dẫn thao tác** — cắm cáp USB, cài EZSetup, tạo chương trình CRBasic bằng Short Cut, kết nối LoggerNet/PC400. Dùng khi cần các bước cấu hình lần đầu, không phải tra thông số. |
| `cr1000x-product-manual.pdf` ↔ [`cr1000x-product-manual/00-catalog.md`](doc/cr1000x-product-manual/00-catalog.md) + 11 file chương (Tier A) + [`cr1000x-product-manual-full.md`](doc/cr1000x-product-manual/cr1000x-product-manual-full.md) (Tier B) | Cẩm nang sản phẩm đầy đủ (334 trang) — tài liệu kỹ thuật chi tiết nhất, ít khi cần mở trực tiếp. | Vai trò: **cẩm nang tra cứu sâu**, đã cắt theo 11 chuyên đề (đấu dây, lập trình đo, giao thức truyền thông...) để đọc trực tiếp theo nhu cầu; riêng `cr1000x-product-manual-full.md` là bản toàn văn dự phòng — **chỉ dùng grep**, không đọc nguyên file (quá lớn). |
| `5. BVTKTC_WOS.pdf` ↔ [`bvtktc-wos.md`](doc/bvtktc-wos.md) | Bản vẽ thiết kế thi công gốc (33 trang, dạng scan/CAD ảnh) — nguồn duy nhất chứa hình vẽ kỹ thuật thật (sơ đồ đấu nối, mặt bằng cột trạm). Hình đã trích riêng ra `doc/images/`. | Vai trò: **bản dịch nội dung kỹ thuật** của bộ bản vẽ (thông số cảm biến 05103/TB4/HygroVUE5/RAD06, kết cấu cột, lý trình 3 trạm WOS dự án Trung Lương – Mỹ Thuận dùng làm tham chiếu) sang văn bản đọc được — dùng thay PDF cho hầu hết câu hỏi, chỉ mở PDF khi cần xem đúng hình vẽ gốc. |
| `C2.TAP III.Q2.1.3` + `C2.TAP III.Q2.2.3 NTĐV ...pdf` (2 file) ↔ [`ho-so-nghiem-thu-wos.md`](doc/ho-so-nghiem-thu-wos.md) | Hồ sơ nghiệm thu vật tư/thiết bị đầu vào (NTĐV) gốc, dạng scan, **473 trang gộp chung cho 7 phân hệ ITS** (CCTV, VDS, VMS, DTS, WOS, TMS, PBX) — WOS chỉ là một phần nhỏ trong đó. | Vai trò: **bản trích lọc riêng phần WOS** từ bộ hồ sơ 473 trang — biên bản nghiệm thu, danh mục thiết bị đã nghiệm thu "Đạt", số lượng, model. Lưu ý đây là nghiệm thu **vật tư đầu vào** (đúng chủng loại/số lượng), không phải nghiệm thu chức năng. |

---

## 4. Hướng dẫn định tuyến tra cứu nhanh (Developer Quick Map)

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

## 5. Tier Table (Bảng phân loại tài liệu theo 3-Tier Context Budget)

| Tier | File | Read | Size | Nội dung | Nguồn gốc |
|---|---|---|---|---|---|
| **A** | [`doc/bvtktc-wos.md`](doc/bvtktc-wos.md) | full | — | Bản dịch nội dung Hồ sơ thiết kế bản vẽ thi công (BVTKTC) trạm WOS: thông số cảm biến 05103/TB4/HygroVUE5/RAD06, đấu nối, kết cấu cột, lý trình 3 trạm (dự án Trung Lương – Mỹ Thuận, tham chiếu) | `_source/pdf/5. BVTKTC_WOS.pdf` |
| **A** | [`doc/ho-so-nghiem-thu-wos.md`](doc/ho-so-nghiem-thu-wos.md) | full | — | Trích phần WOS từ hồ sơ nghiệm thu vật tư/thiết bị đầu vào (NTĐV) 7 phân hệ ITS — biên bản nghiệm thu "Đạt", danh mục/số lượng/model thiết bị | 2 file `_source/pdf/C2.TAP III.Q2.1.3` và `Q2.2.3 NTĐV ...pdf` |
| **A** | [`doc/cr1000x-specifications.md`](doc/cr1000x-specifications.md) | full | 14.2 KB | Thông số kỹ thuật chi tiết, yêu cầu nguồn, dải đo tương tự, xung, chức năng chân | `_source/pdf/s_cr1000x.pdf` |
| **A** | [`doc/cr1000x-datalogger-pw-2022.md`](doc/cr1000x-datalogger-pw-2022.md) | full | 4.5 KB | Ghi chú chênh lệch giữa bản datasheet Revision 2022 (PowerWise Systems) và Revision 2026 đang dùng — không có thông số mới, chỉ có giá trị tham khảo lịch sử/nhà phân phối | `_source/pdf/CR1000x_datalogger_PW.pdf` |
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
| **C** | `_source/pdf/s_cr1000x.pdf` | never | 1.04 MB | File PDF gốc bảng thông số kỹ thuật (8 trang, Revision 06/05/2026) | → bản `.md`: `doc/cr1000x-specifications.md` |
| **C** | `_source/pdf/CR1000x_datalogger_PW.pdf` | never | 0.37 MB | Cùng datasheet "CR1000X Specifications" nhưng Revision cũ hơn (7 trang, 22/04/2022), đóng dấu nhà phân phối PowerWise Systems (Maine, USA) | → bản `.md`: `doc/cr1000x-datalogger-pw-2022.md` |
| **C** | `_source/pdf/cr1000x-getting-started-guide.pdf` | never | 1.6 MB | File PDF gốc hướng dẫn khởi động nhanh (19 trang) | → bản `.md`: `doc/cr1000x-getting-started-guide.md` |
| **C** | `_source/pdf/cr1000x-product-manual.pdf` | never | 11.8 MB | File PDF gốc cẩm nang sản phẩm đầy đủ (334 trang) | → bản `.md`: `doc/cr1000x-product-manual/` |
| **C** | `_source/pdf/5. BVTKTC_WOS.pdf` | never | 25.15 MB | Bản vẽ thiết kế thi công trạm quan trắc thời tiết WOS (33 trang bản vẽ CAD/scan ảnh, không có text kỹ thuật số) | → bản `.md`: `doc/bvtktc-wos.md` |
| **C** | `_source/pdf/C2.TAP III.Q2.1.3 NTĐV CCTV,VDS,VMS,DTS,WOS,TMS,PBX.pdf` | never | 244.98 MB | Hồ sơ thiết kế kỹ thuật nghiệm thu đo vẽ Tập III Quyển 2.1.3 (284 trang bản vẽ scan tổng hợp 7 phân hệ) | → bản `.md`: `doc/ho-so-nghiem-thu-wos.md` |
| **C** | `_source/pdf/C2.TAP III.Q2.2.3 NTĐV CCTV,VDS,VMS,DTS,WOS,TMS,PBX.pdf` | never | 183.89 MB | Hồ sơ thiết kế kỹ thuật nghiệm thu đo vẽ Tập III Quyển 2.2.3 (189 trang bản vẽ scan tổng hợp 7 phân hệ) | → bản `.md`: `doc/ho-so-nghiem-thu-wos.md` |

---

## 6. Liên kết tài liệu trực tuyến (Online Resources)

- Trang chủ thiết bị: https://www.campbellsci.eu/cr1000x
- Cẩm nang trực tuyến (Campbell Scientific Web Help): https://help.campbellsci.com/CR1000X/Default.htm
- Hướng dẫn Short Cut & CRBasic: https://www.campbellsci.com/shortcut
