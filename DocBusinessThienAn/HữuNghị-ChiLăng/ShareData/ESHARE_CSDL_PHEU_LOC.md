# ESHARE — Thiết kế CSDL (kiến trúc phễu lọc)

*Bản chốt · 20/08/2026 · căn cứ cấu trúc và dữ liệu thật đọc từ CSDL `DEV_ITS10`*
*Thay thế toàn bộ các bản trước.*

---

## 1. Bối cảnh

**Cấu trúc gói tin nằm trong CODE, không nằm trong CSDL.** 11 gói tin cố định theo quy chuẩn,
mỗi gói là một lớp C# gắn `[EshPacket]` / `[EshField]`, kèm một provider (truy vấn chiều gửi)
và một handler (xử lý chiều nhận).

CSDL chỉ chứa **phễu lọc** — phần cấu hình được, quyết định mỗi đối tác nhận field nào,
gọi tên là gì, đơn vị nào, mã nào.

```
Chiều GỬI    gói tin (code) → PHỄU LỌC → đối tác
Chiều NHẬN   đối tác → PHỄU LỌC ngược → gói tin (code) → handler ghi vào CSDL
```

**Hiện trạng dữ liệu:** mọi bảng ShareData đều rỗng, trừ `ShareDataPartner` (4 dòng) và
`ShareDataSubscription` (5 dòng) ⇒ không cần script migrate.

---

## 2. Tổng hợp thay đổi

| Bảng | Việc | Vai trò |
|---|---|---|
| `ShareDataMapping` | **TẠO MỚI** | **Phễu lọc** — đối tác này khác chuẩn ở đâu |
| `ShareDataCodeSet` | **TẠO MỚI** | Bộ mã quy đổi giá trị: `1 → on`, `1 → slow` |
| `ShareDataAlertLog` | **TẠO MỚI** | Cảnh báo & lỗi (màn này đang chạy không có bảng) |
| `ShareDataDataSource` | **DROP** | Bỏ whitelist, và chỉ khai được một bảng |
| `ShareDataMappingProfile` | **DROP** | Thay bằng `ShareDataMapping` |
| `ShareDataActivityLog` | **ALTER** | `+ MappingId`, `+ PacketVersion` |
| `ShareDataEventSource` | **ALTER** | Nới `DatatypeCode` lên `nvarchar(32)` |
| `ShareDataPartner` | giữ nguyên | Đối tác + tham số kết nối C2C |
| `ShareDataSubscription` | giữ nguyên | Đăng ký: đối tác × gói tin × chiều × lịch |
| `ShareDataSession` | giữ nguyên | Phiên kết nối C2C |

**3 bảng mới · 2 DROP · 2 ALTER · 3 giữ nguyên.**

### Không còn bảng nào cho cấu trúc gói tin

`ShareDataPacket` và `ShareDataPacketTable` trong các bản trước **không làm nữa**. Danh sách
11 gói tin là hằng trong code; `EshPacketCatalog` quét assembly lúc khởi động và trả về qua
API cho giao diện. Danh mục `shareData_type` trong `SysConfigType` giữ để hiển thị nhãn.

### Sơ đồ quan hệ

```
        ┌── CODE (không phải CSDL) ──────────────────┐
        │  Packet101 + [EshField]                    │
        │  Packet101Provider   (truy vấn chiều gửi)  │
        │  Packet101Handler    (xử lý chiều nhận)    │
        └──────────────┬─────────────────────────────┘
                       │ fieldKey
ShareDataCodeSet ◄─────┤
   (+ValuesJson)       │ codeSetId
        ▲              ▼
        └────── ShareDataMapping (+ItemsJson)        ← PHỄU LỌC
                       ▲ PartnerId · DatatypeId · Direction
                       │
ShareDataPartner ──1:N── ShareDataSubscription ──1:N── ShareDataSession
                                  │
                                  ├──► ShareDataActivityLog  (nhật ký + BẰNG CHỨNG)
                                  └──► ShareDataAlertLog     (cảnh báo, sửa được)
```

---

## 3. Quy ước nhà — bắt buộc tuân theo

Đọc từ `ShareDataPartner` / `ShareDataSubscription` đang có:

