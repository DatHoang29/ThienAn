# Phân hệ Chia sẻ dữ liệu (ShareData) — Mô tả chức năng theo màn hình

**Đối tượng đọc:** tester, người nghiệm thu.
**Mục đích:** hiểu mỗi màn hình dùng để làm gì, có những gì trên màn, bấm được gì, và kết quả sẽ hiện ra ở đâu.
**Phạm vi:** 5 màn — Chia sẻ dữ liệu, Ánh xạ dữ liệu, Bộ mã, Nguồn dữ liệu, Nhật ký & Cảnh báo.
**Nguồn dữ liệu của tài liệu:** đọc trực tiếp mã nguồn giao diện, mã nguồn backend và CSDL `DEV_ITS10` (ngày rà: 21/09/2026).

---

## 1. Phân hệ này làm gì

Hệ thống ITS của mình trao đổi dữ liệu với **đối tác** bên ngoài theo hai chiều:

- **Chiều gửi (Outbound)**: hệ thống tự lấy dữ liệu trong CSDL nghiệp vụ, đóng thành **gói tin** theo đúng khuôn JSON đối tác yêu cầu, rồi gửi đi theo lịch.
- **Chiều nhận (Inbound)**: đối tác đẩy gói tin vào hệ thống, hệ thống bóc gói và ghi vào CSDL.

Năm màn hình trong tài liệu này là nơi **khai báo cấu hình** cho hai luồng đó, và nơi **xem lại** những gì đã xảy ra.

### 1.1 Chuỗi phụ thuộc giữa các màn — đọc trước khi test

Các màn **không độc lập**. Khai thiếu một mắt nào thì mắt sau không chạy:

```
[1] Đối tác            (màn Chia sẻ dữ liệu)
      ↓
[2] Gói tin + Trường   (màn Nguồn dữ liệu)
      ↓
[3] Bộ mã quy đổi      (màn Bộ mã)          ← tùy chọn, chỉ khi cần đổi giá trị
      ↓
[4] Hồ sơ ánh xạ       (màn Ánh xạ dữ liệu)  ← phải BẬT "Đang dùng"
      ↓
[5] Đăng ký chia sẻ    (màn Chia sẻ dữ liệu) ← phải kết nối đối tác mới bật được
      ↓
[6] Nhật ký / Cảnh báo (màn Nhật ký, màn Cảnh báo lỗi)
```

Hai hệ quả tester cần nhớ:

- Tạo **đăng ký** mà chưa có **hồ sơ ánh xạ đang bật** cho đúng bộ ba (đối tác × gói tin × chiều) thì hệ thống **không gửi gì cả**, và ghi một cảnh báo mã `ESH-1304` ở màn Cảnh báo. Đây không phải lỗi hệ thống.
- Từ bản cập nhật mới nhất, ô chọn **gói tin** ở màn Ánh xạ chỉ liệt kê gói tin mà **đối tác đó đã đăng ký** và **đúng chiều đang chọn**. Nếu danh sách rỗng, phải vào màn Chia sẻ dữ liệu tạo đăng ký trước. Thứ tự nhập bắt buộc: **Đối tác → Chiều → Gói tin**.

### 1.2 Dữ liệu GỬI ĐI phụ thuộc vào đâu

| Thứ tự | Yếu tố | Khai ở đâu | Ghi chú cho tester |
|---|---|---|---|
| 1 | Câu truy vấn lấy dữ liệu của từng gói tin | **Cố định trong mã nguồn**, không có màn nào sửa được | Quyết định gói tin lấy từ bảng nào, cột nào — xem bảng tra ở mục 5.5 |
| 2 | Danh sách trường của gói tin | Màn **Nguồn dữ liệu** | Chỉ khai *tên trường*; **không** quyết định lấy dữ liệu từ bảng nào |
| 3 | Khuôn JSON gửi cho đối tác + luật quy đổi | Màn **Ánh xạ dữ liệu** (hồ sơ đang bật) | Không có hồ sơ bật → không gửi |
| 4 | Bảng quy đổi giá trị | Màn **Bộ mã** | Tùy chọn |
| 5 | Gửi cho ai, gói nào, bao lâu một lần | Màn **Chia sẻ dữ liệu** → Đăng ký | Đăng ký phải ở trạng thái Hoạt động |

> **Điểm dễ hiểu sai nhất:** khai thêm một trường ở màn Nguồn dữ liệu **không** làm hệ thống tự đi lấy dữ liệu cho trường đó. Trường chỉ có dữ liệu khi câu truy vấn trong mã nguồn có trả về đúng khóa field đó. Danh sách trường hiện đang khai nhưng không có nguồn nằm ở mục 5.6.

### 1.3 Dữ liệu NHẬN VỀ phụ thuộc vào đâu

| Thứ tự | Yếu tố | Khai ở đâu | Ghi chú cho tester |
|---|---|---|---|
| 1 | Gói tin do đối tác đẩy vào | Đối tác gửi | Hệ thống bị động, không có lịch |
| 2 | Vị trí từng trường trong JSON đối tác | Màn **Ánh xạ dữ liệu**, hồ sơ chiều Nhận về hoặc Hai chiều | Không có hồ sơ bật → không bóc được gói |
| 3 | Câu ghi vào CSDL của từng gói tin | Bảng `ShareDataPacketWrite` trong CSDL — **hiện không có màn UI nào quản lý** | Phải chạy SQL để khai, xem mục 5.7 |
| 4 | Bảng quy đổi giá trị ngược | Màn **Bộ mã** | Tùy chọn |

> **Rất quan trọng:** dữ liệu nhận về được ghi vào **CSDL riêng `DEV_ITS10_Inbound`**, **không** ghi vào CSDL nghiệp vụ `DEV_ITS10`. Tester tìm dữ liệu nhận về trong CSDL nghiệp vụ sẽ không thấy gì và dễ kết luận sai là "không nhận được gói".

### 1.4 Bấm trên giao diện xong thì xem kết quả ở đâu

