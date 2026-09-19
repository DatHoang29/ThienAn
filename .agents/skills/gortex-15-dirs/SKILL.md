---
name: gortex-15-dirs
description: "Work in the . +15 dirs area — 506 symbols across 45 files (99% cohesion)"
---

# . +15 dirs

506 symbols | 45 files | 99% cohesion

## When to Use

Use this skill when working on files in:
- ``
- `.agents/scripts/auto_preview.py`
- `.agents/scripts/check_doc_links.py`
- `.agents/scripts/checklist.py`
- `.agents/scripts/component_registry.py`
- `.agents/scripts/dependency_graph.py`
- `.agents/scripts/generate_manifest.py`
- `.agents/scripts/session_manager.py`
- `.agents/scripts/tests/test_toolkit.py`
- `.agents/scripts/validate_kit.py`
- `.agents/scripts/validation_runner.py`
- `.agents/scripts/verify_all.py`
- `.agents/skills/api-patterns/scripts/api_validator.py`
- `.agents/skills/database-design/scripts/schema_validator.py`
- `.agents/skills/frontend-design/scripts/accessibility_checker.py`
- `.agents/skills/frontend-design/scripts/ux_audit.py`
- `.agents/skills/geo-fundamentals/scripts/geo_checker.py`
- `.agents/skills/i18n-localization/scripts/i18n_checker.py`
- `.agents/skills/lint-and-validate/scripts/lint_runner.py`
- `.agents/skills/lint-and-validate/scripts/type_coverage.py`
- `.agents/skills/mobile-design/scripts/mobile_audit.py`
- `.agents/skills/nextjs-react-expert/scripts/convert_rules.py`
- `.agents/skills/nextjs-react-expert/scripts/react_performance_checker.py`
- `.agents/skills/performance-profiling/scripts/bundle_analyzer.py`
- `.agents/skills/performance-profiling/scripts/lighthouse_audit.py`
- `.agents/skills/seo-fundamentals/scripts/seo_checker.py`
- `.agents/skills/testing-patterns/scripts/test_runner.py`
- `.agents/skills/vulnerability-scanner/scripts/dependency_analyzer.py`
- `.agents/skills/vulnerability-scanner/scripts/security_scan.py`
- `.agents/skills/webapp-testing/scripts/playwright_runner.py`
- `external-call::dep:component_registry.build_lock`
- `external-call::dep:component_registry.build_manifest`
- `external-call::dep:component_registry.canonical_json`
- `external-call::dep:component_registry.is_semver`
- `external-call::dep:component_registry.normalize_list`
- `external-call::dep:component_registry.version_satisfies`
- `external-call::dep:dependency_graph.render`
- `external-call::dep:playwright.sync_api.sync_playwright`
- `external-call::dep:validation_runner.CONSOLE`
- `external-call::dep:validation_runner.execute_suite`
- `external-call::dep:validation_runner.locate_toolkit_root`
- `external-call::dep:validation_runner.print_summary`
- `external-call::dep:validation_runner.write_report`
- `external-call::stdlib:tokenize`
- `external-call::stdlib:yaml`

## Key Files

