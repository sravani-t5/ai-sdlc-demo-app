using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status, decimal amount = 100m) =>
        new() { Id = id, CustomerName = "Test School", Amount = amount, Status = status };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    private static void AssertStillPendingWithOneApprover(Invoice invoice, string firstApprover)
    {
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        var recorded = Assert.Single(invoice.Approvers);
        Assert.Equal(firstApprover, recorded.Username);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

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
    public void Approve_HighValueInvoiceWithDirectorFirst_RecordsFirstApproverAndKeepsInvoicePending()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        Assert.Single(result.Approvers);
        Assert.Equal("demo.director", result.Approvers[0].Username);
        Assert.Equal("Director", result.Approvers[0].Role);
    }

    [Fact]
    public void Approve_HighValueInvoiceWithManagerFirst_RecordsFirstApproverAndKeepsInvoicePending()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        Assert.Single(result.Approvers);
        Assert.Equal("demo.manager", result.Approvers[0].Username);
        Assert.Equal("Manager", result.Approvers[0].Role);
    }

    [Fact]
    public void Approve_HighValueInvoiceWithDirectorThenDifferentDirector_ApprovesInvoice()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.director", "Director");
        var result = service.Approve("INV-1", "demo.director2", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director2", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal(2, result.Approvers.Count);
        Assert.Equal("demo.director", result.Approvers[0].Username);
        Assert.Equal("demo.director2", result.Approvers[1].Username);
    }

    [Fact]
    public void Approve_HighValueInvoiceWithManagerThenDirector_ApprovesInvoice()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.manager", "Manager");
        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal(2, result.Approvers.Count);
    }

    [Fact]
    public void Approve_HighValueInvoiceWithDirectorThenManager_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.director", "Director");
        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.manager", "Manager"));

        Assert.Contains("second approver", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Director", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Single(invoice.Approvers);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_HighValueInvoiceWithManagerThenManager_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.manager", "Manager");
        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.manager2", "Manager"));

        Assert.Contains("Director", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Single(invoice.Approvers);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_HighValueInvoiceWithSameUsernameDifferentCasing_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "Demo.Manager", "Manager");
        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", " demo.manager ", "Director"));

        Assert.Contains("same user", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Single(invoice.Approvers);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_HighValueInvoiceSameUserAsManagerThenDirector_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.manager", "Manager");
        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.manager", "Director"));

        Assert.Contains("same user", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Single(invoice.Approvers);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_ApprovedInvoiceThirdApproval_ThrowsInvalidOperationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.manager", "Manager");
        service.Approve("INV-1", "demo.director", "Director");

        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.manager2", "Manager"));

        Assert.Contains("Approved", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
        Assert.Equal(2, invoice.Approvers.Count);
        Assert.Equal("demo.director", invoice.DecidedBy);
        Assert.NotNull(invoice.DecidedAt);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "  "));
    }

    // ---- Added after review: cases the generated tests did not cover ----

    [Fact]
    public void Approve_HighValueInvoiceSameDirectorTwice_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.director", "Director");
        var ex = Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.director", "Director"));

        Assert.Contains("same user", ex.Message, StringComparison.OrdinalIgnoreCase);
        AssertStillPendingWithOneApprover(invoice, "demo.director");
    }

    [Theory]
    [InlineData("DEMO.DIRECTOR")]
    [InlineData("Demo.Director")]
    [InlineData("  demo.director  ")]
    public void Approve_HighValueInvoiceSameDirectorWithCasingOrSpaceVariation_ThrowsAndLeavesInvoiceUnchanged(string secondUsername)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.director", "Director");
        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", secondUsername, "Director"));

        AssertStillPendingWithOneApprover(invoice, "demo.director");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Approve_HighValueInvoiceFirstApproverWithoutApprovalRole_ThrowsAndLeavesInvoiceUnchanged(string? role)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.user", role!));

        Assert.Empty(invoice.Approvers);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }

    [Fact]
    public void Approve_HighValueInvoiceDirectorThenAdmin_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.director", "Director");
        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.admin", "Admin"));

        AssertStillPendingWithOneApprover(invoice, "demo.director");
    }

    [Fact]
    public void Approve_HighValueInvoiceLegacyApproverFirst_IsRecordedAsManagerAndInvoiceStaysPending()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.approver", "Approver");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        var recorded = Assert.Single(result.Approvers);
        Assert.Equal("demo.approver", recorded.Username);
        Assert.Equal("Manager", recorded.Role);
    }

    [Fact]
    public void Approve_HighValueInvoiceDirectorThenLegacyApprover_ThrowsApprovalRuleViolationExceptionAndLeavesInvoiceUnchanged()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m);
        var service = ServiceFor(invoice);

        service.Approve("INV-1", "demo.director", "Director");
        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.approver", "Approver"));

        AssertStillPendingWithOneApprover(invoice, "demo.director");
    }

    [Theory]
    [InlineData("Manager", 50_001)]
    [InlineData("Approver", 50_001)]
    [InlineData("Admin", 50_000)]
    public void Approve_RejectedSingleApproverAttempt_DoesNotRecordAnApprover(string role, int amount)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, amount);
        var service = ServiceFor(invoice);

        Assert.Throws<ApprovalRuleViolationException>(() => service.Approve("INV-1", "demo.user", role));

        Assert.Empty(invoice.Approvers);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
    }
}