```
PK          : ID nvarchar(64), viết HOA — không phải "Id"
Đuôi chuẩn  : Remark nvarchar(256), TenantId nvarchar(64), Code nvarchar(64),
              CreateTime datetime, CreateUId nvarchar(64),
              UpdateTime datetime, UpdateUId nvarchar(64),
              RowStatus nvarchar(32), IsDelete datetime
Nullable    : mọi cột trừ ID
Enum        : lưu kiểu int
Tập con     : lưu JSON nvarchar(max)
Index       : PK_<Bảng>_ID clustered · index_<Bảng>_CT trên CreateTime
```

**Ba điểm dễ sai:**

1. **`IsDelete` là `datetime`**, không phải `bit`. Xoá mềm = ghi mốc thời gian.
   Mọi bộ lọc viết `IsDelete IS NULL`; mọi filtered index kèm `WHERE IsDelete IS NULL`.
2. **Toàn bộ CSDL này không có FOREIGN KEY nào** ⇒ không thêm FK, chỉ thêm index.
3. **Khoá chính tên `ID`** hai chữ hoa — SqlSugar mặc định map thành `Id` nếu không khai rõ.

---

## 4. Khớp nối với `ShareDataSubscription` — không ALTER dòng nào

Subscription đã có sẵn đúng ba cột cần thiết:

```
PartnerId   nvarchar(64)  →  ShareDataPartner.ID
DatatypeId  nvarchar(32)  →  mã gói tin trong code, VD '101'
Direction   nvarchar(16)  →  OUTBOUND | INBOUND
```

**Khoá tra cứu phễu lọc lúc chạy:** `(PartnerId, DatatypeId, Direction)` → `ShareDataMapping`.

**Ba cột thừa trên Subscription** (không drop được, đã NULL sẵn cả 5 dòng):

| Cột | Xử lý |
|---|---|
| `DataSourceId` | để NULL — nguồn dữ liệu do provider trong code quyết định |
| `MappingProfileId` | để NULL — giải theo bộ ba lúc chạy, tránh lỗi thời khi có phễu lọc mới |
| `EventSourceId` | **vẫn dùng** — nguồn kích hoạt riêng của từng đăng ký |

Bốn cột lập lịch `RunStatus` / `IntervalSeconds` / `NextTimeRun` / `LastTimeRun` **đã có sẵn** —
service bám vào bộ lập lịch đang có, không tạo cơ chế thứ hai.

---

## 5. DDL

### 5.1 `ShareDataMapping` — phễu lọc

```sql
CREATE TABLE dbo.ShareDataMapping (
    ID                nvarchar(64)   NOT NULL,
    PartnerId         nvarchar(64)   NULL,   -- -> ShareDataPartner.ID
    DatatypeId        nvarchar(32)   NULL,   -- mã gói tin trong code, VD '101'
    PacketVersion     nvarchar(16)   NULL,   -- '1.0' — từ [EshPacket(Version = "1.0")]
    Direction         nvarchar(16)   NULL,   -- OUTBOUND | INBOUND — KHỚP Subscription
    Format            nvarchar(8)    NULL,   -- DATA | FILE — KHỚP Subscription.Format
    TargetRootEntity  nvarchar(128)  NULL,   -- tên thực thể gốc phía đối tác (BB)
    ItemsJson         nvarchar(max)  NULL,   -- CHỈ ghi field lệch chuẩn — mục 6.1
    IsActive          bit            NULL,
    Name              nvarchar(128)  NULL,
    Remark            nvarchar(256)  NULL,
    TenantId          nvarchar(64)   NULL,
    Code              nvarchar(64)   NULL,
    CreateTime        datetime       NULL,
    CreateUId         nvarchar(64)   NULL,
    UpdateTime        datetime       NULL,
    UpdateUId         nvarchar(64)   NULL,
    RowStatus         nvarchar(32)   NULL,
    IsDelete          datetime       NULL,
    CONSTRAINT PK_ShareDataMapping_ID PRIMARY KEY CLUSTERED (ID)
);
CREATE INDEX index_ShareDataMapping_CT ON dbo.ShareDataMapping(CreateTime);

-- Duy nhất 1 phễu lọc đang dùng cho mỗi (đối tác × gói tin × chiều).
-- Đây chính là khoá tra cứu lúc chạy.
CREATE UNIQUE INDEX UX_ShareDataMapping_Active
    ON dbo.ShareDataMapping(PartnerId, DatatypeId, Direction)
    WHERE IsActive = 1 AND IsDelete IS NULL;
```

**Ghi chú từng cột:**

