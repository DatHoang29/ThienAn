# VideoWall — Chỉ mục tài liệu

Thư mục này gồm **2 khu** khác mục đích. Xác định bạn đang làm gì rồi vào đúng khu.

| Bạn đang… | Đọc |
|---|---|
| Tìm hiểu nghiệp vụ / yêu cầu dự án | Khu 2 → `videowall_plan.md`, `transcript-videowall-28082026.md` |
| Tra bộ lệnh ISAPI / kiểu response thiết bị | Khu 1 → `ISAPI-Videowall-Controller/` (đọc `README.md` trước) |
| Tìm hiểu kiến trúc công cụ WPF (Thiết lập Scene & Auto-Log) | Khu 2 → `videowall-kien-truc-van-hanh.md` |
| Thử công cụ tại máy (không có thiết bị) | Khu 2 → `videowall-test-local.md` |
| Chuẩn bị đi test 2 ngày | Khu 2 → `videowall-test-2ngay.md` |
| Xem thông số phần cứng DS-C30S-S11 | `_source/DS-C30S-S11_Datasheet_20250324.md` |

---

## KHU 1 — Reference thiết bị & ISAPI *(tài liệu tra cứu, ít đổi)*

| Mục | Nội dung |
|---|---|
| `ISAPI-Videowall-Controller/` | Bộ tài liệu ISAPI controller: `README.md` → `00-api-catalog` → `01`…`10`; `09A-api-reference.md` **1.7 MB — chỉ `grep` theo endpoint, không đọc nguyên file**; `09B-practical-guide-and-tested-responses.md` = đo thật (ưu tiên `09B` khi mâu thuẫn `09A`); `VideoWall_ISAPI_API_List.md` |
| `KeyWord/HIKVISION_ISAPI_VIDEOWALL_DICTIONARY.md` | Bảng thuật ngữ ISAPI VideoWall — **đọc đầu tiên** khi mới vào |
| `API/README.md` | Ghi chú API |
| `Controller-phan-cung/Controller-phan-cung.md` | Tài liệu phần cứng controller |
| `TableSQL/Vw_Tables_Analysis_And_Design.md` | Phân tích & thiết kế bảng CSDL `Vw*` |
| `_source/` | PDF / XLSX gốc + `DS-C30S-S11_Datasheet_20250324.md` (datasheet đã convert) — **PDF chỉ cho người xem** |

## KHU 2 — Công cụ WPF (Live Mode: Thiết lập Scene & Tự động ghi Log) *(kế hoạch & vận hành)*

| File | Nội dung | Đọc khi |
|---|---|---|
| `videowall-kien-truc-van-hanh.md` | **Tài liệu chính.** Phần A bối cảnh & quyết định (chỉ 2 tầng WPF ↔ Thiết bị) · B cơ chế & thiết kế (Auto session log, chuỗi handler Digest, layout Response động) · C runbook (Vận hành tại hiện trường & Kịch bản) | luôn bắt đầu ở đây |
| `videowall-test-local.md` | 2 cách chạy thử công cụ tại máy không cần thiết bị: Bộ test tự động .NET · MockServer riêng (`scripts/VwMockServerRunner`) + app WPF (đầy đủ các bước kiểm thử) | thử sản phẩm trước |
| `videowall-test-2ngay.md` | Checklist tick-box đi test 2 ngày tại TCB: Ngày 1 / Ngày 2 / "thế nào là đủ" | trước & trong chuyến đi |
| `videowall_plan.md` | Đặc tả yêu cầu gốc — gộp họp 19/08 + 25/08 (bản lịch sử, không sửa) | đối chiếu yêu cầu ban đầu |
| `transcript-videowall-28082026.md` | Transcript đầy đủ buổi họp chuẩn bị 28/08 (bản lịch sử, không sửa) | tra chi tiết ai nói gì |

---

*Con trỏ trong bộ nhớ: `.agents/memory/videowall-record-replay-plan.md`.*
