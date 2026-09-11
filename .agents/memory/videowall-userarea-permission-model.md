---
name: videowall-userarea-permission-model
description: VideoWall Tầng 3 (user-area) permission model uses Config JSON array of discrete {Col,Row} grid cells, not bounding-box, not ScreenIds — UserId is optional (OrgId-only rows valid)
metadata:
  type: project
---

Model phân quyền Tầng 3 (theo user/org, khác Tầng 1 org-scope theo controller) của VideoWall
(entity `VwUserAreaPermission`, `src/Modules/VideoWall/Module.VideoWall.Core/Entities/` trong
repo `TA-ITS015-WEBAPI-V1.0`) đã chốt **cuối cùng** là: cột `Config` — mảng JSON các ô lưới rời
rạc `{Col,Row}` (lưới 8×4, 0-based) — KHÔNG phải bounding-box (`ColStart/ColEnd/RowStart/RowEnd`,
model code có sẵn từ 10/09/2026 nhưng đã bị thay), và KHÔNG phải "ScreenIds" (mảng ID màn hình
qua `VwScreen.ID`, mô tả trong biên bản họp gốc).

`UserId` là **optional** — 1 dòng có `UserId` cụ thể (ghi đè cho user đó) HOẶC chỉ có `OrgId`
(áp dụng cho cả đơn vị), không đồng thời cả hai, không thiếu cả hai. Mỗi actor (1 UserId cụ thể,
hoặc 1 OrgId không kèm UserId) chỉ được có ĐÚNG 1 dòng ("CHECK DUPLICATE").

**Why:** Quyết định này dựa trên đối chiếu TRỰC TIẾP 2 nguồn khác thời điểm trong cùng ngày họp
11/09/2026: (1) ghi chú thô `DocBusinessThienAn/VideoWall-0911.txt` phác thảo đúng schema
`OrgId/UserId/Config` với ví dụ `Config: [{1,1},{1,2},{1,3}]`; (2) biên bản họp đầy đủ đã verify
`Videowall-script.md` §3, lời anh Sơn xác nhận trực tiếp "một record cấu hình chỉ chứa User hoặc
Org thôi... hoặc là phân quyền cho User cụ thể, hoặc là cho cả Org" — xác nhận bản ghi chỉ-có-OrgId
là tính năng có chủ đích, không phải dữ liệu rác. Model Config-array còn tốt hơn bounding-box vì
không bị giới hạn "phải gọn trong 1 hình chữ nhật" và không cần JOIN sang `VwScreen` như ScreenIds.

**How to apply:** Khi làm việc tiếp với `VwUserAreaPermission`/`VwUserAreaAccessService`, coi
Config-array + UserId-optional là model ĐÚNG và CUỐI CÙNG — không quay lại bounding-box hay
ScreenIds trừ khi có quyết định mới ghi đè rõ ràng. Plan chi tiết (đã implement, đã review) ở
`DocBusinessThienAn/HữuNghị-ChiLăng/Plan/videowall-user-area-permission-plan.md`. Xem
[[videowall-outstanding-issues-20260911]] cho danh sách bug CHƯA fix của phần CRUD này (Update
thiếu duplicate-check, UserId/OrgId chưa trim, thiếu trong checklist AllowAnonymous).