| Cột | Dùng để làm gì | Tại sao cần |
|---|---|---|
| `PartnerId` | Đối tác của phễu lọc này | **Bảng DUY NHẤT trong nhóm mới có cột đối tác.** Mọi thứ riêng theo đối tác đều nằm ở đây, không rải sang bảng khác |
| `DatatypeId` | Mã gói tin, VD `'101'` | Cùng kiểu `nvarchar(32)` với `Subscription.DatatypeId` để join không bị implicit conversion |
| `PacketVersion` | Phiên bản gói tin mà phễu lọc này được viết cho | Gói tin fix cứng nên phiên bản là hằng trong code (`[EshPacket(Version)]`). Ghim lại để biết phễu lọc có còn khớp với gói tin hiện tại không sau khi triển khai bản mới |
| `Direction` | `OUTBOUND` \| `INBOUND` | ⚠ Phải cùng kiểu và cùng tập giá trị với `Subscription.Direction nvarchar(16)`. Bảng `MappingProfile` cũ dùng `nvarchar(8)` với `OUT`/`IN` — lệch, phải thống nhất |
| `Format` | `DATA` \| `FILE` | Khớp `Subscription.Format` |
| `TargetRootEntity` | Tên thực thể gốc phía đối tác, VD `'BB'` | Đối tác không chỉ gọi field khác tên mà còn gọi **bảng** khác tên |
| `ItemsJson` | Danh sách field lệch chuẩn | Field không có dòng ⇒ gửi theo chuẩn. Đối tác tuân thủ chuẩn có `ItemsJson = []` |
| `IsActive` | Phễu lọc đang được áp dụng | Duy nhất một bản đang dùng, DB đảm bảo bằng filtered unique index — nếu không sẽ có hai bản cùng hiệu lực và service không biết chọn cái nào |

### 5.2 `ShareDataCodeSet` — bộ mã quy đổi giá trị

```sql
CREATE TABLE dbo.ShareDataCodeSet (
    ID           nvarchar(64)   NOT NULL,
    Name         nvarchar(128)  NULL,   -- 'Tình trạng giao thông'
    Description  nvarchar(256)  NULL,
    Scope        int            NULL,   -- 1 = bộ mã chuẩn (TCVN) · 2 = biến thể cho đối tác
    ValuesJson   nvarchar(max)  NULL,   -- lược đồ mục 6.2
    Remark       nvarchar(256)  NULL,
    TenantId     nvarchar(64)   NULL,
    Code         nvarchar(64)   NULL,   -- 'TRAFFIC_COND', 'DEVICE_STATE'
    CreateTime   datetime       NULL,
    CreateUId    nvarchar(64)   NULL,
    UpdateTime   datetime       NULL,
    UpdateUId    nvarchar(64)   NULL,
    RowStatus    nvarchar(32)   NULL,
    IsDelete     datetime       NULL,
    CONSTRAINT PK_ShareDataCodeSet_ID PRIMARY KEY CLUSTERED (ID)
);
CREATE INDEX index_ShareDataCodeSet_CT ON dbo.ShareDataCodeSet(CreateTime);
CREATE UNIQUE INDEX UX_ShareDataCodeSet_Code
    ON dbo.ShareDataCodeSet(Code) WHERE IsDelete IS NULL;
```

**Ghi chú:**

| Cột | Dùng để làm gì | Tại sao cần |
|---|---|---|
| `Code` | Mã bộ, VD `'TRAFFIC_COND'` | Chính là giá trị khai trong `[EshField(CodeSet = "TRAFFIC_COND")]` — sợi dây nối giữa code và CSDL |
| `Scope` | `1` = chuẩn TCVN · `2` = biến thể cho đối tác lệch chuẩn | Để lọc trên màn Bộ mã. **KHÔNG có `PartnerId`**: một bộ mã dùng được cho nhiều đối tác, và ràng buộc "đối tác nào dùng bộ mã nào" đã nằm ở `ItemsJson.codeSetId` — khai thêm ở đây là nguồn sự thật kép |
| `ValuesJson` | Danh sách cặp quy đổi | Luôn đọc cùng bộ mã, không truy vấn độc lập, mỗi bộ chỉ vài dòng ⇒ bảng con riêng không mua thêm gì |

> Chiều `INBOUND` dùng **ngược lại** cặp `sourceValue` ↔ `standardValue`. Không cần bảng riêng,
> không cần cấu hình riêng.

