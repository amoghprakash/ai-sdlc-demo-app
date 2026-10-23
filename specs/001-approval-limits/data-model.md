# Data Model: Approval Limits by Role

## Entities

### Invoice

Represents the invoice under review.

| Field | Type | Notes |
|-------|------|-------|
| Id | string | Unique invoice identifier |
| CustomerName | string | Customer or vendor label |
| Amount | decimal | Invoice value used in the approval-limit checks |
| Status | enum | Draft, PendingApproval, PendingSecondApproval, Approved, Rejected |
| CreatedAt | DateTime | Invoice creation time |
| DecidedBy | string? | Final approver or closing approver, if applicable |
| DecidedAt | DateTime? | Decision timestamp |
| DecisionNote | string? | Reason captured for rejection or notation |
| FirstApprover | string? | User who recorded the first approval above 500,000 |
| SecondApprover | string? | User who completed the second approval above 500,000 |

### Role

Represents the approval authority assigned to a user.

| Field | Type | Notes |
|-------|------|-------|
| Name | string | Manager or Director |
| Limit | decimal | Business boundary for the role |

### ApprovalAttempt

A validation object or domain operation result representing one approval request.

| Field | Type | Notes |
|-------|------|-------|
| InvoiceId | string | Target invoice |
| Approver | string | User making the approval attempt |
| Role | Role | Role assigned to that approver |
| Amount | decimal | Invoice total under evaluation |
| IsValid | bool | Whether the request passes validation |
| Reason | string? | Rejection reason if invalid |

## State transitions

```text
Draft -> PendingApproval
PendingApproval -> Approved
PendingApproval -> Rejected
PendingApproval -> PendingSecondApproval (only when amount > 500,000 and the request is valid)
PendingSecondApproval -> Approved
```

## Validation rules

- Manager approvals are valid only when Amount <= 50,000.
- Director approvals are valid only when Amount <= 500,000.
- Amounts above 500,000 require a valid first approval, then a valid second approval by a different Director.
- The same user cannot appear as both approvers on one invoice.
- Rejected attempts must fail before mutating any invoice state.

## Relationships

- One invoice has zero or more approval attempts over time.
- One invoice may have a first approver and second approver for high-value approvals.
- One role may be assigned to multiple users.
- A user may hold different roles across different contexts, but the same invoice cannot include the same user twice in the approval flow.
