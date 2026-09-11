# 🎯 Central Meeting Hub — Trung tâm Biên bản Họp, Script & Ghi âm

> **Dự án:** Cao tốc Hữu Nghị – Chi Lăng (HN-CL)  
> **Mục đích:** Bảng điều khiển tập trung **duy nhất** (Single Source of Truth) quản lý toàn bộ biên bản họp, script chuyển thể và file ghi âm gốc của các phân hệ (**ShareData**, **VideoWall**, **Plan toàn tuyến**).  
> **Quy tắc lưu trữ:**  
> - **Tier A (AI & Đọc trực tiếp):** Bản script/transcript định dạng `.md` nằm trong `doc/transcript/` của từng phân hệ.  
> - **Tier C (Human-only & Lưu trữ gốc):** File ghi âm `.m4a` nằm trong `_source/audio/`.

---

## 🧭 Bảng Tổng Hợp Cuộc Họp Toàn Tuyến (Meeting Matrix)

| Ngày | Phân hệ | Chủ đề & Quyết định cốt lõi | Thời lượng | Bản Script / Transcript (Tier A) | Audio gốc (Tier C) |
|---|---|---|:---:|---|---|
| **2026-09-11** | **ShareData** | **Chuẩn hóa Metadata Gói tin & Mapping 2 chiều:**<br>• Lưu danh sách trường động (gói 101,...) thay vì nối bảng SQL cứng.<br>• Cơ chế Mapping: Bộ mã quy đổi CodeSet, hàm SUM/AVG, format đầu ra.<br>• Luồng Inbound/Outbound qua WebAPI & Background Service.<br>• Bảo toàn nguyên trạng 9 gói tin cũ đã demo. | ~111 phút | 📄 [2026-09-11-sharedata-script.md](ShareData/doc/transcript/2026-09-11-sharedata-script.md) | 🎙️ [Audio 1](Plan/_source/audio/2026-09-11-sharedata-videowall-1.m4a)<br>🎙️ [Audio 2](Plan/_source/audio/2026-09-11-sharedata-videowall-2.m4a) |
| **2026-09-11** | **VideoWall** | **Kiến trúc 3 tầng & Phân quyền Màn hình:**<br>• Tách riêng Background Service giao tiếp ISAPI phần cứng qua NATS để chống blocking WebAPI.<br>• 3 trụ cột: Thiết lập (Config), Điều khiển (Control), Trạng thái (Telemetry).<br>• Phân quyền theo Screen ID: ưu tiên User > Org, mặc định Full quyền. | ~111 phút | 📄 [2026-09-11-videowall-script.md](VideoWall/doc/transcript/2026-09-11-videowall-script.md) | 🎙️ [Audio 1](Plan/_source/audio/2026-09-11-sharedata-videowall-1.m4a)<br>🎙️ [Audio 2](Plan/_source/audio/2026-09-11-sharedata-videowall-2.m4a) |
| **2026-09-09** | **ShareData** | **Review luồng truyền nhận & Tự động hóa:**<br>• Luồng gửi/nhận file & API, SQL alias mapping đối tác.<br>• Cơ chế gửi theo giờ (9h sáng hàng ngày), switch tự động gửi khi có data mới.<br>• Socket Wrapper & cơ chế xóa log đầu ngày. | ~61 phút | 📄 [2026-09-09-review-sharedata.md](ShareData/doc/transcript/2026-09-09-review-sharedata.md) | 🎙️ [Review 1](ShareData/_source/audio/2026-09-09-review-1.m4a)<br>🎙️ [Review 2](ShareData/_source/audio/2026-09-09-review-2.m4a) |
| **2026-09-09** | **VideoWall** | **Phân quyền Khu vực & Ma trận hiển thị:**<br>• Rào phạm vi thao tác người dùng theo tọa độ ma trận (lưới 8x5).<br>• Dựng cây phân cấp Zone bằng SqlSugar `ToTree()` tối ưu.<br>• Trang quản trị dịch vụ tập trung (Health Check qua NATS). | 10:37 | 📄 [2026-09-09-videowall-phan-quyen-va-layout.md](VideoWall/doc/transcript/2026-09-09-videowall-phan-quyen-va-layout.md) | 🎙️ [Audio ghi âm](VideoWall/_source/audio/2026-09-09-videowall-phan-quyen-va-layout.m4a) |
| **2026-09-08** | **Plan** (Toàn tuyến) | **Kế hoạch triển khai & Nghiệm thu toàn tuyến:**<br>• Rà soát thiết bị TMC/ITS: Camera CCTV, PTZ, VDS, biển báo VMS.<br>• Hệ thống giám sát EMS SolarWinds, Trạm cân, Trạm thời tiết (WOS).<br>• Kiến trúc hàng đợi MQTT / Kafka, mốc nghiệm thu ~Tháng 11. | ~48 phút | 📄 [2026-09-08-hop-ke-hoach-1.md](Plan/doc/transcript/2026-09-08-hop-ke-hoach-1.md)<br>📄 [2026-09-08-hop-ke-hoach-2.md](Plan/doc/transcript/2026-09-08-hop-ke-hoach-2.md) | 🎙️ [Phần 1](Plan/_source/audio/2026-09-08-hop-ke-hoach-1.m4a)<br>🎙️ [Phần 2](Plan/_source/audio/2026-09-08-hop-ke-hoach-2.m4a) |
| **2026-08-28** | **VideoWall** | **Chuẩn bị mượn & kiểm thử Controller Hikvision:**<br>• Kịch bản mượn thiết bị DS-C66S từ nhà thầu, test API bằng Postman/curl.<br>• Backup cấu hình IP qua Web/API, chuẩn bị nhân sự test 2 ngày. | ~50 phút | 📄 [2026-08-28-videowall-chuan-bi.md](VideoWall/doc/transcript/2026-08-28-videowall-chuan-bi.md) | _(Lưu trữ nội bộ)_ |

