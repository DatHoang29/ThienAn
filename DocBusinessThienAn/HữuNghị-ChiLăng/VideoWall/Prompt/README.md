# VideoWall — Prompt

Thư mục này chứa **prompt thực thi** cho phân hệ VideoWall (`<task-slug>-prompt.md`). Sau khi một task đã triển khai, kiểm thử và nghiệm thu xong, prompt được giữ lại để lập trình viên review và đối chiếu sau khi code change hoàn tất (chỉ xóa khi người dùng trực tiếp yêu cầu theo `.agents/rules/thienan_rules.md`).

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

---

## Fix UI sau test nhánh fix/20261005-videowall-validation (08/10/2026)

| Prompt | Nội dung | Trạng thái |
|---|---|---|
| [`videowall-schedule-radio-hanh-dong-don-le`](videowall-schedule-radio-hanh-dong-don-le-prompt.md) | Form Lập lịch: field "Hành động" chỉ còn 1 lựa chọn (`activate_scene`) nên radio-group vô nghĩa — đổi sang điền sẵn + hiển thị chữ tĩnh, tự phục hồi radio khi BE mở thêm hành động | ⚠️ **Chờ áp dụng** |

---

## Đợt refactor bộ test (30/09/2026)

> 🔴 **Thứ tự áp BẮT BUỘC, ⛔ không được đảo.** Sổ theo dõi trạng thái là [`../Plan/Test_Refactor_MasterPlan.md`](../Plan/Test_Refactor_MasterPlan.md) — tra trạng thái ở đó, ⛔ không tra ở bảng này.

| # | Prompt | Nội dung | Trạng thái |
|---|---|---|---|
| 01 | [`videowall-don-blank-line-nhan-doi`](videowall-don-blank-line-nhan-doi-prompt.md) | Phục hồi 16 tệp test bị nhân đôi toàn bộ dòng trống. Thuần whitespace, kèm script nhận biết theo vùng và bất biến "dòng không rỗng phải y nguyên" | ✅ **30/09/2026 · Đã thực thi** |
| 02 | [`videowall-mirror-thu-muc-test-theo-project`](videowall-mirror-thu-muc-test-theo-project-prompt.md) | Dời 25 tệp thành `WebApi/` + `Worker/` + `Wpf/` phản chiếu cây project nguồn. 🔴 Sửa `test.csproj` cùng lượt | ✅ **30/09/2026 · Đã thực thi** |
| 03 | [`videowall-doi-ten-va-tach-vai-mock-server`](videowall-doi-ten-va-tach-vai-mock-server-prompt.md) | `VwISAPIMockServerHikvision` → `VwISAPIServerHikvisionMock`; 11 tệp partial → 14 tệp tách theo vai (hạ tầng · Auth · Scenario · DeviceState · Routes) | ✅ **30/09/2026 · Đã thực thi** |
| 04 | [`test-chuan-hoa-ten-lop-gia-lap`](test-chuan-hoa-ten-lop-gia-lap-prompt.md) | `FakeStringLocalizer` xoá (dùng `host.Localizer`) · `RecordingPublisherTest` → `PublisherSpy` · `FakeSqlException` → `SqlExceptionFake` | ✅ **30/09/2026 · Đã thực thi** |
| 05 | `sharedata-chuyen-mock-http-sang-server-that` | 📌 Nằm ở `ShareData/Prompt/`. ShareData bỏ `HttpMessageHandler` giả, chuyển ≈30 call site sang `HttpListener` thật; xoá `tests/Mock/` | ✅ **30/09/2026 · Đã thực thi** |
| 06 | `test-cap-nhat-rule-sau-refactor` | Đồng bộ mục 6.3, 7, 15, 19.18 của `thienan_rules.md` với cách làm mới | ✅ **30/09/2026 · Đã thực thi** |
| 07 | `videowall-go-compatibility-shim-phan-quyen` | Gỡ lớp đệm `Compatibility/` (2 tệp khai báo kiểu trong namespace sản xuất). Xoá 7 bài test bám mô hình quyền cũ theo `TenantId`; giữ 4 bài bằng cách bỏ dòng đi qua lớp đệm. 📌 Áp độc lập với 04–06 | ✅ **30/09/2026 · Đã thực thi** |
| 08 | `test-gom-phan-he-vao-thu-muc-its` | Gom `ShareData/` + `VideoWall/` vào `tests/ITS/`; `VwMockServerRunner/` về trong VideoWall; namespace bỏ đoạn `Modules` thành `Tests.<PhânHệ>.*`. 🔴 Sửa `test.csproj` cùng lượt | ✅ **30/09/2026 · Đã thực thi** |

> 🔴 **Vì sao 01 phải áp trước tiên:** 16 tệp test đang bị nhân đôi dòng trống. Mọi prompt sau đều trích đoạn code; trích trên nền đã hỏng thì đoạn "trước khi sửa" ⛔ không khớp được với tệp trên đĩa.
> 🔴 **Vì sao 06 áp trước 07/08:** rule chỉ được viết lại sau khi cách làm mới đã chạy xanh, ⛔ không viết rule cho thứ chưa kiểm chứng.
> 🔴 **Còn lại phải áp đúng thứ tự `07 → 08`:** prompt 07 trích đường dẫn theo layout hiện tại (`tests/VideoWall/WebApi/...`) và xoá `tests/VideoWall/Compatibility/`; áp 08 trước là phải viết lại toàn bộ đường dẫn trong 07.

> 📌 **Điều kiện chạy:** SQL Server **local** (mục 11 của `thienan_rules.md`). Các bài NATS cần broker NATS ở localhost — `Host.GuardAllConnectionsLocal` chặn cứng mọi endpoint không phải local.

---

## Đợt trước — đã đóng

| Prompt | Nội dung | Trạng thái |
|---|---|---|
| [`videowall-bo-mock-nats-publisher`](videowall-bo-mock-nats-publisher-prompt.md) | Bỏ `FakeVwPublisher` và `FakeNatsPublisherTest` khỏi 7 bài test, chuyển sang `IVwPublisher` / `IVwNatsPublisher` thật theo quy tắc 19.18 | ✅ **Đã thực thi** · xác minh lại 30/09/2026: ⛔ không còn `FakeVwPublisher` / `FakeNatsPublisherTest` trong `tests/`; cả 3 nơi dùng publisher đều lấy bản thật qua `host.Services.GetRequiredService<IVwNatsPublisher>()` |
| [`videowall-bo-cleanup-trong-test`](videowall-bo-cleanup-trong-test-prompt.md) | Gỡ dọn dữ liệu trong từng bài test, trả việc dọn về `Host.ClearAllData()` theo mục 6 | ✅ **Đã thực thi** · xác minh lại 30/09/2026: 4 lệnh dọn vô tác dụng (dùng `Guid.NewGuid()` sinh tại chỗ nên ⛔ không khớp dòng nào) đã bị xoá; các khối `finally` nhóm A đã gỡ |

> ⚠️ Hai dòng trên trước đây ghi *"Chưa thực thi"* — sai. Đã đối chiếu trực tiếp với mã nguồn ngày 30/09/2026 và đóng lại.
> 📌 Còn 8 lệnh `Deleteable<VwController>()` / `Deleteable<VwScreen>()` **không mệnh đề lọc** ở phần Arrange của `VwWindowSceneTests` và `VwCascadeIntegrationTests` — đó là dọn kiểu Arrange, ⛔ không thuộc phạm vi prompt `bo-cleanup-trong-test`. Đã ghi vào mục *"Việc chưa làm"* của MasterPlan để chủ dự án quyết.
