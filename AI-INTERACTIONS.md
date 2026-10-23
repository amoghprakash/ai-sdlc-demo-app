# AI Interaction Log

This file records AI-assisted work completed for the AI in SDLC assignment.

## 2026-10-21 - Context and specification setup

### Interaction 1 - Repository understanding
Tool/Agent: ChatGPT
Purpose: Understand the assignment, inspect the existing repository structure, identify the current approval implementation, existing tests, authentication roles, and required deliverables.

Human review:
- Confirmed the existing application uses Approver and Admin roles.
- Confirmed the feature requires introducing Manager and Director behavior.
- Confirmed baseline unit tests passed before implementation.

### Interaction 2 - Spec Kit initialization
Tool/Agent: GitHub Spec Kit
Purpose: Initialize specification-driven development artifacts and Copilot skills.

Result:
- Added Spec Kit infrastructure.
- Added Copilot Spec Kit skills.
- Added project constitution.

### Interaction 3 - Initial feature specification
Tool/Agent: GitHub Copilot with /speckit-specify
Purpose: Generate the approval-limits feature specification.

Human review:
The initial generated specification contained two issues:
1. It assumed Manager and Director roles already existed.
2. It treated the first approval above 500,000 as a rejection.

Decision:
Requested revision before implementation.

### Interaction 4 - Specification revision
Tool/Agent: GitHub Copilot
Purpose: Correct the specification after human review.

Result:
- Manager and Director are explicitly introduced by the feature.
- A valid first approval above 500,000 records the first approver and leaves the invoice pending.
- The second approver must be a different user and a Director.
- Invalid second approvals preserve prior approval state.
- Boundary cases 50,000 / 50,001 and 500,000 / 500,001 are explicit.

### Interaction 5 - Planning
Tool/Agent: GitHub Copilot with /speckit-plan
Purpose: Produce the implementation design from the approved specification.

Generated artifacts:
- plan.md
- research.md
- data-model.md
- contracts/invoice-approval.md
- quickstart.md

### Interaction 6 - Task breakdown
Tool/Agent: GitHub Copilot with /speckit-tasks
Purpose: Produce an actionable implementation task breakdown.

Generated artifact:
- tasks.md

## Review principle

AI output is not accepted automatically. Specifications, code, tests and review findings are checked against the assignment requirements, repository architecture and actual application behavior before acceptance.

### Interaction 7 - Single-approver implementation
Tool/Agent: GitHub Copilot using approval-implementation agent
Purpose: Implement Manager and Director single-approver limits only.

Initial review finding:
Copilot initially mapped legacy Approver to Manager and Admin to Director.

Human decision:
Rejected that implicit mapping because the specification introduces Manager and Director as explicit roles.

Correction:
- Only Manager and Director are valid approval roles.
- Manager limit is 50,000 inclusive.
- Director limit is 500,000 inclusive.
- Over-limit attempts fail before invoice mutation.
- Two-approver workflow remains intentionally unimplemented in this step.

Validation:
`dotnet test backend/InvoiceApp.sln`
Result: 10 passed, 0 failed.

### Interaction 8 - Two-approver implementation
Tool/Agent: GitHub Copilot using approval-implementation agent
Purpose: Implement the high-value two-approver workflow.

Implemented:
- First approval above 500,000 records the first approver.
- Invoice moves to PendingSecondApproval.
- First approver may be Manager or Director.
- Second approval must be by a different user.
- Second approver must be Director.
- Valid second approval completes the invoice.
- Duplicate second approver is rejected without changing prior approval data.
- Manager as second approver is rejected without changing prior approval data.

Human review:
- Removed duplicate same-user validation.
- Removed misleading/dead Manager high-value validation branch.
- Strengthened negative tests to verify decision state remains unchanged.

Validation:
`dotnet test backend/InvoiceApp.sln`
Result: 13 passed, 0 failed.

### Interaction 9 - Playwright end-to-end coverage
Tool/Agent: GitHub Copilot
Purpose: Add the required rejected over-limit approval test through the UI.

Implemented:
- Manager signs in through the UI.
- Creates and submits an invoice for 50,001.
- Attempts approval.
- Verifies the clear Manager-limit rejection message.
- Reloads the application.
- Verifies the invoice remains Pending Approval with the original amount.

Environment issue:
Playwright's bundled Chromium download repeatedly timed out.

Resolution:
Configured Playwright to use the locally installed Google Chrome browser.

Validation:
`npm test`
Result: 1 Playwright test passed using Chrome.

### Interaction 10 - Mutation testing and test strengthening
Tool/Agent: Stryker.NET + GitHub Copilot approval-review agent
Purpose: Measure test effectiveness and strengthen approval-rule tests.

Baseline:
- Unit tests: 13
- Overall mutation score: 52.83%
- ApprovalService mutation score: 64.37%
- ApprovalService covered-code score: 75.68%
- Killed: 56
- Survived: 20
- No coverage: 30

Action:
Added six focused unit tests targeting approval boundaries, role validation,
high-value first approval, duplicate approvers, Director-only second approval,
status transitions, and unchanged-state guarantees.

After improvement:
- Unit tests: 19
- Overall mutation score: 57.55%
- ApprovalService mutation score: 70.11%
- ApprovalService covered-code score: 78.21%
- Killed: 61
- Survived: 19
- No coverage: 26

Human review:
Production behavior was not weakened or altered to improve the mutation score.
Tests were strengthened based on meaningful surviving mutants in ApprovalService.

### Interaction 11 - Final pull request review
Tool/Agent: GitHub Copilot using approval-review agent
Purpose: Review the complete diff from main to feature/approval-limits.

Finding 1:
Severity: Medium
Issue: PendingSecondApproval invoices were not available in the UI approval queue, so the second approval could not be completed through the application.
Verdict: Fixed
Action:
- Updated the UI to surface PendingSecondApproval invoices.
- Added a Playwright test for the complete high-value two-step approval flow.

Finding 2:
Severity: Medium
Issue: The generated data model documented PendingSecondApproval -> Rejected, but that transition was not part of the assignment scope or implemented behavior.
Verdict: Fixed
Action:
- Removed the unsupported transition from the generated data-model documentation.

Finding 3:
Severity: Medium
Issue: The generated API contract documented HTTP 400 for invalid approval attempts, while the existing shared controller convention returns HTTP 409 for InvalidOperationException.
Verdict: Fixed
Action:
- Updated the API contract documentation to HTTP 409.
- Preserved the existing shared controller behavior.

Validation:
`dotnet test backend/InvoiceApp.sln`
Result: 19 passed, 0 failed.

`npm test`
Result: 2 Playwright tests passed.
