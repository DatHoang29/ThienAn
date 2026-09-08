# ĐẶC TẢ YÊU CẦU — VIDEOWALL

> Gộp từ `Plan_19_08.md` (19/08) và `videowall-25-08.txt` (25/08) — đọc theo thứ tự thời gian để thấy yêu cầu tiến triển ra sao qua từng buổi.

19/08
====

> Nguồn: nội dung cuộc họp + file `KeHoachTiepTheo.txt` — tách riêng phần VideoWall từ `Plan_19_08.md` gốc.
> Phần Share Data cùng giai đoạn xem tại [`../ShareData/sharedata_plan.md`](../ShareData/sharedata_plan.md).
> Các mục đánh dấu **[Cần chốt]** là chỗ chưa có kết luận.
> Các mục đánh dấu **🆕** là bổ sung từ file kế hoạch mới.

---

## 0. Tổng quan & mốc thời gian

### Mốc thời gian

| Hạng mục | Nội dung | Thời điểm |
|---|---|---|
| VideoWall | Form/dịch vụ test luồng tích hợp thiết bị | Bắt đầu **tuần sau**, **2 ngày** test |
| VideoWall — TCP 🆕 | Đề xuất kiểm thử qua **TCP** với thiết bị | **Qua tuần** (đang xem xét) |
| Tối ưu / chuẩn hóa lại | Refactor, tối ưu | Tháng sau |

### Nguyên tắc chung

1. **Làm xong luồng trước, tối ưu sau.** Không cầu toàn ở giai đoạn này.
2. Giai đoạn 1 cho phép **cấu hình cứng** (file config: IP, endpoint, key…). Bảng cấu hình động làm sau.
3. Ưu tiên **có bản chạy được để treo chạy dài ngày**, hơn là bản đẹp mà chưa chạy.
4. Không có demo cuối tháng cho hạng mục này — trôi sang tháng sau.

---

## 1. VIDEOWALL

### 1.1. Hiện trạng

- Thiết bị có **hai màn hình** (một cái chưa xác định rõ chức năng).
- Bên đối tác **mới lắp lần đầu**, cấu hình còn nguyên bản → thuận lợi.

### 1.2. Yêu cầu cần làm

1. **Làm form / dịch vụ test luồng tích hợp với thiết bị** 🆕 — đây là hạng mục chính, không chỉ là khảo sát.
2. **Thử split màn hình** theo vị trí mong muốn.
3. Xác định **cơ chế điều khiển**: điều khiển layout bằng cách nào, giao thức gì.
4. Kiểm tra khả năng **tự liên kết lại** giữa các vùng hiển thị trong các trường hợp khác nhau.

### 1.3. Giao thức kết nối 🆕

- Đề xuất **kiểm thử qua TCP** với thiết bị — **qua tuần** mới triển khai, hiện đang **xem xét**.
- Cần xác định trước: cổng TCP, định dạng lệnh (chuỗi ASCII hay binary), có cần giữ kết nối thường trực (keep-alive) hay mở/đóng theo từng lệnh, cơ chế xác nhận phản hồi.

### 1.4. Kế hoạch

- Bắt đầu **tuần sau**, dành **2 ngày** test trực tiếp.
- Cần **estimate thời gian** cho cả VideoWall và Share Data.

### 1.5. [Cần chốt]

- Hãng / model của videowall controller.
- Tài liệu API / bộ lệnh TCP của thiết bị.
- Số layout preset cần hỗ trợ và cách lưu preset.
- VideoWall có cần lấy nguồn hiển thị từ VMS không, hay chỉ điều khiển layout.

---

## 2. THỨ TỰ ƯU TIÊN ĐỀ XUẤT

> Số thứ tự giữ nguyên theo bản gốc `Plan_19_08.md` để dễ đối chiếu khi họp — các mục còn lại (Share Data, Hạ tầng) nằm ở tài liệu ShareData.

| # | Việc | Ghi chú |
|---|---|---|
| 7 | VideoWall: form/dịch vụ test tích hợp thiết bị, split màn hình | Tuần sau, 2 ngày |
| 8 | VideoWall: kiểm thử TCP với thiết bị | Qua tuần |

---

## 3. DANH SÁCH CÂU HỎI CẦN CHỐT

**VideoWall**

9. Hãng / model controller và **tài liệu bộ lệnh TCP / API**?
10. Số lượng **preset layout** cần hỗ trợ và cách lưu?
11. VideoWall có tích hợp **nguồn hiển thị từ VMS** không?

25/08
====

> Nguồn: `videowall-25-08.txt` — ghi chú thô từ buổi trao đổi, giữ nguyên văn phong gốc (không diễn giải lại).

```
WPF
- AI gen dc
- C#
- Giao dien truc quan


project WPF + project BI

event delegate c# (output)

- dang nhap
- tham so controller/screen/....
- thong tin cong input/output
- cau hinh scene (them/xoa/sua/cau hinh vi tri)
- lay thong tin scene
- activate scene

- thoi gian hoat dong/bat/tat


tab chưa cac component 

url
username/password
.....
[Connect] 
thiet lap scene
[       ]     [Set]
[		]

Name [   ]  Vi tri thiet lap [     ]      [Set]


Log (kiểm tra xem có bảng log nào tận dụng được không không có thì tạo mới)
[ 10:00 -- thiet lap sene {}
  10:01  Response  OK
                  Fail {....}


					]


thiet lap scene

Cac output riêng rẻ ()
Cac output có chồng windows
```
