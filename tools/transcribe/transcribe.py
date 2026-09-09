# -*- coding: utf-8 -*-
"""Transcribe recordings in <module>/_source/audio/*.m4a -> <module>/doc/transcript/<name>.md

faster-whisper large-v3 (int8, CPU), greedy + anti-repetition-loop settings.
Output carries provenance frontmatter and is re-run safe (finished .md is skipped).

Usage:
    python transcribe.py                      # default: ShareData review batch
    python transcribe.py <module> [<module>]  # e.g. python transcribe.py ShareData Plan
"""
import sys, os, time, datetime, io, glob

sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")
from faster_whisper import WhisperModel

HNCL = r"C:\ThienAn\DocBusinessThienAn\HữuNghị-ChiLăng"
MODULES = sys.argv[1:] or ["ShareData"]

# --- max-quality profile (time is not a constraint) ---
MODEL   = "large-v3"
COMPUTE = "float32"     # no quantization loss (slowest, most accurate on CPU)
THREADS = 8
LANG    = "vi"
BEAM    = 8             # explore more paths on ambiguous / noisy speech

# biases vocabulary toward real project jargon instead of phonetic guesses
INITIAL_PROMPT = (
    "Cuộc họp kỹ thuật dự án cao tốc Hữu Nghị – Chi Lăng (ITS, C2C, ISO 14827). "
    "Thuật ngữ: ShareData, ESHARE, gói tin, mapping, trường dữ liệu, payload, "
    "cấu hình gói tin, đối tác, WebAPI, JSON, XML, cơ sở dữ liệu, SqlSugar, "
    "VideoWall, tường màn hình, controller, ISAPI, cascade, decoder, kịch bản, "
    "camera, VDS, CCTV, PTZ, VMS, trạm thu phí, MQTT, Kafka, SolarWinds."
)


def hms(sec: float) -> str:
    sec = int(round(sec))
    return f"{sec // 3600:02d}:{(sec % 3600) // 60:02d}:{sec % 60:02d}"


def jobs():
    out = []
    for m in MODULES:
        adir = os.path.join(HNCL, m, "_source", "audio")
        tdir = os.path.join(HNCL, m, "doc", "transcript")
        for src in sorted(glob.glob(os.path.join(adir, "*.m4a"))):
            stem = os.path.splitext(os.path.basename(src))[0]
            out.append((m, src, os.path.join(tdir, stem + ".md")))
    return out


def transcribe(model, module, src, out_md):
    name = os.path.basename(src)
    print(f"\n=== {module}/{name} ===", flush=True)
    t0 = time.time()
    segments, info = model.transcribe(
        src, language=LANG, beam_size=BEAM, patience=1.5,
        initial_prompt=INITIAL_PROMPT,
        vad_filter=True, vad_parameters=dict(min_silence_duration_ms=1000, threshold=0.5),
        condition_on_previous_text=False, no_repeat_ngram_size=3,
        compression_ratio_threshold=2.4, log_prob_threshold=-1.0, no_speech_threshold=0.6,
        word_timestamps=True, hallucination_silence_threshold=2.0,
        temperature=[0.0, 0.2, 0.4, 0.6, 0.8, 1.0],
    )
    dur = info.duration
    print(f"duration={hms(dur)}  lang={info.language} ({info.language_probability:.2f})", flush=True)

    partial = out_md + ".partial.txt"
    seg_lines, paras, cur, last_end = [], [], [], 0.0
    with io.open(partial, "w", encoding="utf-8") as pf:
        for s in segments:
            txt = s.text.strip()
            if not txt:
                continue
            seg_lines.append(f"- `[{hms(s.start)}]` {txt}")
            if cur and (s.start - last_end) > 2.0:
                paras.append(" ".join(cur)); cur = []
            cur.append(txt); last_end = s.end
            pf.write(f"[{hms(s.start)}] {txt}\n"); pf.flush()
            print(f"  {(s.end/dur*100 if dur else 0):5.1f}%  [{hms(s.start)}] {txt[:70]}", flush=True)
    if cur:
        paras.append(" ".join(cur))

    today = datetime.date.today().isoformat()
    fm = [
        "---",
        "tier: A",
        "read: full",
        f"source: ../../_source/audio/{name}",
        f"duration: {hms(dur)}",
        f"model: faster-whisper/{MODEL} ({COMPUTE}, cpu, beam={BEAM}, +initial_prompt, +hallucination_guard)",
        f"transcribed: {today}",
        "status: raw-asr   # chưa hiệu đính",
        "---", "",
        f"# Transcript — {os.path.splitext(name)[0]}", "",
        "> ⚙️ Bản ghi tự động (ASR), **chưa hiệu đính**. Có thể sai tên riêng / thuật ngữ; "
        "đoạn im lặng cuối file dễ bị model bịa (\"like & share\"…) — xoá khi soát.", "",
        "## Toàn văn", "",
    ]
    fm += [f"{p}\n" for p in paras]
    fm += ["---", "", "## Theo mốc thời gian", ""] + seg_lines + [""]
    os.makedirs(os.path.dirname(out_md), exist_ok=True)
    with io.open(out_md, "w", encoding="utf-8") as f:
        f.write("\n".join(fm))
    os.remove(partial)
    print(f"--> wrote {out_md}  ({len(seg_lines)} seg, {(time.time()-t0)/60:.1f} min)", flush=True)


def main():
    js = jobs()
    print(f"modules={MODULES}  ->  {len(js)} file(s)", flush=True)
    for m, s, _ in js:
        print("   ", m, os.path.basename(s), flush=True)
    print(f"\nloading {MODEL} ({COMPUTE}, beam={BEAM}) ...", flush=True)
    model = WhisperModel(MODEL, device="cpu", compute_type=COMPUTE, cpu_threads=THREADS)
    print("model ready", flush=True)
    for m, src, out_md in js:
        if os.path.exists(out_md) and os.path.getsize(out_md) > 500 and not os.path.exists(out_md + ".partial.txt"):
            print(f"-- skip (done): {os.path.basename(out_md)}", flush=True)
            continue
        transcribe(model, m, src, out_md)
    print("\nALL DONE", flush=True)


if __name__ == "__main__":
    main()
