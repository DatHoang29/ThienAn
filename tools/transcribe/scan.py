# -*- coding: utf-8 -*-
"""Map every .m4a under DocBusinessThienAn by size+sha1, and note which already
have a transcript. Read-only."""
import sys, os, hashlib, unicodedata
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\ThienAn\DocBusinessThienAn"


def nfc(s):
    return unicodedata.normalize("NFC", s)


def sha1(p):
    h = hashlib.sha1()
    with open(p, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


m4a = []
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        if f.lower().endswith(".m4a"):
            m4a.append(os.path.join(dp, f))

by_hash = {}
print("=== .m4a files ===")
for p in sorted(m4a):
    hs = sha1(p)
    by_hash.setdefault(hs, []).append(p)
    print(f"{os.path.getsize(p)/1048576:8.2f} MB  {hs[:16]}  {nfc(os.path.relpath(p, ROOT))}")

print("\n=== duplicate groups (same audio, different name) ===")
for hs, ps in by_hash.items():
    if len(ps) > 1:
        print(f"{hs[:16]}:")
        for p in ps:
            print(f"    {nfc(os.path.relpath(p, ROOT))}")

print("\n=== existing transcripts ===")
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        if f.lower().endswith(".md") and ("transcript" in f.lower() or "review" in f.lower()
                                          or "MakeUp" in f or "họp" in nfc(f).lower() or "hop" in f.lower()):
            p = os.path.join(dp, f)
            print(f"{os.path.getsize(p):>8} B  {nfc(os.path.relpath(p, ROOT))}")
