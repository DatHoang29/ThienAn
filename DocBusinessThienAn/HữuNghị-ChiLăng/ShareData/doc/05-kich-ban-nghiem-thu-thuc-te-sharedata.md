# ✅ Kịch Bản Nghiệm Thu Thật — Phân Hệ ShareData

> 📅 **Soạn:** 16/10/2026 · **Người chạy:** người dùng tự khởi động dự án và kiểm tay · **Môi trường:** DB **local/test** (⛔ không chạy trên staging hay môi trường thật).
> 📌 **Căn cứ:** [`Sharedata_MasterPlan.md`](../Plan/Sharedata_MasterPlan.md) PHẦN VII, [`02-mapping-goi-tin-101-111.md`](02-mapping-goi-tin-101-111.md), code Worker/WebAPI/FE tại thời điểm soạn.
> 📌 **Phạm vi:** nghiệm thu luồng nghiệp vụ với **API và DB thật**. Phần Playwright với API giả lập đã chấp nhận (R16 bỏ); kịch bản này thay cho nó.

Mỗi mục có: **Bước** → **Kết quả mong đợi** → **☐ Đạt / ☐ Không đạt**. Ghi lỗi vào bảng cuối tài liệu.

---

## 🧭 0. Điều kiện trước khi chạy

### 0.1. Trỏ đúng DB (quan trọng) — chạy bằng VS Code debug

- `ShareDataWorker/appsettings.json` **và** `appsettings.Development.json` đều **mặc định trỏ DB staging `10.10.8.30`**. Chạy mà không có biến môi trường ghi đè là **ghi lên staging**.
- [`.vscode/launch.json`](../../../../.vscode/launch.json) đã có sẵn cấu hình ép về local bằng biến môi trường (biến môi trường thắng file `appsettings`). **Chỉ dùng các cấu hình sau, không dùng cấu hình `Server 10.10.8.30`:**

| Mục đích | Cấu hình debug nên chọn |
|---|---|
| WebAPI + Worker cùng lúc | Compound **`🚀 ShareData (WebAPI + Worker Local)`** |
| WebAPI riêng | `🌐 TAC_WebAPI (Local Docker)` — cổng `http://localhost:5005` |
| Worker riêng | `📦 ShareDataWorker (Local Docker)` — `DOTNET_ENVIRONMENT=Development` |
| Frontend | `🎨 WebVue (Dev Server)` (compound ShareData **không** gồm FE, mở thêm riêng) |

- Hai cấu hình Local trỏ `Server=localhost`, CSDL `DEV_ITS10`, `DEV_ITS10_Log` (WebAPI) và `DEV_ITS10_Inbound` (Worker), **cổng mặc định 1433**. Bạn đã chạy được nên giữ nguyên; nếu SQL Server local của bạn ở cổng khác (ví dụ Docker `14333`), sửa `Server=localhost,<cổng>` trong **chính cấu hình Local** cho khớp, không sửa file `appsettings`.
- Tự kiểm sau khi lên: log khởi động của Worker và truy vấn `SELECT @@SERVERNAME, DB_NAME()` trên DB mà các script ở 0.2 đã chạy phải **khớp** với DB mà WebAPI đang dùng.
- ⛔ Đừng bấm F5 bằng cấu hình `Attach` hay `dotnet run` trần cho Worker: khi đó không có biến môi trường và Worker kết nối staging.

### 0.2. Chạy script SQL (người dùng tự chạy, theo thứ tự)

Chạy trên DB local; script có `USE` riêng.

