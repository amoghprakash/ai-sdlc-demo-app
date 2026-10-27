# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only. Approval actions use action-level roles; Manager and Director may approve, while rejection remains Approver or Admin only.
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chrome && npm test`
- Playwright is configured with `channel: 'chrome'` and requires Google Chrome; installing bundled Chromium with `npx playwright install chromium` is not sufficient.

## Demo accounts (not real credentials)

- `demo.approver` / `ChangeMe123!` (role Approver)
- `demo.admin` / `ChangeMe123!` (role Admin)

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.

## Approval limits feature

Business rules:

- Manager may approve invoices up to and including 50,000.
- Director may approve invoices up to and including 500,000.
- Invoices above 500,000 require two distinct approvers.
- The first approver above 500,000 may be a Manager or Director.
- The second approver must be a Director.
- The same user must never count as both approvers.
- A valid first approval above 500,000 records the first approver and keeps the invoice pending.
- An invalid approval attempt must return a clear reason.
- Invalid attempts must not change invoice state or previously recorded approval data.

Review expectations:

- Check the exact boundaries 50,000 / 50,001 and 500,000 / 500,001.
- Verify rejected attempts are atomic.
- Verify duplicate-user and non-Director second approvals are tested.
- Review generated tests for whether they would catch a real defect.
- Keep implementation changes small and independently testable.
- Run `dotnet test backend/InvoiceApp.sln` before committing.
- Use Playwright for the required rejected over-limit UI test.
- Record AI-assisted decisions and review findings in the assignment log.

Spec Kit artifacts for this feature are under:
`specs/001-approval-limits/`
