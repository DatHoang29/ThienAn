---
tier: A
read: full
source: _source/pdf/s_cr1000x.pdf
source_pages: 1-8
extracted: 2026-09-09
---

# Campbell Scientific CR1000X — Specifications (Bảng thông số kỹ thuật chi tiết)

> **Nguồn**: `_source/pdf/s_cr1000x.pdf` (CR1000X Specifications, Campbell Scientific, Revision: 05/2026).  
> **Mục tiêu phân hệ WOS**: Tích hợp thu thập dữ liệu quan trắc thời tiết (Nhiệt độ, độ ẩm, lượng mưa qua cảm biến TB4, tốc độ và hướng gió qua cảm biến 05103).

---

## 1. Điều kiện hoạt động chung (Operating Environment)

- **Môi trường hoạt động tiêu chuẩn**: -40 °C đến +70 °C, không ngưng tụ.
- **Môi trường mở rộng (ký hiệu XT)**: -55 °C đến +85 °C, không ngưng tụ.
- **Khuyến nghị hiệu chuẩn**: Định kỳ 3 năm/lần.

---

## 2. Thông số hệ thống (System Specifications)

- **Bộ xử lý (CPU)**: Renesas RX63N (32-bit, có hardware FPU) chạy ở tốc độ 100 MHz.
- **Bộ nhớ (Memory)**:
  - Bộ nhớ chương trình (SRAM): 4 MB chạy chương trình và bảng dữ liệu.
  - Bộ nhớ lưu trữ dữ liệu (Flash): 128 MB flash dành cho hệ điều hành, cấu hình, và dữ liệu backup.
  - Hỗ trợ thẻ nhớ ngoài: Khe cắm MicroSD (CRD: drive) hỗ trợ lên đến 16 GB (FAT32).
- **Hệ điều hành**: Pre-emptive multi-tasking OS, hỗ trợ ngôn ngữ lập trình CRBasic.
- **Độ chính xác xung nhịp (Clock Accuracy)**:
  - Chuẩn: ±3 phút/năm (-40 °C đến +70 °C).
  - Có GPS đồng bộ: Độ chính xác theo chuẩn thời gian thực GPS (microseconds).
  - Đồng bộ mạng: Hỗ trợ giao thức NTP (Network Time Protocol).

---

## 3. Thông số vật lý (Physical Specifications)

- **Kích thước**: 23.8 cm × 10.1 cm × 6.2 cm (9.4 in × 4.0 in × 2.4 in).
- **Trọng lượng**: Khoảng 0.86 kg (1.9 lb).
- **Vỏ bọc (Case Material)**: Hợp kim nhôm phủ tĩnh điện (Anodized aluminum), chống nhiễu điện từ EMI/RFI.
- **Kiểu cầu đấu (Terminal Strips)**: Cầu đấu tháo rời (removable terminal blocks), vít siết, khoảng cách chân 3.81 mm.

---

## 4. Yêu cầu nguồn điện (Power Requirements)

- **Nguồn vào chính (POWER IN terminals)**:
  - Điện áp danh định: 12 VDC.
  - Dải điện áp hoạt động: 10 VDC đến 18 VDC.
  - Bảo vệ: Chống phân cực ngược (Reverse polarity protection) và chống quá áp/sụt áp.
- **Nguồn nuôi qua cổng USB (USB Power)**:
  - Khi cắm USB (5 VDC): Data logger có thể nạp chương trình, cấu hình và đo kiểm cơ bản.
  - *Lưu ý*: Cổng CS I/O và các chân 5V, 12V, SW12 sẽ không hoạt động nếu chỉ dùng nguồn USB.
- **Pin nuôi bộ nhớ và xung nhịp (Internal Lithium Battery)**:
  - Loại pin: AA, 2.4 Ah, 3.6 VDC (Tadiran TL 5903/S).
  - Tuổi thọ: 3 năm khi không có nguồn điện ngoài.
