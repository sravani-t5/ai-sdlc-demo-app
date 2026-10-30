<!--
Sync Impact Report
- Version change: none -> 1.0.0
- Modified principles: none (new constitution)
- Added sections: Core Principles, Additional Constraints, Development Workflow, Governance
- Removed sections: none
- Follow-up TODOs: none
-->

# Invoice Approval Demo Constitution

## Core Principles

### I. Domain-First Business Rules
Business rules, approval decisions, and validation logic MUST live only in the Domain project. Controllers, middleware, and Program configuration MAY pass data into the service layer, but they MUST NOT implement approval logic, role limits, or business-rule checks themselves. This keeps the rule model testable, reviewable, and independent from the web layer.

### II. Rule Coverage and Boundary Precision
Every business rule MUST have automated tests, including negative cases and exact threshold boundaries. Approval logic MUST be validated at the boundary values called out by the feature spec, including 50,000 / 50,001 and 500,000 / 500,001. Tests MUST fail when a rule is violated and MUST assert the real outcome instead of only checking mock interactions.

### III. Rejection Must Preserve Invoice State
Any rejected approval attempt MUST leave the invoice exactly as it was before the attempt. The status, approver list, DecidedBy field, and DecidedAt timestamp MUST remain unchanged when the rule is violated. This requirement is non-negotiable because a failed approval must be atomic and auditable.

### IV. Incremental Delivery
Work MUST be completed in small, reviewable steps, with one commit per step unless a wider change is explicitly required by the task. This repository is not to be broadened with unrelated refactors. Changes MUST stay focused on the current task, and branch creation or switching is expressly not permitted while operating under this constitution.

### V. Secret Hygiene and Safe Demo Data
No real credentials, tokens, customer data, or production secrets MAY be committed to this repository. Demo accounts are allowed only when they use clearly fake values, and any credentials MUST be limited to the repository’s provided sample data and config conventions. The project MUST remain safe for demonstration and review.

## Additional Constraints

The following constraints apply across all work in this repository:

- Business logic MUST remain in the Domain project and MUST never be mixed into controllers, Program.cs, or UI code.
- Every rule change MUST include tests for both success and failure paths, with explicit negative tests for edge conditions and duplicate-user scenarios.
- Approval-limit behavior MUST be implemented as named constants in the Domain layer rather than inline literal thresholds.
- Demo users and sample credentials MUST be kept in appsettings.json only, with fake passwords and no real credentials.
- The project MUST remain focused on the current task and MUST NOT introduce unrelated changes or new branches.

## Development Workflow

The project workflow MUST follow these rules:

- Run the relevant test suite before and after each commit; the default command is dotnet test backend/InvoiceApp.sln.
- For approval logic, every rule from R1 through R5 MUST have at least one supporting test, and each rejection path MUST confirm the invoice remains unchanged.
- Work MUST proceed in small steps with one commit each so each rule or behavior change is isolated and reviewable.
- Review MUST verify rule boundaries, role checks, duplicate approver prevention, and the unchanged-state guarantee on rejected decisions.

## Governance

This constitution governs all software work in the repository. Any amendment to governance, architecture, testing expectations, or review requirements MUST be documented here with a version bump, a rationale, and a clear date of amendment. Compliance reviews MUST verify that business rules remain in the Domain project, that each rule is backed by tests, and that rejected approvals cannot mutate the invoice.

**Version**: 1.0.0 | **Ratified**: 2026-10-27 | **Last Amended**: 2026-10-27
