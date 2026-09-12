# Dự án Cao tốc Hữu Nghị – Chi Lăng (HN-CL) — Chỉ mục tài liệu & Meeting Hub

Kho tài liệu nghiệp vụ, kỹ thuật & **quản lý biên bản họp, script chuyển thể, ghi âm tập trung** của dự án **Cao tốc Hữu Nghị – Chi Lăng (HN-CL)**.

Phân loại tài liệu theo **3-Tier Context Budget**:

| Tier | Ý nghĩa | Định dạng / Vị trí | Hành vi AI |
|---|---|---|---|
| **Tier A** | AI-first (đọc trực tiếp) | `.md` < 150 KB, `.json` schema/cấu hình, `.sql` — nằm trong `doc/` | Đọc nguyên file trực tiếp. Là nguồn sự thật khi làm việc với agent. |
| **Tier B** | On-demand (grep-only) | `.md` ≥ 150 KB, bộ API reference dump, `data/*.json` log đo thật | **CHỈ grep / đọc offset + limit**. Cấm đọc nguyên file. Vào qua `00-catalog.md`. |
| **Tier C** | Human-only (không tự mở) | `_source/**` (PDF, XLSX, DOCX, ZIP, Audio), `**/images/**` (ảnh chụp) | **KHÔNG tự ý mở**. Dành cho người đối chiếu. Chỉ mở khi người dùng chỉ đích danh file. |

---

## 🎯 Bảng Điều Khiển Biên Bản Họp, Script & Ghi Âm Tập Trung (Meeting Hub)

> **Mục đích:** Quản lý toàn bộ biên bản họp, script chuyển thể và file ghi âm gốc của các phân hệ (**ShareData**, **VideoWall**, **Plan toàn tuyến**) ngay tại file này (Single Source of Truth). Không cần mở nhiều file rời rạc.

### 🧭 Bảng Tổng Hợp Cuộc Họp Toàn Tuyến (Meeting Matrix)

