---
name: request-routing
version: 1.0.0
priority: P0
trigger: always_on
---

# Request Routing - AG Kit

> Always-active. Classify every request, then auto-route to the best specialist agent(s) before responding.

---

## 📥 REQUEST CLASSIFIER (STEP 1)

**Before ANY action, classify the request:**

| Request Type     | Trigger Keywords                           | Active Tiers                   | Result                      |
| ---------------- | ------------------------------------------ | ------------------------------ | --------------------------- |
| **QUESTION**     | "what is", "how does", "explain"           | TIER 0 only                    | Text Response               |
| **SURVEY/INTEL** | "analyze", "list files", "overview"        | TIER 0 + Explorer              | Session Intel (No File)     |
| **SIMPLE CODE**  | "fix", "add", "change" (single file)       | TIER 0 + TIER 1 (lite)         | Inline Edit                 |
| **COMPLEX CODE** | "build", "create", "implement", "refactor" | TIER 0 + TIER 1 (full) + Agent | **{task-slug}.md Required** |
| **NEW APP**      | "new app", "from scratch", "build me a/an", multi-page | `project-planner` (loads `app-builder`) → `orchestrator` | **{task-slug}.md + app-builder** |
| **DESIGN/UI**    | "design", "UI", "page", "dashboard"        | TIER 0 + TIER 1 + Agent        | **{task-slug}.md Required** |
| **SLASH CMD**    | /create, /orchestrate, /debug              | Command-specific flow          | Variable                    |

> 🔴 **NEW APP / scaffold from scratch:** route through `project-planner` or `orchestrator` (both load `app-builder`), NOT a lone specialist like `frontend-specialist`. A specialist alone has no project-detection, tech-stack selection, or template knowledge — `app-builder` does. Or run `/create`.

---

## 🤖 INTELLIGENT AGENT ROUTING (STEP 2 - AUTO)

**ALWAYS ACTIVE: Before responding to ANY request, automatically analyze and select the best agent(s).**

> 🔴 **MANDATORY:** You MUST follow the protocol defined in `@[skills/intelligent-routing]`.

### Auto-Selection Protocol

1. **Analyze (Silent)**: Detect domains (Frontend, Backend, Security, etc.) from user request.
2. **Select Agent(s)**: Choose the most appropriate specialist(s).
3. **Inform User**: Concisely state which expertise is being applied.
4. **Apply**: Generate response using the selected agent's persona and rules.

### Response Format (MANDATORY)

When auto-applying an agent, inform the user:

```markdown
🤖 **Applying knowledge of `@[agent-name]`...**

[Continue with specialized response]
```

**Rules:**

1. **Silent Analysis**: No verbose meta-commentary ("I am analyzing...").
2. **Respect Overrides**: If user mentions `@agent`, use it.
3. **Complex Tasks**: For multi-domain requests, use `orchestrator` and ask Socratic questions first.

### ⚡ AUTO SUBAGENT / MULTI-AGENT DELEGATION (Rule 19.70)

> 🔴 **MANDATORY:** Khi prompt chứa danh sách từ 2 task trở lên (đặc biệt các dạng đánh số `1.`, `2.`, `3.`... hoặc gạch đầu dòng `- `, danh sách `@prompt-*.md`), hoặc khi prompt chứa các phần việc được ngăn cách bởi dấu phân cách (như `=========`, `---`, `===`...): BẮT BUỘC tự động kích hoạt subagents song song để xử lý độc lập từng phần việc theo [thienan_rules.md](./thienan_rules.md) (Rule #24).

### ⚠️ AGENT ROUTING CHECKLIST (MANDATORY BEFORE EVERY CODE/DESIGN RESPONSE)

**Before ANY code or design work, you MUST complete this mental checklist:**

| Step | Check | If Unchecked |
|------|-------|--------------|
| 1 | Did I identify the correct agent for this domain? | → STOP. Analyze request domain first. |
| 2 | Did I READ the agent's `.md` file (or recall its rules)? | → STOP. Open `.agents/agent/{agent}.md` |
| 3 | Did I announce `🤖 Applying knowledge of @[agent]...`? | → STOP. Add announcement before response. |
| 4 | Did I load required skills from agent's frontmatter? | → STOP. Check `skills:` field and read them. |

**Failure Conditions:** Vi phạm checklist (không chọn agent, không thông báo, bỏ qua quy chuẩn) = vi phạm protocol bắt buộc.

> 🔴 **Self-Check Trigger:** Every time you are about to write code or create UI, ask yourself:
> "Have I completed the Agent Routing Checklist?" If NO → Complete it first.

---

