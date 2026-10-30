# Feature Specification: Approval Limits by Role

**Feature Branch**: feature/approval-limits

**Created**: 2026-10-27

**Status**: Draft

**Input**: User description: "Add approval limits by role to the invoice approval service. A Manager may approve invoices up to 50,000 (50,000 itself allowed). A Director may approve invoices up to 500,000. Any invoice above 500,000 needs two distinct approvers and the second approver must be a Director. The same user may never count as both approvers on one invoice, regardless of role or username casing. A rejected attempt returns a clear reason and changes nothing on the invoice (status, approver list, decided-by, decided-at). Assumptions: the legacy Approver role is treated as Manager; for invoices above 500,000 the first approver may be a Manager or Director and the invoice stays PendingApproval until a second, different Director approves; thresholds are named constants. Delivery must be in two steps: Step 1 single-approver limits and rejection behaviour; Step 2 the two-approver rule. Do not create or switch git branches. Stay on feature/approval-limits."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manager approval within policy limits (Priority: P1)

A manager needs to approve invoice requests that are within the manager's spending authority without unnecessary process delays. The system must allow manager approvals up to the correct threshold while rejecting requests that exceed that limit with a clear, actionable reason.

**Why this priority**: This is the baseline approval rule and governs the common approval path for most invoice decisions. If it is incorrect, the company cannot trust invoice approval outcomes.

**Independent Test**: A manager can approve a 50000 invoice and must be blocked from approving a 50001 invoice with a policy rejection reason.

**Acceptance Scenarios**:

1. **Given** a manager is acting on an invoice worth 50000, **When** the manager approves it, **Then** the invoice is marked approved and the approval succeeds.
2. **Given** a manager is acting on an invoice worth 50001, **When** the manager approves it, **Then** the approval is rejected with a clear limit-based reason and the invoice remains unchanged.

---

### User Story 2 - Director approval within policy limits (Priority: P1)

A director needs to approve higher-value invoices within their authority while preserving the invoice status and decision history if a rule is violated.

**Why this priority**: Directors have a higher approval limit, but that authority must still be enforced exactly. This protects the policy boundary and ensures consistent escalation behavior.

**Independent Test**: A director can approve a 500000 invoice, and a 500001 invoice is accepted only as the first step in a two-approver flow and stays PendingApproval until a second, different Director approves.

**Acceptance Scenarios**:

1. **Given** a director approves an invoice worth 500000, **When** the approval is submitted, **Then** the invoice is approved successfully.
2. **Given** a director approves an invoice worth 500001 as the first approver, **When** the approval is submitted, **Then** the approval is accepted, the invoice remains PendingApproval, and it becomes Approved only after a second, different Director approves.

---

### User Story 3 - High-value invoice requires a second director approver (Priority: P1)

For invoices above 500000, the approval path must include two distinct approvers, with the second approver always being a director. The system must prevent the same user from being counted twice and must leave the invoice unchanged if the approval attempt is invalid.

**Why this priority**: This is the highest-risk rule because it introduces multi-step approval and must reliably block duplicate or invalid approval combinations.

**Independent Test**: A high-value invoice can remain pending until a second, different director approves it, and invalid approver pairings or duplicate usernames are rejected with a clear reason and no invoice mutation.

**Acceptance Scenarios**:

1. **Given** an invoice worth 500001 is pending approval and a Director is the first approver, **When** a different Director submits the second approval, **Then** the invoice becomes Approved.
2. **Given** an invoice worth 500001 is pending approval and a Manager is the first approver, **When** a different Director submits the second approval, **Then** the invoice becomes Approved.
3. **Given** an invoice worth 500001 is pending approval and a Director is the first approver, **When** a Manager submits the second approval, **Then** the approval is rejected, the invoice remains unchanged, and the status stays PendingApproval.
4. **Given** an invoice worth 500001 is pending approval and a Manager is the first approver, **When** a different Manager submits the second approval, **Then** the approval is rejected, the invoice remains unchanged, and the status stays PendingApproval.
5. **Given** an invoice worth 500001 is pending approval and the second approver username matches the first approver using different casing, **When** the second approval is submitted, **Then** the approval is rejected and the invoice remains unchanged.
6. **Given** an invoice worth 500001 is pending approval and the same user holds a different role on the second approval attempt, **When** the second approval is submitted, **Then** the approval is rejected and the invoice remains unchanged.

