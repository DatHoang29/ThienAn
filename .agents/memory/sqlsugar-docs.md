---
type: reference
created: 2026-08-04
updated: 2026-08-04
---

# SqlSugar Documentation (No-Entity & Low-Code)

The following resources cover SqlSugar's No-Entity queries, string expressions, raw SQL, and JSON‑to‑SQL capabilities:

- https://www.donet5.com/home/Doc?typeId=1197
- https://www.donet5.com/home/Doc?typeId=1198
- https://www.donet5.com/home/Doc?typeId=2420
- https://www.donet5.com/home/Doc?typeId=2421
- https://www.donet5.com/home/Doc?typeId=2422
- https://www.donet5.com/home/Doc?typeId=2423
- https://www.donet5.com/home/Doc?typeId=2424
- https://www.donet5.com/home/Doc?typeId=2562
- https://www.donet5.com/home/Doc?typeId=2569

These links provide code examples for:
- **No‑Entity Query** (`Queryable<object>()`, `AS`, `AddJoinInfo`)
- **String Expressions** (`Select`, `Where`, `OrderBy` using strings)
- **Raw SQL** (`Ado.GetDataTableAsync` with parameters)
- **JSON → SQL** dynamic query building

---

## CodeFirst & Schema Migration Pitfalls (Dự án Thiên An)

### 1. Bẫy `EnableIncreTable` (Tử huyệt khiến CodeFirst KHÔNG tạo bảng)
Trong file cấu hình hạ tầng `SqlSugarSetup.cs` (`Shared.Infrastructure.dll`), logic quét entity tạo bảng được viết như sau:
```csharp
List<Type> source = (from element in App.EffectiveTypes
    where !element.IsInterface && !element.IsAbstract && element.IsClass && element.IsDefined(typeof(SugarTable), inherit: false)
    where !element.GetCustomAttributes<IgnoreTableAttribute>().Any()
    select element)
    .WhereIF(P_1.TableSettings.EnableIncreTable, (Type type2) => type2.IsDefined(typeof(IncreTableAttribute), inherit: false))
    .ToList();
```
- Nếu bật `"EnableIncreTable": true`, SqlSugar sẽ lọc và **CHỈ quét những Entity có gắn attribute `[IncreTableAttribute]`**.
- Vì trong toàn bộ repo hiện tại **không có entity nào gắn `[IncreTableAttribute]`**, danh sách bảng cần khởi tạo sẽ bị rỗng (`source.Count == 0`) ⇒ **KHÔNG CÓ BẢNG NÀO ĐƯỢC TẠO** dù `EnableInitTable: true`!
- **Cấu hình ĐÚNG khi cần đồng bộ bảng/cột local:**
  ```json
  "DbSettings": { "EnableInitDb": true },
  "TableSettings": { "EnableInitTable": true, "EnableIncreTable": false }
  ```
- Sau khi chạy test tạo schema xong, **luôn revert toàn bộ cờ về `false`**.

### 2. Bẫy `Expression Tree` với biến Private / Static trong SqlSugar
Khi viết LINQ query với SqlSugar (ví dụ `.Where(x => x.Field == MyClass.SomeVar)`):
- Nếu `SomeVar` là **`private` hoặc `private static`**, SqlSugar Expression parser sẽ quăng lỗi runtime:
  `SqlSugarException : Field "SomeVar" can't be private`
- **Cách khắc phục:**
  1. Khai báo thuộc tính là `public static readonly`.
  2. Hoặc gán ra biến cục bộ (`var target = MyClass.SomeVar;`) bên trong hàm trước khi truyền vào biểu thức `.Where(x => x.Field == target)`.

You can refer to this file in future sessions when needing SqlSugar patterns.