| Ngày | Phân hệ | Chủ đề & Quyết định cốt lõi | Thời lượng | Bản Script / Transcript (Tier A) | Audio gốc (Tier C) |
|---|---|---|:---:|---|---|
| **2026-09-11** | **ShareData** | **Chuẩn hóa Metadata Gói tin & Mapping 2 chiều:**<br>• Lưu danh sách trường động (gói 101,...) thay vì nối bảng SQL cứng.<br>• Cơ chế Mapping: Bộ mã quy đổi CodeSet, hàm SUM/AVG, format đầu ra.<br>• Luồng Inbound/Outbound qua WebAPI & Background Service.<br>• Bảo toàn nguyên trạng 9 gói tin cũ đã demo. | ~111 phút | 📄 [2026-09-11-sharedata-script.md](ShareData/doc/transcript/2026-09-11-sharedata-script.md) | 🎙️ [Audio 1](Plan/_source/audio/2026-09-11-sharedata-videowall-1.m4a)<br>🎙️ [Audio 2](Plan/_source/audio/2026-09-11-sharedata-videowall-2.m4a) |
| **2026-09-11** | **VideoWall** | **Kiến trúc 3 tầng & Phân quyền Màn hình:**<br>• Tách riêng Background Service giao tiếp ISAPI phần cứng qua NATS để chống blocking WebAPI.<br>• 3 trụ cột: Thiết lập (Config), Điều khiển (Control), Trạng thái (Telemetry).<br>• Phân quyền theo Screen ID: ưu tiên User > Org, mặc định Full quyền. | ~111 phút | 📄 [2026-09-11-videowall-script.md](VideoWall/doc/transcript/2026-09-11-videowall-script.md) | 🎙️ [Audio 1](Plan/_source/audio/2026-09-11-sharedata-videowall-1.m4a)<br>🎙️ [Audio 2](Plan/_source/audio/2026-09-11-sharedata-videowall-2.m4a) |
| **2026-09-09** | **ShareData** | **Review luồng truyền nhận & Tự động hóa:**<br>• Luồng gửi/nhận file & API, SQL alias mapping đối tác.<br>• Cơ chế gửi theo giờ (9h sáng hàng ngày), switch tự động gửi khi có data mới.<br>• Socket Wrapper, kiểm tra 291 dòng & fix lỗi Case-Sensitivity. | ~61 phút | 📄 [2026-09-09-review-sharedata.md](ShareData/doc/transcript/2026-09-09-review-sharedata.md) | 🎙️ [MakeUp 3](../MakeUp%20Chi%20Ngo%CC%82%20Go%CC%80%20Va%CC%82%CC%81p%203.m4a)<br>🎙️ [MakeUp 4](../MakeUp%20Chi%20Ngo%CC%82%20Go%CC%80%20Va%CC%82%CC%81p%204.m4a) |
| **2026-09-09** | **VideoWall** | **Phân quyền Khu vực & Ma trận hiển thị:**<br>• Rào phạm vi thao tác người dùng theo tọa độ ma trận (lưới 8x5).<br>• Dựng cây phân cấp Zone bằng SqlSugar `ToTree()` tối ưu.<br>• Ghép nối Service thật qua NATS, bỏ Mock Data. | 10:37 | 📄 [2026-09-09-videowall-phan-quyen-va-layout.md](VideoWall/doc/transcript/2026-09-09-videowall-phan-quyen-va-layout.md) | 🎙️ [MakeUp 5](../MakeUp%20Chi%20Ngo%CC%82%20Go%CC%80%20Va%CC%82%CC%81p%205.m4a) |
| **2026-09-08** | **Plan** (Toàn tuyến) | **Kế hoạch triển khai & Nghiệm thu toàn tuyến:**<br>• Rà soát thiết bị TMC/ITS: Camera CCTV, PTZ, VDS, biển báo VMS.<br>• Hệ thống giám sát EMS SolarWinds, Trạm cân, Trạm thời tiết (WOS).<br>• Kiến trúc hàng đợi MQTT / Kafka, mốc nghiệm thu ~Tháng 11. | ~48 phút | 📄 [2026-09-08-hop-ke-hoach-1.md](Plan/doc/transcript/2026-09-08-hop-ke-hoach-1.md)<br>📄 [2026-09-08-hop-ke-hoach-2.md](Plan/doc/transcript/2026-09-08-hop-ke-hoach-2.md) | 🎙️ [Phần 1](Plan/_source/audio/2026-09-08-hop-ke-hoach-1.m4a)<br>🎙️ [Phần 2](Plan/_source/audio/2026-09-08-hop-ke-hoach-2.m4a) |
| **2026-08-28** | **VideoWall** | **Chuẩn bị mượn & kiểm thử Controller Hikvision:**<br>• Kịch bản mượn thiết bị DS-C66S từ nhà thầu, test API bằng Postman/curl.<br>• Backup cấu hình IP qua Web/API, chuẩn bị nhân sự test 2 ngày. | ~50 phút | 📄 [2026-08-28-videowall-chuan-bi.md](VideoWall/doc/transcript/2026-08-28-videowall-chuan-bi.md) | _(Lưu trữ nội bộ)_ |

---

### 📊 Chi tiết Trọng tâm từng Phân hệ

#### 1. Phân hệ Chia sẻ Dữ liệu (ShareData)
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

#### 2. Phân hệ Tường Màn Hình (VideoWall)
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

---

### 📝 Bảng Ghi Chú Nhanh Cuộc Họp (Quick Notes Check-list)

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

### 👥 Ma trận Phân công Nhiệm vụ & Tiến độ (Master RACI)

| Thành viên | Phân hệ phụ trách | Hạng mục công việc chính | Hạn chót |
|---|---|---|:---:|
| **Đạt** | Backend WebAPI & DB | • Thiết kế CSDL: Bảng Metadata trường gói tin, Bảng Mapping, Bảng phân quyền màn hình.<br>• Xây dựng WebAPI nhận/gửi ShareData và API điều khiển VideoWall.<br>• Chuẩn hóa DTO và cấu trúc JSON trao đổi. | **18/09/2026** |
| **Hiếu** | Core Service Worker | • Lập trình Background Service VideoWall giao tiếp ISAPI thiết bị Hikvision.<br>• Đấu nối giao tiếp message lệnh và trạng thái qua NATS.<br>• Xây dựng module Mapping Engine động (CodeSet, Hàm tính toán) cho ShareData. | **18/09/2026** |
| **Kiên** | Frontend UI/UX | • Móc API VideoWall lên Web: hiển thị ma trận màn hình, rào phân quyền các ô.<br>• Tinh gọn giao diện ShareData: chuyển cấu hình trường vào Modal/Popup.<br>• Tối ưu tương tác kéo thả camera và chuyển scene. | **18/09/2026** |

---

## Cây thư mục phân hệ

