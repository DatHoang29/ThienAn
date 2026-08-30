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
    │   ├── README.md                         🤖👤 chỉ mục Single Source of Truth của phân hệ ShareData
    │   └── ...                               (toàn bộ tài liệu phân hệ xem tại ShareData/README.md)
    └── VideoWall/
        ├── README.md                         🤖👤 chỉ mục Single Source of Truth của phân hệ Video Wall
        └── ...                               (toàn bộ tài liệu phân hệ xem tại VideoWall/README.md)
```

---

## 🤖 Dữ liệu cho AI đọc

### HN-CL / ShareData

> 🔴 **Single Source of Truth**: Toàn bộ danh mục tài liệu, phân loại chi tiết và quy tắc nghiệp vụ của phân hệ Chia sẻ Dữ liệu được quản lý tập trung và duy nhất tại:
> 👉 **[`ShareData/README.md`](ShareData/README.md)** (Gồm yêu cầu hệ thống, mapping gói tin 101–111, kế hoạch triển khai). Mọi cập nhật tài liệu ShareData chỉ thực hiện tại file này.

### HN-CL / VideoWall

> 🔴 **Single Source of Truth**: Toàn bộ danh mục tài liệu, phân loại chi tiết và hướng dẫn vận hành của phân hệ Video Wall được quản lý tập trung và duy nhất tại:
> 👉 **[`VideoWall/README.md`](VideoWall/README.md)** (Chia 2 khu: Khu 1 Reference ISAPI/Thiết bị & Khu 2 Công cụ WPF: Live Mode, Auto-Log, Scene Setup). Mọi cập nhật tài liệu Video Wall chỉ thực hiện tại file này.

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
