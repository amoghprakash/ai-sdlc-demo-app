# Invoice Approval Demo Constitution

## Core Principles

### I. Business Rules Stay in the Domain
Invoice lifecycle and approval rules must be implemented in
`backend/src/InvoiceApp.Domain`.

Controllers may collect authenticated user information and map HTTP requests,
but they must not contain invoice approval business rules.

The API and UI must consume the domain behaviour rather than duplicate it.

### II. Rejected Actions Must Not Change State
Any invalid approval attempt must fail with a clear reason and leave the
invoice unchanged.

This includes, but is not limited to:
- Manager attempting to approve above the Manager limit.
- A single approver attempting to complete an invoice above 500,000.
- The same user attempting to count as both approvers.
- A non-Director attempting to act as the second approver.

Tests must verify both the returned error and the unchanged invoice state.

### III. Approval Boundaries Must Be Explicitly Tested
Approval-limit tests must cover values immediately below, exactly at, and
immediately above important boundaries.

Required business boundaries:
- Manager: up to and including 50,000.
- Director: up to and including 500,000.
- Above 500,000: two distinct approvers are required and the second approver
  must be a Director.

Tests must be capable of detecting an incorrect comparison such as `<`
instead of `<=`.

### IV. Implement Incrementally
The feature must not be implemented as one large change.

Implementation is divided into independently testable stages:

1. Context and specification.
2. Single-approver Manager and Director limits with unit tests.
3. Two-approver workflow with negative tests.
4. End-to-end coverage and review improvements.

Each implementation stage must leave the project in a working state and have
a meaningful Git commit describing what changed and why.

### V. AI Output Requires Human Review
AI-generated code, specifications, tests and review findings are suggestions,
not automatically accepted changes.

Every generated test must be reviewed for whether it can detect a real
business-rule defect.

Every pull-request review finding must receive one of these verdicts:
- Fixed.
- Dismissed with a reason.
- Deferred with a reason.

AI interactions relevant to the assignment must be recorded in the project
interaction log.

## Technical and Security Constraints

- Runtime: .NET 8.
- API: ASP.NET Core.
- Unit tests: xUnit.
- End-to-end tests: Playwright.
- Business rules belong in `InvoiceApp.Domain`.
- Storage remains the existing in-memory repository unless the feature
  specifically requires otherwise.
- Do not add a database or unnecessary infrastructure.
- Do not commit real passwords, tokens, customer information or other
  sensitive information.
- Demo credentials already supplied by the repository may remain local demo
  data.
- Prefer the simplest implementation that correctly satisfies the specified
  business rules.

## Development and Quality Workflow

Before implementation:
1. Maintain `AGENTS.md` with the business rules, project layering, test
   commands and review expectations.
2. Produce the feature specification using Spec Kit.
3. Produce an implementation plan and task breakdown.
4. Review the generated specification before coding.

During implementation:
1. Implement single-approver limits before the two-approver workflow.
2. Add or update unit tests with each business-rule change.
3. Run `dotnet test backend/InvoiceApp.sln` before every feature commit.
4. Keep rejected operations atomic: validate before mutating invoice state.

Before completion:
1. Add at least one Playwright end-to-end test for a rejected over-limit
   approval through the UI.
2. Run mutation testing and record mutation scores before and after test
   improvements.
3. Review the final pull request with an AI agent.
4. Record every review finding and its verdict.
5. Verify all required assignment evidence is referenced from `ASSIGNMENT.md`.

## Governance

This constitution guides specification, implementation and review for the
invoice approval limits feature.

Changes that conflict with these principles must be justified explicitly in
the feature plan or pull-request review.

The existing repository conventions in `AGENTS.md` remain applicable unless
the feature specification deliberately changes them.

**Version**: 1.0.0 | **Ratified**: 2026-10-20 | **Last Amended**: 2026-10-20
