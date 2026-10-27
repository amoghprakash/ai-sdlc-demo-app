# Feature Specification: Approval Limits by Role

**Feature Branch**: `[001-approval-limits]`

**Created**: 2026-10-21

**Status**: Draft

**Input**: User description: "Add approval limits by role to the invoice approval service. The current demo has Approver and Admin roles only; this feature must introduce Manager and Director roles and demo users as part of the implementation. A Manager may approve invoices up to and including 50,000. A Director may approve invoices up to and including 500,000. Any invoice above 500,000 requires two distinct approvers, and the second approver must be a Director. The first approver for an invoice above 500,000 may be either a Manager or a Director. The same user may never count as both approvers on one invoice. A valid first approval above 500,000 must record the first approver and leave the invoice pending for a second approval instead of rejecting. The second approval must complete the approval and must come from a different user and a Director. Invalid attempts, such as the same user approving twice or a Manager attempting the second approval, must return a clear reason and must not change any invoice state or previously recorded approval data. Preserve the existing .NET 8 architecture and keep business rules in InvoiceApp.Domain."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manager approval within policy (Priority: P1)

A Manager needs to approve a lower-value invoice within the defined threshold. The approval should be accepted when the invoice amount is within the Manager boundary and should remain stable when the amount is above that threshold.

**Why this priority**: This is the normal approval flow for routine invoices and serves as the baseline validation for the role-based approval policy.

**Independent Test**: A Manager can approve an invoice at or below 50,000 and the invoice is approved in a valid state.

**Acceptance Scenarios**:

1. **Given** an invoice valued at 45,000, **When** a Manager approves it, **Then** the invoice is approved and the approval is accepted.
2. **Given** an invoice valued at 50,000, **When** a Manager approves it, **Then** the invoice is approved because the amount is exactly at the manager limit.
3. **Given** an invoice valued at 50,001, **When** a Manager attempts to approve it, **Then** the approval is rejected with a clear explanation and the invoice remains unchanged.

---

### User Story 2 - Director approval within the higher threshold (Priority: P1)

A Director needs to approve invoices that are within the Director ceiling. This rule allows a Director to handle larger invoices up to 500,000 while preserving the stricter review requirement for amounts above that ceiling.

**Why this priority**: The Director role exists to handle more significant approvals, but it still must honor the boundary conditions and escalation rules.

**Independent Test**: A Director can approve an invoice up to 500,000 and the system enforces the additional approval step above 500,000.

**Acceptance Scenarios**:

1. **Given** an invoice valued at 250,000, **When** a Director approves it, **Then** the invoice is approved.
2. **Given** an invoice valued at 500,000, **When** a Director approves it, **Then** the invoice is approved because the amount is exactly at the director limit.
3. **Given** an invoice valued at 500,001, **When** a Director attempts the first approval, **Then** the system records the first approver and leaves the invoice in a pending second-approval state instead of rejecting the first approval.

---

### User Story 3 - Two-step approval for high-value invoices (Priority: P1)

Invoices above 500,000 require a two-step approval process with independent approval authority. The first approver may be a Manager or Director, but the second approver must be a Director and must be a different user than the first approver.

**Why this priority**: This is the core control for high-value transactions and prevents a single user from being the sole decision-maker for large disbursements.

**Independent Test**: High-value invoices require a recorded first approval and then a valid second approval by a different Director to complete the approval.

**Acceptance Scenarios**:

1. **Given** an invoice valued at 600,000, **When** a Manager acts as the first approver and a different Director acts as the second approver, **Then** the first approval is recorded and the invoice moves to a pending second-approval state, and then the second approval completes the invoice approval.
2. **Given** an invoice valued at 600,000, **When** the same user tries to act as both approvers, **Then** the attempt is rejected with a clear reason and no invoice state or approval data changes.
3. **Given** an invoice valued at 600,000, **When** a Manager attempts to act as the second approver, **Then** the approval is rejected with a clear reason and the invoice remains unchanged.
4. **Given** an invoice valued at 600,000, **When** the second approver is not a Director, **Then** the approval is rejected and the invoice remains unchanged.

---

### User Story 4 - Rejected attempts remain safe and explainable (Priority: P1)

When an approval attempt fails, the business needs a clear explanation and assurance that no state change or previously recorded approval data has been altered.

