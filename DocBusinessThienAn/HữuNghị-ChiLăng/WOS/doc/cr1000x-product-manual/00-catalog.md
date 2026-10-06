---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 1-334
extracted: 2026-09-09
---

# CR1000X Product Manual — Danh mục & Chỉ mục tra cứu toàn bộ cẩm nang (00-catalog)

> **Tài liệu gốc**: `_source/pdf/cr1000x-product-manual.pdf` (Campbell Scientific CR1000X/CR1000Xe Product Manual, Revision: 05/2026, 334 trang).  
> **Phân loại**: Thư mục chuyên đề bao gồm 11 file chuyên đề **Tier A** (< 150 KB/file, đọc trực tiếp) và 1 file toàn văn **Tier B** (`cr1000x-product-manual-full.md`, grep-only).

---

## 1. Định hướng tra cứu nhanh cho Kỹ sư phân hệ WOS

Tùy theo tác vụ phát triển, kỹ sư hoặc AI tra cứu trực tiếp vào các file tương ứng sau:

| Nhiệm vụ kỹ thuật | File cần mở | Nội dung cốt lõi |
|---|---|---|
| **Đấu nối dây cảm biến (Gió, Mưa, Nhiệt ẩm)** | [`02-wiring-and-terminal-functions.md`](02-wiring-and-terminal-functions.md) | Sơ đồ cọc đấu chân, chân nguồn 12V/SW12, chân đếm xung P1/P2/C1-C8, chân 4-20mA RG1/RG2 |
| **Kỹ thuật đo cảm biến thời tiết** | [`07-measurements.md`](07-measurements.md) | Đo xung mưa thùng lật TB4, đo điện áp gió 05103, đo dòng loop 4-20mA, bù nhiệt lạnh CJC |
| **Tích hợp giao thức Modbus & SDI-12** | [`08-communications-protocols.md`](08-communications-protocols.md) | Bảng thanh ghi Modbus RTU/TCP, tập lệnh SDI-12, thiết lập cổng nối tiếp RS-232/RS-485 |
| **Cấu hình mạng IP, Web, EZSetup** | [`03-setup-and-communications.md`](03-setup-and-communications.md) | Cài đặt địa chỉ IP, kết nối USB/RNDIS, nạp chương trình Short Cut qua mạng |
| **Bảo mật mạng, Tài khoản & TLS** | [`04-pre-installation-and-security.md`](04-pre-installation-and-security.md) | Mã định danh UID, chứng chỉ số TLS, mở/khóa các cổng dịch vụ HTTP/FTP/Telnet/Ping |
| **Cấu trúc bảng dữ liệu & Định dạng số liệu** | [`06-data-tables-and-memory.md`](06-data-tables-and-memory.md) | Định dạng TOA5, bảng thời gian thực, lưu trữ trên Flash và thẻ nhớ ngoài MicroSD |
| **Chẩn đoán sự cố, Báo lỗi & Lọc nhiễu** | [`09-maintenance-and-troubleshooting.md`](09-maintenance-and-troubleshooting.md) | Mã lỗi Watchdog, trễ quét SkippedScan, trị số rỗng NAN/INF, triệt vòng lặp mass |
| **Tra cứu thanh ghi Status & Settings** | [`10-settings-and-status-tables.md`](10-settings-and-status-tables.md) | Bảng tra cứu toàn bộ biến Status và cấu hình hệ thống Settings nâng cao |
| **Thông số chi tiết & Từ điển thuật ngữ** | [`11-specifications-and-glossary.md`](11-specifications-and-glossary.md) | Thông số mở rộng CR1000Xe/CR1000X và từ điển thuật ngữ chuyên ngành A–Z |

---

## 2. Danh mục chi tiết các chương (Table of Chapters)

| STT | File | Chương tương ứng trong PDF | Trang PDF | Dung lượng |
|---|---|---|---|---|
| 01 | [`01-introduction-and-components.md`](01-introduction-and-components.md) | Chương 1: Introduction<br>Chương 2: Precautions<br>Chương 3: Initial inspection<br>Chương 4: Components | 17–23 | ~15 KB |
| 02 | [`02-wiring-and-terminal-functions.md`](02-wiring-and-terminal-functions.md) | Chương 5: Wiring panel and terminal functions | 24–38 | ~35 KB |
| 03 | [`03-setup-and-communications.md`](03-setup-and-communications.md) | Chương 6: Setting up the CR1000X/CR1000Xe | 39–60 | ~45 KB |
| 04 | [`04-pre-installation-and-security.md`](04-pre-installation-and-security.md) | Chương 7: Pre-installation & Security | 61–82 | ~50 KB |
| 05 | [`05-installation-and-cloud.md`](05-installation-and-cloud.md) | Chương 8: Installation<br>Chương 9: CampbellCloud onboarding | 83–91 | ~20 KB |
| 06 | [`06-data-tables-and-memory.md`](06-data-tables-and-memory.md) | Chương 10: Working with data<br>Chương 11: Data memory | 92–114 | ~45 KB |
| 07 | [`07-measurements.md`](07-measurements.md) | Chương 12: Measurements | 115–138 | ~60 KB |
| 08 | [`08-communications-protocols.md`](08-communications-protocols.md) | Chương 13: Communications protocols | 139–167 | ~70 KB |
| 09 | [`09-maintenance-and-troubleshooting.md`](09-maintenance-and-troubleshooting.md) | Chương 14: Maintenance<br>Chương 15: Tips and troubleshooting | 168–217 | ~110 KB |
| 10 | [`10-settings-and-status-tables.md`](10-settings-and-status-tables.md) | Chương 16: Information tables and settings (advanced) | 218–253 | ~85 KB |
| 11 | [`11-specifications-and-glossary.md`](11-specifications-and-glossary.md) | Chương 17: CR1000Xe specs<br>Chương 18: CR1000X specs<br>Phụ lục A: Glossary | 254–334 | ~135 KB |
| Full | [`cr1000x-product-manual-full.md`](cr1000x-product-manual-full.md) | Toàn bộ 334 trang (Tier B, dùng cho `grep_search`) | 1–334 | ~800 KB |
