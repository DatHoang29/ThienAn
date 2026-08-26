# DocBusinessThienAn — Chỉ mục tài liệu

Kho tài liệu nghiệp vụ & kỹ thuật dự án **Cao tốc Hữu Nghị – Chi Lăng (HN-CL)**.

Mỗi file được gắn một nhãn để biết ai là người đọc chính:

| Nhãn | Ý nghĩa |
|---|---|
| 🤖 **AI** | Text thuần (.md/.json/.sql) — AI đọc trực tiếp làm ngữ cảnh khi code, phân tích, trả lời. Đây là nguồn sự thật (source of truth) khi làm việc với agent. |
| 👤 **Human** | PDF, XLSX, ảnh, zip — dành cho người xem/đối chiếu. AI **không** đọc được (hoặc đọc rất tốn kém); đã tách vào `_source/` hoặc `images/`. |
| 🤖👤 **Cả hai** | File text nhưng người cũng đọc thường xuyên (README, glossary, báo cáo). |

**Quy ước thư mục**

- Cây thư mục chia theo **chủ đề nghiệp vụ**: `HN-CL/<Phân hệ>/`
- File AI đọc nằm ở **gốc mỗi chủ đề**.
- `_source/` = file gốc cho người (PDF, XLSX, zip). **Không cần đưa vào ngữ cảnh AI.**
- `images/` = ảnh trích từ tài liệu gốc, được nhúng bằng link tương đối trong file `.md` cùng cấp. Giữ nguyên vị trí, đừng di chuyển kẻo gãy link.

---

## Cây thư mục

```
DocBusinessThienAn/
├── INDEX.md                                  🤖👤 file bạn đang đọc
└── HN-CL/
    ├── ShareData/
    │   ├── 01-yeu-cau-nghiep-vu.md           🤖 yêu cầu hệ thống phần mềm chia sẻ dữ liệu
    │   ├── 02-mapping-goi-tin-101-111.md     🤖 mapping field gói tin 101–111
    │   ├── 03-audit-20260812.md              🤖👤 báo cáo audit ShareDataWorker
    │   └── sharedata_plan.md                 🤖 kế hoạch họp 19/08 + 25/08 (phần Share Data)
    └── VideoWall/
        ├── HIKVISION_ISAPI_VIDEOWALL_GLOSSARY.md   🤖👤 từ điển thuật ngữ — đọc đầu tiên
        ├── videowall_plan.md                 🤖 kế hoạch họp 19/08 + 25/08 (phần VideoWall)
        ├── API/                              🤖 Postman collection + environment
        ├── Controller-phan-cung/             🤖 + images/ 👤 hướng dẫn phần cứng DS-C66S
        ├── ISAPI-Videowall-Controller/       🤖 + images/ 👤 bộ tài liệu API ISAPI (00→10)
        ├── TableSQL/                         🤖 phân tích CSDL phân hệ Vw*
        └── _source/                          👤 2 PDF gốc (11 MB)
```

---

## 🤖 Dữ liệu cho AI đọc

### HN-CL / ShareData

| File | Kích thước | Nội dung | Khi nào đọc |
|---|---|---|---|
| `01-yeu-cau-nghiep-vu.md` | 28 KB · 569 dòng | Yêu cầu hệ thống phần mềm chia sẻ dữ liệu (TMC-PM-ITS-ESHARE / TA-ShareData), trích từ Chỉ dẫn kỹ thuật HN-CL | Nguồn yêu cầu gốc — đọc trước khi đụng vào ShareDataWorker |
| `02-mapping-goi-tin-101-111.md` | 20 KB · 233 dòng | Mapping từng field payload (camelCase) ↔ bảng/cột WebAPI cho gói tin 101–111, kèm quy tắc tính toán | Khi implement/sửa mapping, chốt hợp đồng API với đội tích hợp |
| `03-audit-20260812.md` | 16 KB · 110 dòng | Báo cáo audit `ShareDataWorker`, `.Core`, `.Tests` (bản gộp 14/08/2026) — gồm cả các quyết định "đã chốt / không làm" | Khi cần biết trạng thái & nợ kỹ thuật hiện tại của module |
| `sharedata_plan.md` | 16 KB · 344 dòng | Đặc tả yêu cầu + kế hoạch họp **19/08 và 25/08** gộp theo mốc ngày (luồng gửi/nhận, mapping, chuẩn hóa, ràng buộc HA/NATS, câu hỏi cần chốt + ghi chú thô 25/08). Phần VideoWall cùng giai đoạn xem `VideoWall/videowall_plan.md` | Khi cần bối cảnh nghiệp vụ + deadline hiện tại của ShareDataWorker |

