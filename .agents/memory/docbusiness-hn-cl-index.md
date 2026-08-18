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
- **`BusinessAnalysis/`** — `Business_Analysis_HN_CL_2.md`: Yêu cầu chức năng/phi chức năng toàn bộ 16 phân hệ (ITS Lõi, TMC-TMS, VMS, VDS, CCTV, Web, ShareData, VideoWall, Giám sát mạng, Thu phí...).
- **`ShareData/`** — `01-yeu-cau-nghiep-vu.md` (đọc trước khi sửa ShareDataWorker), `02-mapping-goi-tin-101-111.md` (mapping payload ↔ DB), `03-audit-20260812.md` (hiện trạng nợ kỹ thuật).
- **`VideoWall/`** — Đọc `HIKVISION_ISAPI_VIDEOWALL_GLOSSARY.md` đầu tiên. Bộ API ISAPI ở `ISAPI-Videowall-Controller/00→10`. File `09A-api-reference.md` rất lớn (1.6 MB) — chỉ `grep_search` theo endpoint cụ thể, không đọc nguyên file. Khi có mâu thuẫn giữa `09A` (spec hãng) và `09B-practical-guide-and-tested-responses.md` (đo thật trên DS-C66S-H88-CL) → ưu tiên `09B`.

## 3. Cách thức áp dụng khi nhận yêu cầu
1. Khi user hỏi về nghiệp vụ hoặc yêu cầu implement/sửa đổi tính năng liên quan đến phân hệ bất kỳ, **mở `DocBusinessThienAn/HữuNghị-ChiLăng/INDEX.md` trước** để xác định đúng tài liệu nghiệp vụ gốc.
2. Đối chiếu quy tắc nghiệp vụ trong tài liệu trước khi viết code hoặc đề xuất giải pháp.
