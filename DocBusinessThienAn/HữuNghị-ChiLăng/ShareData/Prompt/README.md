# ShareData — Prompt

Thư mục này chứa **prompt thực thi** cho phân hệ ShareData (`<task-slug>-prompt.md`). Sau khi một task đã triển khai, kiểm thử và nghiệm thu xong, prompt được giữ lại để lập trình viên review và đối chiếu sau khi code change hoàn tất (chỉ xóa khi người dùng trực tiếp yêu cầu theo `.agents/rules/thienan_rules.md`).

Tài liệu sống nằm ở [`../Plan/`](../Plan/), không đặt trong thư mục này.

### Chú giải ký hiệu

| Ký hiệu | Ý nghĩa |
| --- | --- |
| ✅ | Đạt — khớp / đúng / đã hoàn tất |
| ⚠️ | Cần lưu ý — có vấn đề, nhưng chưa phải dừng lại để sửa; ghi nhận rồi xử lý sau |
| ❌ | Không đạt — không khớp / không đúng / không áp dụng được |
| 🔴 | Rủi ro nghiêm trọng — phải xử lý trước khi đi tiếp |
| ⛔ | Cấm tuyệt đối |
| 📌 | Ghi chú bối cảnh — nguồn dữ liệu, ngày đo, thuật ngữ |

> Ký hiệu chỉ nói **mức độ**; trục đánh giá do tiêu đề cột của từng bảng nói rõ.

## Danh sách Prompt

*(Hiện tại toàn bộ prompt đã được hoàn thành, kiểm chứng và xoá theo yêu cầu của người dùng).*

### Quyết định hiện hành

- Cơ chế phát hiện dữ liệu mới (Event-Driven CT + NATS) đã hoàn thành trong tuần (22/09/2026) song song với luồng gửi nối đuôi.
- Cursor lưu ở bảng `ShareDataLastSend`, không thêm `LastDataId` vào `ShareDataSubscription`.
- **Gói 106 — đảo ngược quyết định lọc `Source` (25/09/2026):** policy vẫn `AlwaysIncremental`, gửi đủ 7 trường thật (`detectTime`, `lane`, `locationCode`, `speed`, `height`, `width`, `length`); 4 trường tải trọng `grossWeight`/`axleWeights`/`axleCount`/`isOverweight` để `null` vì `TmsTrafficData` không có cột nào cho chúng — tầng ánh xạ tự trả null, không cần lọc gì thêm. ⛔ **Không** lọc `Source`, ⛔ **không** có `SourceAllowList`, ⛔ **không** còn `AND 1=0`. Quyết định `AND 1=0` (chốt 24/09) đã bị chính chủ dự án bãi bỏ 25/09/2026 vì nó chặn nhầm cả 7 trường có dữ liệu thật — xem đầy đủ ở bảng lịch sử bên dưới. Khi có bảng/cột WIM thật thì bổ sung vào `SELECT`.
- Bảo đảm giao hàng là **at-least-once**; tin xóa và `Idempotency-Key` vẫn hoãn.
- Snapshot 101 và 105 còn vấn đề khối lượng query độc lập; prompt nối đuôi không tuyên bố đã sửa hai vấn đề đó.
- **Đua tranh đồng thời (concurrent trigger) — rà soát 25/09/2026**: tính đúng đắn khi nhiều lần bắn NATS / nhiều instance service xử lý đồng thời cùng 1 Subscription do khoá OCC `LockedSubscription` đảm bảo (`DataOutboundService.cs:527`, UPDATE nguyên tử qua cột `NextTimeRun`, dùng chung cho cả luồng định kỳ và luồng NATS trigger) — đúng 1 lần export, các lần thua giành lock im lặng bỏ qua (không log, không ghi `ShareDataAlertLog`). `DebounceSec` chỉ là bộ lọc sơ bộ chạy **trước** khi giành lock, dùng snapshot bộ nhớ nên có khe hở đua nhau thật — không phải điểm chốt tính đúng đắn. ⚠️ **Chưa có test kiểm chứng trực tiếp kịch bản đua tranh đồng thời thật** (kiểu bắn N lời gọi song song cùng lúc) — test gần nhất `ChangeTracking_WhenLockHeldByActiveWorker_DoesNotStealLockAndLeavesNextTimeRunIntact_Test` (`DataChangeWorkerTests.cs:782`) chỉ dựng sẵn lock đang bị giữ (set `NextTimeRun` tương lai) rồi gọi `ProcessSubscriptions(packetCode, ...)` đúng 1 lần tuần tự, không phải đua tranh thật.

