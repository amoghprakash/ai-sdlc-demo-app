# Tasks: Approval Limits by Role

**Input**: Design documents from `/specs/001-approval-limits/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the existing architecture and prepare the approval flow changes without altering the project structure.

- [ ] T001 Review the current role/session model in `backend/src/InvoiceApp.Api/Auth/` and confirm how to extend it with Manager and Director demo identities while preserving existing Approver/Admin behavior.
- [ ] T002 [P] Confirm the current invoice lifecycle in `backend/src/InvoiceApp.Domain/Invoice.cs` and `backend/src/InvoiceApp.Domain/InvoiceStatus.cs` and identify the state additions needed for a pending second-approval state above 500,000.
- [ ] T003 [P] Review the current approval endpoint contracts in `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs` and the repository contract in `backend/src/InvoiceApp.Domain/IInvoiceRepository.cs` before implementing the approval policy changes.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the domain and validation primitives required before any story-specific work begins.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 Define the approval-role validation model in `backend/src/InvoiceApp.Domain/ApprovalService.cs` so the service can evaluate role, amount, and approval sequencing before mutating invoice state.
- [ ] T005 [P] Extend the invoice status model in `backend/src/InvoiceApp.Domain/InvoiceStatus.cs` to represent pending second approval and preserve the existing Draft, PendingApproval, Approved, and Rejected states.
- [ ] T006 [P] Add the domain fields required for first/second approver tracking in `backend/src/InvoiceApp.Domain/Invoice.cs` while keeping the record consistent with the existing invoice lifecycle.
- [ ] T007 Ensure the approval service fails atomically for invalid attempts in `backend/src/InvoiceApp.Domain/ApprovalService.cs` by validating all rules before any invoice mutation occurs.
- [ ] T008 Review the API auth setup in `backend/src/InvoiceApp.Api/Program.cs` and `backend/src/InvoiceApp.Api/Auth/SessionAuthMiddleware.cs` to ensure Manager/Director users can be authenticated without changing the repo’s architecture.

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel.

---

## Phase 3: User Story 1 - Manager approvals within policy (Priority: P1) 🎯 MVP

**Goal**: Enforce the Manager rule at 50,000 and reject over-limit Manager approvals without mutating invoice state.

**Independent Test**: A Manager can approve 45,000 and 50,000, while 50,001 is rejected with a clear reason and the invoice remains unchanged.

### Implementation for User Story 1

- [ ] T009 [P] [US1] Add boundary tests for 45,000, 50,000, and 50,001 in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` to prove the Manager threshold and rejection behavior.
- [ ] T010 [US1] Implement Manager limit validation in `backend/src/InvoiceApp.Domain/ApprovalService.cs` so the rule is enforced as `amount <= 50,000` and invalid attempts return a clear reason.
- [ ] T011 [US1] Confirm atomicity for rejected Manager approvals in `backend/src/InvoiceApp.Domain/ApprovalService.cs` using the repository state preservation pattern already established in `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`.
- [ ] T012 [US1] Verify the API response semantics from `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs` for invalid Manager approvals and ensure controllers still delegate business rules to the domain service.

**Checkpoint**: User Story 1 should be fully functional and independently testable.

---

## Phase 4: User Story 2 - Director approvals within the higher threshold (Priority: P1)

**Goal**: Enforce the Director rule up to 500,000 and preserve the escalation path above that amount.

**Independent Test**: A Director can approve 250,000 and 500,000, while a Director’s first approval at 500,001 is recorded and the invoice enters a pending second-approval state.

### Implementation for User Story 2

- [ ] T013 [P] [US2] Add boundary tests for 250,000, 500,000, and 500,001 in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` to prove the Director threshold and the high-value pending state.
- [ ] T014 [US2] Implement Director validation in `backend/src/InvoiceApp.Domain/ApprovalService.cs` so `amount <= 500,000` is allowed and the service records a first approver when the amount exceeds 500,000.
- [ ] T015 [US2] Add the pending second-approval flow in `backend/src/InvoiceApp.Domain/Invoice.cs` and `backend/src/InvoiceApp.Domain/InvoiceStatus.cs` so an invoice above 500,000 does not reject the valid first approval.
- [ ] T016 [US2] Confirm the error message and state preservation for invalid Director attempts in `backend/src/InvoiceApp.Domain/ApprovalService.cs` and `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`.

**Checkpoint**: User Stories 1 and 2 should both work independently and remain atomic.

---

## Phase 5: User Story 3 - Two-step approval for high-value invoices (Priority: P1)

**Goal**: Implement the required second-approval path for invoices above 500,000 and block invalid duplicate or role-based attempts.

**Independent Test**: A Manager or Director first approval above 500,000 is recorded and pending, but a second approval only succeeds when it is by a different Director and completes the invoice approval; duplicate users and Manager second approvals are rejected without changing state.

### Implementation for User Story 3

- [ ] T017 [P] [US3] Add tests for valid two-step approval, duplicate-user rejection, and Manager second-approval rejection in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`.
- [ ] T018 [US3] Implement the first-approval validation in `backend/src/InvoiceApp.Domain/ApprovalService.cs` so an invoice above 500,000 records the first approver and leaves the invoice pending for a second approval.
- [ ] T019 [US3] Implement the second-approval validation in `backend/src/InvoiceApp.Domain/ApprovalService.cs` so the approver must be a different user, must be a Director, and can complete the invoice approval only when all rules pass.
- [ ] T020 [US3] Ensure invalid second approvals preserve the previously recorded first approval data in `backend/src/InvoiceApp.Domain/ApprovalService.cs` and `backend/tests/InvoiceApp.Tests/FakeInvoiceRepository.cs`.
- [ ] T021 [US3] Update the API contract assumptions in `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs` so the controller continues to return domain errors without embedding business rules.

