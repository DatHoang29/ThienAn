# VideoWall — Chỉ mục tài liệu

Thư mục này gồm **2 khu** khác mục đích. Xác định bạn đang làm gì rồi vào đúng khu.

| Bạn đang… | Đọc |
|---|---|
| Tìm hiểu nghiệp vụ / yêu cầu dự án | Khu 2 → `doc/Plan/videowall_plan.md`, `doc/Transcript/transcript-videowall-28082026.md` |
| Tra bộ lệnh ISAPI / kiểu response thiết bị | Khu 1 → `doc/ISAPI-Videowall-Controller/` (đọc `README.md` trước); response đo thật → `data/logs-api/` |
| **Kịch bản test API — 1 controller / 12 màn** | Khu 2 → `doc/KichBan/KichBan_VideoWall_DS-C30S-S11_12Man.md` |
| **Kịch bản test API — 4 controller / 32 màn** | Khu 2 → `doc/KichBan/KichBan_VideoWall_DS-C66S_4Controller_32Man.md` (+ giải thích topology & gọi API 4 khung: `doc/GiaiThich_KetNoi_VideoWall_DS-C66S-H88-CL.md`) |
| Xem thông số phần cứng DS-C30S-S11 | `doc/DS-C30S-S11_Datasheet_20250324.md` |

---

## KHU 1 — Reference thiết bị & ISAPI *(tài liệu tra cứu, ít đổi)*

| Mục | Nội dung |
|---|---|
| `doc/ISAPI-Videowall-Controller/` | Bộ tài liệu ISAPI controller: `README.md` → `00-api-catalog` → `01`…`10`; `09-api-reference.md` **1.7 MB — chỉ `grep` theo endpoint, không đọc nguyên file**; `VideoWall_ISAPI_API_List.md` |
| `data/logs-api/` | **Log đo thật trên thiết bị** (`session-*-real.json`) — nguồn sự thật cho response ISAPI |
| `doc/Controller-phan-cung/Controller-phan-cung.md` | Tài liệu phần cứng controller |
| `doc/TableSQL/Vw_Tables_Analysis_And_Design.md` | Phân tích & thiết kế bảng CSDL `Vw*` |
| `_source/` | PDF / XLSX gốc — **PDF chỉ cho người xem** (datasheet đã convert sang `doc/DS-C30S-S11_Datasheet_20250324.md`) |

## KHU 2 — Công cụ WPF (Live Mode: Thiết lập Scene & Tự động ghi Log) *(kế hoạch & vận hành)*

| File | Nội dung | Đọc khi |
|---|---|---|
| `doc/KichBan/KichBan_VideoWall_DS-C30S-S11_12Man.md` | 20 kịch bản test API (KB-01 → KB-20) cho **1 controller** DS-C30S-S11 / 12 màn lưới 4×3, toạ độ ảo 7680×5760, công thức ID, mã lỗi, runbook | Test/vận hành cấu hình 1 khung |
| `doc/KichBan/KichBan_VideoWall_DS-C66S_4Controller_32Man.md` | Kịch bản test API cho **4 controller** (1 trung tâm + 3 con) / 32 màn lưới 8×4. Có **KB-00 probe read-only** chạy tại hiện trường. Đi kèm `doc/GiaiThich_KetNoi_VideoWall_DS-C66S-H88-CL.md` (topology + cơ chế gọi API 4 khung, neo vào code `Module.VideoWall`) | Test/vận hành cấu hình 4 khung |
| `doc/Plan/videowall_plan.md` | Đặc tả yêu cầu gốc — gộp họp 19/08 + 25/08 (bản lịch sử, không sửa) | đối chiếu yêu cầu ban đầu |
| `doc/Transcript/transcript-videowall-28082026.md` | Transcript đầy đủ buổi họp chuẩn bị 28/08 (bản lịch sử, không sửa) | tra chi tiết ai nói gì |

## Tier Table