| Thao tác trên UI | Kết quả thấy ngay | Kết quả phải chờ | Xem ở đâu |
|---|---|---|---|
| Thêm/sửa đối tác, gói tin, trường, bộ mã, hồ sơ ánh xạ | Có, lưới cập nhật ngay | — | Chính màn đó, và màn Nhật ký tab Cấu hình |
| Kết nối / ngắt kết nối đối tác | Trạng thái phiên đổi | — | Màn Chia sẻ dữ liệu, thẻ Phiên hoạt động |
| Bật đăng ký gửi | Trạng thái đăng ký thành Hoạt động | **Gói tin thật chỉ được gửi khi tới kỳ gửi theo lịch** | Màn Nhật ký tab Truyền nhận |
| Đăng ký nhận | Trạng thái đăng ký | **Phải chờ đối tác đẩy gói tin vào** | Màn Nhật ký tab Truyền nhận |
| Cấu hình sai / gửi lỗi | — | Xuất hiện sau kỳ gửi | Màn Cảnh báo lỗi, kèm mã `ESH-xxxx` |

---

## 2. Màn **Chia sẻ dữ liệu** (Sharing)

### 2.1 Mục đích
Màn trung tâm của phân hệ. Ba việc: quản lý **đối tác**, quản lý **kết nối** tới đối tác, và quản lý **đăng ký chia sẻ** (gói tin nào gửi/nhận với đối tác nào, bao lâu một lần).

### 2.2 Bố cục

**Hàng thẻ thống kê (trên cùng)**
| Thẻ | Ý nghĩa |
|---|---|
| Phiên hoạt động | Số phiên kết nối đang sống |
| Đăng ký đang chạy | Số đăng ký ở trạng thái Hoạt động |
| Cảnh báo | Số cảnh báo chưa xử lý |

**Cột trái — Danh sách ĐỐI TÁC**
- Mỗi dòng: tên đối tác + trạng thái kết nối.
- Nút **Thêm** (quyền `sharedataSharing:add`).
- Nút trên từng dòng: **Nhân bản**, **Sửa**, **Xoá**.
- Chưa có đối tác nào thì hiện "Chưa có đối tác".

**Cột phải — Chi tiết đối tác đang chọn**
| Thông tin | Ý nghĩa |
|---|---|
| Địa chỉ | IP/host, cổng và endpoint đã cấu hình |
| Lần kết nối gần nhất | Thời điểm thiết lập phiên gần nhất |
| Lần gửi gần nhất | Thời điểm gửi gói tin gần nhất |
| Lần nhận gần nhất | Thời điểm nhận gói tin gần nhất |
| Lần ngắt gần nhất | Thời điểm ngắt phiên gần nhất |
| Ngày tạo / Cập nhật gần nhất | Thông tin quản trị |

- Nút **Kết nối** — chỉ hiện khi phiên chưa hoạt động.
- Nút **Ngắt kết nối** — hỏi xác nhận "Ngắt kết nối với ...?". Khi ngắt, hệ thống **tự tắt các đăng ký gửi đang hoạt động** của đối tác đó và báo số lượng đã tắt.
- Chưa chọn đối tác thì hiện "Chọn một đối tác để xem chi tiết".

**Lưới ĐĂNG KÝ của đối tác đang chọn** (hai tab: **Gửi đi** / **Nhận về**)
| Cột | Ý nghĩa |
|---|---|
| Loại dữ liệu (gói tin) | Gói tin được đăng ký |
| Mapping áp dụng | Hồ sơ ánh xạ đang áp dụng cho bộ ba này. **Đây là giá trị suy ra tại thời điểm mở màn**, luôn khớp với hồ sơ mà hệ thống thực dùng. Trống nghĩa là chưa có hồ sơ nào bật → gửi/nhận sẽ thất bại |
| Chế độ | Một lần / Sự kiện / Định kỳ |
| Trạng thái | Chờ kích hoạt / Hoạt động / Tạm dừng / Từ chối / Đã hủy / Hết hạn |
| Bật/Tắt | Công tắc bật tắt đăng ký |
| Thông tin | Người tạo, thời điểm tạo/sửa |
| Thao tác | Sửa, Xoá, Hủy đăng ký |

- Nút **Tạo đăng ký gửi** / **Tạo đăng ký nhận** theo tab đang mở.
- Chưa có đăng ký thì hiện "Chưa có đăng ký".
- Công tắc bị khoá kèm chú thích "Cần kết nối với đối tác trước khi bật đăng ký".

### 2.3 Form **Đối tác** (Thêm / Sửa)

| Trường | Bắt buộc | Ý nghĩa và quy tắc |
|---|---|---|
| Mã đối tác | Có | Định danh đối tác, không trùng |
| Tên đối tác | Có | |
| Địa chỉ (IP/host) | Có | |
| Cổng | Có | |
| Endpoint | Không | Chỉ phần đường dẫn, phải bắt đầu bằng `/`, ví dụ `/sharedata/inbox` |
| Địa chỉ đầy đủ | — | Chỉ để xem trước, hệ thống tự ghép Địa chỉ + Cổng + Endpoint |
| API nhận về (URL) | Không | Phải bắt đầu `http://` hoặc `https://`, sai định dạng sẽ báo "URL không hợp lệ" |
| Hồ sơ mã hóa | Không | ASN / XML_A |
| Bên khởi tạo | Không | Ai chủ động mở kết nối |
| Tên đăng nhập | Không | Dùng cho phiên C2C |
| Mật khẩu | Không | |
| Heartbeat (giây) | Không | Nhịp giữ kết nối |
| Datagram size | Không | Kích thước gói đàm phán |
| Response timeout (giây) | Không | |
| Dùng TLS | Không | Công tắc |
| Ghi chú | Không | Tối đa 256 ký tự |

- Nút **Thử kết nối** chỉ dùng được khi đang ở chế độ Sửa.
- Xoá đối tác đang kết nối sẽ bị chặn: "Ngắt kết nối trước khi xoá".

### 2.4 Form **Đăng ký chia sẻ** (Thêm / Sửa)

Chiều được chọn bằng **tab**: *Gửi đi* hoặc *Nhận về*. Mỗi tab là một form riêng. Khi Sửa thì không đổi được chiều.

