# DocBusinessThienAn — Chỉ mục tài liệu nghiệp vụ & kỹ thuật

> Kho lưu trữ tập trung tài liệu nghiệp vụ, đặc tả kỹ thuật, kịch bản kiểm thử và dữ liệu đo kiểm thực tế cho các dự án của Thiên Ân.

---

## 1. Nguyên tắc 3-Tier Context Budget

Phân loại tài liệu theo **chi phí context** (token budget), quy định rõ hành vi cho Trợ lý AI:

| Tier | Tên gọi | Định dạng / Vị trí | Hành vi AI bắt buộc |
|---|---|---|---|
| **Tier A** | **AI-first** | `.md` < 150 KB, `.json` schema/cấu hình, `.sql` — nằm trong `doc/` | Đọc nguyên file trực tiếp khi cần ngữ cảnh. Đây là nguồn sự thật (SSOT) khi code, phân tích và trả lời. |
| **Tier B** | **On-demand** | `.md` ≥ 150 KB, bộ dump API reference lớn, `data/*.json` log đo thực tế | **CHỈ `grep` / đọc theo `offset` + `limit`**. TUYỆT ĐỐI KHÔNG đọc nguyên file. Bắt buộc tra cứu thông qua file mục lục `00-catalog.md` hoặc README của thư mục. |
| **Tier C** | **Human-only** | `_source/**` (PDF, XLSX, DOCX, ZIP), `**/images/**` (ảnh chụp) | **KHÔNG tự ý mở**. Dành riêng cho người dùng đối chiếu. AI chỉ được mở khi người dùng yêu cầu đích danh tên file. Mọi file Tier C bắt buộc phải có bản `.md` tương ứng trong `doc/`. |

---

## 2. Cấu trúc thư mục tổng thể (Target Layout)

```
DocBusinessThienAn/
├── INDEX.md                          🤖👤 Entry point cấp cao nhất (file bạn đang đọc)
├── llms.txt                          🤖    Bản đồ compact cho AI định tuyến nhanh
└── HữuNghị-ChiLăng/                        🤖👤 Dự án Cao tốc Hữu Nghị – Chi Lăng (GIỮ có dấu)
    ├── INDEX.md                      🤖👤 Chỉ mục cấp dự án
    ├── ShareData/                    🤖👤 Phân hệ Chia sẻ Dữ liệu (ESHARE)
    │   ├── README.md                 🤖👤 SSOT của module + Tier Table
    │   ├── doc/                      🤖    Tier A: Nghiệp vụ, mapping gói tin 101–111, kế hoạch
    │   └── _source/                  👤    Tier C: Bản gốc cho người đối chiếu (xlsx/)
    └── VideoWall/                    🤖👤 Phân hệ Video Wall
        ├── README.md                 🤖👤 SSOT của module + Tier Table
        ├── doc/                      🤖    Tier A + B: Toàn bộ .md kỹ thuật, API, kịch bản
        │   ├── ISAPI-Videowall-Controller/ 🤖 Tier A + B (09-api-reference.md: 1.670 KB grep-only)
        │   ├── Controller-phan-cung/  🤖 Tier A (kèm images/ phục vụ hiển thị)
        │   ├── KichBan/              🤖 Tier A (Kịch bản 1 controller / 4 controller)
        │   ├── Plan/                 🤖 Tier A (Bản kế hoạch lịch sử)
        │   ├── TableSQL/             🤖 Tier A (Phân tích & thiết kế CSDL)
        │   └── Transcript/           🤖 Tier A (Transcript cuộc họp chuẩn bị)
        ├── data/                     🤖    Tier B: Log đo thực tế trên thiết bị (logs-api/)
        └── _source/                  👤    Tier C: Bản gốc cho người đối chiếu (pdf/, xlsx/, img/)
```

---

## 3. Quy ước đặt tên & Ngoại lệ (Naming Conventions & Exceptions)

1. **Ngoại lệ thư mục dự án (Project Directory Exception)**:
   > 🔴 **ĐÃ CHỐT 08/09/2026**: Thư mục cấp dự án được **giữ nguyên tiếng Việt có dấu** (ví dụ: `HữuNghị-ChiLăng/`) để khớp 100% với định danh quản lý dự án của Thiên Ân và bảo toàn toàn bộ liên kết nội bộ trong `.agents/memory/MEMORY.md`. **TUYỆT ĐỐI KHÔNG ĐỔI TÊN, KHÔNG BỎ DẤU**.

2. **Quy chuẩn tên file và thư mục con**:
   - Tên file và thư mục kỹ thuật bên trong module (`doc/`, `data/`, `_source/`...) viết bằng **tiếng Việt không dấu hoặc tiếng Anh, dùng kebab-case** (`ten-tai-lieu.md`).
   - Đánh số thứ tự (`01-`, `02-`) khi tài liệu có luồng đọc tuần tự.
   - File mục lục cho bộ tài liệu nhiều phần luôn đặt là `00-catalog.md` (hoặc `00-api-catalog.md`).

---

## 4. Danh mục các dự án

| Dự án | Thư mục | Phân hệ trực thuộc | Chỉ mục chi tiết |
|---|---|---|---|
| **Cao tốc Hữu Nghị – Chi Lăng** | [`HữuNghị-ChiLăng/`](HữuNghị-ChiLăng/INDEX.md) | `ShareData`, `VideoWall` | [`HữuNghị-ChiLăng/INDEX.md`](HữuNghị-ChiLăng/INDEX.md) |

---

## 5. Quy tắc bảo trì & thêm tài liệu mới

1. **Nguyên tắc SSOT**: `README.md` của mỗi phân hệ là manifest duy nhất chứa **Tier Table**. Khi thêm/sửa/xoá file trong `doc/` hoặc `data/`, bắt buộc cập nhật Tier Table tương ứng.
2. **Quy tắc chuyển thể bản gốc**: Khi tiếp nhận file gốc (PDF/XLSX/DOCX), luôn đưa bản gốc vào `_source/{pdf,xlsx,docx,img,zip}/` và chuyển thể thành file `.md` đặt trong `doc/` kèm **Frontmatter Provenance**.
3. **Quy tắc Token Bomb Gate**: File `.md` có dung lượng $\ge 150\text{ KB}$ bắt buộc khai báo `Tier: B` (`grep-only`) và phải có file `00-catalog.md` làm cửa vào.
4. **Kiểm tra tự động**: Sau mỗi lần chỉnh sửa tài liệu, chạy script kiểm tra tính toàn vẹn:
   ```bash
   python .agents/scripts/check_doc_links.py DocBusinessThienAn
   ```
