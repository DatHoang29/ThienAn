# Đặc tả UI phân hệ Video Wall (dành cho Tester)

> **Phiên bản:** 1.0 · **Ngày:** 02/10/2026
> **Nguồn đối chiếu:** FE `TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall` · BE `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall`
> **Mục đích:** Giúp tester nắm bối cảnh nghiệp vụ Video Wall, hiểu từng màn hình làm gì, ai dùng, kỳ vọng ra sao để thiết kế test case.

**Quy ước quan trọng**

- **Validation lấy BE làm chuẩn.** Ở những chỗ FE đang kiểm tra khác BE, tài liệu ghi quy tắc theo BE và đánh dấu ⚠️ *FE hiện tại* (FE sẽ sửa sau). Tổng hợp ở [mục 8](#8-điểm-lệch-febe-và-lưu-ý-khi-test).
- Tên màn hình, nhãn nút, câu thông báo trong tài liệu là **tên mô tả**. Chữ hiển thị thật (i18n) do nhóm dịch thuật xử lý riêng, không dùng làm tiêu chí pass/fail.
- Tài liệu **chỉ bao gồm các chức năng có giao diện**. Các API chỉ có ở BE (đấu nối thiết bị, nhật ký kích hoạt, khe/cổng, cấu hình topology) không nằm trong phạm vi.
- Mã user story: `US-<màn>-<số>`. Tiêu chí chấp nhận (AC) viết dạng checklist.

---

## Mục lục

1. [Bối cảnh nghiệp vụ](#1-bối-cảnh-nghiệp-vụ)
2. [Thuật ngữ](#2-thuật-ngữ)
3. [Mô hình dữ liệu và luồng tổng thể](#3-mô-hình-dữ-liệu-và-luồng-tổng-thể)
4. [Quy tắc chung cho màn hình danh sách (CRUD)](#4-quy-tắc-chung-cho-màn-hình-danh-sách-crud)
5. [Phân quyền vùng màn hình (áp dụng xuyên suốt)](#5-phân-quyền-vùng-màn-hình-áp-dụng-xuyên-suốt)
6. [Đặc tả từng màn hình](#6-đặc-tả-từng-màn-hình)
   - 6.1 [Dashboard](#61-dashboard--tổng-quan)
   - 6.2 [Bộ điều khiển](#62-bộ-điều-khiển-controller)
   - 6.3 [Nguồn tín hiệu](#63-nguồn-tín-hiệu-source)
   - 6.4 [Màn hình đầu ra](#64-màn-hình-đầu-ra-output--screen)
   - 6.5 [Sơ đồ đấu nối cổng ra](#65-sơ-đồ-đấu-nối-cổng-ra-output-map)
   - 6.6 [Kịch bản hiển thị](#66-kịch-bản-hiển-thị-scene-editor)
   - 6.7 [Giám sát tường](#67-giám-sát-tường-monitor)
   - 6.8 [Lập lịch](#68-lập-lịch-schedule)
   - 6.9 [Tích hợp ITS – quy tắc sự kiện](#69-tích-hợp-its--quy-tắc-sự-kiện)
   - 6.10 [Phân quyền vùng màn hình](#610-phân-quyền-vùng-màn-hình-wall-permission)
7. [Luồng nghiệp vụ đầu-cuối gợi ý để test](#7-luồng-nghiệp-vụ-đầu-cuối-gợi-ý-để-test)
8. [Điểm lệch FE/BE và lưu ý khi test](#8-điểm-lệch-febe-và-lưu-ý-khi-test)
9. [Câu hỏi mở cần dev/BA xác nhận](#9-câu-hỏi-mở-cần-devba-xác-nhận)
10. [Phụ lục](#10-phụ-lục)

---

## 1. Bối cảnh nghiệp vụ

**Video Wall** là bức tường gồm nhiều màn hình ghép lại, đặt trong phòng điều hành giao thông (ITS). Nhân viên vận hành dùng nó để xem đồng thời nhiều nguồn hình: camera IP, tín hiệu HDMI/DVI từ máy trạm, trang web giám sát…

Cấu hình phần cứng hiện tại (được FE/BE dùng làm hằng số):

| Hạng mục | Giá trị |
|---|---|
| Kích thước tường | **8 cột × 4 hàng = 32 màn hình** |
| Màn hình | Panel LCD 55", độ phân giải logic **3840 × 2160** (4K) mỗi màn |
| Toạ độ toàn tường | 8 × 3840 = **30 720 px** ngang · 4 × 2160 = **8 640 px** dọc |
| Bộ điều khiển (controller) | Model mặc định **DS-C66S-S12**, khung 4U |
| Cổng mỗi bộ điều khiển | **8 cổng vào** (Cổng vào 1..8) và **8 cổng ra** (P1..P8) |

**Mô hình phân cấp 2 tầng:** một **bộ điều khiển trung tâm** quản lý nhiều **bộ điều khiển con**; mỗi bộ con nối tới một nhóm màn hình qua cổng ra. Nếu không khai báo cha–con thì bộ điều khiển là **độc lập**.

**Cách tường hiển thị nội dung:**

1. Khai báo phần cứng: bộ điều khiển, màn hình đầu ra (vị trí trên lưới + cổng ra), nguồn tín hiệu.
2. Thiết kế **kịch bản** (scene): bố cục gồm nhiều **cửa sổ**. Mỗi cửa sổ là một khung hiển thị trên toạ độ toàn tường và gắn với một nguồn tín hiệu.
3. **Kích hoạt** kịch bản. Có 3 cách: người vận hành bấm *Áp dụng* trên màn Giám sát, **lịch hẹn giờ**, hoặc **sự kiện ITS** (tai nạn, ùn tắc…) qua quy tắc sự kiện.
4. BE gửi lệnh qua NATS xuống Worker. Worker điều khiển thiết bị thật, rồi phát thông báo để mọi máy trạm đang mở màn Giám sát tự cập nhật. (Đang bị lỗi)

**Phân quyền theo vùng:** mỗi người dùng/đơn vị chỉ được thao tác trên một **vùng ô lưới** của tường (ví dụ đội A chỉ dùng cột 1–2). SuperAdmin có toàn quyền.

---

## 2. Thuật ngữ

| Thuật ngữ | Ý nghĩa |
|---|---|
| **Bộ điều khiển (Controller)** | Thiết bị xử lý video wall, có IP + tài khoản đăng nhập (giao thức ISAPI). |
| **Bộ trung tâm / Bộ con / Độc lập** | Vai trò suy ra từ quan hệ cha–con: có cha → *Bộ con*; được bộ khác chọn làm cha → *Bộ trung tâm*; còn lại → *Độc lập*. |
| **Vùng phủ (coverage)** | Khối ô lưới mà bộ điều khiển phụ trách: cột/hàng bắt đầu (tính từ 0) + số cột/hàng phủ. |
| **Nguồn tín hiệu (Source)** | Nội dung đưa lên tường. 3 loại: **Tín hiệu cục bộ** (`local_signal`, vào qua cổng vật lý), **Camera IP / luồng IP** (`ip_stream`, URL RTSP), **Liên kết controller** (`cascade_link`, dây nối giữa các bộ điều khiển; không kéo lên màn hình được). |
| **Màn hình đầu ra (Screen/Output)** | Một panel trên tường, xác định bằng (Cột, Hàng) trên lưới và nối với *Bộ điều khiển + Cổng ra*. |
| **Kịch bản (Scene)** | Bố cục hiển thị gồm nhiều cửa sổ. Có cờ **Mặc định** (chỉ 1 kịch bản) và **Trạng thái** Bật/Tắt. |
| **Cửa sổ (Window)** | Hình chữ nhật (X, Y, W, H, Z-index) trên toạ độ toàn tường, gắn 1 nguồn. |
| **Kích hoạt / Áp dụng** | Gửi kịch bản xuống tường thật. Khác với *chọn để xem/sửa*. |
| **Kịch bản đang phát (serving)** | Kịch bản BE ghi nhận đang chiếu trên tường. Mọi máy trạm dùng chung giá trị này. |
| **Xem trước (preview)** | Trên màn Giám sát: chọn một kịch bản khác để xem bố cục, **chưa** gửi xuống tường. |
| **Vùng được cấp** | Tập ô (Cột, Hàng) mà người dùng/đơn vị được phép thao tác. |
| **Snap lưới** | Khi kéo/thả/resize cửa sổ, toạ độ tự làm tròn theo bội số kích thước 1 màn (3840 × 2160). |

---

## 3. Mô hình dữ liệu và luồng tổng thể


### 3.3 Danh sách màn hình trong phạm vi

| # | Màn hình | Mục đích chính | Người dùng điển hình |
|---|---|---|---|
| 6.1 | Dashboard | Xem tổng quan trạng thái tường | Mọi người |
| 6.2 | Bộ điều khiển | Khai báo thiết bị, quan hệ trung tâm/con | Kỹ thuật |
| 6.3 | Nguồn tín hiệu | Khai báo nguồn | Kỹ thuật |
| 6.4 | Màn hình đầu ra | Khai báo panel, vị trí, cổng | Kỹ thuật |
| 6.5 | Sơ đồ đấu nối | Gán cổng ra ↔ màn hình bằng thao tác trực quan | Kỹ thuật |
| 6.6 | Kịch bản | Thiết kế bố cục cửa sổ (kéo–thả) | Vận hành / kỹ thuật |
| 6.7 | Giám sát | Xem tường, đổi kịch bản đang phát | Vận hành |
| 6.8 | Lập lịch | Tự động đổi kịch bản theo giờ | Vận hành | (Đang làm)
| 6.9 | Tích hợp ITS | Tự động đổi kịch bản theo sự kiện | Vận hành | (Đang sửa)
| 6.10 | Phân quyền vùng | Cấp vùng ô lưới cho user/đơn vị | Quản trị |

---

## 4. Quy tắc chung cho màn hình danh sách (CRUD)

Áp dụng cho các màn: Bộ điều khiển, Nguồn tín hiệu, Màn hình đầu ra, Lập lịch, Tích hợp ITS, Phân quyền vùng.

**Bố cục chung:** *Khung tìm kiếm* (trên) → nút **Tìm kiếm / Đặt lại** → *Bảng dữ liệu* có thanh công cụ **Thêm · Xoá (nhiều) · Làm mới · Tuỳ chỉnh cột** → cột **Thao tác** cố định bên phải.

- [ ] **Tìm kiếm:** nhập điều kiện → bấm Tìm kiếm (hoặc Enter ở ô Tên) → bảng nạp lại theo điều kiện. Ô Tên tìm theo *chứa chuỗi*.
- [ ] **Đặt lại:** xoá hết điều kiện và nạp lại bảng.
- [ ] **Phân trang:** mặc định 50 dòng/trang. Kích thước trang và cột sắp xếp được ghi nhớ trên trình duyệt (riêng Bộ điều khiển hiển thị dạng cây, không phân trang).
- [ ] **Thêm:** mở hộp thoại với giá trị mặc định (xem từng màn). Lưu thành công → thông báo *Lưu thành công*, đóng hộp thoại, nạp lại bảng.
- [ ] **Sao chép:** mở hộp thoại điền sẵn dữ liệu dòng đang chọn. Lưu sẽ **tạo bản ghi mới** (ID mới), nên phải đổi Mã nếu Mã là duy nhất.
- [ ] **Sửa:** mở hộp thoại với dữ liệu hiện tại; Lưu cập nhật bản ghi.
- [ ] **Xoá 1 dòng:** hỏi xác nhận (kèm tên bản ghi) → Đồng ý → xoá → thông báo *Xoá thành công*. Huỷ → không làm gì.
- [ ] **Xoá nhiều:** nút Xoá trên thanh công cụ **chỉ hiện khi đã tick ≥ 1 dòng**. Xác nhận có số lượng bản ghi. Nếu một ID không còn tồn tại → BE báo *dữ liệu không khớp* và **không xoá dòng nào**.
- [ ] **Cột Audit:** hiển thị người tạo/sửa và thời gian.
- [ ] **Hộp thoại:** kéo di chuyển được; **không đóng khi bấm ra ngoài**; nút *Huỷ* đóng mà không lưu.
- [ ] **Mã trùng:** BE kiểm tra Mã duy nhất trong số bản ghi chưa xoá (Mã được cắt khoảng trắng hai đầu trước khi so). Trùng → báo *Mã đã tồn tại*.
- [ ] **Trường Trạng thái** (Bật/Tắt) là **bắt buộc** và phải là giá trị hợp lệ.
- [ ] **Nút theo quyền:** Thêm/Sao chép cần quyền `:add`, Sửa cần `:update`, Xoá cần `:delete`, Tìm kiếm cần `:page` (mã quyền ở [Phụ lục 10.1](#101-mã-quyền-nút-v-auth)). Không có quyền → nút bị ẩn.

---

## 5. Phân quyền vùng màn hình (áp dụng xuyên suốt)

### 5.1 Xác định quyền hiệu lực của người đăng nhập

| Thứ tự | Điều kiện | Quyền hiệu lực |
|---|---|---|
| 1 | Tài khoản **SuperAdmin** (hoặc tiến trình nền của hệ thống) | **Toàn quyền** |
| 2 | Có bản ghi phân quyền gắn **trực tiếp User** | Theo bản ghi của User (**ưu tiên hơn Đơn vị**) |
| 3 | Không có bản ghi User, có bản ghi của **Đơn vị** mà user thuộc về | Theo bản ghi Đơn vị |
| 4 | Không có bản ghi nào | **Không có quyền** (chặn mọi thao tác ghi liên quan vùng) |

Kết quả của một bản ghi:
- Đánh dấu **Toàn quyền** → được thao tác cả tường.
- Có danh sách ô → **Theo vùng**.
- Danh sách ô rỗng hoặc sai định dạng → coi như **Không có quyền**.



### 5.2 Quy tắc "cửa sổ nằm trong vùng"

- Một cửa sổ (X, Y, W, H) **phủ** các ô: cột từ `X / 3840` đến `(X + W − 1) / 3840`, hàng từ `Y / 2160` đến `(Y + H − 1) / 2160` (chia lấy phần nguyên).
- Cửa sổ hợp lệ khi **mọi ô nó phủ** đều thuộc vùng được cấp. Chỉ cần lấn 1 px sang ô ngoài vùng là bị chặn.
- Kịch bản *nằm trong vùng* khi **mọi cửa sổ** (chưa xoá) của nó nằm trong vùng.

### 5.3 Bảng quyền theo thao tác (chuẩn theo BE)

| Thao tác | Điều kiện để được phép (khi không toàn quyền) |
|---|---|
| Tạo kịch bản mới | Có vùng (không phải "Không có quyền") |
| Sửa thông tin kịch bản (mã, tên, ghi chú…) | Mọi cửa sổ hiện có của kịch bản nằm trong vùng |
| Xoá kịch bản (1 hoặc nhiều) | Mọi cửa sổ nằm trong vùng; kịch bản rỗng thì vẫn phải có vùng |
| Đặt kịch bản mặc định | **Chỉ SuperAdmin** (bản ghi Toàn quyền cũng **không** đủ) |
| Thêm cửa sổ | Vị trí mới nằm trong vùng |
| Sửa cửa sổ | **Cả vị trí cũ và vị trí mới** đều nằm trong vùng |
| Xoá cửa sổ | Vị trí cửa sổ nằm trong vùng |
| Thêm / Xoá lịch, quy tắc sự kiện | Kịch bản đích nằm trong vùng |
| Sửa lịch, quy tắc sự kiện | Kịch bản đích **cũ** và **mới** đều nằm trong vùng |
| Xem (đọc) kịch bản, cửa sổ | Không giới hạn: ai cũng xem được mọi vùng |
| Kích hoạt kịch bản | Xem ⚠️ [L-01](#8-điểm-lệch-febe-và-lưu-ý-khi-test): hiện chỉ FE chặn |
| Quản lý bản ghi phân quyền vùng | Không kiểm tra vùng; chỉ theo quyền menu/nút |

Ngoài BE, FE còn hỗ trợ trực quan: ô ngoài vùng bị **làm mờ/gạch chéo** (di chuột có chú thích *Ngoài vùng được cấp*), cửa sổ ngoài vùng bị **khoá kéo/resize**, và có thông báo cảnh báo khi thao tác bị chặn. BE luôn kiểm tra lại khi lưu.

---

## 6. Đặc tả từng màn hình

### 6.1 Dashboard – Tổng quan

**Mục đích:** Nhìn nhanh sức khoẻ tường màn hình và bộ điều khiển. Màn **chỉ đọc**, không có thao tác ghi.

**Bố cục**

| Khu vực | Nội dung |
|---|---|
| 4 thẻ tổng | Tổng số màn hình · Màn **Bật** (trạng thái kết nối = Online) · Màn **Tắt** (còn lại) · Tổng số bộ điều khiển |
| Trạng thái bộ điều khiển | Mỗi dòng: thẻ Mã (tô màu của bộ điều khiển) · Tên · IP (trống → "—") · Model · *Hoạt động/Đã dừng* (theo Trạng thái cấu hình) · "8 cổng" |
| Lưới thời gian hoạt động | Lưới 8 cột, mỗi ô 1 màn: Mã · Số giờ chạy (`—h` nếu không có) · Độ phân giải. Viền dưới tô màu bộ điều khiển. Di chuột → tooltip Mã–Tên, giờ chạy, độ phân giải |
| Bảng chi tiết màn hình | Mã · Tên · Bộ điều khiển · Trạng thái (Bật/Tắt) · Thời gian chạy · Độ phân giải · Loại panel. Cao tối đa 360px, cuộn |
| Thống kê nhanh | Giờ chạy trung bình (làm tròn) · Giờ chạy lớn nhất · Độ phân giải phổ biến · Kịch bản đang chạy · Topology (luôn `8×4`) |

### 6.2 Bộ điều khiển (Controller)

**Mục đích:** Khai báo các bộ điều khiển video wall, thông tin kết nối, quan hệ trung tâm–con và vùng phủ trên lưới.

**Bố cục danh sách**
- Tìm kiếm: Tên · Model (danh mục) · Trạng thái.
- Bảng **dạng cây**: bộ trung tâm ở gốc, bộ con lồng bên dưới, mặc định mở hết; **không phân trang**, sắp theo Mã.
- Cột: Mã (nút cây) · Tên · **Vai trò** (Trung tâm / Con / Độc lập) · Model · IP · Tài khoản · **Vùng phủ** (hiển thị đánh số từ 1, ví dụ "Cột 1–2, Hàng 1–4") · Màu · Trạng thái · Audit · Thao tác.
- Tick bộ trung tâm **không** tự tick các bộ con (tránh xoá nhầm cả nhánh).

**Hộp thoại Thêm/Sửa – 2 tab**

*Tab "Thông tin cơ bản"*: Mã · Tên · Model · Địa chỉ IP · Trạng thái · Tài khoản · Mật khẩu (ẩn ký tự, có nút hiện) · **Bộ điều khiển cha** · Ghi chú.

*Tab "Vùng phủ"*: Cột bắt đầu · Hàng bắt đầu · Số cột phủ · Số hàng phủ · Số khe vào · Số khe ra · Màu (danh mục màu, có ô màu xem trước).

Giá trị mặc định khi Thêm: Trạng thái = Bật, Model = `DS-C66S-S12`, Chassis = `4U`, Màu = primary, Cột/Hàng bắt đầu = 0, Phủ 2 cột × 4 hàng, 6 khe vào, 6 khe ra.

**Validation (chuẩn BE)**

| Trường | Quy tắc | ⚠️ FE hiện tại |
|---|---|---|
| Mã | Thêm mới: **không được trùng** với bộ điều khiển chưa xoá. BE không ràng buộc bắt buộc/độ dài | FE bắt buộc. Sửa: xem L-04 |
| Tên | **Bắt buộc**, ≤ 64 ký tự. Khi **Sửa**: không trùng Tên với bản ghi khác | — |
| Model | ≤ 64 | FE bắt buộc |
| Địa chỉ IP | **Bắt buộc**, ≤ 32 | FE thêm kiểm tra định dạng IPv4 |
| Tài khoản | **Bắt buộc**, ≤ 64 | FE **không** đánh dấu bắt buộc |
| Mật khẩu | **Bắt buộc**, ≤ 64 | FE **không** đánh dấu bắt buộc |
| Ghi chú | ≤ 256 | — |
| Trạng thái | Bắt buộc, giá trị hợp lệ | — |
| Cột/Hàng bắt đầu | ≥ 0 (nếu nhập) | — |
| Số cột/hàng phủ | **> 0** (nếu nhập) | FE cho nhập 0 |
| Số khe vào / ra | ≥ 0 (nếu nhập) | — |
| Bộ điều khiển cha | Xem quy tắc phân cấp bên dưới | — |

**Quy tắc phân cấp (BE)**
1. Không được chọn **chính mình** làm cha.
2. Bộ cha phải **tồn tại** (chưa xoá).
3. Bộ cha phải là bộ **không có cha** (tối đa 2 tầng, không có tầng 3).
4. Bộ **đang có bộ con** (đang là trung tâm) thì **không được gán cha**.
5. Để trống ô cha → bộ đó là trung tâm/độc lập. Khi sửa, xoá ô cha thì bộ con trở về độc lập.

### 6.3 Nguồn tín hiệu (Source)

**Mục đích:** Khai báo các nguồn nội dung có thể đưa lên tường.

**Bố cục danh sách**
- Tìm kiếm: Tên · Loại nguồn · Bộ điều khiển · Trạng thái.
- Cột: Mã · Tên · Loại nguồn (tag) · Loại tín hiệu · Bộ điều khiển (trống → hiển thị *Dùng chung*) · **Điểm kết nối** · Tỉ lệ khung hình · Trạng thái · Thứ tự · Audit · Thao tác. Sắp xếp mặc định theo Thứ tự tăng dần.
- *Điểm kết nối*: Tín hiệu cục bộ / Liên kết controller → "Cổng vào N"; Camera IP → URL.
- Cột "Tín hiệu" (có tín hiệu / mất / bất thường) đang **tạm ẩn** do chưa có dữ liệu từ thiết bị.

**Hộp thoại Thêm/Sửa** – trường hiển thị **thay đổi theo Loại nguồn**:

| Trường | Tín hiệu cục bộ | Liên kết controller | Camera IP | Chưa chọn loại |
|---|---|---|---|---|
| Mã, Tên, Loại nguồn | ✔ | ✔ | ✔ | ✔ |
| Loại tín hiệu | ✔ | ✔ | ✔ | ẩn |
| Bộ điều khiển | ✔ (FE bắt buộc) | ✔ (FE bắt buộc) | ✔ tuỳ chọn, trống = *Dùng chung* | ẩn |
| Cổng vào (1..8) | ✔ (FE bắt buộc, khoá khi chưa chọn bộ điều khiển) | ✔ (FE bắt buộc) | ẩn | ẩn |
| URL | ✔ tuỳ chọn (trang client) | ẩn | ✔ (FE bắt buộc, gợi ý `rtsp://...`) | ẩn |
| Độ phân giải tối đa, Trạng thái, Thứ tự, Ghi chú | ✔ | ✔ | ✔ | ✔ |

Mặc định khi Thêm: Trạng thái = Bật, Thứ tự = 100.

**Hành vi phụ thuộc**
- Danh sách *Loại tín hiệu* được lọc theo loại nguồn (theo cấu hình danh mục). Nếu sau khi lọc chỉ còn **1** lựa chọn thì tự chọn sẵn.
- Đổi Loại nguồn → bỏ Loại tín hiệu không còn hợp lệ, xoá Cổng vào/URL nếu loại mới không dùng, xoá thông báo lỗi.
- Đổi Bộ điều khiển → xoá Cổng vào đã chọn.

**Validation (chuẩn BE)**

| Trường | Quy tắc | ⚠️ FE hiện tại |
|---|---|---|
| Mã | **Bắt buộc**, ≤ 64, không trùng | — |
| Tên | **Bắt buộc**, ≤ 64 | — |
| Loại nguồn | ≤ 64 | FE bắt buộc |
| Loại tín hiệu | ≤ 64 | — |
| URL | **≤ 128** | FE cho tới **256** |
| Cổng vào | ≥ 0 | FE giới hạn 1..8 |
| Độ phân giải tối đa, Tỉ lệ khung hình | ≤ 128 | — |
| Ghi chú | ≤ 256 | — |
| Trạng thái | Bắt buộc | — |
| Thứ tự | ≥ 0 | — |
| Bộ điều khiển | Nếu chọn thì phải tồn tại (chưa xoá) | — |
| *Quy tắc bắt buộc theo loại nguồn* (bảng trên) | **BE không kiểm tra** | FE kiểm tra (L-06) |


### 6.4 Màn hình đầu ra (Output / Screen)

**Mục đích:** Khai báo từng panel trên tường: vị trí trên lưới, bộ điều khiển và cổng ra nối tới.

**Bố cục danh sách**
- Tìm kiếm: Tên · Bộ điều khiển · Trạng thái.
- Cột: Mã · Tên · Bộ điều khiển · Cổng ra · **Vị trí** (dạng `H{hàng+1} · C{cột+1}`) · Kích thước panel · **Kết nối** (Online = xanh, Warning = vàng, Offline = đỏ; trống → *Chưa xác định*) · Trạng thái · Thứ tự · Audit · Thao tác.

**Hộp thoại Thêm/Sửa – 3 tab**
- *Thông tin cơ bản*: Mã · Tên · Bộ điều khiển · Cổng ra (P1..P8, khoá khi chưa chọn bộ điều khiển; đổi bộ điều khiển → xoá cổng) · Output ID (ISAPI – mã đầu ra trên thiết bị) · Trạng thái.
- *Vị trí & panel*: Cột lưới · Hàng lưới (tính từ 0) · Rộng px · Cao px · Kích thước panel (inch) · Model panel.
- *Thông tin khác*: Thứ tự · Ghi chú.

Mặc định khi Thêm: Bật, Loại panel LCD, 55", `3840x2160@30Hz`, 3840 × 2160, Thứ tự 100.

**Hành vi khi lưu**
- FE tự tính toạ độ pixel: `PosX = Cột × Rộng`, `PosY = Hàng × Cao` (chỉ khi đã có đủ giá trị).

**Validation (chuẩn BE)**

| Trường | Quy tắc | ⚠️ FE hiện tại |
|---|---|---|
| Mã | **Bắt buộc**, ≤ 64, không trùng | — |
| Tên | **Bắt buộc**, ≤ 64 | — |
| Độ phân giải, Loại panel, Model panel | ≤ 128 | — |
| Kích thước panel | ≥ 0 | — |
| Ghi chú | ≤ 256 | — |
| Trạng thái | Bắt buộc | — |
| PosX, PosY, Thứ tự | ≥ 0 | — |
| Bộ điều khiển | **Khi Thêm**: nếu chọn thì phải tồn tại. Khi Sửa: BE không kiểm tra (L-08) | FE bắt buộc |
| Cổng ra | BE không ràng buộc | FE bắt buộc, giới hạn 1..8 |
| Trùng (Bộ điều khiển + Cổng ra) | **BE không chặn** | Form này cũng **không** chặn; chỉ màn Sơ đồ đấu nối chặn (L-09) |
| Trùng vị trí (Cột, Hàng) | **BE không chặn** | Sơ đồ đấu nối chỉ hiển thị màn đầu tiên (L-10) |


### 6.5 Sơ đồ đấu nối cổng ra (Output Map)

**Mục đích:** Gán/đổi/huỷ gán **cổng ra của bộ điều khiển ↔ màn hình** bằng thao tác trực quan thay vì sửa form. Mỗi lần gán là một lệnh **cập nhật Màn hình đầu ra** (ghi Bộ điều khiển + Cổng ra).

**Bố cục**
1. **Thẻ bộ điều khiển** (hàng trên): viền theo màu bộ điều khiển. Tag `4K CORE` nếu Trạng thái = Bật, `OFFLINE` nếu Tắt (theo cấu hình, không phải kết nối thật). Mỗi thẻ có 8 ô cổng `Mã-P1` … `Mã-P8`. Cổng đã gán → tô nền màu bộ điều khiển; cổng đang chọn → làm nổi bật.
2. **VIDEO WALL GRID MAP**: luôn vẽ tối thiểu **8 × 4** ô. Nếu có màn ở vị trí vượt khung thì lưới tự mở rộng.
   - Ô có màn: Mã màn + nhãn cổng (`MãBĐK-Pn`, chưa gán → `—`). Viền dưới theo màu bộ điều khiển. Màn ở trạng thái Tắt → hiển thị mờ.
   - Ô không có màn: *Chưa cấu hình* + vị trí `R{hàng}C{cột}`, không bấm được.
3. **Thanh thông tin** (dưới cùng, hiện khi có lựa chọn).


### 6.6 Kịch bản hiển thị (Scene Editor)

**Mục đích:** Thiết kế bố cục cửa sổ cho tường. **Chọn kịch bản ở đây chỉ để chỉnh sửa, không đổi kịch bản đang phát trên tường.**

**Bố cục 3 cột**

| Vùng | Nội dung |
|---|---|
| Header | Tiêu đề *Quản lý kịch bản* + nút **+ Thêm** (quyền `videoWallScene:add`) |
| Trái – Danh sách kịch bản | Ô tìm kiếm (theo tên **hoặc** mã, không phân biệt hoa thường). Thẻ kịch bản: tên, tag *Mặc định*, thời gian sửa gần nhất dạng tương đối (x phút/giờ/ngày trước; ≥ 30 ngày → ngày dd/mm/yyyy). Menu "…": thông tin audit · **Sao chép** · **Đặt mặc định** · **Xoá**. Không có kịch bản → *Không có kịch bản* |
| Giữa – Canvas | Lưới tường 8 × 4, các ô đánh nhãn `SCR-01` … `SCR-32` (theo hàng). Cửa sổ vẽ theo toạ độ, tô màu theo **loại nguồn**, có icon + nhãn. Chưa chọn kịch bản → gợi ý *Chọn một kịch bản* |
| Giữa – Thuộc tính cửa sổ | Hiện khi chọn 1 cửa sổ: nguồn (tên, loại) · X · Y · W · H · Z-index · công tắc **Khoá** · nút **Xoá cửa sổ** |
| Giữa – Thông tin kịch bản | Mã (bắt buộc) · Tên (bắt buộc) · Ghi chú (đếm ký tự) · nút **Huỷ** · **Lưu** |
| Phải – Bảng nguồn | Nguồn đang **Bật**, nhóm theo loại (bỏ nhóm rỗng và bỏ *Liên kết controller*). Mỗi nguồn: chấm màu, tên, dòng phụ (cục bộ: `Loại tín hiệu · Bộ điều khiển`; Camera IP: loại tín hiệu hoặc `RTSP`). Kéo được |

**Thao tác trên canvas**

| Thao tác | Hành vi |
|---|---|
| Kéo nguồn từ bảng phải thả vào canvas | Tạo cửa sổ **1 ô (3840 × 2160)** tại ô thả (làm tròn xuống theo ô, ép nằm trong tường). Tên/nhãn = tên nguồn. Z-index = lớn nhất + 1. Cửa sổ mới được chọn sẵn. Thả vào ô ngoài vùng được cấp → cảnh báo, không tạo |
| Bấm cửa sổ | Chọn cửa sổ. Bấm vùng trống → bỏ chọn |
| Kéo cửa sổ | Di chuyển, **snap theo ô** (bội số 3840 / 2160), không ra ngoài tường. Vị trí mới ngoài vùng được cấp → đứng ở vị trí hợp lệ gần nhất |
| 8 tay nắm resize (chỉ cửa sổ đang chọn, không khoá, trong vùng) | Đổi kích thước theo ô; tối thiểu 1 ô |
| Phím **Delete** | Xoá cửa sổ đang chọn (ngoài vùng → cảnh báo) |
| Công tắc Khoá | Cửa sổ khoá không kéo/resize được; có icon ổ khoá. ⚠️ Chỉ tồn tại trên giao diện, **không được lưu** (L-11) |
| Sửa số ở bảng thuộc tính | Bước nhảy X/W = 3840, Y/H = 2160. Cả vị trí cũ và mới phải trong vùng, nếu không → cảnh báo, không đổi |

Mọi thay đổi trên canvas/form đánh dấu kịch bản là **đã sửa (dirty)**. Nút **Lưu** chỉ bật khi dirty.

**Validation (chuẩn BE)**

*Kịch bản*

| Trường | Quy tắc | ⚠️ FE hiện tại |
|---|---|---|
| Mã | **Bắt buộc**, ≤ 64, không trùng | — |
| Tên | **Bắt buộc**, **≤ 64** | FE cho nhập **128** ký tự |
| Ghi chú | ≤ 256 | — |
| Ảnh thu nhỏ | ≤ 256 | (chưa có UI) |
| Trạng thái | Bắt buộc | Mặc định Bật, chưa có UI đổi |
| Số cột / hàng lưới | > 0 | Cố định 8 × 4 |
| Thứ tự | ≥ 0 | Mặc định 100 |
| Mặc định | Người không phải SuperAdmin tạo mới → luôn **không** mặc định. Sửa thông tin **không** đổi được cờ này | — |

*Cửa sổ*

| Trường | Quy tắc | ⚠️ FE hiện tại |
|---|---|---|
| Mã | **Bắt buộc**, ≤ 64, **duy nhất toàn hệ thống** | FE tự sinh `<Mã kịch bản>-W<n>` |
| Tên | **Bắt buộc**, ≤ 64 | FE lấy tên nguồn |
| Nhãn | ≤ 256 | — |
| X, Y | ≥ 0 | — |
| W, H | **> 0** | Ô số trong bảng thuộc tính: W ≥ 960, H ≥ 540 |
| Z-index | **≥ 0** | Ô số: 1..100 |
| Độ mờ, Thứ tự | ≥ 0 | — |
| Kịch bản, Nguồn | Nếu có thì phải tồn tại | FE bắt buộc có nguồn |
| Vị trí | Theo [mục 5](#5-phân-quyền-vùng-màn-hình-áp-dụng-xuyên-suốt) | — |

**User stories**

**US-SC-01 – Tạo kịch bản mới**
- [ ] Bấm **+ Thêm** (nếu đang có thay đổi chưa lưu → hỏi *Bỏ thay đổi?*) → canvas trống, Mã trống, Tên mặc định *Kịch bản mới*, trạng thái dirty.
- [ ] Kịch bản **chưa được ghi xuống BE** cho tới khi bấm Lưu.
- [ ] Bấm Huỷ ở kịch bản mới → (hỏi xác nhận nếu dirty) → bỏ hẳn, canvas về trạng thái chưa chọn.
- [ ] Tài khoản **Không có quyền** vùng → BE từ chối tạo.

**US-SC-02 – Lưu kịch bản**
- [ ] Kiểm tra trước khi lưu: thiếu Mã → *Nhập mã kịch bản*; thiếu Tên → *Nhập tên kịch bản*; có cửa sổ không có nguồn hoặc W/H ≤ 0 → *Cửa sổ thứ n không hợp lệ*. Có lỗi → không gửi gì.
- [ ] Thứ tự lưu: (1) Thêm/Cập nhật kịch bản → (2) Cập nhật cửa sổ cũ còn giữ, thêm cửa sổ mới, xoá cửa sổ đã bỏ → (3) Nạp lại để lấy ID thật.
- [ ] Kịch bản cũ chỉ gọi cập nhật thông tin **khi Mã/Tên/Ghi chú/lưới/trạng thái thực sự đổi**. Nhờ vậy người dùng chỉ có quyền một phần vùng vẫn lưu được thay đổi cửa sổ của mình.
- [ ] Cửa sổ ngoài vùng được cấp **không được gửi** cập nhật (giữ nguyên).
- [ ] Thứ tự (orderNo) cửa sổ được đánh lại 1..n theo thứ tự hiện tại.
- [ ] Thành công → *Lưu thành công*, bỏ dirty, bỏ chọn cửa sổ. Lỗi → *Lưu thất bại*.
- [ ] ⚠️ Lưu **không nguyên tử**: lỗi giữa chừng có thể để lại trạng thái lưu dở (L-13).
- [ ] Mỗi lần thêm/sửa/xoá cửa sổ, BE gửi lệnh *đồng bộ cửa sổ* xuống Worker (không có phản hồi trên UI).

**US-SC-03 – Chuyển kịch bản khi đang sửa**
- [ ] Đang dirty, chọn kịch bản khác → hỏi *Bỏ thay đổi?*. Huỷ → ở lại; Đồng ý → nạp kịch bản mới.
- [ ] Bấm **Huỷ** ở form khi dirty → hỏi (*Bỏ* / *Tiếp tục sửa*) → Bỏ: nạp lại dữ liệu gốc.

**US-SC-04 – Kịch bản phủ ngoài vùng được cấp**
- [ ] Người dùng có quyền theo vùng mở kịch bản có cửa sổ ngoài vùng → form thông tin **khoá** Mã/Tên/Ghi chú, hiển thị cảnh báo có biểu tượng ổ khoá. Vẫn sửa và lưu được cửa sổ **trong** vùng.
- [ ] Ô ngoài vùng làm mờ; cửa sổ ngoài vùng chọn được để xem nhưng **không** kéo/resize/xoá được.

**US-SC-05 – Sao chép kịch bản**
- [ ] Nếu có cửa sổ ngoài vùng (hoặc user không có quyền) → cảnh báo, **chặn toàn bộ** (không sao chép một phần).
- [ ] Hộp nhập Mã mới, mặc định `<Mã cũ>-COPY`, không được để trống.
- [ ] Kết quả: kịch bản mới tên `<Tên cũ> (copy)`, **không** mặc định, kèm bản sao toàn bộ cửa sổ (mã cửa sổ theo mã mới). Sau đó tự mở kịch bản mới.
- [ ] Lỗi → *Sao chép thất bại*.

**US-SC-06 – Xoá kịch bản**
- [ ] Xác nhận → nếu có cửa sổ ngoài vùng → cảnh báo, không xoá.
- [ ] Xoá cửa sổ trước, sau đó xoá kịch bản. Nếu đang mở kịch bản đó → canvas về trạng thái trống.

**US-SC-07 – Đặt kịch bản mặc định**
- [ ] Mục *Đặt mặc định* **chỉ hiện với SuperAdmin** (và có quyền `videoWallScene:update`); bị mờ nếu kịch bản đã là mặc định.
- [ ] Sau khi đặt: kịch bản này có tag *Mặc định*, **mọi kịch bản khác mất cờ** (tại mọi thời điểm chỉ có 1 kịch bản mặc định).
- [ ] Người không phải SuperAdmin gọi được API → BE từ chối.

---

### 6.7 Giám sát tường (Monitor)

**Mục đích:** Phòng điều hành xem bố cục đang chiếu và **đổi kịch bản đang phát** cho cả tường. Canvas ở màn này **chỉ xem** (không kéo, sửa, xoá cửa sổ). Muốn sửa bố cục thì vào màn Kịch bản.

**Bố cục**
- **Thanh công cụ trái:** ô chọn kịch bản (chỉ kịch bản **Bật**) · **Áp dụng** (quyền `videoWallMonitor:apply`) · **Reset**.
- **Thanh công cụ phải:** menu *Màn hình* (Bật tất cả / Tắt tất cả) · nút **Toàn màn hình**.
- **Canvas chỉ xem:** như màn Kịch bản, ô ngoài vùng được cấp bị làm mờ.
- **Thanh trạng thái:** `Scene: <tên>` · `Màn bật: x/y` (đếm theo **Trạng thái cấu hình** của màn, không phải trạng thái kết nối) · nếu đang xem trước: `● Xem trước, chưa áp dụng (Đang phát: <tên>)`.

**Khái niệm cần nắm:** *kịch bản đang hiển thị trên canvas* có thể khác *kịch bản đang phát*. Chúng khác nhau khi người dùng đang **xem trước**.

**User stories**

**US-MN-01 – Mở trang**
- [ ] Kịch bản hiển thị ban đầu, theo ưu tiên: kịch bản **BE đang phát** → kịch bản **Mặc định** → kịch bản Bật **đầu tiên**.
- [ ] Nếu lấy từ BE đang phát → coi là *đang phát*, nút Áp dụng **tắt**. Nếu BE chưa phát kịch bản nào (lấy Mặc định/đầu tiên) → đó chỉ là **xem trước**, nút Áp dụng **bật** để người dùng chủ động đẩy xuống tường.
- [ ] Kịch bản BE đang phát mà hiện đang Tắt vẫn được bổ sung vào ô chọn.
- [ ] Nạp lỗi → *Không tải được dữ liệu giám sát*.

**US-MN-02 – Xem trước và Áp dụng**
- [ ] Chọn kịch bản khác ở ô chọn → canvas đổi, **không gọi API kích hoạt**. Thanh trạng thái hiện dấu ● xem trước. Nút Áp dụng và Reset bật.
- [ ] Bấm **Áp dụng**:
  - Người dùng không toàn quyền và có cửa sổ ngoài vùng (hoặc không có quyền) → cảnh báo, dừng.
  - Hiện hộp xác nhận nêu tên kịch bản. **Huỷ → không làm gì**, nút không bị treo trạng thái quay.
  - Đồng ý → nút chuyển trạng thái đang xử lý, ô chọn bị khoá → gọi kích hoạt **theo Mã kịch bản** → *Đã áp dụng lên tường* → trở thành *đang phát*, dấu ● biến mất.
  - Lỗi → *Áp dụng thất bại*.
- [ ] Bấm **Reset** → quay về kịch bản đang phát → thông báo *Đã quay về kịch bản đang phát*.


**Quy tắc BE khi kích hoạt (áp dụng cho Giám sát, Chạy thử lịch, Chạy thử ITS)**

| Bước | Quy tắc |
|---|---|
| Đầu vào | Phải có **Mã kịch bản** hoặc **Mã loại sự kiện** (≤ 64 ký tự) |
| Theo Mã kịch bản | Không tìm thấy (hoặc đã xoá) → lỗi *không tồn tại* |
| Theo loại sự kiện | Lấy các quy tắc **đang Bật** của loại sự kiện đó → chọn theo Ưu tiên `CRITICAL > HIGH > NORMAL > LOW > khác`; cùng mức → quy tắc **tạo sớm hơn**. Không có quy tắc → lỗi *không tìm thấy quy tắc*; quy tắc không có kịch bản đích → lỗi |
| Kịch bản **Tắt** | Ghi nhật ký **thất bại** + lỗi *kịch bản đang tắt* |
| Không có bộ điều khiển nào | Ghi nhật ký thất bại + lỗi *chưa cấu hình bộ trung tâm* (chỉ khi kịch bản có Output ID) |
| Thành công | Gửi lệnh xuống Worker (không chờ), ghi nhật ký **thành công**, trả kết quả ngay |
| Lấy kịch bản đang phát | Kịch bản của bộ điều khiển được kích hoạt **gần nhất**; không có thì kịch bản mang cờ "đang kích hoạt"; không có nữa → lỗi *chưa có kịch bản đang phát* (Giám sát xử lý êm, không hiện lỗi) |

---

### 6.8 Lập lịch (Schedule)

**Mục đích:** Tự động thực hiện hành động (chủ yếu là chuyển kịch bản) theo giờ.

**Bố cục danh sách**
- Tìm kiếm: Tên · Kịch bản đích (chỉ kịch bản Bật) · Trạng thái.
- Cột: Mã · Tên · **Loại lịch** (Hàng ngày = xanh lá, Hàng tuần = xanh dương, Một lần = vàng, Cron = xám) · **Thời gian** · **Hành động** · Trạng thái (**công tắc ngay trên bảng**) · **Lần chạy kế tiếp** · Lần chạy gần nhất · Audit · Thao tác (**Chạy thử**, Sao chép, Sửa, Xoá).
- Sắp xếp mặc định: ngày tạo giảm dần.
- Hiển thị Thời gian: Hàng ngày → *Hàng ngày lúc HH:mm*; Hàng tuần → `HH:mm (T2, T3…)`; Một lần → `YYYY-MM-DD HH:mm`; Cron → biểu thức.
- Hành động *Chuyển kịch bản* hiển thị `Kịch bản: <tên đậm>`.
- Lịch Tắt → cột Lần chạy kế tiếp hiển thị *Đã tắt* (chữ mờ).

**Hộp thoại Thêm/Sửa**

| Trường | Hiển thị khi | Ghi chú |
|---|---|---|
| Mã, Tên | Luôn | — |
| Loại lịch (radio) | Luôn | Hàng ngày · Hàng tuần · Một lần · Cron |
| Ngày trong tuần (checkbox T2…CN) | Hàng tuần | Phải chọn ≥ 1 ngày (FE) |
| Giờ chạy (HH:mm) | Hàng ngày, Hàng tuần | — |
| Ngày giờ chạy (YYYY-MM-DD HH:mm) | Một lần | — |
| Biểu thức Cron | Cron | Gợi ý `0 6 * * 1-5` |
| Hành động (radio) | Luôn | **Chuyển kịch bản** · **Tắt màn hình** · **Bật màn hình** |
| Kịch bản đích | Hành động = Chuyển kịch bản | Chỉ kịch bản Bật, nhãn `Mã — Tên` |
| Trạng thái, Ghi chú | Luôn | — |

Mặc định khi Thêm: Bật, Hàng ngày, `06:00`, T2–T6, Chuyển kịch bản.

**Hành vi khi lưu (FE)**
- Chỉ giữ trường ứng với loại lịch đang chọn; các trường còn lại về rỗng. Đổi hành động khác *Chuyển kịch bản* → xoá kịch bản đích.
- **Lần chạy kế tiếp** do FE tự tính khi lịch Bật: Hàng ngày → lần gần nhất *sau thời điểm hiện tại*; Hàng tuần → ngày gần nhất trong các ngày đã chọn. **Một lần / Cron → để trống**. Lịch Tắt → để trống.
- Sao chép/Thêm: *Lần chạy gần nhất* để trống.

**Validation (chuẩn BE)**

| Trường | Quy tắc | ⚠️ FE hiện tại |
|---|---|---|
| Mã | **Bắt buộc**, ≤ 64, không trùng | — |
| Tên | **Bắt buộc**, ≤ 128 | — |
| Giờ chạy, Ngày trong tuần | ≤ 128 | FE bắt buộc theo loại |
| Ngày giờ một lần, Cron, Hành động | ≤ 64 | FE bắt buộc theo loại |
| Ghi chú | ≤ 256 | — |
| Trạng thái | Bắt buộc | — |
| Loại lịch | ≥ 0 (nếu có) | FE bắt buộc, chỉ 1..4 |
| Kịch bản đích | Nếu có thì phải tồn tại; phải nằm trong vùng được cấp (mục 5) | FE bắt buộc khi hành động = Chuyển kịch bản |
| Ràng buộc theo loại lịch / hành động | **BE không kiểm tra**; BE cũng không kiểm cú pháp Cron | (L-14) |

---

### 6.9 Tích hợp ITS – quy tắc sự kiện

**Mục đích:** Khi hệ thống ITS phát sinh sự kiện (tai nạn, ùn tắc, thời tiết…), tường **tự chuyển** sang kịch bản phù hợp.

**Bố cục danh sách**
- Tìm kiếm: Loại sự kiện (danh mục loại sự kiện TMS) · Nguồn sự kiện · Ưu tiên · Trạng thái.
- Cột: Mã · **Loại sự kiện** (tag màu theo mã: ACCIDENT/EMERGENCY = đỏ, CONGESTION = vàng, WEATHER_ALERT = xám, khác = xanh) · Nguồn sự kiện · Ưu tiên (tag) · Kịch bản đích · Ghi chú · Trạng thái (công tắc) · Audit · Thao tác (Chạy thử, Sao chép, Sửa, Xoá).
- Sắp xếp mặc định: Ưu tiên tăng dần **theo chữ cái** (⚠️ L-15).
- Nạp danh mục loại sự kiện lỗi → cột hiển thị ID thô.

**Hộp thoại Thêm/Sửa:** Mã · Loại sự kiện · Nguồn sự kiện · Ưu tiên · Kịch bản đích (chỉ kịch bản Bật) · Trạng thái · Ghi chú.
Mặc định: Bật, Nguồn = `ITS_EVENT`, Ưu tiên = `NORMAL`.

**Validation (chuẩn BE)**

| Trường | Quy tắc | ⚠️ FE hiện tại |
|---|---|---|
| Mã | **Bắt buộc**, ≤ 64, không trùng | — |
| Loại sự kiện | BE không ràng buộc | FE bắt buộc |
| Nguồn sự kiện | ≤ 128 | FE bắt buộc |
| Ưu tiên | ≤ 64 | FE bắt buộc |
| Kịch bản đích | Nếu có thì phải tồn tại; nằm trong vùng được cấp | FE bắt buộc |
| Ghi chú | ≤ 256 | — |
| Trạng thái | Bắt buộc | — |
| Trùng (Loại sự kiện + Ưu tiên) | **BE không chặn** | Khi trùng, BE chọn quy tắc **tạo sớm hơn** |

**User stories**

**US-IT-01 – Bật/tắt quy tắc trên bảng:** giống US-SH-01 (lỗi → công tắc trả về trạng thái cũ).

**US-IT-02 – Chạy thử (mô phỏng sự kiện)**
- [ ] Quy tắc đang **Tắt** → cảnh báo *Quy tắc đang tắt, không chạy thử được*.
- [ ] Xác nhận → gửi kích hoạt **theo Loại sự kiện** (không gửi mã kịch bản) → *Đã gửi sự kiện X*.
- [ ] ⚠️ BE tự chọn quy tắc theo ưu tiên trong **tất cả** quy tắc Bật cùng loại sự kiện. Kịch bản được phát **có thể không phải** kịch bản của dòng vừa bấm (L-16).
- [ ] Mỗi lần kích hoạt đều ghi nhật ký (thành công/thất bại) ở BE.
- [ ] Màn Giám sát đang mở phải chuyển sang kịch bản BE chọn.

**US-IT-03** – Tạo/sửa/xoá quy tắc có kịch bản đích ngoài vùng → BE từ chối.

---

### 6.10 Phân quyền vùng màn hình (Wall Permission)

**Mục đích:** Cấp cho **một người dùng** hoặc **một đơn vị** quyền thao tác trên một tập ô của tường (xem quy tắc ở [mục 5](#5-phân-quyền-vùng-màn-hình-áp-dụng-xuyên-suốt)).

**Bố cục danh sách**
- Tìm kiếm: *Áp dụng cho* (Người dùng / Đơn vị) → tương ứng ô chọn Người dùng (nhãn `Họ tên (username)`) hoặc cây Đơn vị. Đổi loại → xoá giá trị lọc của loại kia. Đặt lại → về *Người dùng*.
- Cột: **Loại** (tag Người dùng / Đơn vị) · **Đối tượng** (tên user/đơn vị, không tra được → ID) · **Vùng được cấp** (lưới thu nhỏ, ô được cấp tô màu; toàn quyền → tô kín) · **Số màn** (`đã cấp/tổng`, toàn quyền → `tổng/tổng`, cấu hình lỗi → *Lỗi cấu hình*) · Audit · Thao tác.
- Kích thước lưới = (cột lớn nhất + 1) × (hàng lớn nhất + 1) của các màn đã khai báo. Chưa có màn → 8 × 4.

**Hộp thoại Thêm/Sửa (rộng 920px)**
- *Áp dụng cho* (radio Người dùng / Đơn vị). Đổi → xoá đối tượng của loại kia.
- **Toàn quyền** (checkbox) → lưới bị vô hiệu, đếm hiển thị *Toàn quyền*.
- Người dùng **hoặc** Đơn vị (bắt buộc theo loại đã chọn) · Mô tả (≤ 256).
- **Lưới chọn ô:** tiêu đề cột `C1…`, tiêu đề hàng `H1…`; mỗi ô hiển thị tên/mã màn tại vị trí đó (không có → `—`), viền trên theo màu bộ điều khiển, tooltip `Cột x, Hàng y — tên màn (mã BĐK)`. Chú thích màu chỉ liệt kê bộ điều khiển có nối màn.
- Đếm `Đã chọn n/tổng` · nút **Chọn tất cả** · **Bỏ chọn**.

**Thao tác trên lưới chọn ô**

| Thao tác | Hành vi |
|---|---|
| Bấm 1 ô | Đảo trạng thái chọn của ô đó |
| Nhấn giữ chuột và kéo | Chọn **khối chữ nhật** từ ô bắt đầu tới ô hiện tại, có xem trước màu. Ô bắt đầu **chưa chọn** → cả khối được **thêm**; ô bắt đầu **đã chọn** → cả khối bị **bỏ** |
| Nhấn **Esc** khi đang kéo | Huỷ lượt kéo, **không** đóng hộp thoại |
| Bấm tiêu đề cột/hàng | Nếu cả cột/hàng đã chọn → bỏ hết; ngược lại → chọn hết |
| Đang bật Toàn quyền | Mọi thao tác chọn ô bị vô hiệu |

**Validation (chuẩn BE)**

| Quy tắc | Chi tiết |
|---|---|
| Đối tượng | Phải có **đúng một** trong hai: Người dùng **hoặc** Đơn vị. Thiếu cả hai → lỗi; có cả hai → lỗi |
| Duy nhất | Mỗi Người dùng chỉ có **1** bản ghi; mỗi Đơn vị chỉ có **1** bản ghi (loại Đơn vị). Trùng → *Phân quyền đã tồn tại* |
| Không toàn quyền | Danh sách ô **bắt buộc**, ≥ 1 ô, không trùng ô, mỗi ô nằm trong **Cột 0..7, Hàng 0..3** |
| Toàn quyền | Danh sách ô không bắt buộc (FE gửi rỗng) |
| Mô tả/Ghi chú | ≤ 256 |
| Độ dài ID | Người dùng ≤ độ dài khoá; Đơn vị ≤ 64 |

## 7. Luồng nghiệp vụ đầu-cuối gợi ý để test

**E2E-01 – Dựng tường từ đầu**
1. Bộ điều khiển: tạo 1 bộ trung tâm `CTR-00`, 4 bộ con `CTR-01..04`, mỗi bộ con chọn cha = `CTR-00`, vùng phủ 2 cột × 4 hàng.
2. Màn hình đầu ra: tạo 32 màn (Cột 0..7, Hàng 0..3).
3. Sơ đồ đấu nối: gán `CTR-01-P1..P8` cho 8 màn cột 1–2…, kiểm tra màu/nhãn.
4. Nguồn: 2 camera IP (dùng chung) + 2 tín hiệu cục bộ trên `CTR-01` cổng vào 1, 2.
5. Dashboard: tổng 32 màn, 5 bộ điều khiển.

**E2E-02 – Thiết kế và phát kịch bản**
1. Kịch bản: tạo `SC-NORMAL`, kéo 4 nguồn, resize 1 cửa sổ chiếm 2×2 ô, lưu.
2. Mở Giám sát ở **2 trình duyệt** (A, B). A chọn `SC-NORMAL` → Áp dụng → B tự chuyển.
3. B đang xem trước kịch bản khác khi A áp dụng → B **không** bị đổi canvas, chỉ đổi dòng "Đang phát".

**E2E-03 – Tự động hoá**
1. Lập lịch Hàng ngày → kiểm tra Lần chạy kế tiếp; Chạy thử → Giám sát đổi.
2. Tạo 2 quy tắc cùng loại sự kiện `ACCIDENT`: HIGH → `SC-A`, CRITICAL → `SC-B`. Chạy thử dòng HIGH → kỳ vọng phát `SC-B`. Tắt quy tắc CRITICAL → chạy lại → phát `SC-A`.
3. Tắt kịch bản đích rồi chạy thử → lỗi kịch bản đang tắt.

**E2E-04 – Phân quyền vùng**
1. Cấp user `op1` cột 1–2 (16 ô). Đăng nhập `op1`.
2. Kịch bản: kéo nguồn vào cột 5 → bị chặn; vào cột 1 → được; resize lấn sang cột 3 → không lấn được.
3. Mở kịch bản phủ cả tường → thông tin bị khoá, chỉ sửa được cửa sổ cột 1–2, lưu được.
4. Sao chép/Xoá kịch bản phủ cả tường → bị chặn. Đặt mặc định → không thấy mục.
5. Lập lịch / ITS với kịch bản đích phủ cả tường → BE từ chối.
6. Giám sát: áp dụng kịch bản phủ cả tường → FE chặn (đồng thời kiểm tra L-01 bằng API).
7. Xoá bản ghi phân quyền của `op1` (không có bản ghi đơn vị) → mọi thao tác ghi bị chặn, ô lưới đều mờ.

