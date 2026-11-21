# Requirements Document

## Introduction

Tính năng **VmsEventDefault** (Nội dung mặc định VMS theo sự kiện sự cố) là một module quản lý mới cho phép người dùng tạo, sửa, xóa nội dung hiển thị mặc định (text hoặc hình ảnh) gắn theo **sự kiện sự cố** (`EventType`) thay vì gắn theo từng thiết bị (`EquipmentId`) như tính năng DefaultVms hiện có.

Module tái sử dụng cấu trúc và quy ước của tính năng tham chiếu `VmsCurrent` / "Quản lý nội dung mặc định" (DefaultVms), nhưng có các khác biệt cốt lõi:

- Dữ liệu được khóa theo **sự kiện sự cố** (`EventId`) thay vì `EquipmentId`.
- Bảng dữ liệu mới `VmsEventDefault` chỉ chứa `RowData` + `Url` + cờ đánh dấu loại nội dung (TEXT/IMAGE), **không** có các trường `Default*` (không `DefaultRowData`, `DefaultUrl`,...), và có thêm cột `DefaultConfig` (JSON) lưu toàn bộ thông tin cấu hình sau khi lưu.
- Hỗ trợ đầy đủ **Thêm / Sửa / Xóa** (khác với DefaultVms chỉ có Sửa/Xóa).
- Người dùng có thể chèn các **token nội dung động** (`{VehiclePlate}`, `{VehicleSpeed}`, `{VehicleLenght}`) vào nội dung Dòng 1 / Dòng 2; các token này lấy từ bảng cấu hình tham số qua `getConfigDataByCode` (FE) / tra cứu cấu hình (BE).
- Trên form có ô đánh dấu (checkbox) "Sử dụng nội dung này cho sự kiện" nhằm chuẩn bị cho nút "Đặt làm mặc định" sẽ bổ sung sau (nằm ngoài phạm vi spec này).
- Việc sinh ảnh phải **tái sử dụng đúng luồng/code hiện có** (preview `fetchVmsPreviewBase64` ở FE + lưu file base64 qua `ImageService.SaveImageAsync` ở BE).

Đây là tính năng liên repo (cross-repo):

- **Backend (BE)**: `.NET 10 / Furion / SqlSugar / Wolverine CQRS` — repo `TA-ITS015-WEBAPI-V1.0`. Tạo trong một **thư mục feature mới** (không dùng lại thư mục `Current`).
- **Frontend (FE)**: `Vue 3 / Element Plus / VxeTable` — repo `TA-ITS015-WEBVUE-V1.0`.

Thứ tự triển khai: **Xây dựng BE trước, sau đó tới FE**. Người dùng sẽ tự triển khai code theo hướng dẫn; agent không chỉnh sửa code trực tiếp.

> **Lưu ý quan trọng về API client**: Các file TypeScript client trong `src/api-services/` được **sinh tự động từ Swagger** (`pnpm build-api`). Sau khi hoàn thành BE và build thành công, PHẢI regenerate các api-services trước khi triển khai FE. TUYỆT ĐỐI không tạo/sửa file trong `api-services/` bằng tay.

## Glossary

