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
    public void Approve_ManagerAtLimit_ApprovesInvoice()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, ApprovalService.ManagerApprovalLimit);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_ManagerAboveLimit_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, ApprovalService.ManagerApprovalLimit + 1m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.manager", "Manager"));

        Assert.Contains("Manager", ex.Message);
        Assert.Contains("50,000", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_DirectorAtLimit_ApprovesInvoice()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, ApprovalService.DirectorApprovalLimit);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_DirectorAboveHighValueThreshold_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, ApprovalService.DirectorApprovalLimit + 1m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.director", "Director"));

        Assert.Contains("500,000", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_LegacyApproverAtManagerLimit_ApprovesInvoice()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, ApprovalService.ManagerApprovalLimit);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.approver", "Approver");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.approver", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_LegacyApproverAboveManagerLimit_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, ApprovalService.ManagerApprovalLimit + 1m);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.approver", "Approver"));

        Assert.Contains("Manager", ex.Message);
        Assert.Contains("50,000", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_AdminRole_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, ApprovalService.ManagerApprovalLimit);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.admin", "Admin"));

        Assert.Contains("Admin", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_UnknownRole_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, ApprovalService.ManagerApprovalLimit);
        var service = ServiceFor(invoice);

        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.unknown", "Unknown"));

        Assert.Contains("Unknown", ex.Message);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
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
