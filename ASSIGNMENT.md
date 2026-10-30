# AI in SDLC Assignment: Approval limits by role

## Links
- Pull request: https://github.com/sravani-t5/ai-sdlc-demo-app/pull/1
- Fork and branch: https://github.com/sravani-t5/ai-sdlc-demo-app (branch feature/approval-limits)
- Demo video: TODO paste the video link here
- AI interaction log: ai-interaction-log.jsonl (structured, one JSON entry per line) and ai-interaction-log.md (readable summary generated from it)

## Overview
The feature adds approval limits by role to the invoice approval service of the demo app (ASP.NET Core 8):
a Manager may approve up to 50,000, a Director up to 500,000, anything above 500,000 needs two distinct
approvers with the second one a Director, the same user never counts twice, and a rejected attempt returns
a clear reason and changes nothing on the invoice.

Tools: GitHub Copilot in VS Code (Agent mode, free tier) for the Spec Kit steps, the implementation and the
review agent; Claude (chat) for planning, review of generated code and tests, and analysis of mutation results.
Only the public demo repository was shared with the tools. It contains fake demo credentials and no real or
customer data. Every interaction is in ai-interaction-log.md.

## 1. Context that earns its place (AGENTS.md)
AGENTS.md was extended with: the six approval rules (R1 to R6, including that Admin and unknown roles have no
approval authority), the assumptions, the layering for the feature (rules only in the Domain project, controllers
only pass data, the UI only shows the reason), the test, end-to-end and mutation commands, the testing rules
(exact boundaries, unchanged-invoice assertions on every rejection), the working method (two steps, one commit
each, stay on the branch, log every interaction) and the review expectations (a verdict for every finding).

Evidence that it matters: when Copilot generated the project constitution, my prompt did not ask it to log.
It still ran scripts/log-ai.sh and checked the current branch with git branch --show-current, because AGENTS.md
tells every agent to do both. It also stayed on feature/approval-limits for every Spec Kit step.

## 2. Skills, hooks and agents
- Skills (.github/skills): write-approval-tests (required boundary and negative cases, quality checklist) and
  review-diff (review checklist and one fixed findings table format). Spec Kit also added its own speckit-* skills.
- Hooks: a git post-commit hook (scripts/hooks/post-commit, activated with core.hooksPath) logs every commit,
  and scripts/log-ai.sh writes every log row in one fixed format:
  Time (UTC) | Agent | Step | Prompt summary | Result summary. The agents are instructed to call the same script.
  The review-diff skill fixes the format of review output. The script also repairs a missing trailing newline,
  after a bug where two rows merged into one line.
- Agents (.github/agents): spec-agent (writes spec, plan and tasks, never edits code), implementer (one task at
  a time, validate before mutating), test-agent (sub-agent of the implementer, uses write-approval-tests) and
  reviewer (sub-agent, read-only, uses review-diff). By design the implementer hands off to the test agent and the reviewer.
  In practice: I selected only the custom reviewer agent in the Copilot agent picker, for the pull request review (two runs in one session). The spec, implementation and test steps ran in Copilot's built-in Agent mode, which followed the instructions in AGENTS.md and used the Spec Kit skills. The spec, implementer and test agents are defined in .github/agents but I did not select them, so I did not exercise delegation between agents.

## 3. Specification (Spec Kit v0.16.0)
Files: .specify/ (constitution and templates) and specs/001-approval-limits/ (spec, plan, research, data model,
contracts, quickstart, tasks). Review of the generated spec found a contradiction: one requirement said a Director
approving above 500,000 is rejected, another said the first approval is accepted and the invoice stays pending.
Review of tasks.md found that one task would have blocked Admin at the controller, which contradicts rule R6
(the Domain decides). Both were corrected before any code was written.

## 4. Implementation in steps (each commit stands alone)
| Commit | Message |
|---|---|
| c9657fd | docs: extend AGENTS.md with approval rules, layering, test and review expectations |
| dc6f10c | chore: add logging script, post-commit hook, skills and agent definitions |
| bbf629b | docs: add Spec Kit constitution, specification, plan and tasks for approval limits |
| 90c8e4e | test: record mutation baseline before approval limits |
| 8d368a4 | feat: enforce single-approver limits by role (Manager 50k, Director 500k) |
| 2e0658f | feat: require two distinct approvers (second a Director) above 500k |
| e40a014 | test: add Playwright e2e for rejected over-limit approval |
| efe6630 | test: strengthen approval tests to kill surviving mutants |
| 63f55c6 | docs: update AI interaction log |
| 5e2287f | fix: reject a blank or missing approver name before approving (review finding 1) |
| (this one) | docs: add ASSIGNMENT.md write-up |

