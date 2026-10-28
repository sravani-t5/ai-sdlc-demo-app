# Approval Contract

## Purpose

Defines the approval flow contract for invoice approval requests in the invoice approval demo service.

## Request

POST /api/approvals/{invoiceId}/approve

- Requires a signed-in session with a valid role.
- The API reads the current username and role from the session.
- The controller passes the username and role context into the Domain approval logic.

POST /api/approvals/{invoiceId}/reject

- Requires a signed-in session with a valid role.
- Accepts a JSON body containing a `reason` string.
- The controller passes the reject reason to the Domain service.

## Success responses

- HTTP 200 OK with the updated invoice payload.
- Valid approval succeeds if all authorization and validation rules pass.

## Failure responses

- HTTP 409 Conflict for domain rule violations, including invalid role authority, invalid amount thresholds, duplicate approvers, or invalid second Director conditions.
- HTTP 400 Bad Request for malformed requests.
- HTTP 404 Not Found if the invoice does not exist.

## Error shape

The API surfaces a clear message in the `error` field when a rule is violated, and the UI displays that message to the user.

## Notes

- `Admin` and unknown roles are not authorized for approval under this feature.
- For high-value invoices above 500000, valid approval is a two-step process: first approval may be Manager or Director; second approval must be a different Director.
- Invalid approvals do not mutate the invoice state.
