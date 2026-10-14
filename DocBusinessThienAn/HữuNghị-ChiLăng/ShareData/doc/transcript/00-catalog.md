# Transcript — Mục lục (ShareData)

> Bản ghi `.md` chuyển thể từ các file ghi âm. ShareData không có thư mục `_source/audio/` cục bộ —
> audio gốc nằm chung ở [`../../../Plan/_source/audio/`](../../../Plan/_source/audio/) (xem bảng bên
> dưới cho từng file cụ thể).
> Audio gốc là Tier C (human-only, không vào git). Transcript ở đây là Tier A.

| Transcript | Audio nguồn | Ngày | Thời lượng | Chủ đề | Tier |
|---|---|---|---|---|---|
| [`12-10-2026-sharedata-va-scheduler-chung.md`](12-10-2026-sharedata-va-scheduler-chung.md) | `../_source/Đường Số 10.m4a` | 12-10-2026 | ~16 phút (trích xuất) | ShareData & Chuẩn hóa Component Lập lịch (Schedule) chung toàn hệ thống: Gom 1 component lịch duy nhất dùng `RadioButton` có props bật/tắt mode (liên tục, ngày, tuần, định kỳ); chuẩn hóa cấu trúc Tab phân cấp (theo chuẩn TMS quản lý); bóc tách tầng xử lý dữ liệu (JSON) và tầng tích hợp kết nối `HandleConnection` (`API`, `File`, `Kafka` convert `JSON` → `byte[]`); quản lý cấu hình kết nối theo đối tác trong `ShareDataPartner` (chốt 13/10 thay vì đề xuất `SystemConfig`); Dynamic Query load danh sách cột cho `SuperAdmin` có Validator & Audit Log | A |

> ⚙️ Bản ghi được gộp và chuẩn hóa theo quy chuẩn Tier A (Frontmatter Provenance, Mục 1 Tóm tắt cốt lõi, Mục 2 Kịch bản đối thoại chi tiết).
> Các nội dung liên quan phân hệ VideoWall (phân quyền User/Org, dựng cây Zone, layout ma trận) đã được tách riêng sang [`../../../VideoWall/doc/transcript/`](../../../VideoWall/doc/transcript/).