| # | Script (thư mục `sql/`) | CSDL | Ghi chú |
|---|---|---|---|
| 00 | [`00-sharedata-migration-mot-lan.sql`](../sql/00-sharedata-migration-mot-lan.sql) | `DEV_ITS10` | Một lần; dọn bảng thừa, chuyển dữ liệu sang Packet/Mapping (bỏ qua nếu DB đã dọn) |
| 01 | [`01-sharedata-cau-truc.sql`](../sql/01-sharedata-cau-truc.sql) | `DEV_ITS10` | Cột đối tác (`Address`, `Port`, `EndPointApiUrl`) trên DEV_ITS10 |
| 02 | [`02-sharedata-seed-goi-tin.sql`](../sql/02-sharedata-seed-goi-tin.sql) | `DEV_ITS10` | Mẫu SQL 101–110, SyncMode mặc định khi null, bật gói 110, tắt gói 111; cờ `@GhiDeMauGoiTin = 0` không ghi đè |
| 03 | [`03-sharedata-seed-cau-hinh.sql`](../sql/03-sharedata-seed-cau-hinh.sql) | `DEV_ITS10` | Khởi tạo SysConfigType, nạp 24 mã lỗi ESH-xxxx và nhóm `sharedata_tracker` (15 bảng, idempotent) |
| 04 | [`04-sharedata-dich-thuat.sql`](../sql/04-sharedata-dich-thuat.sql) | `DEV_ITS10` | 48 khóa bản dịch vi/en cho MaxLookBackDays, SQL trích xuất, SQL Validator, Metadata và **12 khóa lỗi cấu hình kết nối đối tác** (`sharedata.connectConfig*`) (MERGE không ghi đè). **Bắt buộc chạy trước mục C**, nếu không thông báo lỗi ở C3/C6 hiện khóa thô |
| 05 | [`05-sharedata-du-lieu-gia-lap.sql`](../sql/05-sharedata-du-lieu-gia-lap.sql) | `DEV_ITS10` | **Chỉ local/test** (104 Thời tiết, 106 WIM, 110 Sự cố/VMS) |
| 06 | [`06-sharedata-doi-ten-cot-dong-bo.sql`](../sql/06-sharedata-doi-ten-cot-dong-bo.sql) | `DEV_ITS10` | Đổi tên cột đồng bộ ShareDataPacket/ShareDataMapping (SyncKind→SyncMode, TimeColumn/CursorColumn→CursorTimeColumn, LookbackDays→MaxLookBackDays). Idempotent; chỉ cần khi DB còn tên cũ. |

> ⚠️ **Cảnh báo thứ tự chạy:** Với CSDL cũ còn mang tên cột cũ (`SyncKind`, `TimeColumn`/`CursorColumn`, `LookbackDays`), cần chạy script `06` trước script `02` (hoặc trước khi nghiệm thu) để đổi tên và bảo toàn dữ liệu cấu hình. Các truy vấn kiểm tra ở mục 0.5 đều dùng tên cột mới (`SyncMode`, `CursorTimeColumn`, `MaxLookBackDays`), DB cũ bắt buộc phải chạy `06` trước.
> Chi tiết mục đích, thứ tự và hướng dẫn thực thi xem tại [`sql/README.md`](../sql/README.md).

### 0.3. Thành phần cần chạy

| Thành phần | Ghi chú |
|---|---|
| **WebAPI** (`TAC_WebAPI`) | Cổng `5005` theo cấu hình Local; đăng nhập **SuperAdmin** (lưu SQL yêu cầu SuperAdmin) |
| **Frontend** (WEBVUE) | `npm run dev` như thường lệ |
| **ShareDataWorker** | Chạy với biến môi trường ở 0.1 |
| **NATS** | Ở `DOTNET_ENVIRONMENT=Development` (cấu hình Local) file `appsettings.Development.json` đặt `Nats:Enabled = false`, nên **mục D9 (gửi ngay khi có dữ liệu mới) không chạy được** ở cấu hình mặc định. Muốn thử D9: chạy một NATS local (cổng `4222`) và thêm `"Nats__Enabled": "true"` vào `env` của cấu hình `📦 ShareDataWorker (Local Docker)`. Không thử D9 thì luồng quét định kỳ vẫn chạy bình thường |
| **Receiver giả** (đối tác nhận) | Xem 0.4 |

### 0.4. Receiver giả (đối tác nhận)

Lưu thành `receiver.mjs`, chạy `node receiver.mjs 9090`. Mỗi request nhận được ghi ra `received-<n>.json` (method, url, header, body). Tạo file rỗng tên `FAIL` cùng thư mục để receiver trả **500** (giả lập lỗi), xóa file để trả **200**.