- **Dòng tiêu thụ trung bình (Average Current Drain at 12 VDC)**:
  - Chế độ nghỉ (Idle): < 1 mA (điển hình).
  - Quét chủ động chu kỳ 1 Hz (Active 1 Hz Scan): 1 mA.
  - Quét chủ động chu kỳ 20 Hz (Active 20 Hz Scan): 55 mA.
  - Khi bật cổng nối tiếp (RS-232 / RS-485): Cộng thêm 25 mA.
  - Yêu cầu nguồn Ethernet:
    - Chế độ 1 phút (Ethernet 1 Minute): Cộng thêm 1 mA.
    - Chế độ chờ (Ethernet Idle): Cộng thêm 4 mA.
    - Chế độ kết nối truyền tin (Ethernet Link): Cộng thêm 47 mA.

---

## 5. Thông số nguồn ra (Power Output Specifications)

### 5.1 Giới hạn công suất ra toàn hệ thống (System Power Out Limits @ 12 VDC)

| Nhiệt độ (°C) | Dòng cấp tối đa (A) |
|---|---|
| -40 °C | 4.53 A |
| +20 °C | 3.00 A |
| +70 °C | 1.83 A |
| +85 °C | 1.56 A |

### 5.2 Cổng nguồn ra 12 V và SW12 (Switched 12V)

- **Chân 12V**: Nguồn 12 V không ngắt (unswitched), lấy trực tiếp từ nguồn chính qua cầu chì tự phục hồi.
- **Chân SW12-1 và SW12-2**: Hai kênh nguồn 12 V có thể điều khiển bật/tắt độc lập bằng lệnh phần mềm CRBasic (`SW12()`).
- **Giới hạn dòng cho SW12**:

| Nhiệt độ (°C) | Dòng cấp tối đa SW12 (mA) |
|---|---|
| -40 °C | 1310 mA |
| 0 °C | 1004 mA |
| +20 °C | 900 mA |
| +50 °C | 690 mA |
| +70 °C | 550 mA |
| +80 °C | 470 mA |

### 5.3 Nguồn 5 V cố định (5 V Fixed Output)

- Điện áp ra: 5 VDC (±5%).
- Dòng cấp tối đa: 230 mA (bảo vệ bằng cầu chì tự phục hồi).

### 5.4 Chân C (C Terminals as Power Output)

- Chân C1–C8 có thể cấu hình xuất mức logic cao 5 V hoặc 3.3 V với dòng tối đa:
  - Nguồn 5 V: Cấp tối đa 20 mA mỗi chân.
  - Nguồn 3.3 V: Cấp tối đa 10 mA mỗi chân.

### 5.5 Kích thích điện áp (Voltage Excitation VX1–VX4)

- Số kênh: 4 kênh độc lập (VX1, VX2, VX3, VX4).
- Dải điện áp: ±4 VDC.
- Độ phân giải: 0.06 mV.
- Độ chính xác: ±(0.1% giá trị cài đặt + 2 mV).
- Dòng cấp tối đa (Source/Sink): ±40 mA mỗi kênh.

---

## 6. Thông số đo tương tự (Analog Measurement Specifications)

### 6.1 Đo điện áp (Voltage Measurements)

- **Số kênh**:
  - 16 kênh đơn cực (Single-Ended: 1 đến 16).
  - Hoặc 8 kênh vi sai (Differential: 1 đến 8, gồm 2 chân H và L).
- **Bộ chuyển đổi ADC**: 24-bit ADC với bộ lọc số tích hợp.
- **Trở kháng đầu vào**: 20 GΩ (điển hình).
- **Dải điện áp và độ phân giải**:

| Tần số lọc (fN1) (Hz) | Dải đo (Range) (mV) | Vi sai đảo cực: RMS (µV) | Bits | Đơn cực / Vi sai không đảo: RMS (µV) | Bits |
|---|---|---|---|---|---|
| 15,000 Hz | ±5000 mV | 8.2 µV | 20 bit | 11.8 µV | 19 bit |
| 15,000 Hz | ±1000 mV | 1.9 µV | 20 bit | 2.6 µV | 19 bit |
| 15,000 Hz | ±200 mV | 0.75 µV | 19 bit | 1.0 µV | 18 bit |
| 50/60 Hz (chuẩn) | ±5000 mV | 0.6 µV | 24 bit | 0.88 µV | 23 bit |
| 50/60 Hz (chuẩn) | ±1000 mV | 0.14 µV | 23 bit | 0.2 µV | 23 bit |
| 50/60 Hz (chuẩn) | ±200 mV | 0.05 µV | 22 bit | 0.08 µV | 22 bit |
| 5 Hz | ±5000 mV | 0.18 µV | 25 bit | 0.28 µV | 25 bit |
| 5 Hz | ±1000 mV | 0.04 µV | 25 bit | 0.07 µV | 24 bit |
| 5 Hz | ±200 mV | 0.02 µV | 24 bit | 0.03 µV | 23 bit |

- **Độ chính xác đo điện áp**:
  - Trong dải 0 đến +40 °C: ±(0.04% số đo + offset).
  - Trong dải -40 đến +70 °C: ±(0.06% số đo + offset).
  - Mở rộng -55 đến +85 °C (XT): ±(0.08% số đo + offset).
- **Hệ số triệt nhiễu đồng pha (CMRR)**: > 100 dB tại 50/60 Hz.
- **Hệ số triệt nhiễu thường pha (NMRR)**: > 70 dB tại 50/60 Hz.

### 6.2 Đo vòng lặp dòng điện (Current-Loop Measurements 4–20 mA)

- **Chân chuyên dụng**: Chân **RG1** và **RG2** (Kênh dòng vào).
- **Trở kháng shunt nội**: 100 Ω tích hợp sẵn (tương thích trực tiếp với cảm biến chuẩn công nghiệp 0–20 mA hoặc 4–20 mA mà không cần điện trở ngoài).
- **Dải đo**: 0 đến 20 mA (tối đa chịu được 30 mA liên tục).
- **Độ phân giải**: 0.05 µA (tại tần số lọc 50/60 Hz).
- **Độ chính xác**: ±(0.05% số đo + 1 µA).

### 6.3 Đo điện trở & Cảm biến nhiệt độ (Resistance, RTD, Thermocouple)

- **Cầu đo ratiometric**: Hỗ trợ 4-wire, 3-wire, 2-wire full/half bridge cho cảm biến nhiệt độ PT100, PT1000, Thermistor.
- **Đo nhiệt điện ngẫu (Thermocouple)**: Tích hợp cảm biến đo nhiệt độ cầu đấu nội (Internal Panel Temperature) để bù nhiệt điểm nối lạnh (Cold Junction Compensation - CJC).

---

## 7. Thông số đo xung (Pulse Measurement Specifications)

Hỗ trợ đếm xung đo tốc độ gió (anemometer) và đo lượng mưa (tipping bucket rain gauge).

- **Kênh đo xung chuyên dụng (P1, P2)**:
  - **Chế độ High Frequency**: Tần số lên đến 250 kHz (sóng vuông) hoặc 100 kHz (mức logic).
  - **Chế độ Switch Closure**: Tần số tối đa 150 Hz, có mạch lọc chống rung tiếp điểm (de-bounce filter) với thời gian trễ 1.2 ms — **dùng cho cảm biến đo mưa thùng lật TB4**.
  - **Chế độ Low-level AC**: Tín hiệu xoay chiều biên độ nhỏ từ 20 mV đến 20 V RMS (tần số 0.5 Hz đến 200 kHz) — **dùng cho cảm biến tốc độ gió cuộn dây phát điện (VD: bộ phát xung tốc độ gió Campbell 05103)**.
