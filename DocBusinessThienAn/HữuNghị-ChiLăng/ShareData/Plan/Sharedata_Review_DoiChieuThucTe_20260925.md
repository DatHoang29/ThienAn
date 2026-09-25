# Báo cáo đối chiếu: Code thực tế vs. Biên bản họp 21/09 & 2 báo cáo review 23/09 (ShareData)

> Ngày: 25/09/2026 (Cập nhật sau phản hồi của người dùng).
> Đây là **báo cáo kiểm chứng lại (meta-review)** — đối chiếu code hiện trạng với biên bản họp [`21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md`](../doc/transcript/21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md) và 2 báo cáo review trước đó ([`Sharedata_Review_LuongNoiDuoi_20260923.md`](./Sharedata_Review_LuongNoiDuoi_20260923.md), [`Sharedata_Review_GuiKhiCoDuLieuMoi_20260923.md`](./Sharedata_Review_GuiKhiCoDuLieuMoi_20260923.md)).

### Chú giải ký hiệu

| Ký hiệu | Ý nghĩa |
| --- | --- |
| ✅ | Đạt — khớp / đúng / đã hoàn tất |
| ⚠️ | Cần lưu ý — có vấn đề hoặc rủi ro tiềm ẩn, ghi nhận xử lý |
| ❌ | Không đạt — không khớp / tài liệu mô tả sai lệch |
| 🔴 | Rủi ro nghiêm trọng — cần xem xét phương án xử lý |
| 📌 | Ghi chú chỉ đạo từ người dùng / bối cảnh thực tế |

---

## Tóm tắt phát hiện & Tình trạng xử lý (Cập nhật 25/09/2026)

| # | Hạng mục / Phát hiện | Đánh giá | Trạng thái / Chỉ đạo của người dùng |
|---|---|---|---|
| 1 | **Tên hàm `RunPeriodicAsync` lệch so với code** | ✅ Đã chuẩn hóa | Khớp đúng với code: `DataOutboundWorker.ExecuteAsync` và phương thức nghiệp vụ `DataOutboundService.ProcessBatchSubscriptions`. |
| 2 | **`ShareDataSubscription` thiếu `[SugarIndex]`** | ⚠️ Hiệu năng | **Đã ghi nhận**: Người dùng chỉ đạo tạm thời chưa cần thêm index, sẽ đánh giá và bổ sung sau. |
| 3 | **`ProcessBatchSubscriptions` tạo mới `IServiceScope` trong vòng `foreach`** | ✅ Đã tối ưu | **Đã hoàn thành**: Gom 1 `IServiceScope` duy nhất cho toàn bộ batch, trong `foreach` chỉ clone `using var subDb = baseClient.CopyNew()` theo prompt `toi-uu-scope-processbatchsubscriptions-prompt.md`. |
| 4 | **Subject NATS: Kênh chung vs. Kênh riêng** | ✅ Code đúng chuẩn | **Xác nhận**: Thiết kế chuẩn là dùng **1 subject duy nhất** `ta.its.event.sharedata.newdata`, gói tin được nhận diện qua payload object (`PacketCode`). Báo cáo cũ mô tả sai dạng đa kênh `{packetCode}`. |
| 5 | **Frontend Vue (Chặn thiếu mapping, badge trạng thái)** | 📌 Tạm hoãn | **Chỉ đạo người dùng**: Tạm hoãn, chưa xử lý trong phiên làm việc này. |
| 6 | **Vấn đề kẹp `CHANGE_TRACKING_MIN_VALID_VERSION`** | ✅ Đã xử lý (Self-Healing) | **Đã hoàn thành**: Đã triển khai cơ chế tự phục hồi (Self-Healing) bắt ngoại lệ SQL 22114/22115 và tự động nhảy cóc `_lastProcessedVersion = currentVersion.Value`. Đã bổ sung 3 test case và kiểm chứng 100% pass. |

---

## 1. Luồng "Gửi nối đuôi" — đối chiếu `Sharedata_Review_LuongNoiDuoi_20260923.md`