```js
import http from 'node:http';
import fs from 'node:fs';

const port = Number(process.argv[2] ?? 9090);
let n = 0;

http.createServer((req, res) => {
  let body = '';
  req.on('data', (c) => (body += c));
  req.on('end', () => {
    n++;
    const fail = fs.existsSync('FAIL');
    fs.writeFileSync(
      `received-${n}.json`,
      JSON.stringify({ method: req.method, url: req.url, headers: req.headers, body }, null, 2),
    );
    console.log(`#${n} ${req.method} ${req.url} ${body.length}B -> ${fail ? 500 : 200}`);
    res.writeHead(fail ? 500 : 200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ success: !fail }));
  });
}).listen(port, () => console.log(`Receiver http://localhost:${port}`));
```

> Nếu đối tác cấu hình `SuccessField`/`ResponseContract`, trả đúng trường đó thay vì `success`.

### 0.5. Truy vấn kiểm tra (chỉ đọc, chạy trên DB local)

```sql
SELECT Code, SyncMode, Status, CursorTimeColumn, MaxLookBackDays FROM dbo.ShareDataPacket ORDER BY Code;
SELECT TOP 20 * FROM dbo.ShareDataLastSend;                                   -- mốc gửi nối đuôi
SELECT TOP 30 * FROM dbo.ShareDataActivityLog ORDER BY CreateTime DESC;       -- nhật ký cha–con
SELECT TOP 30 * FROM dbo.ShareDataAlertLog    ORDER BY CreateTime DESC;       -- cảnh báo ESH-xxxx
-- Trên CSDL Inbound:
SELECT TOP 20 * FROM dbo.ShareDataInboundPacket ORDER BY CreateTime DESC;     -- gói nhận
```

> Nếu bảng nào không có cột `CreateTime`, bỏ mệnh đề `ORDER BY`.

---

## 🅰️ A. Chế độ và trạng thái gói tin (màn hình *Cấu hình gói tin / SQL Center*, thư mục `dataSource`)

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| A1 | Mở danh sách gói tin | Tag chế độ mỗi gói **khớp cột `SyncMode` trong DB** (0.5): 101, 102, 105, 108 là *gửi toàn bộ*; 103, 104, 106, 107, 109 là *nối đuôi* | ☐ Đạt ☐ Không đạt |
| A2 | Mở gói **105** (chưa chỉnh gì) | Chế độ mặc định **Snapshot / gửi toàn bộ** (không phải nối đuôi) | ☐ ☐ |
| A3 | Đổi 105 sang *nối đuôi*, đặt `CursorTimeColumn` và `MaxLookBackDays = 7` → Lưu → tải lại trang → mở lại | Chế độ, cột mốc, 7 ngày **giữ nguyên**; tag danh sách đổi theo | ☐ ☐ |
| A4 | Xóa trắng `MaxLookBackDays` → Lưu → mở lại | Ô **rỗng** (không quay về 7) | ☐ ☐ |
| A5 | Đổi 105 về *gửi toàn bộ* → Lưu | Lưu được; câu SQL không bị kiểm cấu trúc nối đuôi | ☐ ☐ |
| A6 | Xem gói **111** (và 110 nếu chưa bật) | Hiện **"Đã tắt"**; không có nút chạy/gửi | ☐ ☐ |

## 🅱️ B. SQL trích xuất và an toàn

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| B1 | Gói 101 → *Xem trước (preview)* | Có cột và dữ liệu thật, trả trong vài giây, không treo | ☐ ☐ |
| B2 | SQL `SELECT … ORDER BY …` **không có TOP** → preview | Chạy được, không lỗi cột trùng tên | ☐ ☐ |
| B3 | Gói nối đuôi (103): bỏ `__lastTime` hoặc `__rowid` khỏi SELECT → Lưu | **Bị từ chối**, thông báo nêu thiếu cột | ☐ ☐ |
| B4 | Gói nối đuôi: đổi `ORDER BY … DESC` → Lưu | **Bị từ chối** (sắp giảm dần) | ☐ ☐ |
| B5 | Lưu lần lượt: `DELETE FROM TmsZone`; `SELECT 1; SELECT 2`; `EXEC sp_who`; `SELECT * FROM SysUser`; `SELECT * FROM ShareDataPartner`; `SELECT * FROM OtherDb.dbo.TmsZone` | **Mỗi câu đều bị từ chối** với thông báo đúng ngôn ngữ đang chọn | ☐ ☐ |
| B6 | Nút **"Mẫu mặc định"** | Nạp lại đúng template của gói từ DB | ☐ ☐ |
| B7 | Chọn đối tác A → sửa SQL riêng → Lưu; mở đối tác B và mẫu chung | SQL của A được giữ; **B và mẫu chung không đổi** | ☐ ☐ |
| B8 | (Nếu có tài khoản không phải SuperAdmin) thử lưu SQL | **Bị chặn quyền** | ☐ ☐ |

## 🅲 C. Đối tác và kết nối (màn *Đối tác & Đăng ký chia sẻ*, thư mục `sharing`)

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| C1 | Tạo đối tác **P-REST**: Address `http://localhost`, Port `9090`, EndPoint `/api/receive`, kiểu REST | Lưu được, mở lại giữ nguyên | ☐ ☐ |
| C2 | Trong cấu hình kết nối thêm `Format=Json`, `Enabled=true` và 1 key tùy ý → Lưu → mở lại | Mọi key **giữ nguyên**; giá trị bí mật (Token/ApiKey/Password) **bị che** khi hiển thị | ☐ ☐ |
| C3 | Thêm key trùng tên cột (`Address`, `Port`, `EndPointApiUrl`), key trùng nhau, key rỗng | **Từng trường hợp bị từ chối**, thông báo hiển thị theo ngôn ngữ đang chọn (nhóm lỗi: khóa trùng cột, khóa trùng nhau, khóa rỗng), không hiện khóa thô `lz.validation...` | ☐ ☐ |
| C4 | Mở đối tác khác rồi quay lại P-REST | Cấu hình **không lẫn** sang nhau (không mang token qua đối tác khác) | ☐ ☐ |
| C5 | Tạo đối tác kiểu FTPS (rồi DISK, NAS) → Lưu được; mở lại giữ nguyên kiểu đã nhập | Được chấp nhận và gửi qua đúng kênh (FTPS bật `UseSsl`, DISK/NAS đi kênh File). Một giá trị lạ (ví dụ "ABC") vẫn bị từ chối | ☐ ☐ |
| C6 | Kiểm tra 12 trường hợp cấu hình kết nối đối tác sai (JSON không phải object, cú pháp lỗi, key rỗng, key trùng tên cột, key bãi bỏ OutputPath, trùng lặp key, cấu trúc lồng nhau, Timeout không nguyên dương, Boolean sai định dạng, AuthType/Encryptor không hỗ trợ, FileNamePattern thiếu token) | Từng trường hợp bị từ chối với thông điệp đa ngôn ngữ chuẩn từ BaseMsg.ShareData.Validation (mã ConnectConfigIssue tương ứng) | ☐ ☐ |

