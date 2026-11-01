# `.agents/` — bộ khung AI dùng chung (SSOT)

Mọi công cụ AI (Claude Code, Codex, Antigravity, Gemini CLI, Kiro, Cline) dùng chung thư mục này. Sửa quy tắc, skill, hook hay MCP thì sửa ở đây; tệp riêng của từng công cụ (`.claude/`, `.codex/`, `.gemini/`, `.kiro/`, `.mcp.json`…) chỉ trỏ về hoặc được sinh ra từ đây.

> ⛔ Đã gỡ bộ AG-Kit (01/11/2026): thư mục này do dự án tự quản. Không chạy `ag-kit update` — lệnh đó chép lại toàn bộ skill, agent và công cụ của kit đã bỏ.

## Cấu trúc

| Đường dẫn | Nội dung | Ai đọc |
|---|---|---|
| `rules/` | Quy tắc dự án. `thienan_rules.md` là P0, `core-protocol.md` là quy tắc nền, `request-routing.md` định tuyến sang rule theo miền (BE, FE, test, DB, tài liệu, git) | Claude qua `.claude/CLAUDE.md`, Codex/Kiro qua `AGENTS.md`, Antigravity tự quét |
| `memory/` | Quy ước và quyết định dùng chung cả nhóm; mục lục `MEMORY.md` | Đọc đầu phiên (`core-protocol.md` §3) |
| `skills/` | Skill nạp theo nhu cầu, mỗi skill một `SKILL.md` | Antigravity, Codex; công cụ khác qua `AGENTS.md` |
| `agent/` | Vai chuyên gia (frontmatter `skills:` trỏ tới `skills/`) | Antigravity, điều phối subagent |
| `hooks/` | `guardrails.mjs` (chặn vi phạm P0: test sai chỗ, `git add .`, build FE, `--no-build`…), `gortex-enforce.mjs` + `gortex-hook.mjs` (buộc dùng Gortex), `prompt-submit.mjs` (nhắc SSOT mỗi prompt), `validate-tool-call.mjs` (chặn lệnh phá ổ đĩa); test ở `hooks/tests/` | Claude qua `.claude/settings*.json`, Antigravity qua `hooks.json` |
| `hooks.json` | Đăng ký hook cho Antigravity | Antigravity |
| `mcp_config.json` | Nguồn duy nhất danh sách MCP server | `mcp/sync.mjs`, Antigravity |
| `mcp/` | `sync.mjs` sinh cấu hình MCP cho từng công cụ; `dab.mjs` + `dab-config.json` chạy DAB cho DB dev/test/staging; `.env` chứa chuỗi kết nối (không commit, mẫu `.env.example`) | |
| `scripts/` | `check_doc_links.py` kiểm cấu trúc và link tài liệu `DocBusinessThienAn`; `Fix-DoubledBlankLines.ps1` | Chạy tay |

## MCP

Sửa danh sách server trong `mcp_config.json`, rồi sinh lại cấu hình của từng công cụ (không sửa tay các tệp sinh ra):

```bash
node .agents/mcp/sync.mjs --check                       # exit 1 khi có tệp lệch nguồn
node .agents/mcp/sync.mjs --apply                       # .mcp.json, .codex/, .gemini/, .kiro/, .cline/, .vscode/
node .agents/mcp/sync.mjs --apply --target antigravity  # ~/.gemini/antigravity/mcp_config.json (sao lưu trước khi ghi)
```

Đổi mật khẩu DB (`mcp/.env`) hay bảng trong `mcp/dab-config.json` thì không cần chạy lại `sync.mjs`. Sau mọi thay đổi MCP, mở lại công cụ AI vì MCP chỉ nạp lúc khởi động.

## Kiểm tra

```bash
node --test '.agents/hooks/tests/*.test.mjs'                  # hook + MCP
node .agents/mcp/sync.mjs --check                             # cấu hình MCP các công cụ khớp nguồn
python3 .agents/scripts/check_doc_links.py DocBusinessThienAn # tài liệu nghiệp vụ
```
