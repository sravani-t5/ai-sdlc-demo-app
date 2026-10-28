# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.
- `specs/001-approval-limits`: the Spec Kit specification, plan and tasks for the approval-limits feature. `.specify/memory/constitution.md` holds the project principles.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only. The approve endpoint accepts Manager, Director, Approver (legacy) or Admin at the controller, but the Domain service decides whether the role may approve (see R6).
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`

## Demo accounts (not real credentials)

- `demo.approver` / `ChangeMe123!` (role Approver)
- `demo.admin` / `ChangeMe123!` (role Admin)
- `demo.manager` / `ChangeMe123!` (role Manager), `demo.manager2` (role Manager) - planned, added in the implementation step
- `demo.director` / `ChangeMe123!` (role Director), `demo.director2` (role Director) - planned, added in the implementation step

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.

## Approval limits by role (feature spec)

Roles that may approve: `Manager`, `Director`. The legacy `Approver` role is treated as `Manager`. `Admin` and any unknown role have no approval authority under this feature (R6).

| Rule | Behaviour |
|---|---|
| R1 | A Manager may approve invoices with Amount <= 50,000 (50,000 itself is allowed). |
| R2 | A Director may approve invoices with Amount <= 500,000 (500,000 itself is allowed). |
| R3 | Amount > 500,000 needs two distinct approvers. The second approver must be a Director. |
| R4 | The same user can never count as both approvers on one invoice, whatever role they hold. Compare usernames, case-insensitively. |
| R5 | A rejected attempt throws an `ApprovalRuleViolationException` (a subclass of `InvalidOperationException`, so `Handle(...)` maps it to 409) with a clear reason. The invoice must be left exactly as it was: status, approver list, `DecidedBy`, `DecidedAt`. |
| R6 | Roles other than Manager, Director and legacy Approver (including Admin and unknown roles) cannot approve. The attempt is rejected with a reason and the invoice is unchanged. |

Assumptions (the spec in `specs/001-approval-limits/spec.md` is the source of truth; keep this table in sync with it):
- On a > 500,000 invoice, the first approver may be a Manager or a Director. A first approval is accepted, not rejected, and the invoice stays `PendingApproval` until a second, different Director approves; only then does it become `Approved`. A Director alone can never complete an invoice above 500,000.
- An invoice at or below a role's limit is approved in one step by that role.
- Thresholds live in named constants in the Domain project, not as literals.

## Layering for this feature

- Domain (`InvoiceApp.Domain`): all limit and two-approver logic in `ApprovalService`. Role is passed in as a parameter. The domain must not reference the web, sessions or `HttpContext`.
- Api (`InvoiceApp.Api`): controllers read the username and role from the session and pass them to the service. Controllers hold no limit logic.
- UI (`wwwroot/app.js`): shows the rejection reason returned by the API. It performs no rule checks.
- Add the demo users (`demo.manager`, `demo.manager2`, `demo.director`, `demo.director2`) in `appsettings.json` only. All demo passwords are fake.

## Testing rules

- Test command: `dotnet test backend/InvoiceApp.sln`
- End-to-end: `cd e2e && npm install && npx playwright install chromium && npm test`
- Mutation testing: `dotnet tool install -g dotnet-stryker --version 4.13.0`, then run `dotnet stryker` from `backend/tests/InvoiceApp.Tests`.
- Every rule R1 to R6 needs at least one test. R3, R4 and R6 are the rules agents get wrong, so each needs negative tests.
- Test boundaries exactly: 50,000 / 50,001 and 500,000 / 500,001.
- Every rejection test must also assert that the invoice is unchanged (status, approver list, `DecidedBy`, `DecidedAt`).
- Name tests `Method_Condition_ExpectedResult`. No test may be assertion-free. Read every generated test and ask: would this fail if the code were wrong?

## Working method for agents

- Work in small steps, one commit each. Step 1: single-approver limits (R1, R2, R5, R6). Step 2: two-approver rule (R3, R4).
- Do not change code outside the current task. Do not change existing tests to make them pass without saying why. If an existing test approves as `Admin` and now fails because of R6, say so and update it deliberately.
- Run `dotnet test` before every commit. Commit messages say what changed and why.
- Do not create or switch git branches. Stay on `feature/approval-limits`.
- Log each AI interaction by running `scripts/log-ai.sh "<agent>" "<step>" "<prompt summary>" "<result summary>"`.

## Review expectations

Every review finding gets one verdict in the PR description: **Fixed**, **Dismissed** (with a reason) or **Deferred** (with a reason). Check these first: off-by-one at the thresholds, role checks that can be bypassed (Admin, unknown roles), a rejected attempt that still mutates the invoice, the same user counted twice, and a second approver who is not a Director.