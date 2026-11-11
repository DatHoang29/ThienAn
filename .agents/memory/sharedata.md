# ShareData — trạng thái và quyết định đã chốt

> Gom từ memory cá nhân Claude và memory nhóm ngày 09/11/2026. Tài liệu nghiệp vụ gốc ở `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/` (transcript ưu tiên hơn MasterPlan).

## ShareData: đúng phạm vi transcript

> ShareData nối đuôi: làm đúng phạm vi transcript 27–28/10; câu SQL người dùng cố tình sửa thì không rào hết, không thêm cơ chế chặn hay đổi câu mẫu vì các ca đó

Nối đuôi ShareData chỉ làm đúng phạm vi biên bản transcript 27–28/10. Câu SQL do người dùng tự sửa tùy ý, nên các ca họ cố tình viết khác đi thì không cố rào hết.

**Why:** 01/11/2026 tôi đề xuất đổi câu mẫu gói 101 và sửa điều kiện cảnh báo cột thời gian trống; người dùng trả lời vấn đề không nằm ở câu mẫu vì câu SQL sửa được tùy ý, "cứ làm đúng theo trong phạm vi transcript, còn mấy case họ đã cố tình sửa SQL thì mình không rào hết được".

**How to apply:** Gặp ca biên do người dùng tự viết SQL thì báo cáo và ghi vào giới hạn đã biết, không tự đề xuất thêm cơ chế chặn, cảnh báo mới hay đổi seed, trừ khi người dùng yêu cầu. Câu mẫu giữ nguyên. 01/11/2026 người dùng chốt: bỏ hết cảnh báo khi lưu câu nối đuôi, lưu được là lưu, có gì thì ghi `ShareDataActivityLog`. Worker ghi rủi ro vào nhóm CONFIG (tab Nhật ký cấu hình), một lần cho mỗi đăng ký × câu SQL. Câu chắc chắn làm mất dòng vẫn bị từ chối. Đừng thêm lại cảnh báo trên màn hình. Ngoại lệ do người dùng chọn: 04/11/2026 cột mốc dùng UpdateTime trơn và khóa chỉ có ngày giờ; 05/11/2026 so cột mốc bằng >=, ghép ngày giờ sai định dạng, có cột mốc mà không dùng biến mốc, cột mốc khác công thức PA1 hoặc phần thời gian thiếu CreateTime đều bị chặn khi lưu, xem project_sharedata-updatetime-coalesce. 05/11/2026 tôi lý luận 3 ca vàng (số không đệm 0, cột chuỗi trơn, cột thời gian trống) không chặn được vì máy không phân biệt được; người dùng bác: chỉ cần ép một công thức thì khỏi phải phân biệt. Gặp ca "máy không đoán được" thì xét trước cách ép một dạng duy nhất rồi mới kết luận không chặn được. Liên quan project_sharedata-cursor-single-doc.

## ShareData: phạm vi & trạng thái

> ShareData đợt 14/10/2026 (cập nhật 08/11): phạm vi chốt — SQL động là trọng tâm, mã hóa chỉ viết sẵn không nối luồng; test 08/11 (lọc Tests.ShareData): BE 851 ca, 848 đạt, 3 bỏ qua, 0 đỏ; FE 156 ca, 155 đạt, 1 bỏ qua; vstest của C# Dev Kit không phải phiên test

Phạm vi ShareData đợt 14/10/2026 (chốt 16/10/2026, đối chiếu transcript `doc/transcript/14-10-2026-sharedata-dynamic-query-va-connection.md`):
- Trọng tâm: SQL động thay query hardcode (ExtractSql ở ShareDataPacket/ShareDataMapping, SqlValidator, 5 tham số) — ĐÃ XONG.
- Mã hóa (EncryptManager, AES-256-GCM, thư mục Modules.Integration.ShareData/Encryption): chỉ VIẾT SẴN. Luồng DataInboundService/DataOutboundService KHÔNG xử lý mã hóa (quyết định người dùng 16/10); đã đưa prompt gỡ phần nối. Master Plan DQ-SV-8 từng ghi test `Roundtrip_WhenInboundCodecConfigured_DecryptsSuccessfully` không tồn tại trong mã test.
- Cleanup: đã đổi tên thành `DataCleanupService`/`IDataCleanupService` (thư mục `DataCleanup`), viết sẵn, chưa đăng ký DI (chưa kích hoạt).
- Xác thực chiều nhận (SV-9) và SenderProcessedAt đã bỏ; Protobuf/ASN.1/khóa bất đối xứng chấp nhận chưa làm.

Test BE (đo thật, `dotnet test tests/BE/test.csproj -f net10.0 --filter FullyQualifiedName~ShareData`): 18/10 sáng 626 test/623 pass; sau đợt đổi tên (Delivery, DataWorkerConfigService, ShareDataConfigService, xóa seed) = 605 test, 602 pass, 0 fail, 3 skip RealServer (gọi máy chủ thật), ~12 phút. Giảm 21 test do xóa test seed class và gộp test. Lỗi NATS chỉ-khi-chạy-cả-bộ ghi ngày 16/10 (515 test) không tái hiện lần này. 24/10/2026 sau đợt bỏ test không giá trị (unit test helper, ca trùng, gộp [Theory]): 755 ca → 418 ca, 418 pass, 3 phút 28 giây; xóa 7 tệp test rỗng. Bẫy: khi phiên khác cũng đang `dotnet test` (có build) thì testhost bị sập ("Test host process crashed", chỉ chạy được một phần) hoặc cổng 18090–18093 bị chiếm → hàng trăm test fail giả. 29/10/2026 (sau mốc nối đuôi: chữ ký công thức, guard theo kiểu, SetLastSend): 487 ca, 482 pass, 0 fail, 5 skip (3 RealServer, 1 hiệu năng CursorQueryPerformanceTests, 1 gộp hai nguồn E2 chờ bảng mốc nhiều key), 9 phút 20 giây khi chạy song song với FE. Tiến trình `vstest.console` có cha là CSDevKit (VS Code test explorer, sống hàng giờ) không phải phiên `dotnet test` — không cần chờ. 30/10/2026 (nhóm mốc nhiều nguồn + bộ kiểm WHERE chặn chỉ-thời-gian/hằng số/cột mốc không từ cột, cảnh báo K >= @k): 505 ca, 501 pass, 0 fail, 4 skip, 3 phút 33 giây. Tối 30/10 (Worker giữ Đặt lại/Đặt mốc giữa lượt, nhả lock không bật lại đăng ký, cảnh báo cột thời gian trống): 512 ca, 508 pass, 4 skip. Tối 30/10 lần 2 (sau khi phiên khác thêm khóa ESH-2306 ở FE/BE): BE 512 ca, 508 pass, 4 skip; FE datasource+sqlEditor 65 ca: 58 pass, 6 fail, 1 skip — chạy lại riêng thì 55, 60, 66 pass (55/60 chập chờn do layoutMainScrollbarRef), 11/11b/16 hỏng ổn định do handleBeforeSqlTabLeave (before-leave) đang làm dở ở index.vue chặn chuyển tab bằng mã khi scopeSwitching. Bẫy: Đặt lại / Đặt lại SQL riêng bị chặn khi đăng ký Active (ESH-2306) nên test phải tạm dừng đăng ký trước; tệp seed gói 101 được ~25 test đọc qua SeedScriptReader nên đổi câu 101 là phải sửa các test đó. FE ca 4, 33, 57, 58 của datasource.test.ts chập chờn khi máy bận (kể cả lúc đang build BE) hoặc dính lỗi toàn cục `layoutMainScrollbarRef` (ngoài phạm vi), chạy riêng thì pass. 01/11/2026 (khóa Đặt mốc khi đăng ký bật): BE 533 ca, 529 pass, 0 fail, 4 skip, 8 phút 40 giây; FE datasource+sqlEditor 67 ca: 64 pass, 2 fail do chạy cùng lúc build BE (4, 33, chạy riêng đều pass), 1 skip. Bẫy: Đặt mốc cũng bị chặn khi đăng ký Active (ESH-2306), test phải tạm dừng đăng ký trước. Bẫy SqlSugar (phát hiện và sửa ở Worker 01/11): `db.Ado.SqlQueryAsync<dynamic>` trả 0 dòng khi SQL lỗi lúc chạy (đổi kiểu dữ liệu), chỉ lỗi biên dịch như sai tên bảng mới ném ngoại lệ; `GetDataTableAsync` báo lỗi bình thường, nên đừng dùng lại dạng dynamic cho truy vấn cần biết lỗi. Sau sửa: BE 536 ca, 532 pass, 0 fail, 4 skip, 4 phút 17 giây. 03/11/2026 (bỏ hẳn dạng 2 cột, chạy trên bản sao): BE 548 ca, 543 pass, 0 fail, 5 skip, 4 phút 38 giây; thêm test câu riêng còn @lastTime và Worker kiểm cấu trúc trước khi đọc mốc: BE 551 ca, 546 pass, 0 fail, 5 skip, 4 phút 56 giây. Tối 03/11 (ESH-1308 chỉ khi đã có LastKey, Đặt mốc chặn mốc rỗng, viết lại seed ShareDataPacket bị mất 27/10): BE 555 ca, 550 pass, 0 fail, 5 skip, 7 phút 34 giây. 04/11 (gỡ CursorTimeColumn, tham số thừa GetLastSend): BE 555/550/5; FE 84 ca: 82 đạt, 1 bỏ qua, ca 53 (thanh tab) đỏ khi chạy cả bộ nhưng chạy riêng 3/3 đạt (chập chờn). 04/11 chiều (thêm kiểm ORDER BY câu gộp nhiều nguồn `sqlIncrementalGroupOrderInvalid` +2 ca, gỡ test CleanArchive -3 ca): BE 554 ca, 549 đạt, 0 đỏ, 5 bỏ qua, 5 phút 27 giây. FE cả bộ cùng lúc: 84 ca, 83 đạt, 1 bỏ qua (ca 33), 0 đỏ, 6,6 phút; ca 53 tối đa 7,9px/khung. Mục Kiểm thử nối đuôi nay nằm ở Master Plan VII.10 mục 5 (đã bỏ khỏi Sổ tay).

Test FE: Playwright cần WebAPI chạy + đăng nhập; ESLint của WEBVUE không chạy được (ESLint 10, chưa có `eslint.config.js`) — lỗi sẵn có ngoài ShareData.