- **VmsEventDefault_System**: Toàn bộ hệ thống của tính năng nội dung mặc định VMS theo sự kiện sự cố (bao gồm cả thành phần BE và FE).
- **VmsEventDefault_BE**: Thành phần backend (module/feature CQRS) xử lý CRUD và lưu trữ dữ liệu.
- **VmsEventDefault_FE**: Thành phần frontend (trang danh sách + dialog thêm/sửa) trong repo Vue.
- **VmsEventDefault_Entity**: Bảng dữ liệu `VmsEventDefault` (SqlSugar entity), khóa theo sự kiện sự cố.
- **Incident_Event**: Sự kiện sự cố (`TmsEventType`) mà nội dung mặc định gắn vào; được xác định bởi `EventId`.
- **Content_Type_Flag**: Cờ đánh dấu loại nội dung của bản ghi, nhận một trong hai giá trị: `text` hoặc `image`.
- **RowData**: Chuỗi JSON chứa nội dung 2 dòng, dạng `{ "Row1": "...", "Row2": "..." }`.
- **Url**: Đường dẫn tương đối tới file ảnh đã lưu (kết quả sinh ảnh), hoặc chuỗi base64 khi gửi từ FE trước khi BE lưu file.
- **DefaultConfig**: Chuỗi JSON tổng hợp toàn bộ thông tin cấu hình của bản ghi sau khi lưu (nội dung text, đường dẫn ảnh, font, màu chữ, màu nền, cỡ chữ, loại nội dung, cờ sử dụng cho sự kiện,...).
- **Content_Token**: Token nội dung động dạng `{TokenName}` (ví dụ `{VehiclePlate}`, `{VehicleSpeed}`, `{VehicleLenght}`) có thể chèn vào Dòng 1 / Dòng 2, lấy từ bảng cấu hình tham số.
- **Use_For_Event_Flag**: Cờ đánh dấu "Sử dụng nội dung này cho sự kiện" trên form, dùng chuẩn bị cho nút "Đặt làm mặc định" trong tương lai.
- **Image_Service**: Dịch vụ sinh/lưu ảnh phía BE (`ImageService`, phương thức `SaveImageAsync`, `IsBase64String`).
- **Preview_Service**: Hàm FE `fetchVmsPreviewBase64` gọi API preview VMS trả về ảnh base64.
- **Config_Lookup_FE**: Hàm FE `getConfigDataByCode(BaseConfigTypeEnum...)` lấy danh sách cấu hình (màu chữ, màu nền, font, cỡ chữ, token nội dung động).
- **OperateType**: Loại thao tác trên form, nhận giá trị `add` hoặc `edit`.

## Requirements

### Requirement 1: Bảng dữ liệu VmsEventDefault (Entity)

**User Story:** Là lập trình viên backend, tôi muốn có một bảng dữ liệu mới khóa theo sự kiện sự cố, để lưu nội dung mặc định (text/ảnh) độc lập với thiết bị.

#### Acceptance Criteria

1. THE VmsEventDefault_BE SHALL định nghĩa một entity SqlSugar tên `VmsEventDefault` với `[SugarTable("VmsEventDefault")]` kế thừa `EntityTenant`.
2. THE VmsEventDefault_Entity SHALL chứa trường `EventId` dùng làm khóa liên kết tới Incident_Event.
3. THE VmsEventDefault_Entity SHALL chứa trường `RowData` kiểu chuỗi lưu chuỗi JSON hợp lệ biểu diễn đúng 2 dòng nội dung (`Row1`, `Row2`).
4. THE VmsEventDefault_Entity SHALL chứa trường `Url` kiểu chuỗi lưu đường dẫn ảnh đã sinh, với độ dài không vượt quá hằng số độ dài định nghĩa cho trường.
5. THE VmsEventDefault_Entity SHALL chứa trường `ContentType` biểu diễn Content_Type_Flag chỉ nhận đúng một trong hai giá trị hợp lệ `text` hoặc `image`.
6. WHERE `ContentType` nhận giá trị ngoài tập `{text, image}`, THE VmsEventDefault_BE SHALL coi đó là giá trị không hợp lệ (xử lý ở tầng validation).
7. THE VmsEventDefault_Entity SHALL chứa trường `DefaultConfig` kiểu chuỗi lưu chuỗi JSON hợp lệ chứa cấu hình tổng hợp.
8. THE VmsEventDefault_Entity SHALL chứa các trường thuộc tính hiển thị tương tự `VmsCurrent`: `Name`, `Size`, `Font`, `Color`, `BackColor`.
9. THE VmsEventDefault_Entity SHALL KHÔNG chứa bất kỳ trường tiền tố `Default` nào của dữ liệu hiển thị (không `DefaultRowData`, `DefaultUrl`, `DefaultColor`, `DefaultBackColor`, `DefaultFont`).
10. WHERE một trường có giới hạn độ dài, THE VmsEventDefault_Entity SHALL sử dụng hằng số độ dài từ `EntityConst` phù hợp với kiểu và mục đích dữ liệu của trường.