## 🅳 D. Gửi (chiều ra)

Điều kiện: receiver chạy ở 0.4; Worker đang chạy; đối tác P-REST đã tạo. Tạo **đăng ký chiều gửi** (Outbound) cho gói cần thử với chu kỳ ngắn (~30 giây).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| D1 | Đăng ký gói **101** (Snapshot) | Receiver nhận request; body JSON đủ trường theo mapping; **số phần tử = số dòng nguồn** (không bị cắt cứng 100); header có `PartnerCode`, `PacketCode`, `SerialNbr` | ☐ ☐ |
| D2 | Màn *Lịch sử chia sẻ* (`history`) | Có bản ghi **cha** và các bản ghi **con** theo bước/trang; mô tả **không nối lặp** | ☐ ☐ |
| D3 | Đăng ký gói **103** (nối đuôi), chờ 1 chu kỳ; sau đó chạy `UPDATE dbo.TmsTrafficData SET UpdateTime = GETDATE() WHERE ID IN (<3 ID bất kỳ>)` trên DB local; chờ chu kỳ kế | Chu kỳ sau gửi **đúng 3 dòng** vừa đổi; chu kỳ tiếp theo không có dòng mới → ghi *không có dữ liệu mới*, **không gửi lặp**; `ShareDataLastSend` tiến, không lùi | ☐ ☐ |
| D4 | Tạo file `FAIL` cạnh receiver → chờ chu kỳ gửi → xóa file `FAIL` → chờ chu kỳ | Khi lỗi: cảnh báo ở màn *Nhật ký lỗi* (`errorLog`), `ShareDataLastSend` **không tiến**; khi hết lỗi: gửi bù đủ dữ liệu bị lỡ, không sót | ☐ ☐ |
| D5 | Tìm tệp bằng chứng trong `NasStorage:BasePath` (mặc định `sharedata/send`, tương đối với thư mục chạy Worker) theo `Out/<Đối tác>/yyyy/MM/dd/<Gói>/…`; so sánh với `body` ở `received-<n>.json` | Tệp bằng chứng **đúng từng byte** bản đã gửi | ☐ ☐ |
| D6 | Đặt khóa cũ `ArchiveEnabled=false`, chờ 1 lần gửi | Khóa cũ bị bỏ qua, **vẫn sinh tệp bằng chứng** bình thường; log cha không ghi chú không lưu | ☐ ☐ |
| D7 | Đối tác kiểu **File**: `DirectoryPath` nằm trong `thư mục gốc BasePath`, `FileNamePattern` có `{serial}`/`{timestamp}` | Sinh tệp đúng tên, mỗi trang một tệp, không ghi đè | ☐ ☐ |
| D8 | `DirectoryPath` ngoài thư mục cho phép; `FileNamePattern` có `../` hoặc `/` | **Bị từ chối**, không ghi tệp nào ngoài thư mục | ☐ ☐ |
| D9 | (Cần NATS) Đăng ký gói **101** với *gửi ngay khi có dữ liệu mới* bật, `DebounceSec = 10`; chạy `UPDATE dbo.TmsZoneStatus SET UpdateTime = GETDATE() WHERE ID = '<ID>'` | Gửi trong khoảng `DebounceSec`, **không đợi hết chu kỳ** | ☐ ☐ |
| D10 | Đăng ký chế độ *Single* (một lần): gửi thành công; làm lại với receiver lỗi (`FAIL`) | Thành công → *hết hạn*; lỗi → **giữ hoạt động** và thử lại ở chu kỳ sau | ☐ ☐ |
| D11 | (Mã hóa chỉ viết sẵn `EncryptManager`, chưa nối luồng gửi/nhận theo quyết định 16/10/2026) | Chưa nối vào luồng (quyết định 16/10/2026): BỎ QUA, chỉ nghiệm thu khi áp dụng mã hóa | Bỏ qua (chưa nối) |
| D12 | (Tùy chọn — cần hạ tầng thử, mặc định kênh chưa bật) Đối tác kiểu **Kafka**, `BootstrapServers` + `Topic` trong cấu hình; broker local | Message là byte[] UTF-8 JSON, có 4 header định danh; gửi lỗi (tắt broker) thì cảnh báo và mốc không tiến | ☐ ☐ |
| D13 | (Tùy chọn — cần hạ tầng thử, mặc định kênh chưa bật) Đối tác kiểu **Ftp** (hoặc **FTPS**), `RemotePath` trong cấu hình; FTP server local | Tệp tải lên đúng nội dung, tên theo mẫu; sai mật khẩu thì lỗi rõ ràng, mốc không tiến | ☐ ☐ |
| D14 | (Tùy chọn — cần hạ tầng thử, mặc định kênh chưa bật) Cấu hình `Format=Csv` cho kênh **File** hoặc **Ftp** | Tệp CSV phân cách dấu phẩy, đúng thứ tự cột; bằng chứng lưu đúng byte như tệp đã gửi | ☐ ☐ |

