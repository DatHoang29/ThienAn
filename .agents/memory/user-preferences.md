---
type: user_preferences
created: 2026-07-21
updated: 2026-07-22
---

# User Preferences

## Workflow & Execution Rules
- **DO NOT auto-run `dotnet build`**: The AI must NOT automatically execute `dotnet build` or compilation commands after code changes unless explicitly requested by the user.
- **DO NOT auto-commit or push Git**: The AI must NOT automatically execute `git add`, `git commit`, or `git push` after completing code changes under any circumstances unless explicitly requested by the user.
- **DO NOT auto-run dev server / app**: The AI must NOT automatically run `dotnet run`, dev servers, or start the web app in background after building code unless explicitly requested by the user.
- **PRESERVE USER MANUAL EDITS (Tôn trọng code sửa tay)**: Khi người dùng đã chủ động sửa tay hoặc yêu cầu bỏ bớt các điều kiện/hằng số rác (VD: bỏ `"APPROVED"`, `"OUT"`, `"BATCH"`), AI KHÔNG ĐƯỢC TỰ Ý THÊM LẠI các điều kiện/hardcoded string đó trong các lần sửa code tiếp theo.
- **SINGLE-STATEMENT IF FORMATTING**: Đối với câu lệnh `if` 1 dòng không ngoặc `{}` (Single-Statement If), BẮT BUỘC ngắt dòng và thụt lề cho câu lệnh thực thi (VD: `if (condition)\n    statement;`), tuyệt đối không viết trên cùng 1 dòng.