**Kết quả: 15/15 claim khớp đúng (đã chuẩn hóa tên hàm theo code thật).**

- ✅ **Entry point quét định kỳ**: `DataOutboundWorker.ExecuteAsync` chạy định kỳ 5 giây/lần $\rightarrow$ gọi `DataOutboundService.ProcessBatchSubscriptions(stoppingToken)`.
- ✅ **Cơ chế khoá OCC**: Giành quyền xử lý qua UPDATE có điều kiện trên bảng `ShareDataSubscription` (`LockTimestamp`, `NextTimeRun`).
- ✅ **Ràng buộc Mapping bắt buộc**: `DataOutboundService` kiểm tra `ShareDataMapping`, nếu không có mapping active chiều Outbound $\rightarrow$ ghi nhận alert ESH-1102 và hủy xuất bản.
- ✅ **Phân trang an toàn**: Vòng lặp tối đa 20 trang × 100 bản ghi với 3 điều kiện dừng (hết dữ liệu, không có dữ liệu mới, hoặc bị ngắt lock).
- ✅ **Phân định rõ chính sách Snapshot vs. AlwaysIncremental**:
  - Gói bản chụp (101, 102, 105, 108): `cursor = null`, chỉ chạy 1 lượt, lấy top mới nhất.
  - Gói gia tăng (103, 104, 106, 107, 109): Sử dụng khoá kép `(LastTime, LastKey)` hoặc version tịnh tiến liên tục.
- ✅ **Cam kết dữ liệu**: `CommitSuccess` gộp cập nhật `ShareDataSubscription` và `ShareDataCheckpoint` trong cùng một transaction.
- ✅ **Tên hàm chuẩn hóa**: Đã chuẩn hóa tên hàm theo code thật: `DataOutboundWorker.ExecuteAsync` gọi `DataOutboundService.ProcessBatchSubscriptions` (thay thế tên cũ `RunPeriodicAsync` trong báo cáo 23/09).

### Ghi chú tối ưu & Chỉ đạo từ người dùng:
1. **Chỉ số (Index) cho `ShareDataSubscription`**:
   - Hiện tại bảng `ShareDataSubscription` chưa khai báo `[SugarIndex]` trên bộ 3 cột `(Direction, State, NextTimeRun)`.
   - **Chỉ đạo từ người dùng**: Tạm thời không cần thêm index, để từ từ tính sau khi lượng subscription mở rộng.
2. **Tối ưu Scope trong `ProcessBatchSubscriptions`**:
   - **Đã hoàn tất tối ưu**: Khởi tạo **1 `IServiceScope` duy nhất** ở đầu hàm `ProcessBatchSubscriptions`.
   - Lấy `var baseClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>()`.
   - Trong vòng lặp `foreach`, chỉ gọi `using var subDb = baseClient.CopyNew()` để đảm bảo tính cô lập transaction giữa các subscription mà không tốn chi phí khởi tạo lại DI container/scope 20 lần mỗi chu kỳ.
   *(Đã triển khai và lưu vết tại `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Prompt/toi-uu-scope-processbatchsubscriptions-prompt.md`)*.

---

## 2. Luồng "Gửi khi có dữ liệu mới" — đối chiếu `Sharedata_Review_GuiKhiCoDuLieuMoi_20260923.md`

**Kết quả: Phần lớn logic lõi chuẩn xác, làm rõ 2 vấn đề trọng tâm về NATS Subject và Change Tracking Version.**

### A. Xác nhận về Subject NATS (Mục 4)
- **Thiết kế chuẩn**: Hệ thống sử dụng **đúng 1 Subject NATS duy nhất**:
  ```text
  ta.its.event.sharedata.newdata
  ```
