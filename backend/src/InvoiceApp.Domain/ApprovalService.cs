using System.Globalization;

namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// High-value invoices require a first approval plus a second, different Director approval.
/// Lower-value invoices follow the single-approver role rules and limits.
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
        EnsureApproverIsNamed(approver);

        if (invoice.Amount > DirectorApprovalLimit)
        {
            // Validate everything first; nothing on the invoice changes if this throws.
            var effectiveRole = ValidateHighValueApproval(invoice, approver, role);
            RecordHighValueApproval(invoice, approver, effectiveRole);
            _repository.Update(invoice);
            return invoice;
        }

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

    private static void EnsureApproverIsNamed(string? approver)
    {
        if (string.IsNullOrWhiteSpace(approver))
        {
            throw new ApprovalRuleViolationException("An approver username is required to approve an invoice.");
        }
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
        var effectiveRole = RequireApprovalRole(role);
        var limit = effectiveRole == DirectorRole ? DirectorApprovalLimit : ManagerApprovalLimit;

        if (invoice.Amount > limit)
        {
            throw new ApprovalRuleViolationException(
                $"{effectiveRole} '{approver}' cannot approve invoice {invoice.Id}: " +
                $"amount {FormatAmount(invoice.Amount)} exceeds the {effectiveRole} limit of {FormatAmount(limit)}.");
        }
    }

    // Returns the effective role (Manager or Director) of a valid approval attempt.
    private static string ValidateHighValueApproval(Invoice invoice, string approver, string role)
    {
        var effectiveRole = RequireApprovalRole(role);

        if (invoice.Approvers.Count == 0)
        {
            // First approval: a Manager or a Director is accepted.
            return effectiveRole;
        }

        // Stored usernames are already trimmed, so only the incoming name needs trimming.
        var normalizedApprover = approver.Trim();
        var sameUserAlreadyApproved = invoice.Approvers.Any(existing =>
            string.Equals(existing.Username, normalizedApprover, StringComparison.OrdinalIgnoreCase));

        if (sameUserAlreadyApproved)
        {
            throw new ApprovalRuleViolationException(
                $"User '{normalizedApprover}' cannot approve invoice {invoice.Id} because the same user already approved it.");
        }

        if (effectiveRole != DirectorRole)
        {
            throw new ApprovalRuleViolationException(
                $"Invoice {invoice.Id} requires a second approver from a different Director.");
        }

        return effectiveRole;
    }

    private static void RecordHighValueApproval(Invoice invoice, string approver, string effectiveRole)
    {
        var username = approver.Trim();
        invoice.Approvers.Add(new InvoiceApprover(username, effectiveRole));

        if (invoice.Approvers.Count < 2)
        {
            // First approval: the invoice stays PendingApproval.
            return;
        }

        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = username;
        invoice.DecidedAt = DateTime.UtcNow;
    }

    private static string RequireApprovalRole(string role) =>
        ResolveEffectiveRole(role)
        ?? throw new ApprovalRuleViolationException(
            $"Role '{role}' cannot approve invoices. Only Manager and Director may approve.");

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

    // Plain number with comma grouping, no currency symbol, same output on every machine.
    private static string FormatAmount(decimal amount) =>
        amount.ToString("#,##0.##", CultureInfo.InvariantCulture);
}