### Requirement 2: Cấu trúc module/feature BE theo quy ước CQRS

**User Story:** Là lập trình viên backend, tôi muốn feature mới tuân theo cấu trúc CQRS chuẩn của module VMS trong một thư mục riêng, để giữ tính nhất quán và không ảnh hưởng thư mục `Current`.

#### Acceptance Criteria

1. THE VmsEventDefault_BE SHALL đặt entity trong lớp `Core/Entities` của module VMS với tên lớp theo quy ước `Vms{Feature}`.
2. THE VmsEventDefault_BE SHALL đặt Controller, `Commands`, `Queries`, `Dto`, `Validators` trong một thư mục feature mới có tên theo tên chức năng, nằm hoàn toàn bên ngoài thư mục `Current`.
3. THE VmsEventDefault_BE SHALL không tạo mới, sửa đổi hoặc xóa bất kỳ tệp nào bên trong thư mục `Current`.
4. THE VmsEventDefault_BE SHALL định tuyến tất cả endpoint của feature theo tiền tố `api/vms` và gán vào nhóm tài liệu `VMS`.
5. WHEN Controller nhận một yêu cầu hợp lệ, THE VmsEventDefault_BE SHALL điều phối qua `MessBus.InvokeAsync` tới đúng một handler tương ứng với kiểu dữ liệu đầu vào.
6. IF `MessBus.InvokeAsync` không tìm thấy handler tương ứng với yêu cầu, THEN THE VmsEventDefault_BE SHALL trả về phản hồi lỗi chỉ ra không xử lý được yêu cầu và không thay đổi dữ liệu.
7. THE VmsEventDefault_BE SHALL cung cấp đầy đủ 5 endpoint cho feature: thêm mới, cập nhật, xóa, phân trang, và danh sách theo điều kiện tìm kiếm.
8. IF một endpoint được gọi với dữ liệu đầu vào không hợp lệ theo Validators, THEN THE VmsEventDefault_BE SHALL từ chối yêu cầu, trả về phản hồi lỗi chỉ ra trường không hợp lệ, và giữ nguyên dữ liệu hiện có.
9. THE VmsEventDefault_BE SHALL đặt tên DTO theo quy ước module: `Add{Feature}Input`, `Update{Feature}Input`, `Delete{Feature}Input`, `Page{Feature}Input`, `Page{Feature}Output`.

### Requirement 3: Thêm mới nội dung mặc định theo sự kiện

**User Story:** Là người vận hành, tôi muốn thêm mới một bản ghi nội dung mặc định cho một sự kiện sự cố, để chuẩn bị sẵn nội dung hiển thị cho sự kiện đó.

#### Acceptance Criteria

1. WHEN VmsEventDefault_BE nhận yêu cầu thêm mới có `EventId` không rỗng và các trường chuỗi nằm trong giới hạn độ dài cho phép (tối đa 500 ký tự cho `Url`), THE VmsEventDefault_BE SHALL tạo một bản ghi mới trong VmsEventDefault_Entity với các trường được cung cấp và trả về định danh bản ghi vừa tạo trong vòng 3 giây.
2. IF trường `EventId` rỗng hoặc không được cung cấp trong yêu cầu thêm mới, THEN THE VmsEventDefault_BE SHALL từ chối yêu cầu, không tạo bản ghi nào, và trả về thông báo lỗi cho biết `EventId` là bắt buộc.
3. IF một trường chuỗi trong yêu cầu vượt quá giới hạn độ dài cho phép (`Url` vượt quá 500 ký tự), THEN THE VmsEventDefault_BE SHALL từ chối yêu cầu, không tạo bản ghi nào, và trả về thông báo lỗi cho biết trường nào vượt giới hạn.
4. WHEN yêu cầu thêm mới chứa `Url` là chuỗi base64 hợp lệ, THE Image_Service SHALL lưu chuỗi base64 thành file ảnh và bản ghi SHALL lưu đường dẫn tương đối của file thay cho chuỗi base64.
5. IF việc lưu chuỗi base64 thành file ảnh thất bại, THEN THE VmsEventDefault_BE SHALL không tạo bản ghi nào và trả về thông báo lỗi cho biết không lưu được file ảnh.
6. WHEN yêu cầu thêm mới chứa `Url` không phải chuỗi base64, THE VmsEventDefault_BE SHALL lưu giá trị `Url` nguyên trạng.
7. WHEN một bản ghi được lưu thành công, THE VmsEventDefault_BE SHALL sinh chuỗi JSON DefaultConfig tổng hợp và lưu vào trường `DefaultConfig` của bản ghi.