Mới nhất 08/11/2026 (người dùng bảo xóa 2 test đọc tệp seed đã xóa: `SeedPacketScript_WhenCatalogPacketsMissing_InsertsLivePacketsOnce_Test`, `ProcessSubscriptions_AfterLegacySqlFixScript_…` 3 ca, gỡ `ReadPacketSeedBatch`/`ReadLegacySqlFixBatch` ở SeedScriptReader; thêm `ShareDataApiLatencyTests` đo 41 API đọc, để `Skip`): lọc `FullyQualifiedName~Tests.ShareData` được 851 ca, 848 đạt, 0 đỏ, 3 bỏ qua, 11 phút 21 giây. Lọc này không gồm 3 ca RealServer (lọc `~ShareData` mới gồm); xUnit 2 tính Theory có Skip là 1 ca nên 3 bỏ qua = 2 CursorQueryPerformanceTests + 1 ShareDataApiLatencyTests. 09/11/2026 gỡ 4 ca ActivityLog khỏi bộ đo theo yêu cầu, còn 37 API. Cũng 09/11 (gỡ `outbound_lock_budget_percent`, gỡ nguồn biến `lastRunTime`, `startOfYear`, `partnerCode`): lọc `Tests.ShareData` 854 ca, 851 đạt, 0 đỏ, 3 bỏ qua, 15 phút 42 giây. Trước đó tối 06/11/2026 (lưu câu so thêm bảng nguồn và công thức trong câu con của cột mốc, GetCursorIdentity; Tạo cấu hình riêng hỏi trước khi bỏ mốc): BE 857 ca, 848 đạt, 5 bỏ qua, 4 đỏ chỉ vì người dùng đã xóa hai tệp test còn đọc (`sharedata-seed-goi-tin-ShareDataPacket.sql`, D2 `sharedata-sua-cau-rieng-moc-hai-cot-ShareDataSql.sql`), 7 phút 33 giây; FE sqlEditor 11, 11b, 11c, 59, 59b đạt. Trước đó 06/11 (test Worker cho câu viết công thức một lần trong câu con, sửa Worker đọc cột câu WITH): BE 852 ca, 847 đạt, 5 bỏ qua, 0 đỏ, 11 phút 5 giây. Trước đó (thêm luật câu con TOP): BE 849 ca, 844 đạt, 5 bỏ qua, 0 đỏ, 8 phút 22 giây; FE không đổi mã nên không chạy lại. Trước đó 05/11/2026 khuya (vá 6 ca mất dòng, project-sharedata-cursor-holes): BE 839 ca, 834 đạt, 5 bỏ qua, 0 đỏ, 7 phút 19 giây; FE `views/sharedata` 156 ca, 155 đạt, 1 bỏ qua (ca 33), 0 đỏ, 10,1 phút. Trước đó tối 05/11 (bỏ tệp SQL kiểm tra, thêm test ca biên nhiều Worker `DataOutboundServiceTests.Clones.cs`): BE 798 ca, 793 đạt, 5 bỏ qua, 0 đỏ, 7 phút 46 giây; 31 ca mới đều đã thử phá mã trên bản sao tạm và thấy đỏ. Bẫy: khôi phục mã đã phá bằng `rsync -a` giữ ngày giờ cũ nên MSBuild không build lại, DLL còn mã đã phá làm 52 ca đỏ giả; phải `touch` tệp trước khi chạy lại. Trước đó (làm mục 9 bàn giao): BE 766 ca, 761 đạt, 5 bỏ qua, 0 đỏ, 5 phút 55 giây; FE `cursor.test.ts` 9 ca đạt. Trước đó (ép cột mốc theo công thức PA1): BE 764 ca, 759 đạt, 5 bỏ qua, 0 đỏ, 5 phút 57 giây; FE `views/sharedata` 155 ca, 153 đạt, 1 bỏ qua (ca 33), 1 đỏ do kiểm tra ca 70, sửa rồi chạy riêng đạt, 13,4 phút. Trước đó cùng ngày (từ chối thêm ngày giờ sai định dạng và cột mốc không dùng biến mốc): BE 741 ca, 736 đạt, 5 bỏ qua, 0 đỏ, 5 phút 35 giây. Trước đó cùng ngày (rà bàn giao + Sổ tay, viết thêm test còn thiếu): BE 725 ca, 720 đạt, 5 bỏ qua; FE `views/sharedata` 152 ca, 151 đạt, 1 bỏ qua (ca 33), 0 đỏ, 13,1 phút; sau đó FE chỉ đổi câu dịch nên chỉ chạy lại `sqlVariablePanel`, `cursor`. Chi tiết ở Master Plan VII.10 mục 5. 5 ca BE bỏ qua theo log: 3 ở `tests/BE/RealServer` (gọi máy chủ thật), 2 ở `CursorQueryPerformanceTests` (đo 1 triệu dòng, so PA1/PA2 trên bảng tạm). Ghi chú 29/10 "1 gộp hai nguồn E2" đã cũ, ca gộp hai nguồn nay chạy thật; muốn liệt kê ca bỏ qua thì đọc dòng `Skipped` trong log, không lấy từ ghi chú này (05/11 đã báo sai vì vậy). Bẫy: `UpdateWithDiffLogAsync(entity)` mặc định bỏ qua cột null, muốn xóa giá trị một cột phải truyền `false` (đã gây lỗi NextTimeRun không được xóa); hàm nằm ở thư viện tham chiếu nên Gortex không tìm thấy định nghĩa. Vue: trang cha gán `operateType` rồi gọi `openDialog` ngay, prop con chỉ cập nhật sau tick nên phải `await nextTick()` trước khi đọc. 05/11/2026 sáng (từ chối so cột mốc bằng `>=` khi lưu, +3 -1 ca): BE 564 ca, 559 đạt, 5 bỏ qua, 0 đỏ, 4 phút 53 giây. 04/11/2026 chiều (thêm test tệp D2 sửa câu riêng mốc 2 cột, 3 ca): BE 562 ca, 557 đạt, 5 bỏ qua, 0 đỏ, 9 phút 53 giây; trước đó (chặn UpdateTime trơn, khóa chỉ có ngày giờ, ORDER BY câu gộp, gỡ CleanArchive, test seed 101) BE 559/554/5. FE 04/11 16:15 (sau khi khóa ô Mốc lần đầu, bỏ biểu tượng (i), trả khóa nguồn @pageSize, sửa nạp bộ biến khi đổi tab): 89 ca, 88 đạt, 1 bỏ qua (ca 33), 0 đỏ, 8,3 phút.

Bẫy khi chạy FE: chạy với `--output` ra thư mục riêng, vì lượt khác (Playwright tự dọn test-results khi khởi động) hoặc `rm -rf test-results` làm đỏ giả ENOENT ở browserContext.close. Máy đổi mạng giữa lượt (net::ERR_NETWORK_CHANGED) làm không tải được chunk từ localhost, trang trắng, locator.click chờ hết 60 giây: ca 3 và ca 40 đỏ kiểu này, chạy riêng đều đạt. Đừng chạy hai phiên Claude cùng lúc trên một bộ test.

**Why:** tránh lần sau lặp lại việc nối mã hóa vào luồng hoặc tin số liệu cũ trong Master Plan.
**How to apply:** trước khi chạy test BE: `lsof -nP -iTCP:18090-18093 -sTCP:LISTEN` và `pgrep -fl "dotnet test|testhost"`; có phiên khác thì chờ nó xong, không kill tiến trình của người dùng. Ưu tiên khối "Kết quả kiểm chứng" mới nhất trong Master Plan nếu có. Xem feedback_prompt-first-fixes — cần sửa thì viết prompt, không tự sửa.

## ShareData: chuẩn SQL "như trước"

> ShareData — chuẩn \"như trước khi chuyển SQL động\" là các hàm QueryPacketNNN trong Worker ở commit 56bb4bc3, KHÔNG phải ShareDataPacketSqlCatalogUtil; seed 02 giống hệt 101–109 (105 chỉ bỏ TOP); không khôi phục 105 về TollTransactionOut

Đối chiếu SQL gói tin 101–111 phải lấy chuẩn là **SQL chạy thật của Worker ở HEAD**: các hàm `QueryPacket101..111` trong `ShareDataWorker/Infrastructure/Services/DataOutbound/Extraction/DataOutboundExtractionProcess.cs` (repo `TA-ITS015-WEBAPI-V1.0`, commit `56bb4bc3`). `ShareDataPacketSqlCatalogUtil.cs` (Module.ShareData.Core) ở HEAD chỉ phục vụ `PacketQueryHandler` dựng SQL xem mẫu ở FE và đã lệch so với Worker (vd gói 105 trong catalog đọc `TollTransactionOut`, Worker đọc `TollTransactionIn`).

Kết quả đo ngày 16/10/2026 (so seed `02-sharedata-seed-goi-tin.sql` với `QueryPacketNNN`, chuẩn hóa khoảng trắng): 101–109 giống hệt; riêng 105 chỉ khác `TOP (@snapshotTop)` (=100) vì đã chốt bỏ trần snapshot. 110: bản cũ NotReady trả rỗng, seed có SQL theo phương án A; 111 bỏ. Gói 105 đọc `TollTransactionIn` là quyết định chủ dự án 24/09/2026.

**Why:** ngày 16/10 tôi đã kết luận sai "gói 105 lệch bản gốc, cần khôi phục về TollTransactionOut" vì so với catalog cũ; người dùng nghi ngờ đúng ("sao phải khôi phục 105").
**How to apply:** khi cần chứng minh "SQL động giống như trước", lấy bản cũ bằng `git -C TA-ITS015-WEBAPI-V1.0 show 56bb4bc3:src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Extraction/DataOutboundExtractionProcess.cs`; không đề xuất sửa seed 105. Xem project_sharedata-verification-status cho phạm vi và số test; sửa thì viết prompt theo feedback_prompt-first-fixes.

## ShareData: tracker bảng riêng

> ShareData — danh sách bảng theo dõi Change Tracking chuyển từ SysOpConfig (nhóm sharedata_tracker) về bảng riêng ShareData (đổi ShareDataTrackVersion thành bảng nhiều dòng, mỗi dòng 1 bảng nguồn có mốc riêng); sửa bằng .sql, không cần UI; mã lỗi ESH và thuật ngữ vẫn ở bảng hệ thống chung

