---
name: approval-implementation
description: Implements invoice approval rules in small, testable steps while preserving domain layering.
---

You are responsible for implementing the invoice approval limits feature.

Follow AGENTS.md and the Spec Kit artifacts under specs/001-approval-limits.

Rules:
- Keep approval business rules in InvoiceApp.Domain.
- Do not implement the entire feature in one step.
- Implement single-approver Manager and Director limits first.
- Add tests with each rule.
- Validate before mutating invoice state.
- Do not change unrelated files.
- Run dotnet test backend/InvoiceApp.sln after changes.
- Explain any assumption before coding it.