**Checkpoint**: The high-value approval workflow should be functionally complete and independently testable.

---

## Phase 6: User Story 4 - Rejected attempts preserve state and reason (Priority: P1)

**Goal**: Guarantee that every invalid approval attempt returns a clear reason and preserves the prior invoice state.

**Independent Test**: Any invalid approval attempt, including duplicate-user or non-Director second approvals, returns a clear message and leaves the invoice unchanged.

### Implementation for User Story 4

- [ ] T022 [P] [US4] Add regression tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` covering clear rejection reasons and unchanged state for invalid approvals.
- [ ] T023 [US4] Harden the validation flow in `backend/src/InvoiceApp.Domain/ApprovalService.cs` to fail before mutation and keep the previous invoice values and recorded approval data intact.
- [ ] T024 [US4] Confirm the HTTP error contract in `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs` surfaces user-friendly reasons without exposing internal implementation details.

**Checkpoint**: All approval-rule stories should be independently functional and safe against invalid state transitions.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, demo quality, and verification of cross-cutting rule coverage.

- [ ] T025 [P] Add or update an end-to-end rejection test under `e2e/tests/` for a high-value invoice that attempts an invalid second approval and confirms the invoice remains unchanged.
- [ ] T026 [P] Add or update a Playwright page-object or test helper under `e2e/pages/` if the UI must explicitly exercise the role-based approval scenario.
- [ ] T027 Run `dotnet test backend/InvoiceApp.sln` and verify the boundary and state-preservation tests pass for the approval-limit feature.
- [ ] T028 Review the final rule coverage against the spec in `specs/001-approval-limits/spec.md` and confirm all required boundaries and rejection cases are present before closing out the feature.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user-story work.
- **User Stories (Phase 3-6)**: Depend on Foundational completion; they can proceed in priority order or in parallel if multiple contributors are available.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **User Story 1 (US1)**: No dependency on other stories; can start after the foundational phase.
- **User Story 2 (US2)**: No dependency on other stories; can start once the foundational rules are in place.
- **User Story 3 (US3)**: Depends on the first-approval and role-validation primitives created in earlier phases.
- **User Story 4 (US4)**: Depends on the core approval-validation flow and atomicity checks.

### Parallel Opportunities

- `T002` and `T003` in Setup can run in parallel.
- `T005` and `T006` in Foundational can run in parallel.
- `T009`, `T013`, and `T017` are independent story test additions and can be prepared in parallel once the foundational phase is complete.
- `T025` and `T026` in the final validation phase can be developed and run in parallel.

---

## Parallel Example: User Story 3

```bash
# Run these validations together once the foundational approval rules are in place:
Task: "Add tests for valid two-step approval, duplicate-user rejection, and Manager second-approval rejection in backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs"
Task: "Implement the first-approval validation in backend/src/InvoiceApp.Domain/ApprovalService.cs"
Task: "Implement the second-approval validation in backend/src/InvoiceApp.Domain/ApprovalService.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Validate the Manager boundary tests independently.
5. Stop and verify before expanding to the higher-value workflow.

### Incremental Delivery

1. Complete Setup + Foundational.
2. Add US1 Manager policy.
3. Add US2 Director policy and high-value first-approval state.
4. Add US3 second-approval flow and duplicate-user checks.
5. Add US4 atomic rejection testing and final validation.

### Parallel Team Strategy

With multiple contributors:

1. Team completes Setup + Foundational together.
2. Developer A focuses on User Story 1.
3. Developer B focuses on User Story 2.
4. Developer C focuses on User Story 3 and User Story 4.
5. Final validation and end-to-end coverage are completed together.

---

## Notes

- [P] tasks indicate different files or independent work items.
- [Story] labels map tasks to the relevant user story for traceability.
- Each story remains independently testable before the next story is added.
- The final pass should confirm the exact boundaries at 50,000, 50,001, 500,000, and 500,001 are explicitly covered.
- Invalid approval attempts must leave the invoice state and previously recorded approval data intact.
