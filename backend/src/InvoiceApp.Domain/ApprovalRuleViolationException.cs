namespace InvoiceApp.Domain;

public class ApprovalRuleViolationException : InvalidOperationException
{
    public ApprovalRuleViolationException(string message)
        : base(message)
    {
    }
}
