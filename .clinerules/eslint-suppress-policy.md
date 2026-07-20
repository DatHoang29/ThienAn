## Brief overview
Quy tắc xử lý ESLint warning/error trong dự án. Ưu tiên cấu hình ESLint đúng cách thay vì dùng inline disable comment chắp vá.

## Khi gặp ESLint warning hoặc error
- **KHÔNG** dùng `// eslint-disable-next-line` hoặc `/* eslint-disable */` để tắt rule một cách chắp vá.
- Thay vào đó, cấu hình rule trong file `.eslintrc.json` (hoặc `.eslintrc.js`) cho đúng scope cần áp dụng.
- Ví dụ: nếu test files cần dùng `require()` (dynamic import trong Jest), thêm override trong `.eslintrc.json`:
  ```json
  {
    "overrides": [
      {
        "files": ["**/*.test.ts", "**/*.test.tsx"],
        "rules": {
          "@typescript-eslint/no-require-imports": "off"
        }
      }
    ]
  }
  ```

## Trường hợp ngoại lệ (được phép dùng inline disable)
- Chỉ khi rule cần tắt cho **đúng 1 dòng duy nhất** và không có pattern lặp lại trong project.
- Nếu thấy phải dùng inline disable comment ở **2 nơi trở lên** cho cùng một rule → bắt buộc chuyển sang cấu hình ESLint override.

## Quy trình khi sửa ESLint issues
1. Xác định rule nào đang vi phạm (ví dụ: `@typescript-eslint/no-require-imports`).
2. Kiểm tra xem pattern này có lặp lại ở nhiều file không (dùng `search_files`).
3. Nếu lặp lại: thêm override vào `.eslintrc.json` cho đúng file pattern (ví dụ: `**/*.test.ts`).
4. Nếu chỉ 1 chỗ duy nhất: cho phép inline disable kèm comment giải thích lý do.
5. Chạy lại ESLint để xác nhận không còn warning/error.