## 5. Tests
- 60 unit tests (xUnit): boundaries at 50,000 / 50,001 and 500,000 / 500,001, the legacy Approver role,
  Admin and unknown roles (including null and blank), blank or null approver names, first approval above 500,000
  stays pending, Director then different Director, Manager then Director, Director then Manager (rejected),
  Manager then Manager (rejected), same user with different casing, spaces or role (rejected), and every rejection
  asserts status, approver list, DecidedBy and DecidedAt are unchanged and that nothing was saved.
- 1 Playwright test: a Manager signs in, creates and submits an invoice of 50,001, tries to approve it, and
  the UI shows the rejection reason and the invoice stays Pending Approval, also after a reload.
- Checks that the tests catch real mistakes: changing > to >= on the limit failed 3 boundary tests; changing the
  case-insensitive username comparison to a case-sensitive one failed 3 tests; raising the Manager limit to 60,000
  made the Playwright test fail. Each change was reverted.

## 6. Mutation scores (Stryker.NET 4.13.0)
Whole project (dotnet stryker), before any approval code, 7 tests:
    Mutants created 51, killed 12, survived 10, no coverage 17
    The final mutation score is 30.77 %

Whole project after Step 2, 37 tests:
    Mutants created 94, killed 41, survived 12, no coverage 17
    The final mutation score is 58.57 %

Whole project after hardening the tests, 53 tests:
    Mutants created 94, killed 52, survived 1, no coverage 17
    The final mutation score is 74.29 %

Whole project after the review fix, 60 tests (final):
    Mutants created 103, killed 59, survived 1, no coverage 17
    The final mutation score is 76.62 %

ApprovalService.cs only (dotnet stryker -m "**/ApprovalService.cs"):
    Before: 20 mutants tested, killed 12, survived 8       The final mutation score is 60.00 %
    After hardening: 51 tested, killed 50, survived 1      The final mutation score is 98.04 %
    Final: 58 tested, killed 57, survived 1                The final mutation score is 98.28 %

The 17 uncovered mutants are all in DiscountCalculator.cs, an existing class with no tests that is unrelated to
approval limits (deferred). The one survivor in ApprovalService.cs changes Any() to All() in the same-user check.
It is an equivalent mutant: at that point the invoice never holds more than one approver, so no test can tell
the two apart (dismissed).

## 7. Review of the pull request
The Reviewer agent (read-only Copilot custom agent using the review-diff skill) reviewed the diff in two runs.
Run 1 used the review-diff prompt on the branch diff against main. The agent read only 5 of about 60 changed
files and reported no findings. I did not accept that as a clean result, because manual review and mutation
testing had already found real issues in the same code. Run 2 used a stricter prompt that named the files to read
in full and required a "checked and clean" table with evidence. It returned 3 findings and marked 6 of 7 checklist
items clean. My verdicts: 1 Fixed (blank or null approver name, commit 5e2287f), 1 Deferred (public mutable
Approvers list, not reachable through the API), 1 Dismissed (the end-to-end suite is thin, the brief asks for one
Playwright test). One claim in finding 1 was inaccurate: null and blank roles were already rejected and tested.
The pull request description lists these and the findings from my own review of the generated code and from the
mutation run, each with its source, a verdict and a reason.

## 8. What I learned
- Generated code and generated tests can pass each other and still be weak: the first tests depended on a
  duplicated threshold string, a machine-dependent currency format and a fake repository whose Update did nothing.
- The mutation run found what review missed: Reject on an already-decided invoice was untested, and no test
  could notice a missing save.
- A spec generated by a tool needs a human read: it contradicted itself on the first approval above 500,000.
- Context in AGENTS.md changes agent behaviour even when the prompt does not mention it.
- An AI review can say "no findings" after reading a fraction of the diff. Check what it actually read.

## How to run
- Unit tests: dotnet test backend/InvoiceApp.sln
- End-to-end: cd e2e && npm install && npx playwright install chromium && npm test
- Mutation: cd backend/tests/InvoiceApp.Tests && dotnet stryker

## AI interaction log
ai-interaction-log.jsonl holds one entry per AI interaction with these fields: tool and model (ai_tool, model), agent details
(agent_name, agent_type, mode), prompts and follow-ups (request), skills and sub-agents (skills_used, subagents_used), tools and
integrations (tools_used, mcp_tools_used), AI actions and outputs (suggestion, artifacts), human intervention (human_decision),
execution evidence (timestamp, action_verification, related_ids with commit ids) and the outcome (status, limitation).
ai-interaction-log.md is a readable summary generated from it (at a glance, human oversight, timeline).
scripts/log-ai.sh writes each entry, the git post-commit hook writes one per commit, and a Copilot hook configuration
(.github/hooks/ai-log.json) records agent sessions and tool calls in ai-hook-events.jsonl where the tool supports it.
Entries up to commit 8391f86 were reconstructed afterwards from the chat, the git history and the earlier markdown log, and say
so in their limitation field. Later entries are written live. No MCP servers were used. The repository's .mcp.json defines a Playwright MCP server, which I did not use; the Playwright test was run with npx playwright test.