| Tier | File | Read | Size | Nội dung | Nguồn gốc |
|---|---|---|---|---|---|
| A | `doc/DS-C30S-S11_Datasheet_20250324.md` | full | 9 KB | Thông số kỹ thuật chi tiết controller DS-C30S-S11 | `_source/pdf/DS-C30S-S11_Datasheet_20250324.pdf` |
| A | `doc/GiaiThich_KetNoi_VideoWall_DS-C66S-H88-CL.md` | full | 28 KB | Giải thích kiến trúc topology & cơ chế kết nối 4 controller | Phân tích kỹ thuật nội bộ |
| A | `doc/SoDoCauHinh_VideoWall_DS-C66S-H88-CL.md` | full | 4 KB | Sơ đồ cấu hình hệ thống VideoWall DS-C66S trạm Hữu Nghị - Chi Lăng | `_source/img/1WUpuklkneHhPEFDdv0FV4AhHXWfmyWG6g361CUyP2VARkDsVwnCxuzMc2MkxEpJHUO.jpg` |
| A | `doc/Controller-phan-cung/Controller-phan-cung.md` | full | 48 KB | Tài liệu phần cứng controller Hikvision DS-C66S | `_source/pdf/Controller phần cứng.pdf` |
| A | `doc/ISAPI-Videowall-Controller/00-api-catalog.md` | full | 40 KB | Danh mục và bảng tra cứu API ISAPI cho controller | `_source/xlsx/VideoWall_ISAPI_API_List.xlsx` |
| A | `doc/ISAPI-Videowall-Controller/01-reading-guide.md` | full | 1 KB | Hướng dẫn cấu trúc tài liệu ISAPI | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| A | `doc/ISAPI-Videowall-Controller/02-overview.md` | full | 3 KB | Tổng quan giao thức và kiến trúc ISAPI | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| A | `doc/ISAPI-Videowall-Controller/03-isapi-framework.md` | full | 8 KB | Khung giao tiếp ISAPI, cơ chế chứng thực Digest | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| A | `doc/ISAPI-Videowall-Controller/04-quick-start-guide.md` | full | 61 KB | Hướng dẫn khởi động nhanh các lệnh điều khiển VideoWall | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| A | `doc/ISAPI-Videowall-Controller/05-device-management.md` | full | 65 KB | Các API quản trị thiết bị chung (thông tin, giờ, mạng) | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| A | `doc/ISAPI-Videowall-Controller/06-information-security.md` | full | 6 KB | Các API bảo mật thông tin và chứng chỉ | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| A | `doc/ISAPI-Videowall-Controller/07-video-general.md` | full | 11 KB | Các API video tổng quát và quản lý kênh video | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| A | `doc/ISAPI-Videowall-Controller/08-decoding-and-video-wall.md` | full | 7 KB | Hướng dẫn giải mã và quản lý VideoWall | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| B | `doc/ISAPI-Videowall-Controller/09-api-reference.md` | grep-only | 1.670 KB | Vendor spec đầy đủ các endpoint ISAPI Hikvision | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| A | `doc/ISAPI-Videowall-Controller/10-how-to-video-guidance.md` | full | 1 KB | Hướng dẫn tham khảo video hỗ trợ kỹ thuật Hikvision | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| A | `doc/ISAPI-Videowall-Controller/README.md` | full | 5 KB | Mục lục và danh sách sơ đồ Mermaid của bộ ISAPI | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` |
| A | `doc/ISAPI-Videowall-Controller/VideoWall_ISAPI_API_List.md` | full | 30 KB | Danh sách API ISAPI VideoWall tổng hợp | `_source/xlsx/VideoWall_ISAPI_API_List.xlsx` |
| A | `doc/KichBan/KichBan_VideoWall_DS-C30S-S11_12Man.md` | full | 45 KB | 20 kịch bản test API cho 1 controller DS-C30S-S11 / 12 màn | Kịch bản kiểm thử nội bộ |
| A | `doc/KichBan/KichBan_VideoWall_DS-C66S_4Controller_32Man.md` | full | 27 KB | Kịch bản test API cho 4 controller DS-C66S / 32 màn | Kịch bản kiểm thử nội bộ |
| A | `doc/Plan/videowall_plan.md` | full | 4 KB | Đặc tả yêu cầu gốc gộp các cuộc họp 19/08 + 25/08 (bản lịch sử) | Kế hoạch nội bộ |
| A | `doc/TableSQL/Vw_Tables_Analysis_And_Design.md` | full | 35 KB | Phân tích và thiết kế cấu trúc các bảng CSDL Vw* | Thiết kế kỹ thuật nội bộ |
| A | `doc/Transcript/transcript-videowall-28082026.md` | full | 75 KB | Transcript đầy đủ cuộc họp chuẩn bị ngày 28/08 (bản lịch sử) | Biên bản họp nội bộ |
| A | `data/logs-api/README.md` | full | 5 KB | Hướng dẫn đọc log đo thực tế và nhận diện mock | Ghi chú vận hành nội bộ |
| B | `data/logs-api/session-20260903-real.json` | grep-only | 1.635 KB | Log đo thực tế trên thiết bị DS-C66S ngày 03/09/2026 | Đo trực tiếp thiết bị trạm |
| B | `data/logs-api/session-20260904-real.json` | grep-only | 2.569 KB | Log đo thực tế trên thiết bị DS-C66S ngày 04/09/2026 | Đo trực tiếp thiết bị trạm |
| C | `_source/pdf/Controller phần cứng.pdf` | never | 5.298 KB | Tài liệu phần cứng gốc từ nhà sản xuất Hikvision | → bản `.md`: `doc/Controller-phan-cung/Controller-phan-cung.md` |
| C | `_source/pdf/DS-C30S-S11_Datasheet_20250324.pdf` | never | 790 KB | Datasheet gốc thiết bị DS-C30S-S11 | → bản `.md`: `doc/DS-C30S-S11_Datasheet_20250324.md` |
| C | `_source/pdf/ISAPI_Controller_Videowall Controller.pdf` | never | 6.201 KB | Tài liệu đặc tả ISAPI gốc 512 trang từ Hikvision | → bản `.md`: `doc/ISAPI-Videowall-Controller/` |
| C | `_source/xlsx/VideoWall_ISAPI_API_List.xlsx` | never | 32 KB | Bảng tính tổng hợp danh mục API ISAPI gốc | → bản `.md`: `doc/ISAPI-Videowall-Controller/VideoWall_ISAPI_API_List.md` |
| C | `_source/img/1WUpuklkneHhPEFDdv0FV4AhHXWfmyWG6g361CUyP2VARkDsVwnCxuzMc2MkxEpJHUO.jpg` | never | 496 KB | Ảnh sơ đồ kết nối VideoWall gốc | → bản `.md`: `doc/SoDoCauHinh_VideoWall_DS-C66S-H88-CL.md` |

---

*Con trỏ trong bộ nhớ: `.agents/memory/videowall-record-replay-plan.md`.*