### Requirement 4: Cập nhật nội dung mặc định theo sự kiện

**User Story:** Là người vận hành, tôi muốn sửa nội dung mặc định đã tạo, để điều chỉnh nội dung hoặc hình thức hiển thị cho sự kiện.

#### Acceptance Criteria

1. WHEN VmsEventDefault_BE nhận yêu cầu cập nhật có ID tồn tại, các trường bắt buộc không rỗng và các trường chuỗi nằm trong giới hạn độ dài cho phép, THE VmsEventDefault_BE SHALL cập nhật bản ghi tương ứng với các trường được cung cấp và trả về phản hồi thành công trong vòng 3 giây.
2. IF yêu cầu cập nhật vi phạm quy tắc hợp lệ (trường bắt buộc rỗng hoặc vượt giới hạn độ dài), THEN THE VmsEventDefault_BE SHALL từ chối yêu cầu, trả về thông báo lỗi chỉ ra trường vi phạm, và giữ nguyên dữ liệu hiện có.
3. IF ID trong yêu cầu cập nhật không tồn tại, THEN THE VmsEventDefault_BE SHALL trả về lỗi dữ liệu không tồn tại và không thay đổi bất kỳ dữ liệu nào.
4. WHEN yêu cầu cập nhật chứa `Url` là chuỗi base64 hợp lệ, THE Image_Service SHALL lưu chuỗi base64 thành file ảnh và bản ghi SHALL lưu đường dẫn tương đối của file.
5. IF `Url` trong yêu cầu cập nhật là chuỗi base64 không hợp lệ (không giải mã được), THEN THE VmsEventDefault_BE SHALL từ chối yêu cầu, giữ nguyên đường dẫn ảnh hiện có và trả về thông báo lỗi cho biết dữ liệu ảnh không hợp lệ.
6. WHEN một bản ghi được cập nhật thành công, THE VmsEventDefault_BE SHALL sinh lại chuỗi JSON DefaultConfig tổng hợp và lưu vào trường `DefaultConfig`.

### Requirement 5: Xóa nội dung mặc định theo sự kiện

**User Story:** Là người vận hành, tôi muốn xóa một bản ghi nội dung mặc định, để loại bỏ nội dung không còn cần thiết.

#### Acceptance Criteria

1. WHEN VmsEventDefault_BE nhận yêu cầu xóa với ID không rỗng và tồn tại trong VmsEventDefault_Entity, THE VmsEventDefault_BE SHALL xóa bản ghi tương ứng khỏi VmsEventDefault_Entity và trả về phản hồi xác nhận xóa thành công.
2. IF ID trong yêu cầu xóa rỗng hoặc không được cung cấp, THEN THE VmsEventDefault_BE SHALL từ chối yêu cầu, trả về lỗi xác thực cho biết ID là bắt buộc, và giữ nguyên toàn bộ bản ghi trong VmsEventDefault_Entity.
3. IF ID trong yêu cầu xóa không tồn tại trong VmsEventDefault_Entity, THEN THE VmsEventDefault_BE SHALL từ chối yêu cầu, trả về lỗi cho biết dữ liệu không tồn tại, và không thay đổi bất kỳ bản ghi nào trong VmsEventDefault_Entity.
4. WHEN VmsEventDefault_BE nhận một yêu cầu xóa, THE VmsEventDefault_BE SHALL hoàn tất xử lý và trả về phản hồi (thành công hoặc lỗi) trong vòng 3 giây.

