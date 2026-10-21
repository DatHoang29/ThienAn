---
name: gortex-tool-map
version: 1.1.0
priority: P0
trigger: always_on
description: Quy trình nạp tool Gortex deferred và quy định riêng của dự án ThienAn về công cụ Gortex MCP.
---

# Gortex — Quy Trình Nạp Tool & Quy Định Dự Án

> **Thuộc hệ thống SSOT `.agents/rules/`.** Tool Gortex MCP là deferred, nạp trước khi đọc/tìm mã nguồn.

## 1. Nạp Tool Deferred

Trước khi thao tác trên mã nguồn, nạp tool schema:
```
ToolSearch: select:mcp__gortex__explore,mcp__gortex__search_symbols,mcp__gortex__get_symbol_source,mcp__gortex__search_text,mcp__gortex__find_usages,mcp__gortex__read_file,mcp__gortex__edit_file,mcp__gortex__verify_change,mcp__gortex__detect_changes
```

## 2. Trường Hợp Ngoại Lệ Được Dùng Read / Grep / Glob / Bash

- File **không** phải mã nguồn đã index: `.md`, `.json`, `.sql`, cấu hình, tài liệu `DocBusinessThienAn/`, plan, scratchpad.
- Thư mục build `bin/`, `obj/`, `node_modules/`, `dist/`.
- Khi Gortex timeout / báo lỗi tích hợp thật: báo rõ cho user, không tự fallback sang shell.

## 3. Cơ Chế Cưỡng Chế (Hook Guardrails)

Hook `.agents/hooks/gortex-enforce.mjs`:
- `PreToolUse`: Chặn Read/Grep/Glob/Bash đụng vào file `.cs/.vue/.ts/.tsx/.js` ngoài vùng miễn trừ (exit code 2).
- Tắt tạm khi daemon Gortex không chạy: đặt biến môi trường `GORTEX_ENFORCE=off`.
