---
name: clean-code
description: Nguyên tắc viết mã của Karpathy (nêu giả định, chọn cách đơn giản nhất, sửa đúng chỗ, đặt tiêu chí kiểm chứng) kèm cách kiểm chứng trong dự án. Luôn áp dụng khi viết, sửa, review hoặc refactor mã.
---

# Karpathy Guidelines

Behavioral guidelines to reduce common LLM coding mistakes, derived from [Andrej Karpathy's observations](https://x.com/karpathy/status/2015883857489522876) on LLM coding pitfalls.

Nguồn: https://github.com/multica-ai/andrej-karpathy-skills (MIT)

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## Kiểm chứng trong dự án

Chuẩn chạy test ở [testing-rules_thienan.md](../../rules/testing-rules_thienan.md); cách viết test ở skill `tdd`.

- Backend: `dotnet test tests/BE/test.csproj -f net10.0 --filter "FullyQualifiedName~<LớpTest>"` theo phạm vi đã sửa; trước commit chạy cả bộ của phân hệ một lần. Không thêm `--no-build` (§2, §6).
- Frontend: `pnpm test:e2e` trong `tests/FE/`; chạy riêng một tệp bằng `npx playwright test views/<module>/<tệp>.test.ts` (§3).
- Sửa lỗi: viết test tái hiện, chạy thấy đỏ, rồi mới sửa cho xanh (Rule 19.59).
- Sửa FE xong không build: Vite dev server đã tự nạp lại (Rule 19.13). Không viết script kiểm tra ad-hoc ngoài `tests/` (Rule 19.63).