Quyết định chủ dự án 17/10/2026: danh sách bảng nguồn theo dõi Change Tracking là quy tắc nghiệp vụ riêng của ShareData, nên lưu trong bảng của ShareData (gộp vào bảng `ShareDataTrackVersion` và đổi tên — tên đề xuất `ShareDataTrackTable`, mỗi dòng = 1 bảng nguồn, có `LastVersion` riêng + cột truy vết), không dùng `SysOpConfig` nhóm `sharedata_tracker` nữa. Cấu hình sửa bằng script `.sql`, không làm màn hình — đây là ngoại lệ có chủ đích với Rule 19.82 (UI-first). Mã lỗi ESH (`SysConfigData` loại `sharedata_error_code`) và bản dịch (`SysTerminology`) GIỮ ở bảng hệ thống chung.

**Why:** người dùng muốn khi có lỗi thì truy ngay trong một bảng của ShareData ("bug mình biết truy từ đâu lẹ nhanh"), không phụ thuộc cấu hình hệ thống chung.

Bổ sung 17/10/2026 (chốt hướng B): bảng đổi tên tiếp `ShareDataTrackTable` → **`ShareDataSourceTable`** (danh mục bảng nguồn duy nhất), `Status` → `IsTracked` (bool), thêm `IsQueryable`, `Category`, `DefaultAlias`; danh sách 14 bảng viết cứng `MetadataService.AllowedTables` chuyển vào đây (+ `TmsVehicleRegistration`); khi Lưu/Dò cột/Xem mẫu ở SQL Center, SQL chỉ được đọc bảng `IsQueryable` (Worker và SampleData không kiểm lại); dự phòng khi DB chưa có danh mục = các dòng của seed class. Mô tả cột (`KnownFieldsCatalog`) giữ trong code. Tham số vận hành Worker + giới hạn kiểm BE/FE lưu `SysOpConfig` nhóm `sharedata_config` (có dự phòng trong code), 23 mã `sharedata_fe_config` chuyển sang đó.

**How to apply:** khi viết prompt/tài liệu (doc 05 mục F, Phụ lục B, bước dựng dữ liệu nền; script 03; Master Plan DQ-SV-11) thì hướng dẫn tracker bằng SQL trên bảng mới, không hướng dẫn qua màn *Cấu hình hoạt động*; khóa `ShareDataTracker:GroupCode` trong appsettings trở thành khóa chết (chỉ liệt kê, không tự xóa — xem feedback_limit-file-churn). Prompt thực hiện: P15 (chạy sau P13-BE, trước P14 đổi tên Connect → PacketSend). Sửa thì viết prompt theo feedback_prompt-first-fixes.

## ShareData: khóa nối đuôi

> Lịch sử chốt khóa nối đuôi: 02/11 chọn PA1 nối chuỗi; 03/11 bỏ hẳn dạng 2 cột ở giao diện, WebAPI, Worker, seed

Mục 1.4 của biên bản 22/10/2026 (khóa nối đuôi, khóa tổng hợp `yyyyMMddHHmmss_ZoneId`) được người dùng chốt **để hỏi lại người dự họp** (ghi 23/10/2026). Không làm tiếp phần này cho tới khi có câu trả lời.

**Đã có trong mã:** mốc lưu ở `ShareDataLastSend` (mỗi cặp đối tác × gói tin, hai cột `LastTime` + `LastKey`, chỉ tiến lên, ghi sau khi gửi thành công); tham số hóa 5 biến; chặn cửa sổ bằng "Quét lùi tối đa".

**Chưa làm / chưa chốt:**
- Có cần lưu thật một chuỗi duy nhất kiểu `yyyyMMddHHmmss_ZoneId` hay hai cột như hiện nay là đủ. Bản ghi âm [22/10 30:05] chỉ là ví dụ nói còn đắn đo ("đọc được, hay dưới dạng key của mình"), bản tóm tắt 1.4 viết nặng hơn.
- Có cần gán điều kiện theo từng trường ([22/10 29:05]) không. Bản tóm tắt ghi ngược là tránh cấu hình lọc rời rạc trên giao diện.
- Chưa có màn xem / đặt lại mốc đã gửi; đổi cách chọn khóa trong SQL mà mốc cũ còn nguyên có thể bỏ sót hoặc lặp dữ liệu.
- Khóa ghép nhiều thành phần chỉ làm được bằng cách người viết SQL tự nối chuỗi; `@lastKey` so sánh dạng chuỗi nên phần số không đệm số 0 (ZoneId 9 và 10) có thể sai thứ tự, chưa có cảnh báo.

**Cập nhật 23/10/2026 (sau khi nghe thêm tệp 3, 4 và sếp dán ghi chú):** sếp đưa câu mẫu cũ dựng khóa là một chuỗi `CONVERT(varchar(23), Write_Date, 121) + '_' + MSLANE` so sánh `> '{0}'` (thời gian định dạng cố định + mã làn) → trả lời phần lớn câu "khóa tổng hợp một chuỗi hay hai cột". Còn mở: câu mẫu không có cột thời gian riêng (hệ thống hiện đòi `__lastTime` và `__lastKey`, cần kiểm lại), `TOP` trong `ORDER BY NGAYSOAT` không theo khóa (có thể sót dòng), sếp muốn biến `@` có tên nghĩa và "gán nó là cái gì" (hiện chỉ 5 biến cố định), điều kiện riêng theo trạm (mốc 2027, đầu năm) = chỗ của SQL riêng, "đầu năm" cần biến mới như `@yearStart`. Đã ghi ở mục 1.4 phần B của biên bản 23-10-2026 (đã gộp 1.4 và 1.5); phần "Cách hiểu" và "Điểm cần lưu ý" **sếp đang xác nhận** (người dùng báo 23/10/2026).

**Phân tích mã 23/10/2026 (chưa sửa gì):** câu mẫu nguyên văn bị chặn (`{1}` lỗi cú pháp, `'{0}'` là chuỗi hằng nên bị coi Snapshot, bảng `SoatVeVangLai` không có trong danh mục, TOP ở câu con, thiếu `__lastTime`/`__lastKey`). Viết lại theo chuẩn (`__lastKey` = biểu thức ghép, `__lastTime` = Write_Date, TOP (@pageSize) + ORDER BY __lastKey ở câu ngoài, giữ `(@lastTime IS NULL OR Write_Date >= @lastTime)` để quét lùi không gửi lại hết) thì làm được ngay. Bộ kiểm tra chỉ xem có TOP, ORDER BY không DESC, có hai cột; không kiểm ORDER BY khớp khóa. "Đầu năm" tạm viết `DATEFROMPARTS(YEAR(GETDATE()),1,1)`. Cảnh báo "đệm số 0" ở dòng trên là thừa khi WHERE và ORDER BY dùng cùng biểu thức chuỗi.

**Đối chiếu 24/10/2026:** wireframe 10 (sửa 14/09, trước họp) chỉ thể hiện phương án quét theo cửa sổ (`@fromTime`=lastRunTime, `@toTime`=now), không có lastKey/TOP/ORDER BY. Mã hiện tại làm khóa nối đuôi dạng "2 điều kiện" (OR) — tương đương khóa ghép một chuỗi của sếp nhưng dùng được index; còn phương án cửa sổ 5–10 phút thì CHƯA có thật (`@fromDate` = now − số ngày quét lùi, không phải lần chạy trước). Câu "dùng 2 điều kiện thường sai… để vậy cũng đúng" [19:24–19:30] mâu thuẫn, cần hỏi lại (nghi là sai ở dạng AND).

**ĐÃ ĐÓNG 25/10/2026 (người dùng chốt, không hỏi sếp nữa):** giữ cơ chế 2 cột (`__lastTime` + `__lastKey`, lọc dạng OR); quy ước `__lastTime` dùng giờ máy chủ ghi (CreateTime/UpdateTime) nên dòng đến muộn không sót; quét định kỳ theo khung giờ người dùng tự làm bằng biến (`@fromTime` = Lần chạy trước, `@toTime` = Thời điểm chạy). Không cần cột `TAC_PrimaryKey`. Đã thêm kiểm ORDER BY (__lastTime rồi __lastKey) khi lưu SQL nối đuôi.

**CHỐT LẠI 02/11/2026, thay cho chốt 25/10:** sếp không muốn dùng 2 cột, chọn PA1 gốc (nối chuỗi), tối ưu tính sau. Đã đổi seed, script, test, mẫu FE, tài liệu ngày 02/11 (prompt đã xóa theo yêu cầu); seed chưa chạy trên DB nào. Lỗi riêng chưa sửa: `CommitSuccess` đọc mốc trong giao dịch nên nhiều Worker song song có thể deadlock 1205, đã gửi mà không ghi mốc (đo trên bản sao: nguyên trạng đỏ 3/5, đưa đọc ra ngoài giao dịch đỏ 0/5, toàn bộ ShareData 564 đạt). PA1 chạy được ngay ở chế độ chỉ khóa (31.588 trang/lượt so với 421 của 2 cột, bảng 1 triệu dòng). PA2 đo 11.790 trang chủ yếu nhờ `ORDER BY` theo cột gốc, không phải nhờ tính công thức một dòng; bộ kiểm hiện buộc câu chỉ khóa `ORDER BY __lastKey`. Đo 02/11 (`CursorQueryPerformanceTests`, 1 triệu dòng, 4 vị trí mốc): thêm chặn dưới mà vẫn `ORDER BY __lastKey` KHÔNG giảm (31.588); chặn dưới + `ORDER BY` cột gốc đọc 481, 483, 421, 5 trang, bằng dạng 2 cột, nhưng phải nới luật kiểm. Ca lệch mốc đo được: PA2 dòng cuối bị sửa thì sót 4/10 dòng, PA1 không sót. `FORMAT()` lỗi trên Azure SQL Edge của máy dev (CLR tắt), dùng `CONVERT(varchar(23), cột, 121)`.

**Cập nhật cuối ngày 02/11/2026:** đã sửa deadlock của `CommitSuccess` (đọc mốc trước câu ghi đầu tiên); đã chạy seed và script chuyển mốc trên DB `test`, chưa chạy trên DB dev, staging, production. Đã có trang xem và đặt mốc riêng `views/sharedata/cursor`, cần chạy `sharedata-seed-menu-SysMenu.sql` để hiện menu.

