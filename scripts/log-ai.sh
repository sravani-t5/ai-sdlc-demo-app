#!/usr/bin/env bash
# Usage: scripts/log-ai.sh "<agent>" "<step>" "<prompt summary>" "<result summary>"
set -euo pipefail
LOG="$(git rev-parse --show-toplevel)/ai-interaction-log.md"
clean() { printf '%s' "${1:-n/a}" | tr '\n|' '  '; }
# make sure the file ends with a newline before appending
if [ -s "$LOG" ] && [ -n "$(tail -c1 "$LOG")" ]; then printf '\n' >> "$LOG"; fi
printf '| %s | %s | %s | %s | %s |\n' \
  "$(date -u +'%Y-%m-%d %H:%M')" "$(clean "${1:-}")" "$(clean "${2:-}")" "$(clean "${3:-}")" "$(clean "${4:-}")" >> "$LOG"
