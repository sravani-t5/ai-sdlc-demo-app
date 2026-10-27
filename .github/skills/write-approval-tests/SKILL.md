---
name: write-approval-tests
description: Write xUnit tests for the invoice approval rules (limits by role, two distinct approvers). Use when adding or reviewing tests for ApprovalService.
---

# Writing approval-rule tests

Follow AGENTS.md. Tests go in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs and use FakeInvoiceRepository.

## Required cases
- Boundaries exactly: Manager at 50,000 passes and 50,001 is rejected. Director at 500,000 passes and 500,001 needs a second approver.
- Over 500,000: first approval leaves the invoice PendingApproval; a second distinct Director approves it.
- Negative rule R4: the same user approving twice is rejected, including the same username with different casing and the same user under a different role.
- Negative rule R3: a Manager as second approver is rejected.
- Every rejection test must assert the invoice is unchanged: Status, approver list, DecidedBy, DecidedAt.
- The rejection reason must be non-empty and name the cause (limit exceeded, same approver, second approver not a Director).

## Test quality checklist
- Name tests Method_Condition_ExpectedResult.
- Each test has at least one meaningful assertion on state, not just "does not throw".
- Ask of each test: if the code were wrong, would this fail? Mentally change a >= to > or drop the same-user check to confirm.
- Do not weaken or delete an existing test to make a new one pass.
