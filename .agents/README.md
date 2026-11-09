# `.agents/` — bộ khung AI dùng chung (SSOT)

Mọi công cụ AI của dự án (Claude Code, Codex, Antigravity) dùng chung thư mục này. Sửa quy tắc, skill, agent, memory, hook hay MCP thì sửa ở đây. Thư mục riêng của từng công cụ (`.claude/`, `.codex/`) không tạo thêm tệp: chỉ giữ tệp cấu hình công cụ bắt buộc, trỏ về đây (dòng nạp, đăng ký hook) hoặc sinh từ đây (MCP). Không symlink, không chép tệp agent hay skill.

> ⛔ Đã gỡ bộ AG-Kit (01/11/2026): thư mục này do dự án tự quản. Không chạy `ag-kit update` — lệnh đó chép lại toàn bộ skill, agent và công cụ của kit đã bỏ. Kiro, Cline, Gemini CLI đã bỏ khỏi bộ khung (09/11/2026; Gemini CLI đã được Google thay bằng Antigravity CLI, Antigravity không đọc `.gemini/`).

## Cấu trúc

| Đường dẫn | Nội dung |
|---|---|
| `rules/` | Quy tắc dự án. `thienan_rules.md` là P0, `core-protocol.md` là quy tắc nền, `request-routing.md` định tuyến sang rule theo miền (BE, FE, test, DB, tài liệu, git), `gortex-tool-map.md` quy định dùng Gortex |
| `memory/` | Memory duy nhất của mọi công cụ: mục lục `MEMORY.md` (mỗi tệp một dòng ≤ 150 ký tự) và tệp chủ đề. Claude Code tắt auto memory riêng (`autoMemoryEnabled: false`) |
| `skills/` | 5 skill (`clean-code` gồm nguyên tắc Karpathy, `tdd`, `brainstorming`, `plan-writing`, `code-review`), mỗi skill một `SKILL.md` chỉ có `name`, `description`; mục lục `skills/INDEX.md` |
| `agent/` | Vai chuyên gia, mỗi vai một tệp (`name` trùng tên tệp, `skills:` trỏ tới `skills/`); mục lục `agent/INDEX.md` |
| `hooks/` | `guardrails.mjs` (chặn vi phạm P0: test sai chỗ, `git add .`, build FE, `--no-build`…), `validate-tool-call.mjs` (chặn lệnh phá ổ đĩa), `guarded-rtk.mjs` (Codex: chạy hai bộ chặn trước rồi mới cho rtk viết lại lệnh shell), `gortex-enforce.mjs` (chặn Read/Grep/Glob/Bash đọc mã đã index), `gortex-hook.mjs` (ngữ cảnh Gortex), `prompt-submit.mjs` (nhắc SSOT cho Codex); test ở `hooks/tests/` |
| `hooks.json` | Đăng ký hook cho Antigravity |
| `mcp_config.json` | Nguồn duy nhất danh sách MCP server (Gortex chạy bộ 21 tool `--tools facade-v1`) |
| `mcp/` | `sync.mjs` sinh cấu hình MCP cho từng công cụ; `dab.mjs` + `dab-config.json` chạy DAB cho DB dev/test/staging (chỉ đọc: tool ghi tắt ở `runtime.mcp.dml-tools`); `.env` chứa chuỗi kết nối (không commit, mẫu `.env.example`) |
| `scripts/` | `check_doc_links.py` kiểm cấu trúc và link tài liệu `DocBusinessThienAn`; `Fix-DoubledBlankLines.ps1` |

## Mỗi công cụ đọc `.agents/` thế nào

Danh sách tệp nạp sẵn mỗi phiên (rule luôn bật, mục lục skill, mục lục agent, memory) nằm ở khối "Nạp sẵn mỗi phiên" trong `AGENTS.md` cấp cha; tổng ≤ 20.000 ký tự (test chặn).

| Công cụ | Tệp nạp sẵn | Skill | Agent | Hook | MCP |
|---|---|---|---|---|---|
| Claude Code | `.claude/CLAUDE.md` nạp `@../AGENTS.md`, rồi nạp lồng các dòng `@` | `skills/INDEX.md` nạp sẵn | `agent/INDEX.md` nạp sẵn | `.claude/settings*.json` (rtk qua `rtk hook claude`) | `.mcp.json` (sinh); staging tắt bằng `disabledMcpjsonServers` |
| Codex | đọc `AGENTS.md`, mở tệp theo đường dẫn | tự quét `.agents/skills/` | theo `agent/INDEX.md` | `[hooks]` trong `.codex/config.toml` (`guarded-rtk.mjs codex`) — chỉ chạy khi dự án đã trust và hook đã duyệt bằng `/hooks` | `.codex/config.toml` (sinh, staging `enabled = false`) |
| Antigravity | tự quét `rules/` | tự quét `skills/` | đọc thẳng `agent/` | `hooks.json` | `--target antigravity` |

Agent dùng theo kiểu áp dụng vai: đọc `agent/<tên>.md` rồi làm theo trong cuộc trò chuyện; cần chạy riêng thì gọi subagent thường kèm câu "đọc `.agents/agent/<tên>.md` và làm theo".