| Trường | Bắt buộc | Ghi chú |
|---|---|---|
| Đối tác | Có | Khi Sửa thì không đổi được |
| Loại dữ liệu (gói tin) | Có | Khi Sửa thì không đổi được |
| Ánh xạ áp dụng | — | Khối thông báo chỉ để xem: tên hồ sơ ánh xạ sẽ được dùng, kèm danh sách key sẽ gửi/nhận. Nếu chưa có hồ sơ bật, hiện cảnh báo vàng và nhắc vào màn Ánh xạ |
| Chế độ | Có | **Theo sự kiện** hoặc **Định kỳ** |
| Giãn cách tối thiểu (giây) | Không | Chỉ hiện khi chế độ Theo sự kiện |
| Nguồn sự kiện | Không | Chỉ hiện khi chế độ Theo sự kiện |
| Kiểu lịch | — | Liên tục / Hàng ngày |
| Chu kỳ (giây) | — | Dùng cho lịch Liên tục |
| Khung giờ | — | Giờ bắt đầu và kết thúc trong ngày |
| Ngày trong tuần | — | T2 … CN |
| Giờ gửi | — | Dùng cho lịch Hàng ngày |
| Theo sự kiện (công tắc) | — | Bật: gửi ngay khi có sự cố, không đợi kỳ kế tiếp. Tắt: chỉ gửi theo lịch |
| Định dạng | — | **Dữ liệu** hoặc **Tệp** |
| Ưu tiên (0-10) | — | Mặc định 5 |
| Ghi chú | Không | Tối đa 256 ký tự |

Toàn bộ nhóm lịch **chỉ có nghĩa ở chiều Gửi đi**. Ở tab Nhận về, màn hiện chú thích: *"Chiều nhận là bị động: đối tác đẩy gói tin vào hộp nhận, hệ thống xử lý ngay khi có — không cần cấu hình lịch gửi."*

### 2.5 Quy tắc nghiệp vụ cần kiểm

1. Không tạo được **hai đăng ký còn sống** cho cùng (đối tác × gói tin × chiều).
2. Đối tác đang **Vô hiệu hóa** thì không tạo/sửa được đăng ký của đối tác đó.
3. Đăng ký đang **Hoạt động** thì phải **Tạm dừng** trước khi Sửa hoặc Xoá.
4. Đăng ký ở trạng thái kết thúc (Từ chối / Đã hủy / Hết hạn) thì không sửa được nữa.
5. Bật lại đăng ký khi đối tác **chưa kết nối** sẽ bị từ chối.
6. Tạo đăng ký lúc đối tác chưa kết nối thì đăng ký vào trạng thái **Chờ kích hoạt**, không phải Hoạt động — đây là hành vi đúng.
7. Sửa lịch xong, kỳ gửi kế tiếp được tính lại ngay, không đợi mốc cũ.

### 2.6 Giới hạn đã biết
- Hộp thoại **Xem trước & Xuất file (bằng chứng)** chưa hoạt động: bấm sẽ hiện "Chức năng xuất dữ liệu chưa có API backend, vui lòng chờ bản cập nhật."

---

## 3. Màn **Ánh xạ dữ liệu** (Mapping)

### 3.1 Mục đích
Khai **hồ sơ ánh xạ** (còn gọi là phễu lọc): dịch giữa gói tin của hệ thống mình và **khuôn JSON mà đối tác yêu cầu**. Hồ sơ trả lời ba câu: đối tác muốn JSON hình dạng gì, mỗi khóa trong JSON đó lấy giá trị từ trường nào, và giá trị có cần quy đổi/ép kiểu/định dạng lại không.

### 3.2 Lưới danh sách

Bộ lọc: Mã hồ sơ, Tên hồ sơ, Đối tác, Gói tin, Chiều, Đang dùng.

| Cột | Ý nghĩa |
|---|---|
| Mã | Mã hồ sơ, hệ thống **tự sinh** theo `MÃ_ĐỐI_TÁC_MÃ_GÓI_TIN_CHIỀU` (ví dụ `DOITAC01_101_COMMONDATA_OUT`) |
| Tên | Tên hồ sơ, người dùng tự đặt |
| Đối tác | |
| Gói tin | |
| Chiều | Gửi đi / Nhận về / Hai chiều |
| Số trường | Số khóa trong khuôn đã ánh xạ được vào trường gói tin |
| Đang dùng | Công tắc bật/tắt hồ sơ |
| Thông tin | Người tạo, thời điểm |
| Thao tác | Nhân bản, Sửa, Xoá |

Quyền: `eshMapping:add`, `eshMapping:update`, `eshMapping:delete`.

### 3.3 Hộp thoại hồ sơ — Tab 1: **Thông tin chung**

| Trường | Bắt buộc | Quy tắc |
|---|---|---|
| Đối tác | **Có** | Khi Sửa thì khoá. Không còn khái niệm "hồ sơ dùng chung" — bắt buộc phải gắn đối tác |
| Chiều | **Có** | Gửi đi / Nhận về / Hai chiều. Chọn Hai chiều sẽ hiện chú thích: chỉ dùng khi cấu trúc gửi và nhận giống nhau |
| Gói tin | **Có** | **Chỉ liệt kê gói tin mà đối tác đã đăng ký đúng chiều đang chọn.** Chiều Hai chiều thì gói tin phải có đăng ký ở *cả hai* chiều. Chỉ tính đăng ký ở trạng thái **Hoạt động** hoặc **Tạm dừng**. Chưa chọn đối tác thì ô bị khoá |
| Mã | Có | Chỉ đọc, tự sinh |
| Tên | Có | |
| Đang dùng | — | Công tắc, kèm chú thích "Chỉ 1 hồ sơ được bật cho mỗi cặp đối tác × gói tin" |
| Ghi chú | Không | Tối đa 256 ký tự |

Hành vi cần kiểm:
- Đổi **đối tác** → gói tin đang chọn bị bỏ và toàn bộ cây ánh xạ được làm mới (vì mã đối tác được nhúng vào khuôn).
- Đổi **chiều** → nếu gói tin đang chọn không còn đăng ký ở chiều mới, hệ thống bỏ chọn và báo "Đổi chiều nên gói tin đang chọn không còn đăng ký".
- Đối tác chưa có đăng ký nào ở chiều đó → ô gói tin rỗng kèm dòng nhắc vào màn Chia sẻ dữ liệu đăng ký trước.