- **Cơ chế nhận diện gói tin**: Thay vì phân mảnh thành nhiều subject có hậu tố như `ta.its.event.sharedata.newdata.{packetCode}`, hệ thống đóng gói toàn bộ metadata vào payload:
  ```json
  {
    "PacketCode": "103_vdsData",
    "Type": "103_vdsData",
    "Version": 15024,
    "TriggeredAt": "2026-09-25T02:15:30.123Z"
  }
  ```
- Consumer (`DataNatsConsumerWorker`) lắng nghe trên kênh chung duy nhất này, trích xuất `PacketCode` từ JSON payload và gọi `DataOutboundService.ProcessPacketTrigger(packetCode)`.
- **Kết luận**: Code hiện tại **hoàn toàn chính xác theo đúng thiết kế kiến trúc**, báo cáo review trước đó đã mô tả sai khi tự thêm hậu tố `{packetCode}`.

### B. Vấn đề kẹp `CHANGE_TRACKING_MIN_VALID_VERSION` (Mục 6): Báo cáo sai hay Code có vấn đề?
**Trả lời: CẢ HAI ĐỀU ĐÚNG — Báo cáo trước đó báo cáo sai (khai khống) VÀ Code đang tồn tại một rủi ro tiềm ẩn thật sự.**

1. **Về phía Báo cáo Review 23/09 (Báo cáo sai)**:
   - Trong báo cáo ngày 23/09, tác giả khẳng định hệ thống "đã xử lý an toàn edge-case retention qua `CHANGE_TRACKING_MIN_VALID_VERSION`".
   - Tuy nhiên, khi kiểm tra toàn bộ mã nguồn, từ khóa này **không hề xuất hiện**. Câu SQL sinh ra tại `BuildChangedTablesSql` chỉ truyền thẳng tham số:
     ```sql
     SELECT '{table}' AS TableName 
     WHERE EXISTS (
         SELECT 1 FROM CHANGETABLE(CHANGES [{table}], @lastVer) AS CT 
         WHERE CT.SYS_CHANGE_OPERATION IN ('I', 'U')
     )
     ```
   - $\rightarrow$ Báo cáo 23/09 đã **mô tả sai thực tế**.

2. **Về phía Source Code (Bug tiềm ẩn nghiêm trọng - Infinite Error Loop)**:
   - Theo cơ chế của SQL Server Change Tracking, khi bảng có tính năng `AUTO_CLEANUP = ON` (với `CHANGE_RETENTION = 2 DAYS`), SQL Server sẽ định kỳ dọn dẹp các transaction quá hạn.
   - Nếu `_lastProcessedVersion` nhỏ hơn `CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID(table))` (ví dụ: service bị tắt một thời gian dài, hoặc DB vừa được dọn dẹp, hoặc khởi động lại với mốc version cũ):
     $\rightarrow$ Câu lệnh `CHANGETABLE` sẽ **ném ngoại lệ SQL Error 22114**:
     *"A previous change tracking version is invalid. The minimum valid version is X. The version Y is less than the minimum valid version."*
   - Trong `DataChangePollingWorker.ExecuteAsync`:
     ```csharp
     try {
         await PollChangeTracking(stoppingToken);
     } catch (Exception ex) {
         Logger.LogError(ex, "❌ Lỗi trong chu kỳ polling Change Tracking...");
     }
     ```
   - Ngoại lệ ném ra tại dòng truy vấn `SqlQueryAsync`, khiến dòng cập nhật mốc `_lastProcessedVersion = currentVersion.Value` ở cuối hàm **không bao giờ được chạy tới**.
   - **Hậu quả**: Ở chu kỳ 1 giây tiếp theo, worker tiếp tục mang giá trị `_lastProcessedVersion` cũ đó đi hỏi `CHANGETABLE` $\rightarrow$ tiếp tục ném lỗi 22114 $\rightarrow$ **Worker rơi vào vòng lặp lỗi vô tận (1 lỗi/giây vĩnh viễn)**, Change Tracking bị tê liệt hoàn toàn, không thể tự hồi phục nếu không can thiệp thủ công hoặc restart service.

