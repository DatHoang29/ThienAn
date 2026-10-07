---
name: implement
description: "Implement a piece of work based on a spec or set of tickets."
disable-model-invocation: true
when_to_use: "User-invoked only: implement a piece of work from a spec or set of tickets, using TDD, then review and commit."
allowed-tools: Skill, Bash
version: 1.0.0
---

Implement the work described by the user in the spec or tickets.

Call the Skill tool with "tdd" where possible, at pre-agreed seams.

Run typechecking regularly, single test files regularly, and the full test suite once at the end.

Once done, call the Skill tool with "code-review" to review the work.

Commit your work to the current branch.
