# Transcript — Mục lục (ShareData)

> Bản ghi `.md` chuyển thể từ các file ghi âm trong [`../../_source/audio/`](../../_source/audio/)
> Audio gốc là Tier C (human-only, không vào git). Transcript ở đây là Tier A.

| Transcript | Audio nguồn | Ngày | Thời lượng | Chủ đề | Tier |
|---|---|---|---|---|---|
| [`2026-09-09-review-sharedata.md`](2026-09-09-review-sharedata.md) | `_source/audio/2026-09-09-review-1.m4a`, `_source/audio/2026-09-09-review-2.m4a` | 2026-09-09 | ~61 phút | Review ShareData toàn diện: Luồng gửi/nhận file & API, cấu hình gói tin, SQL alias mapping đối tác, bổ sung xử lý theo giờ/time (9h sáng hàng ngày), bỏ cấu hình sự kiện/gửi 1 lần, switch tự động gửi khi có data mới, Socket Wrapper & xóa log đầu ngày | A |
| [`2026-09-11-sharedata-script.md`](2026-09-11-sharedata-script.md) | `Plan/_source/audio/2026-09-11-sharedata-videowall-{1,2}.m4a` | 2026-09-11 | ~111 phút (hợp nhất) | Chuẩn hóa cấu hình gói tin (gói 101,...): Bảng metadata trường dữ liệu động, cơ chế mapping 2 chiều (Outbound/Inbound), bộ mã quy đổi CodeSet, hàm tính toán SUM/AVG, format đầu ra, WebAPI + Background Service xử lý qua NATS/bảng đệm, tinh gọn UI modal, bảo toàn 9 gói tin cũ | A |

> ⚙️ Bản ghi được gộp và chuẩn hóa theo quy chuẩn Tier A (Frontmatter Provenance, Executive Summary, Key Takeaways, Timeline Breakdown).
> Các nội dung liên quan phân hệ VideoWall (phân quyền User/Org, dựng cây Zone, layout ma trận) đã được tách riêng sang [`../../../VideoWall/doc/transcript/`](../../../VideoWall/doc/transcript/).
