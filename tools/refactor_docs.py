# -*- coding: utf-8 -*-
"""One-off: refactor DocBusinessThienAn to the new layout.

- Adds _source/audio/ + _source/README.md (Tier C guard) where missing.
- Adds doc/transcript/ + 00-catalog.md stub per module.
- Dedupes the 5 loose 'MakeUp Chi Ngô Gò Vấp*' recordings at the repo-doc root,
  moving real ones into the right module _source/audio/ with dated kebab names.
- Moves existing Plan/Transcript/*.md into module doc/transcript/.
- NFD-safe throughout (filenames on disk use combining marks).

Usage:  python tools/refactor_docs.py            # dry-run (print plan)
        python tools/refactor_docs.py --apply    # do it
"""
import sys, os, shutil, hashlib, subprocess, unicodedata

REPO = r"C:\ThienAn"
DOCROOT = os.path.join(REPO, "DocBusinessThienAn")
HNCL = os.path.join(DOCROOT, "HữuNghị-ChiLăng")
APPLY = "--apply" in sys.argv
sys.stdout.reconfigure(encoding="utf-8")

SOURCE_README = (
    "# _source — Bản gốc tài liệu (Human-only / Tier C)\n\n"
    "> 👤 **AI không đọc thư mục này.** Toàn bộ nội dung cần thiết đã được chuyển thể "
    "sang các file Markdown trong `doc/`. Chỉ mở file ở đây khi người dùng yêu cầu đích danh.\n"
)

log = []


def act(msg):
    log.append(msg)
    print(("APPLY " if APPLY else "PLAN  ") + msg)


def nfc(s):
    return unicodedata.normalize("NFC", s)


def resolve(dirpath, name):
    """Return real on-disk path of `name` inside dirpath, matching NFC-insensitively."""
    if not os.path.isdir(dirpath):
        return None
    for e in os.listdir(dirpath):
        if nfc(e) == nfc(name):
            return os.path.join(dirpath, e)
    return None


def sha1(p):
    h = hashlib.sha1()
    with open(p, "rb") as f:
        for c in iter(lambda: f.read(1 << 20), b""):
            h.update(c)
    return h.hexdigest()


def tracked(path):
    r = subprocess.run(["git", "-C", REPO, "ls-files", "--error-unmatch", path],
                       capture_output=True)
    return r.returncode == 0


def ensure_dir(p):
    if not os.path.isdir(p):
        act(f"mkdir  {os.path.relpath(p, REPO)}")
        if APPLY:
            os.makedirs(p, exist_ok=True)


def write_file(p, content):
    if os.path.exists(p):
        return
    act(f"write  {os.path.relpath(p, REPO)}")
    if APPLY:
        os.makedirs(os.path.dirname(p), exist_ok=True)
        with open(p, "w", encoding="utf-8") as f:
            f.write(content)


def same_bytes(a, b):
    try:
        return os.path.getsize(a) == os.path.getsize(b) and sha1(a) == sha1(b)
    except OSError:
        return False


def move(src, dst, into_source=False, dst_wins=False):
    """Move src->dst on disk. If the destination lives under a (git-ignored)
    _source/ tree and src is tracked, untrack it first (keep the bytes).
    Re-run safe: if dst already exists, drop src (when same bytes, or dst_wins)."""
    if src is None or not os.path.exists(src):
        act(f"SKIP move (missing): {os.path.relpath(dst, REPO)}")
        return
    if os.path.exists(dst):
        if same_bytes(src, dst) or dst_wins:
            remove(src, f"(bản đích {os.path.relpath(dst, REPO)} đã có, giữ bản đích)")
        else:
            act(f"WARN: {os.path.relpath(dst, REPO)} đã tồn tại & KHÁC nội dung src — bỏ qua, xử lý tay")
        return
    is_tracked = tracked(src)
    tag = "  [git untrack -> disk]" if (is_tracked and into_source) else ("  [git mv]" if is_tracked else "")
    act(f"move   {os.path.relpath(src, REPO)}  ->  {os.path.relpath(dst, REPO)}{tag}")
    if APPLY:
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        if is_tracked and into_source:
            subprocess.run(["git", "-C", REPO, "rm", "-q", "--cached", src], check=True)
            shutil.move(src, dst)
        elif is_tracked:
            subprocess.run(["git", "-C", REPO, "mv", src, dst], check=True)
        else:
            shutil.move(src, dst)


def remove(p, why=""):
    if p is None or not os.path.exists(p):
        return
    act(f"delete {os.path.relpath(p, REPO)}   {why}")
    if APPLY:
        if tracked(p):
            subprocess.run(["git", "-C", REPO, "rm", "-q", "-f", p], check=True)
        else:
            os.remove(p)