---

## 📊 Chi tiết Trọng tâm từng Phân hệ

### 1. Phân hệ Chia sẻ Dữ liệu (ShareData)

```
[CSDL Nội Bộ] ──(SqlSugar)──> [Dữ liệu Thô] ──> [Mapping Engine] ──> [JSON Payload] ──> [REST API / File Outbound]
                                                      │
                                           ┌──────────┴──────────┐
                                           │ • CodeSet (Quy đổi) │
                                           │ • Hàm SUM, AVG...   │
                                           │ • Format DateTime   │
                                           └─────────────────────┘
```

* **Quy tắc bất biến:** Bảo toàn 9 gói tin cũ đã nghiệm thu demo.
* **Cấu trúc trường Metadata:** Bảng cấu hình lưu `PacketCode`, `FieldCode`, `FieldName`, `DataType` (`string`, `number`, `datetime`, `bool`), `Description`.
* **Cơ chế Mapping 2 chiều:**
  * **Outbound:** DB nội bộ -> Đổi tên trường & áp dụng CodeSet/Hàm -> Đóng gói payload gửi đi.
  * **Inbound:** WebAPI nhận -> Bảng đệm -> Service map ngược -> Lưu DB chính.
* **Tài liệu chi tiết:** 👉 [ShareData/doc/transcript/2026-09-11-sharedata-script.md](ShareData/doc/transcript/2026-09-11-sharedata-script.md)

---

### 2. Phân hệ Tường Màn Hình (VideoWall)

```
[Người dùng trên Web]
        │
        ▼ (HTTP REST)
[Backend WebAPI]
        │
        ▼ (Message Command qua NATS)
[VideoWall Background Service]  <──(Cách ly lỗi, tự động retry)
        │
        ▼ (ISAPI Digest HTTP)
[Hikvision Controller DS-C66S / DS-C30S]
```

* **Tách Service phần cứng:** Tránh nghẽn thread pool WebAPI khi phần cứng chập chờn/mất mạng.
* **3 trụ cột chức năng:**
  1. *Thiết lập (Config):* Ma trận màn hình, sơ đồ cổng, kịch bản (Scene).
  2. *Điều khiển (Control):* Chuyển scene, bật/tắt nguồn, cắt ghép cửa sổ.
  3. *Giám sát (Status):* Đọc heartbeat, trạng thái kênh, cập nhật real-time lên Web.
* **Phân quyền theo Màn hình:**
  * Bảng phân quyền có 2 cột `UserId` và `OrgId`.
  * **Độ ưu tiên:** `UserId` > `OrgId`.
  * **Mặc định:** Không cấu hình = **Full quyền**.
* **Tài liệu chi tiết:** 👉 [VideoWall/doc/transcript/2026-09-11-videowall-script.md](VideoWall/doc/transcript/2026-09-11-videowall-script.md)

---

## 📝 Bảng Ghi Chú Nhanh Cuộc Họp 11/09/2026 (Quick Notes Check-list)

> *Trích xuất trực tiếp từ bản note thực địa của đội ngũ kỹ thuật (`Sharedata-Videowall-Note_11_09_2026.txt`).*

<details>
<summary><b>👉 Nhấn vào đây để xem 22 điểm chốt ShareData & 6 điểm chốt VideoWall</b></summary>

