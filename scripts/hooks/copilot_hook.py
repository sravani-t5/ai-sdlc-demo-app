#!/usr/bin/env python3
"""Records Copilot agent lifecycle events automatically. Observation only; never blocks the agent."""
import json, subprocess, sys
from datetime import datetime, timezone
from pathlib import Path


def main():
    try:
        payload = json.load(sys.stdin)
        root = Path(subprocess.check_output(
            ["git", "-C", payload.get("cwd") or ".", "rev-parse", "--show-toplevel"], text=True).strip())
        keep = ("hook_event_name", "session_id", "tool_name", "agent_name", "agent_display_name",
                "stop_reason", "transcript_path", "initial_prompt", "source")
        record = {"recorded_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")}
        record.update({k: payload[k] for k in keep if payload.get(k) is not None})
        with open(root / "ai-hook-events.jsonl", "a", encoding="utf-8", newline="\n") as f:
            f.write(json.dumps(record, ensure_ascii=False) + "\n")
    except Exception:
        pass


main()
