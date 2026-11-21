# Vận hành QuestDB HA

Author: KienLT

Tài liệu vận hành cho thư viện `Shared.QuestHa` và công cụ dòng lệnh `QuestHa.Admin`. File cấu hình chứa kết nối phải giữ ngoài Git; mọi ví dụ dùng `<...>` thay cho giá trị thật.

## Mục lục

1. [Tổng quan](#1-tổng-quan)
2. [Checklist triển khai](#2-checklist-triển-khai)
3. [Cấu hình](#3-cấu-hình)
4. [Dùng thư viện trong code](#4-dùng-thư-viện-trong-code)
5. [Lệnh CLI](#5-lệnh-cli)
6. [Đọc/ghi và bảo đảm dữ liệu](#6-đọcghi-và-bảo-đảm-dữ-liệu)
7. [Dọn journal và backlog](#7-dọn-journal-và-backlog)
8. [Backup xuất Parquet hằng ngày](#8-backup-xuất-parquet-hằng-ngày)
9. [Backup lạnh và restore (tuỳ chọn, chạy thưa)](#9-backup-lạnh-và-restore-tuỳ-chọn-chạy-thưa)
10. [Chẩn đoán và sự cố](#10-chẩn-đoán-và-sự-cố)
11. [Chưa kiểm chứng trên hạ tầng thật](#11-chưa-kiểm-chứng-trên-hạ-tầng-thật)

---

## 1. Tổng quan

QuestDB HA giữ hai node QuestDB có dữ liệu giống nhau để đọc không gián đoạn khi một node dừng hoặc chết. Dữ liệu không ghi thẳng vào hai node mà đi qua một **journal SQL Server** làm nguồn sự thật:

```
Pipeline VDS (Worker/WPF)
   │  AppendAsync — ACK chỉ sau khi SQL COMMIT
   ▼
Journal SQL Server (DB QuestJournal riêng, nên có AG đồng bộ)
   │  replicator (đúng 1 Worker sở hữu): giao theo lô, theo từng node, có cursor
   ├──────────► Node A (QuestDB)
   └──────────► Node B (QuestDB)
                  ▲            ▲
WebAPI / Worker ──┴── router đọc có coverage ──┘
   (chốt head journal → chờ cursor + WAL của node phủ head → mới query trên node đó)
```

| Host | Vai (`QuestHaHostRoleEnum`) | Làm gì |
|---|---|---|
| Services.Vds.Worker / Services.Vds.Wpf | `Writer` | Ghi vào journal, chạy replicator, đối soát, giám sát sức khoẻ, dọn journal, **xuất Parquet hằng ngày** (khi bật, mục 8), backup lạnh + IPC vận hành. Chỉ **một** host sở hữu replicator cho một tập node. |
| TAC_WebAPI | `Reader` | Chỉ đọc qua router có coverage. Không replicator, không backup. |
| QuestHa.Admin (CLI) | `Admin` | Chạy một lệnh rồi thoát: trạng thái, đối soát/vá, dọn journal, catalog backup, xuất/nạp Parquet (`export`/`import` chạy ngay trong CLI, kéo HTTP từ node). Backup lạnh/restore/unfence gửi IPC tới Worker. Không tự tạo bảng, không điều khiển QuestDB. |

**Mặc định không đổi hành vi:** `WriteMode=Direct`, `ReadMode=Single`, `Backup.Enabled=false`, `Export.Enabled=false`. Khi đó `AddQuestHa` không đăng ký gì, không có `QuestHaManager`, Worker/WPF/WebAPI chạy đúng đường ghi/đọc cũ. HA chỉ bật khi thêm cấu hình local theo từng nấc (mục 2).

Journal bảo vệ các event **đã được SQL xác nhận commit**. Hàng đợi đầu vào Wolverine vẫn ở RAM: event chưa tới journal có thể mất khi process chết và phụ thuộc backfill từ nguồn. Đây không phải HA cho SQL Server, NATS hay Worker.

---

## 2. Checklist triển khai

Làm theo đúng thứ tự. Mỗi bước có điều kiện nghiệm thu; chưa đạt thì dừng, không sang bước sau.

### 2.1 Hạ tầng (trước khi deploy code)

- [ ] SQL Server cho DB journal riêng (không dùng chung DB nghiệp vụ), có HA đồng bộ (AG synchronous commit). Journal là phụ thuộc bắt buộc của cả đường ghi lẫn đường đọc Router: journal chết thì ghi báo lỗi và đọc trả 503 `QuestHaJournalUnavailable`.
- [ ] Hai node QuestDB **cùng phiên bản**, mỗi node chạy như **Windows service** (backup lạnh cần dừng/start service). Ghi lại tên service và đường dẫn root dữ liệu của từng node.
- [ ] Tạo bảng trên **cả hai node** bằng đúng DDL đang dùng (`show create table <bảng>` trên node hiện tại), giữ nguyên `PARTITION BY` và `DEDUP UPSERT KEYS`. Replicator không tự tạo bảng. Khoá DEDUP phải khớp khoá của `QuestTableSpec`, lệch là đối soát báo sai.
- [ ] Đồng bộ giờ (NTP) cho máy Worker, WebAPI, SQL Server và hai node.
- [ ] Mở cổng: Worker/WebAPI tới PG wire của cả hai node và tới SQL Server journal; CLI chạy trên máy Worker.
- [ ] Tài khoản Windows chạy Worker: quyền dừng/start service QuestDB của node cùng máy (cấp riêng, không dùng quyền admin chung), đọc root node, ghi `BackupRoot`, ghi `Backup.StateDirectory` (đĩa local, ngoài root node và backup root).
- [ ] `BackupRoot` (local hoặc NAS/SMB): kiểm quyền ghi, quota và dung lượng cho số bản giữ (7 ngày + 4 tuần + 7 bản tay mỗi node). Không dùng symlink/DFS/reparse point. (Backup lạnh là tuỳ chọn khi đã bật Export Parquet.)
- [ ] **Export Parquet** (mục 8): `ExportRoot` (local/UNC, không symlink) đủ ≈ 332 MB/ngày × `retention_days` (90 ngày ≈ 30 GB; ngày đỉnh ≈ 963 MB) và tài khoản Worker/CLI ghi được; Worker/CLI tới được cổng HTTP REST của node (mặc định 9000, `http.bind.to`; Basic auth nếu bật); trên node cần nạp ngược khai `cairo.sql.copy.root` trong `server.conf` (restart node) và cho máy chạy CLI ghi được vào thư mục đó.

### 2.2 Migration

- [ ] **Dừng Worker phiên bản cũ** (cả purge cũ) trước khi chạy migration `PurgeState`; giữ dừng tới khi deploy mã mới xong. Nếu để chạy, DELETE của bản cũ không cập nhật biên purge bền.
- [ ] DB journal (`QuestJournal`), chạy lần lượt, Windows authentication, câu lệnh liền một dòng:

  ```powershell
  sqlcmd -S <server> -d QuestJournal -E -C -f 65001 -I -b -i <file>
  ```

  | Thứ tự | File | Ghi chú |
  |---|---|---|
  | 1 | `20261027_QuestJournal_Create.sql` | Tạo bảng journal |
  | 2 | `20261028_QuestBackupCatalog_Add_Retired.sql` | Cột `RetiredUtc`, luỹ đẳng |
  | 3 | `20261028_QuestJournal_Add_PurgeState.sql` | Biên purge bền, luỹ đẳng |
  | 4 | `20261102_QuestJournal_Add_ExportCatalog.sql` | Catalog bản xuất Parquet `QuestExportCatalog`, luỹ đẳng; **trước** khi bật `Export:Enabled` |

  Nghiệm thu: có đủ bảng `QuestOutbox`, `QuestNodeCursor`, `QuestNodeStatus`, `QuestQuarantine`, `QuestBackupCatalog` (cột `RetiredUtc`), `QuestJournalPurgeState`, `QuestExportCatalog`.
- [ ] DB nghiệp vụ VDS (không phải DB journal), cùng cách chạy; cả hai script luỹ đẳng, chạy được trước hoặc sau deploy:

  | File | Việc |
  |---|---|
  | `20261031_VdsConfig_Add_QuestHa.sql` | Seed 41 tham số vận hành `vds_questha_*` (mục 3.2) cho tenant sở hữu (`@tenant`) |
  | `20261031_SysTerminology_Add_QuestHaConfig.sql` | Bản dịch 6 thông báo lỗi khi lưu tham số; sau đó xoá cache sysTerminology và bấm ↻ từng ngôn ngữ |
  | `20261102_VdsConfig_Add_QuestHaExport.sql` | Seed 8 mã `vds_questha_export_*` (mục 3.2) và đổi `vds_questha_journal_retention` 8d → **3d** (chỉ dòng còn đúng `8d`). Chạy **cùng/sau** khi deploy bản code có Export: bản cũ không biết mã `export_*` sẽ từ chối cả gói QuestHa. Không có thông báo lỗi mới ⇒ không cần script SysTerminology bổ sung. |

  **Dòng VdsConfig thắng appsettings.** Trước khi chạy seed ở môi trường khác, sửa khối `@src` cho khớp giá trị đang chạy của môi trường đó (đặc biệt `Health.BackupRequired`, `Backup.DryRun`, `JournalMaintenance`). Bản seed mặc định đặt `vds_questha_health_backup_required=1` và `vds_questha_backup_dry_run=1`.
- [ ] Tài khoản ứng dụng có SELECT/INSERT/UPDATE/DELETE trên các bảng journal, quyền app lock và đọc `sys.dm_db_partition_stats`. Không tự cấp quyền rộng hơn.

### 2.3 Cấu hình

- [ ] Kết nối (journal, PG từng node) để ở file cấu hình local hoặc biến môi trường, không commit, không truyền mật khẩu trên dòng lệnh. Vị trí từng host: mục 3.1. Mẫu: `config/worker.QuestHa.journal.sample.json`, `config/webapi.QuestHa.router.sample.json` trong gói release.
- [ ] Tên node (`Nodes[].Name`) giống hệt nhau ở Worker, WebAPI và file cấu hình CLI: tên là khoá cursor/status, đổi tên = node mới.
- [ ] Chỉ **một** Worker sở hữu replicator cho tập node. Không chạy hai host Worker/WPF với cùng tập node. WebAPI chỉ `ReadMode=Router`.
- [ ] `Backup.StateDirectory` là đường dẫn tuyệt đối; `NodeControls` chỉ khai node **cùng máy** với Worker (adapter hiện chỉ điều khiển service Windows local).

### 2.4 Bật theo từng nấc

| Nấc | Cấu hình Worker | Nghiệm thu trước khi lên nấc sau |
|---|---|---|
| a. Direct | `WriteMode=Direct` (mặc định) | Mã mới chạy như cũ, không lỗi khởi động. |
| b. Shadow | `WriteMode=Shadow`, `ReadMode=Single`, `Nodes` = chỉ node phụ, `SourceNode` = node chính, **`BackupRequired=false`** | Chạy vài ngày; `status` exit 0, Backlog/Health Ok; `compare` node chính ↔ phụ Equal theo cửa sổ đã ổn định. |
| c. Journal | `WriteMode=Journal`, `ReadMode=Router`, `Nodes` = cả hai node, vẫn **`BackupRequired=false`** | `status` exit 0; cursor hai node bắt kịp; Health Ok; diễn tập dừng một node có kế hoạch (đọc chuyển node, ghi không lỗi, node quay lại bắt kịp). |
| d. Export Parquet | Migration catalog; `Export:Enabled=true`, `ExportRoot`, `Nodes[].HttpUrl` (+ `ParquetCopyRoot`); VdsConfig `vds_questha_export_dry_run=1` trước | `export --day <hôm qua>` preview đạt (node phụ, số dòng, trống đĩa); log DryRun 02:00 đúng; đặt `export_dry_run=0` ⇒ sáng hôm sau `export-list` có 2 dòng Verified, `status` không `export.old`; `import --dir … --node <node thử> --into <bảng thử>` khớp số dòng; `purge` (dry-run) đếm được dòng của ngày đã xuất. Sau đó đặt `BackupRequired=true`. |
| e. Backup lạnh (tuỳ chọn, chạy thưa) | `Backup.DryRun=true` (VdsConfig `vds_questha_backup_dry_run=1`) rồi `Backup.Enabled=true` | `backup-run --node X` preview đạt; một bản `--apply` thật Verified (có manifest); `restore --node X --id <id>` preview báo `OK DryRun ContentVerified`; restore thử trên môi trường thử, không trên production. Lịch thưa ⇒ nới `health_backup_warn_after/critical_after` (8.5). |

- `WriteMode=Journal` với `ReadMode=Single` bị từ chối khi khởi động. `ReadMode=Router` ở Worker cũng đòi `WriteMode=Journal`.
- Bật **WebAPI** `ReadMode=Router` chỉ sau khi Worker đã ổn định ở nấc c. Mỗi request đọc giữ một kết nối PG trong lúc chờ coverage (tối đa `CoverageWait`, thử node thứ hai thêm một lần) và mở thêm kết nối probe WAL ⇒ đo số kết nối PG QuestDB và độ trễ API ở giờ cao điểm, chỉnh `Maximum Pool Size` của chuỗi kết nối node nếu cần.
- Giới hạn đã biết của đường đọc: xem mục 10.3.

**`BackupRequired` theo nấc.** Mặc định code, `appsettings.json` của Worker và bản seed VdsConfig đều là `true`: chưa có catalog backup thì Health `Critical` và `status` exit `2`. Ở nấc b và c chưa triển khai backup nên **phải đặt `false`** thì tiêu chí "`status` exit 0" mới đạt:

- Worker/WPF: dòng VdsConfig `vds_questha_health_backup_required` = `0` (nếu đã seed) hoặc `QuestHa:Health:BackupRequired=false` trong `Configuration/Database.json` (nếu chưa seed). Có dòng DB thì dòng DB thắng.
- CLI: `QuestHa:Health:BackupRequired=false` trong file `--config`. CLI không đọc VdsConfig.

Chỉ đặt lại `true` **sau khi** có bản backup Verified đầu tiên: bản xuất Parquet Verified (nấc d, khi `Export:Enabled=true`) hoặc backup lạnh Verified của từng node (nấc e); trạng thái cuối của production là `true`.

### 2.5 Theo dõi

- [ ] Log `[QuestHa]` mức Warning/Error từ Health worker (heartbeat, Down, CatchingUp kéo dài, WAL suspend, backlog, backup cũ) đưa vào hệ thống cảnh báo.
- [ ] `QuestHa.Admin status` định kỳ: exit `2` nghĩa là Health/Backlog từ Warn trở lên.
- [ ] Khi cần điều tra, bật Debug (mục 10.1); tắt lại sau khi xong.

### 2.6 Quay lui

- [ ] Từ Journal về Shadow/Direct: dừng nguồn ghi, chờ `status` báo pending của node chính bằng 0, dừng Worker, đổi cấu hình rồi start. Quay lui khi còn pending thì node chính thiếu các event đã ACK chỉ nằm trong journal.
- [ ] Không xoá DB journal khi quay lui: nó là bằng chứng và là nguồn để phát lại.

---

## 3. Cấu hình

Hai loại, tách bạch:

- **Hạ tầng** (3.1): kết nối, chế độ, thư mục. Đặt trong file cấu hình hoặc biến môi trường; **đổi phải restart** host.
- **Vận hành** (3.2): ngưỡng, lịch, số bản giữ. Đặt trong VdsConfig; **đổi lúc chạy**, không restart Worker.

### 3.1 Hạ tầng — đặt ở đâu

Mọi host đọc section `QuestHa`.

| Host | File cấu hình | Biến môi trường |
|---|---|---|
| Services.Vds.Worker | `Configuration/Database.json` cạnh exe (bắt buộc có file; Worker chỉ nạp `appsettings.json`, `Configuration/Database.json`, `DataTransporter.json`, `Logging.json` — file đặt tên khác **không** được đọc). Giá trị mặc định nằm ở `appsettings.json`. | Không tiền tố: `QuestHa__JournalConnectionString`, `QuestHa__Nodes__0__PgConnectionString`, `QuestHa__Nodes__1__PgConnectionString`. Biến được nạp lại sau các file nên đè được file. |
| Services.Vds.Wpf | `Configuration/Database.json` (và `Database.<Environment>.json`) | Không tiền tố, nhưng các file Configuration nạp **sau** biến môi trường nên đè biến: không khai trùng khoá trong file nếu muốn dùng biến. |
| TAC_WebAPI | File riêng, ví dụ `Configuration/QuestHa.json` (Furion quét mọi `*.json` trong `Configuration/`). Thiếu section hoặc `ReadMode=Single` = đường cũ. | Không tiền tố, ví dụ `QuestHa__Nodes__0__PgConnectionString` (IIS: `environmentVariables` trong `web.config`). Không khai trùng khoá ở cả file và biến. |
| QuestHa.Admin | `--config <file>` JSON có section `QuestHa` | **Tiền tố `QUESTHA_`**, ví dụ `QUESTHA_QuestHa__JournalConnectionString`, `QUESTHA_QuestHa__SourceNode__PgConnectionString`, `QUESTHA_QuestHa__Nodes__0__PgConnectionString`. Biến đè file. |

CLI chỉ in loại exception khi lỗi, không in cấu hình hay chuỗi kết nối.

**Khoá hạ tầng** (mặc định trong ngoặc):

| Khoá `QuestHa:` | Ý nghĩa |
|---|---|
| `WriteMode` (`Direct`) | `Direct` ghi như cũ; `Shadow` ghi node chính rồi sao chép journal bất đồng bộ; `Journal` ACK sau SQL commit. |
| `ReadMode` (`Single`) | `Single` đường đọc cũ (cầu dao + probe); `Router` đọc có coverage. |
| `JournalConnectionString` | DB journal riêng. Bắt buộc khi `WriteMode` khác `Direct` và cho CLI. |
| `Nodes[]` | `Name`, `PgConnectionString`, `Priority` (nhỏ = ưu tiên đọc hơn); cho Export: `HttpUrl`, `HttpUser`/`HttpPassword` (mật khẩu qua biến môi trường), `ParquetCopyRoot` (8.2). PG wire tự thêm `ServerCompatibilityMode=NoTypeLoading` nếu thiếu. Shadow: chỉ node phụ. Journal: cả hai node. Không trùng tên. |
| `SourceNode` | Shadow: node chính ngoài `Nodes`, chỉ phục vụ đối soát/vá, không được trùng tên node phụ. Không đưa vào `Nodes` vì replicator ghi lên mọi node trong danh sách. |
| `CommandTimeoutSeconds` (10) | Thời gian chờ một lệnh SQL/PG. |
| `SettingsTenantId` (trống) | Tenant **sở hữu** tham số vận hành ở 3.2 cho host này. Trống ⇒ Worker/WPF dùng `Service:TenantId`, WebAPI dùng `Default`. Worker và WebAPI cùng cụm phải ra cùng một tenant. |
| `Journal:AppendBatching` (true), `AppendMaxBatch` (200), `AppendQueueCapacity` (10000) | Ghi journal theo lô; hàng đợi đầy thì từ chối ngay (không ACK). |
| `Replicator:*` | `PollInterval` 300ms, `BatchSize` 500, `RewindOnReconnect` 2 phút, `BackoffInitial` 500ms, `BackoffMax` 10s, `ReadyMaxLag` 2s, `StatusWriteInterval` 1s, `ProbeInterval` 2s. |
| `Reconciler:*` | `SettleDelay` 3s, `WalApplyWait` 5s, `Lookback` 30 phút, `AutoInterval` 30s (`null` = chỉ chạy tay). |
| `Router:*` (không phải ba khoá ở 3.2) | `StatusCacheTtl` 2s, `HeartbeatStaleAfter` 10s, `RequireReconcileForFailback` true, `AllowDegradedRead` false, `StatusRefreshTimeout` 2s, `FallbackProbeInterval` 2s, `FallbackMaxAge` 10 phút, `SwitchHistorySize` 50. |
| `JournalMaintenance:Enabled` (true), `AllowPurgeWithoutVerifiedBackup` (false) | Bật/tắt lịch dọn; cờ nguy hiểm bỏ riêng điều kiện thiếu backup Verified — **chỉ khi Export tắt** (Export bật thì cờ không có tác dụng, 8.4). |
| `Backup:Enabled` (false) | Bật lịch backup tự động. |
| `Backup:BackupRoot`, `NodeBackupRoots` | Root backup (local/UNC) và root riêng theo node. |
| `Backup:StateDirectory` | Dấu dừng node; bắt buộc đường dẫn tuyệt đối khi bật `Enabled` hoặc `AdminPipeName`. Mẫu: `C:/ProgramData/ITS015/QuestHa/backup-state`. |
| `Backup:NodeControls` | `{ "A": { "RootPath": "<root dữ liệu node A>", "ServiceName": "<Windows service QuestDB node A>" } }` — chỉ node cùng máy Worker. |
| `Backup:AdminPipeName` | Tên named pipe cho IPC CLI ↔ Worker (rỗng = tắt). Pipe dùng `CurrentUserOnly`: tài khoản CLI phải trùng tài khoản chạy service. |
| `Backup:PollInterval` (30s), `NodeStopTimeout` (60s), `NodeStartTimeout` (90s) | Nhịp lịch; chờ dừng/start service. |
| `Export:Enabled` (false), `ExportRoot`, `HttpTimeout` (10m), `CoverageWait` (2m), `PollInterval` (1m), `MinimumFreeBytes` (1 GiB) | Backup xuất Parquet hằng ngày (8.2). |

### 3.2 Vận hành — qua VdsConfig

Nhóm `vds_config`, mã `vds_questha_*` (49 mã: 41 của `20261031_VdsConfig_Add_QuestHa.sql` + 8 `export_*` của `20261102_VdsConfig_Add_QuestHaExport.sql`), sửa trên web bằng CRUD cấu hình VDS (`vdsConfig`). Giá trị mặc định của bảng dưới = giá trị seed = giá trị trong `appsettings.json` của Worker (Router và ba mã Health nhịp cảnh báo/đối soát: mặc định trong code — trùng nhau).

**Tenant sở hữu.** Router và snapshot tham số là singleton toàn host, nên mỗi host chỉ đọc dòng `vds_questha_*` của **một** tenant: `QuestHa:SettingsTenantId` (3.1); trống ⇒ Worker/WPF dùng `Service:TenantId`, WebAPI dùng `Default`. Dòng của tenant khác vẫn lưu được nhưng **không được áp** lên host nào. Script seed phải chạy với `@tenant` = tenant sở hữu.

**Định dạng:** thời lượng **bắt buộc hậu tố** `s`/`m`/`h`/`d` (`30s`, `5m`, `36h`, `3d`; số trần bị từ chối, không hiểu là phút); số nguyên và byte là số trần; cờ `1`/`0` (`true`/`false`/`on`/`off`); giờ `HH:mm`.

Tiền tố `vds_questha_` được lược khỏi cột đầu để bảng gọn.

**Health**

| Mã (`vds_questha_…`) | Ý nghĩa | Mặc định | Biên |
|---|---|---|---|
| `health_check_interval` | Chu kỳ đánh giá sức khoẻ | 30s | 5s..1h |
| `health_heartbeat_stale_after` | Không thấy heartbeat replicator quá lâu ⇒ Critical | 30s | 5s..1h |
| `health_lag_warn_seconds` | Tuổi pending cũ nhất ⇒ Warn | 10s | 1s..1d; < Critical |
| `health_lag_critical_seconds` | Tuổi pending cũ nhất ⇒ Critical | 60s | 1s..1d |
| `health_backup_warn_after` | Backup Verified cũ hơn ⇒ Warn | 36h | 1h..30d; < Critical |
| `health_backup_critical_after` | Backup Verified cũ hơn ⇒ Critical | 3d | 1h..30d |
| `health_backup_required` | Chưa có backup của node = Critical | 1 | 1/0 |
| `health_reconcile_warn_after` | Lần đối soát gần nhất cũ hơn ⇒ Warn | 15m | 1m..7d |
| `health_repeat_warning_every` | Trạng thái xấu kéo dài ⇒ nhắc lại cảnh báo (tăng mức vẫn báo ngay) | 10m | 1m..1d |
| `health_min_alert_gap` | Khoảng nghỉ tối thiểu giữa hai cảnh báo khi mức không tăng (chống nhảy Ok/Warn) | 1m | 1s..1h |

**Dọn journal**

| Mã (`vds_questha_…`) | Ý nghĩa | Mặc định | Biên |
|---|---|---|---|
| `journal_interval` | Chu kỳ dọn | 5m | 10s..1d |
| `journal_retention` | Tuổi tối thiểu của dòng đã giao trước khi xoá | 3d (trước 02/11/2026: 8d) | 1h..30d; dòng còn phải nằm trong bản xuất/backup lạnh Verified (8.4) |
| `journal_protected_backup_days` | Số ngày backup Verified còn ghim journal | 8 | 1..3650 |
| `journal_batch_size` | Dòng mỗi lô DELETE | 200 | 1..500 |
| `journal_max_rows_per_run` | Trần dòng mỗi lượt | 2000 | 1..1 000 000 |
| `journal_max_run_duration` | Trần thời gian mỗi lượt | 5s | 1s..1m |
| `journal_alert_throttle` | Giãn cách cảnh báo backlog | 5m | 10s..1d |
| `journal_warn_pending_rows` / `journal_critical_pending_rows` | Ngưỡng pending mỗi node | 100000 / 1000000 | 1..10¹²; Warn < Critical |
| `journal_warn_pending_age` / `journal_critical_pending_age` | Ngưỡng tuổi pending | 30s / 5m | 1s..30d; Warn < Critical |
| `journal_warn_bytes` / `journal_critical_bytes` | Ngưỡng dung lượng journal (byte) | 10737418240 / 53687091200 | 1..10¹⁵; Warn < Critical |
| `journal_dry_run` | Lịch nền chỉ đếm, không xoá | 0 | 1/0 |

**Backup**

| Mã (`vds_questha_…`) | Ý nghĩa | Mặc định | Biên |
|---|---|---|---|
| `backup_schedule` | Giờ backup tự động (giờ server) | off | `HH:mm` hoặc `off` (tắt lịch) |
| `backup_weekly_day` | Ngày tạo bản weekly | Sunday | tên ngày (không nhận số) |
| `backup_window_start` / `backup_window_end` | Cửa sổ được phép backup | 01:00 / 05:00 | `HH:mm`; không trùng nhau; đầu > cuối = qua nửa đêm |
| `backup_node_offset` | Lệch giờ giữa các node | 30m | 1m..23h; slot mọi node phải trong ngày và trong cửa sổ |
| `backup_max_attempts` | Số lần thử mỗi slot | 3 | 1..10 |
| `backup_retry_delay` | Chờ giữa hai lần thử | 5m | 10s..7d |
| `backup_daily_copies` / `backup_weekly_copies` / `backup_manual_copies` | Số bản giữ | 7 / 4 / 7 | 7..365 / 4..520 / 1..100 |
| `backup_max_stopped_duration` | Thời gian tối đa node bị dừng | 30m | 1m..1d |
| `backup_min_throughput_bytes_per_second` | Thông lượng tối thiểu để dự toán trước khi dừng | 10485760 | 1..10¹³ |
| `backup_minimum_free_bytes` | Dung lượng trống tối thiểu | 1073741824 | 0..10¹⁵ |
| `backup_dry_run` | Lịch tự động chỉ thử, không dừng node | 1 | 1/0 |

**Export Parquet** (mục 8; bật/tắt, thư mục là hạ tầng `Export:*`)

| Mã (`vds_questha_…`) | Ý nghĩa | Mặc định | Biên |
|---|---|---|---|
| `export_schedule` | Giờ xuất hằng ngày (giờ server), xuất trọn hôm qua | 02:00 | `HH:mm` (không có `off`: tắt bằng `Export:Enabled`) |
| `export_retention_days` | Số ngày giữ thư mục xuất (ngày Verified mới nhất mỗi bảng luôn giữ) | 90 | 7..3650 |
| `export_recheck_days` | Số ngày trước được kiểm lại để bắt dữ liệu đến muộn / xuất bù | 1 | 0..6 |
| `export_max_attempts` | Số lần thử trong ngày | 6 | 1..20 |
| `export_retry_delay` | Chờ giữa hai lần thử | 10m | 1m..6h |
| `export_warn_after` / `export_critical_after` | Ngày xuất mới nhất cũ hơn ⇒ Warn / Critical | 26h / 50h | 1h..30d; Warn < Critical |
| `export_dry_run` | Lịch chỉ xem trước, không ghi file/catalog | 1 | 1/0 |

**Router đọc**

| Mã (`vds_questha_…`) | Ý nghĩa | Mặc định | Biên |
|---|---|---|---|
| `router_coverage_wait` | Ngân sách chờ cursor/WAL phủ head mỗi node | 3s | 1s..1m (0 bị chặn vì mọi đọc sẽ "không phủ") |
| `router_failback_stable_for` | Node ưu tiên phải Ready liên tục bao lâu mới failback | 30s | 1s..1d |
| `router_breaker_cooldown` | Chặn node sau lỗi kết nối rồi cho một lượt thăm dò | 5s | 1s..10m |

**Không đưa vào VdsConfig** (hạ tầng, đổi phải restart): kết nối, `Nodes` (kể cả `HttpUrl`/`HttpUser`/`HttpPassword`/`ParquetCopyRoot`), `Export:Enabled`/`ExportRoot`/`HttpTimeout`/`CoverageWait`/`PollInterval`/`MinimumFreeBytes`, `SourceNode`, `WriteMode`/`ReadMode`, `Enabled` của Backup/JournalMaintenance, `BackupRoot`/`NodeBackupRoots`/`StateDirectory`, `NodeControls`, `AdminPipeName`, `PollInterval`, `NodeStopTimeout`/`NodeStartTimeout`, `Replicator`/`Reconciler`/`Journal`, `AllowPurgeWithoutVerifiedBackup` (cờ an toàn), `CommandTimeoutSeconds`, phần còn lại của Router (`StatusCacheTtl`, `HeartbeatStaleAfter`, `RequireReconcileForFailback`, `AllowDegradedRead`, `StatusRefreshTimeout`, `FallbackProbeInterval`, `FallbackMaxAge`, `SwitchHistorySize` — dựng một lần trong nguồn trạng thái/fallback của router và gắn với tính đúng của coverage, không phải ngưỡng tinh chỉnh).

**Áp lúc chạy như thế nào**

- **Worker/WPF:** khi khởi động, sau mỗi lần nạp lại danh mục VdsConfig (chu kỳ hoặc refresh NATS khi lưu trên web), cả gói 49 mã của tenant sở hữu được kiểm rồi áp nguyên tử qua `QuestHaManager.ApplySettings`. Gói đã áp được lưu vào `config-overrides.json` để dùng lại nếu lần nạp sau không đọc được DB.
- **WebAPI:** chỉ áp nhóm **router** (`router_*`) cho đường đọc của nó — lúc khởi động (tenant sở hữu; DB chưa đọc được thì dùng appsettings và thử lại mỗi phút) và ngay sau khi thêm/sửa/xoá qua API `vdsConfig` một dòng mà mã **cũ hoặc mới** là `router_*` của tenant sở hữu (đổi một dòng router sang mã khác ⇒ router về mặc định). Không có API xoá hàng loạt cho `vdsConfig`. Health/dọn journal/backup không chạy ở WebAPI nên giữ cấu hình file. **Sửa thẳng DB ngoài API (vd chạy script seed) ⇒ restart WebAPI** để đọc lại router.
- **CLI:** không đọc VdsConfig; `status`, `purge` dùng giá trị trong file `--config` (tức ngưỡng của CLI có thể khác ngưỡng đang chạy ở Worker).
- Host đang ở `Direct`/`Single` (không có Manager) bỏ qua mọi mã `vds_questha_*`.
- **Thứ tự đọc → áp.** Mỗi lượt lấy *vé đọc* tăng dần **trước** khi truy vấn DB; kết quả về muộn của lượt bắt đầu sớm hơn lượt đã áp bị bỏ (log Debug). Nhờ vậy hai lần lưu gần nhau, khởi động và refresh chạy chồng nhau không để RAM giữ bộ cũ hơn DB.
- **Hiệu lực từ vòng sau.** Mỗi service chụp bộ tham số một lần đầu mỗi vòng/lượt: purge đang chạy, backup đang chép, lượt chờ coverage, lượt health đang làm giữ bộ cũ tới hết.

**Sai hoặc thiếu**

| Tình huống | Kết quả |
|---|---|
| Thiếu dòng | Dùng giá trị khởi động (appsettings hoặc mặc định trong code). Xoá dòng trên web = quay về giá trị khởi động. |
| **Một** giá trị sai (thiếu hậu tố, sai định dạng, ngoài biên, Warn ≥ Critical — kể cả khi chỉ khai một nửa cặp so với mặc định —, `HH:mm` sai, cửa sổ đầu = cuối, slot node ngoài ngày/cửa sổ, mã `vds_questha_*` lạ) | **Từ chối cả gói**, giữ bộ đang chạy, ghi Warning liệt kê lý do. |
| Lưu/tắt/xoá trên web | WebAPI dựng **bộ sẽ có hiệu lực** sau thao tác (dòng đang bật của tenant + giá trị mới, mã thiếu = mặc định, slot tính cho tối thiểu 2 node) và kiểm bằng **cùng luật** Manager dùng khi áp. Sai ⇒ từ chối lưu, báo tại form: `questHaWarnNotBelowCritical` (cặp, kể cả khi xoá/tắt Critical làm Warn va mặc định), `questHaBackupWindowInvalid`, `questHaBackupSlotOutsideWindow` (vd lịch `23:00` với cửa sổ 01:00–05:00), còn lại `questHaValueInvalid`. Chỉ kiểm khi dòng trước hoặc sau thao tác là mã QuestDB HA. |
| Áp thành công | Log Information `[QuestHa] Áp tham số vận hành (hiệu lực từ vòng sau): Tên: cũ → mới; …`. Gói không đổi gì chỉ ghi Debug. |

---

## 4. Dùng thư viện trong code

Thư viện có **một cửa vào**: `QuestHaManager`. Mọi thành phần khác (journal, replicator, router, backup…) là `internal`. Public còn lại: `AddQuestHa`, `QuestHaHostRoleEnum`, `Options/*`, DTO/enum vào-ra của Manager, `QuestTableSpec`/`QuestColumn`/`QuestColumnTypeEnum`, `QuestHaUnavailableException`, `INodeControl`.

### 4.1 Đăng ký theo vai

```csharp
services.AddQuestHa(configuration, VdsBaseHelper.QuestTableHp.All, QuestHaHostRoleEnum.Writer); // Worker/WPF
services.AddQuestHa(configuration, VdsBaseHelper.QuestTableHp.All, QuestHaHostRoleEnum.Reader); // WebAPI
services.AddQuestHa(config,        VdsBaseHelper.QuestTableHp.All, QuestHaHostRoleEnum.Admin);  // CLI
```

`AddQuestHa` đọc section `QuestHa`, kiểm hợp lệ rồi đăng ký; cấu hình sai ném `InvalidOperationException` ngay lúc đăng ký.

| Vai | Điều kiện đăng ký | Đăng ký |
|---|---|---|
| Writer | `WriteMode=Direct` ⇒ **không đăng ký gì**. Ngược lại phải có `JournalConnectionString`, `Nodes` ≥ 1, `AppendQueueCapacity` > 0, `AppendMaxBatch` > 0; `Journal` thêm `Nodes` ≥ 2 và `ReadMode=Router`; `ReadMode=Router` đòi `WriteMode=Journal`. | Cluster, router, reconciler, backup, health, dọn journal + các hosted worker; Shadow thêm worker sao chép. |
| Reader | `ReadMode` khác `Router` ⇒ **không đăng ký gì**. `Router` cần `JournalConnectionString` và `Nodes` ≥ 2. | Router đọc có coverage; không hosted worker. |
| Admin | `JournalConnectionString` bắt buộc. | Dịch vụ cho lệnh vận hành; không worker nền. |

Không có Manager (HA tắt) thì consumer nhận `null` và đi đường cũ:

```csharp
public class SomeRepository(QuestHaManager? questHa = null)
{
    // Journal: ghi vào journal, ACK sau SQL commit
    if (questHa is { WriteMode: QuestWriteModeEnum.Journal }) await questHa.AppendAsync(table, entity, ct);
    // Shadow: ghi node chính trước, rồi sao chép
    else { await WriteDirectAsync(entity, ct); questHa?.CopyToShadow(table, entity); }

    // Router: đọc có coverage; Single: đường đọc cũ
    var rows = questHa is { ReadMode: QuestReadModeEnum.Router }
        ? await questHa.ReadAsync(db => db.Queryable<Row>().ToListAsync(), ct)
        : await ReadDirectAsync(ct);
}
```

### 4.2 Method của `QuestHaManager`

Method không dùng được ở vai/chế độ hiện tại ném `InvalidOperationException("QuestHaManager.<Method> không dùng được ở vai <Role> …")`. Lỗi tham số vận hành (node sai/trùng, nguồn = đích, `apply` ngoài Shadow/`SourceNode`, thiếu `AdminPipeName`) ném `ArgumentException`.

| Method | Vai | Việc |
|---|---|---|
| `AppendAsync(table, entity, ct)` | Writer, `WriteMode=Journal` | Kiểm **vai Writer trước** (Reader/Admin ném `InvalidOperationException` kể cả khi `WriteMode=Journal`). Ghi một event; hoàn tất khi SQL đã COMMIT (điểm ACK). Lỗi/huỷ lan ra cho caller retry, không ghi thẳng QuestDB. |
| `CopyToShadow(table, entity)` | Writer, `WriteMode=Shadow` | Chụp JSON ngay rồi đưa vào hàng đợi sao chép; không ném lỗi sao chép ra pipeline (đầy hàng đợi/lỗi journal chỉ đếm và cảnh báo có throttle). |
| `ReadAsync<T>(query, ct)` | Writer/Reader, `ReadMode=Router` | Chốt head journal một lần, chạy `query` trên lease của node đã phủ head. Lỗi ⇒ `QuestHaUnavailableException`; huỷ ⇒ `OperationCanceledException`; lỗi khác của query ném nguyên. |
| `GetStatusAsync(ct)` | mọi vai có journal | `QuestHaStatusOutput { Backlog, Health }`. |
| `CompareAsync(input, ct)` | Admin | So sánh một bảng giữa hai node (chỉ đọc). |
| `PatchAsync(input, ct)` | Admin | Vá node đích từ nguồn; xem trước khi `Apply=false`; `Apply` giữ khoá vận hành suốt các bảng. |
| `PurgeAsync(apply, ct)` | Admin | Dọn journal; `apply=false` chỉ đếm. |
| `BackupAsync(node, apply, ct)` | Admin | IPC tới Worker: backup lạnh một node; `apply=false` chỉ tiền kiểm. |
| `RestoreAsync(node, id, apply, allowJournalGap, ct)` | Admin | IPC tới Worker: restore từ bản `id`. |
| `UnfenceNodeAsync(node, apply, ct)` | Admin | IPC tới Worker: gỡ rào thủ công. |
| `ListBackupsAsync(node?, ct)` | Admin | Catalog backup, mới nhất trước. |
| `RetireBackupAsync(id, retired, apply, ct)` | Admin | Retire/unretire một bản; không retire bản Verified mới nhất của node. |
| `RetireCursorAsync(node, staleMinutes, forceLive, apply, ct)` | Admin | Gỡ cursor của node đã bỏ khỏi `Nodes`. |
| `ExportAsync(DateOnly dayLocal, apply, ct)` | Writer/Admin (Reader ⇒ `InvalidOperationException`) | Xuất (hoặc kiểm lại nếu đã có) trọn ngày giờ server ra Parquet theo giờ (mục 8). `apply=false` xem trước: node chọn, số dòng dự kiến từng giờ, dung lượng ước, trống đĩa (≥ ước × 2). Hoãn ⇒ `DeferredReason` (`NoReadyNode`, `NotCovered`, `InsufficientDisk`, `DayNotFinished`, `ExportAlreadyRunning`, `TableFailed`); `Completed` = mọi bảng Verified. |
| `ListExportsAsync(table?, from?, to?, ct)` | Writer/Admin | Catalog `QuestExportCatalog`, ngày mới nhất trước. |
| `ImportAsync(dayFolder, node, apply, ct)` / `ImportAsync(dayFolder, node, apply, intoTable, ct)` | Writer/Admin | Nạp ngược một thư mục ngày (⇒ phiên bản đã công bố mới nhất) hoặc thư mục phiên bản (8.6): SHA-256 theo manifest trước, file trong `ParquetCopyRoot` (tự chép vào thư mục phiên riêng nếu cần, cây đích không qua link), `INSERT … read_parquet` từng giờ, báo số dòng trước/sau; WAL chưa áp xong ⇒ `Error=WalNotApplied`. |
| `ApplySettings(QuestHaRuntimeOptions)` | mọi vai có Manager | Áp tham số vận hành (mục 3.2) coi như vừa đọc xong; trả `true` khi nhận, `false` khi từ chối cả gói; `null` ném `ArgumentNullException`. |
| `BeginSettingsRead()` + `ApplySettings(options, readTicket, out superseded)` | mọi vai có Manager | Host đọc cấu hình từ DB: lấy vé **trước** khi truy vấn, áp kèm vé. Vé cũ hơn bộ đang chạy ⇒ `false`, `superseded=true` (không phải lỗi cấu hình). |
| `QuestHaRuntimeOptions.ValidateEffective(baseline)` / `Effective(baseline)` | (không cần Manager) | Kiểm/dựng bộ có hiệu lực bằng đúng luật `ApplySettings` — dùng khi lưu cấu hình để không lưu được bộ Manager sẽ từ chối. |

### 4.3 `QuestHaUnavailableException` → 503

`ReadAsync` ném `QuestHaUnavailableException` (có `Code`, `RetryAfter`, `ConsecutiveFailures`) khi không trả được dữ liệu đủ. Host dịch sang `QuestDbUnavailableException`; ở WebAPI `VdsExceptionFilter` đổi thành **HTTP 503 + `Retry-After`** (giây).

| Mã | Khi nào | `RetryAfter` thư viện | Ghi chú |
|---|---|---|---|
| `QuestHaJournalUnavailable` | Journal SQL không đọc được head/coverage/status (SQL lỗi, timeout, mất kết nối) | 5s | Fail-closed; không trip breaker node. |
| `QuestHaNotCovered` | Không node nào phủ head đã chốt trong `CoverageWait` | 1s | `ConsecutiveFailures=0`; không trip breaker; lượt kế kiểm coverage ngay. |
| `QuestHa` | Không còn node đủ điều kiện (Down, heartbeat cũ, breaker, đã thử trong request) | 5s | WebAPI trả `Retry-After` 30s cho mã này (hằng cooldown của WebAPI); Worker giữ 5s. |

Worker dùng cùng đường này qua `QuestRepositoryBase`: job giữ watermark và thử lại lượt sau.

---

## 5. Lệnh CLI

`QuestHa.Admin` chạy một lệnh rồi thoát; không khởi chạy replicator, không tạo bảng, không điều khiển QuestDB. Mọi lệnh cần `--config <file>`. Chạy trên máy Worker.

```powershell
dotnet QuestHa.Admin.dll <lệnh> --config C:/secure/questha.json [tham số]
```

| Lệnh | Tham số riêng | Việc | Mặc định |
|---|---|---|---|
| `status` | — | In `Backlog` và `Health` (JSON) | chỉ đọc |
| `compare` | `--source A --target B --table VdsTrafficData\|VdsTrafficStatistic\|all --from <UTC> --to <UTC>` | So sánh hai node theo cửa sổ phút | chỉ đọc |
| `repair` | như `compare` | Vá node đích từ nguồn | dry-run; `--apply` chỉ Shadow với nguồn = `SourceNode` |
| `purge` | — | Dọn journal đã giao | dry-run (chỉ đếm); `--apply` mới DELETE |
| `cursor-retire` | `--node X [--stale-minutes 5] [--force-live]` | Gỡ cursor node đã ngoài `Nodes` | preview; `--apply` mới xoá |
| `backup-list` | `[--node X]` | Liệt kê catalog (Verified, RetiredUtc, tuổi, kích thước, watermark) | chỉ đọc |
| `backup-retire` | `--id <guid> [--unretire]` | Retire/unretire một bản | preview; `--apply` mới ghi |
| `backup-run` | `--node X` | IPC tới Worker: backup lạnh một node | preview; `--apply` tạo bản Manual |
| `restore` | `--node X --id <guid> [--allow-journal-gap]` | IPC tới Worker: restore | preview; `--apply` mới chạy |
| `node-unfence` | `--node X` | IPC tới Worker: gỡ `ManualFence` | preview; `--apply` mới gỡ |
| `export` | `--day yyyy-MM-dd` (ngày giờ server) | Xuất/kiểm lại một ngày ra Parquet (mục 8), chạy ngay trong CLI | xem trước; `--apply` mới ghi file + catalog |
| `export-list` | `[--table X] [--from yyyy-MM-dd] [--to yyyy-MM-dd]` | Liệt kê catalog bản xuất | chỉ đọc |
| `import` | `--dir <thư mục ngày hoặc phiên bản vNNNN> --node X [--into <bảng>]` | Nạp ngược Parquet vào node (8.6) | kiểm SHA + đếm; `--apply` mới nạp |

`--apply` chỉ hợp lệ với `repair`, `purge`, `cursor-retire`, `backup-retire`, `backup-run`, `restore`, `node-unfence`, `export`, `import`. `--allow-journal-gap` chỉ cho `restore`; `--stale-minutes`/`--force-live` chỉ cho `cursor-retire`; `--unretire` chỉ cho `backup-retire`. Tham số sai ⇒ in Usage, exit 1.

**Tham số mỗi lượt** (không lưu thành cấu hình):

| Tham số | Mặc định | Biên | Dùng cho | Ý nghĩa |
|---|---:|---|---|---|
| `--timeout-seconds` | 300 | 1..86400 | mọi lệnh | Thời hạn toàn bộ lượt. Ctrl+C huỷ chủ động. |
| `--wait-seconds` | 60 | 0..3600 | `repair --apply` | Ngân sách chờ backlog/cursor đích trước mỗi bảng, và chờ WAL. |
| `--settle-seconds` | 120 | 0..86400 | `compare`, `repair` | Bỏ phần dữ liệu sự kiện quá mới. |
| `--max-rows` | 100000 | 1..1 000 000 | `repair` | Trần ứng viên vá mỗi bảng. |
| `--max-windows` | 10080 | 1..10080 | `compare`, `repair` | Trần cửa sổ phút, tối đa 7 ngày. |
| `--max-pending` | 1000 | 0..1 000 000 | `repair` | Hoãn khi backlog đích vượt ngưỡng. |
| `--stale-minutes` | 5 | 1..525600 | `cursor-retire` | Heartbeat mới hơn số phút này chặn lệnh. |
| `--force-live` | tắt | — | `cursor-retire` | Bỏ kiểm heartbeat, in WARNING trên stderr; vẫn chặn node còn trong `Nodes`. |

`--from/--to` phải có hậu tố `Z`. Đầu khoảng được làm tròn xuống đầu phút; cuối bị cắt theo settle. Thiếu cursor: chờ Worker tạo cursor, không tạo cursor giả.

**Exit code:** `0` hoàn tất (compare: bằng nhau; repair apply: đã enqueue), `2` còn khác biệt/hoãn/chạm trần/dòng không thể vá, purge hoặc cursor-retire bị chặn, `status` có Health/Backlog từ Warn trở lên, backup/restore/unfence báo chưa hoàn tất (kể cả `restore --allow-journal-gap`), `export` bị hoãn/chưa xong (`DeferredReason`), `import` số dòng sau < manifest, `1` lỗi hoặc tham số sai, `124` hết hạn, `130` Ctrl+C. JSON kết quả ở stdout, mọi enum là chuỗi; thông báo lỗi ở stderr. Timeout toàn CLI trả `124` kể cả khi SQL trả `SqlException` do bị huỷ.

**Ví dụ**

```powershell
dotnet QuestHa.Admin.dll status --config C:/secure/questha.json
dotnet QuestHa.Admin.dll compare --config C:/secure/questha.json --source A --target B --table all --from 2026-10-27T01:00:00Z --to 2026-10-27T02:00:00Z
dotnet QuestHa.Admin.dll repair  --config C:/secure/questha.json --source A --target B --table all --from 2026-10-27T01:00:00Z --to 2026-10-27T02:00:00Z --apply
dotnet QuestHa.Admin.dll purge --config C:/secure/questha.json --apply
dotnet QuestHa.Admin.dll cursor-retire --config C:/secure/questha.json --node OldNode --apply --stale-minutes 10
dotnet QuestHa.Admin.dll backup-list --config C:/secure/questha.json --node A
dotnet QuestHa.Admin.dll backup-run --config C:/secure/questha.json --node A --apply
dotnet QuestHa.Admin.dll restore --config C:/secure/questha.json --node A --id <guid> --timeout-seconds 3600
dotnet QuestHa.Admin.dll node-unfence --config C:/secure/questha.json --node A --apply
dotnet QuestHa.Admin.dll export --config C:/secure/questha.json --day 2026-11-01
dotnet QuestHa.Admin.dll export --config C:/secure/questha.json --day 2026-11-01 --apply --timeout-seconds 3600
dotnet QuestHa.Admin.dll export-list --config C:/secure/questha.json --from 2026-10-01
dotnet QuestHa.Admin.dll import --config C:/secure/questha.json --dir D:/exports/VdsTrafficData/2026/11/2026-11-01 --node B --apply
```

**Lưu ý `repair`**

- Worker phải đang chạy. `--apply` chỉ cho Shadow, nguồn đúng `SourceNode`; không dùng snapshot node để ghi đè dữ liệu Journal.
- Dòng thừa, dòng đích mới hơn và dòng không hợp lệ chỉ được báo. `Journaled` là số dòng đã vào journal, không phải xác nhận đích đã áp xong: chạy `compare` sau khi đích bắt kịp để nghiệm thu.
- `--apply` giữ SQL application lock theo DB journal, ngăn nhiều CLI vá đồng thời; phiên SQL không pooling/reconnect, bộ giám sát huỷ lượt khi mất khoá/kết nối. Kết quả in sau khi xong các bảng; dữ liệu đã commit trước khi huỷ vẫn ở journal.

---

## 6. Đọc/ghi và bảo đảm dữ liệu

### 6.1 Ghi

| WriteMode | Hành vi |
|---|---|
| `Direct` | Ghi thẳng như cũ; không journal. |
| `Shadow` | Ghi node chính trước; sao chép bất đồng bộ vào journal. `Nodes` chỉ node phụ; `SourceNode` = node chính cho CLI. Lỗi sao chép không chặn pipeline. |
| `Journal` | **ACK chỉ sau SQL COMMIT.** Gom các append đang chờ thành một transaction (group commit). SQL lỗi/huỷ không được coi là ACK; pipeline giữ retry và đánh dấu khoảng cần bù. Hàng đợi đầy ⇒ từ chối ngay, không ACK. |

### 6.2 Đọc có coverage

`ReadMode=Router` (Worker và WebAPI dùng chung `QuestHaManager.ReadAsync`):

1. Chốt `MaxSeq` (head) của journal **một lần** cho request.
2. Chọn node Ready theo ưu tiên (loại node Down, heartbeat cũ, breaker, đã thử).
3. Chờ trong ngân sách `CoverageWait` **mỗi node**: cursor ≥ head, không rào phục hồi/quarantine, và WAL đã áp (mục 6.3). Hết ngân sách mới thử node khác (hai node trễ có thể dùng ~6 giây, chưa tính chốt head/mở lease/query); cả hai không phủ ⇒ 503 `QuestHaNotCovered`.
4. Chạy query trên lease của node đã phủ. Head không đuổi theo append mới trong lúc chờ.

Snapshot recovery/cursor/quarantine theo node dùng chung trong tối đa 50 ms; sau WAL luôn đọc mới một SELECT để chặn rào/rewind/quarantine vừa xuất hiện. Huỷ request dừng cả bước đọc journal, chọn node và mở lease.

### 6.3 Ready theo WAL và chờ WAL theo mốc

- **Ready** là trạng thái vận hành để chọn ứng viên đọc. Replicator theo dõi các **mốc sequencer cố định** cho mỗi bảng; writer phủ mốc nào thì bỏ mốc đó, không đòi mọi bảng đồng thời `writerTxn=sequencerTxn`. Mốc chưa phủ quá `ReadyMaxLag` (2s), WAL đứng hoặc backlog tăng tới mức writer không phủ kịp ⇒ `CatchingUp`. Mỗi bảng giữ tối đa 64 mốc; vượt trần thì gộp hai mốc già nhất (chỉ chặn bảo thủ). Kết nối/root/thế hệ đổi thì xoá bằng chứng. WAL suspend vẫn bị rào; node mất kết nối vẫn Down.
- **Chờ WAL** (`WaitWalAppliedAsync`) chốt target = `sequencerTxn` của probe đầu rồi chỉ chờ `writerTxn ≥ target`. Không so với `sequencerTxn` mới nhất vì dưới tải ghi liên tục điều kiện không bao giờ đúng. Đúng vì mọi caller chốt head/cursor **trước** probe đầu: cursor đã phủ head ⇒ row ≤ head đã nằm trong WAL lúc probe đầu. Bảng mất, WAL suspended, hoặc txn nhỏ hơn probe đầu (node restart/restore) ⇒ false. Áp cho đường đọc, reconciler và `repair --apply`.
- `AllowDegradedRead=false`: không có node Ready thì lỗi, không đọc tạm node CatchingUp.

### 6.4 Rewind, SafeSeq, watermark

- `Replicator.RewindOnReconnect` mặc định **2 phút**. Khi node chuyển Down → kết nối lại, replicator hạ `LastSeq` về **`SafeSeq`**, là cursor giao xong đã đủ tuổi cửa sổ này, lưu bền trong `QuestNodeCursor`. Rewind chỉ hạ `LastSeq` về `SafeSeq`, không đặt `SafeSeq` về 0 khi Down.
- Lịch sử thời điểm giao giữ trong RAM; `SafeSeq` đã lưu SQL vẫn còn sau restart. Worker mới liên lạc lần đầu luôn rewind về `SafeSeq` đã lưu rồi tạo lịch sử mới từ các lô thực sự phát lại, nên `SafeSeq` có thể đứng ở mốc cũ thêm một cửa sổ sau restart; journal mới tạo/chưa có mốc đủ tuổi thì mốc là 0. Không suy `SafeSeq` từ tuổi journal hay thời gian node Down.
- **Watermark của backup** = `min(SafeSeq, LastSeq)`, nên **trễ khoảng `RewindOnReconnect` là bình thường**. Với tải đỉnh 294 sự kiện/giây, cửa sổ 120 giây ≈ **35.280 sự kiện cần replay**, cộng dữ liệu phát sinh trong downtime và độ trễ giao thực tế; không phải dự báo thời gian phục hồi. Log `node kết nối lại ⇒ lùi cursor 1624 → 0` khi `SafeSeq` còn 0 là đúng thiết kế.
- **Không** bỏ/thu hẹp rewind và **không** nâng watermark lên `LastSeq`: QuestDB `nosync` có thể mất WAL đã ACK nhưng chưa fsync khi chết đột ngột.
- Pool PG được xoá **riêng node**, tối đa một lần/giây/client node: khi lỗi kết nối, node restart, trước probe reconnect; reader tự xoá khi thấy Down/heartbeat cũ, đổi `ReadySinceUtc`, đổi `RewindCount` của ảnh cục bộ, hoặc vừa đủ điều kiện lại. Không dùng ClearAllPools, không tự retry INSERT.

### 6.5 Failback

Node ưu tiên (`Priority` nhỏ) chỉ được nhận lại đọc khi Ready liên tục ≥ `FailbackStableFor` (30s; production nên dài hơn) **và** lượt đối soát gần nhất sau khi Ready đạt (`RequireReconcileForFailback`). Breaker từng node chặn node lỗi kết nối trong `BreakerCooldown` rồi cho một lượt thăm dò.

### 6.6 Đối soát nền

Reconciler đối soát journal ↔ node theo chu kỳ `AutoInterval`: chốt head H0, `ReadWindow` lọc `Seq ≤ H0`, đợi cursor tới H0 rồi WAL rồi đọc node. Bản node có `journalSeq > H0` cùng key bị loại khỏi cả hai phía; schema VDS không lưu `journalSeq` nên reconciler đọc lại journal trong cùng cửa sổ và loại các key có bản mới `Seq > H0` khỏi cả kỳ vọng lẫn thực tế (kể cả UPSERT key Statistic trong bucket đang mở). Key lệch thật không có bản mới vẫn được so sánh. `LastReconcileOk` chỉ ghi một lần/node/chu kỳ, bằng AND mọi bảng; một bảng lỗi/chưa đối soát chặn chứng nhận. Journal rỗng không miễn điều kiện coverage/WAL/quarantine/thế hệ.

### 6.7 Journal SQL lỗi ⇒ 503

Journal là nguồn chứng nhận của đường đọc. Không đọc được head/coverage/status ⇒ mọi API VDS đọc QuestDB qua Router trả **503** `Retry-After` (mã ở mục 4.3) tới khi SQL trở lại. API không bao giờ trả số liệu thiếu/cũ hoặc vượt rào phục hồi; không còn đọc dự phòng từ "xác nhận SQL gần nhất + probe node sống". Worker giữ luật dự phòng cho việc chọn node nhưng đường đọc job vẫn đòi journal. SQL HA phải bảo vệ cả đường ghi lẫn đường đọc.

### 6.8 Heartbeat khi tách replicator

Reader phụ thuộc heartbeat do replicator ghi vào journal. `Router.HeartbeatStaleAfter` (10s): heartbeat quá tuổi ⇒ node bị coi Down dù QuestDB còn trả lời. Replicator dừng lâu có thể làm cả hai node không đọc được. Giám sát dịch vụ replicator và heartbeat từng node; không tăng ngưỡng để che worker chết.

---

## 7. Dọn journal và backlog

### 7.1 Backlog

`status` trả `Backlog` và `Health`. Backlog có `Level` (`Ok/Warn/Critical`), lý do, tổng dòng metadata xấp xỉ, dung lượng bảng đang dùng (gồm index/LOB, không phải dung lượng trống ổ đĩa), số quarantine mở và pending từng node.

- Pending đếm dòng thực tế đã commit (kể cả Superseded), tối đa `CriticalPendingRows+1` mỗi node; không lấy `MaxSeq-LastSeq` vì IDENTITY có thể có lỗ. Chạm trần thì `PendingRowsLowerBound=true` và hiển thị `≥` (ví dụ `≥1000001`).
- Tuổi tính theo đồng hồ SQL, từ dòng đầu tiên có `Seq>LastSeq`. Thiếu cursor: `CursorExists=false`, đo như `LastSeq=0` và chặn dọn; không tạo cursor giả.
- Biên ngưỡng dùng `>=`; quarantine mở hoặc thiếu cursor tối thiểu Warn. Warn ghi Warning; Critical ghi Error và timeline `JournalBacklog`, giãn theo `AlertThrottle`, tăng mức phát ngay. **Không chặn append**, không loại pending.
- Tài khoản cần quyền đọc `sys.dm_db_partition_stats`.

Ngưỡng mặc định (đổi qua VdsConfig, mục 3.2; cần hiệu chỉnh theo tải và dung lượng thực tế):

| Ngưỡng | Warn | Critical |
|---|---:|---:|
| Pending từng node | 100.000 | 1.000.000 |
| Tuổi pending lâu nhất | 30 giây | 5 phút |
| Journal đang dùng | 10 GiB | 50 GiB |

### 7.2 Purge

`purge` mặc định dry-run (chỉ đếm); `--apply` mới DELETE. Lịch nền chỉ chạy ở host sở hữu replicator, khi `WriteMode` khác `Direct`, có node và `JournalMaintenance.Enabled=true`; `Enabled=false` tắt cả đo/cảnh báo nền lẫn purge; WebAPI không có lịch. `journal_dry_run=1` chỉ đổi lịch nền, CLI vẫn cần `--apply`.

Một dòng chỉ được xoá khi **tất cả** đúng:

- `Seq ≤ MIN(LastSeq, SafeSeq)` của **mọi node** trong `Nodes` (giữ `SafeSeq` để rewind không mất phần cần phát lại);
- `CreatedUtc < SQL now − Retention`;
- `Seq ≤ MIN(WatermarkSeq)` của **tập backup còn bảo vệ journal** (mục 7.3); khi `Export:Enabled=true` thay bằng: nằm trong backup lạnh đó **hoặc** trong bản xuất Parquet Verified của ngày nó với `WatermarkSeq ≥ Seq` (mục 8.4);
- không có quarantine mở ở bất kỳ node nào.

Mọi hàng `QuestNodeCursor` đều được đối chiếu: có cursor ngoài `Nodes` thì chặn với `UnconfiguredNodeCursor`. Không có node/cursor hoặc không có backup Verified thì không xoá (cờ nguy hiểm `AllowPurgeWithoutVerifiedBackup=true` chỉ bỏ điều kiện thiếu backup, và chỉ khi Export tắt). `Direct` không dọn. `Nodes` của CLI phải trùng tập replicator, không dùng danh sách reader của Shadow và không thêm `SourceNode`.

Cơ chế an toàn:

- Khoá SQL session `QuestHa:JournalPurge` theo DB, trên chính kết nối không pooling/reconnect thực thi lệnh; nhiều CLI/worker không dọn đồng thời. Không lấy khoá `QuestOutbox.Append`.
- Mỗi lô đọc `TOP(BatchSize)` dòng kế tiếp theo `Seq` trong mốc cho phép, không lọc tuổi trước TOP; gặp dòng chưa đủ tuổi thì dừng. Giả định `CreatedUtc` tăng theo `Seq` (cùng sinh trong applock Append).
- Trong transaction: kiểm tuổi/RowVer và quarantine chỉ seek danh sách `Seq` hữu hạn, giữ khoá dòng outbox cùng khoá dải quarantine; sau đó mới giữ shared HOLDLOCK mọi cursor/catalog để kiểm lại mốc và DELETE theo khoá chính. Tranh chấp SQL (deadlock/lock timeout 250 ms) nhường lượt; huỷ rollback lô chưa commit, lô đã commit giữ nguyên.
- Khi gặp dòng trẻ, đọc thêm tối đa 500 dòng kế tiếp trong cùng mốc; nếu có dòng đủ tuổi phía sau ⇒ `Reason=NonMonotonicCreatedUtc: BlockingSeq=<Seq>`, giữ nguyên dòng trẻ và mọi dòng phía sau. Bất thường xa hơn 500 dòng sau dòng chặn chưa được phát hiện trong lượt đó. Worker ghi mọi lý do dừng/chặn ở Warning (giãn theo `AlertThrottle`); lý do tuổi bất thường được giữ trong RAM (Backlog tối thiểu Warn) tới lượt purge thành công tiếp theo, mất khi restart.
- `Rows` là số đếm/đã xoá xác nhận. `RowBudget` chỉ trả khi còn ứng viên đủ điều kiện sau trần; `TimeBudget` chỉ trả khi thực sự hết thời gian. Exit `2` khi chạm trần hoặc bị chặn.
- Biên purge bền `QuestJournalPurgeState` được cập nhật cùng transaction DELETE để phát hiện journal đã mất.
- Trần cứng: mỗi lô 500, mỗi lượt tối đa 1 phút, retention tối đa 100 năm (VdsConfig siết thêm ở bảng 3.2). Không reseed `Seq`, không dọn quarantine/catalog, không thu nhỏ file SQL.

### 7.3 Backup ghim journal

Chính sách bảo vệ giữ bản Verified **mới nhất** của mỗi node còn nghĩa vụ journal, cùng các bản Verified chưa retire trong `ProtectedBackupDays`. Unretire không khôi phục journal đã xoá. Khi node đã ngoài `Nodes` **và** không còn cursor, mọi backup của node đó ngừng ghim journal.

### 7.4 `cursor-retire`

`cursor-retire --node X` mặc định chỉ xem trước `LastSeq`/`SafeSeq`; `--apply` xoá đúng hàng cursor của node ngoài cấu hình.

- Node còn trong `Nodes` bị chặn `ConfiguredNodeCursor`; thiếu cursor trả `CursorNotFound`.
- Heartbeat (`QuestNodeStatus.HeartbeatUtc`, đồng hồ SQL) mới hơn `now − stale-minutes` (kể cả tương lai) trả `LiveNodeHeartbeat`; khoá status giữ tới COMMIT để heartbeat không chen giữa kiểm và xoá. Chỉ `--force-live` mới bỏ rào này, luôn in cảnh báo.
- Trước `--apply`: dừng hẳn replicator cũ và xác nhận bỏ nghĩa vụ giao dữ liệu cho node đó, nếu không replicator có thể tạo lại cursor.
- Lệnh không xoá journal, status, quarantine hay file backup. Sau retire, purge sau có thể xoá phần journal từng được cursor đó bảo vệ; node quay lại phải rebuild/restore.

---

## 8. Backup xuất Parquet hằng ngày

Bản backup **chính** khi đã bật: mỗi ngày, Worker (host vai Writer, cùng chỗ bảo trì journal/backup lạnh) xuất trọn ngày hôm trước của mọi bảng QuestDB đã đăng ký (`VdsTrafficData`, `VdsTrafficStatistic`) ra file Parquet nén zstd, **không dừng node**. Bản xuất Verified là căn cứ để dọn journal (journal ~1,7 KB/dòng, 14 ngày ≈ 208 GB ở tải trung bình — không giữ lâu được) và được tính như "có backup" cho Health. Backup lạnh (mục 9) thành tuỳ chọn, chạy thưa.

Số đo lab (P6, node A, 8,64 triệu dòng/ngày trung bình): `/exp?fmt=parquet&compression_codec=zstd` ≈ 9 s và **332 MB/ngày**; ngày đỉnh 25,4 triệu dòng ≈ 963 MB. Xuất **nguyên ngày đỉnh** làm QuestDB tốn 6–7,6 GB RAM ⇒ luôn xuất **theo giờ** (mỗi giờ ≤ 2,6 GB WS, ngày đỉnh ≈ 52 s). `read_parquet` nạp ngược khớp 100 %, nạp lại cùng file không sinh trùng (DEDUP). S6 (200 000 dòng Data + 20 160 dòng Statistic): xuất 2,1 s, 9,7 MB cho 48 file; nạp ngược 1,5 s/bảng, `EXCEPT` hai chiều 0 dòng lệch.

### 8.1 Luồng một lượt

1. **Giờ lịch** `vds_questha_export_schedule` (mặc định `02:00`, **giờ server**) ⇒ ngày cần xuất = hôm qua theo giờ server, khoảng `[00:00, 24:00)` đổi sang UTC của cột designated timestamp (dữ liệu lưu UTC; giờ server +07 ⇒ `[D-1 17:00Z, D 17:00Z)`). Múi giờ có DST: 00:00 không tồn tại ⇒ lúc chuyển giờ; 00:00 lặp hai lần ⇒ **lần đầu** (mốc UTC sớm nhất) — các ngày nối liền, không bỏ sót dòng. Ngày chưa kết thúc không xuất (`DayNotFinished`).
2. **Chọn node:** node **phụ** đang Ready (không phải node router đang đọc; Ready = trạng thái Ready, heartbeat mới, không pause, WAL không suspend, có `HttpUrl`). Chỉ một node Ready ⇒ dùng node đó; không node nào ⇒ hoãn (`NoReadyNode`), thử lại.
3. **Chứng nhận coverage** bằng đúng cơ chế đường đọc (6.2): chốt head journal, node phải có cursor ≥ head, không rào/phục hồi/quarantine ≤ head, WAL mọi bảng áp tới mốc; trong `Export:CoverageWait` (2 phút). Không phủ ⇒ `NotCovered`. Head đã chốt là `WatermarkSeq` của bản xuất.
4. **Mỗi bảng, mỗi giờ** (24 lượt; ngày đổi giờ 23/25): `SELECT count()` qua PG với đúng điều kiện `ts >= from AND ts < to` của câu `/exp` → `GET /exp?query=SELECT * FROM <bảng> WHERE …&fmt=parquet&compression_codec=zstd` **stream thẳng ra `HH.parquet.tmp`** (không giữ file trong RAM; timeout `Export:HttpTimeout`) → đọc **số dòng trong metadata Parquet** (footer `num_rows`, kiểm magic `PAR1` đầu/cuối — chắc chắn, không cần `cairo.sql.copy.root` trên node xuất) phải **bằng** số đếm, lệch ⇒ giờ hỏng → SHA-256 → đổi tên `HH.parquet`. Giờ không có dòng nào không gọi `/exp` (manifest ghi `File=null`, `Rows=0`). Mọi file được ghi vào **phiên bản nháp** `vNNNN` (xem Thư mục); tiến độ từng giờ ghi `progress.json` của bản nháp để lần thử sau **không xuất lại** giờ đã xong (giờ trong tiến độ chỉ được dùng lại khi SHA-256/kích thước/số dòng thật còn khớp).
5. `schema.sql` (kết quả `SHOW CREATE TABLE`) → **kiểm lại toàn bộ bộ file** của bản nháp (tên giờ, SHA-256, số dòng metadata) → **`manifest.json` ghi SAU CÙNG** (file tạm + flush + đổi tên) = **công bố** phiên bản. Phiên bản không có manifest = bản dở: không tính là đã xuất, không nạp ngược được.
6. Catalog `dbo.QuestExportCatalog` (DB journal): thêm dòng Verified (`ExportId` = `ExportId` trong manifest, `Path` = thư mục phiên bản); trong cùng transaction retire dòng còn hiệu lực cũ của cùng bảng/ngày. Catalog chuyển xong mới xoá phiên bản cũ. Lỗi giữa ghi manifest và catalog ⇒ lượt sau thấy manifest mới hơn catalog live (khác `ExportId`), kiểm SHA rồi **ghi bù catalog** theo manifest, không xuất lại.
7. **Kiểm lại dữ liệu đến muộn:** cùng lượt kiểm lại `vds_questha_export_recheck_days` ngày trước (mặc định 1 ⇒ D-2): đếm lại từng giờ và **kiểm SHA-256 thật** từng file của bản đã công bố; giờ có số đếm khác manifest, có dòng journal `Seq > WatermarkSeq` cũ (quét `QuestOutbox` theo `TargetTable, DesignatedTs`) **hoặc file hỏng** (kể cả sửa byte cùng độ dài) được xuất lại vào **phiên bản mới** `v(N+1)`; giờ còn nguyên được chép sang; manifest mới + watermark mới; catalog thêm dòng mới, dòng cũ retire. File của bản đang Verified **không bao giờ** bị sửa: lỗi giữa chừng ⇒ bản cũ và catalog còn nguyên, bản nháp được lượt sau dùng tiếp hoặc dọn. Không có gì đổi ⇒ không tạo phiên bản. Ngày bị lỡ hoàn toàn (hôm trước thử hết lượt) được lượt này xuất bù. `export --day <ngày> --apply` tay trên ngày đã có cũng chạy đúng đường kiểm lại này.
8. **Retention:** xoá thư mục ngày (mọi phiên bản) cũ hơn `vds_questha_export_retention_days` (90), chạy dưới **cùng applock `QuestHa:Export`** với lượt xuất (Worker và CLI): đọc lại catalog trong khoá, **retire catalog TRƯỚC** rồi mới xoá thư mục — lỗi giữa hai bước chỉ để lại thư mục thừa (lượt sau dọn), không bao giờ có dòng Verified trỏ thư mục đã xoá. Chỉ xoá thư mục nằm **trong** `ExportRoot`, không qua reparse point/symlink (luật như backup lạnh) và chỉ chứa mục do bản xuất tạo (`vNNNN`, `HH.parquet`, `manifest.json`, `schema.sql`, `progress.json`, `*.tmp`); có mục lạ ⇒ giữ nguyên + Warning. **Không bao giờ** xoá ngày Verified mới nhất của mỗi bảng (bản còn bảo vệ journal). Khoá đang bị giữ ⇒ để lượt sau.
9. **Lỗi** ⇒ log Warning + Timeline `Export` (chỉ loại lỗi/mã HTTP, không in URL có thông tin xác thực, không mật khẩu); thử lại sau `vds_questha_export_retry_delay` (10m), tối đa `vds_questha_export_max_attempts` (6) trong ngày; file tạm của lần trước dọn ở đầu lần thử. Worker và CLI dùng chung applock `QuestHa:Export` cho xuất và retention (lượt thứ hai trùng giờ ⇒ `ExportAlreadyRunning`).
10. `vds_questha_export_dry_run=1` (mặc định) ⇒ lịch chỉ **xem trước** một lần/ngày (node chọn, số dòng, dung lượng ước, trống đĩa, thư mục sắp hết hạn) vào log/Timeline; không ghi file/catalog, không xoá.

**Thư mục:**

```
{ExportRoot}/{bảng}/{yyyy}/{MM}/{yyyy-MM-dd}/          (thư mục ngày)
    v0001/                      (phiên bản; bình thường chỉ còn MỘT — bản catalog đang trỏ)
        00.parquet … 23.parquet (HH = thứ tự giờ từ 00:00 giờ server; giờ rỗng không có file)
        schema.sql              (SHOW CREATE TABLE — tạo lại bảng đúng timestamp/partition/WAL/DEDUP)
        manifest.json           (ghi sau cùng = công bố; phiên bản đã công bố không bao giờ bị sửa)
    v0002/                      (chỉ khi đang/đã xuất lại: nháp có progress.json, chưa manifest)
```

Phiên bản có manifest = đã công bố, bất biến; phiên bản số lớn hơn mà chưa có manifest = bản nháp (lần thử dở). Catalog `Path` trỏ thư mục phiên bản. Sau khi catalog chuyển sang phiên bản mới, phiên bản cũ (đã retire) và bản nháp bỏ dở được xoá (chỉ thư mục đúng mẫu tên, không link).

`manifest.json` (`Version=2`): `ExportId` (= khoá dòng catalog của phiên bản), `Table`, `DesignatedColumn`, `DayLocal`, `TimeZone` (Id múi giờ server) + `UtcOffset`, `FromUtc`/`ToUtc`, `Node`, `WatermarkSeq`, `Codec=zstd`, tổng `Rows`/`Bytes`, `CreatedUtc` và từng giờ: `Hour`, `FromUtc`/`ToUtc`, `File`, `Rows`, `Bytes`, `Sha256`, `ExportedUtc`, `WatermarkSeq`.

**Xem trước** (`export --day yyyy-MM-dd` không `--apply`): node chọn, watermark, số dòng dự kiến từng giờ, dung lượng ước (50 B/dòng, làm tròn lên từ lab), trống đĩa của `ExportRoot`. Trống phải **≥ ước × 2 + `Export:MinimumFreeBytes`**, không thì `InsufficientDisk` (cả khi apply).

### 8.2 Cấu hình

Hạ tầng (`QuestHa:Export`, file cấu hình local, **đổi phải restart**):

| Khoá | Mặc định | Ý nghĩa |
|---|---|---|
| `Export:Enabled` | `false` | Bật lịch xuất ở Writer **và** cho bản xuất Verified được tính như backup (dọn journal, `Health.BackupRequired`). Bật mà thiếu `ExportRoot` tuyệt đối hoặc không node nào có `HttpUrl` ⇒ Worker từ chối khởi động. |
| `Export:ExportRoot` | — | Thư mục gốc bản xuất (local/UNC), tuyệt đối. Dung lượng: ≈ 332 MB/ngày trung bình × `retention_days` (90 ngày ≈ 30 GB; ngày đỉnh 963 MB). |
| `Export:HttpTimeout` | 10m | Trần một lệnh `/exp` (một giờ) và chờ WAL sau nạp ngược. |
| `Export:CoverageWait` | 2m | Ngân sách chờ node phủ head trước khi xuất. |
| `Export:PollInterval` | 1m | Nhịp worker lịch. |
| `Export:MinimumFreeBytes` | 1 GiB | Trống phải còn ngoài ước × 2. |
| `Nodes[].HttpUrl` | — | REST của node, vd `http://<host>:9000` (rỗng ⇒ node không được chọn để xuất). |
| `Nodes[].HttpUser` / `HttpPassword` | — | Chỉ khi node bật Basic auth HTTP. Mật khẩu qua biến môi trường (`QuestHa__Nodes__N__HttpPassword`; CLI `QUESTHA_QuestHa__Nodes__N__HttpPassword`), không ghi file, không log. |
| `Nodes[].ParquetCopyRoot` | — | Đường dẫn (nhìn từ máy chạy lệnh) tới `cairo.sql.copy.root` của node — dùng cho nạp ngược. |

Vận hành (VdsConfig, đổi lúc chạy, mục 3.2): `export_schedule`, `export_retention_days`, `export_recheck_days`, `export_max_attempts`, `export_retry_delay`, `export_warn_after`, `export_critical_after`, `export_dry_run`.

### 8.3 Catalog `QuestExportCatalog`

Migration `20261102_QuestJournal_Add_ExportCatalog.sql` (DB journal, luỹ đẳng). Mỗi dòng: `ExportId` (trùng `ExportId` trong manifest của phiên bản), `TableName`, `DayLocal` (date, giờ server), `FromUtc`/`ToUtc`, `Node`, `WatermarkSeq`, `[Rows]`, `Bytes`, `Path` (thư mục phiên bản `…\yyyy-MM-dd\vNNNN`), `Verified`, `CreatedUtc`, `RetiredUtc`. Không cần cột mới cho phiên bản (số phiên bản nằm trong `Path`). `export-list [--table X] [--from yyyy-MM-dd] [--to yyyy-MM-dd]` in catalog (ngày mới nhất trước).

### 8.4 Dọn journal khi Export bật

Ngoài các điều kiện ở 7.2 (cursor mọi node, `Retention`, quarantine), một dòng journal chỉ được xoá khi **đã nằm trong một bản backup Verified**:

- trong **bản xuất Verified chưa retire** của cùng bảng có khoảng `[FromUtc, ToUtc)` chứa `DesignatedTs` của dòng (= ngày giờ server của dòng) **và** `WatermarkSeq ≥ Seq`; **hoặc**
- trong **backup lạnh Verified** theo luật hiện có (`Seq ≤ MIN(WatermarkSeq)` của tập backup còn bảo vệ, 7.3).

Dòng của ngày chưa xuất (hôm nay, hoặc ngày xuất hỏng) và dòng **đến muộn sau watermark** ⇒ giữ, tới khi lượt kiểm lại xuất lại giờ đó với watermark mới (hoặc backup lạnh bao nó). Điều kiện "chưa có backup Verified" (`NoVerifiedBackup`) được gỡ khi có ít nhất một bản xuất Verified còn hiệu lực. Cờ `AllowPurgeWithoutVerifiedBackup` **không có tác dụng khi Export bật**: không backup lạnh thì mốc lạnh vẫn là −1 và từng dòng vẫn phải nằm trong bản xuất Verified (cờ chỉ giữ nghĩa cũ khi Export tắt). Kiểm trong transaction DELETE với HOLDLOCK trên catalog (retention không retire chen được). Nhờ vậy `vds_questha_journal_retention` mặc định giảm còn **3d** mà vẫn an toàn.

**Hệ quả với backup lạnh:** dòng đã nằm trong bản xuất có thể bị xoá dù chưa có backup lạnh mới hơn ⇒ restore **strict** từ một backup lạnh cũ sẽ báo journal thiếu sau watermark. Khôi phục khi đó: restore lạnh với `--allow-journal-gap` rồi **nạp ngược Parquet** các ngày từ watermark của bản lạnh tới hiện tại (8.6), tạo backup lạnh mới, strict restore (9.5). Hoặc dựng node chỉ từ Parquet: tạo bảng bằng `schema.sql`, nạp các ngày, phần sau ngày xuất cuối được journal phát lại.

Dữ liệu đến muộn hơn `recheck_days` ngày không được tự xuất lại ⇒ các dòng đó ở lại journal (purge giữ) tới khi chạy tay `export --day <ngày> --apply` cho ngày đó hoặc có backup lạnh bao chúng.

### 8.5 Health

Khi `Export:Enabled=true` (cấu hình của host đánh giá): với mỗi bảng, lấy ngày giờ server mới nhất có bản xuất Verified còn hiệu lực; tuổi tính từ **lần đầu** xuất ngày đó (bản xuất lại do dữ liệu đến muộn không làm "trẻ" lại). Tuổi > `export_warn_after` (26h = 02:00 + 2 giờ của ngày sau mà D-1 chưa xong) ⇒ **Warn** `export.old`; > `export_critical_after` (50h) ⇒ **Critical** `export.stale`; chưa có bản nào ⇒ Warn `export.none`. Catalog không đọc được (thiếu migration) ⇒ Critical `export.catalogUnreadable` (không làm cả journal thành "không đọc được"). `BackupRequired` với node chưa có backup lạnh chỉ được bản xuất thay thế khi **mọi bảng đã đăng ký** đều có bản xuất Verified còn hiệu lực và chưa quá `export_critical_after`; thiếu một bảng (chưa xuất/quá hạn) ⇒ vẫn **Critical** `backup.none`, thông điệp nêu đúng tên bảng thiếu. Bản xuất của bảng không đăng ký không được tính. Backup lạnh chạy thưa vẫn bị `backup.old/stale` theo `health_backup_warn_after/critical_after` — nới hai ngưỡng này (tối đa 30d) cho khớp chu kỳ backup lạnh.

WebAPI chỉ đánh giá export nếu cấu hình WebAPI cũng đặt `Export:Enabled=true` (chỉ để đọc catalog; WebAPI không chạy xuất, không áp mã `export_*` từ VdsConfig — dùng giá trị file).

### 8.6 Nạp ngược (`import`)

```powershell
dotnet QuestHa.Admin.dll import --config C:/secure/questha.json --dir <ExportRoot>\VdsTrafficData\2026\11\2026-11-01 --node A
dotnet QuestHa.Admin.dll import --config C:/secure/questha.json --dir <…>\2026-11-01 --node A --apply [--into VdsTrafficData_restore]
```

1. `--dir` = thư mục ngày (⇒ phiên bản **đã công bố** mới nhất `vNNNN`) hoặc trực tiếp một thư mục phiên bản. Đọc `manifest.json` (không có phiên bản công bố ⇒ từ chối: bản dở); bảng của manifest phải là bảng QuestHa đã đăng ký; `--into` (tuỳ chọn) = bảng đích khác **đã tạo sẵn** bằng `schema.sql` (đổi tên).
2. **Kiểm SHA-256 + số dòng metadata mọi file theo manifest TRƯỚC** khi chạm node; lệch/thiếu ⇒ `InvalidDataException`, exit 1.
3. `read_parquet` chỉ đọc trong `cairo.sql.copy.root` của node (khai trong `server.conf`, cần restart; mặc định `<root>/import`). Phiên bản nằm sẵn trong `ParquetCopyRoot` ⇒ dùng đường dẫn tương đối; không ⇒ CLI chép file vào thư mục phiên **mới** `{ParquetCopyRoot}/questha-import/{bảng}/{ngày}-{8 ký tự}/` (copy root phải có sẵn; **mọi cấp** từ copy root xuống, kể cả thư mục có sẵn, không được là junction/symlink, đường dẫn chuẩn hoá phải nằm trong copy root — vi phạm ⇒ từ chối trước khi chép, exit 1; file không ghi đè), kiểm lại SHA bản chép; xong (kể cả khi lỗi) chỉ xoá đúng thư mục phiên đó.
4. Từng giờ một: `INSERT INTO <bảng> SELECT * FROM read_parquet('<đường dẫn tương đối>')` (không nạp nguyên ngày đỉnh một lần: 7,6 GB RAM khi DEDUP). DEDUP UPSERT của bảng chống trùng ⇒ nạp lại an toàn.
5. Chờ WAL áp, báo `RowsBefore`/`RowsAfter` của bảng trong khoảng ngày và `ManifestRows`. WAL chưa áp xong trong `Export:HttpTimeout` ⇒ `Error=WalNotApplied`, exit 2 (số dòng đủ không chứng minh dữ liệu đã vào: upsert cùng khoá giữ nguyên số dòng); `RowsAfter < ManifestRows` ⇒ `Error=RowsBelowManifest`, exit 2.

Không có `--apply` ⇒ chỉ kiểm SHA và đếm số dòng hiện có. Nạp vào bảng đang nhận dữ liệu thật thì dữ liệu nạp đi thẳng vào node đó (không qua journal) — dùng cho dựng lại node/khôi phục, không dùng thay đường ghi.

---

## 9. Backup lạnh và restore (tuỳ chọn, chạy thưa)

**Khi Export Parquet bật (mục 8):** backup lạnh là **tuỳ chọn**, chạy thưa (vd hằng tuần/tháng hoặc trước thay đổi lớn) để có điểm khôi phục nguyên node nhanh; bản xuất Parquet là backup hằng ngày và là căn cứ dọn journal. Restore strict từ bản lạnh cũ có thể báo journal thiếu ⇒ xem 8.4.

**Windows:** QuestDB OSS 10 báo `CHECKPOINT`/`SNAPSHOT` "not supported on Windows" và `BACKUP TABLE` chỉ có ở bản Enterprise (đã kiểm trên lab). Node chạy Windows chỉ có **backup lạnh** (dừng node, chép root, kiểm, start lại) và cần node còn lại Ready trong lúc đó. Node Linux dùng được checkpoint để backup không dừng node; chưa cài trong mã hiện tại.

### 9.1 Luồng backup

1. **Preview** (`backup-run --node X`): CLI gửi IPC tới Worker; Worker tiền kiểm catalog/migration, root, quyền ghi, dung lượng, peer Ready, dự toán I/O. Không dừng node.
2. **Apply** (`--apply`): tạo bản **Manual**, bỏ qua `Backup.Enabled`/`DryRun`, không giới hạn cửa sổ lịch, rồi chạy retention.
3. Worker thực hiện: Fence node (router bỏ node) → Pause replicator (chờ lô đang chạy) → ghi dấu dừng bền (`StateDirectory`) → `StopClean` → chốt watermark → chép root → đếm lại file/byte và băm SHA-256 → ghi **manifest** → ghi catalog **Verified** → Start + chờ node trả lời → Resume → Unfence. Start thất bại thì giữ Fenced và ghi Error.
4. **Dự toán trước downtime:** từ chối nếu `bytes × 3 × 1,25 / MinThroughputBytesPerSecond > MaxStoppedDuration` (copy + hash nguồn/đích). `MaxStoppedDuration` (30 phút) tính từ trước `StopClean`: copy/hash theo chunk 128 KiB có huỷ, probe peer tối đa mỗi 5 giây và một lần cuối trước khi start lại; quá hạn/host huỷ/peer lỗi ⇒ bỏ bản dở và start lại node bằng token cleanup độc lập.
5. Timeout/Ctrl+C của CLI **không** huỷ việc Worker đã nhận; xem kết quả ở catalog (`backup-list`) và Timeline.

Peer chưa Ready chỉ chờ tới hết cửa sổ, không tiêu hao `MaxAttempts`. `MaxStoppedDuration` sau khi đã dừng node trừ `MaxAttempts`; huỷ do host được truyền ra ngoài lịch.

### 9.2 Lịch, khung giờ, NodeOffset

- Mặc định `Backup.Enabled=false`, `DryRun=true`, `Schedule=off`. Bật lịch: `Enabled=true` (file) và `backup_schedule=HH:mm` (VdsConfig); lịch tự động chỉ **dừng node thật** khi `backup_dry_run=0`.
- `HH:mm` dùng InvariantCulture và timezone server. Node sắp theo tên tăng dần, lệch nhau `NodeOffset`, mỗi slot phải nằm trong `[WindowStart, WindowEnd)` (hỗ trợ qua nửa đêm). Ngày `WeeklyDay` (mặc định Sunday) tạo bản weekly, ngày khác tạo daily.
- Lịch đổi qua VdsConfig có hiệu lực từ tick sau; lịch sai chỉ báo lỗi một lần.

### 9.3 Retention

Mỗi node giữ: bản Verified mới nhất của **7 ngày local khác nhau gần nhất** có daily (`DailyCopies`), của **4 tuần local khác nhau gần nhất** có weekly (`WeeklyCopies`, tuần bắt đầu thứ Hai), và riêng **`ManualCopies=7`** bản gọi tay mới nhất; luôn giữ latest Verified. Bản gọi tay không tính vào quota daily/weekly. Cấu hình bị chặn nếu giữ ít hơn 7 daily + 4 weekly.

- Marker `.questha.json` lưu Weekly/Manual (marker cũ thiếu Manual coi là bản theo lịch), ghi tạm → `Flush(true)` → đổi tên thay thế; toàn bộ marker đọc một lần trước chọn/xoá.
- Marker rỗng/hỏng chỉ cô lập bản tương ứng (giữ file + catalog), không chặn retention node khác; lỗi I/O truy cập huỷ lượt để tránh xoá khi chưa xác định quyền sở hữu. Thư mục thiếu chứng từ sở hữu không bị tự xoá.
- Retention và dọn bản dở xoá manifest cùng thư mục.
- `backup-retire --id <guid>` preview rồi `--apply`; `--unretire` chỉ gỡ cờ, không khôi phục journal đã purge hoặc vượt `ProtectedBackupDays`. Bản Verified mới nhất của node không retire riêng lẻ được.

### 9.4 Manifest SHA-256

Mỗi bản backup có `{thư mục backup}.questha-manifest.json` (cạnh marker, ngoài thư mục được chép) gồm `BackupId`, node và từng file: đường dẫn tương đối, kích thước, SHA-256. Manifest được ghi bền sau khi so nguồn/đích và **trước** catalog Verified. **Bản backup không có manifest bị `restore` từ chối** (không có cờ bỏ qua): tạo bản Verified mới. Health vẫn tính bản không manifest là backup hợp lệ, nên sau khi nâng cấp từ bản cũ hãy tạo ngay một bản Verified mới cho từng node.

### 9.5 Restore

`restore --node X --id <guid>` (mặc định preview, qua IPC):

1. Từ chối bản chưa Verified, retired hiệu lực, hoặc journal có khoảng trống sau watermark (kiểm cả `MIN(Seq)` và biên purge bền, kể cả journal rỗng/giữ quarantine ở đầu).
2. **Băm lại toàn bộ backup theo manifest trước khi dừng node.** Preview báo `OK DryRun ContentVerified` khi khớp; lệch ⇒ `ERROR InvalidDataException`, node không bị đụng.
3. Apply: ghi `BeginRecovery` bền (thế hệ g) → Fence → Pause → `StopClean` → đổi root cũ thành `.prerestore-<guid>` → chép backup vào root → số file/tổng byte phải khớp manifest → đặt cursor về watermark → Start + chờ trả lời → mở rào. Cổng đọc vẫn chờ `Phase=None`, coverage/WAL và đối soát.
4. Restore lỗi sau khi đổi root: giữ node bị rào, **không** start/mở rào trực tiếp trên root cũ với cursor cũ; chạy lại strict restore từ bản Verified có journal đủ. Mọi root `.prerestore-*` được giữ qua các lần thử; chỉ dọn sau khi đã nghiệm thu bản thay thế và còn đủ dung lượng dự phòng.
5. **Journal đã mất đoạn sau watermark:** chỉ `--allow-journal-gap` mới cho phép cưỡng bức. Luôn cảnh báo bắt buộc backfill, trả exit 2, giữ node pause/rào thủ công bền, `Phase=Restoring`, không `ArmRecovery` và **không tự Ready**, kể cả sau restart host. Quy trình tiếp: backfill dữ liệu thiếu vào một root đã kiểm chứng, tạo backup Verified mới có watermark còn đủ journal, chạy **strict restore** từ bản đó để bàn giao thế hệ mới, sau đó `node-unfence` (preview rồi `--apply`) để gỡ `ManualFence`. Không có lệnh nào "xác nhận backfill" bằng lời hay cờ force.
6. `node-unfence` khi `Phase` khác `None`: chỉ gỡ nếu chính host đó vừa hoàn tất strict restore, còn chứng cứ thế hệ đã bàn giao và journal chưa có gap sau cursor. Restart host làm mất chứng cứ RAM ⇒ chạy lại strict restore trước khi gỡ. CAS theo thế hệ ngăn mở nhầm rào khi restore khác chen vào; gỡ rào không sửa cursor/pha phục hồi và không tự Ready.
7. **Timeout:** preview/restore băm lại toàn bộ backup nên đặt `--timeout-seconds` đủ lớn (≈ dung lượng backup ÷ tốc độ đọc đĩa/UNC, cộng thời gian dừng/chép khi apply). CLI hết hạn (exit 124) không huỷ việc Worker đã nhận. Sau chép chỉ so số file/tổng byte (hash đã kiểm trước khi dừng node); `BackupRoot` phải chỉ cấp quyền cho tài khoản service.

Nên restore thử trên môi trường thử trước, không phải production.

### 9.6 Recovery khi host chết giữa backup

- Dấu node do backup dừng được ghi bền (`WriteThrough` + `Flush(true)`) trước khi dừng, vào `Backup.StateDirectory` (đĩa local bền, ngoài root node và backup root, đường dẫn tuyệt đối ổn định qua deploy, chỉ tài khoản service có quyền). Owner lock chống phục hồi chồng backup dùng cùng state.
- **Startup recovery** chạy trước replicator/lịch/IPC (kể cả khi đã tắt lịch) để start lại node còn dấu; không xoá dấu khi node chưa start/probe thành công. Marker dừng rỗng/hỏng được coi là có thể đã dừng ⇒ chạy `EnsureStarted`. Node khởi động lỗi giữ dấu/rào và Timeline Error; host vẫn phục vụ journal/node còn lại. `Direct`/`Nodes` trống không chạy recovery.
- Node không còn trong cấu hình/control phải được vận hành xử lý dấu theo root cũ; không tự suy node/service mới.
- Khi bật `Backup.Enabled` hoặc `AdminPipeName`, DI đặt `HostOptions.ShutdownTimeout` ≥ `NodeStopTimeout + NodeStartTimeout + 30s` (mặc định 180s). Host không dùng DI này phải giữ mức tối thiểu tương ứng, và SCM/OS phải cấp thời gian shutdown tương ứng; process bị kill vẫn dựa vào dấu để phục hồi ở startup sau.

### 9.7 Bản mồ côi

Startup recovery chỉ dọn **đích của lượt backup có định danh lưu bền** trong `StateDirectory`, sau khi node đã start/probe thành công, có marker sở hữu hợp lệ và không có catalog Verified trỏ cùng đường dẫn. Các thư mục owned khác ngoài catalog chỉ được **cảnh báo** lúc startup để vận hành đối chiếu (catalog có thể đã mất) — không tự xoá. Dấu cũ chỉ lưu root node không cấp quyền tự xoá bản nào. Lỗi dọn ghi Warning/Timeline và giữ dấu để startup sau thử lại; lượt backup mới trên node đó bị chặn tới khi xử lý dấu: gỡ nguyên nhân (file bị khoá, quyền, catalog SQL) rồi **restart Worker**.

### 9.8 Giới hạn

- Chỉ điều khiển Windows service **local**; topology remote cần adapter `INodeControl` riêng. Adapter đọc nhãn `STATE` của `sc.exe`: kiểm trên Windows bản địa hoá trước rollout.
- `BackupRoot` qua reparse point/symlink/DFS mount bị từ chối; không cấu hình đường dẫn lab.
- IPC dùng `PipeOptions.CurrentUserOnly` ở cả hai đầu.
- Lỗi phần cứng/driver không thể bảo đảm khởi động lại trong ngân sách phần mềm.

---

## 10. Chẩn đoán và sự cố

### 10.1 Log

| Category (bật Debug khi điều tra) | Ghi gì |
|---|---|
| `Shared.QuestHa.Infrastructure.Services.Replication` | Mỗi probe theo node/bảng: `writerTxn`, `sequencerTxn`, số checkpoint, tuổi checkpoint già nhất (giây), exists/suspended, `probeEpoch/epoch/accepted`; lý do mỗi lần đổi trạng thái. Mẫu epoch cũ có `accepted=false`. |
| `Shared.QuestHa.Infrastructure.Services.Routing` | Node bị loại và lý do (State, heartbeat cũ, breaker, đã thử trong request). |

Information chỉ ghi khi đổi `State` với lý do (journal lag, WAL chưa apply/tuổi backlog, probe cũ, reconnect/epoch, rào). Log vận hành chỉ in loại exception/mã `SqlException.Number`. Debug chỉ in chi tiết **đã làm sạch** (che `Password=`/`Pwd=`, thay chuỗi giống chuỗi kết nối bằng `[connection string ***]`, kèm stack trace; không in `Exception.Data`). Sau lỗi mở kết nối SQL, journal chỉ `ClearPool` đúng pool liên quan tối đa một lần/giây. Tắt Debug khi xong.

Log quan trọng cần có trong hệ thống cảnh báo: `[QuestHa]` Warning/Error từ Health worker; `[QuestHa] Áp tham số vận hành …` (VdsConfig đã áp, mục 3.2); Warning khi purge dừng/bị chặn; Timeline `JournalBacklog`.

### 10.2 Triệu chứng → nguyên nhân → xử lý

| Triệu chứng | Nguyên nhân thường gặp | Xử lý |
|---|---|---|
| API VDS trả 503 `QuestHaJournalUnavailable`, `Retry-After` 5s | Journal SQL không đọc được (AG failover, mạng, timeout, hết quyền) | Kiểm SQL Server/AG và kết nối từ WebAPI/Worker; hết lỗi thì tự hồi phục, không cần restart. Đọc không bao giờ trả dữ liệu thiếu. |
| 503 `QuestHaNotCovered` kéo dài | Cursor/WAL của cả hai node chưa phủ head trong `CoverageWait` | `status`: xem pending/lag từng node; kiểm replicator còn chạy, node có WAL suspend không. Cân nhắc tăng `router_coverage_wait` (tối đa 1m) sau khi đã đo. |
| 503 mã `QuestHa` (không có node đủ điều kiện) | Cả hai node Down/heartbeat cũ/breaker/đang CatchingUp | Xem Debug `Routing`; kiểm replicator còn ghi heartbeat (`Router.HeartbeatStaleAfter` 10s); khôi phục node. |
| `status` exit 2, Health Critical "chưa có backup" | `BackupRequired=true` khi chưa triển khai backup | Đặt `BackupRequired=false` đến khi có bản Verified đầu tiên (mục 2.4). |
| Backlog Warn/Critical | Pending/tuổi pending/dung lượng journal vượt ngưỡng | Xem node nào chậm (cursor, lag); node Down lâu thì khôi phục hoặc `cursor-retire` nếu đã bỏ node. Ngưỡng chỉ cảnh báo, không chặn append. |
| Node `CatchingUp` lâu | Journal lag > `ReadyMaxLag`, WAL chưa áp hoặc mốc WAL đứng, vừa reconnect (đang replay từ `SafeSeq`), tải Statistic dồn một mốc | Bật Debug `Replication`: xem `writerTxn` so với `sequencerTxn`, tuổi checkpoint, `accepted`. Replay sau reconnect kéo dài ~`RewindOnReconnect` là bình thường. Chưa áp WAL kéo dài ⇒ kiểm đĩa/CPU node phụ, WAL suspend. |
| Log `node kết nối lại ⇒ lùi cursor X → 0` | `SafeSeq` còn 0 (journal mới hoặc chưa có mốc giao đủ tuổi) | Đúng thiết kế; không phải lỗi. |
| `purge` exit 2 với `UnconfiguredNodeCursor` | Còn cursor của node đã bỏ khỏi `Nodes` | Dừng replicator cũ rồi `cursor-retire --node <X> --apply`. |
| `purge` exit 2 với `NonMonotonicCreatedUtc` | Dòng trẻ chặn dòng đủ tuổi phía sau (đồng hồ SQL lùi, sửa tay `CreatedUtc`) | Điều tra `BlockingSeq`; không tự xoá tay. |
| `purge` không xoá gì | Thiếu backup Verified, cursor chưa phủ, còn quarantine mở, chưa đủ `Retention` | Đọc `Reason` trong JSON; xem mục 7.2. |
| `export` exit 2 `NoReadyNode` / `NotCovered` | Không node nào Ready (heartbeat, CatchingUp, pause, thiếu `HttpUrl`) hoặc node chưa phủ head/WAL trong `Export:CoverageWait` | `status`; chờ node bắt kịp; lịch tự thử lại sau `export_retry_delay`. |
| `export` `TableFailed` "file có X dòng ≠ đếm PG Y" | Dữ liệu của giờ đó đổi trong lúc xuất (đến muộn) | Lần thử sau tự xuất lại đúng giờ đó; kéo dài thì kiểm nguồn ghi giờ cũ. |
| `export` `TableFailed` "HTTP 401/403" | Node bật Basic auth mà thiếu/sai `HttpUser`/`HttpPassword` | Cấp lại qua biến môi trường. |
| Health `export.old`/`export.stale` | Ngày hôm qua chưa xuất xong sau 02:00 + 2 giờ (hết lượt thử, `export_dry_run=1`, `ExportRoot` đầy) | Xem log `[QuestHa.Export]`/Timeline `Export`; chạy tay `export --day <ngày> --apply`. |
| Health `backup.none` "bản xuất Parquet chưa phủ đủ bảng — thiếu/quá hạn: X" | `BackupRequired=true`, chưa có backup lạnh, bảng X chưa có bản xuất Verified hoặc quá `export_critical_after` | Xem lỗi xuất của bảng X; chạy tay `export --day <ngày> --apply`. |
| Log "Giữ lại phiên bản cũ …" / "không xoá thư mục hết hạn …: … mục lạ" | Trong thư mục ngày/phiên bản có file/thư mục không do bản xuất tạo, hoặc link | Kiểm tra và dời mục lạ ra ngoài; lượt sau tự dọn (catalog đã retire trước). |
| `import` exit 1 `IOException` | Cây đích dưới `ParquetCopyRoot` có junction/symlink, hoặc copy root chưa có | Gỡ link, tạo đúng thư mục copy root của node. |
| `import` exit 2 `WalNotApplied` | WAL bảng đích chưa áp xong trong `Export:HttpTimeout` (đang suspend/tải nặng) | Kiểm `wal_tables()`; nạp lại an toàn (DEDUP). |
| Health `export.catalogUnreadable` / purge lỗi `QuestExportCatalog` | Chưa chạy migration catalog mà đã bật `Export:Enabled` | Chạy `20261102_QuestJournal_Add_ExportCatalog.sql`. |
| `import` `InvalidDataException` | Thiếu manifest (bản dở) hoặc file lệch SHA-256 | Dùng ngày khác hoặc xuất lại ngày đó; không sửa tay manifest. |
| `backup-run` bị từ chối khi preview/apply | Thiếu catalog/migration, `BackupRoot` không ghi được, thiếu dung lượng, dự toán vượt `MaxStoppedDuration`, peer chưa Ready, `StateDirectory` không tuyệt đối | Đọc câu trả lời CLI; chạy migration `RetiredUtc`; chỉnh `BackupRoot`/quyền/dung lượng; chờ peer Ready; chỉnh `MaxStoppedDuration`/`MinThroughputBytesPerSecond` nếu ổ đủ nhanh. |
| `backup-run` không kết nối được | `AdminPipeName` rỗng, Worker không chạy, hoặc khác tài khoản Windows | Khai `AdminPipeName`, chạy CLI cùng tài khoản service, trên máy Worker. |
| Lượt backup mới trên node bị chặn, log báo còn dấu dừng chưa dọn | Startup recovery chưa dọn được dấu của lượt trước (file khoá, quyền, catalog SQL) | Gỡ nguyên nhân rồi **restart Worker** để startup recovery thử lại. |
| `restore` báo `InvalidDataException` hoặc từ chối | Backup lệch manifest, không có manifest, chưa Verified, đã retired, journal thiếu sau watermark | Dùng bản Verified khác; tạo bản mới; journal thiếu thì xem 9.5 mục 5. Node không bị đụng khi lỗi ở bước kiểm. |
| Node sau restore không tự Ready | `--allow-journal-gap` hoặc restore lỗi giữa chừng ⇒ giữ rào bền | Quy trình 9.5 (backfill → backup mới → strict restore → `node-unfence`). Không dùng `node-unfence` để bỏ qua kiểm chứng. |
| Sửa VdsConfig nhưng Worker không đổi | Gói có một giá trị sai ⇒ bị từ chối cả gói; hoặc chưa tới chu kỳ nạp | Xem Warning của `QuestHaManager`/`OpConfigManager` (liệt kê lý do); sửa giá trị rồi lưu lại (phát refresh). |
| Sửa `router_*` nhưng WebAPI không đổi | Sửa thẳng DB, không qua API | Restart WebAPI hoặc lưu lại dòng đó trên web. |
| CLI exit 124 | Hết `--timeout-seconds` | Tăng timeout (restore/preview băm lại toàn bộ backup). Việc Worker đã nhận vẫn chạy. |

### 10.3 Giới hạn đã biết

- **Lượt đọc coverage chậm trùng hết `CoverageWait`:** nếu một lượt đọc coverage SQL chậm đúng lúc hết `CoverageWait`, trả 503 `QuestHaJournalUnavailable` (cố ý — lỗi journal không bị che thành "chưa phủ") và không thử node kế tiếp; client thử lại sau `Retry-After` 5s. Từng gặp ngay sau Fence trong diễn tập backup.
- **Tải Statistic dồn một mốc:** nhiều camera chốt cùng một mốc thống kê làm node phụ áp WAL chậm ⇒ có thể gián đoạn đọc vài chục giây. Fail-closed, không trả dữ liệu sai.
- **Pool node phụ reset lặp** khi node đó dao động giữa Ready và không Ready (mỗi lần reset tối đa một lần/giây/node).
- `GetStatusAsync` ở WebAPI dùng ngưỡng health của file cấu hình (WebAPI không áp nhóm health từ VdsConfig); CLI dùng ngưỡng của file `--config`.
- `NotCovered` không phạt node: không tăng breaker, `ConsecutiveFailures=0`. Muốn biết breaker xem `BreakerTripped`, trạng thái và tuổi heartbeat từng node; thông điệp "lỗi kết nối liên tiếp" của `QuestDbUnavailableException` cũng được dựng cho nhánh hết node nên không suy ra breaker từ câu chữ.
- `LagSeq` chỉ đo cursor journal, không chứng nhận WAL đã áp. Chưa có số đo để nới `CoverageWait`/`ReadyMaxLag`; giữ `CoverageWait=3s`, `ReadyMaxLag=2s`, `AllowDegradedRead=false`.
- Hàng đợi đầu vào Wolverine ở RAM (mục 1).

---

## 11. Chưa kiểm chứng trên hạ tầng thật

Cần làm trên hạ tầng thật trước khi bật production: Export Parquet trên topology hai máy (node phụ ở máy khác, `/exp` qua mạng, Basic auth, UNC `ExportRoot`), ngày đỉnh thật theo giờ khi node phụ đang nhận replicate, lịch 02:00 trong Worker thật và kiểm lại D-2, nạp ngược vào node đích qua copy root UNC; adapter `sc.exe` với Windows service thật; node chết thật trên máy khác (diễn tập mới mô phỏng phía client), mạng đứt hoặc trễ cao; node còn lại chết giữa lúc chép backup; lịch backup tự động và IPC `backup-run`/`restore`; WAL suspend, đĩa đầy, lệch đồng hồ; tốc độ đĩa/NAS production; hiệu năng với SQL Server và topology thật; đường đọc WebAPI chạy host thật dưới tải (mới kiểm bằng test đơn vị); đường nạp tham số VdsConfig ở host thật (Lamar ở WPF, Furion ở WebAPI) — xác nhận bằng cách đổi `vds_questha_health_lag_warn_seconds` trên web rồi tìm log `[QuestHa] Áp tham số vận hành` ở Worker.

Chưa bật bất kỳ chế độ HA nào trên môi trường đang chạy; cần đo tải trên topology tách máy và diễn tập crash/mất ACK/WAL suspend, backup/restore trước production.