---

### Edge Cases

- When an invoice amount is exactly 50000 or exactly 500000, the approval succeeds for the relevant role and the invoice reaches its allowed status without a rejection.
- When an invoice amount is 50001 or 500001, the system enforces the role limit or two-approver requirement and rejects invalid approval attempts without mutating the invoice.
- When a username is repeated in different casing, such as "ManagerOne" and "managerone", the system treats it as the same user and rejects the second approval as a duplicate.
- When an invoice above 500000 receives approval from the same user twice in the same decision process, the second approval is rejected and the invoice remains PendingApproval with the original decision metadata intact.
- When an approval attempt is rejected after an invoice already has a prior status or decision record, the invoice remains unchanged in status, approver list, DecidedBy, and DecidedAt.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a Manager to approve an invoice when the invoice amount is 50000 or less.
- **FR-002**: The system MUST reject a Manager approval when the invoice amount is greater than 50000 and provide a clear reason tied to the approval limit.
- **FR-003**: The system MUST allow a Director to approve an invoice when the invoice amount is 500000 or less.
- **FR-004**: The system MUST accept a Director approval for an invoice above 500000 as the first approver, keep the invoice PendingApproval, and require a second, different Director before the invoice can become Approved.
- **FR-005**: The system MUST require two distinct approvers for invoices above 500000.
- **FR-006**: The second approver for invoices above 500000 MUST be a Director.
- **FR-007**: The system MUST reject any approval attempt that attempts to use the same user as both approvers on a single invoice, regardless of role or username casing.
- **FR-008**: The system MUST treat the legacy Approver role as a Manager for approval-limit evaluation.
- **FR-009**: For invoices above 500000, the first approver may be either a Manager or a Director, and the invoice MUST remain PendingApproval until a second, different Director approves it.
- **FR-010**: Rejected approval attempts MUST return a clear policy reason and MUST leave the invoice unchanged in status, approver list, DecidedBy, and DecidedAt.
- **FR-011**: Approval thresholds MUST be expressed as named constants in the Domain layer rather than inline literal values.
- **FR-012**: The system MUST preserve the invoice state exactly when an approval rule is violated so the invoice remains auditable and consistent.
- **FR-013**: Roles other than Manager, Director, and the legacy Approver, including Admin and any unknown role, MUST have no approval authority under this feature; approval attempts by such roles MUST be rejected with a clear reason and the invoice MUST remain unchanged.
- **FR-014**: The web UI MUST show the rejection reason returned by the API to the user.

### Key Entities *(include if feature involves data)*

- **Invoice**: A financial record with an amount, current approval status, list of approvers, and decision metadata such as who decided and when.
- **Approver**: A user acting in a role such as Manager, Director, or the legacy Approver role, whose identity and permission determine whether an approval is valid.
- **Approval Decision**: The action of approving an invoice, including whether the decision succeeds, fails, or waits for additional approval.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Approval decisions correctly enforce the role thresholds at 50000, 50001, 500000, and 500001 for all relevant roles.
- **SC-002**: Rejected approval attempts never leave the invoice in a mutated state, with 100% of failed approval cases preserving status, approver list, DecidedBy, and DecidedAt.
- **SC-003**: High-value invoice approval paths require two distinct approvers and keep the invoice PendingApproval until a second different Director approves it.
- **SC-004**: Duplicate-approver attempts are rejected consistently regardless of username casing, with zero cases where the same user counts twice on one invoice.
- **SC-005**: Approval attempts from unsupported roles, including Admin and unknown roles, are rejected consistently and never advance invoice approval without an explicit valid approval path.

## Assumptions

- The legacy Approver role is treated as a Manager for approval-limit decisions.
- The approval authority depends on the invoice amount and approved role, not on unrelated invoice data.
- For invoices above $500,000, the first approver can be either a Manager or a Director, but the final approval requires a second, different Director.
- Thresholds are defined as named constants in the Domain layer and are not hard-coded at the controller or UI layer.
- The feature will be implemented in two delivery steps: first the single-approver limits and rejection rules, then the multi-approver high-value rule.
