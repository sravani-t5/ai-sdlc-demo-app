---
name: review-diff
description: Review a git diff of the approval-limits feature and report findings in a fixed table format. Use when reviewing a pull request.
---

# Reviewing the diff

Check, in this order:
1. Off-by-one at 50,000 and 500,000.
2. Role checks that can be bypassed (unknown role, legacy Approver, Admin).
3. A rejected attempt that still changes the invoice (status, approver list, DecidedBy, DecidedAt).
4. The same user counted twice (case differences, different roles).
5. Second approver not being a Director.
6. Business logic leaking into controllers or Program.cs.
7. Tests that cannot fail, are assertion-free, or miss a negative rule.

## Output format (always exactly this)
Return a markdown table with these columns, one row per finding, then a one-line summary:

| # | Severity (High/Med/Low) | File:line | Finding | Suggested fix |

Do not give a verdict. The author adds Fixed, Dismissed (with reason) or Deferred (with reason) in the PR description.
