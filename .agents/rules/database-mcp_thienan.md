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

## ⚡ 1. Dual Source of Truth: Gortex & DAB MCP (Rule 19.62)

1. **Gortex-First Protocol cho Mã Nguồn**:
   - Mọi thao tác tìm kiếm, đọc, phân tích đồ thị và sửa mã nguồn (C#, Vue, TypeScript, SQL...) BẮT BUỘC dùng tool Gortex MCP (tên tool và ánh xạ xem [gortex-tool-map.md](./gortex-tool-map.md)).
   - Tận dụng đồ thị AST để đo blast radius, tìm callers, usages và phân tích ngữ cảnh trước khi sửa code.
2. **DAB MCP Local cho Cơ Sở Dữ Liệu (cập nhật 16/10/2026)**:
   - Mọi kiểm tra schema, danh sách bảng, cột và dữ liệu đọc từ DB **local** qua DAB MCP: `mssql_dev` (Docker `localhost:14333/DEV_ITS10`) và `mssql_test` (Docker `localhost:14333/test`), bằng các tool `describe_entities`, `read_records`, `aggregate_records`.
   - `mssql_staging` (`10.10.8.30/DEV_ITS10`) chỉ dùng khi người dùng yêu cầu trực tiếp.
   - ⛔ Tuyệt đối không phỏng đoán cột/bảng từ Entity code cũ hoặc tài liệu không cập nhật.
3. **Phạm vi đối chiếu khi review / viết plan / viết prompt: chỉ local**:
   - Chỉ đối chiếu code working tree, kết quả test (xUnit trên DB local, Playwright) và DB local.
   - ⛔ CẤM đưa kết quả đọc staging thành phát hiện, việc cần làm, hay căn cứ đánh trạng thái Đã làm/Chưa làm trong báo cáo, plan, prompt hoặc MasterPlan.
   - Kiểm tra staging và triển khai do người dùng tự làm sau khi local ổn; chỉ ghi "chờ người dùng kiểm staging" nếu cần nhắc.
4. **Cơ Chế Fallback (Default-as-Fallback)**:
   - CHỈ KHI Gortex/DAB MCP không thực hiện được (báo lỗi, timeout, file chưa được đánh chỉ mục, hoặc thao tác trên file tài liệu markdown) mới chuyển sang dùng công cụ mặc định hệ thống.

---

## 🔒 2. An Toàn CSDL: Read-Only & Cấm Tự Chạy DDL/DML

1. **MCP Database Read-Only Rule (Section 10)**:
   - Kết nối DAB MCP local `mssql_dev`/`mssql_test` CHỈ hoạt động ở chế độ **Read-Only**; `mssql_staging` chỉ khi người dùng yêu cầu trực tiếp.
   - ⛔ **TUYỆT ĐỐI CẤM** dùng MCP để thực thi các câu lệnh thay đổi dữ liệu (`INSERT`, `UPDATE`, `DELETE`) trên bất kỳ CSDL nào.
2. **Strict Manual SQL Execution & Direct `.sql` File Edit Rule (Section 9 & Rule 19.4)**:
   - ⛔ **CẤM TUYỆT ĐỐI** tự ý chạy các câu lệnh DDL (`ALTER TABLE`, `CREATE TABLE`, `DROP TABLE`, `CREATE INDEX`) hoặc script thay đổi dữ liệu trực tiếp trên bất kỳ môi trường DB nào.
   - ⛔ **CẤM** tự viết hàm tạo bảng dạng `EnsureTablesCreated()` hoặc gọi `InitTables<T>()` trong code.
   - ✅ **Quyền sửa trực tiếp file `.sql` (`DocBusinessThienAn/**/sql/*.sql`)**: AI **ĐƯỢC QUYỀN tạo mới và sửa/cập nhật trực tiếp vào các tệp kịch bản `.sql`** mà không cần chờ viết prompt xin phép trước. Lập trình viên sẽ là người kiểm tra (check) và chạy thực thi cuối cùng trên CSDL.
   - **Quy chuẩn bắt buộc khi viết/sửa file `.sql`:**
     - **Quy chuẩn đặt tên file `.sql` (`ddMMyyyy` bắt buộc)**: Tên mọi tệp kịch bản SQL mới hoặc sửa BẮT BUỘC bắt đầu bằng tiền tố ngày tháng theo định dạng `ddMMyyyy-<ten-mo-ta-kebab-case>.sql` (ví dụ: `15102026-bo-sung-dich-thuat-sql-trich-xuat.sql`, `02102026-dong-bo-thuat-ngu-systerminology.sql`). ⛔ **CẤM** dùng `yyyyMMdd` hay định dạng ngày tháng lộn xộn khác để đảm bảo 1 rule duy nhất toàn dự án. Chuẩn này áp dụng cho script lẻ **không thuộc** bộ script gom theo vai trò dưới đây.
     - **Gom script theo vai trò, không tách theo ngày (bổ sung 16/10/2026):** tách nhiều file theo ngày làm người chạy không biết chạy file nào, theo thứ tự nào. Mỗi phân hệ giữ thư mục `sql/` với **vài file, mỗi file một vai trò cố định**:
       - Tên `NN-<phan-he>-<vai-tro>.sql`, `NN` là **thứ tự chạy** (ví dụ ShareData: `01` cấu trúc bảng/cột, `02` seed mẫu gói tin, `03` seed cấu hình, `04` dịch thuật, `05` dữ liệu giả lập chỉ cho local/test). Không đặt ngày trong tên; ghi `Cập nhật lần cuối: ddMMyyyy` trong phần đầu file.
       - Thay đổi mới thuộc vai trò nào thì **sửa vào đúng file đó**, ⛔ không tạo thêm file theo ngày. Chỉ tạo file mới khi có vai trò chưa có.
       - Mọi file **chạy lại được** (idempotent: `IF NOT EXISTS` / `MERGE` / `UPDATE` có điều kiện), không ghi đè giá trị quản trị đã sửa trên màn hình, có `USE [<TênDB>]; GO`, `PRINT` số dòng đã thêm/đã bỏ qua.
       - Thao tác phá hủy một lần (xóa bảng/cột) tách file riêng `00-<phan-he>-migration-mot-lan.sql`, có `IF EXISTS` bảo vệ và ghi rõ "chỉ chạy một lần".
       - Mỗi thư mục `sql/` có `README.md`: thứ tự chạy, mục đích từng file, môi trường được chạy (file dữ liệu giả lập không chạy lên môi trường thật).
       - Dựa trên cấu trúc bảng/cột hiện có ở DB local khi gom; không đổi nội dung nghiệp vụ của script trong lúc gom.
     - Với các kịch bản chạy trên CSDL triển khai, đầu file BẮT BUỘC có `USE [<TênDB>]; GO` (ví dụ `USE [DEV_ITS10]; GO`) và ghi rõ schema `dbo.<TênBảng>` để tránh lỗi `Msg 208: Invalid object name` khi tab query đứng nhầm database.
     - Đối chiếu cấu trúc cột và dữ liệu qua DB local (`mssql_dev`) trước khi viết (ví dụ: bảng `dbo.SysTerminology` lưu tiền tố nhóm ở cột `Name = N'lz.label' | N'lz.exception' | N'lz.validation'...`, phần mã sau tiền tố ở cột `Code`, và nội dung dịch thuật ở cột `Value`).

---

## 🛡️ 3. Cách Ly Module & An Toàn Hạ Tầng (Scope Isolation)

1. **Module Isolation Scope (Section 8)**:
   - Mỗi phân hệ (ShareData, TMS, VideoWall, Toll...) sở hữu tập bảng và logic nghiệp vụ riêng.
   - ⛔ CẤM viết câu lệnh SQL query chéo trực tiếp vào bảng nội bộ của module khác. Giao tiếp giữa các module phải thông qua API endpoint hoặc Event Bus (`MessBus`).
2. **An Toàn Hạ Tầng & Docker (Section 17)**:
   - ⛔ CẤM tự ý can thiệp vào Docker containers, khởi động lại service nền, chạy `docker-compose down/up` hoặc thay đổi cấu hình mạng hạ tầng trừ khi có chỉ đạo trực tiếp từ Leader.
