using InvoiceApp.Domain;

namespace InvoiceApp.Tests;

internal class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly object _lock = new();
    private readonly List<Invoice> _invoices = new();

    public FakeInvoiceRepository(params Invoice[] seed) => _invoices.AddRange(seed);

    public IReadOnlyList<Invoice> GetAll()
    {
        lock (_lock) return _invoices.Select(invoice => invoice.Copy()).ToList();
    }

    public Invoice? GetById(string id)
    {
        lock (_lock) return _invoices.FirstOrDefault(candidate => candidate.Id == id)?.Copy();
    }

    public IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids)
    {
        var wanted = ids.ToHashSet();
        lock (_lock) return _invoices.Where(invoice => wanted.Contains(invoice.Id)).Select(invoice => invoice.Copy()).ToList();
    }

    public Invoice Add(Invoice invoice)
    {
        lock (_lock) _invoices.Add(invoice);
        return invoice;
    }

    public void Update(Invoice invoice)
    {
        lock (_lock)
        {
            var index = _invoices.FindIndex(candidate => candidate.Id == invoice.Id);
            if (index >= 0) _invoices[index] = invoice;
        }
    }

    public Invoice? TryUpdate(string id, InvoiceStatus expectedStatus, Action<Invoice> update)
    {
        lock (_lock)
        {
            var invoice = _invoices.FirstOrDefault(candidate => candidate.Id == id);
            if (invoice is null || invoice.Status != expectedStatus)
            {
                return null;
            }

            update(invoice);
            return invoice.Copy();
        }
    }

    public bool Remove(string id)
    {
        lock (_lock) return _invoices.RemoveAll(invoice => invoice.Id == id) > 0;
    }
}
