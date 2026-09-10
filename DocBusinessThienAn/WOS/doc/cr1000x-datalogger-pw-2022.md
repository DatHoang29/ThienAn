---
tier: A
read: full
source: _source/pdf/CR1000x_datalogger_PW.pdf
source_pages: 1-7
extracted: 2026-09-10
---

# Campbell Scientific CR1000X — "PW" datasheet (Revision 04/2022, bản PowerWise Systems)

> **Nguồn**: `_source/pdf/CR1000x_datalogger_PW.pdf` — cùng tài liệu **"CR1000X Specifications"** của Campbell Scientific như `_source/pdf/s_cr1000x.pdf` (đã chuyển thể đầy đủ tại [`cr1000x-specifications.md`](cr1000x-specifications.md)), nhưng là **bản Revision cũ hơn — 22/04/2022** (7 trang) thay vì Revision 06/05/2026 (8 trang) đang dùng làm nguồn chính.
>
> Trang cuối file này đóng dấu liên hệ nhà phân phối **PowerWise Systems** (124 Main Street, Bucksport, ME 04416, USA; www.powerwisesystems.com; +1 207 370 6517; sales@powerwisesystems.com) thay vì khối "Regional Offices" toàn cầu của Campbell Scientific — đây là bản in lại/tái bản của **PowerWise Systems với vai trò nhà phân phối/đại lý thiết bị Campbell Scientific tại Mỹ** (chữ "PW" trong tên file = PowerWise). Cần đối chiếu xem PowerWise Systems có phải là nhà cung cấp trong chuỗi mua sắm thiết bị WOS của dự án hay không (hồ sơ nghiệm thu hiện có — `ho-so-nghiem-thu-wos.md` — không nhắc tên công ty này).

## Kết luận: không có thông số kỹ thuật mới

Đã đối chiếu toàn bộ 7 trang của file này với 8 trang của `s_cr1000x.pdf` (nguồn của `cr1000x-specifications.md`). Nội dung **giống hệt nhau** (cùng tiêu đề, cùng mục lục, cùng producer "madbuild" của Campbell Scientific) — chỉ khác nhau ở phần đã được **cập nhật/sửa giữa 2 lần phát hành**, liệt kê đầy đủ bên dưới. Vì bản 2026 mới hơn và đã là nguồn của tài liệu Tier A hiện có, file PW **không bổ sung thông số kỹ thuật nào mới** cho việc tích hợp phần mềm.

## Các thay đổi giữa Revision 2022 (file PW) và Revision 2026 (tài liệu hiện dùng)

| # | Nội dung | Bản 2022 (PW) | Bản 2026 (hiện dùng) | Ảnh hưởng |
|---|---|---|---|---|
| 1 | **Cảnh báo nguồn cấp cảm biến qua 12V/SW12/CS I/O** | Không có | Có thêm khối NOTE: *"To prevent voltage input issues with sensors and peripherals, do not use more than 16 V when powering them through the 12V, SW12-1, SW12-2, or CS I/O port on the data logger."* | An toàn thiết kế nguồn cấp cảm biến hiện trường — **hiện KHÔNG có trong `cr1000x-specifications.md`** (transcription bỏ sót, xem mục Errata bên dưới). |
| 2 | **Độ rộng bộ đếm xung (P1/P2/C1-C8)** | "Each terminal has its own independent **32-bit counter**" | "Each terminal has its own independent **24-bit counter**" (Maximum Counts Per Scan: 2²⁴) | Bản 2022 lỗi thời/không chính xác — con số **24-bit đã đúng** và khớp với `cr1000x-product-manual/11-specifications-and-glossary.md:869-871` đã có trong hệ thống tài liệu. Không cần sửa gì ở plan tích hợp (`WosRegisterAddress`/pulse-count logic nên dùng 24-bit, không phải 32-bit). |
| 3 | **Chân đo tần số cao (High Frequency input)** | Chỉ liệt kê `Terminals: C1-C8` | Mở rộng thành `Terminals: C1-C8, P1-P2` | Bản 2026 làm rõ P1/P2 cũng đo được high-frequency pulse, không chỉ switch-closure/low-level AC — khớp với `07-measurements.md` đã ghi nhận trước đó. |
| 4 | **Cảnh báo RS-232 trên ComC1/ComC3** | Không có | Có thêm khối WARNING: ComC1/ComC3 trên CR1000X (khác CR1000Xe) **không tương thích RS-232**, cắm lâu dài (vd modem RV50(X)) có thể **làm hỏng datalogger**; chỉ ComC5/ComC7 an toàn cho RS-232 trên CR1000X | **Đã có sẵn** trong `cr1000x-product-manual/02-wiring-and-terminal-functions.md:142-154` — không phải thông tin mới, chỉ là bản spec-sheet ngắn (7 trang) được bổ sung cảnh báo này muộn hơn cẩm nang đầy đủ. |
| 5 | **Ký hiệu chân SPI** | `MOSI` / `MISO` | `COPI` / `CIPO` (thuật ngữ SPI mới thay MOSI/MISO) | Đổi tên thuật ngữ, không đổi chức năng phần cứng. |
| 6 | **Modbus/DNP3** | "DNP3" (không ghi rõ vai trò) | "DNP3 **outstation**" (ghi rõ chỉ đóng vai trò slave) | Làm rõ hơn, khớp với kết luận tích hợp đã chốt (CR1000X là Modbus/DNP3 server/slave, backend là client). |

## Errata phát hiện trong `cr1000x-specifications.md` (đã sửa)

Khi đối chiếu cả 2 bản PDF gốc với bản `.md` đã chuyển thể, phát hiện 2 lỗi transcription (không liên quan đến sự khác biệt revision — cả 2 bản PDF đều thống nhất, chỉ riêng bản `.md` bị sai):

1. **CPU**: `.md` ghi "32-bit ARM Cortex-A8" — **sai**. Nguyên văn cả 2 PDF: *"Processor: Renesas RX63N (32-bit with hardware FPU, running at 100 MHz)"*. Đã sửa thành "Renesas RX63N".
2. **Dung lượng thẻ nhớ mở rộng**: `.md` ghi "lên đến 32 GB" — **sai**. Nguyên văn cả 2 PDF: *"Data storage expansion: Removable microSD flash memory, up to 16 GB"*. Đã sửa thành "16 GB".

## Không cần chuyển thể toàn văn

Vì nội dung trùng ~95% với `cr1000x-specifications.md` (đã Tier A, đầy đủ), file này chỉ ghi lại phần **chênh lệch** thay vì lặp lại toàn bộ 7 trang. Khi cần tra thông số chi tiết (nguồn điện, đo tương tự, đo xung, bảng chân...), dùng `cr1000x-specifications.md` làm nguồn chính.
