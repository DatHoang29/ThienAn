---
trigger: model_decision
description: Apply when writing, building, refactoring, or fixing code — project-type agent routing, the Socratic Gate, Plan Mode phases, and the final checklist/scripts. Skip for pure questions or text-only responses.
---

# Code Rules (TIER 1) - AG Kit

> Loaded when the request involves writing or modifying code.

---

## 📱 Project Type Routing

| Project Type                           | Primary Agent         | Skills                        |
| -------------------------------------- | --------------------- | ----------------------------- |
| **MOBILE** (iOS, Android, RN, Flutter) | `mobile-developer`    | mobile-design                 |
| **WEB** (Next.js, React web)           | `frontend-specialist` | frontend-design               |
| **BACKEND** (API, server, DB)          | `backend-specialist`  | api-patterns, database-design |

> 🔴 **Mobile + frontend-specialist = WRONG.** Mobile = mobile-developer ONLY.

---

## 🛑 GLOBAL SOCRATIC GATE

**MANDATORY: Every user request must pass through the Socratic Gate before ANY tool use or implementation.**

| Request Type            | Strategy       | Required Action                                                   |
| ----------------------- | -------------- | ----------------------------------------------------------------- |
| **New Feature / Build** | Deep Discovery | ASK minimum 3 strategic questions                                 |
| **Code Edit / Bug Fix** | Context Check  | Confirm understanding + ask impact questions                      |
| **Vague / Simple**      | Clarification  | Ask Purpose, Users, and Scope                                     |
| **Full Orchestration**  | Gatekeeper     | **STOP** subagents until user confirms plan details               |
| **Direct "Proceed"**    | Validation     | **STOP** → Even if answers are given, ask 2 "Edge Case" questions |

**Protocol:**

1. **Never Assume:** If even 1% is unclear, ASK.
2. **Handle Spec-heavy Requests:** When user gives a list (Answers 1, 2, 3...), do NOT skip the gate. Instead, ask about **Trade-offs** or **Edge Cases** (e.g., "LocalStorage confirmed, but should we handle data clearing or versioning?") before starting.
3. **Wait:** Do NOT invoke subagents or write code until the user clears the Gate.
4. **Reference:** Full protocol in `@[skills/brainstorming]`.

---

## 🏁 Plan Mode (4-Phase)

1. ANALYSIS → Research, questions
2. PLANNING → `{task-slug}.md`, task breakdown
3. SOLUTIONING → Architecture, design (NO CODE!)
4. IMPLEMENTATION → Code + tests

---

## 🏁 Final Checklist Protocol

**Trigger:** When the user says "run the final checks", "final checks", "run all the tests", or similar phrases.

| Task Stage       | Command                                            | Purpose                        |
| ---------------- | -------------------------------------------------- | ------------------------------ |
| **Manual Audit** | `python .agents/scripts/checklist.py .`             | Priority-based project audit   |
| **Pre-Deploy**   | `python .agents/scripts/checklist.py . --url <URL>` | Full Suite + Performance + E2E |

**Priority Execution Order:**

1. **Security** → 2. **Lint** → 3. **Schema** → 4. **Tests** → 5. **UX** → 6. **Seo** → 7. **Lighthouse/E2E**

**Rules:**

- **Completion:** A task is NOT finished until `checklist.py` returns success.
- **Reporting:** If it fails, fix the **Critical** blockers first (Security/Lint).

**Available Scripts (10 total):**

| Script                     | Skill                 | When to Use         |
| -------------------------- | --------------------- | ------------------- |
| `security_scan.py`         | vulnerability-scanner | Always on deploy    |
| `lint_runner.py`           | lint-and-validate     | Every code change   |
| `test_runner.py`           | testing-patterns      | After logic change  |
| `schema_validator.py`      | database-design       | After DB change     |
| `ux_audit.py`              | frontend-design       | After UI change     |
| `accessibility_checker.py` | frontend-design       | After UI change     |
| `seo_checker.py`           | seo-fundamentals      | After page change   |
| `mobile_audit.py`          | mobile-design         | After mobile change |
| `lighthouse_audit.py`      | performance-profiling | Before deploy       |
| `playwright_runner.py`     | webapp-testing        | Before deploy       |

> 🔴 **Agents & Skills can invoke ANY script** via `python .agents/skills/<skill>/scripts/<script>.py`

---

## 🛠️ .NET & Solution Troubleshooting Protocol

**Khi gặp lỗi thiếu tham chiếu, không nhận diện được Test trong IDE, hoặc lỗi khi debug:**

1. **Kiểm tra đăng ký trong Solution (`.sln`):**
   - Mọi dự án mới tạo (đặc biệt là `*.Tests.csproj`) BẮT BUỘC phải được thêm vào file `.sln` chính của workspace.
   - Nếu IDE/VS Code không quét được test hoặc báo thiếu reference, hãy kiểm tra và chạy:
     `dotnet sln <path-to-sln> add <path-to-csproj>`
   - Lệnh tự động thêm tất cả project: `dotnet sln <sln-file> add $(find . -name "*.csproj")`

2. **Quy tắc thực thi lệnh .NET:**
   - KHÔNG KHUYÊN DÙNG chạy trực tiếp file đơn lẻ dạng `dotnet File.cs` cho project xUnit/C#.
   - LUÔN LUÔN dùng `dotnet test <csproj_or_sln>` hoặc `dotnet build` để nạp đủ các thư viện và dependency.

3. **Vòng đời Test (xUnit Lifecycle):**
   - Khi có khởi tạo DB / Host nặng, BẮT BUỘC dùng `IClassFixture<T>` để tránh gọi constructor N lần gây đụng độ `DROP TABLE` khi chạy test song song.

4. **Kiểm tra Connection String trước khi `dotnet test` (BẮT BUỘC):**
   - Trước khi thực thi `dotnet test`, BẮT BUỘC kiểm tra cấu hình: Tất cả Connection Strings của RDBMS (SQL Server, PostgreSQL, v.v.) và NoSQL/Cache (Redis) phải trỏ về **LOCAL** (`localhost`, `127.0.0.1`, `(localdb)`, `.`).
   - Nếu phát hiện bất kỳ chuỗi kết nối nào trỏ tới server từ xa (như `10.10.8.30` hoặc remote IP/host), **HỦY NGAY LẬP TỨC VÀ BÁO CÁO LẠI CHO NGƯỜI DÙNG**.


