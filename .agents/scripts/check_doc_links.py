#!/usr/bin/env python3
"""
check_doc_links.py - Document Structure & Tier Standard Validator for DocBusinessThienAn.

Exit codes (according to AG Kit contract):
  0: All checks passed (or not applicable)
  1: Findings met the failure threshold
  2: Invalid arguments or missing input path
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
from pathlib import Path
from typing import Any

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")

TOKEN_BOMB_THRESHOLD_BYTES = 150 * 1024  # 150 KB


def slugify(text: str) -> str:
    """Convert heading text to GitHub Markdown anchor slug."""
    text = text.lower().strip()
    # Remove markdown link formatting inside heading: [text](#link) -> text
    text = re.sub(r'\[([^\]]+)\]\([^)]+\)', r'\1', text)
    # Remove code backticks
    text = text.replace('`', '')
    # Remove punctuation except hyphen, underscore, whitespace, unicode letters/digits
    text = re.sub(r'[^\w\s-]', '', text, flags=re.UNICODE)
    # Convert spaces and multiple hyphens to single hyphen
    text = re.sub(r'[-\s]+', '-', text).strip('-')
    return text


def parse_size_str(size_str: str) -> int | None:
    """Parse size strings like '27 KB', '1.670 KB', '1,670 KB', '5.2 MB' to bytes."""
    size_str = size_str.strip()
    m = re.match(r'^([\d.,]+)\s*(B|KB|MB|GB)?$', size_str, re.IGNORECASE)
    if not m:
        return None
    val_str, unit = m.group(1), (m.group(2) or 'B').upper()
    # Handle thousand separators vs decimal separators
    if ',' in val_str and '.' in val_str:
        val_str = val_str.replace(',', '')
    elif '.' in val_str:
        parts = val_str.split('.')
        if len(parts) == 2 and len(parts[1]) == 3 and int(parts[0]) > 0:
            # e.g. 1.670 KB means 1670 KB
            val_str = parts[0] + parts[1]
    elif ',' in val_str:
        parts = val_str.split(',')
        if len(parts) == 2 and len(parts[1]) == 3 and int(parts[0]) > 0:
            val_str = parts[0] + parts[1]
        else:
            val_str = val_str.replace(',', '.')

    try:
        val = float(val_str)
    except ValueError:
        return None

    multipliers = {'B': 1, 'KB': 1024, 'MB': 1024 * 1024, 'GB': 1024 * 1024 * 1024}
    return int(val * multipliers.get(unit, 1))


class DocValidator:
    def __init__(self, target_root: Path):
        self.root = target_root.resolve()
        self.findings: list[dict[str, Any]] = []

    def add_finding(self, category: str, file_path: str | Path, message: str, level: str = "error") -> None:
        rel_path = str(Path(file_path).relative_to(self.root)) if Path(file_path).is_absolute() else str(file_path)
        self.findings.append({
            "category": category,
            "file": rel_path,
            "message": message,
            "level": level,
        })

    def run_all(self) -> list[dict[str, Any]]:
        self.check_markdown_links()
        self.check_tier_tables()
        self.check_token_bombs()
        self.check_source_provenance()
        return self.findings

    def check_markdown_links(self) -> None:
        """Check 1: Validate all relative markdown links and anchors."""
        md_files = list(self.root.rglob("*.md"))
        heading_cache: dict[Path, set[str]] = {}

        def get_headings(file_path: Path) -> set[str]:
            if file_path in heading_cache:
                return heading_cache[file_path]
            slugs = set()
            try:
                with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
                    for line in f:
                        m = re.match(r'^(#{1,6})\s+(.*)', line)
                        if m:
                            slugs.add(slugify(m.group(2)))
                        # Also check HTML anchors <a name="xyz" or id="xyz">
                        for am in re.finditer(r'<a\s+[^>]*(?:name|id)=["\']([^"\']+)["\']', line, re.IGNORECASE):
                            slugs.add(slugify(am.group(1)))
            except Exception:
                pass
            heading_cache[file_path] = slugs
            return slugs

        link_pattern = re.compile(r'!?\[([^\]]*)\]\(([^)]+)\)')

        for md_file in md_files:
            try:
                with open(md_file, "r", encoding="utf-8", errors="ignore") as f:
                    content = f.read()
            except Exception as e:
                self.add_finding("Read Error", md_file, f"Cannot read file: {e}")
                continue

            # Remove code blocks so code examples aren't treated as broken links
            cleaned = re.sub(r'```.*?```', '', content, flags=re.DOTALL)
            cleaned = re.sub(r'`[^`\n]+`', '', cleaned)

            for match in link_pattern.finditer(cleaned):
                raw_dest = match.group(2).strip()
                if not raw_dest:
                    continue

                # Strip title if present: (url "title")
                raw_dest = raw_dest.split()[0]

                # Skip external protocols, mailto, template placeholders
                if re.match(r'^(https?|ftp|mailto|file):', raw_dest, re.IGNORECASE):
                    continue
                if "{{" in raw_dest or "<" in raw_dest:
                    continue

                dest_part, _, anchor_part = raw_dest.partition('#')

                # Case 1: Anchor only in current file
                if not dest_part:
                    if anchor_part:
                        current_headings = get_headings(md_file)
                        if slugify(anchor_part) not in current_headings:
                            self.add_finding(
                                "Broken Anchor",
                                md_file,
                                f"Anchor '#{anchor_part}' not found in current file."
                            )
                    continue

                # Resolve destination file
                dest_path = (md_file.parent / dest_part).resolve()
                if not dest_path.exists():
                    self.add_finding(
                        "Broken Link",
                        md_file,
                        f"Target path does not exist: '{raw_dest}' -> {dest_path}"
                    )
                    continue

                # If anchor is present and destination is a markdown file
                if anchor_part and dest_path.suffix.lower() == ".md":
                    target_headings = get_headings(dest_path)
                    if slugify(anchor_part) not in target_headings:
                        self.add_finding(
                            "Broken Anchor",
                            md_file,
                            f"Anchor '#{anchor_part}' not found in target '{dest_part}'."
                        )

    def check_tier_tables(self) -> None:
        """Check 2, 3, 4: Tier Table completeness, size drift, and Tier-to-Read mapping."""
        # Locate module README files with Tier Table
        for readme_path in self.root.rglob("README.md"):
            try:
                with open(readme_path, "r", encoding="utf-8", errors="ignore") as f:
                    content = f.read()
            except Exception:
                continue

            if "## Tier Table" not in content:
                continue

            module_dir = readme_path.parent

            # Parse Tier Table rows
            table_match = re.search(r'## Tier Table\s*\n\s*\|([^\n]+)\|\s*\n\s*\|([^\n]+)\|\s*\n((?:\|[^\n]+\|\s*\n?)+)', content)
            if not table_match:
                self.add_finding("Tier Table Syntax", readme_path, "Tier Table header found but could not parse table structure.")
                continue

            rows = table_match.group(3).strip().split('\n')
            table_files: set[str] = set()

            for row in rows:
                cols = [c.strip() for c in row.split('|')[1:-1]]
                if len(cols) < 6:
                    continue
                tier, file_col, read_col, size_col, desc, source_col = cols[:6]
                file_rel = file_col.replace('`', '').strip()
                read_mode = read_col.replace('*', '').strip().lower()
                tier = tier.replace('*', '').strip().upper()

                if not file_rel:
                    continue

                table_files.add(file_rel)
                target_file = (module_dir / file_rel).resolve()

                # Rule: File must exist on disk
                if not target_file.exists():
                    self.add_finding("Missing File in Tier Table", readme_path, f"Declared file does not exist on disk: '{file_rel}'")
                    continue

                # Rule: Check Tier-to-Read mapping
                expected_reads = {"A": "full", "B": "grep-only", "C": "never"}
                if tier in expected_reads:
                    expected = expected_reads[tier]
                    if read_mode != expected:
                        self.add_finding(
                            "Tier-to-Read Mismatch",
                            readme_path,
                            f"File '{file_rel}' has Tier '{tier}' but Read is '{read_mode}' (expected '{expected}')."
                        )

                # Rule: Size drift check (within 10%)
                declared_bytes = parse_size_str(size_col)
                if declared_bytes is not None and target_file.is_file():
                    actual_bytes = target_file.stat().st_size
                    # Allow minor drift for files under 5 KB
                    if actual_bytes > 5120 and declared_bytes > 5120:
                        drift = abs(actual_bytes - declared_bytes) / declared_bytes
                        if drift > 0.10:
                            self.add_finding(
                                "Size Drift",
                                readme_path,
                                f"File '{file_rel}' size drifted by {drift*100:.1f}%: declared '{size_col}', actual {actual_bytes // 1024} KB."
                            )

            # Rule: Completeness check - all .md/.json in doc/ and data/ must be in Tier Table
            for search_dir_name in ("doc", "data"):
                search_dir = module_dir / search_dir_name
                if not search_dir.is_dir():
                    continue
                for item in search_dir.rglob("*"):
                    if item.is_file() and item.suffix.lower() in (".md", ".json"):
                        rel_to_module = str(item.relative_to(module_dir)).replace("\\", "/")
                        if rel_to_module not in table_files:
                            self.add_finding(
                                "Unregistered Document",
                                readme_path,
                                f"File exists in '{search_dir_name}/' but is missing from Tier Table: '{rel_to_module}'"
                            )

    def check_token_bombs(self) -> None:
        """Check 5: Any .md >= 150 KB must be Tier B (grep-only)."""
        for md_file in self.root.rglob("*.md"):
            if not md_file.is_file():
                continue
            size = md_file.stat().st_size
            if size >= TOKEN_BOMB_THRESHOLD_BYTES:
                # Read frontmatter if present
                try:
                    with open(md_file, "r", encoding="utf-8", errors="ignore") as f:
                        head = "".join([f.readline() for _ in range(20)])
                except Exception:
                    head = ""

                tier_m = re.search(r'^tier:\s*([A-Za-z]+)', head, re.MULTILINE | re.IGNORECASE)
                if tier_m:
                    assigned_tier = tier_m.group(1).upper()
                    if assigned_tier != "B":
                        self.add_finding(
                            "Token Bomb",
                            md_file,
                            f"File size is {size // 1024} KB (>= 150 KB) but assigned Tier is '{assigned_tier}' (MUST be 'B')."
                        )
                else:
                    # Check if referenced in any Tier Table as Tier A
                    # This file is large text and must be treated as Tier B
                    pass

    def check_source_provenance(self) -> None:
        """Check 6: Source files in _source/ must have markdown twins."""
        for source_dir in self.root.rglob("_source"):
            if not source_dir.is_dir():
                continue
            module_dir = source_dir.parent
            readme_path = module_dir / "README.md"
            tier_table_text = ""
            if readme_path.is_file():
                try:
                    with open(readme_path, "r", encoding="utf-8", errors="ignore") as f:
                        tier_table_text = f.read()
                except Exception:
                    pass

            for src_file in source_dir.rglob("*"):
                if src_file.is_file():
                    if src_file.name.lower() == "readme.md":
                        continue
                    rel_to_module = str(src_file.relative_to(module_dir)).replace("\\", "/")
                    # Check if declared in Tier Table
                    if rel_to_module not in tier_table_text and src_file.name not in tier_table_text:
                        self.add_finding(
                            "Unmapped Source File",
                            src_file,
                            f"Source file '{rel_to_module}' has no declared .md twin in Tier Table."
                        )


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate DocBusinessThienAn 3-Tier standard and links.")
    parser.add_argument("target", nargs="?", default="DocBusinessThienAn", help="Target directory to inspect")
    parser.add_argument("--json", action="store_true", help="Output findings as JSON")
    args = parser.parse_args()

    target_path = Path(args.target).resolve()
    if not target_path.exists():
        # Try finding DocBusinessThienAn under current working directory
        candidate = Path.cwd() / args.target
        if candidate.exists():
            target_path = candidate
        else:
            sys.stderr.write(f"Error: Target path does not exist: {args.target}\n")
            return 2

    # If pointing to project root, narrow to DocBusinessThienAn if present
    if target_path.name != "DocBusinessThienAn" and (target_path / "DocBusinessThienAn").is_dir():
        target_path = target_path / "DocBusinessThienAn"

    validator = DocValidator(target_path)
    findings = validator.run_all()

    if args.json:
        output = {
            "target": str(target_path),
            "findings_count": len(findings),
            "passed": len(findings) == 0,
            "findings": findings,
        }
        print(json.dumps(output, indent=2, ensure_ascii=False))
    else:
        print(f"=== DOC STRUCTURE & TIER VALIDATOR ===")
        print(f"Target: {target_path}")
        print(f"Findings: {len(findings)}\n")

        if findings:
            for i, f in enumerate(findings, 1):
                level_icon = "❌" if f["level"] == "error" else "⚠️"
                print(f"{i}. {level_icon} [{f['category']}] {f['file']}")
                print(f"   {f['message']}\n")
            print(f"Result: FAILED with {len(findings)} findings.")
            return 1
        else:
            print("✅ All document structure and link checks passed with 0 findings.")
            return 0


if __name__ == "__main__":
    raise SystemExit(main())