## 🅴 E. Nhận (chiều vào)

Điều kiện: một đối tác có đăng ký chiều **Inbound**, ánh xạ ngược cho gói 101 hoặc 103.

Dùng nội dung `body` đã bắt được ở D1/D3 làm dữ liệu thử (đã đúng định dạng của chính hệ thống).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| E1 | `POST /api/sharedata/shareDataInbound/receive` với header `PartnerCode`, `PacketCode`, `SerialNbr`; body = nội dung ở trên | HTTP **200**; `ShareDataInboundPacket` (CSDL Inbound) có 1 dòng mới; Worker xử lý xong, `ProcessState` chuyển sang hoàn tất; dữ liệu vào bảng đích | ☐ ☐ |
| E2 | Gửi lại **đúng gói cũ** | Không nhân đôi dòng ở bảng đích (MERGE) | ☐ ☐ |
| E3 | Thiếu `PartnerCode` hoặc đối tác không tồn tại | Bị từ chối, không có dòng mới | ☐ ☐ |
| E4 | Body không đọc được (sai định dạng JSON) | Gói ghi nhận **lỗi** có mã cảnh báo (lỗi đọc gói); không làm hỏng các gói khác | ☐ ☐ |
| E5 | (Mã hóa chỉ viết sẵn `EncryptManager`, chưa nối luồng gửi/nhận theo quyết định 16/10/2026) | Chưa nối vào luồng (quyết định 16/10/2026): BỎ QUA, chỉ nghiệm thu khi áp dụng mã hóa | Bỏ qua (chưa nối) |

