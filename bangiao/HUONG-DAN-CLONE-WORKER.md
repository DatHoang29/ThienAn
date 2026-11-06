# Clone ShareDataWorker thành nhiều dịch vụ

Mỗi đối tác một dịch vụ chỉ gửi, thêm đúng một dịch vụ chỉ nhận, tất cả dùng chung CSDL. Mỗi dịch vụ là một bản sao thư mục publish, chỉ khác nhau ở `appsettings.json`.

Ví dụ dưới đây dùng 4 đối tác `A101` đến `A104`, tổng cộng 5 dịch vụ. Đổi mã đối tác và đường dẫn `D:\` theo máy chủ thực tế.

## 1. Publish

Chạy trên máy dev, tại thư mục gốc WEBAPI:

```bash
dotnet publish src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj -c Release -r win-x64 --self-contained false -o publish/ShareDataWorker
```

Máy chủ cần có .NET 10 Runtime.

## 2. Clone thư mục

Chép thư mục publish lên máy chủ thành 5 thư mục:

```text
D:\ShareDataWorker-A101
D:\ShareDataWorker-A102
D:\ShareDataWorker-A103
D:\ShareDataWorker-A104
D:\ShareDataWorker-Receiver
```

## 3. Sửa `appsettings.json` trong từng thư mục

Giống nhau ở cả 5 bản:

| Khóa | Giá trị |
|---|---|
| `ConnectionStrings:DefaultConnection` | CSDL chính |
| `ConnectionStrings:InboundConnection` | CSDL nhận, phải trùng với CSDL nhận của WebAPI |
| `Nats:Url` | Máy chủ NATS |
| `NasStorage:BasePath` | Một đường dẫn tuyệt đối dùng chung, ví dụ `D:\ShareData\send` |

Khác nhau ở từng bản:

| Thư mục | `ShareData:Role` | `ShareData:SelfPartnerCode` | `Nats:ClientName` |
|---|---|---|---|
| `ShareDataWorker-A101` | `SendOnly` | `A101` | `ITS_SHAREDATA_A101` |
| `ShareDataWorker-A102` | `SendOnly` | `A102` | `ITS_SHAREDATA_A102` |
| `ShareDataWorker-A103` | `SendOnly` | `A103` | `ITS_SHAREDATA_A103` |
| `ShareDataWorker-A104` | `SendOnly` | `A104` | `ITS_SHAREDATA_A104` |
| `ShareDataWorker-Receiver` | `ReceiveOnly` | không khai | `ITS_SHAREDATA_RECEIVER` |

Tệp mặc định chưa có khối `ShareData`, phải thêm vào. Ví dụ bản `A101`:

```json
"ShareData": { "Role": "SendOnly", "SelfPartnerCode": "A101" },
```

Bản nhận:

```json
"ShareData": { "Role": "ReceiveOnly" },
```

Thiếu `Role` thì bản đó chạy cả gửi lẫn nhận, trong khi chỉ được có một con nhận. Sai giá trị `Role` thì dịch vụ báo lỗi và dừng ngay lúc khởi động.

## 4. Chuẩn bị trước khi chạy

- [ ] Trên giao diện, mỗi đối tác đang bật, đã **Kết nối** và có đăng ký gửi đang bật. Thiếu một điều kiện thì dịch vụ bỏ qua đối tác đó, không báo lỗi.
- [ ] Nếu máy chủ đang chạy dịch vụ `ShareDataWorker` cũ thì dừng lại, vì nó chạy cả hai chiều:

```powershell
sc.exe stop ShareDataWorker
sc.exe config ShareDataWorker start= disabled
```

## 5. Tạo thử một dịch vụ

Mở PowerShell bằng quyền Administrator. Dùng `sc.exe`, không dùng `sc`. Sau dấu `=` phải có một khoảng trắng.

```powershell
sc.exe create ShareDataWorker-A101 binPath= "D:\ShareDataWorker-A101\ShareDataWorker.exe" start= auto
sc.exe start ShareDataWorker-A101
sc.exe query ShareDataWorker-A101
```

Sau 1 phút dịch vụ vẫn `RUNNING` thì làm tiếp bước 6. Nếu không, dừng ở đây, xem lỗi ở Event Viewer → Windows Logs → Application và gỡ dịch vụ theo mục "Dừng và gỡ".

Phải thử một dịch vụ trước vì `Program.cs` đặt cứng tên dịch vụ `ShareDataWorker`. Theo tài liệu Windows thì tên này được bỏ qua khi mỗi dịch vụ chạy tiến trình riêng, nhưng chưa thử trên máy chủ thật.

## 6. Tạo các dịch vụ còn lại

```powershell
sc.exe create ShareDataWorker-A102 binPath= "D:\ShareDataWorker-A102\ShareDataWorker.exe" start= auto
sc.exe create ShareDataWorker-A103 binPath= "D:\ShareDataWorker-A103\ShareDataWorker.exe" start= auto
sc.exe create ShareDataWorker-A104 binPath= "D:\ShareDataWorker-A104\ShareDataWorker.exe" start= auto
sc.exe create ShareDataWorker-Receiver binPath= "D:\ShareDataWorker-Receiver\ShareDataWorker.exe" start= auto

sc.exe start ShareDataWorker-A102
sc.exe start ShareDataWorker-A103
sc.exe start ShareDataWorker-A104
sc.exe start ShareDataWorker-Receiver
```

## 7. Kiểm tra

Chờ vài lượt gửi, rồi kiểm:

- [ ] `sc.exe query` cả 5 dịch vụ đều báo `RUNNING`.
- [ ] **Lịch sử chia sẻ → Nhật ký truyền nhận** có lượt gửi thành công của đủ 4 đối tác.
- [ ] **Cảnh báo & lỗi** không có cảnh báo mới.
- [ ] Dừng rồi khởi động lại một dịch vụ gửi: dịch vụ gửi tiếp từ mốc, không gửi lại từ đầu.

## Thêm đối tác

Đối tác không có bản clone riêng sẽ không được gửi, vì mỗi bản chỉ lấy đúng mã của nó. Thêm đối tác thì lặp lại bước 2, 3, 5 cho mã đó.

## Nâng cấp

Dừng các dịch vụ, chép bản publish mới vào từng thư mục **nhưng không ghi đè `appsettings.json`**, rồi khởi động lại.

## Dừng và gỡ

```powershell
sc.exe stop ShareDataWorker-A101
sc.exe delete ShareDataWorker-A101
```
