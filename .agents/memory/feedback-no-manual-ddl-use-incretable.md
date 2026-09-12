---
name: feedback-no-manual-ddl-use-incretable
description: Absolutely NO manual ALTER TABLE / DDL queries on ANY DB (both local and 10.10.8.30); use SqlSugar EnableIncreTable/EnableInitTable
metadata:
  node_type: memory
  type: feedback
  created: 2026-09-12
---

# Cấm chạy thủ công câu lệnh ALTER TABLE / DDL — Tận dụng Code-First SqlSugar

## Quy tắc bắt buộc:
1. **Tuyệt đối KHÔNG tự ý soạn/chạy lệnh DDL thủ công** (`ALTER TABLE`, `CREATE TABLE`, `DROP COLUMN` qua sqlcmd, PowerShell, SqlConnection, SqlCommand...) trên BẤT KỲ database nào:
   - Server DEV `10.10.8.30`: Tuyệt đối READ-ONLY, cấm mọi hành động DDL/write.
   - Database test cục bộ `127.0.0.1:14333`: Cũng KHÔNG được chạy DDL thủ công.
2. **Luôn tận dụng cơ chế Code-First tự động**:
   - Khi thêm Entity mới hoặc thêm field/cột mới, hãy dựa vào các cấu hình:
     - `EnableInitDb`
     - `EnableInitTable`
     - `EnableIncreTable` (Incremental update bảng/cột của SqlSugar)
   - Trong môi trường test (`Host.cs`), `EnableInitTable` và `EnableIncreTable` đã được cấu hình sẵn để tự động đồng bộ schema an toàn.