## 🅵 F. Bảng theo dõi động (màn *Cấu hình hoạt động*, thư mục `cfgSystem/sysOpConfig`, nhóm `sharedata_tracker`)

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| F1 | Lọc nhóm `sharedata_tracker` | Thấy 15 dòng; `TmsEquipment` ở trạng thái **tắt** | ☐ ☐ |
| F2 | Sửa 1 thiết bị trong `TmsEquipment` | **Không** kích hoạt gửi 102/103/108 | ☐ ☐ |
| F3 | Bật `TmsEquipment`, chờ ≤ `ConfigReloadSeconds` (mặc định 60 giây), sửa thiết bị, rồi **tắt lại** | Log Worker ghi *đã cập nhật cấu hình bảng giám sát (Thêm mới: 1)*; sửa thiết bị lúc bật thì **có** kích hoạt; sau khi tắt lại thì dừng | ☐ ☐ |
| F4 | Xóa mềm 1 dòng (ví dụ `TmsWeather`) | Bảng đó **ngừng** được theo dõi sau ≤ 60 giây | ☐ ☐ |
| F5 | Thêm 1 dòng với **tên bảng không tồn tại** | Cảnh báo ghi tên bảng sai; các bảng khác **vẫn chạy** | ☐ ☐ |
| F6 | **Tắt tất cả** dòng của nhóm | Worker báo ESH-1603 **đúng 1 lần**, không còn kích hoạt tức thì; luồng quét định kỳ (D1/D3) **vẫn gửi** | ☐ ☐ |
| F7 | Bật lại các dòng đã tắt | Theo dõi trở lại sau ≤ 60 giây, không quét lại từ đầu, không mất thay đổi | ☐ ☐ |

## 🅶 G. Gói 110 (Gói 110 chạy trên nguồn có sẵn — phương án A, 16/10/2026, Master Plan R13)

Điều kiện: đã chạy script 05 để có mock TmsWeather; Worker đang chạy.

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| G1 | Gói 110 đang bật; đăng ký REST tới receiver; đã chạy script 05 để có mock TmsWeather (cùng mock sự cố, VMS) | Receiver nhận payload có `incidentMessage`, `weatherMessage`, `guidanceContent`, `locationKm`, `locationMet`, `publishedTime`; `channel`, `deliveryState` rỗng (chưa có nguồn) | ☐ ☐ |
| G2 | Chu kỳ kế tiếp | Gửi **toàn bộ** lại (Snapshot), không có mốc | ☐ ☐ |
| G3 | Đặt gói 110 `Status = 0` | Không gửi, không sinh cảnh báo mới | ☐ ☐ |
| G4 | Gói 111 | Không chạy, không cảnh báo | ☐ ☐ |

