# Tasks: Approval Limits by Role

**Input**: Design documents from `/specs/001-approval-limits/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

## Phase 1: Step 1 - Single-approver limits and rejection behavior

**Goal**: Add the Manager and Director role checks, enforce the 50000 and 500000 thresholds, add the rejection exception and reason handling, reject Admin and unknown roles, keep the legacy `Approver` role passing the controller gate while the Domain service decides authority, and lock the invoice state on every rejected attempt.

**Independent Test**: A manager can approve a 50000 invoice, a director can approve a 500000 invoice, a 50001 manager attempt is rejected, unsupported roles cannot approve, and the legacy `Approver` role is treated as a `Manager`.

### Tests for Step 1

- [ ] T000 [US1] Record the baseline mutation score before any code changes by running `dotnet stryker` from `backend/tests/InvoiceApp.Tests` and saving the text output to `notes-mutation-before.txt`
- [ ] T001 [P] [US1] Add Manager limit tests at 50000 and 50001 in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs
- [ ] T002 [P] [US1] Add Director limit tests at 500000 and 500001 in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs
- [ ] T003 [US1] Add rejection-state preservation assertions for invalid single-approver attempts in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs
- [ ] T004 [US1] Add Admin and unknown-role rejection tests in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs
- [ ] T005 [P] [US1] Add legacy `Approver` role-as-Manager tests at 50000 and 50001 covering FR-008 in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs

### Implementation for Step 1

- [ ] T006 [P] [US1] Add Manager and Director demo user entries in backend/src/InvoiceApp.Api/appsettings.json
- [ ] T007 [US1] Add named approval threshold constants in backend/src/InvoiceApp.Domain/ApprovalService.cs
- [ ] T008 [US1] Add `ApprovalRuleViolationException` in backend/src/InvoiceApp.Domain/ApprovalRuleViolationException.cs
- [ ] T009 [US1] Check whether `Invoice` already records approvers as a list of `(username, role)` values in backend/src/InvoiceApp.Domain; if not, add it with no change to existing behaviour
- [ ] T010 [US1] Update `ApprovalService` to pass the current `username` and `role` into the approval method and validate all approval rules before any mutation occurs in backend/src/InvoiceApp.Domain/ApprovalService.cs
- [ ] T011 [US1] Keep `Admin` and legacy `Approver` passing the controller role gate; `ApprovalService` alone decides whether a role may approve under R6, and `Handle(...)` maps `ApprovalRuleViolationException` to 409 without putting limit logic into the controller in backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs
- [ ] T012 [US1] Final commit: `git commit -m "feat: approval limits single-approver rules"`

**Checkpoint**: Step 1 should be independently testable and complete as a commit-sized approval-limits increment.

---

## Phase 2: Step 2 - Two-distinct-approver rule for invoices above 500000

**Goal**: Enforce the multi-approver rule, require a second Director for high-value invoices, reject duplicate users across role/casing, and verify the invoice stays PendingApproval until the second valid Director approval.

**Independent Test**: A 500001 invoice can be approved by a first Manager or Director and then finalized only by a second, different Director; same-user or wrong-role second approvals are rejected without mutation.

### Tests for Step 2

- [ ] T013 [P] [US2] Add first-approval success tests for Director-first and Manager-first paths in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs
- [ ] T014 [P] [US2] Add second-approval rejection tests for Manager as second approver in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs
- [ ] T015 [P] [US2] Add duplicate-user rejection tests for same username with different casing in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs
- [ ] T016 [US2] Add duplicate-user rejection tests for the same user holding a different role in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs
- [ ] T017 [US2] Add unchanged-state assertions for every invalid second-approver attempt in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs

### Implementation for Step 2

- [ ] T018 [US2] Add high-value invoice rule logic and two-approver tracking in backend/src/InvoiceApp.Domain/ApprovalService.cs
- [ ] T019 [US2] Enforce same-user detection using case-insensitive username comparison in backend/src/InvoiceApp.Domain/ApprovalService.cs
- [ ] T020 [US2] Require the second approver to be a different Director for invoices above 500000 in backend/src/InvoiceApp.Domain/ApprovalService.cs
- [ ] T021 [US2] Validate that the invoice remains `PendingApproval` after a valid first approval and only becomes `Approved` after the second valid Director approval in backend/src/InvoiceApp.Domain/ApprovalService.cs
- [ ] T022 [US2] Final commit: `git commit -m "feat: approval limits two-approver enforcement"`

**Checkpoint**: Step 2 should be independently testable and complete as the second commit-sized approval-limits increment.

---

## Phase 3: Final validation and UI regression

**Goal**: Confirm the user-facing rejection path works through the static UI and run mutation testing for the approval rules.

**Independent Test**: A rejected over-limit approval from the UI shows the API error message and does not mutate the invoice.

- [ ] T023 [P] Add a Playwright regression test for a rejected over-limit approval flow in e2e/tests/approval-limits.spec.ts
- [ ] T024 [P] Add page-object support for the rejection message and approval flow in e2e/pages/invoice-page.ts
- [ ] T025 [P] Verify the UI shows the rejection reason returned by the API in backend/src/InvoiceApp.Api/wwwroot/app.js and the matching DOM contract in e2e tests
- [ ] T026 Run `dotnet tool install -g dotnet-stryker --version 4.13.0` and `dotnet stryker` from backend/tests/InvoiceApp.Tests, save the text output to `notes-mutation-after.txt`, and validate mutation resilience
- [ ] T027 Final commit: `git commit -m "test: approval-limits validation and mutation checks"`

---

## Dependencies & Execution Order

- Phase 1 must complete before Phase 2.
- Phase 2 must complete before Phase 3.
- Tasks marked [P] may run in parallel inside their phase when different files are involved.
- The final mutation run is intentionally last because it validates the complete approval rule set across both commit-sized steps.

## Parallel Opportunities

- Within Phase 1: T001 and T006 can run in parallel; T002 and T003 can run in parallel; T004 and T007/T008 can run in parallel where staffing allows.
- Within Phase 2: T013, T014, T015, and T016 can run in parallel because they are distinct test cases in the same file.
- Within Phase 3: T023 and T024 can be prepared in parallel, then T025 and T026 execute after the UI regression is in place.

## Implementation Strategy

### MVP First

1. Complete Step 1 (Phase 1) to establish authority checks and the rejection contract.
2. Validate the boundary behavior and unchanged-state guarantees.
3. Complete Step 2 (Phase 2) for multi-approver enforcement.
4. Validate with Playwright UI regression and mutation testing in Phase 3.

### Incremental Delivery

- Commit 1: single-approver rules (Manager/Director thresholds, rejection exception, Admin/unknown-role rejection, legacy `Approver` handling)
- Commit 2: multi-approver rules (second Director requirement, duplicate-user check, case-insensitive comparison)
- Final validation: UI regression + mutation testing
