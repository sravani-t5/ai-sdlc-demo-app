#!/usr/bin/env python3
"""Append one structured entry to ai-interaction-log.jsonl.
Usage: scripts/log-ai.sh "<agent>" "<step>" "<request>" "<result>" [--flag value ...]
"""
import argparse, json, os, re, subprocess
from datetime import datetime, timezone
from pathlib import Path

CUSTOM_AGENTS = {"spec-agent", "implementer", "test-agent", "reviewer"}


def split_list(value):
    return [x.strip() for x in value.split(",") if x.strip()] if value else []


def split_records(value, keys):
    records = []
    for chunk in (value.split(";") if value else []):
        parts = [p.strip() for p in chunk.split("|")]
        if not parts or not parts[0]:
            continue
        parts += [""] * (len(keys) - len(parts))
        records.append(dict(zip(keys, parts[: len(keys)])))
    return records


def slug(text):
    return re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-")[:40] or "x"


def main():
    p = argparse.ArgumentParser()
    for name in ("agent", "step", "request", "result"):
        p.add_argument(name)
    for flag in ("name", "type", "tool", "model", "mode", "stage", "related", "parent", "context",
                 "skills", "subagents", "mcp", "tools", "human", "verify", "limitation",
                 "status", "artifacts", "time", "id"):
        p.add_argument("--" + flag)
    a = p.parse_args()

    agent = a.agent.lower()
    is_hook = "hook" in agent
    is_custom = agent in CUSTOM_AGENTS
    when = a.time or datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    compact = datetime.strptime(when, "%Y-%m-%dT%H:%M:%SZ").strftime("%Y%m%dT%H%M%SZ")

    if a.tool:
        tool = a.tool
    elif is_hook:
        tool = "git post-commit hook"
    elif "claude" in agent:
        tool = "Claude (claude.ai chat)"
    else:
        tool = os.environ.get("AI_TOOL", "GitHub Copilot in VS Code")

    entry = {
        "id": a.id or f"{compact}-{slug(a.agent)}-{slug(a.step)}",
        "timestamp": when,
        "stage": a.stage or slug(a.step),
        "agent": "agent",
        "agent_name": a.name or a.agent,
        "agent_type": a.type or ("custom" if is_custom else "built-in"),
        "ai_tool": tool,
        "model": a.model or os.environ.get("AI_MODEL", "not recorded"),
        "mode": a.mode or ("n/a" if is_hook else "agent"),
        "related_ids": split_list(a.related),
        "parent_task": a.parent,
        "context": a.context or "Assignment for AI in SDLC: approval limits by role",
        "request": a.request,
        "skills_used": split_records(a.skills, ["name", "type", "purpose", "invocation"]),
        "subagents_used": split_records(a.subagents, ["name", "type", "task", "status"]),
        "mcp_tools_used": split_records(a.mcp, ["server", "tool", "purpose"]),
        "tools_used": split_list(a.tools),
        "suggestion": a.result,
        "human_decision": a.human,
        "action_verification": a.verify,
        "limitation": a.limitation,
        "status": a.status or "applied",
        "artifacts": split_list(a.artifacts),
    }
    root = Path(subprocess.check_output(["git", "rev-parse", "--show-toplevel"], text=True).strip())
    with open(root / "ai-interaction-log.jsonl", "a", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(entry, ensure_ascii=False) + "\n")


if __name__ == "__main__":
    main()
