## Brief overview
Quy tắc bắt buộc chạy TypeScript typecheck sau mỗi khi hoàn thành một task/plan, trước khi báo cáo hoàn thành. Áp dụng cho dự án aicc_mobile.

## Quy tắc chạy typecheck

- **SAU KHI hoàn thành bất kỳ task nào**: Chạy `npm run typecheck` (hoặc `npx tsc --noEmit`) để xác nhận không có TypeScript errors
- **TRƯỚC KHI dùng attempt_completion**: Phải đảm bảo typecheck pass
- **Nếu typecheck fail**: Fix các lỗi TypeScript trước khi hoàn thành task

## Cú pháp chạy
```bash
# Trong thư mục aicc_mobile
cd aicc_mobile && npm run typecheck

# Hoặc chạy trực tiếp
npx tsc --noEmit
```

## Trigger cases
- Sau khi tạo/sửa file TypeScript
- Sau khi refactor code
- Trước khi báo cáo hoàn thành task