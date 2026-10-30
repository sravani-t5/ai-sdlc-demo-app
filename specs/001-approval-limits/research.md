# Research: Approval Limits by Role

## Decision

The approval feature will enforce role-based limits in the Domain layer via `ApprovalService`, with the API and UI acting as thin adapters. Role checks are performed by the service using the username and role context passed from the session, and every invalid approval attempt throws a domain-specific `ApprovalRuleViolationException` before any invoice mutation occurs.

## Rationale

This keeps all business rules in the Domain project and keeps controllers focused on session and request handling. It also matches the repo constitution: business logic must stay out of `Program.cs`, API controllers, and UI code, while rejected approvals must leave the invoice absolutely unchanged.

## Alternatives considered

- Controller-side enforcement: rejected because it violates the Domain-first rule and weakens testability.
- UI validation only: rejected because the UI is a client-only surface and cannot be trusted for business rule enforcement.
- Allowing Admin to approve: rejected because the feature spec explicitly states Admin and unknown roles have no approval authority under R6.

## Findings

- The existing `ApprovalService` is a single-approver model with no role checks, no limit constants, and no mutation guard on invalid decisions.
- `ApprovalsController` currently allows `Approver` and `Admin` roles through `[RequireRole("Approver", "Admin")]`, which means a legacy Admin approval path exists in code and will need deliberate adjustment to comply with R6.
- The static UI already displays rejection errors from the API in the decision-error area; the requirement for the UI to show the rejection reason is consistent with the existing client flow and should remain as a user-facing contract rather than a new rule-checking layer.
- The repository already tracks the approval-limits task on `feature/approval-limits` and must not create or switch branches while proceeding.
