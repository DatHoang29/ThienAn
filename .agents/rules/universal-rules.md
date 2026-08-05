---
trigger: always_on
---

# Universal Rules (TIER 0) - AG Kit

> Always-active rules that apply to every request, regardless of domain.

---

## 🌐 Language Handling

When user's prompt is NOT in English:

1. **Internally translate** for better comprehension
2. **Respond in user's language** - match their communication
3. **Code comments/variables** remain in English

---

## 🧹 Clean Code (Global Mandatory)

**ALL code MUST follow `@[skills/clean-code]` rules. No exceptions.**

- **Code**: Concise, direct, no over-engineering. Self-documenting.
- **Testing**: Mandatory. Pyramid (Unit > Int > E2E) + AAA Pattern.
- **Performance**: Measure first. Adhere to current Core Web Vitals standards.
- **Formatting**: Single-statement `if` MUST ALWAYS break line and indent (e.g. `if (condition)\n    return;`). NEVER write inline on the same line (`if (condition) return;`).

---

## 🔒 SQL & Module Isolation Scope (Mandatory Rule)

- **Strict Module Scope**: All SQL scripts (DDL & DML) generated or updated for a module MUST ONLY target tables within that module's official entity scope (e.g., for `ShareData` module: `EshPartner`, `EshDataSource`, `EshMappingProfile`, `EshFieldMapping`, `EshSubscription`, `EshExportLog`, `EshSystemLog`, `EshEventSource`).
- **FORBIDDEN Outside Operations**: NEVER perform `CREATE`, `ALTER`, `DROP`, `INSERT`, `UPDATE`, or `DELETE` operations on tables owned by other modules (such as `TmsTrafficData`, `TmsWeather`, `TmsIncident`, `TollTransactionOut`...). External tables belong strictly to their host modules and must never be created, altered, or mutated by another module's scripts.

---

## 🛑 Manual SQL Execution Rule (Mandatory Rule)

- **NO Auto-Executing Database Mutations**: When creating or updating SQL scripts (`.SQL`), ONLY write or modify the file on disk. NEVER automatically run or execute DDL/DML script files (`INSERT`, `UPDATE`, `DELETE`, `ALTER`, `DROP`) against databases (remote or local) without explicit user instruction.
- **User Review First**: Always present the script file to the user for review so they can manually inspect and execute it themselves.