**Cập nhật 03/11/2026:** giao diện chỉ còn PA1: bỏ nguồn `lastTime` và ô thời gian ở Đặt mốc, cột Nguồn chỉ là chữ theo tên biến, không còn ô chọn; câu chưa đúng quy chuẩn thì khóa ô ngày, có nút Sửa câu (không có câu báo nhắc dạng 2 cột); câu có `__lastKey` mà lọc bằng biến tên khác thì giao diện chặn lưu. Câu gộp nhiều nguồn dạng một chuỗi đã có test.

**Nhập mốc (03/11/2026):** người dùng nhắc lại đã review là không cho gõ tay mốc vì dễ sai (tôi không tìm thấy trao đổi gốc). Giao diện chỉ có ô chọn ngày giờ (tự ghép `...000_`, chỉ khi công thức kiểu 121, trống là từ đầu); cả tab Mẫu chung và tab đối tác chỉ đặt mốc bắt đầu, sửa mốc đang chạy ở khối 4. Mốc đang chạy (trang Mốc gửi đã bỏ 07/11); câu dạng khác không đặt được mốc. API SetLastSend vẫn nhận chuỗi bất kỳ.

**Bỏ hẳn dạng 2 cột (03/11/2026, người dùng yêu cầu):** gỡ nguồn `lastTime`, cột `__lastTime`, 4 mã lỗi và `SqlIncrementalGroupSourceMismatch` ở WebAPI, Worker, FE; tệp seed bỏ bước 1c. Giữ: cột `ShareDataLastSend.LastTime` (không ALTER, chỉ là giờ ghi mốc); tệp `sharedata-chuyen-doi-moc-ShareDataLastSend.sql` chỉ bỏ bước đổi mốc, còn gỡ unique index cũ và chép nhóm mốc từ bảng cũ vì không liên quan 2 cột; FE giữ nút Sửa câu để đổi câu riêng còn `@lastTime`. Câu riêng còn `@lastTime` thì Worker dừng với ESH-1306; 3 câu trên DB dev (TTCSDL, Kiên gói 101, Đ gói 108) người dùng tự bấm Sửa câu. `CursorQueryPerformanceTests` cố ý giữ các phép đo 2 cột vì là căn cứ so sánh, không gọi mã đã bỏ.

**Why:** transcript Gemini tự chép có thể sai, câu mâu thuẫn phải hỏi người dự họp trước (xem feedback_transcript-may-be-wrong).
**How to apply:** khi người dùng quay lại mục 1.4, bắt đầu từ các câu hỏi trên; đề xuất đã nêu: cột "Mốc đã gửi" dạng đọc được kèm nút đặt lại, và cảnh báo khóa ghép thiếu đệm số 0.

## ShareData: biến SQL

> ShareData biến SQL (tham số câu truy vấn): nguồn yêu cầu, danh sách \"Lấy từ\" đã chốt, chế độ suy từ SQL; wireframe HTML chỉ tham khảo, UI tự thiết kế

Biến SQL ShareData (24/10/2026):
- Yêu cầu gốc = biên bản 23-10-2026 mục 1.4 + ghi chú văn bản của sếp; `_source/html/10-wireframe-man-hinh-anh-xa.html` (W0 "Tham số câu truy vấn": Tên/Kiểu/Lấy từ/Ghi chú) chỉ là ý tưởng UI, sửa trước 23/10 nên thiếu lastKey, đầu năm, hằng số.
- Đã chốt: BỎ nguồn `tenant`, `partner`, `subscription` của wireframe; "Lấy từ" chỉ gồm nguồn chuẩn có căn cứ (mốc khóa lần trước, lần chạy trước, thời điểm chạy, đầu ngày/tháng/năm, hằng số); cần thêm thì bổ sung sau.
- Đã chốt: chế độ gửi (theo khóa @lastKey hay quét định kỳ @fromTime/@toTime) suy từ biến câu SQL dùng.
- Khi làm FE: bảng tham số là COMPONENT RIÊNG (vd `dataSource/component/sqlVariablePanel.vue`), KHÔNG sửa `sqlEditor.vue` và phần danh sách trường; chỉ gắn vào trang cha `dataSource/index.vue`.
- Tài liệu: KHÔNG ghi bản phân tích vào biên bản/tệp mới, KHÔNG sửa wireframe — giữ nguyên làm bản đối chiếu (người dùng 24/10/2026).
- Đã chốt phương án B: trạm (SQL riêng) vừa ghi đè giá trị biến mẫu chung, vừa khai thêm biến riêng. ShareDataSqlVariable: dòng có PartnerId trùng Code với mẫu chung = ghi đè giá trị; không trùng = biến riêng của trạm.
- 29/10/2026 người dùng: mọi tệp HTML trong `_source/html/` (cả `sharedata-datasource-doi-tac.html`, `sharedata-datasource-doi-tac-v2-thu.html`) chỉ để tham khảo; bản `v2-thu` là SAI. Sếp chỉ chốt cơ chế ở service (biên bản 27–28/10), UI do nhóm tự thiết kế. Câu "Khối 3 bảng 3 cột · Wireframe W0.2" (từng ở tóm tắt transcript 28/10 §1.5) là Gemini dựa wireframe, không phải lời sếp; 29/10/2026 người dùng cho lược mọi phần giao diện khỏi biên bản 27–28/10 (đã gộp vào tệp 28/10), chỉ giữ ý xử lý nối đuôi.

- 03/11/2026: bỏ ô chọn nguồn, nguồn theo tên biến: `@lastKey`, `@lastKey_<nhóm>` nối tiếp lần trước, biến khác là giá trị nhập (Thời điểm chạy đã bỏ, dùng `GETDATE()` trong câu).
- 09/11/2026: gỡ hẳn ba nguồn `lastRunTime`, `startOfYear`, `partnerCode` ở WebAPI, Worker, FE; chỉ còn `lastKey` và giá trị nhập. Bộ biến cũ còn nguồn đó: lưu bị từ chối, chạy thử và Worker coi như biến chưa khai. Danh sách "Lấy từ" ở dòng trên của ngày 24/10 là lịch sử.

**Why:** người dùng xác nhận thứ tự căn cứ: transcript (lời thoại) là gốc, wireframe chỉ để tham khảo khi tự thiết kế UI.
**How to apply:** phân tích/thiết kế biến và giao diện dẫn từ lời thoại + cơ chế service trước; không lấy wireframe HTML làm căn cứ bắt buộc; liên quan project_sharedata-lastkey-open-questions.

## ShareData: một nguồn tài liệu nối đuôi

> ShareData nối đuôi: nguồn tài liệu duy nhất là biên bản transcript/28-10-2026 (Phần A, B lời họp + Phần C thiết kế, gộp doc 08 ngày 30/10/2026)

30/10/2026 người dùng yêu cầu gộp `doc/08-thiet-ke-luong-noi-duoi-toan-trinh-ui-be-worker.md` vào `doc/transcript/28-10-2026-sharedata-xu-ly-noi-key-3-phuong-an-lastkey-va-toi-uu-hieu-nang.md` ("tôi đọc 1 nguồn thôi"); doc 08 đã xóa. Biên bản gồm Phần A (27/10), Phần B (28/10) là lời họp, Phần C (mục `C.x`, không phải lời họp) là thiết kế và đối chiếu mã: hiệu năng PA1/PA2/PA3 ở C.4.3, edge case và nhóm mốc nhiều nguồn ở C.6, kịch bản kiểm thử trên giao diện ở C.7 (30/10). Chốt 30/10 "không đổi câu mẫu seed" đã thay ngày 04/11/2026: xem project_sharedata-updatetime-coalesce.

Ngoại lệ 01/11/2026: người dùng yêu cầu tệp kịch bản test riêng `doc/03-kich-ban-test-noi-duoi.md` ("nhiễu thông tin quá") — chỉ thao tác trên giao diện (người dùng không muốn chạy SQL/script; từ 06/11/2026: CSDL `DEV_ITS015`, đối tác PC08 × gói `101_commonData`, hồ sơ `PC08_101COMMONDATA_OUT`, đăng ký gửi 30 giây có sẵn, tạo cấu hình riêng câu nối đuôi, kênh File; 299 dòng `TmsZoneStatus`, 35 dòng chưa có `UpdateTime`; mỗi lượt một trang nên `@pageSize = 21` gửi qua 15 lượt; trước 06/11 dùng đối tác Đạt × gói 108 trên VmsCurrent), bước làm + kết quả phải thấy; C.7 giữ làm bản chi tiết và trỏ sang tệp này. Sửa luồng nối đuôi thì cập nhật cả tệp kịch bản.

**Why:** người dùng muốn đọc một chỗ duy nhất về cơ chế nối đuôi, không phải đối chiếu hai tệp; riêng lúc chạy test thì cần bản gọn.
**How to apply:** ghi chú, thiết kế, kết quả đo mới về nối đuôi thì sửa Phần C của biên bản 28/10, không tạo tài liệu rời; ghi rõ "không phải lời họp" khi thêm vào biên bản. Liên quan project_sharedata-sql-variables, feedback_limit-file-churn.

## ShareData: việc để ngỏ

> ShareData — các việc đã chốt để lại (trường dư SQL riêng do người khác làm, chọn Bỏ qua/Để trống trên UI chưa làm); Quét lùi tối đa đã bỏ hẳn 26/10

Người dùng chốt ngày 25/10/2026 khi đối chiếu code với transcript + wireframe:

- **Trường dư ở SQL riêng đối tác**: rà mã 09/11/2026 thấy đã làm: câu riêng lưu FieldsJson riêng, trường chỉ câu riêng có hiện dấu "+" ở cột So mẫu chung, sửa được qua hộp Chỉnh sửa trường, màn Ánh xạ lấy trường theo câu riêng của đối tác. Nút "Tạo trường" không còn trong mã (chỉ sót câu dịch `fieldCompareCreate`).
- **Quét lùi tối đa (`MaxLookBackDays`)**: ĐÃ BỎ HẲN 26/10/2026 (người dùng duyệt): xóa khỏi entity/DTO/Worker/FE/seed, bỏ cấu hình `packet_lookback_days_min/max`. Cần chặn quét lùi thì người dùng tự viết vào câu SQL (`DATEADD` với `GETDATE()`; nguồn "Thời điểm chạy" đã bỏ 03/11/2026). Cột cũ trên DB để nguyên.
- **Trường thiếu ở SQL riêng — chọn Bỏ qua / Để trống trên UI** (ý tưởng của sếp, biên bản 23/10 §1.1): chưa làm UI. Worker đã có sẵn: nút ánh xạ `$exclude: true` = bỏ qua (không có khóa trong gói gửi), còn lại cột không có trong dòng SQL = `null`; có test `ProcessSubscriptions_WhenMappedColumnMissingFromSql_SkipsOrSendsNull_Test`.