### 5.3 `ShareDataAlertLog` — cảnh báo & lỗi

```sql
CREATE TABLE dbo.ShareDataAlertLog (
    ID              nvarchar(64)   NOT NULL,
    OccurredAt      datetime       NULL,
    Severity        nvarchar(16)   NULL,   -- warning | error
    AlertSource     nvarchar(32)   NULL,   -- session|packet|subscription|protocol|funnel
    AlertCode       nvarchar(32)   NULL,   -- ESH-xxxx hoặc reason-cd giao thức
    PartnerId       nvarchar(64)   NULL,
    SessionId       nvarchar(64)   NULL,
    SubscriptionId  nvarchar(64)   NULL,
    DatatypeId      nvarchar(32)   NULL,
    Message         nvarchar(1000) NULL,
    DetailJson      nvarchar(max)  NULL,
    Acknowledged    bit            NULL,
    AckBy           nvarchar(64)   NULL,
    AckAt           datetime       NULL,
    Remark          nvarchar(256)  NULL,
    TenantId        nvarchar(64)   NULL,
    Code            nvarchar(64)   NULL,
    CreateTime      datetime       NULL,
    CreateUId       nvarchar(64)   NULL,
    UpdateTime      datetime       NULL,
    UpdateUId       nvarchar(64)   NULL,
    RowStatus       nvarchar(32)   NULL,
    IsDelete        datetime       NULL,
    CONSTRAINT PK_ShareDataAlertLog_ID PRIMARY KEY CLUSTERED (ID)
);
CREATE INDEX index_ShareDataAlertLog_CT ON dbo.ShareDataAlertLog(CreateTime);
CREATE INDEX IX_ShareDataAlertLog_Unacked
    ON dbo.ShareDataAlertLog(OccurredAt DESC)
    WHERE Acknowledged = 0 AND IsDelete IS NULL;
```

**Vì sao KHÔNG gộp vào `ShareDataActivityLog`:** ba cột `Acknowledged` / `AckBy` / `AckAt`
bắt bảng này phải **UPDATE được**, trong khi `ActivityLog` chứa `Hash` + `FilePath` là bằng
chứng **chỉ ghi thêm**. Ngoài ra khối lượng lệch xa (đăng ký `PERIODIC` 30 giây/lần đẩy
`ActivityLog` lên hàng triệu dòng, cảnh báo thì thưa) và chính sách lưu trữ khác nhau.

> Đặt tên `AlertSource` chứ không phải `Source` — tránh nhầm với `ActivityLog.TargetType`.

### 5.4 Bảng cũ

```sql
DROP TABLE dbo.ShareDataDataSource;       -- 0 dòng
DROP TABLE dbo.ShareDataMappingProfile;   -- 0 dòng

-- ActivityLog gánh luôn vai trò bằng chứng xuất tệp
ALTER TABLE dbo.ShareDataActivityLog ADD MappingId     nvarchar(64) NULL;
ALTER TABLE dbo.ShareDataActivityLog ADD PacketVersion nvarchar(16) NULL;

CREATE INDEX IX_ShareDataActivityLog_Transfer
    ON dbo.ShareDataActivityLog(PartnerId, DatatypeId, OccurredAt DESC)
    WHERE LogType = 'TRANSFER';

-- EventSource: nới mã gói tin cho khớp mã trong code
ALTER TABLE dbo.ShareDataEventSource ALTER COLUMN DatatypeCode nvarchar(32) NULL;
```

| Cột thêm | Dùng để làm gì | Tại sao cần |
|---|---|---|
| `MappingId` | Phễu lọc đã dùng để gửi | Đối soát: gói tin này được dịch sang ngôn ngữ đối tác theo bản phễu lọc nào |
| `PacketVersion` | Phiên bản gói tin trong code, VD `'1.0'` | Khi đối tác khiếu nại "thiếu field", cột này cho biết lúc đó hệ thống đang chạy bản gói tin nào |

`ShareDataActivityLog` **đã có sẵn** toàn bộ payload bằng chứng ở tab TRANSFER
(`SerialNbr`, `PacketNbr`, `PduType`, `Format`, `ByteSize`, `RecordCount`, `FilePath`,
`Hash`, `Status`, `ErrorMessage`), và `Action` đã khai sẵn giá trị `EXPORT`.

> ⚠ **Bảng này chỉ ghi thêm — không sửa, không xoá.** `Hash` + `FilePath` là bằng chứng đã
> gửi gì cho ai lúc nào. Nếu service nào đang UPDATE bảng này thì phải bỏ.

