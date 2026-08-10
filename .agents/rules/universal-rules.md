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
- **No Duplicate XML Comments**: NEVER generate stacked or duplicate `/// <summary>` XML comment blocks on any class, method, or property. Each symbol MUST have at most ONE concise `<summary>` block. Always update existing docstrings in-place.
- **Async Method Naming**: Do NOT append the `Async` suffix to asynchronous method names (e.g. use `ProcessBatchSubscriptions` instead of `ProcessBatchSubscriptionsAsync`), as the method return type (`Task` / `Task<T>`) already explicitly indicates asynchrony.
- **C# / .NET CA2263**: ALWAYS prefer generic `Enum.IsDefined<TEnum>(value)` (or `Enum.IsDefined(enumValue)` in .NET 7+) over the non-generic `Enum.IsDefined(typeof(TEnum), value)` to prevent unnecessary object boxing and reflection overhead.

---

## 🔒 SQL & Module Isolation Scope (Mandatory Rule)

- **Strict Module Scope**: All SQL scripts (DDL & DML) generated or updated for a module MUST ONLY target tables within that module's official entity scope (e.g., for `ShareData` module: `EshPartner`, `EshDataSource`, `EshMappingProfile`, `EshFieldMapping`, `EshSubscription`, `EshExportLog`, `EshSystemLog`, `EshEventSource`).
- **FORBIDDEN Outside Operations**: NEVER perform `CREATE`, `ALTER`, `DROP`, `INSERT`, `UPDATE`, or `DELETE` operations on tables owned by other modules (such as `TmsTrafficData`, `TmsWeather`, `TmsIncident`, `TollTransactionOut`...). External tables belong strictly to their host modules and must never be created, altered, or mutated by another module's scripts.

---

## 🛑 Strict Manual SQL Execution Rule (Mandatory Rule)

- **ABSOLUTELY NO Auto-Executing Database Mutations**: When creating or updating SQL scripts (`.SQL`) or database configurations, ONLY write or modify files on disk. 
- **FORBIDDEN Auto-Mutations**: NEVER automatically run or execute any DDL/DML operations (`INSERT`, `UPDATE`, `DELETE`, `ALTER`, `DROP`, `TRUNCATE`) against any database (remote or local, via scripts, C# code, or tools) without explicit prior request from the user.
- **User Review First**: Always present the generated SQL script file to the user for inspection so they can manually review and execute it themselves.

---

## 🔒 MCP Database Read-Only Rule (Mandatory Rule)

- **Strict MCP Read-Only Mode**: ALL database interactions executed via MCP servers (`mssql_dev`, `mssql_demo`, `mssql_test`, etc.) MUST be strictly READ-ONLY. Allowed tools & queries are limited to inspecting schemas and reading data (`SELECT` queries, `list_tables`, `describe_table`, `sample_data`, `get_relationships`, etc.).
- **ABSOLUTELY FORBIDDEN MCP Operations**: NEVER execute any `INSERT`, `UPDATE`, `DELETE`, `DROP`, `ALTER`, `TRUNCATE`, `CREATE`, or stored procedure execution mutating state via any MCP database tool under any circumstances.