**Why:** người dùng muốn giữ phạm vi hẹp, phần nào chưa chốt hoặc đã giao người khác thì không tự làm.
**How to apply:** khi rà ShareData, chỉ báo lại các mục này là "đang để ngỏ", không đề xuất làm tiếp trừ khi người dùng nhắc. Liên quan sharedata-sql-variables.

## ShareData: mục biên bản đã chốt

> Các mục rà biên bản ShareData người dùng đã chốt ngày 02/11/2026; lần rà sau không báo lại, không kiểm lại

Người dùng chốt ngày 02/11/2026 sau khi rà biên bản họp ShareData. Rà lại lần sau thì không báo các mục này.

**Đang đúng, không cần kiểm hay báo lại:**
- Nút Chỉnh sửa (26/10): danh sách trường xem trên bảng, sửa qua hộp thoại; khung SQL và bảng tham số sửa trực tiếp khi không có đăng ký gửi bật.
- Bảng bị ảnh hưởng của câu ghi (26/10 [12:50]): `writeSqlPanel.vue` chỉ hiện bảng tự nhận diện, không thêm ô sửa. Câu trả lời gốc là "sửa lại đi hiện tại đang đúng", tôi hiểu là giữ nguyên.
- Trường lệch mẫu chung (23/10): chưa có UI Bỏ qua/Để trống, Worker đã hỗ trợ; trường dư đã làm (rà 09/11/2026).
- Gói treo hiện số 0 màu đỏ (22/10 [13:17]) và cắt chuỗi lỗi dài.

**Sếp bảo để yên:** chưa cần dọn. Giữ `cleanup_enabled = 0`; phần gói nhận đã xong và phạm vi dọn theo cấu hình chưa xử lý. `cleanup_retention_days` mặc định 30 theo biên bản 14/10.

**Chưa cần đo tải (SV-6):** mong muốn là khởi động nhiều dịch vụ trên máy chủ vẫn chạy đúng.

**09/11/2026 người dùng chốt, rà sau KHÔNG báo lại (kể cả ở mục "việc để ngỏ"):**
- Ca dòng ghi với thời gian cũ hơn mốc (mốc 10:00, lúc 10:05 ghi dòng `UpdateTime` 09:30): dữ liệu đúng thì `UpdateTime` phải là giờ ghi, nên đây là dữ liệu sai; giữ nguyên.
- Dọn tệp cũ (CleanArchive): để dạng chú thích trong mã là được.
- Đo tải SV-6: chưa cần.
- Đo tốc độ API Nhật ký hoạt động: đã gỡ 4 ca (Page, Page.FilterSuccess, Summary, GetList) khỏi `ShareDataApiLatencyTests`, không nhắc chỉ mục `index_ShareDataActivityLog_OA` nữa.

**PA1:** chạy đúng PA1 gốc là được, không ghi chú việc tối ưu hay số trang đọc như một việc còn lại.

**Nguồn Thời điểm chạy (`now`):** người dùng đổi ý 03/11/2026, đã bỏ cả giao diện lẫn máy chủ vì thừa và rối. Cần giờ hiện tại thì viết `GETDATE()` trong câu (đã có test lưu và Worker gửi); câu nối đuôi muốn lùi mốc thì Đặt mốc. `@now` của câu ghi chiều nhận là tham số hệ thống riêng, vẫn giữ.

**Khác lời họp nhưng đã chốt (03/11/2026, không báo là lệch):**
- Mốc đang chạy sửa được (Đặt mốc, Đặt lại; 03/11 ở trang Mốc gửi, từ 07–08/11 ở khối 4. Mốc đang chạy), dù ghi âm có câu "Value này là hệ thống detect" [A 05:49–06:15]. Đây là **chốt trong họp với sếp, không có ghi âm** (người dùng xác nhận 06/11/2026): khi đối chiếu lời họp với thiết kế thì xếp vào "chốt trong họp", không xếp vào "mình thiết kế thêm". 08/11/2026 người dùng chốt đảo lại: mốc đang chạy ở khối 4 trong Cấu hình gói tin, bỏ tab Mốc gửi, xem project-sharedata-cursor-terms. Bảng tham số chỉ đặt Mốc bắt đầu; mốc đang chạy ở khối 4.
- Tham số cố định (`@pageSize`, biến Giá trị nhập) lưu ở `ShareDataSql.VariablesJson` đi theo câu SQL mẫu chung hoặc riêng đối tác, không lưu theo đối tác như bản vẽ `b7af82ba` [A-4 03:16–03:35]. Đối tác dùng mẫu chung dùng chung giá trị, muốn khác thì tạo cấu hình riêng.
Đã ghi ở đầu Phần C biên bản `transcript/28-10-2026-…`.

**Giữ `DefaultValue` và ô Mốc bắt đầu (chốt 03/11/2026):** người dùng có thử bỏ để chỉ còn một nguồn rồi đổi ý, vì họp cho người dùng tự đặt điểm bắt đầu [B 06:46–07:10]. Mốc đang chạy chỉ lưu ở `LastKey`; `DefaultValue` là đầu vào người dùng đặt, chỉ dùng khi `LastKey` trống. Đừng đề xuất bỏ lại trừ khi được yêu cầu.

**Why:** người dùng không muốn bị hỏi lại các mục đã chốt khi bàn giao.

**How to apply:** khi rà ShareData, bỏ qua các mục trên trừ khi người dùng nhắc lại. Liên quan project_sharedata-deferred-items, project_sharedata-lastkey-open-questions, project_handover-2026-11.

## ShareData: chưa cần dọn tệp

> ShareData dọn tệp bằng chứng/nhật ký nghiệp vụ (CleanArchive) — sếp chốt 02/11/2026 chưa cần làm; 04/11 đã gỡ phần kích hoạt, để lại dạng chú thích

Dọn tệp bằng chứng, nhật ký nghiệp vụ, cảnh báo và gói nhận đã xong (`CleanArchive` trong `DataCleanupService.cs`): sếp chốt 02/11/2026 **chưa cần làm**. Ngày 04/11/2026 người dùng yêu cầu gỡ phần đã kích hoạt: mã WEBAPI trả về bản HEAD (khối `CleanArchive` lại nằm trong chú thích, `DataCleanupWorker` chỉ gọi `CleanTrackingLogs`), bỏ khóa `cleanup_enabled`/`cleanup_retention_days` khỏi `ShareDataConst`, `DataWorkerConfig`, `DataWorkerConfigService`; bỏ test `CleanArchive_WhenEnabledOrDisabled_*`; seed SysOpConfig về 45 tham số (xóa mềm hai khóa đó); bàn giao không nhắc tới (người dùng bảo bỏ dòng này ở mục 9 ngày 04/11).

**Why:** người dùng nhắc "sếp trước đó có nói là chưa cần làm"; trước đó phiên 02/11 đã tự mở khối này dù đã chốt chưa cần.

**How to apply:** không kích hoạt lại `CleanArchive` hay thêm tham số `cleanup_*` khi chưa có yêu cầu mới; dọn nhật ký hạ tầng ESH-16xx (`CleanTrackingLogs`) vẫn giữ. Xem feedback_handover-notes-pending-only.

## ShareData: cột mốc có CreateTime

> ShareData cột mốc phải có CreateTime dự phòng — 04/11/2026 chọn chặn khi lưu câu UpdateTime trơn và khóa chỉ có ngày giờ; câu mẫu 101, Sửa câu, mẫu gợi ý dùng COALESCE(UpdateTime, CreateTime)

04/11/2026 người dùng nêu quy tắc ThienAn: thêm mới thì `UpdateTime` trống, nên cột mốc chỉ dùng `UpdateTime` sẽ không gửi dòng mới (DB dev: 35/299 dòng `TmsZoneStatus`). Người dùng chọn phương án (a):
- Câu mẫu seed gói 101 đổi sang `COALESCE(zs.UpdateTime, zs.CreateTime)` ở cột mốc, `WHERE`, `dataTime` (thay chốt 30/10 "không đổi câu mẫu"). CSDL đã có câu cũ chỉ đổi khi chạy seed với `@GhiDeMaGoi = N'101'` hoặc sửa trên giao diện; đổi công thức thì đối tác bị đặt lại mốc (ESH-1308).
- `SqlValidatorHelper.ValidateCursorTime` chỉ gọi khi lưu qua API (mẫu chung, SQL riêng): từ chối `sqlIncrementalUpdateTimeEmpty` (trừ câu có nhánh `UpdateTime IS NULL` như mẫu hai nhóm) và `sqlIncrementalKeyTimeOnly` (cột mốc kiểu ngày giờ).
- 05/11/2026 người dùng chốt thêm: so đúng biểu thức cột mốc bằng `>=` cũng từ chối khi lưu (`sqlIncrementalKeyFilterInclusive`, `InclusiveCursorColumn`), áp dụng mẫu chung, câu riêng, nhóm mốc. Chặn dưới theo cột thời gian gốc `COALESCE(...) >= …@lastKey…` vẫn lưu được.
- 05/11/2026 chặn thêm khi lưu: ghép ngày giờ không đi từ năm đến giây (`sqlIncrementalKeyDateUnsortable`, `UnsortableDateCursor`, trong `ValidateCursorTime`); có cột `__lastKey` mà không dùng biến mốc (`sqlIncrementalCursorVariableMissing`, `ValidateCursorColumnUsed`, gọi cả khi đổi nguồn biến). Ví dụ `FORMAT(Time, 'HHmm')` trong phần phân tích PA1 của biên bản nay cũng bị từ chối.
- 05/11/2026 người dùng chốt ép một công thức: cột mốc khác `CONVERT(varchar(23), <thời gian có CreateTime>, 121) + '_' + <khóa>` bị từ chối khi lưu (`sqlIncrementalKeyFormulaInvalid`, `InvalidFormulaCursor`), gồm cột chuỗi trơn, số ép sang chuỗi, `FORMAT`, `CONCAT`, kiểu CONVERT khác 121, phần thời gian không có `CreateTime`. Không còn dạng chỉ khóa; 16 bảng nguồn đều có `CreateTime`, `UpdateTime`, `ID` chuỗi. Tối 05/11 siết thêm: phần khóa sau `'_'` phải bắt đầu bằng cột `ID` luôn có giá trị, xem project-sharedata-cursor-holes. Dải Quy chuẩn mốc và Sửa câu theo cùng công thức. DB dev 05/11 có 19 dòng trống cả hai cột thời gian (`TmsTrafficStatistic` 18, `TmsIncident` 1): lược đồ cho phép NULL, các dòng đó không được gửi.
- Câu đã lưu từ trước: Worker vẫn chạy, chỉ ghi rủi ro qua `FindCursorRisks`, không dừng gói (khác dạng 2 cột ở ESH-1306).
- FE: `withCreateTimeFallback`, `hasBareUpdateTime` ở `cursorSections.ts`; Sửa câu đổi cả cột mốc lẫn `WHERE`; mẫu gợi ý `select-incremental` dùng `COALESCE`.
- Giữ ngoại lệ: `TmsZoneStatus` ở chiều nhận vẫn ghi `UpdateTime = ISNULL(s.dataTime, @now)` khi thêm mới vì bảng không có cột giờ dữ liệu riêng.

