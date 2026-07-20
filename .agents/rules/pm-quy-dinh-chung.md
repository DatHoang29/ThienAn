---
trigger: model_decision
description: Áp dụng khi viết code, viết comment (XML Documentation), đặt tên biến/hàm/lớp, và thực hiện commit source code (GIT/SVN) theo Quy định chung của Phòng Phần mềm.
---

# Quy Định Chung Phòng Phần Mềm (P.PM Quy Định Chung)

> Tài liệu này được biên soạn dựa trên `P.PM Quy Dinh Chung.md` nhằm hướng dẫn và chuẩn hóa cách viết comment trong source code, cách đặt tên biến/hàm/lớp và định dạng thông điệp commit mã nguồn.

---

## 1. Quy định về Comment trong Source Code (XML Documentation)

### 1.1. Bản Quyền & Đầu File (Copyright Header)
Tất cả các file source code mới tạo cần phải có comment bản quyền ở đầu file:
```csharp
/*
* Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
* Website: http://tacorp.vn
*/
```

### 1.2. Comment của Lớp (Class Summary Header)
Tất cả các định nghĩa Class phải có mô tả XML Summary:
```csharp
/// <summary>
/// Description: [Mô tả chi tiết mục đích và chức năng của lớp]
/// Author: [Tên viết tắt của lập trình viên, ví dụ: Anhlq]
/// Create Date: [dd/MM/yyyy, ví dụ: 31/08/2023]
/// Cr1: [Name/Initial] - [Date] - [Description] (Thông tin cập nhật/Change Request)
/// Cr2: [Name/Initial] - [Date] - [Description]
/// </summary>
```

### 1.3. Comment của Hàm/Phương thức (Method Summary Header)
Tất cả các phương thức/hàm (đặc biệt là public) cần được comment rõ ràng:
```csharp
/// <summary>
/// Description: [Mô tả chi tiết chức năng của hàm, ví dụ: Hàm tạo mảng byte Message để gửi xuống Client]
/// Author: [Tên viết tắt của lập trình viên]
/// Create Date: [dd/MM/yyyy]
/// Cr1: [Name/Initial] - [Date] - [Description]
/// Cr2: [Name/Initial] - [Date] - [Description]
/// </summary>
/// <param name="[Tên_tham_số_1]">[Mô tả tham số 1]</param>
/// <param name="[Tên_tham_số_2]">[Mô tả tham số 2]</param>
/// <returns>[Mô tả chi tiết kiểu/giá trị trả về]</returns>
```

### 1.4. Comment của Biến thành viên / Trường dữ liệu (Field/Property Summary)
Các biến private, public field hoặc property quan trọng trong Class cần được chú thích:
```csharp
/// <summary>
/// Description: [Mô tả ý nghĩa của biến/trường dữ liệu]
/// Author: [Tên viết tắt của lập trình viên]
/// Create Date: [dd/MM/yyyy]
/// </summary>
```

---

## 2. Quy định đặt tên Biến, Hàm và Lớp (Naming Conventions)

Để đảm bảo tính nhất quán và dễ bảo trì, hãy tuân thủ cách đặt tên sau:

- **Tên Lớp (Class Name)**: Bắt đầu bằng tiền tố `cls_` kết hợp viết hoa chữ cái đầu của mỗi từ (PascalCase).
  - *Ví dụ*: `cls_Utilities`, `cls_SocketHelper`.
- **Tên Hàm/Phương thức (Method/Function Name)**: Bắt đầu bằng tiền tố `fn_` kết hợp PascalCase.
  - *Ví dụ*: `fn_GetByteMessage`, `fn_ConnectServer`.
- **Tên Biến thành viên/Trường dữ liệu của Lớp (Member Field Name)**: Bắt đầu bằng tiền tố `v` kết hợp PascalCase.
  - *Ví dụ*: `vTimeToHandShake`, `vMaxRetryCount`.
- **Tên Biến cục bộ & Tham số (Variable & Parameter Name)**: Sử dụng phong cách Hungarian Notation viết thường kiểu dữ liệu, kết hợp với camelCase cho phần mô tả.
  - *Ví dụ*: `strType` (string), `strKey` (string), `data` (byte array), `intCount` (int).

---

## 3. Quy định khi Commit Source Code (Commit Message Guidelines)

Tất cả các commit lên hệ thống quản lý mã nguồn (GIT/SVN) phải tuân thủ cấu trúc 3 phần rõ ràng:

### Cấu trúc thông điệp commit:
```
1. [Mã Version / Sub Version / Change Request]
2. Content:
- [Tên module/project thay đổi]: [Nội dung chi tiết thay đổi, các Issue/CR xử lý]
3. Link refer:
[Đường dẫn tham khảo bổ sung, ví dụ: link SVN cũ, link Drive...]
```

### Ví dụ mẫu commit chuẩn:
```
1. Version 2.0 / CR009
2. Content:
- SC_TA-TLS: Thư mục gốc các gói của phân hệ soát vé trạm thu phí DT741
- Xử lý Issue ABC: Khắc phục lỗi không nhận diện loại vé mới.
- Xử lý cho Change request BCD: Tối ưu hóa thời gian xử lý byte message.
3. Link refer:
http://115.78.1.139:8090/svn/TAC_SourceCode/TTP_DT741_v2/DT741_TicketChecker_MTC
```

---

## 4. Quy định đồng bộ mã nguồn sau sửa đổi khẩn cấp (Hotfix Rule)
- Trong trường hợp sửa lỗi khẩn cấp tại công trường/khách hàng, lập trình viên được phép build trực tiếp từ máy cá nhân.
- **Bắt buộc**: Trong vòng **2 ngày làm việc** (không tính ngày lễ, thứ 7, chủ nhật) sau khi build, lập trình viên phải commit toàn bộ source code đã sửa đổi lên GIT/SVN để đảm bảo phiên bản chạy tại công trường và trên mã nguồn lưu trữ là hoàn toàn khớp nhau và đều là phiên bản mới nhất.
