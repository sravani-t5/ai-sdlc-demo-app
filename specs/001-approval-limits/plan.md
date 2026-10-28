# Implementation Plan: Approval Limits by Role

**Branch**: feature/approval-limits | **Date**: 2026-10-27 | **Spec**: specs/001-approval-limits/spec.md

**Input**: Feature specification from `/specs/001-approval-limits/spec.md`

## Summary

The approval workflow will enforce approval limits in `ApprovalService` within `InvoiceApp.Domain`, using named constants for the 50000 and 500000 thresholds. Manager approvals are allowed up to 50000, Director approvals are allowed up to 500000, and invoices above 500000 require a second distinct Director approval before the invoice can become `Approved`. Rejected attempts must throw `ApprovalRuleViolationException` and leave the invoice state unchanged. The API layer remains a thin wrapper that passes session user and role through to the service, while the static UI continues to surface the API rejection reason to the user.

## Technical Context

**Language/Version**: .NET 8 / ASP.NET Core Web API in C#

**Primary Dependencies**: ASP.NET Core MVC, xUnit, Playwright, static browser UI in `wwwroot`

**Storage**: In-memory repository (`InMemoryInvoiceRepository`) for the demo app

**Testing**: xUnit for service-level rules; Playwright for end-to-end checks of the role-based flow and UI error display

**Target Platform**: Local web application running in the browser against the ASP.NET Core API

**Project Type**: Web service with static UI and in-memory data store

**Performance Goals**: Not material for this feature; small invoice approval operations in memory, with invariant checks before mutation

**Constraints**: all approval logic must stay in the Domain project; rejected approvals must be atomic and preserve invoice state; no branch creation or switching for this feature

**Scale/Scope**: Single invoice approval workflow with role-based limits and multi-step approval for high-value invoices

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- PASS: Business rules remain in `InvoiceApp.Domain`; controllers and UI do not enforce approval policy.
- PASS: The design includes explicit unit tests for threshold boundaries and rejection paths.
- PASS: Rejected approval attempts are designed to leave status, approver list, `DecidedBy`, and `DecidedAt` unchanged.
- PASS: Implementation is planned in two steps: single-approver rules first, then the multi-approver high-value rule.
- RISK TO RESOLVE: The current API controller and any tests/UI that treat `Admin` as an approval-capable role do not match R6. This must be deliberately corrected during implementation and test updates.

## Project Structure

### Documentation (this feature)

```text
specs/001-approval-limits/
├── plan.md              # This file
├── research.md          # Research findings and decisions
├── data-model.md        # Entity and state definitions
├── quickstart.md        # Validation scenarios and commands
├── contracts/           # API contract docs for the approval flow
├── spec.md              # Requirements and acceptance criteria
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── InvoiceApp.Domain/
│   │   ├── ApprovalService.cs
│   │   ├── Invoice.cs
│   │   ├── InvoiceStatus.cs
│   │   └── IInvoiceRepository.cs
│   ├── InvoiceApp.Api/
│   │   ├── Controllers/
│   │   │   ├── ApprovalsController.cs
│   │   │   └── InvoicesController.cs
│   │   ├── Auth/
│   │   ├── wwwroot/
│   │   │   ├── app.js
│   │   │   ├── index.html
│   │   │   └── styles.css
│   │   └── appsettings.json
│   └── InvoiceApp.Api.csproj
├── tests/
│   └── InvoiceApp.Tests/
│       ├── ApprovalServiceTests.cs
│       └── FakeInvoiceRepository.cs
└── InvoiceApp.sln
```

**Structure Decision**: A single backend web-service project with a Domain layer and a static UI is sufficient; no new project type or branch is required.

## Complexity Tracking

No constitution violations require exception tracking for this feature.
