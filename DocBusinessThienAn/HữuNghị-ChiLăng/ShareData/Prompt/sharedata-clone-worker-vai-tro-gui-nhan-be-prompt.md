# `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Prompt/sharedata-clone-worker-vai-tro-gui-nhan-be-prompt.md`

> Backend Worker. Không có phần FE, không đổi CSDL.
> Trước khi viết mã đọc `.agents/rules/backend-dotnet.md` và `.agents/rules/testing-rules_thienan.md`.

## Mục tiêu

Chạy được đúng kịch bản anh Sơn nêu trong họp 16/09/2026: clone Worker thành nhiều con dùng chung CSDL, **4 con chỉ gửi, 1 con chỉ nhận**, để kiểm tra luồng gửi nhận khi có nhiều đối tác và đo tải CSDL.

Nguồn: biên bản `doc/transcript/16-09-2026-dinh-danh-doi-tac-va-test-tai.md`, đã xóa khỏi repo ở commit `88923aa7`. Lấy lại bằng `git show 88923aa7^:"DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/doc/transcript/16-09-2026-dinh-danh-doi-tac-va-test-tai.md"`. Các đoạn chính:

- `[10:38]` "thiết lập một cái config gì đó để biết được là cái dịch vụ này đang đóng vai trò là gửi thôi... copy thành bốn bản. Có một cái chỗ khai báo cái mã."
- `[12:00]` "Vẫn xài chung DB hết!... một thằng dịch vụ thứ năm bật cái vai trò là nhận."
- `[13:46]` "Gửi nhiều, nhận một thôi!"
- `[14:09]` "nếu mà mình không có khai báo gì hết thì mặc định là vừa gửi vừa nhận."

## Hiện trạng trong code

| Điểm | Hiện tại | Hệ quả khi clone |
|:--|:--|:--|
| `AddWorkerInfrastructure` (`ShareDataWorkerExtensions.cs`) | Đăng ký cứng 5 worker và `HeartbeatPublisher`, không đọc cờ nào | Con nào cũng vừa gửi vừa nhận |
| `ProcessSubscriptions` (2 hàm, `DataOutboundService.cs`) | Lấy mọi đăng ký đến hạn | Các con tranh nhau toàn bộ đăng ký, không con nào đóng vai riêng một đối tác |
| `ProcessGroupAsync` (`DataInboundService.cs`) | Khóa từng gói bằng cách chuyển `ProcessState` từ Pending sang Processing | Không xử lý trùng, nhưng nhiều con nhận có thể ghi gói số 2 trước gói số 1 nên không giữ được thứ tự `SerialNbr` |
| `DataNatsService.HandleMessages` | Không dùng queue group, mọi con đều nhận tín hiệu | Khóa `ProcessingUntil` chặn chạy cùng lúc, nhưng gói Snapshot bật `SendOnNewData` có thể bị gửi lại sau khi con trước nhả khóa |

`ShareData:Role` và `ShareData:SelfPartnerCode` chưa từng có trong mã WEBAPI. Lúc đóng SV-4 ngày 12/10/2026 bộ test chỉ khóa việc luôn đăng ký đủ worker.

## Việc cần làm

### 1. Cờ vai trò `ShareData:Role`

Sửa `AddWorkerInfrastructure`, đăng ký worker theo cờ:

| Giá trị | Worker được đăng ký |
|:--|:--|
| Không khai, rỗng, `Both` | Đủ cả 5 worker như hiện tại |
| `SendOnly` | `DataOutboundWorker`, `DataChangeTrackingWorker`, `DataNatsWorker`, `DataCleanupWorker` |
| `ReceiveOnly` | `DataInboundWorker`, `DataCleanupWorker` |

- `HeartbeatPublisher` đăng ký ở mọi vai trò.
- Không phân biệt hoa thường.
- Giá trị khác ba giá trị trên: ném lỗi lúc khởi động, vì chạy nhầm thành `Both` sẽ làm các con cùng nhận và hỏng kịch bản.
- Service lõi trong `AddShareDataWorkerCoreServices` giữ nguyên.

### 2. Mã định danh `ShareData:SelfPartnerCode`

- Không khai: xử lý mọi đối tác như hiện tại.
- Có khai: cả hai hàm `ProcessSubscriptions` chỉ lấy đăng ký của đối tác có `Code` bằng mã này, thêm điều kiện vào `SqlFunc.Subqueryable<ShareDataPartner>()` đang có.
- Đọc cấu hình theo nếp `DataOutboundTransport`: `IConfiguration? configuration = null` ở hàm dựng để test cũ không phải sửa.
- Không đổi `AddIdentityHeaders`: header `PartnerCode` đang lấy theo `Partner.Code`, khi dùng chung một CSDL thì đúng bằng mã con đang đóng vai. Khi hai hệ thống dùng hai CSDL riêng, mã này là mã bên nhận (MasterPlan SV-3a); trường hợp đó ngoài phạm vi prompt này.

### 3. Test viết trước, chạy đỏ rồi mới sửa

