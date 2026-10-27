namespace InvoiceApp.Domain;

public enum InvoiceStatus
{
    Draft,
    PendingApproval,
    PendingSecondApproval,
    Approved,
    Rejected
}
