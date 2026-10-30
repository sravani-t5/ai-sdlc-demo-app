using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class InvoiceTests
{
    [Fact]
    public void NewInvoice_StartsAsAnEmptyDraftWithNoApprovers()
    {
        var invoice = new Invoice();

        Assert.Equal(string.Empty, invoice.Id);
        Assert.Equal(string.Empty, invoice.CustomerName);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Empty(invoice.Approvers);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
    }

    [Fact]
    public void NewInvoices_DoNotShareTheirApproverList()
    {
        var first = new Invoice();
        var second = new Invoice();

        first.Approvers.Add(new InvoiceApprover("demo.manager", "Manager"));

        Assert.Empty(second.Approvers);
    }
}