### Requirement 6: Truy vấn danh sách nội dung mặc định

**User Story:** Là người vận hành, tôi muốn xem danh sách nội dung mặc định theo sự kiện với phân trang và tìm kiếm, để tra cứu và quản lý dữ liệu.

#### Acceptance Criteria

1. WHEN VmsEventDefault_BE nhận yêu cầu phân trang với `Page` ≥ 1 và `PageSize` trong khoảng 1 đến 100, THE VmsEventDefault_BE SHALL trả về danh sách bản ghi tương ứng của trang được yêu cầu kèm tổng số bản ghi khớp điều kiện.
2. IF `Page` < 1 hoặc `PageSize` nằm ngoài khoảng 1 đến 100, THEN THE VmsEventDefault_BE SHALL áp dụng giá trị mặc định `Page` = 1 và `PageSize` = 50 cho tham số không hợp lệ và trả về danh sách theo giá trị mặc định đó.
3. WHERE yêu cầu phân trang có điều kiện lọc theo `Code` hoặc `Name`, THE VmsEventDefault_BE SHALL chỉ trả về các bản ghi có `Code` hoặc `Name` chứa chuỗi tìm kiếm (không phân biệt chữ hoa/thường).
4. WHERE yêu cầu phân trang có điều kiện lọc theo `EventId`, THE VmsEventDefault_BE SHALL chỉ trả về các bản ghi có `EventId` trùng khớp chính xác giá trị lọc.
5. WHEN không có bản ghi nào khớp điều kiện lọc, THE VmsEventDefault_BE SHALL trả về danh sách rỗng với tổng số bản ghi bằng 0.
6. WHEN VmsEventDefault_BE trả về mỗi bản ghi trong danh sách, THE VmsEventDefault_BE SHALL bao gồm đầy đủ các cột `RowData`, `Url`, `ContentType` và `DefaultConfig` để FE hiển thị và tạo preview.
7. WHEN VmsEventDefault_BE nhận yêu cầu danh sách không phân trang theo điều kiện tìm kiếm, THE VmsEventDefault_BE SHALL trả về toàn bộ bản ghi khớp điều kiện, áp dụng cùng quy tắc lọc `Code`, `Name`, `EventId` như yêu cầu phân trang.

### Requirement 7: Kiểm tra hợp lệ dữ liệu đầu vào (Validation)

**User Story:** Là lập trình viên backend, tôi muốn dữ liệu đầu vào được kiểm tra hợp lệ, để bảo đảm toàn vẹn dữ liệu trước khi lưu.

#### Acceptance Criteria

1. WHEN nhận yêu cầu thêm mới hoặc cập nhật, THE VmsEventDefault_BE SHALL kiểm tra `EventId` không rỗng và không chỉ gồm ký tự khoảng trắng.
2. WHERE `RowData` được cung cấp, THE VmsEventDefault_BE SHALL kiểm tra độ dài chuỗi không vượt quá giá trị giới hạn hằng số đã định nghĩa cho trường `RowData`.
3. WHERE `Url` được cung cấp và không bắt đầu bằng tiền tố `data:` (không phải chuỗi base64), THE VmsEventDefault_BE SHALL kiểm tra độ dài chuỗi không vượt quá giá trị giới hạn hằng số đã định nghĩa cho trường `Url`.
4. THE VmsEventDefault_BE SHALL chỉ chấp nhận `ContentType` bằng đúng một trong hai giá trị `text` hoặc `image` (phân biệt chữ hoa/thường), và từ chối mọi giá trị khác, kể cả giá trị rỗng.
5. IF bất kỳ quy tắc hợp lệ nào bị vi phạm, THEN THE VmsEventDefault_BE SHALL từ chối toàn bộ yêu cầu, không ghi hoặc thay đổi dữ liệu trong cơ sở dữ liệu, và trả về thông báo lỗi bản địa hóa xác định rõ trường vi phạm.

### Requirement 8: Trang danh sách FE (index.vue)

