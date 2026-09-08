---
tier: A
read: full
source: _source/img/1WUpuklkneHhPEFDdv0FV4AhHXWfmyWG6g361CUyP2VARkDsVwnCxuzMc2MkxEpJHUO.jpg
source_pages: 1
extracted: 2026-08-20
---

# SƠ ĐỒ CẤU HÌNH VIDEO WALL DS-C66S-H88-CL

**Video wall 8 × 4 = 32 màn hình 55" (1920 × 1080)**

> Nguồn: `1WUpuklkneHhPEFDdv0FV4AhHXWfmyWG6g361CUyP2VARkDsVwnCxuzMc2MkxEpJHUO.jpg`

---

## 1. Bố trí màn hình (Video Wall)

- Lưới **8 cột × 4 hàng = 32 màn hình**, mỗi màn **55" / 1920 × 1080**.

### Chú thích vùng hiển thị

| Ký hiệu | Vùng | Ý nghĩa |
|---|---|---|
| 🟦 Xanh | ITS / MAP | 01 cửa sổ |
| 🟧 Cam | CCTV | 20 cửa sổ / 20 camera |

### Ghi chú bố trí

- **Vùng ITS/MAP:** 6 cột × 2 hàng = 12 màn (khối giữa của video wall).
- **Vùng CCTV:** 20 màn = 20 camera (viền quanh khối ITS/MAP).

---

## 2. Phân vùng điều khiển

Video wall được chia thành **3 khu vực điều khiển**, mỗi khu vực do một **bộ điều khiển con DS-C66S-H88-CL** phụ trách.

### KHU VỰC ĐIỀU KHIỂN 1 — 4 cột × 4 hàng = 16 màn

**Bộ điều khiển con 1 – DS-C66S-H88-CL**

| Thành phần | Số lượng | Kết quả |
|---|---|---|
| DS-C66S-02HI/4K | 2 | 4 input 4K |
| DS-C66S-04HO | 4 | 16 output FHD |

- Phụ trách: 4 cột × 4 hàng = 16 màn
- Luồng: **4 × 4K in / 16 × FHD out**

### KHU VỰC ĐIỀU KHIỂN 2 — 2 cột × 4 hàng = 8 màn

**Bộ điều khiển con 2 – DS-C66S-H88-CL**

| Thành phần | Số lượng | Kết quả |
|---|---|---|
| DS-C66S-02HI/4K | 1 | 2 input 4K |
| DS-C66S-04HO | 2 | 8 output FHD |

- Phụ trách: 2 cột × 4 hàng = 8 màn
- Luồng: **2 × 4K in / 8 × FHD out**

### KHU VỰC ĐIỀU KHIỂN 3 — 2 cột × 4 hàng = 8 màn

**Bộ điều khiển con 3 – DS-C66S-H88-CL**

| Thành phần | Số lượng | Kết quả |
|---|---|---|
| DS-C66S-02HI/4K | 1 | 2 input 4K |
| DS-C66S-04HO | 2 | 8 output FHD |

- Phụ trách: 2 cột × 4 hàng = 8 màn
- Luồng: **2 × 4K in / 8 × FHD out**

---

## 3. Bộ điều khiển trung tâm — DS-C66S-H88-CL

**Chức năng:** Quản lý nguồn HDMI / LAN–Web, hiển thị phần mềm ITS, tổng hợp 20 camera, chia 8 luồng 4K, quản lý scene.

### Nguồn vào / Cấu hình trung tâm (qua HDMI / LAN / Web)

- ITS Software / Workstation
- 20 camera qua NVR / VMS
- Nguồn ITS local: HDMI 4K
- Nguồn CCTV: HDMI từ NVR/VMS hoặc ma trận
- Cấu hình & vận hành qua Web Interface / LAN RJ45

### Card trung tâm

- 4 × DS-C66S-02HO/4K = 8 output 4K
- Card input HDMI: DS-C66S-04HI / DS-C66S-02HI/4K (theo nguồn thực tế)

### Đầu ra bộ trung tâm

| Đến | Số luồng |
|---|---|
| Bộ điều khiển con 1 | 4 × 4K OUT |
| Bộ điều khiển con 2 | 2 × 4K OUT |
| Bộ điều khiển con 3 | 2 × 4K OUT |
| **Tổng cộng từ H88-CL** | **8 × 4K OUT** |

---

## 4. Tổng cấu hình thiết bị

| Thiết bị | Số lượng |
|---|---|
| DS-C66S-H88-CL | 4 |
| DS-C66S-PWR | 4 |
| DS-C66S-04HO | 8 |
| DS-C66S-02HO/4K | 4 |
| DS-C66S-02HI/4K | 4 |
| DS-C66S-04HI | 8 |
| Switch mạng Gigabit | 01 |

---

## 5. Tổng hợp Card / Cáp / Phụ kiện

### Card

| Card | Số lượng |
|---|---|
| DS-C66S-PWR | 4 bộ |
| DS-C66S-04HO | 8 card |
| DS-C66S-02HO/4K | 4 card |
| DS-C66S-02HI/4K | 4 card |
| DS-C66S-04HI | 8 card |

### Cáp HDMI

- **32 sợi HDMI FHD:** từ 3 bộ H88-CL con ra 32 màn hình 55".
- **8 sợi HDMI 4K:** từ bộ H88-CL trung tâm đến 3 bộ H88-CL con.
- **HDMI nguồn vào ITS / NVR / VMS:** theo cấu hình thực tế.

### Cáp mạng

- 4 sợi Cat6/Cat6A RJ45 quản lý cho 4 bộ H88-CL.
- 01 uplink từ switch tới mạng quản lý.

### Phụ kiện

- 4 dây nguồn AC cho 4 bộ H88-CL.
- 32 dây nguồn cho 32 màn hình.
- 01 switch mạng Gigabit.
- Patch cord / đấu nối / nhãn cáp / thanh nguồn PDU / tủ rack (nếu lắp tủ).

---

## 6. Luồng tín hiệu

```
ITS Software + 20 camera / NVR
        │
        ▼
Switch LAN / nguồn HDMI
        │
        ▼
DS-C66S-H88-CL  ──(chia 8 luồng 4K)──►  3 bộ H88-CL con  ──►  Xuất ra 32 màn FHD
```

**Ghi chú:** Camera được tổng hợp qua NVR/VMS hoặc nguồn HDMI đưa vào H88-CL.
