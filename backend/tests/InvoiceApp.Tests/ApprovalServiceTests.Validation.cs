using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

// Added after the agent review (finding 1): an approval must name who approved it.
public partial class ApprovalServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Approve_WithoutApproverName_ThrowsAndLeavesInvoiceUnchanged(string? approver)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, ApprovalService.ManagerApprovalLimit);
        var (service, repository) = ServiceWithRepositoryFor(invoice);

        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", approver!, "Manager"));

        Assert.Contains("approver username is required", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Empty(invoice.Approvers);
        Assert.Empty(repository.UpdatedInvoiceIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Approve_HighValueInvoiceWithoutApproverName_ThrowsAndLeavesInvoiceUnchanged(string? approver)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var (service, repository) = ServiceWithRepositoryFor(invoice);

        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", approver!, "Director"));

        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Empty(invoice.Approvers);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Empty(repository.UpdatedInvoiceIds);
    }

    [Fact]
    public void Approve_HighValueSecondApprovalWithoutApproverName_ThrowsAndKeepsOnlyTheFirstApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);
        service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "  ", "Director"));

        AssertStillPendingWithOneApprover(invoice, "demo.manager");
    }
}
