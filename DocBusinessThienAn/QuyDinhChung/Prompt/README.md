# Prompt — Quy định chung & bộ khung AI (harness)

Prompt cho các việc dùng chung toàn repo (rule `.agents/`, hook, cấu hình Claude Code), không thuộc phân hệ nào.

| # | Prompt | Nguồn | Trạng thái |
|---|---|---|---|
| 1 | `harness-thu-gon-rule-nap-san-prompt.md` | `/doctor` 21/10/2026: rule nạp sẵn ~76k ký tự (~19k token) mỗi phiên, `thienan_rules.md` 54,6k vượt ngưỡng | ✅ **Đã thực thi và xoá theo yêu cầu (21/10/2026).** Thu gọn phần nạp sẵn từ 72.280 ký tự xuống 19.868 ký tự (giảm ~72,5%, tiết kiệm ~13.100 token/phiên; riêng `thienan_rules.md` giảm từ 55.155 xuống 11.930 ký tự ≤ 12k). Phân loại toàn bộ 58 quy tắc sang 21 quy tắc P0/Chung nạp sẵn và bảng chỉ mục 6 miền quy chuẩn chuyên sâu; bảo toàn 100% số hiệu rule và quy chuẩn nghiệp vụ. |
| 2 | `harness-hook-gortex-chon-local-va-sua-hook-loi-prompt.md` | `/doctor` 21/10/2026: 2 bộ hook Gortex trên 8 sự kiện; 3 hook dự án ghi lỗi 100% lần chạy | ✅ **Đã thực thi và xoá theo yêu cầu (21/10/2026).** Đã bỏ 8 hook Gortex cấp người dùng khỏi `~/.claude/settings.json`, bổ sung `--mode=enrich` vào bộ hook Gortex local (`.claude/settings.local.json`); sửa 3 hook dự án trong `.claude/settings.json` dùng `$CLAUDE_PROJECT_DIR` và tăng timeout lên 10s, giải quyết triệt để lỗi `MODULE_NOT_FOUND` khi chuyển thư mục làm việc. |
