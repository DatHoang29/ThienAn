# `.agents/` — bộ khung AI dùng chung (SSOT)

Mọi công cụ AI (Claude Code, Codex, Gemini CLI, Kiro, Cline, Antigravity) dùng chung thư mục này. Sửa quy tắc, skill, agent, hook hay MCP thì sửa ở đây. Thư mục riêng của từng công cụ (`.claude/`, `.codex/`, `.gemini/`, `.kiro/`…) không tạo thêm tệp: chỉ giữ tệp cấu hình công cụ bắt buộc, trỏ về đây (dòng nạp, đăng ký hook) hoặc sinh từ đây (MCP). Không symlink, không chép tệp agent hay skill.

> ⛔ Đã gỡ bộ AG-Kit (01/11/2026): thư mục này do dự án tự quản. Không chạy `ag-kit update` — lệnh đó chép lại toàn bộ skill, agent và công cụ của kit đã bỏ.

## Cấu trúc

| Đường dẫn | Nội dung |
|---|---|
| `rules/` | Quy tắc dự án. `thienan_rules.md` là P0, `core-protocol.md` là quy tắc nền, `request-routing.md` định tuyến sang rule theo miền (BE, FE, test, DB, tài liệu, git), `gortex-tool-map.md` quy định dùng Gortex |
| `memory/` | Quy ước và quyết định dùng chung cả nhóm; mục lục `MEMORY.md` |
| `skills/` | Skill nạp theo nhu cầu, mỗi skill một `SKILL.md` (`name` trùng tên thư mục); mục lục `skills/INDEX.md` |
| `agent/` | Vai chuyên gia, mỗi vai một tệp (`name` trùng tên tệp, `skills:` trỏ tới `skills/`); mục lục `agent/INDEX.md` |
| `hooks/` | `guardrails.mjs` (chặn vi phạm P0: test sai chỗ, `git add .`, build FE, `--no-build`…), `gortex-enforce.mjs` + `gortex-hook.mjs` (buộc dùng Gortex), `prompt-submit.mjs` (nhắc SSOT mỗi prompt), `validate-tool-call.mjs` (chặn lệnh phá ổ đĩa); test ở `hooks/tests/` |
| `hooks.json` | Đăng ký hook cho Antigravity |
| `mcp_config.json` | Nguồn duy nhất danh sách MCP server |
| `mcp/` | `sync.mjs` sinh cấu hình MCP cho từng công cụ; `dab.mjs` + `dab-config.json` chạy DAB cho DB dev/test/staging; `.env` chứa chuỗi kết nối (không commit, mẫu `.env.example`) |
| `scripts/` | `check_doc_links.py` kiểm cấu trúc và link tài liệu `DocBusinessThienAn`; `Fix-DoubledBlankLines.ps1` |

## Mỗi công cụ đọc `.agents/` thế nào

Danh sách tệp nạp sẵn mỗi phiên (rule luôn bật, mục lục skill, mục lục agent, memory) nằm ở khối "Nạp sẵn mỗi phiên" trong `AGENTS.md` cấp cha.

| Công cụ | Tệp nạp sẵn | Skill | Agent | Hook | MCP |
|---|---|---|---|---|---|
| Claude Code | `.claude/CLAUDE.md` nạp `@../AGENTS.md`, rồi nạp lồng các dòng `@` | `skills/INDEX.md` nạp sẵn | `agent/INDEX.md` nạp sẵn | `.claude/settings*.json` | `.mcp.json` (sinh) |
| Codex | đọc `AGENTS.md`, mở tệp theo đường dẫn | tự quét `.agents/skills/` | theo `agent/INDEX.md` | `[hooks]` trong `.codex/config.toml` — chỉ chạy khi dự án đã trust và hook đã duyệt bằng `/hooks` | `.codex/config.toml` (sinh) |
| Gemini CLI | `context.fileName` = `AGENTS.md` trong `.gemini/settings.json`, nạp lồng các dòng `@` | tự quét `.agents/skills/` | `agent/INDEX.md` nạp sẵn | `hooks` trong `.gemini/settings.json` | `.gemini/settings.json` (sinh) |
| Kiro | tự nạp `AGENTS.md`; `#[[file:…]]` trong `.kiro/steering/main.md` | `skills/INDEX.md` qua steering | `agent/INDEX.md` qua steering | chưa nối: Kiro chỉ nhận hook từ tệp riêng trong `.kiro/hooks/` | `.kiro/settings/mcp.json` (sinh) |
| Cline | đọc `AGENTS.md`, mở tệp theo đường dẫn | tự quét `.agents/skills/` | theo `agent/INDEX.md` | extension VS Code chưa hỗ trợ (plugin chỉ có ở Cline CLI/SDK) | tệp global: `--target cline`, `--target cline-cli` |
| Antigravity | tự quét `rules/` | tự quét `skills/` | đọc thẳng `agent/` | `hooks.json` | `--target antigravity` |

