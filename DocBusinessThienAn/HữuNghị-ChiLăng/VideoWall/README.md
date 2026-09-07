# VideoWall — Chỉ mục tài liệu

Thư mục này gồm **2 khu** khác mục đích. Xác định bạn đang làm gì rồi vào đúng khu.

| Bạn đang… | Đọc |
|---|---|
| Tìm hiểu nghiệp vụ / yêu cầu dự án | Khu 2 → `videowall_plan.md`, `transcript-videowall-28082026.md` |
| Tra bộ lệnh ISAPI / kiểu response thiết bị | Khu 1 → `ISAPI-Videowall-Controller/` (đọc `README.md` trước); response đo thật → `LogsAPI/` |
| **Kịch bản test API — 1 controller / 12 màn** | Khu 2 → `KichBan_VideoWall_DS-C30S-S11_12Man.md` |
| **Kịch bản test API — 4 controller / 32 màn** | Khu 2 → `KichBan_VideoWall_DS-C66S_4Controller_32Man.md` (+ giải thích topology & gọi API 4 khung: `_source/doc/GiaiThich_KetNoi_VideoWall_DS-C66S-H88-CL.md`) |
| Xem thông số phần cứng DS-C30S-S11 | `_source/DS-C30S-S11_Datasheet_20250324.md` |

---

## KHU 1 — Reference thiết bị & ISAPI *(tài liệu tra cứu, ít đổi)*

| Mục | Nội dung |
|---|---|
| `ISAPI-Videowall-Controller/` | Bộ tài liệu ISAPI controller: `README.md` → `00-api-catalog` → `01`…`10`; `09A-api-reference.md` **1.7 MB — chỉ `grep` theo endpoint, không đọc nguyên file**; `VideoWall_ISAPI_API_List.md` |
| `LogsAPI/` | **Log đo thật trên thiết bị** (`session-*-real.json`) — nguồn sự thật cho response ISAPI |
| `Controller-phan-cung/Controller-phan-cung.md` | Tài liệu phần cứng controller |
| `TableSQL/Vw_Tables_Analysis_And_Design.md` | Phân tích & thiết kế bảng CSDL `Vw*` |
| `_source/` | PDF / XLSX gốc + `DS-C30S-S11_Datasheet_20250324.md` (datasheet đã convert) — **PDF chỉ cho người xem** |

## KHU 2 — Công cụ WPF (Live Mode: Thiết lập Scene & Tự động ghi Log) *(kế hoạch & vận hành)*

| File | Nội dung | Đọc khi |
|---|---|---|
| `KichBan_VideoWall_DS-C30S-S11_12Man.md` | 20 kịch bản test API (KB-01 → KB-20) cho **1 controller** DS-C30S-S11 / 12 màn lưới 4×3, toạ độ ảo 7680×5760, công thức ID, mã lỗi, runbook | Test/vận hành cấu hình 1 khung |
| `KichBan_VideoWall_DS-C66S_4Controller_32Man.md` | Kịch bản test API cho **4 controller** (1 trung tâm + 3 con) / 32 màn lưới 8×4. Có **KB-00 probe read-only** chạy tại hiện trường. Đi kèm `_source/doc/GiaiThich_KetNoi_VideoWall_DS-C66S-H88-CL.md` (topology + cơ chế gọi API 4 khung, neo vào code `Module.VideoWall`) | Test/vận hành cấu hình 4 khung |
| `videowall_plan.md` | Đặc tả yêu cầu gốc — gộp họp 19/08 + 25/08 (bản lịch sử, không sửa) | đối chiếu yêu cầu ban đầu |
| `transcript-videowall-28082026.md` | Transcript đầy đủ buổi họp chuẩn bị 28/08 (bản lịch sử, không sửa) | tra chi tiết ai nói gì |

---

*Con trỏ trong bộ nhớ: `.agents/memory/videowall-record-replay-plan.md`.*