Cột mốc chỉ có ngày giờ không vá ở Worker: `ToString()` mất mili giây sửa được, nhưng dòng trùng giờ vẫn sót nên chặn khi lưu là đủ.

**Why:** dòng mới thêm là ca thường gặp do chính quy tắc dự án, không phải câu người dùng cố tình viết khác, nên là ngoại lệ của feedback_sharedata-scope-transcript.

**How to apply:** câu nối đuôi mới, test và ví dụ tài liệu dùng `COALESCE(UpdateTime, CreateTime)`; test Worker cố ý dùng `UpdateTime` trơn chỉ để kiểm rủi ro hoặc câu cũ. Sửa thêm quy tắc này thì cập nhật biên bản C.5, C.6, kịch bản `doc/03`, mục 5 Nối đuôi theo mốc trong bàn giao HTML.

## ShareData: lủng nối đuôi

> Rà nối đuôi 05/11/2026 ra 8 ca mất hoặc gửi lại dòng: 1, 3, 4, 5, 6, 7, 8 đã sửa có test đỏ trước; ca 2 người dùng chốt giữ nguyên, sửa tay qua khối 4. Mốc đang chạy; gói 108 DB dev người dùng tự chạy seed

05/11/2026 rà nối đuôi có đọc DB dev và chạy thử trên bản sao tạm, ra 8 ca. Tối cùng ngày người dùng bảo sửa 1, 3, 4, 5, 7, 8 và khôi phục gói 108; test ở `DataOutboundServiceTests.CursorSafety.cs`, `ShareDataPacketControllerTests`, `ShareDataPartnerControllerTests`.

Đã sửa:
1. Xóa rồi tạo lại đối tác, gói tin cùng mã: xóa mốc khi xóa và khi tạo mới (`ClearCursorsAsync`, `ClearPacketCursors`), khi tạo mới là để dọn mốc sót của dữ liệu xóa từ trước.
3. `JOIN` nhân dòng: `Extract` lấy dư một dòng khi `TOP (@biến)` (`AskOneExtraRow`), `FindDuplicatedKey` so trùng trong trang và với dòng đầu trang kế, `ValidateCursor` không gửi, giữ mốc, ghi ESH-1309. `TOP` số cố định chỉ bắt được khi trùng trong trang, dòng đầu nhóm rơi đúng cuối trang thì vẫn lọt; nhóm mốc có tên cũng chỉ so trong trang.
4. `OFFSET` khác 0: từ chối khi lưu (`sqlIncrementalOffsetNotAllowed`).
5. `UNION` nhiều nguồn một mốc: từ chối khi lưu (`sqlIncrementalUnionOneCursor`), kể cả qua bảng dẫn xuất, CTE.
7. Phần khóa: bắt đầu bằng cột `ID`, ghép thêm thì `ID` khác hoặc bọc `ISNULL`, `COALESCE`; `ID` của bảng phía tùy chọn trong OUTER JOIN bị từ chối (`HasNonEmptyIdKey`, dùng lại `sqlIncrementalKeyFormulaInvalid`).
8. API từ chối đổi mã đối tác (`partnerCodeLocked`).

Câu đã lưu từ trước vẫn chạy, 4, 5, 7 chỉ ghi rủi ro ở Nhật ký cấu hình. Riêng ca 3 dừng thật ở Worker cả với câu cũ, vì đi tiếp là mất dòng.

06/11/2026 người dùng chốt nốt:
- Ca 6 (`TOP` lồng sắp theo cột khác cột mốc): từ chối khi lưu (`sqlIncrementalInnerTopOrderInvalid`, `HasInnerTopOffCursor`), xét câu con ở FROM, JOIN, CTE, nhánh UNION; bỏ qua câu con ở cột SELECT, WHERE, bên phải APPLY.
- Ca 2 (`UpdateTime` cũ hơn mốc): **giữ nguyên, không làm phát hiện bằng Change Tracking**. Người dùng: muốn sửa thì tắt đăng ký, Đặt mốc lùi ở khối 4. Mốc đang chạy (lúc chốt là trang Mốc gửi, đã bỏ 07/11). Câu ghi gói 101 ghi vào `DEV_ITS10_Inbound`, câu gửi đọc `DEV_ITS10`, nên không đụng nhau. 20 dòng `TmsZoneStatus` `UpdateTime` sớm hơn `CreateTime` trên DB dev chưa rõ nguồn gốc.

**Gói 108 DB dev:** MCP chỉ đọc theo rule nên tôi không tự chạy. Người dùng chạy `sharedata-seed-goi-tin-ShareDataSql.sql` với `@GhiDeMaGoi = N'108'`; tham số này giữ nguyên bộ biến.

**Why:** các ca này lọt qua luật công thức PA1 vì luật cũ chỉ xét phần thời gian và chấp nhận mọi phần khóa.

**How to apply:** đừng đề xuất lại phát hiện ca 2 tự động trừ khi người dùng hỏi; sửa tiếp theo feedback_test-red-before-fix. Luật phần khóa thay câu "khóa sau '_' viết dạng nào cũng được" ở project_sharedata-updatetime-coalesce.

## ShareData: thuật ngữ mốc

> ShareData UI chỉ dùng 2 thuật ngữ mốc — \"Mốc bắt đầu\" (DefaultValue, theo câu SQL) và \"Mốc đang chạy\" (theo đối tác); mốc đang chạy ở khối 4 Cấu hình gói tin (chốt 08/11), hiện nguyên chuỗi, không chữ đặt tay

06/11/2026 người dùng thấy rối vì tab Cấu hình gói tin và tab Mốc gửi không liên kết, và một khái niệm có nhiều tên ("tham số của gói tin", "Mốc lần đầu", "mốc khởi tạo").

Chốt:
- **Mốc bắt đầu** = DefaultValue của biến @lastKey, lưu theo câu SQL (mẫu chung hoặc riêng), chỉ dùng khi đối tác chưa có mốc.
- **Mốc đang chạy** = ShareDataLastSend theo (PartnerCode, PacketCode, CursorKey); có thì luôn được ưu tiên.
- Không dùng tên khác cho hai khái niệm này ở giao diện, i18n, tài liệu.

07/11/2026 làm khối 4, 08/11/2026 người dùng CHỐT (tài liệu bàn giao, Master Plan, biên bản C, doc/03 đã sửa theo): bỏ tab Mốc gửi, mốc đang chạy thành khối "4. Mốc đang chạy" ở tab Cấu hình gói tin (`dataSource/component/cursorBlock.vue`), theo phạm vi tab (Mẫu chung: đối tác dùng mẫu chung; tab đối tác: đúng đối tác đó), chỉ hiện khi câu dùng mốc. Hộp Đặt mốc báo gửi trùng hoặc bỏ qua khi dời mốc, hộp Đặt lại nêu Mốc bắt đầu sẽ gửi lại. Đã gỡ store sharedataLink, pageLink và nút liên kết hai trang; seed menu gỡ shareDataCursor (người dùng tự chạy). Hướng này đảo chốt 03/11 "sửa mốc chỉ ở trang Mốc gửi".

Cơ sở lời họp (rà 07/11): bản gốc transcript 27/10 phần 4 [01:39–02:15] (git `fd63ee74`, dòng 327–329) bị lược khi gộp vào tệp 28-10; ngày 07/11 đã chép lại vào A-4, A.1.2 và bảng quyết định Phần C của tệp 28-10. Trong đoạn này sếp nói cột "Giá trị lần trước" bỏ được nhưng phải hiện ở chỗ khác: "một là gom chung để ở từng đối tác…, hai là một cái trang màn hình riêng". Khối 4 ở tab đối tác là cách một, trang Mốc gửi là cách hai. Danh sách ở tab Mẫu chung là mình tự thêm, nhưng mỗi dòng là một đối tác. Câu "sẽ hiện rất là nhiều" [00:31–00:45] nói về ô Giá trị đã lưu trong bảng tham số của mẫu, không nói về danh sách theo đối tác, nên khuyên giữ danh sách. Không tài liệu hay transcript nào nói mốc đang chạy dùng chung giữa các đối tác.

**Why:** chốt sếp 03/11 không hiện mốc đang chạy ở Cấu hình gói tin, nên phải nối hai trang bằng liên kết và cách gọi thống nhất.
**How to apply:** sửa UI/tài liệu mốc thì dùng đúng hai tên trên. Khối 4 có 3 cột (Đối tác, Mốc đang chạy, Thao tác); có mốc hiện nguyên chuỗi như CSDL, chưa có thì "Chưa có, lượt gửi tới lấy …"; không còn trang Mốc gửi. Liên quan project_sharedata-transcript-closed, feedback_lock-ineffective-inputs.

## ShareData: một trang mỗi lượt

> 08/11/2026 Worker bỏ vòng vét nhiều trang (`outbound_max_pages_per_run`), mỗi lượt gửi đúng 1 trang `@pageSize`; 09/11 gỡ luôn `outbound_lock_budget_percent` (mã, test, seed xóa mềm)