---

## 6. Lược đồ JSON

### 6.1 `ShareDataMapping.ItemsJson`

**Chỉ ghi field lệch chuẩn.** Field không có dòng ⇒ gửi theo chuẩn của gói tin.

```json
[
  {
    "fieldKey":     "averageSpeed",
    "targetEntity": "BB",
    "targetKey":    "vanToc",
    "targetUnit":   "m/s",
    "codeSetId":    null,
    "expression":   null,
    "defaultValue": null,
    "isExcluded":   false
  }
]
```

| Khoá | Dùng để làm gì | Tại sao cần |
|---|---|---|
| `fieldKey` | Khoá trỏ vào `[EshField("averageSpeed", …)]` trong lớp gói tin | Sợi dây duy nhất nối phễu lọc với code. ⚠ Đổi `fieldKey` trong code là hỏng phễu lọc của **mọi đối tác** — phải coi nó là hợp đồng, đổi thì phải cập nhật `ItemsJson` kèm theo |
| `targetEntity` | Lệch tên thực thể (`b` → `BB`). NULL = dùng `TargetRootEntity` | Đối tác gọi bảng khác tên |
| `targetKey` | Lệch tên field. NULL = dùng `fieldKey` | Việc chính của phễu lọc |
| `targetUnit` | Lệch đơn vị (`km/h` → `m/s`) | Gốc quy đổi lấy từ `[EshField(Unit = "km/h")]`. Không đặt được ở cấp gói tin vì mỗi đối tác đòi một đơn vị khác |
| `codeSetId` | Lệch bộ mã — trỏ vào một `CodeSet` khác bộ mã chuẩn của field | Chuẩn gửi `"slow"` nhưng đối tác đòi `"Chậm"`. Đối tác khác không bị ảnh hưởng |
| `expression` | Biến đổi riêng cho đối tác này | Lối thoát cho yêu cầu không nằm trong đơn vị hay bộ mã |
| `defaultValue` | Giá trị thay thế khi rỗng | Một số đối tác chấp nhận rỗng, số khác đòi giá trị thay thế |
| `isExcluded` | `true` = đối tác này **không nhận** field đó | Không phải đối tác nào cũng được nhận đủ mọi field. Không có khoá này thì phải làm gói tin riêng cho từng mức độ chia sẻ |

### 6.2 `ShareDataCodeSet.ValuesJson`

```json
[
  { "sourceValue": "1",  "standardValue": "slow",      "displayName": "Chậm",        "orderNo": 1 },
  { "sourceValue": "2",  "standardValue": "normal",    "displayName": "Bình thường", "orderNo": 2 },
  { "sourceValue": "3",  "standardValue": "congested", "displayName": "Ùn tắc",      "orderNo": 3 },
  { "sourceValue": null, "standardValue": "unknown",   "displayName": "Không rõ",    "isDefault": true }
]
```

| Khoá | Dùng để làm gì | Tại sao cần |
|---|---|---|
| `sourceValue` | Giá trị trong CSDL, VD `'1'` | Vế trái của phép quy đổi. Chiều `INBOUND` dùng **ngược lại** cặp này |
| `standardValue` | Giá trị gửi ra, VD `'slow'` | Vế phải — giá trị theo quy chuẩn đối tác nhận được |
| `displayName` | Nhãn tiếng Việt | Chỉ hiển thị trên giao diện quản trị, không gửi ra ngoài |
| `isDefault` | Dùng khi giá trị nguồn không khớp dòng nào | Tránh gửi ra giá trị lạ khi CSDL xuất hiện mã mới chưa kịp khai. Chỉ nên có một dòng đặt cờ này |
| `orderNo` | Thứ tự hiển thị | Sắp theo nghĩa thay vì theo mã |

---

## 7. Bảng giữ nguyên — điểm cần nhớ

### `ShareDataPartner` (24 cột, 4 dòng)

Đối tác và toàn bộ tham số kết nối C2C: `Address` · `Port` · `ProtocolProfile` (ASN | XML_A) ·
`Username` · `PasswordHash` · `InitiatorMode` · `HeartbeatMaxSec` · `DatagramSize` ·
`ResponseTimeoutSec` · `UseTls`.

Hai cột trạng thái **khác nghĩa nhau, đừng nhầm**:

- `Status` — quyết định **hành chính**: đối tác còn được phép trao đổi dữ liệu không
- `SessionState` — sự thật **vật lý**: hiện có đang kết nối không

### `ShareDataSubscription` (35 cột, 5 dòng)

Ba cột làm khoá phễu lọc: `PartnerId` · `DatatypeId` · `Direction`.
Bốn cột lập lịch có sẵn: `RunStatus` · `IntervalSeconds` · `NextTimeRun` · `LastTimeRun`.
Ba cột ngừng dùng, luôn NULL: `DataSourceId` · `MappingProfileId`.
Một cột vẫn dùng: `EventSourceId`.

### `ShareDataSession` (22 cột)

Phiên kết nối C2C và bộ đếm thời gian thực: `State` (máy trạng thái 9 trạng thái) ·
`PacketsSent` · `PacketsRecv` · `LastHeartbeatAt` · `HeartbeatRttMs`.
Không liên quan tới phễu lọc.

### `ShareDataActivityLog` (38 + 2 cột)

Hai loại bản ghi phân biệt bởi `LogType`:

| | `CONFIG` | `TRANSFER` |
|---|---|---|
| Trả lời | ai đã đổi cái gì | gói tin nào đã đi qua |
| Cột riêng | `BeforeJson` · `AfterJson` · `ChangedFields` | `SerialNbr` · `PacketNbr` · `ByteSize` · `RecordCount` · `FilePath` · `Hash` |
| Mục đích | truy trách nhiệm | đối soát và **bằng chứng** |

### `ShareDataEventSource` (14 cột)

Danh mục chủ đề NATS để kích hoạt gửi theo sự kiện. Chỉ nới kiểu `DatatypeCode`.

---

## 8. Luồng chạy

```
① Quét ShareDataSubscription đến hạn      (NextTimeRun <= now, RunStatus = IDLE)
        │  (PartnerId, DatatypeId, Direction)
② Giải ShareDataMapping (IsActive = 1)     → ItemsJson, TargetRootEntity, PacketVersion
        │
③ Gọi provider của gói tin trong CODE      → List<Packet101>
        │
④ PHỄU LỌC chiều gửi:
     ① isExcluded            bỏ field khỏi gói
     ② CodeSet CHUẨN         1 → "slow"          (từ [EshField(CodeSet)])
     ③ CodeSet ĐỐI TÁC       "slow" → "Chậm"     (từ ItemsJson.codeSetId)
     ④ targetUnit            60 km/h → 16.67 m/s (gốc từ [EshField(Unit)])
     ⑤ defaultValue
     ⑥ kiểm Required         thiếu ⇒ AlertLog, KHÔNG gửi
     ⑦ targetEntity.targetKey   BB.vanToc
        │
⑤ Gửi qua phiên của ShareDataPartner
        ├──► ShareDataActivityLog  LogType=TRANSFER · Action=SEND|EXPORT
        │                          hash · filePath · MappingId · PacketVersion
        └──► ShareDataAlertLog     chỉ khi FAILED

CHIỀU NHẬN — phễu lọc chạy ngược, từ ⑦ về ②:
   JSON đối tác → tra theo targetKey → đổi đơn vị ngược → bộ mã ngược
                → List<Packet101> → handler trong CODE ghi vào bảng nghiệp vụ
```

**Thứ tự bảy bước không được đảo.** Đặc biệt ② trước ③: bộ mã chuẩn chạy trước, bộ mã riêng
của đối tác chạy sau. Đảo là ra `1 → "Chậm"` thay vì `1 → "slow" → "Chậm"`.

Toàn bộ luồng **không đọc và không ghi cột mới nào** trên `ShareDataSubscription`
hay `ShareDataPartner`.

---

## 9. Ràng buộc và index