### 3.4 Hộp thoại hồ sơ — Tab 2: **Ánh xạ**

Chia hai bên.

**Bên trái**
- Ô **JSON đối tác**: dán nguyên văn JSON mẫu đối tác cung cấp, rồi bấm **Phân tích**. Hệ thống đọc mọi hình dạng: phẳng, lồng nhiều tầng, có mảng.
- Ô **Bộ khung đầu ra** (chỉ đọc): tài liệu sẽ được lưu và hệ thống dùng để sinh gói tin. Nút **Dựng lại** để tạo lại.

**Bên phải — cây ánh xạ**, vẽ lại đúng cấu trúc JSON đối tác, ba cột:
- *Đối tác*: tên khóa và giá trị mẫu.
- *Hệ thống*: ô chọn trường gói tin, gồm hai nhóm:
  - **Giá trị hệ thống**: `Now` (thời điểm gửi), `Serial` (số thứ tự gói), `PacketCode` (mã gói tin), `PartnerCode` (mã đối tác) — hệ thống tự điền, không khai luật.
  - **Trường của gói tin**: lấy từ màn Nguồn dữ liệu.
- *Luật*: dãy icon cho biết khóa đó đang khai luật gì. Bấm vào để mở form thiết lập.

Nút **Tự động ánh xạ**: khớp khóa đối tác với trường gói tin theo tên (không phân biệt hoa thường, dấu gạch).

Nhãn trên nút cây:
| Nhãn | Ý nghĩa |
|---|---|
| nhân theo bản ghi | Mảng này sẽ được nhân ra theo từng bản ghi dữ liệu |
| danh sách cố định | Mảng hằng, gửi nguyên như JSON mẫu |
| nhóm | Nút lồng, không gắn giá trị |

Ý nghĩa icon cột Luật: bộ mã, kiểu dữ liệu đích, định dạng ngày, định dạng số, giá trị mặc định, giá trị hằng, giá trị hệ thống, và **icon cảnh báo** (khóa bắt buộc đang rỗng, hoặc khóa không chọn trường và cũng không gõ giá trị → sẽ gửi `null`).

**Form thiết lập của một khóa**
| Trường | Ý nghĩa |
|---|---|
| Giá trị gửi đi | Chỉ hiện khi khóa không chọn trường nào. Gõ chữ là chuỗi; muốn gửi số hay true/false thì gõ đúng dạng JSON |
| Bộ mã | Bảng quy đổi giá trị, chọn từ màn Bộ mã. Có nút thêm bộ mã mới ngay tại đây |
| Kiểu dữ liệu phía đối tác | string, number, datetime… |
| Định dạng ngày | Chỉ hiện khi kiểu là ngày |
| Định dạng số | Chỉ hiện khi kiểu là số |
| Giá trị đối tác mặc định | Gửi khi dữ liệu nội bộ rỗng — chỉ dùng ở chiều Gửi |
| Giá trị nội bộ mặc định | Lưu khi đối tác không gửi — chỉ dùng ở chiều Nhận |

Đã chọn bộ mã thì hai ô mặc định bị khoá, vì bản thân bộ mã đã có dòng mặc định riêng.

Cuối tab có khối **"Gói tin còn N trường đối tác không yêu cầu"** để đối chiếu trường nào chưa được dùng.

### 3.5 Hộp thoại hồ sơ — Tab 3: **Dữ liệu gửi thử**

- Bảng nhập **giá trị mẫu**: mỗi dòng là một bản ghi, mỗi cột là một trường gói tin đang được khuôn dùng. Tiêu đề cột có dấu `*` nếu trường bắt buộc và nhãn `mã` nếu trường có bộ mã.
- Nút **Thêm bản ghi**, **Xoá hết**.
- Nút **Dựng payload gửi đi** → sinh JSON vào khối *Payload đối tác sẽ nhận*.
- Khối cảnh báo liệt kê: giá trị không khớp bộ mã, trường bắt buộc còn rỗng.
- Dòng nhắc cho biết payload sẽ ra **một gói tin** (khuôn có mảng nhân theo bản ghi) hay **danh sách nhiều gói tin**.

**Giới hạn của tab này — cần nêu rõ với tester:**
1. Giá trị mẫu **nhập tay**, không lấy dữ liệu thật từ CSDL.
2. Chạy hoàn toàn trên trình duyệt. Nó **không áp** định dạng ngày, định dạng số và biểu thức. Vì vậy payload xem thử sẽ **khác payload thật** ở các trường ngày và số. Đây là giới hạn đã biết, không phải lỗi.

### 3.6 Quy tắc nghiệp vụ cần kiểm

1. **Mỗi (đối tác × gói tin × chiều) chỉ được một hồ sơ bật.** Bật hồ sơ thứ hai sẽ bị từ chối.
2. Hồ sơ có chiều Gửi đi hoặc Hai chiều **bắt buộc khai đủ 4 giá trị hệ thống** `Now`, `Serial`, `PacketCode`, `PartnerCode`. Thiếu thì không lưu được.
3. Phải có ít nhất một khóa lấy dữ liệu thật từ gói tin; hồ sơ chỉ gồm 4 giá trị hệ thống thì không lưu được.
4. Bộ mã khai trong hồ sơ phải tồn tại.
5. Hồ sơ **đang bật** mà có đăng ký **đang Hoạt động** dùng tới thì **không sửa / không xoá / không tắt được**. Muốn sửa thì tạm dừng đăng ký trước.
6. Khi Sửa, bộ ba đối tác × gói tin × chiều bị khoá — chỉ sửa được phần khuôn và luật.
7. Nhân bản hồ sơ tạo bản ghi mới, không ghi đè bản gốc.

---

## 4. Màn **Bộ mã** (CodeSet)

### 4.1 Mục đích
Khai **bảng quy đổi giá trị** giữa hệ thống mình và đối tác, dùng được cho **cả hai chiều**. Ví dụ: nội bộ lưu trạng thái là `1`, đối tác muốn nhận là `on` → khai `1 ⇄ on`. Chiều gửi đổi `1` thành `on`, chiều nhận đổi `on` về `1`.

### 4.2 Lưới danh sách

Bộ lọc: Từ khóa (mã / tên bộ mã), Phạm vi.

