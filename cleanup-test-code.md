# Kế Hoạch Dọn Dẹp Mã Nguồn Rác & Không Sử Dụng Trong ShareDataWorkerTests.cs

## 1. Mục Tiêu
Loại bỏ các `using` không sử dụng và các hàm dọn dẹp dư thừa/rỗng (stub methods) trong tập tin test `ShareDataWorkerTests.cs` nhằm làm sạch mã nguồn, tối ưu hóa kích thước file và tránh gây hiểu lầm cho người phát triển.

---

## 2. Chi Tiết Các Thành Phần Cần Loại BỎ

### Phase 1: Thư Viện Không Sử Dụng (Unnecessary `using` Directives - IDE0005)
1. **`using Microsoft.Extensions.Logging;`** (Dòng 4): 
   - Không có thành phần logging nào được gọi trực tiếp trong `ShareDataWorkerTests.cs`.
2. **`using System.Security.Cryptography;`** (Dòng 11): 
   - Không có lớp hoặc hàm băm mã hóa nào được gọi trong `ShareDataWorkerTests.cs`.

---

### Phase 2: Hàm Helper Rỗng Không Sử Dụng (Unused Dead Method)
1. **`CleanupTestDataForPacket(...)`** (Dòng 1033 - 1036):
   - Hàm dọn dẹp dữ liệu giả lập theo loại gói tin `101-111`.
   - Hiện trạng: Chỉ chứa `return Task.CompletedTask;` và **không được gọi ở bất kỳ đâu** trong toàn bộ solution.
   - Hành động: Xóa bỏ hoàn toàn định nghĩa hàm.

---

### Phase 3: Hàm Helper Rỗng & Các Lời Gọi Dư Thừa (Redundant Stub Method & No-Op Calls)
1. **`CleanupConfiguredSource(...)`** (Dòng 2531 - 2534):
   - Hàm dọn dẹp `DataSource` và `MappingProfile` rỗng (`return Task.CompletedTask;`).
   - Hiện trạng: Đang được gọi dư thừa trong khối `finally` của **12 bài test** (Dòng 3753, 3797, 3839, 3878, 3915, 3957, 3996, 4034, 4098, 4145, 4189, 4242).
   - Hành động:
     - Xóa tất cả 12 lời gọi `await CleanupConfiguredSource(db, dataSource, profile);` trong các khối `finally`.
     - Xóa hoàn toàn định nghĩa hàm `CleanupConfiguredSource`.

---

## 3. Kế Hoạch Kiểm Thử & Xắc Nhận (Verification Plan)
1. **Kiểm tra biên dịch**: Đảm bảo project `ShareDataWorker.Tests.csproj` biên dịch thành công 0 lỗi, 0 cảnh báo liên quan.
2. **Kiểm tra bài test**: Chạy lại các unit test liên quan để đảm bảo việc xóa code rác không làm ảnh hưởng đến kết quả test.
