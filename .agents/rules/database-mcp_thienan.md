---
name: database-mcp
version: 1.0.0
priority: P1
trigger: model_decision
description: Database MCP tooling (Gortex and DAB MCP), staging DB read-only safety, and module isolation scope.
---

# 🗄️ Quy Chuẩn Database & MCP Tooling — Thiên Ân

> **Thuộc hệ thống SSOT `.agents/rules/`**. Quy định giao thức công cụ Gortex MCP, DAB MCP Staging, an toàn CSDL và cách ly module.

---

## ⚡ 1. Dual Source of Truth: Gortex & DAB MCP (Rule 19.62 - P0)

1. **Gortex-First Protocol cho Mã Nguồn**:
   - Mọi thao tác tìm kiếm, đọc, phân tích và sửa mã nguồn (C#, Vue, TypeScript, SQL...) BẮT BUỘC gọi công cụ Gortex MCP (`call_mcp_tool` với `ServerName: "gortex"`).
   - Tận dụng đồ thị AST để đo blast radius, tìm callers, usages và phân tích ngữ cảnh trước khi sửa code.
2. **DAB MCP Staging cho Cơ Sở Dữ Liệu**:
   - Mọi kiểm tra schema, danh sách bảng, cột dữ liệu thực tế BẮT BUỘC đọc trực tiếp từ DB staging (`10.10.8.30/DEV_ITS10`) qua DAB MCP (`mssql_staging__describe_entities`, `read_records`, `aggregate_records`).
   - ⛔ Tuyệt đối không phỏng đoán cột/bảng từ Entity code cũ hoặc tài liệu không cập nhật.
3. **Cơ Chế Fallback (Default-as-Fallback)**:
   - CHỈ KHI Gortex/DAB MCP không thực hiện được (báo lỗi, timeout, file chưa được đánh chỉ mục, hoặc thao tác trên file tài liệu markdown) mới chuyển sang dùng công cụ mặc định hệ thống.

---

## 🔒 2. An Toàn CSDL: Read-Only & Cấm Tự Chạy DDL/DML (P0 Safeguards)

1. **MCP Database Read-Only Rule (Section 10)**:
   - Kết nối DAB MCP vào DB staging CHỈ hoạt động ở chế độ **Read-Only** (`anonymous:read`).
   - ⛔ **TUYỆT ĐỐI CẤM** dùng MCP để thực thi các câu lệnh thay đổi dữ liệu (`INSERT`, `UPDATE`, `DELETE`) trên CSDL staging.
2. **Strict Manual SQL Execution & Direct `.sql` File Edit Rule (Section 9 & Rule 19.4)**:
   - ⛔ **CẤM TUYỆT ĐỐI** tự ý chạy các câu lệnh DDL (`ALTER TABLE`, `CREATE TABLE`, `DROP TABLE`, `CREATE INDEX`) hoặc script thay đổi dữ liệu trực tiếp trên bất kỳ môi trường DB nào.
   - ⛔ **CẤM** tự viết hàm tạo bảng dạng `EnsureTablesCreated()` hoặc gọi `InitTables<T>()` trong code.
   - ✅ **Quyền sửa trực tiếp file `.sql` (`DocBusinessThienAn/**/sql/*.sql`)**: AI **ĐƯỢC QUYỀN tạo mới và sửa/cập nhật trực tiếp vào các tệp kịch bản `.sql`** mà không cần chờ viết prompt xin phép trước. Lập trình viên sẽ là người kiểm tra (check) và chạy thực thi cuối cùng trên CSDL.
   - **Quy chuẩn bắt buộc khi viết/sửa file `.sql`:**
     - Với các kịch bản chạy trên CSDL chính Staging, đầu file BẮT BUỘC có `USE [DEV_ITS10]; GO` và ghi rõ schema `dbo.<TênBảng>` để tránh lỗi `Msg 208: Invalid object name` khi tab query đứng nhầm database.
     - Đối chiếu cấu trúc cột và dữ liệu thực tế qua `mssql_staging` trước khi viết (ví dụ: bảng `dbo.SysTerminology` lưu tiền tố nhóm ở cột `Name = N'lz.label' | N'lz.exception' | N'lz.validation'...`, phần mã sau tiền tố ở cột `Code`, và nội dung dịch thuật ở cột `Value`).

---

## 🛡️ 3. Cách Ly Module & An Toàn Hạ Tầng (Scope Isolation)

1. **Module Isolation Scope (Section 8)**:
   - Mỗi phân hệ (ShareData, TMS, VideoWall, Toll...) sở hữu tập bảng và logic nghiệp vụ riêng.
   - ⛔ CẤM viết câu lệnh SQL query chéo trực tiếp vào bảng nội bộ của module khác. Giao tiếp giữa các module phải thông qua API endpoint hoặc Event Bus (`MessBus`).
2. **An Toàn Hạ Tầng & Docker (Section 17)**:
   - ⛔ CẤM tự ý can thiệp vào Docker containers, khởi động lại service nền, chạy `docker-compose down/up` hoặc thay đổi cấu hình mạng hạ tầng trừ khi có chỉ đạo trực tiếp từ Leader.
