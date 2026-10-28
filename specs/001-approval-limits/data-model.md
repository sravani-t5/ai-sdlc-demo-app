# Data Model: Approval Limits by Role

## Entities

### Invoice

- Id: string
- CustomerName: string
- Amount: decimal
- Status: Draft, PendingApproval, Approved, Rejected
- DecisionNote: string or null
- DecidedBy: string or null
- DecidedAt: DateTime or null
- Approvers: collection of user names in approval sequence for multi-step approvals

### UserSession

- Username: string
- Role: string

### ApprovalDecision

- InvoiceId: string
- ApproverUsername: string
- Role: string
- DecisionType: approve or reject
- Result: accepted, pending, or rejected
- Reason: string or null

## Validation Rules

- Manager approval is valid only when Amount <= 50000.
- Director approval is valid when Amount <= 500000.
- Above 500000, approval requires a second distinct approver and the second approver must be a Director.
- A user may not be counted twice on the same invoice, even when username casing differs.
- Unsupported roles (Admin, unknown roles) are never authorized for approval under this feature.
- Rejected attempts must preserve the invoice state exactly.

## State Transitions

- Draft -> PendingApproval when submitted
- PendingApproval -> Approved when a valid approval succeeds
- PendingApproval -> PendingApproval when a valid first approval is recorded for amounts above 500000
- PendingApproval -> Rejected only when a reject action is explicitly issued, not for invalid approval attempts