```
HữuNghị-ChiLăng/
├── INDEX.md                                  🤖👤 File bạn đang đọc (Chỉ mục cấp dự án & Meeting Hub)
├── ShareData/                                🤖👤 Phân hệ Chia sẻ Dữ liệu (ESHARE)
│   ├── README.md                             🤖👤 SSOT phân hệ ShareData (kèm Tier Table)
│   ├── doc/                                  🤖    Tier A: 01-yeu-cau-nghiep-vu, 02-mapping, sharedata_plan
│   │   └── transcript/                       🤖    Tier A: 2026-09-09-review, 2026-09-11-sharedata-script
│   └── _source/                              👤    Tier C: Bản gốc đối chiếu (xlsx/)
├── VideoWall/                                🤖👤 Phân hệ Video Wall
│   ├── README.md                             🤖👤 SSOT phân hệ Video Wall (kèm Tier Table)
│   ├── doc/                                  🤖    Tier A + B: Tài liệu kỹ thuật, API, kịch bản
│   │   ├── ISAPI-Videowall-Controller/       🤖    Tier A + B (09-api-reference.md: 1.670 KB grep-only)
│   │   ├── Controller-phan-cung/              🤖    Tier A (tài liệu phần cứng + images/)
│   │   ├── KichBan/                          🤖    Tier A (kịch bản 1 controller / 4 controller)
│   │   ├── Plan/                             🤖    Tier A (kế hoạch gốc videowall_plan.md)
│   │   ├── TableSQL/                         🤖    Tier A (thiết kế CSDL Vw*)
│   │   └── transcript/                       🤖    Tier A: 2026-08-28, 2026-09-09, 2026-09-11-videowall-script
│   ├── data/                                 🤖    Tier B: Log đo thực tế trên thiết bị (logs-api/)
│   └── _source/                              👤    Tier C: Bản gốc đối chiếu (pdf/, xlsx/, img/, audio/)
├── WOS/                                      🤖👤 Phân hệ Trạm Thời tiết & Khí tượng (Campbell CR1000X)
│   ├── README.md                             🤖👤 SSOT phân hệ WOS (kèm Tier Table & hướng dẫn tích hợp)
│   ├── doc/                                  🤖    Tier A + B: Toàn bộ .md kỹ thuật, cấu hình, cẩm nang
│   │   ├── cr1000x-specifications.md         🤖    Tier A: Thông số kỹ thuật chi tiết
│   │   ├── cr1000x-getting-started-guide.md  🤖    Tier A: Hướng dẫn khởi động nhanh
│   │   ├── cr1000x-product-manual/           🤖    Tier A + B: Cẩm nang 334 trang (00-catalog, 01–11, full)
│   │   └── images/                           🤖    Sơ đồ đấu nối, ảnh chụp thiết bị
│   └── _source/                              👤    Tier C: Bản gốc đối chiếu (pdf/)
└── Plan/                                     🤖👤 Kế hoạch & biên bản họp xuyên phân hệ
    ├── sharedata_plan.md / videowall_plan.md / TH-0908.md   🤖 Tier A: Bản kế hoạch
    ├── doc/transcript/                       🤖    Tier A: Bản ghi họp .md (2026-09-08-hop-ke-hoach-{1,2}.md)
    └── _source/audio/                        👤    Tier C: Ghi âm gốc (2026-09-08, 2026-09-11)
```

---

## 🤖 Dữ liệu cho AI đọc

### ShareData

> 🔴 **Single Source of Truth**: Toàn bộ danh mục tài liệu, phân loại chi tiết và Tier Table của phân hệ Chia sẻ Dữ liệu được quản lý tập trung và duy nhất tại:
> 👉 **[`ShareData/README.md`](ShareData/README.md)** (Gồm yêu cầu hệ thống, mapping gói tin 101–111, kế hoạch triển khai). Mọi cập nhật tài liệu ShareData chỉ thực hiện tại file này.

### VideoWall

> 🔴 **Single Source of Truth**: Toàn bộ danh mục tài liệu, phân loại chi tiết và hướng dẫn vận hành của phân hệ Video Wall được quản lý tập trung và duy nhất tại:
> 👉 **[`VideoWall/README.md`](VideoWall/README.md)** (Chia 2 khu: Khu 1 Reference ISAPI/Thiết bị & Khu 2 Công cụ WPF: Live Mode, Auto-Log, Scene Setup). Mọi cập nhật tài liệu Video Wall chỉ thực hiện tại file này.

### WOS (Trạm quan trắc thời tiết)