## Thêm, sửa

- **Tệp nạp sẵn:** sửa khối "Nạp sẵn mỗi phiên" trong `AGENTS.md` (dòng `@./…`); giữ tổng ≤ 20.000 ký tự. Ghi chú cho người bảo trì đặt trong comment HTML `<!-- -->` (Claude Code lược khi nạp).
- **Memory:** ghi vào tệp chủ đề trong `memory/`, thêm hoặc sửa dòng mục lục ≤ 150 ký tự. Repo công khai: không ghi mật khẩu, chuỗi kết nối, địa chỉ máy chủ.
- **Skill:** thêm, đổi tên, xóa thì sửa `skills/INDEX.md` và dòng `skills:` của agent cùng lúc; `name` trong `SKILL.md` phải trùng tên thư mục.
- **Agent:** thêm, đổi tên, xóa vai thì sửa `agent/INDEX.md` cùng lúc; `name` phải trùng tên tệp. Không tạo tệp agent trong thư mục công cụ.
- **Hook:** logic chỉ nằm trong `hooks/`; cấu hình từng công cụ ở bảng trên chỉ gọi lại script. Chặn bằng exit 2, cho qua thì không in gì ra stdout.

`hooks/tests/harness.test.mjs` bắt lệch: tệp nạp không tồn tại hoặc bị thiếu, phần nạp sẵn vượt 20.000 ký tự, dòng mục lục memory quá dài, mục lục skill hay agent lệch thư mục, thư mục công cụ có tệp agent riêng, hook của công cụ nào chưa nối, hook rtk tự duyệt quyền hay chạy trước bộ chặn.

## MCP

Sửa danh sách server trong `mcp_config.json`, rồi sinh lại cấu hình của từng công cụ (không sửa tay các tệp sinh ra):

```bash
node .agents/mcp/sync.mjs --check                       # exit 1 khi có tệp lệch nguồn
node .agents/mcp/sync.mjs --apply                       # .mcp.json, .codex/, .vscode/
node .agents/mcp/sync.mjs --apply --target antigravity  # ~/.gemini/antigravity/mcp_config.json
```

- Tệp global của Antigravity chỉ ghi khi chọn `--target`: thay server chạy tệp trong repo (đổi sang đường dẫn tuyệt đối), giữ nguyên server khác, sao lưu trước khi ghi. Entry `gortex` có sẵn trong tệp đó do người dùng tự cấu hình: thêm `--tools facade-v1` bằng tay (bộ mặc định 183 tool, 274 KB định nghĩa; `facade-v1` 21 tool, 15 KB).
- `mssql_staging` tắt mặc định (chỉ dùng khi người dùng yêu cầu): Codex qua `sync.mjs`; Claude Code qua `disabledMcpjsonServers` trong `.claude/settings.local.json`. Bật lại khi cần bằng `/mcp`.
- Ghi CSDL qua MCP bị cấm: `dab-config.json` tắt `create-record`, `update-record`, `delete-record`, `execute-entity`; entity chỉ có quyền `read`.
- Đổi mật khẩu DB (`mcp/.env`) hay bảng trong `mcp/dab-config.json` thì không cần chạy lại `sync.mjs`. Sau mọi thay đổi MCP, mở lại công cụ AI vì MCP chỉ nạp lúc khởi động.

## Thói quen dùng (tiết kiệm token)

- Khảo sát, tìm kiếm thì dùng subagent Explore: subagent thường nạp lại toàn bộ phần nạp sẵn. Yêu cầu subagent trả tóm tắt ngắn.
- Không đổi model, effort hay bật tắt MCP giữa phiên (mất cache prefix).
- Đo: `/context all`, `/usage` (Claude Code).
- rtk (tùy chọn): `brew install rtk-ai/tap/rtk-oss` (bản chỉ Apache-2.0); máy chưa cài thì hook bỏ qua. Lọc output git, ls, grep, find, Playwright; xem mức tiết kiệm bằng `rtk gain`.
  - Claude Code: `rtk hook claude` chỉ viết lại lệnh, không tự duyệt quyền.
  - Codex: qua `hooks/guarded-rtk.mjs` vì rtk tự trả `allow`; script chạy `guardrails`, `validate-tool-call` trước và chỉ chuyển lệnh shell cho rtk.
  - rtk không lọc `dotnet test`: dùng cờ output gọn ở `rules/testing-rules_thienan.md` §6.

Số đo 09/11/2026: phần nạp sẵn ~44.000 → 19.686 ký tự; tool Gortex 183 (274 KB) → 21 (15 KB); skill 131 KB → 16 KB; tool DAB mỗi server 7 → 3; rtk giảm output `git log` 76%, `grep` 65%, `ls` 60%, `find` 35%, `git diff` lớn 28%.

## Kiểm tra

```bash
node --test '.agents/hooks/tests/*.test.mjs'                  # hook, MCP, nối cấu hình, ngân sách nạp sẵn
node .agents/mcp/sync.mjs --check                             # cấu hình MCP các công cụ khớp nguồn
python3 .agents/scripts/check_doc_links.py DocBusinessThienAn # tài liệu nghiệp vụ
```
