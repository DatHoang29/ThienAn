# Clone ShareDataWorker thành nhiều dịch vụ

Một bộ mã `ShareDataWorker` chạy thành nhiều dịch vụ Windows dùng chung CSDL. Mỗi dịch vụ là một bản sao thư mục publish, chỉ khác nhau ở `appsettings.json`:

- `ShareDataWorker`: dịch vụ chạy cả gửi lẫn nhận, như trước giờ.
- `ShareDataWorker_Inbound_<số>`: dịch vụ chỉ nhận.
- `ShareDataWorker_Outbound_<số>`: dịch vụ chỉ gửi.

Các dịch vụ gửi tự chia nhau gửi cho mọi đối tác; khóa đăng ký trên CSDL bảo đảm mỗi lượt chỉ một dịch vụ gửi nên không gửi trùng. Mỗi gói nhận về cũng chỉ một dịch vụ ghi.

Clone chỉ đổi cách chạy dịch vụ. Trên web, người dùng vẫn tạo đối tác, gói tin, đăng ký như bình thường; không có cấu hình nào theo từng dịch vụ.

Ví dụ dưới đây dùng 6 dịch vụ đặt ở `D:\ShareData\Builds`: 1 dịch vụ hai chiều, 1 dịch vụ chỉ nhận, 4 dịch vụ chỉ gửi. Đổi đường dẫn theo máy chủ thực tế.

## 1. Publish

Chạy trên máy dev, tại thư mục gốc WEBAPI:

```bash
dotnet publish src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj -c Release -r win-x64 --self-contained false -o publish/ShareDataWorker
```

Máy chủ cần có ASP.NET Core Runtime 10 (máy đang chạy WebAPI thì đã có).

## 2. Clone thư mục

Chép thư mục publish thành 6 thư mục, tên thư mục trùng tên dịch vụ:

```text
D:\ShareData\Builds\ShareDataWorker
D:\ShareData\Builds\ShareDataWorker_Inbound_1
D:\ShareData\Builds\ShareDataWorker_Outbound_1
D:\ShareData\Builds\ShareDataWorker_Outbound_2
D:\ShareData\Builds\ShareDataWorker_Outbound_3
D:\ShareData\Builds\ShareDataWorker_Outbound_4
```

## 3. Sửa `appsettings.json` trong từng thư mục

Bốn khóa khác nhau ở từng bản; bản `ShareDataWorker` giữ nguyên bốn khóa này như bản publish.

| Thư mục | `ShareData:Role` | `ShareData:ServiceName` | `Nats:ClientName` | `Heartbeat:Subject` |
|---|---|---|---|---|
| `ShareDataWorker` | `Both` | `ShareDataWorker` | `ITS_SHAREDATA_WORKER` | `ta.its.heartbeat.sharedata` |
| `ShareDataWorker_Inbound_1` | `ReceiveOnly` | `ShareDataWorker_Inbound_1` | `ITS_SHAREDATA_WORKER_INBOUND_1` | `ta.its.heartbeat.sharedata_inbound_1` |
| `ShareDataWorker_Outbound_1` | `SendOnly` | `ShareDataWorker_Outbound_1` | `ITS_SHAREDATA_WORKER_OUTBOUND_1` | `ta.its.heartbeat.sharedata_outbound_1` |
| `ShareDataWorker_Outbound_2` | `SendOnly` | `ShareDataWorker_Outbound_2` | `ITS_SHAREDATA_WORKER_OUTBOUND_2` | `ta.its.heartbeat.sharedata_outbound_2` |
| `ShareDataWorker_Outbound_3` | `SendOnly` | `ShareDataWorker_Outbound_3` | `ITS_SHAREDATA_WORKER_OUTBOUND_3` | `ta.its.heartbeat.sharedata_outbound_3` |
| `ShareDataWorker_Outbound_4` | `SendOnly` | `ShareDataWorker_Outbound_4` | `ITS_SHAREDATA_WORKER_OUTBOUND_4` | `ta.its.heartbeat.sharedata_outbound_4` |

- `ShareData:Role`: vai trò của bản đó. Để `Both` hoặc không khai thì bản đó chạy cả gửi lẫn nhận. Sai giá trị thì dịch vụ báo lỗi và dừng ngay lúc khởi động.
- `ShareData:ServiceName`: tên dịch vụ Windows của bản đó, trùng tên đăng ký ở bước 4.
- `Nats:ClientName`: tên kết nối NATS của dịch vụ, đặt riêng từng dịch vụ để phân biệt kết nối trên máy chủ NATS.
- `Heartbeat:Subject`: tín hiệu báo dịch vụ còn sống gửi lên trang **Giám sát dịch vụ**; mỗi dịch vụ một subject riêng thì trang hiện riêng từng dịch vụ.

Các khóa còn lại giống nhau ở cả 6 bản:

| Khóa | Giá trị |
|---|---|
| `ConnectionStrings:DefaultConnection` | CSDL chính |
| `ConnectionStrings:InboundConnection` | CSDL nhận, phải trùng với CSDL nhận của WebAPI |
| `Nats:Url` | Máy chủ NATS. Dịch vụ chạy trên máy chủ thì dùng địa chỉ NATS trong mạng nội bộ; địa chỉ công khai có thể không nối được từ mạng nội bộ. Các khóa `Nats` còn lại và tệp xác thực `Configuration/nats.creds` giữ nguyên như bản publish |
| `FileStorage:BasePath` | Thư mục lưu tệp đã gửi. Để mặc định `sharedata/send` thì mỗi dịch vụ lưu trong thư mục của chính nó, ví dụ `D:\ShareData\Builds\ShareDataWorker_Outbound_1\sharedata\send`. Muốn gom một chỗ thì đặt cùng một đường dẫn tuyệt đối, ví dụ `D:\ShareData\send` |

