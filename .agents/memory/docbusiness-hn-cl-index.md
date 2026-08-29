---
type: reference
created: 2026-08-14
updated: 2026-08-18
---

# Tài liệu Nghiệp vụ Chính Dự án Cao Tốc Hữu Nghị – Chi Lăng (HN-CL)

> 🔴 **NGUYÊN TẮC BẮT BUỘC**: Mỗi khi cần tìm hiểu, tra cứu, đối chiếu hay xác minh logic/luồng nghiệp vụ của dự án Thiên An (Hữu Nghị – Chi Lăng), **BẮT BUỘC ĐỌC `DocBusinessThienAn/HữuNghị-ChiLăng/INDEX.md` ĐẦU TIÊN** làm luồng nghiệp vụ chính và nguồn sự thật (Single Source of Truth). Tuyệt đối không tự suy đoán đường dẫn hoặc logic nếu chưa đối chiếu `INDEX.md`.

## 1. Quy ước phân loại file (áp dụng toàn bộ kho `DocBusinessThienAn/`)
- 🤖 **File `.md/.json/.sql` ở gốc mỗi chủ đề** (`HN-CL/<Phân hệ>/`): AI đọc trực tiếp, chứa toàn bộ đặc tả nghiệp vụ chuẩn.
- 👤 **`_source/` (PDF/XLSX/zip) và `images/`**: Chỉ dành cho người xem, **KHÔNG nạp vào ngữ cảnh AI** (đã có bản `.md` tương ứng).

## 2. Cấu trúc các phân hệ nghiệp vụ chính (tra cứu qua INDEX.md)
- **`ShareData/`** — `01-yeu-cau-nghiep-vu.md` (đọc trước khi sửa ShareDataWorker), `02-mapping-goi-tin-101-111.md` (mapping payload ↔ DB), `03-audit-20260812.md` (hiện trạng nợ kỹ thuật).
- **`VideoWall/`** — Mở `VideoWall/README.md` để biết đọc gì (chia 2 khu). **Khu 1 reference ISAPI**: đọc `KeyWord/HIKVISION_ISAPI_VIDEOWALL_GLOSSARY.md` đầu tiên; bộ API ISAPI ở `ISAPI-Videowall-Controller/00→10`; `09A-api-reference.md` rất lớn (1.7 MB) — chỉ `grep` theo endpoint, không đọc nguyên file; mâu thuẫn `09A` (spec hãng) vs `09B` (đo thật) → ưu tiên `09B`. **Khu 2 công cụ WPF**: `videowall-record-replay.md` (Ghi/Phát lại — tài liệu chính), `videowall-test-2ngay.md` (checklist đi test), `videowall_plan.md` + `transcript-videowall-28082026.md` (gốc).

## 3. Cách thức áp dụng khi nhận yêu cầu
1. Khi user hỏi về nghiệp vụ hoặc yêu cầu implement/sửa đổi tính năng liên quan đến phân hệ bất kỳ, **mở `DocBusinessThienAn/HữuNghị-ChiLăng/INDEX.md` trước** để xác định đúng tài liệu nghiệp vụ gốc.
2. Đối chiếu quy tắc nghiệp vụ trong tài liệu trước khi viết code hoặc đề xuất giải pháp.
