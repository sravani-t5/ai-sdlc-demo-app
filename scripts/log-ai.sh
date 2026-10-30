#!/usr/bin/env bash
# Usage: scripts/log-ai.sh "<agent>" "<step>" "<prompt>" "<result>" [--model M --mode agent --skills "n|type|purpose|auto" --tools "a,b" --human "..." --verify "..." --status applied --artifacts "f1,f2"]
# Appends a structured entry to ai-interaction-log.jsonl and regenerates the readable summary ai-interaction-log.md
set -euo pipefail
ROOT="$(git rev-parse --show-toplevel)"
PY="$(command -v python || command -v python3 || true)"
if [ -z "$PY" ]; then echo "warning: python not found, log skipped" >&2; exit 0; fi
"$PY" "$ROOT/scripts/log_ai.py" "$@" || echo "warning: structured log entry failed" >&2
"$PY" "$ROOT/scripts/summarize_log.py" || echo "warning: summary not regenerated" >&2