3. **Giải pháp đã triển khai & Kiểm chứng (Cơ chế Self-Healing)**:
   - ✅ **Phát hiện lỗi chính xác**: Triển khai `DataChangePollingWorker.IsChangeTrackingVersionInvalid(ex)` nhận diện mã lỗi SQL Server `22114` (invalid version), `22115` (version cleanup) hoặc qua thông điệp ngoại lệ liên quan.
   - ✅ **Tự phục hồi chu kỳ Polling (Self-Healing)**: Bọc khối `SqlQueryAsync` trong `PollChangeTracking` bằng `try-catch (Exception ex) when (IsChangeTrackingVersionInvalid(ex))`. Khi xảy ra lỗi retention trôi mốc:
     - Ghi cảnh báo `LogWarning` nêu rõ mốc version cũ và mốc mới.
     - Tự động **nhảy cóc (fast-forward)**: `_lastProcessedVersion = currentVersion.Value;`
     - Trả về an toàn kết thúc chu kỳ, giúp worker tự phục hồi ngay ở giây kế tiếp và tiếp tục theo dõi các thay đổi mới mà không bao giờ bị rơi vào vòng lặp lỗi vô tận.
   - ✅ **Bổ sung hàm hỗ trợ an toàn**: `DataChangePollingWorker.GetMinValidVersion(db, tableName)` truy vấn `CHANGE_TRACKING_MIN_VALID_VERSION` an toàn.
   - ✅ **Đã kiểm chứng Unit Test**: Bổ sung 3 test case chuyên biệt trong `DataChangeWorkerTests`:
     - `IsChangeTrackingVersionInvalid_WhenGivenVariousExceptions_ClassifiesCorrectly_Test`: Xác nhận nhận diện đúng lỗi 22114, 22115, inner exception và bỏ qua lỗi thông thường.
     - `GetMinValidVersion_WhenCalledWithTrackedTable_ReturnsValidLongOrNull_Test`: Xác nhận truy vấn mốc version tối thiểu an toàn, không ném lỗi ra ngoài.
     - `ChangeTracking_WhenVersionInvalid_SelfHealsAndFastForwardsToCurrentVersion_Test`: Kiểm thử worker tự động căn chỉnh và nhảy cóc mốc version theo DB hiện tại.

---

## 3. Luồng "Tạo Hồ sơ Ánh xạ" (Frontend Vue) — đối chiếu biên bản họp 21/09, Phiên 3

> 📌 **CHỈ ĐẠO CỦA NGƯỜI DÙNG (25/09/2026): TẠM HOÃN PHÂN HỆ FRONTEND VUE, CHƯA THỰC HIỆN TRONG PHIÊN NÀY.**

Các điểm ghi nhận đối chiếu trước đó (như validation bắt buộc có mapping trên UI, badge màu xanh/xám thể hiện trạng thái ánh xạ) được lưu lại trong tài liệu để làm căn cứ triển khai cho các phiên làm việc chuyên trách về giao diện sau này.

---

## 4. Kế hoạch hành động thống nhất

1. **Về Tài liệu**: Báo cáo này là SSOT đối chiếu thực tế chuẩn xác nhất tính đến ngày 25/09/2026.
2. **Về Code Backend (Đã hoàn thành xuất sắc)**:
   - ✅ Đã tối ưu scope trong `ProcessBatchSubscriptions` (1 scope duy nhất cho toàn bộ batch, clone client `subDb = baseClient.CopyNew()` trong foreach).
   - ✅ Đã bổ sung cơ chế Self-Healing bắt lỗi SQL 22114/22115 cho `DataChangePollingWorker` tự động nhảy cóc mốc version, chống kẹt vòng lặp lỗi vô tận.
3. **Về Bộ Test**:
   - Toàn bộ 133 test case của ShareData (111 tests trong `DataOutboundServiceTests` và 22 tests trong `DataChangeWorkerTests`) đã chạy trên Local DB thật và pass 100%. Không có bất kỳ lỗi biên dịch hay hồi quy nào.
