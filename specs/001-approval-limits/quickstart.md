# Quickstart: Approval Limits Validation

## Prerequisites

- .NET 8 SDK
- ASP.NET Core app running locally
- xUnit test project available in `backend/tests/InvoiceApp.Tests`
- Demo users configured in the API appsettings

## Validation scenarios

1. Manager approves 50000 -> succeeds.
2. Manager approves 50001 -> rejected with a clear reason and unchanged invoice state.
3. Director approves 500000 -> succeeds.
4. Director approves 500001 as first approver -> accepted and invoice remains PendingApproval.
5. Second approver by different Director on a 500001 invoice -> invoice becomes Approved.
6. Second approver by a Manager or same user or same user with case-only change -> rejected and invoice unchanged.
7. Admin or unknown-role approval attempt -> rejected with a clear reason and no mutation.
8. UI displays the rejection reason returned by the API.

## Commands

- `dotnet test backend/InvoiceApp.sln`
- Run the app with `dotnet run --project backend/src/InvoiceApp.Api`
- Open the UI and verify the rejection message is shown for invalid approval attempts.

## Expected outcomes

- Invalid approvals return a clear message and maintain the original invoice state.
- Valid approvals follow the role thresholds and two-approver rule.
- Admin-based approval paths no longer approve under this feature, even if the controller previously allowed them.