Thêm vào `tests/BE/ITS/ShareData`, dùng `[Theory]`:

- Vai trò đăng ký đúng worker cho từng giá trị ở bảng trên, gồm cả giá trị sai phải ném lỗi. Có thể lấy lại khung từ `ShareDataWorkerRoleTests.cs` đã xóa ở commit `0dab3ad4`.
- Đi qua `ProcessSubscriptions`: 4 đăng ký của A101 đến A104, con khai `SelfPartnerCode = A102` chỉ gửi đúng đăng ký của A102, 3 đăng ký còn lại giữ nguyên `NextTimeRun`.

Lệnh, không dùng `--no-build`, không chạy song song phiên khác:

```bash
dotnet test tests/BE/test.csproj -f net10.0 --filter "FullyQualifiedName~ShareData"
```

### 4. Hướng dẫn chạy thử đưa vào MasterPlan mục SV-5

1. Publish Worker rồi chép thành 5 thư mục.
2. Sửa `appsettings.json` từng thư mục:

| Con | `ShareData:Role` | `ShareData:SelfPartnerCode` | `Nats:ClientName` |
|:--|:--|:--|:--|
| 1 đến 4 | `SendOnly` | `A101` đến `A104` | `ITS_SHAREDATA_A101` đến `ITS_SHAREDATA_A104` |
| 5 | `ReceiveOnly` | Không khai | `ITS_SHAREDATA_RECEIVER` |

Cả 5 con đặt chung `ConnectionStrings` và cùng một đường dẫn tuyệt đối cho `NasStorage:BasePath`, nếu không tệp bằng chứng sẽ nằm rải ở từng thư mục.

Con nhận bắt buộc có `ConnectionStrings:InboundConnection`, thiếu thì `DataInboundWorker` chỉ ghi cảnh báo và không chạy. WebAPI và con nhận phải trỏ cùng một CSDL nhận: WebAPI lưu `ShareDataInboundPacket` theo kết nối tenant `ShareData`, con nhận đọc theo `InboundConnection`, lệch nhau thì con nhận không thấy gói và gói nằm mãi ở Pending.

3. Cài service tên khác nhau bằng `sc create`, ví dụ `ShareDataWorker-A101`. `Program.cs` đang đặt cứng `ServiceName = "ShareDataWorker"`; theo tài liệu Windows, service chạy tiến trình riêng thì tên này được bỏ qua, nhưng **chưa thử thực tế**, nên cài thử một con trước.
4. Trên giao diện tạo đối tác A101 đến A104, trỏ địa chỉ về WebAPI, endpoint `api/sharedata/sharedatainbound/receive`. Mỗi đối tác khai đăng ký gửi các gói khác nhau, ví dụ A101 gói 101, 102, 103; A102 gói 101, 105, 106; đồng thời khai đăng ký nhận, ánh xạ chiều nhận và câu ghi cho các gói đó.
   - Kết nối cả 4 đối tác và bật các đăng ký. Worker chỉ lấy đối tác đang bật, `SessionState = Connected` và đăng ký `Active`; thiếu một điều kiện thì không gửi và chỉ ghi log "Không có Subscription nào đến hạn".
   - A101 và A102 cùng gửi gói 101 từ cùng dữ liệu nguồn, nên bảng đích và khóa của câu ghi phải có mã đối tác (dùng `@partnerCode`). Nếu không, các đối tác ghi đè nhau và không kiểm được tiêu chí "không lẫn nhau" ở bước 5.
5. Đạt khi:
   - `ShareDataInboundPacket` có gói của đủ 4 mã đối tác, tất cả chuyển sang Done.
   - Dữ liệu 4 đối tác được ghi đủ, không lẫn nhau.
   - `ShareDataAlertLog` không có mã ESH-15xx mới.
6. Đo CPU, RAM, khóa của CSDL trong lúc chạy và ghi vào SV-6. Máy `10.10.8.30` dùng chung nên chọn giờ ít người dùng.

Lưu ý: tất cả các con gửi cùng phát heartbeat lên `ta.its.heartbeat.sharedata`, màn giám sát không phân biệt được con nào còn sống.

## Việc cuối — Cập nhật lại tài liệu gốc

Sửa `Plan/Sharedata_MasterPlan.md`:

- **SV-4:** ghi cờ vai trò đã làm; sửa "4 hosted services" thành 5 worker; bỏ `claimedAt`, thực tế là chuyển `ProcessState`; thay tên test `ShareDataWorkerRoleTests` và `DataOutboundServiceTests.SelfIdentity` bằng test đang có.
- **SV-5:** ghi rõ test hiện có chạy trong một tiến trình với máy nhận giả lập, thêm hướng dẫn ở mục 4 và kết quả chạy thật.
- **SV-6:** ghi số đo sau khi chạy.
- **SV-13 mở rộng:** dòng "Đã hoàn thành 100% (Job Hangfire `sharedata-log-retention-scan`)" sai, job này không có trong mã và lịch sử Git của WEBAPI. Sửa thành Chưa làm.

Cập nhật dòng của prompt này trong `Prompt/README.md`.
