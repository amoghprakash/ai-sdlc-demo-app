# Research: Approval Limits by Role

## Decision

The invoice-approval feature will introduce Manager and Director roles while preserving the current Approver/Admin model and keeping business logic in `backend/src/InvoiceApp.Domain`.

The approval policy will be enforced in the domain service as follows:

- Manager may approve invoices up to and including 50,000.
- Director may approve invoices up to and including 500,000.
- Invoices above 500,000 require a two-stage approval: first approver is recorded and the invoice enters a pending second-approval state.
- The first approver may be either a Manager or a Director.
- The second approver must be a different user and must be a Director.
- The same user cannot count as both approvers on the same invoice.
- Invalid attempts must return a clear reason and leave the invoice state and prior approval data unchanged.

## Rationale

This approach matches the existing repository architecture: the domain layer owns the invoice lifecycle and approval rules, while the API layer only maps requests and responses. It also aligns with the constitution’s requirement that invalid approval attempts be atomic and that approval thresholds be explicitly validated at critical boundaries.

## Alternatives considered

1. Single-approver policy for all invoice values
   - Rejected because it does not satisfy the higher-value dual-approval requirement.

2. Treating the first approval above 500,000 as rejected
   - Rejected because the requirement explicitly states that the first valid approval must be recorded and the invoice left pending for a second approval.

3. Extending the API layer with business logic
   - Rejected because controllers should not contain business rules; the domain layer is the correct authoritative location.

4. Replacing current demo roles entirely
   - Rejected because the application already has an Approver/Admin model and the requirement specifically preserves the existing architecture while adding new roles.

## Open implementation notes

- The invoice status model will need to represent a pending second-approval state above 500,000.
- The domain service should validate role, amount, duplicate-user checks, and second-approver authority before any invoice mutation occurs.
- The API and UI should surface clear rejection reasons without exposing implementation details.