### HN-CL / VideoWall

| File | Kích thước | Nội dung | Khi nào đọc |
|---|---|---|---|
| `HIKVISION_ISAPI_VIDEOWALL_GLOSSARY.md` | 24 KB · 258 dòng | Từ điển thuật ngữ + hướng dẫn nghiệp vụ Video Wall | **Đọc đầu tiên** khi mới vào phân hệ Video Wall |
| `videowall_plan.md` | 4.5 KB · 138 dòng | Đặc tả yêu cầu + kế hoạch họp **19/08 và 25/08** gộp theo mốc ngày (form/dịch vụ test tích hợp thiết bị, giao thức TCP, câu hỏi cần chốt + ghi chú thô 25/08 về công cụ WPF). Phần Share Data cùng giai đoạn xem `ShareData/sharedata_plan.md` | Khi cần bối cảnh & deadline hiện tại của hạng mục VideoWall |
| `ISAPI-Videowall-Controller/README.md` | 8 KB | Mục lục bộ tài liệu ISAPI (chuyển đổi từ PDF 512 trang) | Điểm vào của bộ 00→10 |
| `ISAPI-Videowall-Controller/00-api-catalog.md` | 44 KB · 305 dòng | Danh mục API decoding & video wall (chương 9.7, tr.332–499) kèm số trang PDF gốc | Tra nhanh "có API nào cho việc này" |
| `ISAPI-Videowall-Controller/00B-api-list-full.md` | 238 dòng | Chuyển đổi từ `_source/VideoWall_ISAPI_API_List.xlsx` — bảng đầy đủ 116 API (12 nhóm 9.7.1→9.7.11), mỗi API gắn nhãn Dùng chính / Tùy chọn-mở rộng / Ngoài phạm vi | Tra cứu dạng bảng đầy đủ, đối chiếu API nào thật sự dùng cho dự án |
| `ISAPI-Videowall-Controller/01-reading-guide.md` | < 1 KB | Hướng dẫn cách đọc bộ tài liệu | Lần đầu mở bộ tài liệu |
| `ISAPI-Videowall-Controller/02-overview.md` | 4 KB | Tổng quan thiết bị & hệ thống | Nắm bối cảnh |
| `ISAPI-Videowall-Controller/03-isapi-framework.md` | 8 KB | Khung ISAPI: URL, method, digest auth, mã lỗi | Trước khi gọi API lần đầu |
| `ISAPI-Videowall-Controller/04-quick-start-guide.md` | 60 KB · 1.361 dòng | Luồng thao tác nhanh theo kịch bản | Khi dựng luồng nghiệp vụ mới |
| `ISAPI-Videowall-Controller/05-device-management.md` | 68 KB · 1.185 dòng | Quản lý thiết bị (chung) | Cấu hình, thông tin thiết bị, user |
| `ISAPI-Videowall-Controller/06-information-security.md` | 8 KB | Bảo mật, xác thực, phân quyền | Rà soát bảo mật |
| `ISAPI-Videowall-Controller/07-video-general.md` | 12 KB | Video (chung): stream, kênh | Xử lý nguồn video |
| `ISAPI-Videowall-Controller/08-decoding-and-video-wall.md` | 8 KB | Giải mã & điều khiển tường màn hình | Nghiệp vụ lõi của phân hệ |
| `ISAPI-Videowall-Controller/09A-api-reference.md` | **1,6 MB · 38.212 dòng** | Đặc tả API đầy đủ theo tài liệu hãng | ⚠️ **Quá lớn để nạp cả file.** Chỉ grep/đọc theo đoạn khi cần đặc tả chi tiết một API cụ thể |
| `ISAPI-Videowall-Controller/09B-practical-guide-and-tested-responses.md` | 24 KB · 340 dòng | Response **đo thật** tại trạm Thiên An (DS-C66S-H88-CL, 10.10.9.236), bản đồ phần cứng, hệ toạ độ, các bẫy kỹ thuật | ⭐ Ưu tiên hơn 09A khi có mâu thuẫn — đây là thực tế đo được |
| `ISAPI-Videowall-Controller/10-how-to-video-guidance.md` | < 1 KB | Ghi chú video hướng dẫn | Ít dùng |
| `Controller-phan-cung/Controller-phan-cung.md` | 48 KB · 948 dòng | Quick start phần cứng DS-C66S: bo mạch, nguồn, đấu nối, kích hoạt thiết bị (kèm sơ đồ Mermaid) | Khi lắp đặt / đấu nối / kích hoạt thiết bị |
| `TableSQL/Vw_Tables_Analysis_And_Design.md` | 36 KB · 401 dòng | Phân tích toàn bộ bảng tiền tố `Vw*` trên DEV_ITS10: cột, quan hệ (ERD), nghiệp vụ | Trước khi viết SQL / sửa schema phân hệ Video Wall |
| `API/CollectionPostman/VideoWallAPI.postman_collection.json` | 8 KB | Postman collection các API Video Wall | Test tay hoặc sinh code client |
| `API/EnvPostman/VideoWallEnvironment.postman_environment.json` | 4 KB | Biến môi trường Postman (IP, port, credential) | Đi kèm collection trên |

