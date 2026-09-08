# Dự án Cao tốc Hữu Nghị – Chi Lăng (HN-CL) — Chỉ mục tài liệu

Kho tài liệu nghiệp vụ & kỹ thuật dự án **Cao tốc Hữu Nghị – Chi Lăng (HN-CL)**.

Phân loại tài liệu theo **3-Tier Context Budget**:

| Tier | Ý nghĩa | Định dạng / Vị trí | Hành vi AI |
|---|---|---|---|
| **Tier A** | AI-first (đọc trực tiếp) | `.md` < 150 KB, `.json` schema/cấu hình, `.sql` — nằm trong `doc/` | Đọc nguyên file trực tiếp. Là nguồn sự thật khi làm việc với agent. |
| **Tier B** | On-demand (grep-only) | `.md` ≥ 150 KB, bộ API reference dump, `data/*.json` log đo thật | **CHỈ grep / đọc offset + limit**. Cấm đọc nguyên file. Vào qua `00-catalog.md`. |
| **Tier C** | Human-only (không tự mở) | `_source/**` (PDF, XLSX, DOCX, ZIP), `**/images/**` (ảnh chụp) | **KHÔNG tự ý mở**. Dành cho người đối chiếu. Chỉ mở khi người dùng chỉ đích danh file. |

---

## Cây thư mục phân hệ

```
HữuNghị-ChiLăng/
├── INDEX.md                                  🤖👤 File bạn đang đọc (chỉ mục cấp dự án)
├── ShareData/                                🤖👤 Phân hệ Chia sẻ Dữ liệu (ESHARE)
│   ├── README.md                             🤖👤 SSOT phân hệ ShareData (kèm Tier Table)
│   ├── doc/                                  🤖    Tier A: 01-yeu-cau-nghiep-vu, 02-mapping, sharedata_plan
│   └── _source/                              👤    Tier C: Bản gốc đối chiếu (xlsx/)
└── VideoWall/                                🤖👤 Phân hệ Video Wall
    ├── README.md                             🤖👤 SSOT phân hệ Video Wall (kèm Tier Table)
    ├── doc/                                  🤖    Tier A + B: Tài liệu kỹ thuật, API, kịch bản
    │   ├── ISAPI-Videowall-Controller/       🤖    Tier A + B (09-api-reference.md: 1.670 KB grep-only)
    │   ├── Controller-phan-cung/              🤖    Tier A (tài liệu phần cứng + images/)
    │   ├── KichBan/                          🤖    Tier A (kịch bản 1 controller / 4 controller)
    │   ├── Plan/                             🤖    Tier A (kế hoạch gốc videowall_plan.md)
    │   ├── TableSQL/                         🤖    Tier A (thiết kế CSDL Vw*)
    │   └── Transcript/                       🤖    Tier A (transcript cuộc họp chuẩn bị)
    ├── data/                                 🤖    Tier B: Log đo thực tế trên thiết bị (logs-api/)
    └── _source/                              👤    Tier C: Bản gốc đối chiếu (pdf/, xlsx/, img/)
```

---

## 🤖 Dữ liệu cho AI đọc

### ShareData

> 🔴 **Single Source of Truth**: Toàn bộ danh mục tài liệu, phân loại chi tiết và Tier Table của phân hệ Chia sẻ Dữ liệu được quản lý tập trung và duy nhất tại:
> 👉 **[`ShareData/README.md`](ShareData/README.md)** (Gồm yêu cầu hệ thống, mapping gói tin 101–111, kế hoạch triển khai). Mọi cập nhật tài liệu ShareData chỉ thực hiện tại file này.

### VideoWall

> 🔴 **Single Source of Truth**: Toàn bộ danh mục tài liệu, phân loại chi tiết và hướng dẫn vận hành của phân hệ Video Wall được quản lý tập trung và duy nhất tại:
> 👉 **[`VideoWall/README.md`](VideoWall/README.md)** (Chia 2 khu: Khu 1 Reference ISAPI/Thiết bị & Khu 2 Công cụ WPF: Live Mode, Auto-Log, Scene Setup). Mọi cập nhật tài liệu Video Wall chỉ thực hiện tại file này.

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

**Thư mục ảnh** (ảnh trích từ tài liệu gốc, được nhúng trong file `.md` cùng cấp — không di chuyển):

| Thư mục | Số ảnh | Dung lượng |
|---|---|---|
| `VideoWall/doc/ISAPI-Videowall-Controller/images/` | 36 | 6,9 MB |
| `VideoWall/doc/Controller-phan-cung/images/` | 32 | 5,9 MB |

---

## Quy tắc khi thêm tài liệu mới

1. Có file gốc PDF/XLSX/DOCX → **luôn tạo bản `.md`** đặt trong `doc/` cùng phân hệ; file gốc bỏ vào `_source/{pdf,xlsx,docx,img,zip}/` cùng phân hệ.
2. File `.md` chuyển thể bắt buộc có **Frontmatter Provenance** (`tier`, `read`, `source`, `source_pages`, `extracted`).
3. Ảnh trích ra để trong `images/` **cạnh file `.md`** dùng nó, link tương đối (`images/xxx.png`).
4. Đặt tên file không dấu, dùng kebab-case; đánh số tiền tố (`01-`, `02-`) khi tài liệu có thứ tự đọc.
5. File `.md` dung lượng $\ge 150\text{ KB}$ bắt buộc là Tier B (`grep-only`) và có `00-catalog.md` làm cửa vào.
6. Thêm file mới → cập nhật Tier Table trong `README.md` của phân hệ tương ứng.

_Cập nhật lần cuối: 08/09/2026._
