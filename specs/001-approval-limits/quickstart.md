# Quickstart: Approval Limits by Role

## Prerequisites

- .NET 8 SDK installed
- Repository cloned locally
- Demo application started with `dotnet run --project backend/src/InvoiceApp.Api`

## Validation scenarios

### 1. Manager boundary below and at 50,000

1. Create or open an invoice with amount 45,000.
2. Submit approval as a Manager.
3. Verify the invoice is approved.
4. Create or open an invoice with amount 50,000.
5. Submit approval as a Manager.
6. Verify the invoice is approved.
7. Create or open an invoice with amount 50,001.
8. Submit approval as a Manager.
9. Verify the response contains a clear rejection reason and the invoice remains unchanged.

### 2. Director boundary below and at 500,000

1. Create or open an invoice with amount 250,000.
2. Submit approval as a Director.
3. Verify the invoice is approved.
4. Create or open an invoice with amount 500,000.
5. Submit approval as a Director.
6. Verify the invoice is approved.

### 3. High-value invoice two-step approval

1. Create or open an invoice with amount 600,000.
2. Submit the first approval as a Manager or Director.
3. Verify the invoice is left pending for second approval instead of being rejected.
4. Submit a second approval by a different user who is a Director.
5. Verify the invoice is approved after the second approval.

### 4. Invalid high-value attempts must not mutate state

1. Create or open an invoice with amount 600,000.
2. Submit a first approval and confirm the invoice is pending second approval.
3. Try to reuse the same first approver as the second approver.
4. Verify the attempt fails with a clear reason and the existing approval data is unchanged.
5. Try a second approval from a Manager.
6. Verify the attempt fails with a clear reason and no state changes occur.

## Expected outcomes

- 50,000 and 500,000 are accepted as exact boundary values.
- 50,001 and 500,001 are rejected or routed to the next required approval step as defined in the feature rules.
- Rejected approvals never mutate the invoice state or previously recorded approval data.
- The API and UI both surface a clear error reason for business users.
