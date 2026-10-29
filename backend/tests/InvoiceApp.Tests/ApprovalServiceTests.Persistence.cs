using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

// Added after the mutation run: accepted decisions are saved, rejected ones are not,
// and error messages say what went wrong.
public partial class ApprovalServiceTests
{
    private static (ApprovalService Service, FakeInvoiceRepository Repository) ServiceWithRepositoryFor(params Invoice[] invoices)
    {
        var repository = new FakeInvoiceRepository(invoices);
        return (new ApprovalService(repository), repository);
    }

    [Fact]
    public void Submit_InvoiceThatIsNotADraft_ExplainsWhyAndSavesNothing()
    {
        var (service, repository) = ServiceWithRepositoryFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        var ex = Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));

        Assert.Contains("INV-1", ex.Message);
        Assert.Contains("only drafts can be submitted", ex.Message);
        Assert.Empty(repository.UpdatedInvoiceIds);
    }

    [Fact]
    public void Submit_Draft_SavesTheInvoiceOnce()
    {
        var (service, repository) = ServiceWithRepositoryFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        service.Submit("INV-1");

        Assert.Equal(new[] { "INV-1" }, repository.UpdatedInvoiceIds);
    }

    [Fact]
    public void Approve_WithinLimit_SavesTheInvoiceOnce()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, ApprovalService.ManagerApprovalLimit);
        var (service, repository) = ServiceWithRepositoryFor(invoice);

        service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(new[] { "INV-1" }, repository.UpdatedInvoiceIds);
    }

    [Fact]
    public void Approve_HighValueInvoice_SavesAfterEachAcceptedApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var (service, repository) = ServiceWithRepositoryFor(invoice);

        service.Approve("INV-1", "demo.manager", "Manager");
        Assert.Single(repository.UpdatedInvoiceIds);

        service.Approve("INV-1", "demo.director", "Director");
        Assert.Equal(2, repository.UpdatedInvoiceIds.Count);
    }

    [Fact]
    public void Approve_RejectedHighValueAttempts_SaveNothing()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var (service, repository) = ServiceWithRepositoryFor(invoice);
        service.Approve("INV-1", "demo.manager", "Manager");
        repository.UpdatedInvoiceIds.Clear();

        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.manager", "Manager"));
        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.manager2", "Manager"));
        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.admin", "Admin"));

        Assert.Empty(repository.UpdatedInvoiceIds);
    }

    [Theory]
    [InlineData("Manager", 50_001)]
    [InlineData("Admin", 100)]
    public void Approve_RejectedSingleApproverAttempt_SavesNothing(string role, int amount)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, amount);
        var (service, repository) = ServiceWithRepositoryFor(invoice);

        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.user", role));

        Assert.Empty(repository.UpdatedInvoiceIds);
    }

    [Fact]
    public void Reject_PendingInvoice_SavesTheInvoiceOnce()
    {
        var (service, repository) = ServiceWithRepositoryFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        service.Reject("INV-1", "demo.manager", "Duplicate invoice");

        Assert.Equal(new[] { "INV-1" }, repository.UpdatedInvoiceIds);
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Approved)]
    [InlineData(InvoiceStatus.Rejected)]
    public void Reject_InvoiceThatIsNotPending_ThrowsAndLeavesInvoiceUnchanged(InvoiceStatus status)
    {
        var invoice = InvoiceWith("INV-1", status);
        var (service, repository) = ServiceWithRepositoryFor(invoice);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Reject("INV-1", "demo.manager", "Duplicate invoice"));

        Assert.Contains("only pending invoices can be decided", ex.Message);
        Assert.Equal(status, invoice.Status);
        Assert.Null(invoice.DecisionNote);
        Assert.Null(invoice.DecidedBy);
        Assert.Empty(repository.UpdatedInvoiceIds);
    }

    [Fact]
    public void Reject_WithoutReason_ExplainsAReasonIsRequiredAndSavesNothing()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        var (service, repository) = ServiceWithRepositoryFor(invoice);

        var ex = Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.manager", "  "));

        Assert.Contains("reason is required", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Empty(repository.UpdatedInvoiceIds);
    }

    [Fact]
    public void Approve_UnknownInvoice_ExplainsWhichInvoiceWasNotFound()
    {
        var service = ServiceFor();

        var ex = Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.manager", "Manager"));

        Assert.Contains("INV-404", ex.Message);
        Assert.Contains("was not found", ex.Message);
    }

    [Fact]
    public void Approve_ManagerAboveLimit_MessageNamesTheApproverAndTheInvoice()
    {
        var invoice = InvoiceWith("INV-7", InvoiceStatus.PendingApproval, 50_001m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-7", "demo.manager", "Manager"));

        Assert.Contains("demo.manager", ex.Message);
        Assert.Contains("INV-7", ex.Message);
        Assert.Contains("50,001", ex.Message);
        Assert.Contains("50,000", ex.Message);
    }
}