**User Story:** Là người vận hành, tôi muốn một trang danh sách hiển thị nội dung mặc định theo sự kiện với các thao tác CRUD, để quản lý dữ liệu trực quan.

#### Acceptance Criteria

1. THE VmsEventDefault_FE SHALL hiển thị trang danh sách sử dụng `vxe-grid` với các cột gồm mã, tên, nội dung, ảnh preview, loại nội dung và cột thao tác, với kích thước trang mặc định 50 bản ghi và sắp xếp mặc định theo thời gian tạo giảm dần.
2. WHEN người dùng nhập giá trị vào thanh tìm kiếm và kích hoạt tìm kiếm, THE VmsEventDefault_FE SHALL lọc danh sách theo mã và tên bằng so khớp chứa (contains), không phân biệt hoa thường, và hiển thị kết quả bắt đầu từ trang đầu tiên.
3. WHEN người dùng bấm nút Thêm mới, THE VmsEventDefault_FE SHALL mở dialog thêm/sửa ở chế độ `add`.
4. WHEN người dùng bấm nút Sửa trên một dòng, THE VmsEventDefault_FE SHALL mở dialog thêm/sửa ở chế độ `edit` với dữ liệu của dòng đó.
5. WHEN người dùng xác nhận xóa một dòng trong hộp thoại xác nhận, THE VmsEventDefault_FE SHALL gọi API xóa được sinh từ Swagger, và sau khi nhận phản hồi thành công SHALL làm mới danh sách và hiển thị thông báo xóa thành công.
6. THE VmsEventDefault_FE SHALL gọi các API client được sinh tự động từ Swagger (không tạo/sửa file `api-services/` bằng tay).
7. THE VmsEventDefault_FE SHALL áp dụng phân quyền cấp nút (`v-auth`) theo quy ước `{featureId}:{action}` cho các thao tác thêm, sửa, xóa, phân trang.
8. WHEN người dùng bấm nút Xóa trên một dòng, THE VmsEventDefault_FE SHALL hiển thị hộp thoại xác nhận kèm tên bản ghi trước khi gọi API xóa.
9. IF gọi API xóa thất bại, THEN THE VmsEventDefault_FE SHALL giữ nguyên dữ liệu danh sách hiện tại và hiển thị thông báo lỗi cho biết thao tác xóa không thành công.
10. IF không có bản ghi nào khớp điều kiện tìm kiếm, THEN THE VmsEventDefault_FE SHALL hiển thị trạng thái danh sách rỗng.

### Requirement 9: Dialog thêm/sửa FE với token động và đánh dấu sử dụng

**User Story:** Là người vận hành, tôi muốn nhập nội dung 2 dòng, chèn token động, chọn định dạng và xem preview trước khi lưu, để tạo nội dung chính xác cho sự kiện.

#### Acceptance Criteria