# ---------------------------------------------------------------- structure
MODULES = ["ShareData", "VideoWall", "WOS", "Plan"]
for m in MODULES:
    base = os.path.join(HNCL, m)
    ensure_dir(os.path.join(base, "_source", "audio"))
    if not resolve(os.path.join(base, "_source"), "README.md"):
        write_file(os.path.join(base, "_source", "README.md"), SOURCE_README)
    ensure_dir(os.path.join(base, "doc", "transcript"))

CATALOG = (
    "# Transcript — Mục lục\n\n"
    "> Bản ghi `.md` chuyển thể từ file ghi âm trong `../../_source/audio/`.\n"
    "> Mỗi dòng: file transcript · file audio nguồn · ngày · thời lượng · chủ đề.\n\n"
    "| Transcript | Audio nguồn | Ngày | Thời lượng | Chủ đề | Tier |\n"
    "|---|---|---|---|---|---|\n"
)
for m in ["ShareData", "VideoWall", "Plan"]:
    write_file(os.path.join(HNCL, m, "doc", "transcript", "00-catalog.md"), CATALOG)

# ---------------------------------------------------------------- recordings
root_files = {nfc(e): os.path.join(DOCROOT, e) for e in (os.listdir(DOCROOT) if os.path.isdir(DOCROOT) else [])}
plan_src = os.path.join(HNCL, "Plan", "_source")

# known duplicates: MakeUp base / 2  ==  Plan/_source Nội dung họp 8_9_2026(_2)
pairs = [
    ("MakeUp Chi Ngô Gò Vấp.m4a",   "Nội dung họp 8_9_2026_2.m4a", "2026-09-08-hop-ke-hoach-2.m4a"),
    ("MakeUp Chi Ngô Gò Vấp 2.m4a", "Nội dung họp 8_9_2026.m4a",   "2026-09-08-hop-ke-hoach-1.m4a"),
]
for root_name, plan_name, new_name in pairs:
    rp = root_files.get(nfc(root_name))
    pp = resolve(plan_src, plan_name)
    if rp and pp and os.path.getsize(rp) == os.path.getsize(pp) and sha1(rp) == sha1(pp):
        remove(rp, f"(trùng byte với Plan/_source/{plan_name})")
    elif rp:
        act(f"WARN: {root_name} khác nội dung Plan/{plan_name} — giữ lại, xử lý tay")
    # move the Plan/_source original into _source/audio/ with dated name
    if pp:
        move(pp, os.path.join(plan_src, "audio", new_name), into_source=True)

# ShareData review recordings: MakeUp 3/4/5 -> ShareData/_source/audio/
sd_audio = os.path.join(HNCL, "ShareData", "_source", "audio")
for root_name, new_name in [
    ("MakeUp Chi Ngô Gò Vấp 3.m4a", "2026-09-09-review-1.m4a"),
    ("MakeUp Chi Ngô Gò Vấp 4.m4a", "2026-09-09-review-2.m4a"),
    ("MakeUp Chi Ngô Gò Vấp 5.m4a", "2026-09-09-review-3.m4a"),
]:
    rp = root_files.get(nfc(root_name))
    move(rp, os.path.join(sd_audio, new_name), into_source=True)

# stray transcript-side files at root
for junk in ["MakeUp Chi Ngô Gò Vấp.md", "MakeUp Chi Ngô Gò Vấp 2.md",
             "MakeUp Chi Ngô Gò Vấp 2.md.partial.txt"]:
    remove(root_files.get(nfc(junk)), "(seed/tạm — transcript thật sẽ ở doc/transcript/)")

# ---------------------------------------------------------------- move existing transcripts
tdir = resolve(os.path.join(HNCL, "Plan"), "Transcript")
if tdir:
    mapping = [
        ("Nội dung họp 8_9_2026 .md.md", os.path.join(HNCL, "Plan", "doc", "transcript", "2026-09-08-hop-ke-hoach-1.md")),
        ("Nội dung họp 8_9_2026_2.md",   os.path.join(HNCL, "Plan", "doc", "transcript", "2026-09-08-hop-ke-hoach-2.md")),
        ("transcript-videowall-28082026.md", os.path.join(HNCL, "VideoWall", "doc", "transcript", "2026-08-28-videowall-chuan-bi.md")),
    ]
    mapped = {nfc(o) for o, _ in mapping}
    for old, dst in mapping:
        move(resolve(tdir, old), dst, dst_wins=True)  # bản đích đã thêm frontmatter
    # leftover files not in our mapping?
    rest = [e for e in os.listdir(tdir) if nfc(e) not in mapped] if os.path.isdir(tdir) else []
    if rest:
        act(f"WARN: {os.path.relpath(tdir, REPO)} còn file lạ: {rest}")
    else:
        act(f"rmdir  {os.path.relpath(tdir, REPO)}")
        if APPLY and os.path.isdir(tdir) and not os.listdir(tdir):
            os.rmdir(tdir)

print(f"\n{'APPLIED' if APPLY else 'DRY-RUN'} — {len(log)} action(s).")