- **Kênh đo xung trên chân điều khiển C1–C8**:
  - Hỗ trợ switch-closure (≤ 150 Hz) và high-frequency (≤ 2.3 kHz).
  - Đo cạnh xung (Edge timing): Độ phân giải 500 ns (≤ 1 kHz).
  - Đếm cạnh xung (Edge counting): ≤ 2.3 kHz.
  - Đầu vào vuông góc (Quadrature input): ≤ 2.5 kHz (cho encoder / cảm biến vị trí).

---

## 8. Cổng giao tiếp & Chuẩn truyền thông (Communications Specifications)

CR1000X hỗ trợ hệ thống cổng truyền thông công nghiệp đa dạng:

### 8.1 Cổng Ethernet

- **Cổng kết nối**: RJ45 tiêu chuẩn, 10/100 Mbps (Auto-MDIX).
- **Bảo vệ**: Cách ly từ tính (Magnetic isolation) và bảo vệ chống đột biến điện TVS surge protection.
- **Giao thức Internet hỗ trợ**:
  - Truyền dữ liệu: TCP/IP, UDP, HTTP, HTTPS, FTP, SFTP, MQTT.
  - Quản trị & giám sát: Modbus TCP/IP (Client/Server), DNP3, PakBus TCP, Telnet, Ping/ICMP.
  - Dịch vụ mạng: DHCP, DNS, SLAAC, NTP đồng bộ giờ, TLS v1.2 mã hóa.

### 8.2 Cổng USB

- Cổng micro-USB client dùng để cấu hình trực tiếp từ máy tính qua phần mềm **Device Configuration Utility** hoặc **PC400 / LoggerNet**.
- Hỗ trợ chuẩn Virtual Ethernet over USB (RNDIS) để truy cập web interface trực tiếp qua địa chỉ IP ảo (192.168.86.1).

### 8.3 Cổng nối tiếp công nghiệp (C1–C8 Terminals)

Các chân C1 đến C8 có thể cấu hình linh hoạt thành các cổng truyền thông:
- **SDI-12 Ports**: C1, C3, C5, C7 (tuân thủ đặc tả SDI-12 v1.4, hỗ trợ chế độ cảm biến thông minh trạm thời tiết).
- **RS-485 (Half Duplex)**: Tạo thành các cặp truyền thông RS-485 hai dây (A-, B+).
- **RS-485 / RS-422 (Full Duplex)**: 4 dây (Tx-, Tx+, Rx-, Rx+).
- **TTL / LVTTL Serial**: Hỗ trợ tốc độ baud từ 300 đến 115,200 bps.
- **Giao tiếp I2C & SPI**: Tích hợp giao tiếp bus vi mạch (SCL, SDA, SCLK, COPI, CIPO).

### 8.4 Cổng RS-232 / CPI Port

- Cổng RJ12 hoặc cổng phụ RS-232/CPI để giao tiếp mở rộng module CDM/CPI (Campbell Distributed Modules) với tốc độ bus cực cao (lên tới 1 Mbps).

### 8.5 Cổng CS I/O

- Cổng kết nối chuẩn của Campbell Scientific 9-pin để nối các modem vệ tinh, modem 4G cellular, hoặc màn hình hiển thị cầm tay (keyboard display).

---

## 9. Bảng tra cứu chức năng chân đấu nối (Terminal Functions)

### 9.1 Chức năng chân đầu vào tương tự (Analog Inputs)