1. THE VmsEventDefault_FE SHALL cung cấp dialog nhập liệu gồm hai trường nội dung Dòng 1 và Dòng 2, mỗi trường giới hạn tối đa 255 ký tự.
2. WHEN người dùng chọn một Content_Token (`{VehiclePlate}`, `{VehicleSpeed}`, `{VehicleLenght}`) để chèn, THE VmsEventDefault_FE SHALL chèn chuỗi token vào vị trí con trỏ hiện tại của trường Dòng 1 hoặc Dòng 2 đang được chọn.
3. WHEN dialog được mở, THE VmsEventDefault_FE SHALL lấy danh sách Content_Token từ bảng cấu hình tham số qua Config_Lookup_FE.
4. WHEN dialog được mở, THE VmsEventDefault_FE SHALL lấy danh sách màu chữ, màu nền, font, cỡ chữ từ Config_Lookup_FE tương ứng `VmsFontColor`, `VmsBackgroundColor`, `VmsFontName`, `VmsFontSize`.
5. IF Config_Lookup_FE không trả về được danh sách Content_Token hoặc danh sách định dạng, THEN THE VmsEventDefault_FE SHALL hiển thị thông báo lỗi cho biết không tải được dữ liệu cấu hình và giữ dialog ở trạng thái chưa cho phép lưu.
6. WHEN người dùng thay đổi nội dung hoặc định dạng và không có thay đổi tiếp theo trong khoảng debounce 500 ms, THE VmsEventDefault_FE SHALL gọi Preview_Service để cập nhật ảnh preview.
7. IF Preview_Service không trả về ảnh preview trong vòng 10 giây hoặc trả về lỗi, THEN THE VmsEventDefault_FE SHALL hiển thị thông báo lỗi cho biết không tạo được ảnh preview và giữ nguyên nội dung người dùng đã nhập.
8. THE VmsEventDefault_FE SHALL hiển thị một ô đánh dấu (checkbox) Use_For_Event_Flag "Sử dụng nội dung này cho sự kiện".
9. WHEN người dùng bấm xác nhận lưu và có ít nhất một trong hai trường Dòng 1 hoặc Dòng 2 không rỗng, THE VmsEventDefault_FE SHALL gửi yêu cầu gồm nội dung Dòng 1/Dòng 2 (dạng JSON `RowData`), ảnh preview base64 (`Url`), loại nội dung (`ContentType`), định dạng (font/màu/cỡ) và trạng thái Use_For_Event_Flag.
10. IF cả Dòng 1 và Dòng 2 đều rỗng khi người dùng bấm lưu, THEN THE VmsEventDefault_FE SHALL hiển thị cảnh báo cho biết nội dung không được để trống và không gửi yêu cầu.
11. IF yêu cầu lưu thất bại, THEN THE VmsEventDefault_FE SHALL hiển thị thông báo lỗi cho biết lưu không thành công và giữ nguyên dữ liệu người dùng đã nhập trên dialog.

### Requirement 10: Tái sử dụng luồng sinh ảnh hiện có

**User Story:** Là lập trình viên, tôi muốn luồng sinh ảnh dùng lại đúng code hiện có, để tránh trùng lặp và giữ hành vi nhất quán với tính năng VMS.

#### Acceptance Criteria

1. WHEN cần tạo ảnh preview cho sự kiện, THE VmsEventDefault_FE SHALL tạo ảnh preview bằng cách gọi hàm `fetchVmsPreviewBase64` của Preview_Service hiện có và SHALL KHÔNG khai báo hay khởi tạo bất kỳ hàm/luồng tạo preview mới nào ngoài `fetchVmsPreviewBase64`.
2. WHEN Preview_Service trả về chuỗi base64 preview, THE VmsEventDefault_FE SHALL sử dụng trực tiếp chuỗi base64 nhận được mà không biến đổi lại nội dung ảnh.
3. IF lời gọi `fetchVmsPreviewBase64` thất bại hoặc không trả về chuỗi base64 hợp lệ, THEN THE VmsEventDefault_FE SHALL dừng thao tác tạo preview, giữ nguyên trạng thái dữ liệu trước đó (không lưu ảnh rỗng/hỏng) và hiển thị thông báo lỗi cho biết việc tạo preview không thành công.
4. WHEN cần lưu ảnh từ chuỗi base64, THE VmsEventDefault_BE SHALL lưu ảnh bằng cách gọi hàm `ImageService.SaveImageAsync` của Image_Service hiện có và SHALL KHÔNG khai báo hay khởi tạo bất kỳ hàm/luồng lưu ảnh mới nào ngoài `ImageService.SaveImageAsync`.
5. IF chuỗi base64 đầu vào rỗng, null hoặc không đúng định dạng base64, THEN THE VmsEventDefault_BE SHALL không gọi `ImageService.SaveImageAsync`, không tạo tệp ảnh nào và trả về lỗi cho biết dữ liệu ảnh không hợp lệ.

### Requirement 11: Tổng hợp DefaultConfig JSON sau khi lưu

**User Story:** Là lập trình viên, tôi muốn toàn bộ thông tin của bản ghi vừa tạo được gom vào một JSON, để dùng lại cho các xử lý sau (ví dụ nút "Đặt làm mặc định").

#### Acceptance Criteria