**Why this priority**: This preserves the integrity of the approval record and helps users understand why an approval did not proceed.

**Independent Test**: Any invalid approval attempt returns a specific reason and leaves all invoice state and recorded approval data unchanged.

**Acceptance Scenarios**:

1. **Given** an invoice with a valid current state, **When** an invalid approval request is submitted, **Then** the system returns a clear reason for rejection and the invoice remains unchanged.
2. **Given** an invoice above 500,000 with an existing first approver recorded, **When** an invalid second approval is submitted by the same user or by a non-Director, **Then** the system rejects the attempt and preserves the recorded first approval data.

---

### Edge Cases

- What happens when an invoice amount is exactly 50,000 or exactly 500,000?
- How does the system treat a first approval for an invoice above 500,000 before the second approval is completed?
- How does the system reject duplicate approvers or a non-Director second approver without changing any prior approval data?
- How does the system communicate a rejection reason in a way that is understandable to a business user?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST introduce Manager and Director roles and associated demo users as part of the implementation while preserving the existing .NET 8 architecture and domain-layer business rules.
- **FR-002**: The system MUST allow a Manager to approve an invoice when the invoice value is less than or equal to 50,000.
- **FR-003**: The system MUST reject a Manager approval when the invoice value is greater than 50,000.
- **FR-004**: The system MUST allow a Director to approve an invoice when the invoice value is less than or equal to 500,000.
- **FR-005**: The system MUST require two distinct approvers for any invoice value above 500,000.
- **FR-006**: The first approver for an invoice above 500,000 MAY be either a Manager or a Director.
- **FR-007**: The system MUST record the first approver for an invoice above 500,000 and leave the invoice pending for a second approval rather than rejecting the first approval.
- **FR-008**: The system MUST require the second approver for an invoice above 500,000 to be a different user from the first approver.
- **FR-009**: The system MUST require the second approver for an invoice above 500,000 to be a Director.
- **FR-010**: The system MUST complete the invoice approval only after the second Director approval is accepted for an invoice above 500,000.
- **FR-011**: The system MUST reject any approval attempt in which the same user is used as both approvers on the same invoice.
- **FR-012**: The system MUST reject a second approval attempt by a Manager, regardless of invoice value above 500,000.
- **FR-013**: The system MUST return a clear, user-understandable rejection reason whenever an approval attempt is invalid.
- **FR-014**: The system MUST leave the invoice unchanged and MUST preserve any previously recorded approval data when an approval attempt is rejected.
- **FR-015**: The system MUST keep the approval business rules in the domain layer and preserve the existing .NET 8 architecture.

### Key Entities *(include if feature involves data)*

- **Invoice**: The invoice being reviewed, including its monetary value, approval status, and any recorded first or second approver information.
- **Approver**: A user who can participate in the approval workflow, identified by role and identity.
- **Role**: A permission classification for the approval workflow, such as Manager or Director, introduced as part of this feature.
- **Approval Decision**: A decision to approve or reject an invoice attempt, including the approver, role, amount reviewed, and the reason if rejected.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of valid Manager approvals at 50,000 or below are accepted without policy violations.
- **SC-002**: 100% of valid Director approvals at 500,000 or below are accepted without policy violations.
- **SC-003**: 100% of invoices above 500,000 record the first approver and enter the pending second-approval state without rejecting the valid first approval.
- **SC-004**: 100% of invoices above 500,000 require a different user and a Director for the second approval before the invoice is fully approved.
- **SC-005**: 100% of rejected approval attempts return a clear reason and leave the invoice state and previously recorded approval data unchanged.
- **SC-006**: Business stakeholders can confirm that the approval policy is predictable and auditable at the 50,000 and 500,000 decision boundaries.

## Assumptions

- The current demo application has Approver and Admin roles only, and this feature adds Manager and Director roles and supporting demo accounts as part of implementation.
- Approval rules are enforced at the business-logic layer and not duplicated in the user interface.
- Rejection messages are expected to be clear enough for a business user to understand and act on without technical support.
- The current project architecture remains in place, with business rules retained in the domain project and the API layer only handling user requests and response mapping.
- High-value invoice approval follows a two-stage process: first approval records the approver and sets the invoice to a pending state, then the second approval by a different Director completes the approval.