| Bảng | Tên | Định nghĩa | Bảo đảm điều gì |
|---|---|---|---|
| `ShareDataMapping` | `UX_ShareDataMapping_Active` | `UNIQUE (PartnerId, DatatypeId, Direction) WHERE IsActive = 1 AND IsDelete IS NULL` | Duy nhất một phễu lọc đang dùng — đây là khoá tra cứu lúc chạy |
| `ShareDataCodeSet` | `UX_ShareDataCodeSet_Code` | `UNIQUE (Code) WHERE IsDelete IS NULL` | Mã bộ mã duy nhất; đây là giá trị khai trong `[EshField(CodeSet)]` |
| `ShareDataAlertLog` | `IX_ShareDataAlertLog_Unacked` | `INDEX (OccurredAt DESC) WHERE Acknowledged = 0 AND IsDelete IS NULL` | Truy vấn mặc định của màn Cảnh báo & lỗi |
| `ShareDataActivityLog` | `IX_ShareDataActivityLog_Transfer` | `INDEX (PartnerId, DatatypeId, OccurredAt DESC) WHERE LogType = 'TRANSFER'` | Tách nhật ký truyền nhận khỏi nhật ký cấu hình |
| (mọi bảng) | `PK_<Bảng>_ID` · `index_<Bảng>_CT` | `PRIMARY KEY CLUSTERED (ID)` · `INDEX (CreateTime)` | Đúng quy ước các bảng ShareData đang có |

### Kiểm ở tầng code

Nhẹ hơn hẳn bản metadata — không còn ràng buộc nào về cấu trúc gói tin vì compiler đã lo.
Còn lại ba việc:

| Nội dung | Quy tắc |
|---|---|
| `fieldKey` trong `ItemsJson` phải tồn tại | Đối chiếu với `EshFieldRegistry.Get(packetType)` khi lưu phễu lọc. Sai chính tả thì dòng đó im lặng không có tác dụng |
| `targetKey` không trùng nhau | Trong cùng một `targetEntity`, hai field không được ánh xạ về cùng một tên |
| `ActivityLog` chỉ ghi thêm | Không có đường nào UPDATE hoặc DELETE bảng đó |

---

## 10. Ba việc phải chốt

### 10.1 Nghĩa của `OUTBOUND` — gấp nhất

```ts
// services/sharedata/subscription.types.ts
/** OUTBOUND: mình đăng ký nhận; INBOUND: đối tác đăng ký với mình. */
```
```html
<!-- views/sharedata/sharing/component/editSubscription.vue -->
<el-radio-button label="OUTBOUND">Gửi đi</el-radio-button>
```

Hai chỗ ngược nhau. Phễu lọc chiều gửi và chiều nhận xử lý **ngược nhau hoàn toàn** —
chọn nhầm là sai toàn bộ luồng. Cả 5 dòng dữ liệu hiện có đều `OUTBOUND` nên không suy ra được.

**Đề nghị:** lấy nghĩa của giao diện (`OUTBOUND` = mình gửi đi cho đối tác), sửa chú thích type.
Đồng thời thống nhất `Direction` là `nvarchar(16)` với `OUTBOUND`/`INBOUND` ở mọi bảng.

### 10.2 Danh mục `shareData_type`

Gói tin fix cứng trong code, nhưng giao diện vẫn cần danh sách để hiển thị.
Chọn một: hoặc `EshPacketCatalog` trả qua API và gỡ mục config, hoặc giữ mục config làm nhãn
hiển thị và code là nguồn sự thật về cấu trúc. **Không để cả hai cùng khai được.**

### 10.3 Mã `DatatypeId = '983'`

Cả 5 đăng ký hiện có đều mang mã này, không thuộc dải 101–111. Nếu là mã thật thì phải có
lớp gói tin `[EshPacket("983", …)]` tương ứng, nếu không 5 đăng ký đó thành mồ côi.
Nếu là dữ liệu test thì dọn.

---

## 11. Thứ tự triển khai

1. **Chốt nghĩa `OUTBOUND`** — chặn mọi việc khác
2. `CREATE` 3 bảng · `DROP` 2 bảng · `ALTER` `ActivityLog` + `EventSource` — một đợt, không migrate
3. Attribute `[EshPacket]` / `[EshField]` + `EshFieldRegistry` + `EshPacketCatalog`
   + API `GET /shareData/packet/{code}/fields`
4. `IEshFunnel` chiều gửi + unit test với dữ liệu giả
5. `Packet101` + `Packet101Provider`, chạy thử đầu–cuối với một đối tác
6. Màn **Bộ mã**, màn **Mapping**
7. Phễu lọc chiều nhận + `Packet101Handler`
8. Nhân bản provider/handler cho 10 gói tin còn lại
9. Dọn code FE: xoá `services/sharedata/canonical.types.ts`, `ICanonicalService.ts`,
   `dataSource.types.ts`, thư mục `mock/sharedata/`, `views/sharedata/dataSource/`,
   `views/sharedata/canonical/`, và các trường `@deprecated` trong `mapping.types.ts`