---

## 👤 Tài liệu cho người đọc

Toàn bộ nằm trong `_source/` và `images/`. **AI không cần và không nên nạp các file này** (định dạng nhị phân, dung lượng lớn); mọi nội dung cần thiết đã được chuyển sang bản `.md` tương ứng.

| File | Kích thước | Bản .md tương ứng |
|---|---|---|
| `HN-CL/VideoWall/_source/Controller phần cứng.pdf` | 5,2 MB | `VideoWall/Controller-phan-cung/Controller-phan-cung.md` |
| `HN-CL/VideoWall/_source/ISAPI_Controller_Videowall Controller.pdf` | 6,1 MB | `VideoWall/ISAPI-Videowall-Controller/` (bộ 00→10) |

**Thư mục ảnh** (ảnh trích từ tài liệu gốc, được nhúng trong file `.md` cùng cấp — không di chuyển):

| Thư mục | Số ảnh | Dung lượng |
|---|---|---|
| `HN-CL/VideoWall/ISAPI-Videowall-Controller/images/` | 36 | 6,9 MB |
| `HN-CL/VideoWall/Controller-phan-cung/images/` | 32 | 5,9 MB |

---

## Quy tắc khi thêm tài liệu mới

1. Có file gốc PDF/XLSX/DOCX → **luôn tạo bản `.md`** đặt ở gốc chủ đề; file gốc bỏ vào `_source/` cùng chủ đề.
2. Ảnh trích ra để trong `images/` **cạnh file `.md`** dùng nó, link tương đối (`images/xxx.png`).
3. Đặt tên file không dấu, dùng `-` hoặc `_`; đánh số tiền tố (`01-`, `02-`) khi tài liệu có thứ tự đọc.
4. File `.md` vượt ~500 KB nên tách nhỏ hoặc ghi chú rõ "chỉ đọc theo đoạn" trong INDEX (như `09A-api-reference.md`).
5. Thêm file mới → cập nhật bảng trong INDEX.md này.

_Cập nhật lần cuối: 25/08/2026._
