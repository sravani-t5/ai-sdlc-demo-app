#!/usr/bin/env bash
# Usage: scripts/log-ai.sh "<agent>" "<step>" "<prompt summary>" "<result summary>"
set -euo pipefail
LOG="$(git rev-parse --show-toplevel)/ai-interaction-log.md"
clean() { printf '%s' "${1:-n/a}" | tr '\n|' '  '; }
printf '| %s | %s | %s | %s | %s |\n' \
  "$(date -u +'%Y-%m-%d %H:%M')" "$(clean "${1:-}")" "$(clean "${2:-}")" "$(clean "${3:-}")" "$(clean "${4:-}")" >> "$LOG"
