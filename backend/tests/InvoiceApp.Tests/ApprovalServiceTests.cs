using InvoiceApp.Domain;
using InvoiceApp.Api;
using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
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

    [Theory]
    [InlineData(49_999)]
    [InlineData(50_000)]
    public void Approve_SetsStatusApproverAndTimestamp_ForManagerAtOrBelowLimit(int amount)
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, amount));

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

        Assert.Equal("Manager approvals are limited to invoices up to 50,000.", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Theory]
    [InlineData("Approver", "Unsupported approver role")]
    [InlineData("Admin", "Unsupported approver role")]
    [InlineData("", "An approver role is required")]
    [InlineData("   ", "An approver role is required")]
    public void Approve_RejectsInvalidRole_WithoutMutatingInvoice(string role, string expectedMessage)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.user", role));

        Assert.Contains(expectedMessage, ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.FirstApprover);
        Assert.Null(invoice.SecondApprover);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_RecordsFirstApproverAndCompletesInvoice_WhenSecondApprovalIsValid()
    {
        var invoice = InvoiceWith("INV-2", InvoiceStatus.PendingApproval, 600_000m);
        var service = ServiceFor(invoice);

        var firstApproval = service.Approve("INV-2", "demo.manager", " mAnAgEr ");
        Assert.Equal(InvoiceStatus.PendingSecondApproval, firstApproval.Status);
        Assert.Equal("demo.manager", firstApproval.FirstApprover);

        var secondApproval = service.Approve("INV-2", "demo.director", "director");

        Assert.Equal(InvoiceStatus.Approved, secondApproval.Status);
        Assert.Equal("demo.manager", secondApproval.FirstApprover);
        Assert.Equal("demo.director", secondApproval.SecondApprover);
        Assert.Equal("demo.director", secondApproval.DecidedBy);
        Assert.NotNull(secondApproval.DecidedAt);
    }

    [Fact]
    public void Approve_RejectsDuplicateSecondApprover_WithoutChangingPriorApprovalData()
    {
        var invoice = InvoiceWith("INV-3", InvoiceStatus.PendingApproval, 600_000m);
        var service = ServiceFor(invoice);

        service.Approve("INV-3", "demo.manager", "Manager");

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-3", "DEMO.MANAGER", "Director"));

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

        Assert.Equal("The second approver must be a Director.", ex.Message);
        Assert.Equal(InvoiceStatus.PendingSecondApproval, invoice.Status);
        Assert.Equal("demo.director", invoice.FirstApprover);
        Assert.Null(invoice.SecondApprover);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Theory]
    [InlineData(499_999)]
    [InlineData(500_000)]
    public void Approve_SetsStatusApproverAndTimestamp_ForDirectorAtOrBelowLimit(int amount)
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, amount));

        var result = service.Approve("INV-1", "demo.director", "DIRECTOR");

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

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.manager", "Manager"));

        Assert.Equal("Invoice INV-1 is Draft; only pending invoices can be decided.", ex.Message);
    }

    [Fact]
    public async Task Approve_OnlyOneConcurrentFirstApproverCanTransitionHighValueInvoice()
    {
        using var repository = new SynchronizedReadInvoiceRepository(new InMemoryInvoiceRepository(), 2);
        repository.Add(InvoiceWith("INV-CONCURRENT", InvoiceStatus.PendingApproval, 600_000m));
        var service = new ApprovalService(repository);

        var attempts = await Task.WhenAll(
            TryApproveAsync(service, "demo.manager", "Manager"),
            TryApproveAsync(service, "demo.director", "Director"));

        var success = Assert.Single(attempts, attempt => attempt.Invoice is not null);
        var failure = Assert.Single(attempts, attempt => attempt.Error is not null);
        Assert.Contains("changed while the request was being processed", failure.Error!.Message);
        Assert.NotNull(success.Invoice!.FirstApprover);
        Assert.Contains(success.Invoice.FirstApprover, new[] { "demo.manager", "demo.director" });

        var storedInvoice = repository.GetById("INV-CONCURRENT");
        Assert.Equal(InvoiceStatus.PendingSecondApproval, storedInvoice!.Status);
        Assert.Equal(success.Invoice.FirstApprover, storedInvoice.FirstApprover);
        Assert.Null(storedInvoice.SecondApprover);
        Assert.Null(storedInvoice.DecidedBy);
        Assert.Null(storedInvoice.DecidedAt);
    }

    [Fact]
    public void ApprovalRoleAuthorization_AllowsManagersAndDirectorsToApproveButNotReject()
    {
        Assert.True(IsAuthorized(nameof(ApprovalsController.Approve), "Manager"));
        Assert.True(IsAuthorized(nameof(ApprovalsController.Approve), "Director"));
        Assert.False(IsAuthorized(nameof(ApprovalsController.Reject), "Manager"));
        Assert.False(IsAuthorized(nameof(ApprovalsController.Reject), "Director"));
        Assert.True(IsAuthorized(nameof(ApprovalsController.Reject), "Approver"));
        Assert.True(IsAuthorized(nameof(ApprovalsController.Reject), "Admin"));
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

    private static async Task<(Invoice? Invoice, InvalidOperationException? Error)> TryApproveAsync(
        ApprovalService service,
        string approver,
        string role) =>
        await Task.Run(() =>
        {
            try
            {
                return (service.Approve("INV-CONCURRENT", approver, role), (InvalidOperationException?)null);
            }
            catch (InvalidOperationException error)
            {
                return ((Invoice?)null, error);
            }
        });

    private static bool IsAuthorized(string actionName, string role)
    {
        var method = typeof(ApprovalsController).GetMethod(actionName)!;
        var authorization = method.GetCustomAttributes(typeof(RequireRoleAttribute), inherit: true)
            .Cast<RequireRoleAttribute>()
            .Single();
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession("demo.user", role);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var filterContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new ApprovalsController(ServiceFor()));

        authorization.OnActionExecuting(filterContext);
        return filterContext.Result is null;
    }

    private sealed class SynchronizedReadInvoiceRepository : IInvoiceRepository, IDisposable
    {
        private readonly InMemoryInvoiceRepository _repository;
        private readonly Barrier _readBarrier;
        private readonly int _synchronizedReadCount;
        private int _readCount;

        public SynchronizedReadInvoiceRepository(InMemoryInvoiceRepository repository, int synchronizedReadCount)
        {
            _repository = repository;
            _synchronizedReadCount = synchronizedReadCount;
            _readBarrier = new Barrier(synchronizedReadCount);
        }

        public IReadOnlyList<Invoice> GetAll() => _repository.GetAll();

        public Invoice? GetById(string id)
        {
            var invoice = _repository.GetById(id);
            if (Interlocked.Increment(ref _readCount) <= _synchronizedReadCount &&
                !_readBarrier.SignalAndWait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Concurrent approval requests did not reach the read barrier.");
            }

            return invoice;
        }

        public IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids) => _repository.GetByIds(ids);
        public Invoice Add(Invoice invoice) => _repository.Add(invoice);
        public void Update(Invoice invoice) => _repository.Update(invoice);
        public Invoice? TryUpdate(string id, InvoiceStatus expectedStatus, Action<Invoice> update) =>
            _repository.TryUpdate(id, expectedStatus, update);
        public bool Remove(string id) => _repository.Remove(id);
        public void Dispose() => _readBarrier.Dispose();
    }
}
