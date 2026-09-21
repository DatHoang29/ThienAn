# Plan — Kế hoạch Triển khai & Kho Ghi âm Tập trung Toàn Tuyến

Thư mục quản lý kế hoạch triển khai tổng thể và là **Kho lưu trữ tệp ghi âm tập trung (Audio Hub)** của toàn bộ dự án **Cao tốc Hữu Nghị – Chi Lăng (HN-CL)**.

> 🔴 **Single Source of Truth (SSOT)**: File này là điểm vào quản lý danh mục tài liệu kế hoạch và toàn bộ file audio gốc phân loại theo thư mục ngày (`dd-mm-yyyy`).

---

## Tier Table

| Tier | File | Read | Size | Nội dung | Nguồn gốc |
|---|---|---|---|---|---|
| A | `doc/transcript/00-catalog.md` | full | 2 KB | Mục lục toàn bộ bản ghi cuộc họp kế hoạch toàn tuyến | Biên soạn nội bộ |
| C | `_source/audio/08-09-2026/08-09-2026-hop-ke-hoach-1.m4a` | never | 7.8 MB | Audio cuộc họp kế hoạch toàn tuyến phần 1 (08:00) | → bản `.md`: `doc/transcript/08-09-2026-hop-ke-hoach-1.md` |
| C | `_source/audio/08-09-2026/08-09-2026-hop-ke-hoach-2.m4a` | never | 40.0 MB | Audio cuộc họp kế hoạch toàn tuyến phần 2 (40:00) | → bản `.md`: `doc/transcript/08-09-2026-hop-ke-hoach-2.md` |
| C | `_source/audio/09-09-2026/09-09-2026-review-sharedata-phan-1.m4a` | never | 17 MB | Audio cuộc họp Review ShareData phần 1 (35:36) | → bản `.md`: `../ShareData/doc/transcript/09-09-2026-review-sharedata.md` |
| C | `_source/audio/09-09-2026/09-09-2026-review-sharedata-phan-2.m4a` | never | 12 MB | Audio cuộc họp Review ShareData phần 2 (25:41) | → bản `.md`: `../ShareData/doc/transcript/09-09-2026-review-sharedata.md` |
| C | `_source/audio/09-09-2026/09-09-2026-videowall-phan-quyen-va-layout.m4a` | never | 5.0 MB | Audio cuộc họp VideoWall phân quyền và layout (10:37) | → bản `.md`: `../VideoWall/doc/transcript/09-09-2026-videowall-phan-quyen-va-layout.md` |
| C | `_source/audio/11-09-2026/11-09-2026-sharedata-videowall-1.m4a` | never | 55 MB | Audio cuộc họp ShareData & VideoWall phần 1 (01:01:37) | → bản `.md`: `../ShareData/doc/transcript/11-09-2026-sharedata-script.md`, `../VideoWall/doc/transcript/11-09-2026-videowall-script.md` |
| C | `_source/audio/11-09-2026/11-09-2026-sharedata-videowall-2.m4a` | never | 30 MB | Audio cuộc họp ShareData & VideoWall phần 2 (50:11) | → bản `.md`: `../ShareData/doc/transcript/11-09-2026-sharedata-script.md`, `../VideoWall/doc/transcript/11-09-2026-videowall-script.md` |
| C | `_source/audio/16-09-2026/16-09-2026-review-frontend-sharedata.m4a` | never | 60.5 MB | Audio cuộc họp Review Frontend & Ánh xạ dữ liệu ShareData (32:41) | → bản `.md`: `../ShareData/doc/transcript/16-09-2026-review-frontend-sharedata.md` |
| C | `_source/audio/16-09-2026/16-09-2026-sua-ui-sharedata.m4a` | never | 11.8 MB | Audio cuộc họp Recap & Thống nhất danh mục Sửa UI ShareData (06:13) | → bản `.md`: `../ShareData/doc/transcript/16-09-2026-sua-ui-sharedata.md` |
| C | `_source/audio/16-09-2026/16-09-2026-refactor-backend-sharedata-worker.m4a` | never | 8.0 MB | Audio cuộc họp Định hướng Refactor Backend Worker ShareData (04:12) | → bản `.md`: `../ShareData/doc/transcript/16-09-2026-refactor-backend-sharedata-worker.md` |
| C | `_source/audio/16-09-2026/16-09-2026-dinh-danh-doi-tac-va-test-tai.m4a` | never | 81.3 MB | Audio cuộc họp Định danh đối tác, bỏ phong bì PDU, test tải đa đối tác (16:47) | → bản `.md`: `../ShareData/doc/transcript/16-09-2026-dinh-danh-doi-tac-va-test-tai.md` |
| C | `_source/audio/19-09-2026/19-09-2026-sharedata-http-header-phan-1.m4a` | never | 1.6 MB | Audio cuộc họp thảo luận đưa thông tin định danh vào HTTP Header phần 1 (03:13) | → bản `.md`: `../ShareData/doc/transcript/19-09-2026-truyen-thong-tin-qua-http-header-luong-gui.md` |
| C | `_source/audio/19-09-2026/19-09-2026-sharedata-http-header-phan-2.m4a` | never | 200 KB | Audio cuộc họp thảo luận đưa thông tin định danh vào HTTP Header phần 2 (00:23) | → bản `.md`: `../ShareData/doc/transcript/19-09-2026-truyen-thong-tin-qua-http-header-luong-gui.md` |
| C | `_source/audio/21-09-2026/21-09-2026-sharedata-phan-1-ui-mapping.m4a` | never | 2.8 MB | Audio cuộc họp Phần 1: UI & Luồng Mapping (05:53) | → bản `.md`: `../ShareData/doc/transcript/21-09-2026-hoan-thien-mapping-va-watermark-sharedata.md` |
| C | `_source/audio/21-09-2026/21-09-2026-sharedata-phan-2-watermark-cdc.m4a` | never | 8.3 MB | Audio cuộc họp Phần 2: Watermark & CDC (17:15) | → bản `.md`: `../ShareData/doc/transcript/21-09-2026-hoan-thien-mapping-va-watermark-sharedata.md` |
| C | `_source/audio/21-09-2026/21-09-2026-sharedata-phan-3-mapping-profile.m4a` | never | 4.0 MB | Audio cuộc họp Phần 3: Sinh mã hồ sơ & luồng 1 chiều (08:17) | → bản `.md`: `../ShareData/doc/transcript/21-09-2026-hoan-thien-mapping-va-watermark-sharedata.md` |
| C | `_source/audio/21-09-2026/21-09-2026-sharedata-phan-4-tong-ket.m4a` | never | 1.6 MB | Audio cuộc họp Phần 4: Tổng kết & chốt luồng (03:21) | → bản `.md`: `../ShareData/doc/transcript/21-09-2026-hoan-thien-mapping-va-watermark-sharedata.md` |
| C | `_source/audio/21-09-2026/21-09-2026-sharedata-clip-watermark.m4a` | never | 17.0 MB | Audio clip trích đoạn Watermark nối đuôi (09:09) | → bản `.md`: `../ShareData/doc/transcript/21-09-2026-hoan-thien-mapping-va-watermark-sharedata.md` |
