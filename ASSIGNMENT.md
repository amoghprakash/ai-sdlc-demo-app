# AI in SDLC Assignment

## Feature Summary

Implemented approval limits in the .NET 8 invoice approval application:

- Managers may approve invoices up to and including 50,000.
- Directors may approve invoices up to and including 500,000.
- Invoices above 500,000 require two distinct approvers. The first may be a Manager or Director; the second must be a Director.
- The same user cannot count as both approvers.
- Invalid approval attempts return a clear reason and leave invoice state and previously recorded approval data unchanged.
- Business rules remain in `backend/src/InvoiceApp.Domain`; the API passes the authenticated role to the service.

## SDLC Context and Artifacts

- `AGENTS.md` defines the .NET 8 project layout, domain/API separation, test expectations, role-based Playwright locator conventions, and approval-limit review expectations.
- Spec Kit artifacts used: `specs/001-approval-limits/spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/invoice-approval.md`, `quickstart.md`, `tasks.md`, and `checklists/requirements.md`.
- Custom agents: `approval-implementation` and `approval-review` under `.github/agents/`.
- Custom skills: `speckit-analyze`, `speckit-checklist`, `speckit-clarify`, `speckit-constitution`, `speckit-converge`, `speckit-implement`, `speckit-plan`, `speckit-specify`, `speckit-tasks`, and `speckit-taskstoissues` under `.github/skills/`.
- Hook: `.github/hooks/assignment-hooks.json` defines a `sessionStart` command that prints `AI SDLC agent session started`.
- AI-assisted decisions, human review, test results, mutation testing, and review findings are recorded in `AI-INTERACTIONS.md`.

## Validation Results

- Unit tests: `dotnet test backend/InvoiceApp.sln` - 21 passed, 0 failed. Coverage includes atomic concurrent first approval and action-level approval/rejection authorization.
- Playwright: `npm test` - 3 passed. Coverage includes over-limit rejection, successful two-step approval, and rejected duplicate second approval with first-approval state preserved.
- Mutation testing before test strengthening: overall 52.83%; `ApprovalService` 64.37% (covered-code score 75.68%).
- Mutation testing after test strengthening: overall 57.55%; `ApprovalService` 70.11% (covered-code score 78.21%).

## Review Findings

1. `PendingSecondApproval` was missing from the UI approval queue - fixed; the queue now supports the second approval and has a complete two-approver Playwright test.
2. The data model documented an unsupported `PendingSecondApproval -> Rejected` transition - fixed by removing that transition from the documentation.
3. The API contract documented HTTP 400 for invalid approvals while the shared handler returns HTTP 409 - fixed by aligning the contract to 409.
4. Concurrent first approvals could overwrite `FirstApprover` - fixed with atomic expected-status repository transitions and a concurrency test.
5. Manager and Director could use Reject after approval-role access was added - fixed with action-level role restrictions and an authorization test.
6. The E2E setup instructions installed Chromium although Playwright selects Chrome - fixed by documenting the Google Chrome requirement and matching install command.
7. The draft-state test used an unsupported role - fixed by using Manager and asserting the draft-specific message.
8. The service summary described only a single approver - fixed to document both approval workflows.

## Feature Branch Commit History

Branch: `feature/approval-limits`

- `6d4bcd9` - docs: establish approval limits context and specification
- `fa377b0` - feat: enforce single-approver limits by role
- `835d20f` - feat: add two-approver workflow for high-value invoices
- `a9d7882` - test: add rejected over-limit Playwright coverage
- `5dfab29` - test: strengthen approval rules with mutation testing
- `1d9e132` - fix: address final approval workflow review findings

## Demo Video

VIDEO_LINK_TODO
