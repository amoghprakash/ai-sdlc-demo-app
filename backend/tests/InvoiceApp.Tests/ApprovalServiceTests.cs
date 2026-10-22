using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status, decimal amount = 100m) =>
        new() { Id = id, CustomerName = "Test School", Amount = amount, Status = status };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp_ForManagerAtBoundary()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_000m));

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_RejectsManagerAboveLimit_WithoutMutatingInvoice()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_001m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.manager", "Manager"));

        Assert.Contains("50,000", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_RecordsFirstApproverAndCompletesInvoice_WhenSecondApprovalIsValid()
    {
        var invoice = InvoiceWith("INV-2", InvoiceStatus.PendingApproval, 600_000m);
        var service = ServiceFor(invoice);

        var firstApproval = service.Approve("INV-2", "demo.manager", "Manager");
        Assert.Equal(InvoiceStatus.PendingSecondApproval, firstApproval.Status);
        Assert.Equal("demo.manager", firstApproval.FirstApprover);

        var secondApproval = service.Approve("INV-2", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, secondApproval.Status);
        Assert.Equal("demo.manager", secondApproval.FirstApprover);
        Assert.Equal("demo.director", secondApproval.SecondApprover);
        Assert.Equal("demo.director", secondApproval.DecidedBy);
    }

    [Fact]
    public void Approve_RejectsDuplicateSecondApprover_WithoutChangingPriorApprovalData()
    {
        var invoice = InvoiceWith("INV-3", InvoiceStatus.PendingApproval, 600_000m);
        var service = ServiceFor(invoice);

        service.Approve("INV-3", "demo.manager", "Manager");

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-3", "demo.manager", "Director"));

        Assert.Contains("same user", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(InvoiceStatus.PendingSecondApproval, invoice.Status);
        Assert.Equal("demo.manager", invoice.FirstApprover);
        Assert.Null(invoice.SecondApprover);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_RejectsManagerAsSecondApprover_WithoutChangingPriorApprovalData()
    {
        var invoice = InvoiceWith("INV-4", InvoiceStatus.PendingApproval, 600_000m);
        var service = ServiceFor(invoice);

        service.Approve("INV-4", "demo.director", "Director");

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-4", "demo.manager", "Manager"));

        Assert.Contains("Director", ex.Message);
        Assert.Equal(InvoiceStatus.PendingSecondApproval, invoice.Status);
        Assert.Equal("demo.director", invoice.FirstApprover);
        Assert.Null(invoice.SecondApprover);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp_ForDirectorAtBoundary()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_000m));

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_RecordsFirstApprover_WhenDirectorApprovesInvoiceAboveLimit_AndLeavesItPending()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.PendingSecondApproval, result.Status);
        Assert.Equal("demo.director", result.FirstApprover);
        Assert.Equal(InvoiceStatus.PendingSecondApproval, invoice.Status);
        Assert.Equal("demo.director", invoice.FirstApprover);
        Assert.Null(invoice.SecondApprover);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.approver", "Approver"));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.approver", "Approver"));
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "  "));
    }
}
