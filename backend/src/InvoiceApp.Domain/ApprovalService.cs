using System.Globalization;

namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// Step 1: a single approver decides, within the limit of their role.
/// </summary>
public class ApprovalService
{
    public const decimal ManagerApprovalLimit = 50_000m;
    public const decimal DirectorApprovalLimit = 500_000m;

    public const string ManagerRole = "Manager";
    public const string DirectorRole = "Director";
    public const string LegacyApproverRole = "Approver";

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

        invoice.Status = InvoiceStatus.PendingApproval;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Approve(string invoiceId, string approver, string role)
    {
        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        // Validate everything first; nothing on the invoice changes if this throws.
        ValidateApprovalRole(invoice, approver, role);

        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = reason;
        _repository.Update(invoice);
        return invoice;
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

    private static void ValidateApprovalRole(Invoice invoice, string approver, string role)
    {
        var effectiveRole = ResolveEffectiveRole(role);

        if (effectiveRole is null)
        {
            throw new ApprovalRuleViolationException(
                $"Role '{role}' cannot approve invoices. Only Manager and Director may approve.");
        }

        var limit = effectiveRole == DirectorRole ? DirectorApprovalLimit : ManagerApprovalLimit;

        if (invoice.Amount > limit)
        {
            throw new ApprovalRuleViolationException(
                $"{effectiveRole} '{approver}' cannot approve invoice {invoice.Id}: " +
                $"amount {FormatAmount(invoice.Amount)} exceeds the {effectiveRole} limit of {FormatAmount(limit)}.");
        }
    }

    // Returns Manager or Director, or null when the role has no approval authority.
    // The legacy Approver role counts as Manager. Admin and unknown roles return null.
    private static string? ResolveEffectiveRole(string? role)
    {
        var trimmed = role?.Trim();
        return trimmed switch
        {
            ManagerRole => ManagerRole,
            LegacyApproverRole => ManagerRole,
            DirectorRole => DirectorRole,
            _ => null
        };
    }

    // Plain number, no currency symbol, same output on every machine.
        private static string FormatAmount(decimal amount) =>
        amount.ToString("#,##0.##", CultureInfo.InvariantCulture);
}