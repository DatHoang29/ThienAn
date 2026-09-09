# DocBusinessThienAn — Chỉ mục tài liệu nghiệp vụ & kỹ thuật

> Kho lưu trữ tập trung tài liệu nghiệp vụ, đặc tả kỹ thuật, kịch bản kiểm thử và dữ liệu đo kiểm thực tế cho các dự án của Thiên Ân.

---

## 1. Nguyên tắc 3-Tier Context Budget

Phân loại tài liệu theo **chi phí context** (token budget), quy định rõ hành vi cho Trợ lý AI:

| Tier | Tên gọi | Định dạng / Vị trí | Hành vi AI bắt buộc |
|---|---|---|---|
| **Tier A** | **AI-first** | `.md` < 150 KB, `.json` schema/cấu hình, `.sql` — nằm trong `doc/` | Đọc nguyên file trực tiếp khi cần ngữ cảnh. Đây là nguồn sự thật (SSOT) khi code, phân tích và trả lời. |
| **Tier B** | **On-demand** | `.md` ≥ 150 KB, bộ dump API reference lớn, `data/*.json` log đo thực tế | **CHỈ `grep` / đọc theo `offset` + `limit`**. TUYỆT ĐỐI KHÔNG đọc nguyên file. Bắt buộc tra cứu thông qua file mục lục `00-catalog.md` hoặc README của thư mục. |
| **Tier C** | **Human-only** | `_source/**` (PDF, XLSX, DOCX, ZIP, **audio `.m4a/.mp3/.wav`**), `**/images/**` (ảnh chụp) | **KHÔNG tự ý mở**. Dành riêng cho người dùng đối chiếu. AI chỉ được mở khi người dùng yêu cầu đích danh tên file. Mọi file Tier C bắt buộc phải có bản `.md` tương ứng trong `doc/`. **`_source/**` KHÔNG vào git** (chỉ trên đĩa) — xem `.gitignore`. |

> **Quy tắc 1 dòng cho AI:** chỉ mở `.md` (và `.json`/`.sql` schema trong `doc/`). Mọi định dạng khác = nguồn thô, bỏ qua. File thô **chỉ** nằm trong `_source/`; mỗi file thô → đúng 1 file `.md` trong `doc/` cùng phân hệ.

---

## 2. Cấu trúc thư mục tổng thể (Target Layout)

```
DocBusinessThienAn/
├── INDEX.md                          🤖👤 Entry point cấp cao nhất (file bạn đang đọc)
├── llms.txt                          🤖    Bản đồ compact cho AI định tuyến nhanh
└── HữuNghị-ChiLăng/                  🤖👤 Dự án Cao tốc Hữu Nghị – Chi Lăng (GIỮ có dấu)
    ├── INDEX.md                      🤖👤 Chỉ mục cấp dự án
    │
    │   ┌─ CẤU TRÚC CHUẨN MỖI PHÂN HỆ (ShareData / VideoWall / WOS / Plan) ─┐
    │   │  README.md          🤖👤 SSOT module + Tier Table                  │
    │   │  doc/               🤖   Tier A/B: .md AI đọc                       │
    │   │    ├─ …             🤖   nghiệp vụ / API / kịch bản / TableSQL…     │
    │   │    └─ transcript/   🤖   bản ghi họp .md + 00-catalog.md (cửa vào)  │
    │   │  data/              🤖   Tier B: log đo thực tế (hiện chỉ VideoWall)│
    │   │  _source/           👤   Tier C: bản gốc, KHÔNG vào git             │
    │   │    └─ {pdf,xlsx,docx,img,zip,audio}/                               │
    │   └───────────────────────────────────────────────────────────────────┘
    │
    ├── ShareData/    doc/ (01-yeu-cau, 02-mapping-goi-tin, transcript/)  ·  _source/{xlsx,audio}/
    ├── VideoWall/    doc/ (ISAPI-Videowall-Controller/ [09-api-reference.md 1.670 KB grep-only],
    │                       Controller-phan-cung/, KichBan/, TableSQL/, transcript/)  ·  data/logs-api/
    │                 _source/{pdf,xlsx,img}/
    ├── WOS/          doc/ (cr1000x-specifications, -getting-started, cr1000x-product-manual/ [334 trang],
    │                       images/, transcript/)  ·  _source/pdf/
    └── Plan/         sharedata_plan.md · videowall_plan.md · TH-0908.md
                      doc/transcript/ (2026-09-08-hop-ke-hoach-{1,2}.md)  ·  _source/audio/
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
| **Cao tốc Hữu Nghị – Chi Lăng** | [`HữuNghị-ChiLăng/`](HữuNghị-ChiLăng/INDEX.md) | `ShareData`, `VideoWall`, `WOS` | [`HữuNghị-ChiLăng/INDEX.md`](HữuNghị-ChiLăng/INDEX.md) |

---

## 5. Quy tắc bảo trì & thêm tài liệu mới

1. **Nguyên tắc SSOT**: `README.md` của mỗi phân hệ là manifest duy nhất chứa **Tier Table**. Khi thêm/sửa/xoá file trong `doc/` hoặc `data/`, bắt buộc cập nhật Tier Table tương ứng.
2. **Quy tắc chuyển thể bản gốc**: Khi tiếp nhận file gốc (PDF/XLSX/DOCX/**audio**), luôn đưa bản gốc vào `_source/{pdf,xlsx,docx,img,zip,audio}/` và chuyển thể thành file `.md` đặt trong `doc/` kèm **Frontmatter Provenance**.
3. **Quy tắc ghi âm → transcript**: File `.m4a/.mp3/.wav` vào `_source/audio/`, tên `YYYY-MM-DD-<chu-de-kebab>`. Sinh transcript: `python tools/transcribe/transcribe.py <Module>` → `doc/transcript/<cùng-tên>.md` (frontmatter `source`, `duration`, `model`, `status: raw-asr`). Đăng ký vào `doc/transcript/00-catalog.md`. Soát tay để xoá dòng ảo model bịa ở đoạn im lặng.
4. **Quy tắc git**: `_source/**` **KHÔNG BAO GIỜ vào git** (`.gitignore` đã chặn) — bản gốc chỉ trên đĩa/OneDrive/NAS. Git chỉ mang `.md` dẫn xuất. Nhờ vậy "AI chỉ đọc `.md`" được bảo đảm bằng kiến trúc.
5. **Quy tắc Token Bomb Gate**: File `.md` có dung lượng $\ge 150\text{ KB}$ bắt buộc khai báo `Tier: B` (`grep-only`) và phải có file `00-catalog.md` làm cửa vào.
6. **Kiểm tra tự động**: Sau mỗi lần chỉnh sửa tài liệu, chạy script kiểm tra tính toàn vẹn:
   ```bash
   python .agents/scripts/check_doc_links.py DocBusinessThienAn
   ```