- Quyết định 08/11 ghi ở MasterPlan dòng 5 và `doc/03`; biên bản Phần C (C.3.1, C.3.2, C.3.3 Tầng 3 mục 5, C.4.2 ca 2) và bàn giao còn tả vòng nhiều trang, chưa sửa.
- 09/11/2026 người dùng chốt: đăng ký "Một lần" với câu nối đuôi có `TOP` chỉ gửi 1 trang rồi Hết hạn là chấp nhận được, không làm gì thêm, vì mốc theo đối tác × gói nên đăng ký mới gửi tiếp từ mốc. Luật Hết hạn không sửa, không bật lại được (`SubStateGroup.Finished`) là luật gốc của Văn Hiếu 08/2026, giữ nguyên.

**How to apply:** rà ShareData thì không báo lại hai ý trên là lỗi.

## ShareData: clone Worker

> Anh Sơn muốn clone Worker ShareData (4 con chỉ gửi, 1 con chỉ nhận, chung CSDL); đã chạy 5 tiến trình thật ở máy phát triển 02/11/2026, chưa chạy trên máy chủ, chưa đo tải SV-6

Yêu cầu từ họp 16/09/2026, nhắc lại 02/11/2026: clone Worker, 4 con chỉ gửi đóng vai A101 đến A104, 1 con chỉ nhận, dùng chung CSDL, không khai cờ thì mặc định vừa gửi vừa nhận. Biên bản đã xóa khỏi repo ở commit `88923aa7`, lấy lại bằng `git show 88923aa7^:...`.

**Why:** cờ `ShareData:Role` và `ShareData:SelfPartnerCode` có từ 02/11/2026. Cùng ngày đã chạy 5 tiến trình thật trên macOS, DB `test`, `test_inbound`, máy nhận là bản thay WebAPI: mỗi đối tác đủ dòng, không trùng, cả theo chu kỳ lẫn theo NATS. Kết quả ghi ở MasterPlan mục SV-5.

**How to apply:** Không nói "đã chạy thật trên máy chủ": còn thiếu chạy dịch vụ trên máy chủ và đo tải SV-6. Gói mới phải khai vào `ShareDataSourceTable.PacketCodes` thì `SendOnNewData` mới có tín hiệu. `DataCleanupWorker` chạy ở mọi vai trò nên các con cùng dọn một kho. Liên quan project_handover-2026-11.

**Cập nhật 02/11/2026:** đã chạy 5 tiến trình ở máy phát triển, đạt. Việc còn lại là khởi động nhiều dịch vụ trên máy chủ và kiểm vẫn chạy đúng; người dùng chốt chưa cần đo tải.

**Cập nhật 05/11/2026:** tôi tự viết script PowerShell cài dịch vụ, người dùng bác: cách làm là clone thư mục publish thành nhiều dịch vụ, mỗi bản sửa `appsettings.json` riêng; đã gỡ script. Các bước ở `bangiao/HUONG-DAN-CLONE-WORKER.md` của repo ThienAn (người dùng yêu cầu đưa vào đó). Người dùng bác cả tệp SQL kiểm tra chạy tay: ca biên phải nằm ở test tự động (`DataOutboundServiceTests.Clones.cs`), test đạt là đủ. Phần Windows (`sc.exe`, `ServiceName` đặt cứng) chưa chạy thật. Quên khai `ShareData:Role` thì bản clone chạy cả hai chiều. Lúc chạy thử trên máy này, kết nối NATS `ITS_SHAREDATA_WORKER` là Worker người dùng đang chạy, đừng tắt; dòng lỗi "bảng giám sát rỗng" là do DB test không có danh mục bảng nguồn.

**Cập nhật 10/11/2026:**
- Test 4 gửi + 1 nhận đi qua đúng controller nhận của WebAPI nằm ở `DataOutboundServiceTests.CloneReceiver.cs` (ca chính và 6 ca biên).
- Bản giao anh Sơn là zip clone sẵn 6 thư mục, tên thư mục trùng tên dịch vụ: `ShareDataWorker` (Both, giữ tên cũ), `ShareDataWorker_Inbound_1` (ReceiveOnly), `ShareDataWorker_Outbound_1` … `ShareDataWorker_Outbound_4` (SendOnly). Người dùng chốt bộ tên này; đã bác tên `inbound`, `outbound_n`, bác khai `SelfPartnerCode` ("giữ role là được"), bác kiểu zip một bản build kèm hướng dẫn, bác bước tạo đối tác trong hướng dẫn (đối tác tạo trên web như bình thường). Mỗi bản khai riêng `ShareData:Role`, `ShareData:ServiceName`, `Nats:ClientName`, `Heartbeat:Subject`; bản `ShareDataWorker` giữ bốn khóa này như bản publish; bỏ `appsettings.Development.json`.
- Bố cục zip do người dùng chốt: 6 thư mục build nằm trong thư mục `Builds`, hai tệp hướng dẫn (`DOC-TRUOC-KHI-CHAY.md`, `HUONG-DAN-CLONE-WORKER.md`) ở gốc zip; đường dẫn trong hướng dẫn là `D:\ShareData\Builds\<tên dịch vụ>`.
- Zip để trong `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker-clone-20261110.zip` (người dùng không muốn để ở Downloads) và người dùng muốn đẩy zip lên repo WEBAPI cùng mã; gần 300 MB, người dùng tự stage, commit, push. Mỗi lần dựng lại zip phải báo người dùng stage lại. Hướng dẫn dùng `sc.exe create` một lần rồi `net start`, `net stop`; không viết script cài.
- Dựng bản giao từ bản sao sạch (`git archive HEAD` cộng các tệp sharedata đang sửa), không publish thẳng working tree vì `Shared.Core.Security.dll` cục bộ sẽ lọt vào; xóa thư mục `sharedata/` khỏi bản clone vì trong repo còn một tệp xuất cũ bị git theo dõi. Publish kèm `-p:PathMap=<thư mục build>=/TA-ITS015-WEBAPI-V1.0`, nếu không dll và pdb nhúng đường dẫn thư mục tạm của máy build và đường dẫn đó hiện trong stack trace trên máy chủ.
- Tên dịch vụ Windows đọc từ `ShareData:ServiceName` (`UseConfiguredServiceName`), không khai thì `ShareDataWorker`.
- Chạy dạng dịch vụ Windows: `UseContentRootStoragePath` tính `FileStorage:BasePath` tương đối theo ContentRoot (thư mục chứa exe) nên tệp đã gửi nằm trong `sharedata/send` của chính thư mục bản clone. Người dùng bác cách đặt thư mục hiện hành trong `Program.cs`, và yêu cầu đổi tên khóa `NasStorage:BasePath` thành tên khác; đã đổi thành `FileStorage:BasePath` ở mã, cấu hình, test, tài liệu.
- Người dùng muốn có test đi qua luồng gửi thật cho việc tệp nằm trong thư mục bản clone (`ProcessSubscriptions_WhenCloneRunsAsService_SavesFilesInsideCloneFolder_Test`); test chỉ kiểm giá trị cấu hình đã bỏ. Ca 6 dịch vụ chạy cùng lúc: `ProcessSubscriptions_WhenBothServiceRunsWithInboundAndOutboundClones_EachPartnerRowsWrittenOnce_Test`.
- Hai dịch vụ cùng nhận (`ShareDataWorker` và `ShareDataWorker_Inbound_1`): mỗi gói vẫn ghi một lần nhưng thứ tự ghi giữa các gói cùng đối tác không được bảo đảm. Chưa sửa mã, đã ghi ở hướng dẫn và MasterPlan SV-5.
- NATS (kiểm 10/11/2026): từ mạng công ty không nối được các cổng NATS ở địa chỉ công khai, chỉ nối được ở địa chỉ mạng nội bộ của máy chủ. Bản giao đặt `Nats:Url` theo địa chỉ nội bộ cho cả 6 bản; `appsettings.json` mặc định trong repo chưa đổi. `nats.creds` của Worker trùng tệp của FE, JWT không giới hạn subject. Worker không khai `Nats:Streams` nên chạy pub/sub thường, `Nats:ClientName` chỉ là tên kết nối.
- `DataNatsService.InitSubscribe` luôn đăng ký lắng nghe kể cả khi NATS chưa nối được, nối được là tự nghe (người dùng yêu cầu không phải khởi động lại dịch vụ). Test `InitSubscribe_WhenNatsUnreachableAtStartup_ListensOnceNatsIsReachable_Test`: `NatsService.Instance` là kết nối dùng chung cả tiến trình test, nên test dựng cổng NATS mở muộn bằng đường nối TCP tới broker thật và trả kết nối về broker trước khi assert.
- Trang Giám sát dịch vụ (`views/tms/serviceMonitor`, danh sách thẻ lưu theo trình duyệt) không sửa: mỗi bản clone phát một subject riêng, thêm thẻ từ dòng gợi ý trong Chế độ quản lý; thẻ ShareData có sẵn là của `ShareDataWorker`.
- PR vào `dev` do anh Sơn merge, tài khoản người dùng không có quyền merge. PR chỉ chứa tệp thuộc sharedata; tệp dùng chung (`useGridPager`, `tabPage`, `baseEnum`) trả về như `dev`, hàm sharedata cần thì để ở `utils/sharedata/`.
- Người dùng chốt bỏ kênh Xuất tệp tin khỏi giao diện đối tác (chỉ còn WebAPI, FTP, Kafka); BE giữ nguyên kênh tệp, "chưa đi sâu tới delivery".
- Sửa lỗi 1 test đỏ ở kịch bản 6 dịch vụ clone (`ProcessSubscriptions_WhenBothServiceRunsWithInboundAndOutboundClones_EachPartnerRowsWrittenOnce_Test` khi `byEvent: True`): do nhiều bản gửi cùng quét danh sách đầu lượt, bản gửi sau dùng lại `sub.SerialNbr` cũ trên RAM để gửi tiếp trang sau khi bản gửi trước vừa nhả khóa dẫn đến trùng `SerialNbr` và bị WebAPI từ chối (`[400] lz.exception.sharedata.inboundPacketDuplicated`). Đã sửa bằng cơ chế OCC: thêm điều kiện `.WhereIF(currentSerial == null, s => s.SerialNbr == null)` và `.WhereIF(currentSerial != null, s => s.SerialNbr == currentSerial)` vào lệnh chiếm khóa trong `DataOutboundService.ExportSubscription`. Bộ test ShareData: 880 pass, 0 fail, 6 skip, tổng 886 test (~23 phút). Đã đóng gói lại `ShareDataWorker-clone-20261110.zip`.

