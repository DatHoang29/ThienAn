---
name: videowall-cascade-architecture
description: How the Hữu Nghị–Chi Lăng VideoWall DS-C66S cascade actually works and which docs are canonical
metadata: 
  node_type: memory
  type: project
  originSessionId: c445df9a-ca93-4a19-9ec1-f9a7fa8adc40
  modified: 2026-09-08T15:16:59.047Z
---

VideoWall trạm Hữu Nghị–Chi Lăng = **cascade 2 tầng DS-C66S**: 1 bộ trung tâm (compositor, S12) + 3 bộ con (fan-out 4K→FHD, S6), tường 8×4 = 32 màn 55" 1920×1080.

**Kết luận cốt lõi (đã tốn nhiều vòng làm rõ với user):** 4 khung **KHÔNG có giao tiếp điều khiển với nhau** — chỉ cáp video HDMI 4K (1 chiều) + cáp GENLOCK (vật lý). ISAPI của mỗi DS-C66S chỉ điều khiển tường của chính nó; không có master/slave.
- Backend `Module.VideoWall` chỉ nói ISAPI với **1 thiết bị = bộ trung tâm**. Không `foreach 4 controllers`, không cắt cửa sổ cho bộ con, không SID theo từng khung.
- Bộ trung tâm: 1 wall-logic phủ 32 màn qua 8 output; firmware tự chia canvas ra 8 cổng 4K.
- 3 bộ con: cấu hình lưới 2×2 tĩnh **một lần** qua Web UI của chính bộ con; trong DB chỉ là bản ghi kiểm kê (`VwController.Role="sub"`). Con đường backend→bộ con **duy nhất** = KB-17 (tắt màn qua serial).
- Luồng API cũ test với 1 con DS-C30S-S11 = luồng đích, chỉ trỏ vào bộ trung tâm + lưới 8×4 + canvas đọc từ `GET .../{wall}/outputs`. Cascade **không thêm lệnh API nào**.

**Nguồn sự thật:** ảnh gốc `DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/_source/img/thietkevideowall.jpg`.
**Doc chuẩn:** `.../VideoWall/doc/KienTruc_VideoWall_DS-C66S-Cascade.md` + `.../doc/KichBan/KichBan_VideoWall_DS-C66S_4Controller_32Man.md` (đã viết lại cho cascade, giữ tên file).
**Đã XOÁ vì sai:** `GiaiThich_KetNoi_VideoWall_DS-C66S-H88-CL.md` (§2C/§3c mô tả sai mô hình "backend fan-out xuống 4 khung") + `SoDoCauHinh_VideoWall_DS-C66S-H88-CL.md`.

Response ISAPI thiết bị thật: `.../VideoWall/data/logs-api/session-20260904-real.json` dòng 955–1056 = chuỗi setup-scene đầy đủ (`DeleteAllWindows → AddWindow → SetScene → SaveSceneData (fallback CreateScene khi 403) → ActivateScene`). `baseOutputSize=1920`, `isSupportPlan=false`.

`VwCircuitBreaker` (`CacheConst.cs`) KHÔNG phải code thừa — là circuit-breaker per-IP gọi ở mọi request ISAPI thật (`VwISAPIDeviceClient.cs`), khoá IP sau 2 lần auth-fail để tránh kích hoạt khoá cứng firmware.

Xem [[videowall-cascade-status]] cho tiến độ.