## 🅷 H. Chung

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| H1 | Đổi ngôn ngữ giao diện vi ↔ en, lặp lại B5, C3 và C6 | Thông báo lỗi đổi ngôn ngữ đúng, không còn chuỗi cứng lẫn lộn | ☐ ☐ |
| H2 | Dọn dẹp định kỳ (Cleanup) | Đã viết sẵn logic cleanup ở dạng comment, chưa kích hoạt, chưa có test (Master Plan R15/DQ-SV-9). Cleanup ESH-16 trong DataChangeTrackingService (SV-13) giữ nguyên. | Bỏ qua (chưa kích hoạt) |

---

## 🅸 I. Nghiệm thu toàn trình tự gửi — tự nhận nội bộ (In-Process Loopback)

Kịch bản chứng minh luồng toàn trình gửi → nhận hoạt động trọn vẹn khi chưa có đối tác bên ngoài hoặc không cần phần mềm bên ngoài hỗ trợ. Kịch bản này đã được khóa tự động bằng bài test `ShareDataSendReceiveTests` (chạy thật trên CSDL test local, không dùng mock).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| I1 | Cấu hình đối tác 'Nội bộ' trỏ endpoint về WebAPI cục bộ (`/api/sharedata/shareDataInbound/receive`), tạo đăng ký Outbound & Inbound cho gói Snapshot (105) | Worker gửi dữ liệu thành công qua HTTP; Inbound tiếp nhận, lưu bằng chứng đĩa và MERGE vào bảng đích đầy đủ 100% cột | ☐ ☐ |
| I2 | Gửi lặp lại cùng gói Snapshot (kiểm tra Idempotency) | MERGE không nhân đôi số dòng trong bảng đích | ☐ ☐ |
| I3 | Thử nghiệm gói Incremental (104) qua 2 chu kỳ: đợt 1 (3 dòng ban đầu), đợt 2 (1 dòng mới + 1 dòng cập nhật) | Đợt 1 nạp 3 dòng và tiến mốc; đợt 2 chỉ gửi 2 dòng thay đổi, bảng đích cập nhật đúng giá trị mới và tổng 4 dòng | ☐ ☐ |
| I4 | Thử nghiệm đối tác trỏ endpoint lỗi (404 Not Found) | Gửi thất bại sạch sẽ, mốc không tiến, tệp bằng chứng vẫn lưu trên đĩa, ghi cảnh báo vào `ShareDataAlertLog` | ☐ ☐ |

---

## 🧹 Dọn sau khi test

- Đưa gói 105 về chế độ ban đầu (A5); xóa đăng ký, đối tác thử (P-REST…) bằng màn hình.
- Trả lại các dòng `sharedata_tracker` đã đổi (F3–F7).
- Xóa `received-*.json`, file `FAIL`, thư mục tệp bằng chứng thử trong `sharedata/send`.
- Nếu đã thêm `Nats__Enabled=true` vào cấu hình debug để thử D9, trả lại như cũ.
- Dọn topic/tệp thử cho D12–D14 nếu có chạy: xóa topic/message thử trên Kafka broker, xóa tệp đã tải lên trên FTP server local và thư mục File local.

## 📝 Ghi nhận kết quả

| Mã | Mô tả lỗi / lệch | Mức (🔴/⚠️/ℹ️) | Ghi chú tái hiện |
|---|---|---|---|
| | | | |

**Tiêu chí đạt:** toàn bộ mục A–F đạt; G đạt; H không phát sinh lỗi; I đạt (đã khóa bằng test tự động `ShareDataSendReceiveTests`). Kết quả tổng hợp ghi lại ở `DQ-TEST-3` của MasterPlan.