Agent dùng theo kiểu áp dụng vai: đọc `agent/<tên>.md` rồi làm theo trong cuộc trò chuyện; cần chạy riêng thì gọi subagent thường kèm câu "đọc `.agents/agent/<tên>.md` và làm theo".

## Thêm, sửa

- **Tệp nạp sẵn:** sửa khối "Nạp sẵn mỗi phiên" trong `AGENTS.md` (dòng `@./…`) và `.kiro/steering/main.md` (`#[[file:…]]`) cùng lúc.
- **Skill:** thêm, đổi tên, xóa thì sửa `skills/INDEX.md` cùng lúc; `name` trong `SKILL.md` phải trùng tên thư mục.
- **Agent:** thêm, đổi tên, xóa vai thì sửa `agent/INDEX.md` cùng lúc; `name` phải trùng tên tệp. Không tạo tệp agent trong thư mục công cụ.
- **Hook:** logic chỉ nằm trong `hooks/`; cấu hình từng công cụ ở bảng trên chỉ gọi lại script. Chặn bằng exit 2, cho qua thì không in gì ra stdout (Gemini CLI coi stdout không phải JSON là thông báo hệ thống).

`hooks/tests/harness.test.mjs` bắt lệch: tệp nạp không tồn tại hoặc bị thiếu, mục lục skill hay agent lệch thư mục, thư mục công cụ có tệp agent riêng, hook của công cụ nào chưa nối.

## MCP

Sửa danh sách server trong `mcp_config.json`, rồi sinh lại cấu hình của từng công cụ (không sửa tay các tệp sinh ra):

```bash
node .agents/mcp/sync.mjs --check                       # exit 1 khi có tệp lệch nguồn
node .agents/mcp/sync.mjs --apply                       # .mcp.json, .codex/, .gemini/, .kiro/, .vscode/
node .agents/mcp/sync.mjs --apply --target antigravity  # ~/.gemini/antigravity/mcp_config.json
node .agents/mcp/sync.mjs --apply --target cline        # tệp MCP global của Cline extension VS Code
node .agents/mcp/sync.mjs --apply --target cline-cli    # ~/.cline/data/settings/cline_mcp_settings.json
```

Tệp global (`antigravity`, `cline`, `cline-cli`) chỉ ghi khi chọn `--target`: thay server chạy tệp trong repo (đổi sang đường dẫn tuyệt đối), giữ nguyên server khác, sao lưu trước khi ghi. Đổi mật khẩu DB (`mcp/.env`) hay bảng trong `mcp/dab-config.json` thì không cần chạy lại `sync.mjs`. Sau mọi thay đổi MCP, mở lại công cụ AI vì MCP chỉ nạp lúc khởi động.

## Kiểm tra

```bash
node --test '.agents/hooks/tests/*.test.mjs'                  # hook, MCP, nối cấu hình các công cụ
node .agents/mcp/sync.mjs --check                             # cấu hình MCP các công cụ khớp nguồn
python3 .agents/scripts/check_doc_links.py DocBusinessThienAn # tài liệu nghiệp vụ
```