## Quy chuẩn Giao diện Nguồn dữ liệu ShareData (Đối chiếu Wireframe W0.2 - Chốt 27/10/2026)

  1. Bố cục tổng thể: Giữ nguyên 3 khối dọc phân tách rõ ràng của hệ thống (Khối 1: SQL, Khối 2: Trường dữ liệu, Khối 3: Tham số).
  2. Tab Chiều Gửi / Chiều Nhận: Chuẩn theo kiến trúc phân hệ hiện tại.
  3. Toolbar hành động SQL:
     - Nút Lưu mang nhãn `Lưu & phân tích` (icon FolderChecked).
     - Phía bên trái toolbar: ở tab đối tác hiển thị Mã đối tác in đậm, Tên đối tác, và chip phiên bản mẫu (`Dựa trên mẫu v3 · mẫu hiện tại v5` hoặc `đã đồng bộ`).
     - So sánh code khác biệt (Diff Dialog) giữ nguyên giải pháp Monaco Diff của hệ thống.
  4. Khối 1: Giữ nguyên nút Mở rộng của hệ thống; loại bỏ các dòng chú thích cú pháp rườm rà dưới khung soạn SQL.
  5. Khối 2: Cột trên bảng theo chuẩn wireframe: `Khoá (alias)`, `Tên hiển thị`, `Kiểu`, `Bắt buộc`, `Ghi chú`, `Trạng thái`. Tất cả header cột được căn giữa (`header-align: 'center'`).
  6. Khối 3: Bảng Tham số loại bỏ dòng chú thích dài `Giá trị lần trước:...`; cấu trúc cột tham số giữ nguyên để phân tích sau.
  7. Chiều cao tabs (`.direction-tabs`, `.sql-partner-tabs`): Tối ưu gọn gàng (28px - 32px).

## ShareData: quyết định gom từ memory Codex

> Người dùng chốt trong phiên Codex 13–14/10/2026: ConnectionType ở ShareDataPartner, Kafka để cuối, lập lịch chỉ trong ShareData, chính sách gói 105, 110

- `ConnectionType`/`ConnectionConfig` là thuộc tính của `ShareDataPartner`, không dùng `SystemConfig`/`SystemConfigData` (người dùng sửa khi kiến trúc bị suy từ transcript).
- Kafka để cuối: "để cuối đi phải hỏi lại"; hỏi lại trước khi làm hay nghiệm thu.
- Bộ lập lịch dùng chung chỉ tích hợp trong phạm vi ShareData ("chỉ tập trung ở sharedata thôi"); không sửa Email, VideoWall, Maintenance, VMS/Equipment, TMS, Outbox.
- Gói 105: mặc định Bản chụp toàn bộ, cấu hình được Bản chụp toàn bộ hoặc Gửi nối đuôi, Worker dùng thiết lập đã lưu. Xét chính sách 105 thì đọc `02-mapping-goi-tin-101-111.md` và quyết định mới nhất trước ("105 tui thấy lần nào check bạn lộn quài"); câu transcript, spec mâu thuẫn ghi là lịch sử.
- Gói 110: tạo, cấu hình bình thường với bảng và dữ liệu giả để test toàn trình; chặn tạo `NotReady` cố định không còn là yêu cầu.

**Why:** Codex từng lưu các quyết định này trong memory riêng; 09/11/2026 người dùng chốt memory một nguồn `.agents/memory/` cho mọi công cụ nên gom về đây, bỏ phần trạng thái test cũ.

**How to apply:** coi là quyết định đã chốt; trước khi báo trạng thái vẫn đối chiếu `02-mapping-goi-tin-101-111.md` và mã hiện tại.

## ShareData: kiểm tải nhiều đối tác (11/11/2026)

> Seed kiểm tải `sql/kiem-tai/` K1–K4 chạy staging; test BE bật pooling theo chỉ đạo người dùng; bẫy hai hồ sơ ánh xạ cùng CreateTime

- Sếp muốn kiểm tải bằng **seed dữ liệu**, không thêm cấu hình hay số đo trong mã: tạo nhiều đối tác × gói, giữ 6 dịch vụ như cũ, xem CPU/RAM trên Task Manager. Người dùng chốt 50 đối tác: KT001–KT025 dùng câu mẫu chung, KT026–KT050 câu riêng biến tấu (K4), mỗi câu khác nhau.
- K1–K3 đã được một công cụ khác viết lại lúc 10:30 (khôi phục PC08 đúng ID cũ, bật PC08/TTDHT, hồ sơ viết sẵn theo gói 101–110, đăng ký cả hai chiều) và đã stage sẵn; tôi giữ thiết kế đó, chỉ đổi K1 sang gọi API nhận của chính WebAPI staging (cổng WebAPI 12204, màn hình web 12205, đường dẫn `/api/sharedata/sharedatainbound/receive`, kênh WebAPI, `{"Format":"Json","BypassSsl":"true"}`) theo yêu cầu người dùng. BypassSsl bắt buộc vì gọi bằng IP mà chứng chỉ cấp cho tên miền. Gói 110 không có câu ghi chiều nhận nên bên nhận báo lỗi, bỏ khi đối soát.
- Gói gửi mang header `PartnerCode` = mã đối tác đích, nên gửi vòng về chính WebAPI thì WebAPI nhận dưới chính đối tác đó: cần đăng ký chiều nhận và hồ sơ chiều nhận cùng bộ khung với chiều gửi.
- `BypassSsl = true` làm `HttpDeliverySender` tự tạo `HttpClient`, không đi qua `IHttpClientFactory`: test bật khóa này sẽ gọi ra mạng thật.
- Test BE: `tests/BE/appsettings.Test.json` đổi `Pooling=False` → `Pooling=True` (người dùng chốt 11/11, không giảm tải ca test). Tắt pooling thì ca 50 đối tác mở hàng nghìn kết nối, SQL Server Docker báo "pre-login handshake", 185 ca đỏ dây chuyền. Sau khi bật: lọc `Tests.ShareData` 893 ca, 890 đạt, 3 bỏ qua, 3 phút 12 giây.
- Bẫy: `SeedOutboundSubscription` luôn tạo một hồ sơ chiều gửi; test nào chèn thêm hồ sơ chiều gửi cho cùng đối tác × gói thì Worker chọn giữa hai hồ sơ cùng CreateTime không xác định (đã sửa `WhenShapeHasHeaderAndRecordArray` bằng `MappingShape`). `ProcessSubscriptions()` không khai SelfPartnerCode quét mọi đăng ký đến hạn của CSDL test, nên test chết giữa chừng để lại đăng ký còn dòng thì test sau gửi hộ.
- Test mới: `DataOutboundServiceTests.ManyPartners.cs` (trộn câu chung/riêng, dòng mới giữa lượt, một câu riêng lỗi), `DataOutboundServiceTests.LoadTestLoop.cs` (cấu hình đúng như seed: https + cổng 12204 + đường dẫn chữ thường, ghi lại URL thật bằng `DelegatingHandler`, bản nhận ghi đúng mã/tên/giờ).
- 11/11 chiều: người dùng chốt phạm vi gói = mọi gói đang bật (staging: 101–104, 106–110; 105 tắt, 111 chưa có), giữ cả 110 dù chưa có câu ghi chiều nhận; `@GoiBoQua` ở K2/K3 để bỏ gói khi cần. Không viết câu ghi 110 ("bỏ qua đi").
- Đúng ý người dùng: "không có khái niệm DB local" — entity CodeFirst thì dev, staging cùng cấu trúc; muốn kiểm CSDL nhận thì bật `EnableInitTable` cho ConfigId `ShareDataDB` trong `tests/BE/appsettings.Test.json` (đã bật 11/11). Bảng đích TMS trong `*_Inbound` không thuộc entity của ConfigId này nên vẫn cần tệp N1.
- K3 gán `SerialNbr` như màn hình (`NextSerialAsync`: số lớn nhất của đối tác, kể cả đăng ký đã xóa, + 1); để trống thì gói đầu không mang số, WebAPI không chặn trùng. Đăng ký nhận chỉ tăng số, không lùi, nên số lúc tạo lớn hơn gói đã nhận thì màn hình không phản ánh đã nhận tới đâu.
- Test HTTPS thật: `Mocks/ShareDataHttpsReceiverMock.cs` (Kestrel 127.0.0.1, chứng chỉ tự ký khác tên, chuyển vào controller nhận thật) và `DataOutboundServiceTests.LoadTestHttps.cs` (tắt BypassSsl → cảnh báo chứng chỉ có URL; cấu hình kiểm tải tự seed trong code, 3 dạng câu catalog, câu riêng biến tấu, 4+1+1 dịch vụ, nối log hai đầu qua SerialNbr/ReceiveLogId; xóa đối tác qua API lúc đang gửi dở). **Người dùng chốt 11/11: test không đọc, không chạy tệp `sql/kiem-tai/` (có thể bị xóa), tự seed trong code.** `CatalogRoundTrip.cs` chạy vòng thật 101–104, 106–109 (câu trích/ghi từ seed chuẩn ShareDataSql, bộ mã từ seed chuẩn CodeSet); đã bắt lỗi câu ghi 107 thiếu `ProcessMode` (cột NOT NULL, CSDL nhận test không có DEFAULT) → sửa seed ghi `'manual'`. `CreateWorker` mặc định dùng kết nối mạng thật: dữ liệu test không được trỏ địa chỉ máy chủ thật (đã đổi `LoadTestLoop` sang 127.0.0.1).
- **Người dùng chốt 11/11: "log tiến trình" = ghi vào bảng `ShareDataActivityLog` (Lịch sử chia sẻ), KHÔNG ghi ra tệp, không dựa Event Log.** Hiện trạng: Worker không có log tệp (`Host.CreateDefaultBuilder`), chạy dịch vụ Windows chỉ Warning+ vào Event Log, 6 bản clone chung nguồn `ShareDataWorker`; WebAPI từ chối gói (không có đối tác, tắt, trùng serial) không ghi bảng ShareData (`LogError=false`); mất khóa lúc ghi nhận trang có thể kẹt gửi trùng serial. Đã ghi prompt `Prompt/sharedata-log-truy-vet-kiem-tai-be-prompt.md` (chưa thực thi, cần chốt 3 điểm).
- Còn thiếu test: câu ghi chiều nhận catalog 102–109 mới chỉ kiểm biên dịch, chưa chạy ghi thật; bộ mã hai chiều qua API nhận; xóa dữ liệu khi Worker đang chạy.
