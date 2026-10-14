Đường dẫn tệp prompt: `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Prompt/sharedata-fix-sau-review-prompt.md`

# Sửa bảy điểm còn lại sau review ShareData

## Mục tiêu và phạm vi

Thực hiện **đúng bảy việc** bên dưới theo thứ tự, dựa trên trạng thái mã hiện tại chứ không áp dụng máy móc nhận xét review nếu mã đã đổi. Tuân thủ `.agents/rules/thienan_rules.md`, quy tắc backend/frontend/test và quy chuẩn tài liệu của dự án. Chỉ sửa ShareData trong WEBAPI, ShareDataWorker, WEBVUE, component lịch dùng chung, test ShareData và tài liệu ShareData. Không sửa code Email, VideoWall, Bảo trì, VMS/Equipment, TMS hoặc Outbox. Không tự `git add`, `git reset`, `git commit`, `git push` hay chạy build frontend.

**Quyết định giữ nguyên:** `ConnectionType` và `ConnectionConfig` nằm trong `ShareDataPartner`, không chuyển sang `SystemConfig`/`SystemConfigData`. Component lịch được thiết kế dùng chung nhưng đợt này chỉ tích hợp ShareData. Kafka chưa triển khai: để cuối, báo rủi ro hiện tại và **hỏi lại người dùng trước khi bắt đầu**, không sửa Kafka trong prompt này.

## 1. Chặn cấu hình kết nối sai trước khi lưu

- Hiện `editPartner.vue` cho nhập `ConnectionConfig` tự do; `PartnerValidator` chưa kiểm tra trường này. `ShareDataConnectionDispatcher.BuildRequest` bắt lỗi JSON rồi đặt `RawConfig`, trong khi không có sender nào tiêu thụ khóa đó. Với File, `OutputPath` sai định dạng có thể bị bỏ qua và worker ghi ra đường dẫn mặc định.
- Chốt một hợp đồng rõ cho `ConnectionConfig` theo từng Type đang hoạt động (`API`, `File`): định dạng JSON object, khóa cho phép, kiểu/miền giá trị và trường bắt buộc/tùy chọn. Validate trên WEBAPI lúc thêm/sửa đối tác, trả lỗi cụ thể; WEBVUE hướng dẫn và kiểm tra tương ứng. Không để `RawConfig` biến cấu hình lỗi thành cấu hình có vẻ hợp lệ. Giữ hành vi mặc định có chủ đích khi cấu hình thật sự để trống.
- Kiểm thử với cấu hình hợp lệ, JSON sai, sai kiểu/giá trị và File `OutputPath` hợp lệ; chứng minh không có bản ghi sai được lưu và không xuất tệp sang đường dẫn khác ngoài ý định.

## 2. Tách điều kiện Address/Port theo Type

- Đối chiếu luồng kết nối/nhận dữ liệu hiện có trước khi đổi validator. Với đối tác **chỉ xuất File**, cho lưu cấu hình File hợp lệ mà không phải điền Address/Port HTTP giả; với API vẫn giữ điều kiện địa chỉ/cổng cần thiết. Nếu một đối tác vừa nhận qua mạng vừa xuất File, bảo toàn ràng buộc của luồng nhận và nêu rõ trường nào phục vụ luồng nào.
- Đồng bộ điều kiện bắt buộc, hiển thị và thông báo lỗi ở `editPartner.vue` và `PartnerValidator`; không xóa dữ liệu Address/Port cũ khi đổi Type nếu còn được luồng khác sử dụng. Thêm test tạo/sửa File và API ở đúng ranh giới nghiệp vụ.

## 3. Khôi phục và giữ tệp prompt

- Kiểm tra vì sao `Prompt/sharedata-hoan-thien-sau-ra-soat-prompt.md` không còn và có hay không yêu cầu trực tiếp của người dùng cho phép xóa. **Chỉ khi không có yêu cầu xóa**, khôi phục nội dung gốc từ lịch sử/tài liệu hội thoại đáng tin cậy; không bịa phần không truy được. Nếu không thể khôi phục chính xác, báo rõ giới hạn và nguồn của mọi bản tái dựng.
- Rà các mục R1/R2 ở MasterPlan đang ghi prompt “đã xóa”; chỉ khôi phục tệp nào bị xóa trái quy tắc và có nguồn đáng tin cậy. Nếu người dùng đã yêu cầu xóa một tệp cụ thể, giữ nguyên quyết định đó và ghi rõ trong tài liệu. Giữ lại tệp prompt hiện tại sau thực thi, trừ khi người dùng trực tiếp yêu cầu xóa. Không làm thay đổi lịch sử transcript.

## 4. Sửa các ca Playwright thất bại rồi cập nhật kết luận nghiệm thu