> 🔴 **Single Source of Truth**: Toàn bộ danh mục tài liệu, phân loại chi tiết và Tier Table của phân hệ Trạm quan trắc thời tiết & Cảm biến khí tượng được quản lý tập trung và duy nhất tại:
> 👉 **[`WOS/README.md`](WOS/README.md)** (Gồm thông số Campbell CR1000X, cảm biến gió 05103, cảm biến mưa TB4, giao thức Modbus/SDI-12, cẩm nang 334 trang, hồ sơ BVTKTC 25 bản vẽ, hồ sơ nghiệm thu vật tư đầu vào đợt 1 & 2). Mọi cập nhật tài liệu WOS chỉ thực hiện tại file này.

---

## 👤 Tài liệu cho người đọc

Toàn bộ nằm trong `_source/` và `images/`. **AI không tự ý nạp các file này**; mọi nội dung cần thiết đã được chuyển sang bản `.md` tương ứng trong `doc/`.

| File | Kích thước | Bản .md tương ứng |
|---|---|---|
| `VideoWall/_source/pdf/Controller phần cứng.pdf` | 5,2 MB | `VideoWall/doc/Controller-phan-cung/Controller-phan-cung.md` |
| `VideoWall/_source/pdf/ISAPI_Controller_Videowall Controller.pdf` | 6,1 MB | `VideoWall/doc/ISAPI-Videowall-Controller/` (bộ 00→10) |
| `VideoWall/_source/pdf/DS-C30S-S11_Datasheet_20250324.pdf` | 790 KB | `VideoWall/doc/DS-C30S-S11_Datasheet_20250324.md` |
| `VideoWall/_source/xlsx/VideoWall_ISAPI_API_List.xlsx` | 32 KB | `VideoWall/doc/ISAPI-Videowall-Controller/VideoWall_ISAPI_API_List.md` |
| `ShareData/_source/xlsx/ESHARE_TOAN_BO_BANG.xlsx` | 57 KB | `ShareData/doc/02-mapping-goi-tin-101-111.md` |
| `WOS/_source/pdf/s_cr1000x.pdf` | 1,04 MB | `WOS/doc/cr1000x-specifications.md` |
| `WOS/_source/pdf/cr1000x-getting-started-guide.pdf` | 1,6 MB | `WOS/doc/cr1000x-getting-started-guide.md` |
| `WOS/_source/pdf/cr1000x-product-manual.pdf` | 11,8 MB | `WOS/doc/cr1000x-product-manual/` (bộ 00→11) |
| `WOS/_source/pdf/5. BVTKTC_WOS.pdf` | 25,15 MB | `WOS/doc/bvtktc-wos.md` |
| `WOS/_source/pdf/C2.TAP III.Q2.1.3 NTĐV CCTV,VDS,VMS,DTS,WOS,TMS,PBX.pdf` | 244,98 MB | `WOS/doc/ho-so-nghiem-thu-wos.md` |
| `WOS/_source/pdf/C2.TAP III.Q2.2.3 NTĐV CCTV,VDS,VMS,DTS,WOS,TMS,PBX.pdf` | 183,89 MB | `WOS/doc/ho-so-nghiem-thu-wos.md` |

**Thư mục ảnh** (ảnh trích từ tài liệu gốc, được nhúng trong file `.md` cùng cấp — không di chuyển):

| Thư mục | Số ảnh | Dung lượng |
|---|---|---|
| `VideoWall/doc/ISAPI-Videowall-Controller/images/` | 36 | 6,9 MB |
| `VideoWall/doc/Controller-phan-cung/images/` | 32 | 5,9 MB |
| `WOS/doc/images/` | 6 | ~820 KB |

---

## Quy tắc khi thêm tài liệu mới

1. Có file gốc PDF/XLSX/DOCX → **luôn tạo bản `.md`** đặt trong `doc/` cùng phân hệ; file gốc bỏ vào `_source/{pdf,xlsx,docx,img,zip}/` cùng phân hệ.
2. File `.md` chuyển thể bắt buộc có **Frontmatter Provenance** (`tier`, `read`, `source`, `source_pages`, `extracted`).
3. Ảnh trích ra để trong `images/` **cạnh file `.md`** dùng nó, link tương đối (`images/xxx.png`).
4. Đặt tên file không dấu, dùng kebab-case; đánh số tiền tố (`01-`, `02-`) khi tài liệu có thứ tự đọc.
5. File `.md` dung lượng $\ge 150	ext{ KB}$ bắt buộc là Tier B (`grep-only`) và có `00-catalog.md` làm cửa vào.
6. Thêm file mới → cập nhật Tier Table trong `README.md` của phân hệ tương ứng và bảng tổng hợp trong `INDEX.md`.

_Cập nhật lần cuối: 12/09/2026._\n