| Cột | Ý nghĩa |
|---|---|
| Mã | Mã bộ mã, chính là giá trị được chọn trong hồ sơ ánh xạ |
| Tên | |
| Số giá trị | Số dòng quy đổi đã khai |
| Thông tin | Người tạo, thời điểm |
| Thao tác | Nhân bản, Sửa, Xoá |

Quyền: `eshCanonical:add`, `eshCanonical:update`, `eshCanonical:delete`.

### 4.3 Hộp thoại bộ mã

| Phần | Nội dung |
|---|---|
| Thông tin chung | Mã, Tên, Phạm vi, Ghi chú |
| Giá trị quy đổi | Bảng nhiều dòng, nút **Thêm giá trị**, hiện "N giá trị" |

Mỗi dòng quy đổi:
| Ô | Ý nghĩa |
|---|---|
| Giá trị nội bộ | Giá trị hệ thống mình đang lưu, ví dụ `1` |
| Giá trị đối tác | Giá trị đối tác dùng, ví dụ `passengerCar` |
| Tên hiển thị | Nhãn cho người đọc, ví dụ `Bật` |
| Mặc định | Đánh dấu dòng mặc định |

**Dòng Giá trị mặc định** dùng khi giá trị không khớp dòng nào: chiều gửi lấy ô *Giá trị đối tác*, chiều nhận lấy ô *Giá trị nội bộ*. Để trống thì hệ thống **giữ nguyên giá trị gốc**.

**Khối Thử quy đổi**: nhập một giá trị rồi xem kết quả, trả về một trong ba trạng thái — *Khớp bảng quy đổi*, *Không khớp, dùng giá trị mặc định*, *Không khớp, chưa có mặc định nên giữ nguyên*.

### 4.4 Quy tắc nghiệp vụ cần kiểm
1. Phải khai ít nhất một dòng quy đổi.
2. Trùng **Giá trị nội bộ** → cảnh báo, vì chiều gửi không biết chọn dòng nào.
3. Trùng **Giá trị đối tác** → cảnh báo, vì chiều nhận không biết quy về giá trị nội bộ nào.
4. Sửa một bộ mã ảnh hưởng **mọi hồ sơ ánh xạ** đang dùng bộ mã đó, ở cả hai chiều.

---

## 5. Màn **Nguồn dữ liệu** (DataSource) — gói tin và trường gói tin

### 5.1 Mục đích
Khai **danh mục gói tin** (101–111) và **danh sách trường** của từng gói tin. Danh sách trường ở đây chính là nguồn của ô "Trường của gói tin" trong màn Ánh xạ.

### 5.2 Bố cục
Hai lưới cạnh nhau: **Gói tin** bên trái, **Trường gói tin** bên phải. Chọn một gói tin bên trái thì bên phải nạp trường của gói đó. Chưa chọn thì hiện "Chọn một gói tin ở cột bên trái để xem danh sách trường".

### 5.3 Lưới Gói tin
Bộ lọc: Từ khóa (mã hoặc tên gói tin).

| Cột | Ý nghĩa |
|---|---|
| STT | |
| Tên gói tin | |
| Mã | Mã gói tin, ví dụ `101_commonData` |
| Trạng thái | Đang dùng / Ngừng |
| Thông tin | Người tạo, thời điểm |
| Thao tác | Sửa, Xoá |

Form gói tin: **Mã**, **Tên**, **Phiên bản**, **Mô tả**, **Trạng thái**, **Thứ tự**, **Ghi chú**.

### 5.4 Lưới Trường gói tin
Bộ lọc: Từ khóa (tên hoặc mã trường), lọc theo trạng thái.

| Cột | Ý nghĩa |
|---|---|
| STT | |
| Tên trường | Tên tiếng Việt để người dùng đọc |
| Khóa field | **Khóa kỹ thuật** — đây là cái phải trùng với tên cột mà câu truy vấn trả về. Sai một ký tự là trường không bao giờ có dữ liệu |
| Kiểu | string / number / datetime / bool |
| Trạng thái | |
| Bắt buộc | Trường bắt buộc mà rỗng sẽ làm hệ thống **chặn cả bản ghi** khi gửi |
| Thông tin | Người tạo, thời điểm |
| Thao tác | Sửa, Xoá |

Form trường: **Gói tin** (bắt buộc chọn trước), **Tên trường**, **Khóa field**, **Kiểu**, **Nhóm trường**, **Bắt buộc**, **Trạng thái**, **Ghi chú**.

### 5.5 Bảng tra: mỗi gói tin lấy dữ liệu từ bảng nào, từng trường lấy từ cột nào

Đây là phần quan trọng nhất của màn này. Các bảng dưới đây lấy từ **câu truy vấn thật đang chạy** trong mã nguồn.

Cột *Cách lấy* có hai giá trị:
- **Toàn bộ** — mỗi kỳ gửi lấy lại toàn bộ dữ liệu hiện có.
- **Chỉ bản ghi mới** — chỉ lấy bản ghi phát sinh sau mốc thời gian của kỳ gửi trước. Chạy lần đầu thì lấy từ đầu. Vài gói giới hạn 50 bản ghi mỗi kỳ.

---

**Gói 101 — `101_commonData` · Dữ liệu giao thông chung · Cách lấy: Toàn bộ**
Bảng: `TmsZoneStatus` (gốc) + `TmsZone` (nối theo `ZoneId = ID`) + `TmsTrafficStatistic` (nối theo `ZoneId`)

| Khóa field | Lấy từ |
|---|---|
| zoneStatusId | TmsZoneStatus.ID |
| zoneId | TmsZoneStatus.ZoneId |
| zoneName | TmsZone.Name |
| fromLocationKm | TmsZone.FromKmNumber |
| fromLocationMet | TmsZone.FromMetNumber |
| toLocationKm | TmsZone.ToKmNumber |
| toLocationMet | TmsZone.ToMetNumber |
| laneId | TmsZone.LaneId |
| averageSpeed | TmsZoneStatus.AverageSpeed |
| trafficCondition | TmsZoneStatus.Condition |
| dataTime | TmsZoneStatus.UpdateTime |
| speedLimit | TmsZone.MaxSpeed |
| vehicleCount | TmsTrafficStatistic.TotalVehicleNumber |

