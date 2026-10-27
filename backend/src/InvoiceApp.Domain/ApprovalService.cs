namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// Standard invoices use one approver; invoices above 500,000 require a first approval
/// followed by approval from a different Director.
/// </summary>
public class ApprovalService
{
    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        var invoice = Find(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
        }

        return ApplyTransition(invoice, InvoiceStatus.Draft,
            current => current.Status = InvoiceStatus.PendingApproval);
    }

    public Invoice Approve(string invoiceId, string approver, string role)
    {
        var invoice = Find(invoiceId);

        if (invoice.Amount > 500_000m)
        {
            return HandleHighValueApproval(invoice, approver, role);
        }

        EnsurePending(invoice);
        ValidateSingleApproverLimit(invoice.Amount, role);

        return ApplyTransition(invoice, InvoiceStatus.PendingApproval, current =>
        {
            current.Status = InvoiceStatus.Approved;
            current.DecidedBy = approver;
            current.DecidedAt = DateTime.UtcNow;
        });
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        return ApplyTransition(invoice, InvoiceStatus.PendingApproval, current =>
        {
            current.Status = InvoiceStatus.Rejected;
            current.DecidedBy = approver;
            current.DecidedAt = DateTime.UtcNow;
            current.DecisionNote = reason;
        });
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }

    private Invoice HandleHighValueApproval(Invoice invoice, string approver, string role)
    {
        if (invoice.Status == InvoiceStatus.PendingApproval)
        {
            ValidateSingleApproverLimit(invoice.Amount, role, allowHighValueFirstApproval: true);

            if (string.IsNullOrWhiteSpace(approver))
            {
                throw new InvalidOperationException("An approver is required for the first approval.");
            }

            return ApplyTransition(invoice, InvoiceStatus.PendingApproval, current =>
            {
                current.FirstApprover = approver;
                current.Status = InvoiceStatus.PendingSecondApproval;
            });
        }

        if (invoice.Status == InvoiceStatus.PendingSecondApproval)
        {
            ValidateSecondApproval(invoice, approver, role);

            return ApplyTransition(invoice, InvoiceStatus.PendingSecondApproval, current =>
            {
                current.SecondApprover = approver;
                current.Status = InvoiceStatus.Approved;
                current.DecidedBy = approver;
                current.DecidedAt = DateTime.UtcNow;
            });
        }

        throw new InvalidOperationException(
            $"Invoice {invoice.Id} is {invoice.Status}; only pending or second-approval invoices can be decided.");
    }

    private Invoice ApplyTransition(Invoice invoice, InvoiceStatus expectedStatus, Action<Invoice> update)
    {
        var updatedInvoice = _repository.TryUpdate(invoice.Id, expectedStatus, update);
        if (updatedInvoice is null)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} changed while the request was being processed; reload and try again.");
        }

        return updatedInvoice;
    }

    private static void ValidateSingleApproverLimit(decimal amount, string role, bool allowHighValueFirstApproval = false)
    {
        var normalizedRole = role?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedRole))
        {
            throw new InvalidOperationException("An approver role is required to approve an invoice.");
        }

        if (normalizedRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            if (amount > 50_000m && !allowHighValueFirstApproval)
            {
                throw new InvalidOperationException("Manager approvals are limited to invoices up to 50,000.");
            }

            return;
        }

        if (normalizedRole.Equals("Director", StringComparison.OrdinalIgnoreCase))
        {
            if (amount > 500_000m && !allowHighValueFirstApproval)
            {
                throw new InvalidOperationException("Director approvals are limited to invoices up to 500,000.");
            }

            if (amount > 500_000m)
            {
                return;
            }

            return;
        }

        throw new InvalidOperationException(
            $"Unsupported approver role '{role}'. Only Manager and Director roles are valid for approval limits.");
    }

    private static void ValidateSecondApproval(Invoice invoice, string approver, string role)
    {
        if (string.IsNullOrWhiteSpace(approver))
        {
            throw new InvalidOperationException("An approver is required for the second approval.");
        }

        if (string.Equals(approver, invoice.FirstApprover, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The same user cannot approve an invoice twice.");
        }

        if (!string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The second approver must be a Director.");
        }
    }
}
