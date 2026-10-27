# Invoice Approval Contract

## Overview

The invoice approval flow is exposed through the existing API and UI layer. Business validation is enforced in the domain service so the HTTP contract remains simple and the domain rules remain authoritative.

## Endpoints

### POST /api/approvals/{invoiceId}/approve

Request:

- Path parameter: `invoiceId`
- Authenticated user session required
- Role validation performed by the domain service based on the current user and invoice amount

Success response:

- HTTP 200
- Returns the updated invoice object

Failure response:

- HTTP 409 with a clear error message when the approval is invalid
- Invoice state remains unchanged

### POST /api/approvals/{invoiceId}/reject

Request:

- Path parameter: `invoiceId`
- Body: `{ "reason": "string" }`
- Authenticated user session required

Success response:

- HTTP 200
- Returns the updated invoice object with status set to rejected

Failure response:

- HTTP 400 with reason when input is invalid
- Invoice state remains unchanged

## High-value invoice behavior

For invoices above 500,000:

- A valid first approval records the approver and places the invoice in a pending second-approval state.
- A second approval must use a different user and a Director role.
- A rejected second approval must preserve the earlier recorded first approval data.

## Validation message expectations

The API should return user-facing error text that explains why an approval failed, for example:

- "Manager approvals are limited to invoices up to 50,000."
- "The same user cannot act as both approvers on the same invoice."`r`n- "The second approver must be a Director."
- "The same user cannot act as both approvers on the same invoice."

These messages must not rely on internal implementation details and should be safe for business users to interpret.

