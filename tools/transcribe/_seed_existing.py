# -*- coding: utf-8 -*-
"""Copy the 2 already-existing Plan transcripts to the MakeUp names so the
batch skips them (files 1 & 2). One-off helper."""
import sys, os, shutil, unicodedata
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\ThienAn\DocBusinessThienAn"
PAIRS = [
    # (existing transcript rel-path, target .md name next to the .m4a)
    (r"HữuNghị-ChiLăng\Plan\Transcript\Nội dung họp 8_9_2026_2.md",
     "MakeUp Chi Ngô Gò Vấp.md"),
    (r"HữuNghị-ChiLăng\Plan\Transcript\Nội dung họp 8_9_2026 .md.md",
     "MakeUp Chi Ngô Gò Vấp 2.md"),
]


def nfc(s):
    return unicodedata.normalize("NFC", s)


def find(rel):
    """Resolve a possibly-NFD path under ROOT segment by segment."""
    cur = ROOT
    for seg in rel.split("\\"):
        matches = [e for e in os.listdir(cur) if nfc(e) == nfc(seg)]
        if not matches:
            return None
        cur = os.path.join(cur, matches[0])
    return cur


for src_rel, dst_name in PAIRS:
    src = find(src_rel)
    dst = os.path.join(ROOT, dst_name)
    if src is None:
        print(f"!! source not found: {src_rel}")
        continue
    if os.path.exists(dst):
        print(f"-- exists, skip: {dst_name}")
        continue
    with open(src, "r", encoding="utf-8") as f:
        body = f.read()
    note = (f"> ℹ️ Bản transcript có sẵn (tool khác), copy từ "
            f"`{nfc(os.path.relpath(src, ROOT))}`. Nguồn audio: `{os.path.splitext(dst_name)[0]}.m4a`.\n\n")
    with open(dst, "w", encoding="utf-8") as f:
        f.write(note + body)
    print(f"seeded: {dst_name}  ({os.path.getsize(dst)} B)")