- Lần review gần nhất chạy `datasource.test.ts` và `subscription.test.ts` trên Chromium/Firefox/WebKit: 36 pass, 21 fail, 3 skip; một ca mở trang trắng và không tìm thấy `.sub-direction-tabs`. **Không mặc định tất cả là lỗi sản phẩm.** Tái hiện ca nhỏ nhất, kiểm tra bootstrap, route/API mock, browser console và DOM, rồi sửa đúng nguyên nhân trong ShareData hoặc test harness liên quan.
- Chạy lại các ca bị ảnh hưởng và bộ ShareData FE cần thiết. Giữ artifacts khi fail; khi pass toàn bộ thì dọn artifacts tạm theo quy tắc dự án. Không chạy `npm run build`/`pnpm build` frontend.
- Chỉ sau khi có kết quả thật mới giữ hoặc cập nhật phát biểu “Playwright pass 100%” tại `Sharedata_MasterPlan.md`. Nếu còn lỗi môi trường, ghi rõ số ca và nguyên nhân chưa xác minh; không đánh dấu hoàn tất.

## 5. Kiểm chứng rollback khi ghi audit SQL thất bại

- Mã hiện đặt cập nhật `ExtractSql` và `LogUpdateMandatoryAsync` trong transaction; test hiện chỉ chứng minh đường thành công. Thêm một bài xUnit qua service/repository thật và SQL Server local để **gây lỗi ghi audit có kiểm soát**, rồi xác nhận cập nhật SQL không thành công, giá trị `ExtractSql` cũ còn nguyên và không có trạng thái audit thành công giả.
- Tuân thủ quy tắc test: không Moq/NSubstitute hoặc fake service nghiệp vụ, không `try-catch`/`try-finally` quanh Act/Assert, không tự xóa bảng trong test method, không tạo test-only seam ở production. Nếu không có cách gây lỗi an toàn với hạ tầng test hiện tại, nêu rõ rào cản thay vì viết test soi source hoặc test không chứng minh rollback.

## 6. Test hành vi component lịch và giữ thiết kế dùng chung

- Thay các assertion chỉ `fs.readFileSync`/`toContain("allowedKinds.includes(...)")` bằng kiểm thử hành vi có giá trị: trên ShareData, mode được phép hiển thị và chọn đúng, mode không được phép không thể chọn, dữ liệu lưu/tải lại giữ đúng lịch hiện có; kiểm tra component khi nhận tập `allowedKinds` khác nhau bằng cách mount trong hạ tầng **test** phù hợp, không thêm route hoặc hook chỉ phục vụ test vào production.
- Component vẫn hỗ trợ `continuous`, `daily`, `weekly`, `interval` qua props, nhưng chỉ đưa ra mode mà backend ShareData xử lý được. Kiểm tra trạng thái khi chỉ có một mode được phép và khi `kind` hiện tại không nằm trong `allowedKinds`; không hiển thị biểu mẫu của mode bị cấm. Giữ giao diện RadioButton và i18n đúng hai ngôn ngữ. Không tích hợp component vào Email/VideoWall/Bảo trì trong đợt này.
- Nếu test không thể chạy do hạ tầng Playwright, giải quyết mục 4 trước rồi mới ghi nghiệm thu component.

## 7. Dùng typed API client cho SQL Center

- Trong `views/sharedata/dataSource/index.vue`, thay các lời gọi `axiosInstance` và URL API viết cứng của `getextractsql`, `parseextractcolumns`, `executeextractsqlsample`, `updateextractsql`, `syncextractcolumns` bằng `getAPI(...)` và hàm client sinh từ OpenAPI.
- Nếu client chưa có hàm, dùng quy trình codegen hiện hành từ WEBAPI trước; **không sửa tay** `src/api-services/`. Giữ nguyên kiểm tra SuperAdmin ở cả FE và BE, base path, payload, xử lý lỗi và trạng thái loading. Bổ sung/điều chỉnh Playwright cho quyền truy cập và các thao tác SQL có hành vi thật, không chỉ grep mã nguồn.

## Kafka — dừng để hỏi lại

Không sửa `DataOutboundKafkaSender` hay đánh dấu Kafka hoàn tất trong prompt này. Cuối đợt, báo rõ sender hiện có thể trả `Success=true` dù chưa publish và hỏi người dùng có triển khai Kafka tiếp không; nếu có, cần xác nhận broker, xác thực, topic, payload, mức xác nhận gửi và cách kiểm thử. Chờ câu trả lời trước khi bắt đầu.

## Kiểm chứng cuối

- Chạy `dotnet test tests/BE/test.csproj` hoặc bộ lọc ShareData cần thiết **không dùng `--no-build`**, trên DB local. Chạy Playwright ShareData tại `tests/FE/` cho các ca đã sửa. Không tự động build frontend.
- Báo từng việc từ **1 đến 7** theo trạng thái `Đã làm` hoặc `Chưa làm`, kèm file, test đã chạy, kết quả pass/fail/skip và rủi ro còn lại. Không đưa kết quả test cũ thành kết quả hiện tại.
- Giữ mọi thay đổi unstaged để lập trình viên review; không thay đổi các phân hệ ngoài phạm vi.

## Việc cuối — Cập nhật lại tài liệu gốc

Cập nhật `Sharedata_MasterPlan.md`, `Prompt/README.md` và tài liệu ShareData liên quan theo đúng kết quả đã được kiểm chứng; giữ quyết định Type trong `ShareDataPartner`, phạm vi component lịch mới tích hợp ShareData, và tình trạng Kafka còn chờ hỏi lại. Giữ lại tệp prompt này để đối chiếu sau thực thi.