Dịch vụ chạy trước khi NATS sẵn sàng cũng không phải khởi động lại: nối được NATS là tự phát heartbeat và nghe tín hiệu có dữ liệu mới.

## 4. Đăng ký dịch vụ (một lần)

Mở PowerShell bằng quyền Administrator. Dùng `sc.exe`, không dùng `sc`; sau dấu `=` phải có một khoảng trắng.

```powershell
sc.exe create ShareDataWorker binPath= "D:\ShareData\Builds\ShareDataWorker\ShareDataWorker.exe" start= auto
sc.exe create ShareDataWorker_Inbound_1 binPath= "D:\ShareData\Builds\ShareDataWorker_Inbound_1\ShareDataWorker.exe" start= auto
sc.exe create ShareDataWorker_Outbound_1 binPath= "D:\ShareData\Builds\ShareDataWorker_Outbound_1\ShareDataWorker.exe" start= auto
sc.exe create ShareDataWorker_Outbound_2 binPath= "D:\ShareData\Builds\ShareDataWorker_Outbound_2\ShareDataWorker.exe" start= auto
sc.exe create ShareDataWorker_Outbound_3 binPath= "D:\ShareData\Builds\ShareDataWorker_Outbound_3\ShareDataWorker.exe" start= auto
sc.exe create ShareDataWorker_Outbound_4 binPath= "D:\ShareData\Builds\ShareDataWorker_Outbound_4\ShareDataWorker.exe" start= auto
```

Máy chủ đã có sẵn dịch vụ `ShareDataWorker` thì không tạo lại dòng đầu, chỉ dừng dịch vụ rồi trỏ sang thư mục mới:

```powershell
net stop ShareDataWorker
sc.exe config ShareDataWorker binPath= "D:\ShareData\Builds\ShareDataWorker\ShareDataWorker.exe"
```

## 5. Chạy và dừng

```powershell
net start ShareDataWorker
net start ShareDataWorker_Inbound_1
net start ShareDataWorker_Outbound_1
net start ShareDataWorker_Outbound_2
net start ShareDataWorker_Outbound_3
net start ShareDataWorker_Outbound_4
```

Dừng thì thay `net start` bằng `net stop`.

## 6. Kiểm tra

- [ ] `sc.exe query ShareDataWorker_Outbound_1` (tương tự các dịch vụ còn lại) báo `RUNNING`, hoặc xem trong `services.msc`. Dịch vụ không lên thì xem lỗi ở Event Viewer → Windows Logs → Application.
- [ ] Trang **Giám sát dịch vụ** có 6 thẻ ShareData ở trạng thái **Đang chạy**. Thẻ **Dịch vụ chia sẻ dữ liệu (ShareData)** có sẵn là của `ShareDataWorker`. Năm dịch vụ còn lại phải thêm thẻ lần đầu: bật **Chế độ quản lý**, ở dòng **Có heartbeat từ dịch vụ chưa cấu hình (bấm để thêm):** bấm lần lượt 5 thẻ `+ ta.its.heartbeat.sharedata_…`, mỗi thẻ mở hộp thoại đã điền sẵn, bấm **Xác nhận**. Danh sách thẻ lưu theo trình duyệt, máy khác mở trang phải thêm lại. Thẻ **Đang chạy** nghĩa là dịch vụ đó đã nối được NATS.
- [ ] Tệp đã gửi của từng dịch vụ nằm trong `sharedata\send` của chính thư mục dịch vụ đó.
- [ ] **Lịch sử chia sẻ → Nhật ký truyền nhận** có lượt gửi thành công, **Cảnh báo & lỗi** không có cảnh báo mới.
- [ ] Dừng rồi khởi động lại một dịch vụ gửi: dịch vụ gửi tiếp từ mốc, không gửi lại từ đầu.
- [ ] Theo dõi máy trong lúc chạy: CPU, RAM của các dịch vụ và SQL Server (Task Manager), khóa chờ trên CSDL; ghi lại số đo.

## Lưu ý khi có hai dịch vụ cùng nhận

`ShareDataWorker` và `ShareDataWorker_Inbound_1` chạy cùng lúc là hai dịch vụ cùng nhận. Mỗi gói vẫn chỉ được ghi một lần, nhưng các gói của cùng một đối tác có thể được ghi không theo đúng thứ tự gửi. Cần đúng thứ tự thì chỉ chạy một trong hai dịch vụ này.

## Thêm dịch vụ gửi

Lặp lại bước 2 đến 5 với thư mục tiếp theo (`ShareDataWorker_Outbound_5`…) và bộ tên tương ứng. Thêm đối tác thì không cần thêm dịch vụ, các dịch vụ gửi tự chia nhau gửi.

## Nâng cấp

Dừng các dịch vụ, chép bản publish mới vào từng thư mục **nhưng không ghi đè `appsettings.json`**, rồi khởi động lại. `appsettings.json` còn khóa `NasStorage` thì đổi tên khóa thành `FileStorage`, nếu không dịch vụ lưu tệp về thư mục mặc định.

## Gỡ

```powershell
net stop ShareDataWorker_Outbound_1
sc.exe delete ShareDataWorker_Outbound_1
```