1. WHEN một bản ghi được lưu (thêm mới hoặc cập nhật) thành công, THE VmsEventDefault_BE SHALL tạo chuỗi JSON DefaultConfig chứa đầy đủ 8 thuộc tính: nội dung text, đường dẫn ảnh, font, màu chữ, màu nền, cỡ chữ, loại nội dung và trạng thái Use_For_Event_Flag, với giá trị của từng thuộc tính đúng bằng giá trị vừa được lưu của bản ghi đó.
2. WHEN chuỗi JSON DefaultConfig đã được tạo, THE VmsEventDefault_BE SHALL lưu chuỗi JSON đó vào trường `DefaultConfig` của chính bản ghi vừa lưu trong cùng một giao dịch với thao tác lưu bản ghi.
3. WHERE bản ghi lưu ảnh (`ContentType` = `image`), THE VmsEventDefault_BE SHALL ghi vào trường đường dẫn ảnh trong DefaultConfig là đường dẫn tương đối của ảnh đã lưu (không chứa chuỗi base64).
4. IF một thuộc tính nội dung không có giá trị (null hoặc rỗng), THEN THE VmsEventDefault_BE SHALL biểu diễn thuộc tính đó bằng giá trị rỗng trong JSON để chuỗi DefaultConfig luôn là JSON hợp lệ, đầy đủ 8 thuộc tính.
5. IF việc tạo chuỗi JSON hoặc lưu trường `DefaultConfig` thất bại, THEN THE VmsEventDefault_BE SHALL hủy bỏ (rollback) toàn bộ thao tác lưu bản ghi và trả về thông báo lỗi cho biết việc tổng hợp DefaultConfig không thành công, đồng thời không để lại bản ghi có trường `DefaultConfig` trống hoặc sai lệch.

### Requirement 12: Đăng ký và bản địa hóa

**User Story:** Là lập trình viên, tôi muốn feature được đăng ký đúng và có nhãn đa ngôn ngữ, để hoạt động trong hệ thống và hiển thị tiếng Việt/tiếng Anh.

#### Acceptance Criteria

1. IF một khóa bản địa hóa cần dùng đã tồn tại trong `BaseLocaleManager`, THEN THE VmsEventDefault_BE SHALL sử dụng lại khóa dùng chung đó và SHALL KHÔNG định nghĩa trùng khóa trong `BaseMsg` của module.
2. WHERE feature cần khóa bản địa hóa đặc thù chưa có trong `BaseLocaleManager`, THE VmsEventDefault_BE SHALL bổ sung khóa vào `BaseMsg` của module theo đúng nhóm danh mục thuộc tập `{entity, label, validation, message, exception}`.
3. THE VmsEventDefault_System SHALL cung cấp bản dịch đầy đủ cho mọi khóa bản địa hóa mới của feature ở cả hai ngôn ngữ `vi-VN` và `en-US`.
4. THE VmsEventDefault_FE SHALL sử dụng khóa i18n theo quy ước `lz.{category}.{scope}.{field}`, trong đó `scope` thuộc tập `{base, vms, vmsEventDefault}` tùy phạm vi sử dụng, cho nhãn, thông báo và nút.
5. WHEN người dùng chuyển đổi ngôn ngữ hiển thị, THE VmsEventDefault_FE SHALL hiển thị nhãn, thông báo và nút theo ngôn ngữ đang chọn (`vi-VN` hoặc `en-US`).
6. IF một khóa bản địa hóa của feature thiếu bản dịch cho ngôn ngữ đang chọn, THEN THE VmsEventDefault_FE SHALL hiển thị bản dịch tiếng Việt mặc định thay cho khóa thô.
7. WHEN BE thay đổi hoàn tất và build thành công, THE VmsEventDefault_System SHALL yêu cầu regenerate api-services từ Swagger trước khi triển khai FE.
8. IF api-services chưa được regenerate sau khi BE thay đổi, THEN THE VmsEventDefault_System SHALL coi việc triển khai FE là chưa đủ điều kiện và không tiến hành cho tới khi api-services được cập nhật.