## Đã thực thi và xoá theo Auto-Cleanup

| Prompt | Kết quả |
|---|---|
| `sharedata-don-dead-code-getminvalidversion-prompt.md` | ✅ **26/09/2026** · Xoá dead code `GetMinValidVersion`/`SqlMinValidVersion` khỏi `DataTrackerWorker.cs` (không còn được gọi trong code sản xuất sau khi Self-Healing chốt hướng reactive qua `IsChangeTrackingVersionInvalid`) và bài test tương ứng, toàn bộ test ShareData PASS 100% |
| `sharedata-doi-ten-checkpoint-thanh-lastsend-trong-dataoutboundservice-prompt.md` | ✅ **26/09/2026** · Đổi tên đồng bộ họ `Checkpoint` → `LastSend` trong `DataOutboundService.cs` khớp với entity `ShareDataLastSend`: `GetOrInitCheckpoint`→`GetOrInitLastSend`, `UpdateCheckpoint`→`UpdateLastSend`, field tuple `Checkpoint`→`LastSend`, biến `checkpoint`/`newCheckpoint`→`lastSend`/`newLastSend`, toàn bộ test ShareData PASS 100% |
| `sharedata-di-chuyen-writefailurelogs-buildfailexport-resolvepdutype-prompt.md` | ✅ **26/09/2026** · Chuyển `WriteFailureLogsAsync`→`WriteFailureLogs`, `BuildFailExportAsync`→`BuildFailExport`, `ResolvePduType` từ `DataOutboundService.cs` sang `ShareDataTransferLog` (đúng nguyên tắc 1 nơi duy nhất ghi log); `WriteActivityAsync` tự fallback `pduType ?? ResolvePduType(sub)` khi caller không truyền, chiều Inbound (`packet.PduType`) không đổi hành vi, toàn bộ test ShareData PASS 100% |
| `sharedata-doi-ten-processbatchsubscriptions-processpackettrigger-prompt.md` | ✅ **25/09/2026** · Đổi tên `ProcessBatchSubscriptions` → `ProcessScheduledSubscriptions`, `ProcessPacketTrigger` → `ProcessTriggerSubscriptions` đối xứng và chuẩn hoá kiến trúc, 128 tests PASS 100%. ⚠️ **Rà lại 25/09/2026**: đã kiểm chứng trực tiếp `IDataOutboundService.cs`/`DataOutboundService.cs` hiện tại — một đợt chỉnh sửa sau đó (chưa commit, không có prompt riêng) đã gộp lại thành 2 overload cùng tên `ProcessSubscriptions(CancellationToken)` / `ProcessSubscriptions(string packetCode, CancellationToken)`. Đây là trạng thái thật nhất quán 100% trên toàn bộ code + test (đã `search` toàn repo, không còn dấu vết `ProcessScheduledSubscriptions`/`ProcessTriggerSubscriptions` nào), không phải lỗi dở dang — chỉ là dòng bảng này chưa được cập nhật theo |
| `sharedata-doi-ten-dataoutboundcursor-thanh-dataoutboundlastread-prompt.md` | ✅ **25/09/2026** · Đổi tên `DataOutboundCursor` → `DataOutboundLastRead` đồng nhất với `ShareDataLastSend`, 6 vị trí code & test, 128 tests PASS 100% |
| `sharedata-bo-sung-kiem-tra-cancellation-pollchangetracking-prompt.md` | ✅ **25/09/2026** · Bổ sung kiểm tra Cancellation hợp tác (`stoppingToken.ThrowIfCancellationRequested()`) trong `PollChangeTracking` của `DataTrackerWorker` |
| `sharedata-tu-phuc-hoi-change-tracking-min-valid-version-prompt.md` | ✅ **25/09/2026** · Bổ sung cơ chế Self-Healing tự phục hồi khi mốc version nhỏ hơn `CHANGE_TRACKING_MIN_VALID_VERSION` (mã lỗi 22114/22115): fast-forward `_lastProcessedVersion = currentVersion.Value`, `IsChangeTrackingVersionInvalid`, unit tests (hàm `GetMinValidVersion` chủ động ban đầu đã được xoá ngày 26/09 do dead code khi chốt cơ chế reactive) |
| `toi-uu-scope-processbatchsubscriptions-prompt.md` | ✅ **25/09/2026** · Tối ưu cấp phát `IServiceScope` trong `ProcessBatchSubscriptions`: gom 1 scope chung cho toàn bộ batch, trong vòng `foreach` chỉ clone `using var subDb = baseClient.CopyNew()`, 130 tests PASS 100% |
| `doi-ten-legacylasttimerun-thanh-lasttimerun-prompt.md` | ✅ **25/09/2026** · Đổi tên tham số `legacyLastTimeRun` → `lastTimeRun` trong 2 overload của `GetOrInitializeCheckpoint` (bỏ tiền tố legacy thừa, gọn code) |
| `bo-alert-locklost-prompt.md` | ✅ **25/09/2026** · Bỏ `WriteAlertAsync(LockLost)` khỏi `LockedSubscription` — chỉ giữ log ứng dụng `LogWarningMsg`, không ghi vào `ShareDataAlertLog` nữa vì đây là sự kiện nội bộ cơ chế khoá, không phải lỗi luồng gửi dữ liệu |
| `doi-ten-claimed-thanh-lockedrows-prompt.md` | ✅ **25/09/2026** · Đổi tên biến `claimed` → `lockedRows` trong `LockedSubscription` (đúng vần "lock" đã chốt, rõ đây là số dòng chứ không phải cờ boolean) |
| `gop-lai-logging-vao-dataoutboundservice-prompt.md` | ✅ **25/09/2026** · Đảo ngược `tach-file-logging-dataoutboundservice-prompt.md`: gộp 4 hàm log trở lại cuối `DataOutboundService.cs`, xoá file `DataOutboundService.Logging.cs` (tuân thủ Rule 7 về vị trí private methods) |
| `tach-file-logging-dataoutboundservice-prompt.md` | ✅ **25/09/2026**, ⛔ **ĐẢO NGƯỢC 25/09/2026** · Tách 4 hàm log riêng sang partial class `DataOutboundService.Logging.cs`. Đã bị đảo ngược và gộp lại cuối `DataOutboundService.cs` theo `gop-lai-logging-vao-dataoutboundservice-prompt.md` để tuân thủ Rule 7 (phương thức private nằm cuối class) |
| `bo-sung-summary-dataoutboundservice-prompt.md` | ✅ **25/09/2026** · Bổ sung 9 khối XML summary cho 8 field khai báo và constructor trong `DataOutboundService.cs` |
| `doi-ten-processsubscriptionunderlock-prompt.md` | ✅ **25/09/2026** · Đổi tên hàm private `ProcessSubscriptionUnderLock` thành `LockedSubscription` tại 3 vị trí trong `DataOutboundService.cs`, đồng bộ tài liệu |
| `doi-ten-lease-thanh-lock-prompt.md` | ✅ **25/09/2026** · Đổi toàn bộ thuật ngữ "lease" thành "lock" xuyên suốt cơ chế độc quyền xử lý Subscription: `ReleaseLock`, `ProcessSubscriptionUnderLock`, `LockLost` (giữ nguyên chuỗi `"ESH-1303"`), `DefaultLockBudgetPercent`, cập nhật 8 bài test và log messages |
| `goi-106-go-test-khoa-chan-prompt.md` | ✅ **25/09/2026** · Gỡ bỏ 2 bài test khoá chặn cứng `AND 1=0` của gói 106 (`WhenSourceIsVdsCamera_...`, `WhenSourceIsAny_...`), giữ nguyên các bài test hợp lệ, toàn bộ test gói 106 chuyển xanh |
| `bo-codealiases-doi-pho-service-prompt.md` | ✅ **25/09/2026** · Dọn rỗng `CodeAliases`, '105_rfidData' phân giải tự nhiên qua tiền tố số, '104_rfidData' lộ sai thành 104, không đối phó |
| `goi-106-loc-nguon-wim-prompt.md` | ✅ **24/09/2026** · Chặn gói 106 gửi sai: thêm allow-list `TmsTrafficData.Source` mặc định rỗng (`[]`), thoát sớm bằng C# và ghi log warning khi rỗng, lọc `Source IN` khi có phần tử, 4 trường tải trọng giữ null chờ trạm cân hoạt động, 203 tests PASS 100% |
| `goi-106-loc-source-cung-trong-code-prompt.md` | ✅ **24/09/2026**, ⛔ **BÃI BỎ 25/09/2026** · Thay allow-list ở trên bằng lọc cứng `AND 1=0` + 3 dòng log vô điều kiện trong `QueryPacket106`. Chủ dự án phát hiện 25/09: 4 trường tải trọng vốn không nằm trong `SELECT` nên đã tự trả `null`, không cần lọc gì — `AND 1=0` chặn nhầm luôn cả 7 trường có dữ liệu thật. Đã gỡ ở [`goi-106-go-test-khoa-chan-prompt.md`](./goi-106-go-test-khoa-chan-prompt.md) (phần code) — file prompt gốc đã bị xoá trước khi phát hiện lỗi nên không còn để đánh dấu superseded |
| `goi-105-doc-tolltransactionin-prompt.md` | ✅ **24/09/2026** · Gói 105 chuyển sang đọc trực tiếp bảng `TollTransactionIn` (TOP 100 mới nhất), xử lý `NULLIF` cho biển số chuỗi rỗng về null, 201 tests PASS 100% |
| `don-dau-vet-104-rfiddata-prompt.md` | ✅ **24/09/2026** · Dọn lời mô tả về `104_rfidData` sau khi bản ghi bị xoá, giữ bí danh làm lưới chặn hồi quy, đổi tên 2 bài test, 200 tests PASS 100% |
| `gioi-han-so-dong-goi-ban-chup-prompt.md` | ✅ **24/09/2026** · Giới hạn 100 dòng cho gói bản chụp 105 (áp dụng TOP 100 ở tầng Service, ORDER BY exitTime DESC), 200 tests PASS 100% |
| `thong-nhat-phan-giai-ma-goi-o-resolveactivepacket-prompt.md` | ✅ **23/09/2026** · Thống nhất phân giải mã gói ở `ResolveActivePacket` qua catalog SQL, xoá `ExtractPacketNumber` |
| `sharedata-codeset-cau-truc-moi-prompt.md` | ✅ **18/09/2026** · việc **C1** |
| `sharedata-khung-gio-lich-gui-prompt.md` | ✅ **18/09/2026** · việc **S1** |
| `sharedata-go-duong-nap-packetfield-prompt.md` | ✅ **18/09/2026** · việc **6b** |
| `sharedata-http-quyet-trang-thai-ghi-tep-im-lang-prompt.md` | ✅ **18/09/2026** · việc **A2** |
| `sharedata-bo-canh-bao-truong-thieu-prompt.md` | ✅ **18/09/2026** · việc **C2** |
| `sharedata-dong-bo-extend-theo-ban-giao-prompt.md` | ✅ **18/09/2026** · việc **N1** |
| `sharedata-worker-giai-meta-prompt.md` | ✅ **18/09/2026** · việc **M1** |
| `sharedata-bo-vo-httppayload-prompt.md` | ✅ **18/09/2026** · việc **SV-1a** |
| `sharedata-outbound-gui-noi-duoi-prompt.md` | ✅ **22/09/2026** · việc **SV-12** Checkpoint gửi nối đuôi |
| `sharedata-event-gui-khi-co-du-lieu-moi-prompt.md` | ✅ **22/09/2026** · việc **SV-12** Event-Driven Change Tracking + NATS |
| `sharedata-event-trigger-fix-prompt.md` | ✅ **22/09/2026** · Bảo vệ lease claim NATS, chuyển CT sang code C# kèm cờ AutoEnableChangeTracking, 199 tests PASS 100% |
| `rename-leaseexpiry-nextrundeadline-prompt.md` | ✅ **23/09/2026** · Đổi `LeaseExpiry` thành `NextRunDeadline`, chuẩn hoá thời hạn khoá lease OCC |
| `merge-executeexportloop-into-executeexportforsubscription-prompt.md` | ✅ **23/09/2026** · Gộp `ExecuteExportLoopForSubscription` vào `ExecuteExportForSubscription`, xoá hàm đơn trang cũ |
| `remove-autoenablechangetracking-flag-prompt.md` | ✅ **23/09/2026** · Bỏ cờ `AutoEnableChangeTracking`, Worker luôn tự kích hoạt DDL Change Tracking ở mọi môi trường |
| `rename-executeexportforsubscription-to-exportsubscription-prompt.md` | ✅ **23/09/2026** · Đổi tên `ExecuteExportForSubscription` → `ExportSubscription` |
| `merge-datatriggerpacketmap-and-rename-datatrackingworker-prompt.md` | ✅ **23/09/2026** · Gộp `DataTriggerPacketMap` vào `DataTrackingWorker`, đổi tên worker `DataTrackingWatcherWorker` → `DataTrackingWorker` |
| `extract-initialize-trigger-subscription-prompt.md` | ✅ **23/09/2026** · Tách hàm `InitializeTriggerSubscription` trong `DataOutboundNatsWorker` |
| `rename-common-folder-to-utils-prompt.md` | ✅ **23/09/2026** · Đổi tên thư mục `ShareDataWorker.Core/Common` → `Utils` |
| `config-nats-trigger-subject-prompt.md` | ✅ **23/09/2026** · Đưa cấu hình `TriggerSubjectPrefix` ra `appsettings.json` thay vì hằng số cứng |
| `fix-datanatsworker-summary-and-cancellation-guard-prompt.md` | ✅ **23/09/2026** · Bổ sung XML summary và khôi phục chốt huỷ CancellationToken trong `HandleTrigger` |
| `move-unsubscribe-to-stopasync-prompt.md` | ✅ **23/09/2026** · Chuyển `Transport.Unsubscribe` từ `finally` sang override `StopAsync` |
| `rename-datatrackingdbinitializer-to-datatrackingdbinit-prompt.md` | ✅ **23/09/2026** · Đổi tên `DataTrackingDbInitializer` → `DataTrackingDbInit` |
| `fix-hardcoded-http-scheme-in-restsender-prompt.md` | ✅ **23/09/2026** · Bỏ hardcode `http://`, suy giao thức từ Address, ưu tiên URL tuyệt đối trong `DataOutboundRestSender` |
| `remove-unused-packetqueryresult-prompt.md` | ✅ **23/09/2026** · Xoá DTO không còn dùng `PacketQueryResult.cs`, bảo đảm 0 tham chiếu |
| `dedupe-monitored-tables-and-rename-changetracking-classes-prompt.md` | ✅ **23/09/2026** · Khử trùng lặp 14 bảng nguồn thành tham số `monitoredTables`, đổi tên `DataTrackingWorker` → `DataChangeWatcher`, `DataTrackingDbInit` → `DataChangeTrackingSetup` |
| `merge-changetrackingsetup-and-rename-natsworker-prompt.md` | ✅ **23/09/2026** · Gộp `DataChangeTrackingSetup` vào `DataChangeWatcherWorker`, xoá mã chết `InitializeChangeTracking`, đổi tên `DataOutboundNatsWorker` → `DataNatsWorker`, `DataChangeWatcher` → `DataChangeWatcherWorker`, thêm convention `Init` thay vì `Initialize` vào rules |
| `remove-duplicate-extract-overload-prompt.md` | ✅ **23/09/2026** · Gộp 2 overload `Extract` về đúng 1 hàm duy nhất nhận `DataOutboundCursor? cursor`, xoá bỏ `Default` static, các explicit interface implementation và static `Extract` |
| `optimize-change-tracking-queries-prompt.md` | ✅ **23/09/2026** · Tối ưu truy vấn Change Tracking: gom 14 round-trip thành 1 câu `UNION ALL`, tham số hoá kiểm tra DMV bảng, thay `sys.tables` bằng `IsAnyTable` |
| `extract-process-subscription-under-lease-prompt.md` | ✅ **23/09/2026** · Tách hàm private `ProcessSubscriptionUnderLease` dùng chung quy trình lease OCC giữa quét định kỳ và sự kiện NATS, bổ sung Rule 16 về Pragmatic DRY |
| `fix-datatypeid-identity-mismatch-in-packet-trigger-prompt.md` | ✅ **23/09/2026** · Sửa lỗi phân giải DatatypeId (GUID vs Code) trong `ProcessPacketTrigger`, hỗ trợ cả 2 dạng định danh |
| `rename-process-packet-trigger-prompt.md` | ✅ **23/09/2026** · Đổi tên và chuẩn hoá các hàm packet trigger trong DataOutboundService |
| `bo-sung-test-kich-ban-sap-he-thong-prompt.md` | ✅ **23/09/2026** · Bổ sung 2 test toàn trình cho kịch bản sập hệ thống (lease reclaim & watcher reboot), 198 tests PASS 100% |
| `mo-trigger-cho-goi-ban-chup-prompt.md` | ✅ **23/09/2026** · Mở trigger cho gói bản chụp (101, 102, 105, 108), bổ sung TollTransactionIn (15 bảng), 200 tests PASS 100% |
| `gui-ngay-o-luot-chay-dau-goi-noi-duoi-prompt.md` | ✅ **23/09/2026** · Lượt chạy đầu gói nối đuôi cắm mốc lùi một chu kỳ (GETDATE() - IntervalSeconds) và gửi ngay dữ liệu, 201 tests PASS 100% |
| `thong-nhat-phan-giai-ma-goi-va-mac-dinh-an-toan-prompt.md` | ✅ **23/09/2026** · Thống nhất phân giải mã gói qua SQL Catalog (104_rfidData → 105 Snapshot), đổi mặc định sang `NotReady`, kiểm tra lúc khởi động `CheckActivePackets`, 203 tests PASS 100% |
| `test-mat-ket-noi-csdl-giua-chung-prompt.md` | ✅ **23/09/2026** · Test toàn trình kịch bản mất kết nối CSDL giữa chừng (transport error), 204 tests PASS 100% |

*(Loạt prompt Frontend nhóm A–F từ review 16/09/2026, `16-09-2026-prompt-flatten-datapublication.md` và `sharedata-bo-qua-khi-khong-co-kenh-prompt.md` cũng đã được xóa theo Auto-Cleanup.)*



