"""Write a handover file from a stopped engineer's transcript: every instruction the lead gave it, and its own notes.

usage: python handover.py <transcript.jsonl> <out.md> [notes=60] [note_chars=2500]
"""
import json
import sys

src, out = sys.argv[1], sys.argv[2]
keep = int(sys.argv[3]) if len(sys.argv) > 3 else 60
cut = int(sys.argv[4]) if len(sys.argv) > 4 else 2500
asks, notes = [], []
with open(src, encoding="utf-8", errors="replace") as handle:
    for line in handle:
        if '"type":"user"' not in line and '"type":"assistant"' not in line:
            continue
        try:
            row = json.loads(line)
        except ValueError:
            continue
        message = row.get("message") or {}
        content = message.get("content")
        stamp = row.get("timestamp", "")
        if row.get("type") == "user":
            if isinstance(content, str):
                texts = [content]
            else:
                texts = [b.get("text", "") for b in content or [] if isinstance(b, dict) and b.get("type") == "text"]
            for text in texts:
                text = text.strip()
                # Tool results and harness reminders are not the lead's words.
                if not text or text.startswith("<system-reminder") or text.startswith("[SYSTEM") or "task-notification" in text[:200]:
                    continue
                asks.append((stamp, text))
        elif row.get("type") == "assistant":
            for block in content or []:
                if isinstance(block, dict) and block.get("type") == "text" and block.get("text", "").strip():
                    text = block["text"].strip()
                    if "reached your Fable limit" in text:
                        continue
                    notes.append((stamp, text))
with open(out, "w", encoding="utf-8", newline="\n") as handle:
    handle.write("# Handover from an engineer who was cut off mid-task\n\n")
    handle.write("Times are UTC (local is UTC-8). Part 1 is everything the lead asked of that engineer, in order. Part 2 is that engineer's own notes, oldest first; the last few say what was in flight when it stopped. Treat part 2 as claims to check against the files on disk.\n\n")
    handle.write("## Part 1: what the lead asked\n\n")
    for stamp, text in asks:
        handle.write(f"### {stamp}\n\n{text}\n\n")
    handle.write(f"## Part 2: the engineer's own notes (last {keep})\n\n")
    for stamp, text in notes[-keep:]:
        handle.write(f"### {stamp}\n\n{text[:cut]}{' …' if len(text) > cut else ''}\n\n")
print(out, "asks", len(asks), "notes", len(notes))
