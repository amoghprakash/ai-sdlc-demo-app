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
