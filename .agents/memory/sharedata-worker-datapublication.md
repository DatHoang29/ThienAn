---
name: sharedata-worker-datapublication
description: ShareData Worker outbound (DataPublicationService) — trạng thái thật của code + kế hoạch đang chốt cho luồng gửi
metadata:
  type: project
---

`DataPublicationService` (`TA-ITS015-WEBAPI-V1.0\src\Services\ShareData\ShareDataWorker`) là
dịch vụ xuất bản dữ liệu độc lập, KHÔNG phải scaffold rỗng — đã có metadata động
(`ShareDataPacket`/`ShareDataTable`), phễu ánh xạ cây (`ShareDataMapping.TargetShapeJson` với
`$field`/`$each`/`$extend.codeSet`/`$extend.expression`), CodeSet, watermark tăng dần, lease
chống trùng, so khớp `OrdinalIgnoreCase` xuyên suốt (đã resolve đúng yêu cầu họp 09/09 & 11/09/2026
về case-insensitive).

**Nhưng tầng lấy dữ liệu thô đang sai hướng**: `BuildQuery` (`DataPublicationService.Validation.cs`)
dựng SQL động bằng nối chuỗi từ `ShareDataTable.FieldsJson`/`JoinCondition` lúc chạy. Hiếu xác
nhận 2026-09-14: đây là hướng SAI, phải quay về hard-code kiểu `PacketQueryRegistry` (dictionary
key=packet code, value=hàm SqlSugar viết tay) — đúng ý định ghi sẵn trong comment "NGỪNG DÙNG —
nguồn dữ liệu do IPacketProvider trong mã nguồn quyết định" tại
`Module.ShareData.Core\Entities\ShareDataSubscription.cs:79-82`. `ShareDataPacket`/`ShareDataTable`
vẫn giữ nguyên vai trò nguồn cho tầng mapping/CodeSet/Shape/FE — chỉ tầng raw-query đổi sang
hard-code.

**Luồng gửi (outbound) hiện tại**: chỉ ghi file (`SaveExportFileAsync`), CHƯA gửi HTTP trực tiếp
cho đối tác dù biên bản họp yêu cầu ("gửi trực tiếp qua WebAPI, HTTP POST, không cần heartbeat
riêng" — lời Anh Sơn, không phải xây C2C TCP socket dù entity có field cho hướng đó). Đã chốt
2026-09-14: đợt này CHỈ để lại 1 comment TODO đánh dấu chỗ nối HTTP trong
`ExecuteExportForSubscription`, KHÔNG viết client thật — đợi cột `ShareDataPartner.OutboundApiUrl`
(cần Đạt bổ sung qua `EnableIncreTable`, theo [[do-not-modify-shared-sharedata-entities]]) và một
đợt riêng sau này.

Plan đầy đủ (4 mục: hard-code query registry / đánh dấu chỗ HTTP / lịch daily / switch auto-send
khi có data mới), đã duyệt 2026-09-14, lưu tại
`DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/doc/03-ke-hoach-luong-gui-outbound.md`. Theo
[[feedback-plan-then-handoff-local-files]]: chỉ research+plan, không tự sửa code — chờ Hiếu triển
khai.

**Lưu ý sync máy**: `do-not-modify-shared-sharedata-entities.md`,
`feedback-report-style-no-forensic-detail.md`, và 3 file `videowall-*.md` được `MEMORY.md` trỏ tới
nhưng KHÔNG tồn tại trong `.agents/memory/` trên máy này (2026-09-14) — có thể do junction
`~/.claude/projects/c--ThienAn/memory` bị hỏng/chưa tạo hoặc chưa đồng bộ từ máy khác. Cần kiểm
tra/tạo lại junction (snippet cuối `MEMORY.md`) hoặc chép lại các file đó từ máy đang giữ bản gốc.