---

**Gói 102 — `102_cctvData` · Camera giám sát · Cách lấy: Toàn bộ**
Bảng: `CctvDevice` (gốc) + `TmsEquipment` (nối theo `Ip`)

| Khóa field | Lấy từ |
|---|---|
| cameraCode | TmsEquipment.Code |
| cameraName | CctvDevice.Name |
| snapshot | **Luôn rỗng** — câu truy vấn trả `NULL` cố định |
| snapshotTime | CctvDevice.SnapshotTime |
| deviceState | CctvDevice.DeviceState |
| locationKm | TmsEquipment.KmNumber |
| locationMet | TmsEquipment.MetNumber |
| direction | TmsEquipment.DirectionId |

---

**Gói 103 — `103_vdsData` · Thiết bị dò xe · Cách lấy: Chỉ bản ghi mới, tối đa 50/kỳ**
Bảng: `TmsTrafficData` (gốc) + `TmsEquipment` (nối theo `EquipmentId = ID`)

| Khóa field | Lấy từ |
|---|---|
| detectionId | TmsTrafficData.ID |
| detectTime | TmsTrafficData.DetectTime |
| vehicleType | TmsTrafficData.Type |
| licensePlate | TmsTrafficData.LicensePlate |
| speed | TmsTrafficData.Speed |
| lane | TmsTrafficData.Lane |
| direction | TmsTrafficData.Direction |
| locationRoute | TmsTrafficData.Location |
| equipmentId | TmsTrafficData.EquipmentId |
| locationKm | TmsEquipment.KmNumber |
| locationMet | TmsEquipment.MetNumber |

Gói này khai đủ, không có trường nào thiếu nguồn.

---

**Gói 104 thời tiết — `104_weatherData` · Cách lấy: Chỉ bản ghi mới**
Bảng: `TmsWeather`

| Khóa field | Lấy từ |
|---|---|
| weatherStationId | TmsWeather.RefId |
| locationDetail | TmsWeather.LocationDetail |
| temperature | TmsWeather.Temperature |
| humidity | TmsWeather.Hudmidity |
| windSpeed | TmsWeather.WindSpeed |
| windDirection | TmsWeather.WindDirection |
| rainfall | TmsWeather.Rain |
| rainfallHour | TmsWeather.RainHour |
| visibility | TmsWeather.Foresight |
| weatherDescription | TmsWeather.Description |
| weatherCode | TmsWeather.ShortDescription |
| detectTime | TmsWeather.TimeDetect |

---

**Gói 104 RFID — `104_rfidData` · Nhận dạng phương tiện · Cách lấy: Toàn bộ**
Bảng: `TollTransactionOut` (gốc) + `TmsVehicleRegistration` (nối theo biển số)

| Khóa field | Lấy từ |
|---|---|
| transactionId | TollTransactionOut.TransactionId |
| tagId | TollTransactionOut.TagId |
| licensePlate | TollTransactionOut.PlateEdit, rỗng thì lấy PlateLpr |
| vehicleTypeId | TollTransactionOut.VehicleTypeId |
| entryTime | TollTransactionOut.TransactionDateTimeIn |
| exitTime | TollTransactionOut.TransactionDateTime |
| laneId | TollTransactionOut.LaneId |
| stationId | TollTransactionOut.StationId |
| vehicleBrand | TmsVehicleRegistration.Brand |
| vehicleOwner | TmsVehicleRegistration.Owner |

---

**Gói 106 — `106_wimData` · Cân động · Cách lấy: Chỉ bản ghi mới, tối đa 50/kỳ**
Bảng: `TmsTrafficData`

| Khóa field | Lấy từ |
|---|---|
| detectTime | TmsTrafficData.DetectTime |
| lane | TmsTrafficData.Lane |
| locationCode | TmsTrafficData.Location |
| speed | TmsTrafficData.Speed |
| height | TmsTrafficData.Height |
| width | TmsTrafficData.Width |
| length | TmsTrafficData.Length |

---

**Gói 107 — `107_incidentData` · Sự cố giao thông · Cách lấy: Chỉ bản ghi mới**
Bảng: `TmsIncident` (gốc) + `TmsEventType` (nối theo `EventTypeId = ID`). Mốc so sánh là `UpdateTime`, chưa có thì `StartDate` — nên **sự cố được cập nhật cũng được gửi lại**.

| Khóa field | Lấy từ |
|---|---|
| incidentCode | TmsIncident.Code |
| incidentName | TmsIncident.Name |
| eventTypeId | TmsIncident.EventTypeId |
| eventTypeName | TmsEventType.Name |
| occurredTime | TmsIncident.StartDate |
| locationKm | TmsIncident.KmNumber |
| locationMet | TmsIncident.MetNumber |
| locationRoute | TmsIncident.Location |
| direction | TmsIncident.InfluenceScope |
| injuredCount | TmsIncident.InjuredNumber |
| vehicleCount | TmsIncident.VehicleNumber |
| incidentState | TmsIncident.State |
| description | TmsIncident.Description |
| source | TmsIncident.Source |

---

**Gói 108 — `108_vmsInfo` · Biển báo điện tử · Cách lấy: Toàn bộ**
Bảng: `VmsCurrent` (gốc) + `TmsEquipment` (nối theo `EquipmentId = ID`)

| Khóa field | Lấy từ |
|---|---|
| equipmentCode | TmsEquipment.Code |
| vmsName | VmsCurrent.Name |
| locationKm | TmsEquipment.KmNumber |
| locationMet | TmsEquipment.MetNumber |
| direction | TmsEquipment.DirectionId |
| laneId | TmsEquipment.LaneId |
| displayContent | VmsCurrent.RowData |
| displayImageUrl | VmsCurrent.Url |
| displaySize | VmsCurrent.Size |
| priority | VmsCurrent.Priority |
| executedTime | VmsCurrent.ExecutedDate |

---

**Gói 109 — `109_etcData` · Thu phí không dừng · Cách lấy: Chỉ bản ghi mới, tối đa 50/kỳ**
Bảng: `TollTransactionOut` (gốc) + `TollLane` (nối `LaneId`) + `TollStation` (nối `StationId`)