#### ShareData:
1. Từ cơ sở dữ liệu ra file output, từ những output trong file sẽ lưu tên field trong bảng mới, và kiểu dữ liệu (string, datetime, number - cẩn thận số thực float, bool).
2. Trước đó SqlSugar join các kiểu để ra JSON object mong muốn => lấy từ object đó lưu lại tên field và kiểu giá trị.
3. Bảng mapping theo gói tin, đối tác, chiều gửi (2 chiều): ví dụ gói 101 cho đối tác A.
4. Bảng mapping nếu dùng XML vẫn quy về JSON (ví dụ `<Data>` quy về `"Data"`).
5. Xác định cấu trúc mong muốn 10 trường cho gói 101 liên quan giao thông.
6. Cấu hình bảng 101 gói tin sẽ có những field gì trong CSDL bên mình phải cấu hình trước.
7. Một số hàm có sẵn: `SUM`, `AVG` (tính trung bình).
8. Lưu xuống dưới DB sau khi xử lý xong.
9. Dựa vô gói 101 có những field gì mình sẽ đưa thông tin cho Hiếu lưu DB.
10. Nhãn mô tả, và xử lý giá trị đối tác không có trong CSDL (giá trị mặc định).
11. SqlSugar => object => đọc file config mapping => map xử lý dữ liệu => CodeSet => phép tính (`SUM`, `AVG`) => convert => format đầu ra.
12. Luồng gửi: sau khi xử lý xong gửi dữ liệu API/SSE, ghi ra file, rồi ghi log.
13. Luồng nhận: đi qua WebAPI => lưu vô bảng => Service lấy database ra xử lý dữ liệu nhận.
14. Luồng nhận: xác định đối tác nào => gói tin nào => hướng nhận => đọc bảng cấu hình => thiết lập => CodeSet danh sách.
15. Khi có cấu hình thiết lập mapping => mapping dữ liệu => trường dữ liệu nhận => cột quản lý trong gói tin => CodeSet ngược lại về giá trị CSDL => format lại theo cấu trúc CSDL (`dd/MM/yyyy` => `yyyy-MM-dd HH:mm:ss`) => convert kiểu dữ liệu.
16. Dữ liệu sau khi mapping luồng nhận => tạo câu truy vấn xử lý lưu trữ (thêm mới/chỉnh sửa), check tồn tại => thực thi truy vấn => ghi nhận kết quả.
17. Dùng SqlSugar native.
18. Nhận được dữ liệu là ghi log ngay.
19. Trạng thái ghi thêm: xử lý thành công hay thất bại.
20. Nội dung log chuyển qua Modal cho thống nhất, không dùng sidebar.
21. Không xử lý format byte gửi số bản tin.
22. Luồng nhận xử lý SQL truy vấn không hardcode.

#### VideoWall:
1. User phân quyền theo Screen (màn hình).
2. Luồng: `FE => BE => NATS => SERVICE => GỌI HTTP ISAPI Thiết bị`.
3. Thiết bị - Hướng đi: Thiết lập, điều khiển.
4. Thiết bị - Hướng nhận: Dữ liệu thông số, dữ liệu xử lý, kết quả thực thi.
5. Service điều khiển tính toán thiết lập, điều khiển thiết bị, đọc thông tin định kỳ (bật/tắt).
6. Nếu không có khai báo trong bảng thì mặc định là **Full quyền**; ưu tiên quyền `UserId` hơn `OrgId`, kiểm tra tránh trùng lặp (check duplicate).
</details>

---

## 👥 Ma trận Phân công Nhiệm vụ & Tiến độ (Master RACI)

| Thành viên | Phân hệ phụ trách | Hạng mục công việc chính | Hạn chót |
|---|---|---|:---:|
| **Đạt** | Backend WebAPI & DB | • Thiết kế CSDL: Bảng Metadata trường gói tin, Bảng Mapping, Bảng phân quyền màn hình.<br>• Xây dựng WebAPI nhận/gửi ShareData và API điều khiển VideoWall.<br>• Chuẩn hóa DTO và cấu trúc JSON trao đổi. | **18/09/2026** |
| **Hiếu** | Core Service Worker | • Lập trình Background Service VideoWall giao tiếp ISAPI thiết bị Hikvision.<br>• Đấu nối giao tiếp message lệnh và trạng thái qua NATS.<br>• Xây dựng module Mapping Engine động (CodeSet, Hàm tính toán) cho ShareData. | **18/09/2026** |
| **Kiên** | Frontend UI/UX | • Móc API VideoWall lên Web: hiển thị ma trận màn hình, rào phân quyền các ô.<br>• Tinh gọn giao diện ShareData: chuyển cấu hình trường vào Modal/Popup.<br>• Tối ưu tương tác kéo thả camera và chuyển scene. | **18/09/2026** |\n