| Chân trên cầu đấu | Ký hiệu | Đo điện áp đơn cực (SE) | Đo điện áp vi sai (DIFF) | Cầu đo / Cảm biến nhiệt | Đo cặp nhiệt ngẫu (TC) | Đo vòng lặp dòng (Current Loop) | Đo chu kỳ trung bình (Period Average) |
|---|---|---|---|---|---|---|---|
| Chân 1 & 2 | 1H, 1L | SE 1, SE 2 | DIFF 1 (H, L) | ✓ | ✓ | — | ✓ |
| Chân 3 & 4 | 2H, 2L | SE 3, SE 4 | DIFF 2 (H, L) | ✓ | ✓ | — | ✓ |
| Chân 5 & 6 | 3H, 3L | SE 5, SE 6 | DIFF 3 (H, L) | ✓ | ✓ | — | ✓ |
| Chân 7 & 8 | 4H, 4L | SE 7, SE 8 | DIFF 4 (H, L) | ✓ | ✓ | — | ✓ |
| Chân 9 & 10 | 5H, 5L | SE 9, SE 10 | DIFF 5 (H, L) | ✓ | ✓ | — | ✓ |
| Chân 11 & 12 | 6H, 6L | SE 11, SE 12 | DIFF 6 (H, L) | ✓ | ✓ | — | ✓ |
| Chân 13 & 14 | 7H, 7L | SE 13, SE 14 | DIFF 7 (H, L) | ✓ | ✓ | — | ✓ |
| Chân 15 & 16 | 8H, 8L | SE 15, SE 16 | DIFF 8 (H, L) | ✓ | ✓ | — | ✓ |
| RG1 & RG2 | RG1, RG2 | — | — | — | — | **4–20 mA (Kênh 1, Kênh 2)** | — |

*Lưu ý tiếp địa*: Cứ mỗi cặp kênh tương tự có một chân tiếp địa tương tự **⏚ (Ground)** xen kẽ để chống nhiễu chéo.

### 9.2 Chức năng chân đếm xung (Pulse Terminals)

| Loại xung | Chân P1 | Chân P2 | Chân C1–C8 | Ứng dụng trạm khí tượng |
|---|---|---|---|---|
| **Low-level AC** | ✓ | ✓ | — | Cảm biến phát xung tốc độ gió (cuộn dây xoay chiều như bộ 05103) |
| **Switch-Closure** | ✓ | ✓ | ✓ | Cảm biến đo lượng mưa dạng thùng lật (TB4 tipping bucket) |
| **High Frequency** | ✓ | ✓ | ✓ | Bộ mã hóa xung tốc độ cao |

### 9.3 Chức năng cổng truyền thông số (Communications Ports)

| Chân cầu đấu | RS-485 (Half Duplex) | RS-485 / RS-422 (Full Duplex) | SDI-12 | I2C | SPI | Ứng dụng |
|---|---|---|---|---|---|---|
| **C1** | A- (Port 1) | Tx- | **SDI-12 Data** | SCL | SCLK | Cảm biến SDI-12 / Modbus RTU |
| **C2** | B+ (Port 1) | Tx+ | — | SDA | COPI | Modbus RTU B+ |
| **C3** | A- (Port 2) | Rx- | **SDI-12 Data** | SCL | CIPO | Cảm biến SDI-12 kênh 2 |
| **C4** | B+ (Port 2) | Rx+ | — | SDA | — | Modbus RTU kênh 2 |
| **C5** | A- (Port 3) | Tx- | **SDI-12 Data** | SCL | SCLK | Cảm biến độ ẩm/nhiệt SDI-12 |
| **C6** | B+ (Port 3) | Tx+ | — | SDA | COPI | — |
| **C7** | A- (Port 4) | Rx- | **SDI-12 Data** | SCL | CIPO | Cảm biến áp suất/gió SDI-12 |
| **C8** | B+ (Port 4) | Rx+ | — | SDA | — | — |

---

## 10. Tiêu chuẩn tuân thủ & An toàn (Compliance & Certifications)

- **Tiêu chuẩn an toàn**: CE, UKCA, RCM, ICES-003, FCC Part 15 Class A.
- **Tiêu chuẩn chống sốc & rung**: MIL-STD-810G Method 516.6 (Shock), Method 514.6 (Vibration).
- **Bảo vệ xả tĩnh điện (ESD Protection)**: Tuân thủ IEC 61000-4-2 (±8 kV tiếp xúc, ±15 kV không khí).
- **Chống sét & xung áp lan truyền (Surge Protection)**: Tuân thủ IEC 61000-4-5 cho tất cả các cổng tương tự và truyền thông.