| Khóa field | Lấy từ |
|---|---|
| transactionId | TollTransactionOut.TransactionId |
| entryTime | TollTransactionOut.TransactionDateTimeIn |
| exitTime | TollTransactionOut.TransactionDateTime |
| vehicleTypeId | TollTransactionOut.VehicleTypeId |
| licensePlate | TollTransactionOut.PlateEdit, rỗng thì lấy PlateLpr |
| tagId | TollTransactionOut.TagId |
| laneId | TollTransactionOut.LaneId |
| laneName | TollLane.Name |
| stationId | TollTransactionOut.StationId |
| stationName | TollStation.Name |
| tollPrice | **Luôn rỗng** — câu truy vấn trả `NULL` cố định |
| syncTime | TollTransactionOut.SyncTime |

---

**Gói 110 — `110_wpData` · Cảnh báo phát hành · Cách lấy: Toàn bộ**
Bảng: `TmsIncident` (gốc), lấy kèm nội dung VMS gần nhất tại đúng km của sự cố (`TmsEquipment` + `VmsCurrent`). **Chỉ lấy sự cố chưa kết thúc** — bỏ các sự cố ở trạng thái `FINISHED`, `CANCELED`, `Closed`, `Cancelled`.

| Khóa field | Lấy từ |
|---|---|
| incidentMessage | Ghép `TmsIncident.Name` + ` - ` + `TmsIncident.Description` |
| guidanceContent | VmsCurrent.RowData của biển gần nhất tại km sự cố |
| locationKm | TmsIncident.KmNumber |
| locationMet | TmsIncident.MetNumber |
| publishedTime | TmsIncident.StartDate |

---

**Gói 111 — `111_testData` · Dữ liệu test · Cách lấy: Toàn bộ**
Bảng: `TmsIncident`. Câu truy vấn trả về: `incidentCode` ← Code, `incidentName` ← Name, `locationKm` ← KmNumber, `locationMet` ← MetNumber, `description` ← Description.
**Gói này hiện chưa khai trường nào** trong màn Nguồn dữ liệu, nên màn Ánh xạ không có trường nào để chọn.

### 5.6 Trường đã khai nhưng KHÔNG có nguồn dữ liệu

Các trường sau xuất hiện trong ô chọn ở màn Ánh xạ, nhưng câu truy vấn **không trả về**, nên gửi đi sẽ luôn rỗng. Đây là tình trạng hiện tại của cấu hình, **không phải lỗi giao diện**:

| Gói tin | Trường chưa có nguồn |
|---|---|
| 101_commonData | routeName, roadType, roadAuthority, pavementType, laneCount, shoulderWidth |
| 102_cctvData | cctvDeviceId, speed, vehicleType, và `snapshot` (luôn `NULL`) |
| 104_weatherData | roadSurfaceTemperature, roadAvailability, weatherId |
| 104_rfidData | rfidTransactionId |
| 106_wimData | axleWeights, grossWeight, axleCount, isOverweight, wimRecordId |
| 107_incidentData | incidentId, managementAgency |
| 108_vmsInfo | vmsCurrentId, equipmentRefId |
| 109_etcData | queueLength, vehicleCount, etcTransactionId, và `tollPrice` (luôn `NULL`) |
| 110_wpData | messageId, deliveryState, weatherMessage, wpIncidentId, channel |

Hai điểm lệch khác trong cấu hình hiện tại, nên báo lại cho người khai dữ liệu:

- Gói 101 có một trường khai **khóa field sai chính tả**: `dataTime dsấ` (có khoảng trắng và ký tự lạ). Câu truy vấn trả về `dataTime`, nên trường này sẽ **không bao giờ khớp** và luôn rỗng.
- Gói 101: câu truy vấn **có** trả về `vehicleCount` nhưng danh mục trường **không khai**, nên màn Ánh xạ không chọn được trường này.

### 5.7 Chiều nhận: mỗi gói tin ghi vào bảng nào

Khai trong bảng `ShareDataPacketWrite` (CSDL, **không có màn UI**), chạy theo thứ tự `OrderNo`, ghi vào **CSDL `DEV_ITS10_Inbound`**:

| Gói tin | Thứ tự ghi vào bảng |
|---|---|
| 101_commonData | TmsZone → TmsTrafficStatistic → TmsZoneStatus |
| 102_cctvData | TmsEquipment → CctvDevice |
| 103_vdsData | TmsEquipment → TmsTrafficData |
| 104_rfidData | TmsVehicleRegistration → TollTransactionOut |
| 104_weatherData | TmsWeather |
| 106_wimData | TmsTrafficData |
| 107_incidentData | TmsEventType → TmsIncident |
| 108_vmsInfo | TmsEquipment → VmsCurrent |
| 109_etcData | TollStation → TollLane → TollTransactionOut |

Hai gói **chưa nhận được**, cần khai bổ sung:
- **110_wpData**: câu ghi hiện khai với mã gói tin là `110_masterData`, lệch so với mã gói tin thật `110_wpData` → hệ thống sẽ báo "chưa khai câu ghi chiều nhận".
- **111_testData**: chưa khai câu ghi nào.

---

## 6. Màn **Nhật ký** và màn **Cảnh báo lỗi**

Hai màn này chỉ để **xem lại**, không sửa dữ liệu nghiệp vụ.

### 6.1 Màn Nhật ký (History)

Hai tab:

**Tab *Truyền nhận*** — từng lần gửi/nhận gói tin.
| Cột | Ý nghĩa |
|---|---|
| Thời gian | |
| Chiều | Gửi đi / Nhận về |
| Đối tác | |
| Loại dữ liệu | Gói tin |
| Serial | Số thứ tự gói trong phạm vi đối tác |
| Loại gói (PDU) | Gói dữ liệu hay gói tệp |
| Định dạng | Dữ liệu / Tệp |
| Dung lượng (B) | |
| Số bản ghi | Số bản ghi trong gói |
| Kết quả | Thành công / Thất bại |
| Thao tác | Mở chi tiết |