| File | Symbols |
|------|---------|
| `` | now, kill, parse, reconfigure, count, ... |
| `.agents/scripts/auto_preview.py` | get_project_root, root, is_running, status_server, stop_server, ... |
| `.agents/scripts/check_doc_links.py` | target_root, DocValidator, check_tier_tables, level, check_token_bombs, ... |
| `.agents/scripts/checklist.py` | main |
| `.agents/scripts/component_registry.py` | version_satisfies, component_files, load_frontmatter, root, raw, ... |
| `.agents/scripts/dependency_graph.py` | name, node_id, root, prefix, render, ... |
| `.agents/scripts/generate_manifest.py` | check_file, main, path, expected |
| `.agents/scripts/session_manager.py` | root, root, print_status, root, main, ... |
| `.agents/scripts/tests/test_toolkit.py` | test_dependency_graph_is_synchronized, test_bundle_analyzer_flags_oversized_asset, test_security_scanner_detects_executable_eval_and_secret, test_orchestration_guidance_is_antigravity_first_and_bounded, test_workflow_dependencies_resolve, ... |
| `.agents/scripts/validate_kit.py` | validate_json, findings, validate_python, root, skills, ... |
| `.agents/scripts/validation_runner.py` | toolkit_root, text, print_summary, url, _decode_timeout, ... |
| `.agents/scripts/verify_all.py` | main |
| `.agents/skills/api-patterns/scripts/api_validator.py` | find_api_files, main, check_openapi_spec, project_path, file_path, ... |
| `.agents/skills/database-design/scripts/schema_validator.py` | validate_prisma_schema, find_schema_files, main, project_path, file_path |
| `.agents/skills/frontend-design/scripts/accessibility_checker.py` | file_path, project_path, main, content, tag, ... |
| `.agents/skills/frontend-design/scripts/ux_audit.py` | __init__, audit_directory, audit_file, filepath, directory, ... |
| `.agents/skills/geo-fundamentals/scripts/geo_checker.py` | find_web_pages, project_path, file_path, _read_route_content, file_path, ... |
| `.agents/skills/i18n-localization/scripts/i18n_checker.py` | check_hardcoded_strings, locale_files, is_skipped, root, root, ... |
| `.agents/skills/lint-and-validate/scripts/lint_runner.py` | linter, cwd, main, project_path, run_linter, ... |
| `.agents/skills/lint-and-validate/scripts/type_coverage.py` | project_path, tsconfig, suffixes, check_typescript_coverage, main, ... |
| `.agents/skills/mobile-design/scripts/mobile_audit.py` | audit_directory, get_report, __init__, MobileAuditor, main, ... |
| `.agents/skills/nextjs-react-expert/scripts/convert_rules.py` | parse_frontmatter, filepath, parse_rule_file, output_dir, rules_dir, ... |
| `.agents/skills/nextjs-react-expert/scripts/react_performance_checker.py` | project_path, check_dynamic_imports, extensions, check_image_optimization, check_waterfalls, ... |
| `.agents/skills/performance-profiling/scripts/bundle_analyzer.py` | threshold, total_fail_kib, root, human_bytes, value, ... |
| `.agents/skills/performance-profiling/scripts/lighthouse_audit.py` | timeout, url, run_lighthouse, main |
| `.agents/skills/seo-fundamentals/scripts/seo_checker.py` | check_page, file_path, is_page_file, find_pages, project_path, ... |
| `.agents/skills/testing-patterns/scripts/test_runner.py` | detect_test_framework, main, cmd, project_path, cwd, ... |
| `.agents/skills/vulnerability-scanner/scripts/dependency_analyzer.py` | analyze_package_json, analyze_requirements, findings, root, findings, ... |
| `.agents/skills/vulnerability-scanner/scripts/security_scan.py` | _redact, project_path, _looks_like_web_project, project, path, ... |
| `.agents/skills/webapp-testing/scripts/playwright_runner.py` | main, run_test, screenshot, url, timeout_ms, ... |
| `external-call::dep:component_registry.build_lock` | component_registry.build_lock |
| `external-call::dep:component_registry.build_manifest` | component_registry.build_manifest |
| `external-call::dep:component_registry.canonical_json` | component_registry.canonical_json |
| `external-call::dep:component_registry.is_semver` | component_registry.is_semver |
| `external-call::dep:component_registry.normalize_list` | component_registry.normalize_list |
| `external-call::dep:component_registry.version_satisfies` | component_registry.version_satisfies |
| `external-call::dep:dependency_graph.render` | dependency_graph.render |
| `external-call::dep:playwright.sync_api.sync_playwright` | playwright.sync_api.sync_playwright |
| `external-call::dep:validation_runner.CONSOLE` | validation_runner.CONSOLE |
| `external-call::dep:validation_runner.execute_suite` | validation_runner.execute_suite |
| `external-call::dep:validation_runner.locate_toolkit_root` | validation_runner.locate_toolkit_root |
| `external-call::dep:validation_runner.print_summary` | validation_runner.print_summary |
| `external-call::dep:validation_runner.write_report` | validation_runner.write_report |
| `external-call::stdlib:tokenize` | tokenize |
| `external-call::stdlib:yaml` | yaml |

## Entry Points

- `.agents/skills/mobile-design/scripts/mobile_audit.py::MobileAuditor.audit_file`
- `.agents/skills/frontend-design/scripts/ux_audit.py::UXAuditor.audit_file`

## How to Explore

```
analyze(operation:"communities", id:"community-2386")
explore(operation:"context", task:"understand . +15 dirs", format:"gcx")
relations(operation:"usages", target:{symbol:".agents/skills/mobile-design/scripts/mobile_audit.py::MobileAuditor.audit_file"}, format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
