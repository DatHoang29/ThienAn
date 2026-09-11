# Transcript — Mục lục (VideoWall)

> Bản ghi `.md` chuyển thể từ các file ghi âm họp phân hệ VideoWall.
> Audio gốc là Tier C (human-only, không vào git). Transcript ở đây là Tier A.

| Transcript | Audio nguồn | Ngày | Thời lượng | Chủ đề | Tier |
|---|---|---|---|---|---|
| [`2026-08-28-videowall-chuan-bi.md`](2026-08-28-videowall-chuan-bi.md) | _(audio gốc 28/08 — không còn trong repo)_ | 2026-08-28 | ~50 phút (9.864 từ) | Chuẩn bị kịch bản mượn & kiểm thử thiết bị VideoWall với nhà thầu: test API bằng Postman/curl, backup cấu hình IP qua web/API, lịch 2 ngày, email duyệt chị Tuyền | A |
| [`2026-09-09-videowall-phan-quyen-va-layout.md`](2026-09-09-videowall-phan-quyen-va-layout.md) | `_source/audio/2026-09-09-videowall-phan-quyen-va-layout.m4a` | 2026-09-09 | 10:37 | Thảo luận VideoWall: Phân quyền kịch bản/màn hình theo User/Tổ chức, dựng cây Zone với SqlSugar ToTree & thiết kế layout ma trận kéo thả | A |
| [`2026-09-11-videowall-script.md`](2026-09-11-videowall-script.md) | `Plan/_source/audio/2026-09-11-sharedata-videowall-{1,2}.m4a` | 2026-09-11 | ~111 phút (hợp nhất) | Kiến trúc VideoWall 3 tầng: Loose-coupling qua NATS Message Broker, lý do tách riêng VideoWall Background Service giao tiếp ISAPI thiết bị (chống blocking WebAPI), 3 trụ cột (Config/Control/Status), cơ chế phân quyền màn hình theo User/Org (ưu tiên User > Org, mặc định Full quyền) | A |

> ⚙️ Bản ghi được chuyển thể và chuẩn hóa theo quy chuẩn Tier A (Frontmatter Provenance, Summary, Key Takeaways, Timeline Breakdown).\n