**Tab *Cấu hình*** — ai đã thay đổi cấu hình gì.
| Cột | Ý nghĩa |
|---|---|
| Thời gian | |
| Hành động | Thêm mới, Cập nhật, Xóa, Kết nối, Ngắt kết nối, Tắt/Bật/Hủy/Duyệt/Từ chối đăng ký, Gửi, Nhận, Xuất file |
| Đối tượng | Bản ghi bị tác động |
| Nội dung | Mô tả |
| Trường thay đổi | Danh sách trường đã đổi |
| Người thực hiện | |
| Kết quả | Thành công / Thất bại |

Bộ lọc dùng chung: khoảng thời gian (có nút nhanh **24 giờ / 7 ngày / 30 ngày**), Đối tác, Hành động, Kết quả, và tìm trong nội dung. Phía trên có ba số tổng: **Tổng bản ghi**, **Gói gửi**, **Gói nhận**.

Bấm **Chi tiết** mở hộp thoại gồm: thông tin chung, thông tin gói tin (chiều, serial, số gói, PDU, định dạng, dung lượng, số bản ghi), **file bằng chứng** (đường dẫn và hash), và với hành động cập nhật thì có bảng **so sánh giá trị Trước / Sau** theo từng trường.

### 6.2 Màn Cảnh báo lỗi (AlertLog)

| Cột | Ý nghĩa |
|---|---|
| STT | |
| Thời gian xảy ra | |
| Mức độ | Cảnh báo / Lỗi |
| Nguồn | Phiên kết nối / Gói tin / Đăng ký / Giao thức / Luồng dữ liệu |
| Mã cảnh báo | Mã kỹ thuật dạng `ESH-xxxx`, dùng để tra nguyên nhân |
| Đối tác | |
| Loại dữ liệu | |
| Nội dung | Diễn giải lỗi |
| Trạng thái xử lý | Chưa xử lý / Đã xử lý |
| Thao tác | Chi tiết, Xác nhận đã xử lý |

Bộ lọc: khoảng thời gian (24 giờ / 7 ngày / 30 ngày), Mức độ, Nguồn, Đối tác, Mã cảnh báo, Nội dung, Trạng thái xử lý. Phía trên hiện "**N chưa xử lý**".

Nút **Xác nhận đã chọn** và **Xác nhận tất cả** (xác nhận toàn bộ cảnh báo chưa xử lý theo bộ lọc hiện tại, có hỏi lại). Quyền: `sharedataAlertLog:update`.

Hộp thoại chi tiết bổ sung: mã phiên, mã đăng ký, người và thời điểm xác nhận, ghi chú, và khối **chi tiết kỹ thuật** dạng JSON.

**Vài mã cảnh báo hay gặp khi test:**
| Mã | Nghĩa |
|---|---|
| ESH-1304 | Không tìm thấy hồ sơ ánh xạ chiều gửi đang bật → huỷ kết xuất. Nguyên nhân phổ biến nhất khi "đăng ký bật rồi mà không gửi gì" |
| ESH-1303 | Mất quyền chạy kỳ gửi |

Ngoài ra còn các cảnh báo: gói tin không tìm thấy cấu hình, bộ khung sai cú pháp JSON, thiếu trường bắt buộc, truy vấn dữ liệu lỗi, biểu thức không hợp lệ.

---

## 7. Giới hạn đã biết — không cần báo lỗi

1. **Xem trước & Xuất file (bằng chứng)** ở màn Chia sẻ dữ liệu: chưa có API backend.
2. **Tab Dữ liệu gửi thử** ở màn Ánh xạ: giá trị mẫu nhập tay, không lấy dữ liệu thật; và không áp định dạng ngày / định dạng số / biểu thức nên khác payload thật ở các trường ngày và số.
3. **Lọc gói tin theo đăng ký** ở màn Ánh xạ hiện chỉ chặn trên giao diện. Gọi API trực tiếp vẫn tạo được hồ sơ cho gói tin không có đăng ký.
4. Bảng `ShareDataPacketWrite` (câu ghi chiều nhận) **không có màn UI**, phải khai bằng SQL.
5. Một số trường đã khai nhưng chưa có nguồn dữ liệu — xem mục 5.6.
6. Chiều nhận của gói **110_wpData** và **111_testData** chưa dùng được — xem mục 5.7.
7. Một số màn còn hiển thị nhãn trạng thái cố định trong mã nguồn trong lúc chờ backend cấp danh mục cấu hình.

---

## 8. Trình tự dựng dữ liệu tối thiểu để chạy được một vòng gửi

Đây là trình tự phụ thuộc, không phải danh sách ca kiểm thử:

1. Màn **Nguồn dữ liệu**: xác nhận gói tin cần test đã có và đã khai trường (gói 101–110 đã có sẵn; gói 111 chưa khai trường).
2. Màn **Chia sẻ dữ liệu**: tạo **đối tác**, trạng thái phải là Kích hoạt.
3. Màn **Chia sẻ dữ liệu**: bấm **Kết nối** đối tác.
4. Màn **Chia sẻ dữ liệu**: tạo **đăng ký** chiều Gửi đi cho gói tin cần test, khai lịch.
5. Màn **Bộ mã** (nếu cần đổi giá trị): tạo bộ mã.
6. Màn **Ánh xạ dữ liệu**: tạo hồ sơ cho đúng (đối tác × chiều × gói tin), dán JSON đối tác, bấm Phân tích, ánh xạ trường, khai đủ 4 giá trị hệ thống, rồi **bật Đang dùng**.
7. Quay lại màn **Chia sẻ dữ liệu**: kiểm tra cột *Mapping áp dụng* của đăng ký đã hiện tên hồ sơ. Nếu trống thì bước 6 chưa đúng.
8. Bật đăng ký, **chờ tới kỳ gửi**.
9. Màn **Nhật ký** tab Truyền nhận: xem kết quả. Nếu không có dòng nào, sang màn **Cảnh báo lỗi** đọc mã `ESH-xxxx`.
## 9. Ghi chú
Đây là phần lưu ý về phạm vị ảnh hưởng của hệ Chia sẻ dữ liệu (ShareData)
1. DB sử dụng: DEV_ITS10 và DEV_ITS10_Inbound - Server=10.10.8.30
2. Chiều gửi đi sẽ lưu file tại địa chỉ: "BasePath": "E:\\IIS_WebPool\\ITS\\ITS015\\Services\\TA-ShareData-Service\\sharedata\\send",
