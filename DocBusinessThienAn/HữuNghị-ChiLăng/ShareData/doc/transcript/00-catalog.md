# Transcript — Mục lục (ShareData)

> Bản ghi `.md` chuyển thể từ các file ghi âm. ShareData không có thư mục `_source/audio/` cục bộ —
> audio gốc nằm chung ở [`../../../Plan/_source/audio/`](../../../Plan/_source/audio/) (xem bảng bên
> dưới cho từng file cụ thể).
> Audio gốc là Tier C (human-only, không vào git). Transcript ở đây là Tier A.

| Transcript | Audio nguồn | Ngày | Thời lượng | Chủ đề | Tier |
|---|---|---|---|---|---|
| [`19-09-2026-truyen-thong-tin-qua-http-header-luong-gui.md`](19-09-2026-truyen-thong-tin-qua-http-header-luong-gui.md) | `../../../Plan/_source/audio/19-09-2026/19-09-2026-sharedata-http-header-phan-1.m4a`, `../../../Plan/_source/audio/19-09-2026/19-09-2026-sharedata-http-header-phan-2.m4a` | 19-09-2026 | ~03 phút 37 giây (hợp nhất) | Bổ sung thông tin định danh (`PartnerCode`, `Mapping`) vào HTTP Header khi gọi API luồng Gửi; phía nhận bóc tách Header và tải danh mục Mapping lên RAM để tra cứu nhanh; giữ nguyên Body là mảng payload thuần túy | A |
| [`21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md`](21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md) | `../../../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-phan-{1,2,3,4}*.m4a` | 21-09-2026 | ~35 phút (hợp nhất) | Hoàn thiện Cấu hình Ánh xạ & Cơ chế Gửi nối đuôi ShareData: Chuẩn hóa luồng tạo ánh xạ 1 chiều (Đối tác -> Gói tin -> Hồ sơ ánh xạ), tự động sinh mã hồ sơ ánh xạ, hiển thị trạng thái ánh xạ trên bảng gói tin, cơ chế gửi nối đuôi theo mốc đánh dấu (`LastTime`/`LastKey`), phát hiện dữ liệu mới tức thì (Change Tracking + NATS) | A |
| [`12-10-2026-sharedata-va-scheduler-chung.md`](12-10-2026-sharedata-va-scheduler-chung.md) | `../_source/Đường Số 10.m4a` | 12-10-2026 | ~16 phút (trích xuất) | ShareData & Chuẩn hóa Component Lập lịch (Schedule) chung toàn hệ thống: Gom 1 component lịch duy nhất dùng `RadioButton` có props bật/tắt mode (liên tục, ngày, tuần, định kỳ), chuẩn hóa cấu trúc Tab phân cấp (theo chuẩn TMS quản lý), bóc tách tầng xử lý dữ liệu (JSON) và tầng tích hợp kết nối `HandleConnection` (`API`, `Kafka` convert `JSON` → `byte[]`, `File`), khai báo `Type` kết nối trong `SystemConfig` & `SystemConfigData`, Dynamic Query load danh sách cột cho `SuperAdmin` có Validator & Audit Log | A |

> ⚙️ Bản ghi được gộp và chuẩn hóa theo quy chuẩn Tier A (Frontmatter Provenance, Mục 1 Tóm tắt cốt lõi, Mục 2 Kịch bản đối thoại chi tiết).
> Các nội dung liên quan phân hệ VideoWall (phân quyền User/Org, dựng cây Zone, layout ma trận) đã được tách riêng sang [`../../../VideoWall/doc/transcript/`](../../../VideoWall/doc/transcript/).
