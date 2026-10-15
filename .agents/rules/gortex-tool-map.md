---
name: gortex-tool-map
version: 1.0.0
priority: P0
trigger: always_on
description: Bảng ánh xạ tên tool Gortex trong hướng dẫn (search/read/relations/change) sang tên tool MCP thực tế, và quy trình nạp tool deferred.
---

# Gortex — Bảng ánh xạ tool & quy trình bắt buộc

> **Thuộc hệ thống SSOT `.agents/rules/`.** Hướng dẫn máy (`~/.gortex/instructions/active.md`) dùng tên rút gọn (`search`, `read`, `relations`, `change`), trong khi tool MCP trong phiên có thể mang tên đầy đủ `mcp__gortex__*`. Luôn kiểm tra tên thật bằng ToolSearch, không suy ra "chưa có tool".

## 1. Bước đầu tiên của mọi phiên đụng tới mã nguồn

Tool Gortex là **deferred**: chưa có schema thì gọi sẽ lỗi. Nạp trước khi đọc/tìm mã:

```
ToolSearch: select:mcp__gortex__explore,mcp__gortex__search_symbols,mcp__gortex__get_symbol_source,mcp__gortex__search_text,mcp__gortex__find_usages,mcp__gortex__read_file,mcp__gortex__edit_file,mcp__gortex__verify_change,mcp__gortex__detect_changes
```

Nếu tool đã hiện sẵn dạng rút gọn (`mcp__gortex__search`, `read`, `relations`, `change`) thì dùng luôn theo bảng dưới.

## 2. Ánh xạ

| Hướng dẫn (rút gọn) | Tool đầy đủ | Dùng khi |
|---|---|---|
| `explore(operation:"localize"/"task")` | `mcp__gortex__explore` | Tìm file/symbol cho một yêu cầu, hoặc bắt đầu chẩn đoán/sửa |
| `search(operation:"symbols")` | `mcp__gortex__search_symbols` | Tìm theo tên symbol/khái niệm |
| `search(operation:"text")` | `mcp__gortex__search_text` | Tìm chuỗi/regex literal |
| `read(target:{symbol})` | `mcp__gortex__get_symbol_source` | Đọc thân một hàm/lớp |
| `read(operation:"file")` | `mcp__gortex__read_file` | Đọc cả file được yêu cầu đích danh |
| `relations(operation:"usages"/"callers")` | `mcp__gortex__find_usages` / `get_callers` | Ai dùng/gọi symbol |
| `change(operation:"impact")` | `mcp__gortex__get_editing_context` / `verify_change` | Trước khi sửa |
| `edit` / `refactor` | `mcp__gortex__edit_file` / `edit_symbol` / `rename_symbol` | Sửa mã đã index |
| `change(operation:"detect")` | `mcp__gortex__detect_changes` | Sau khi sửa |

## 3. Được dùng Read/Grep/Glob/Bash khi

- File **không** phải mã nguồn đã index: `.md`, `.json`, `.sql`, cấu hình, tài liệu `DocBusinessThienAn/`, plan, scratchpad.
- Thư mục build `bin/`, `obj/`, `node_modules/`, `dist/`.
- Gortex báo lỗi tích hợp thật (tool vắng dù server đã cấu hình) → báo cho user, **không** tự fallback sang shell.

## 4. Cưỡng chế

Hook `.agents/hooks/gortex-enforce.mjs` (đăng ký trong `.claude/settings.json`, launcher `.agents/hooks/gortex-hook.mjs` trong `.claude/settings.local.json` dùng `$CLAUDE_PROJECT_DIR`):
- `PreToolUse` Read|Grep|Glob|Bash → exit 2 khi đụng `.cs/.vue/.ts/.tsx/.js` ngoài vùng được miễn.
- `SessionStart` → nhắc nạp tool.
- Tắt tạm khi daemon Gortex không chạy: đặt `GORTEX_ENFORCE=off`.
