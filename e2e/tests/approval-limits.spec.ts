import { expect, test } from '@playwright/test';
import { InvoicePage } from '../pages/invoice-page';

test.describe('approval limits', () => {
  test('a Manager approving an invoice above 50,000 is rejected with a reason and the invoice stays pending', async ({ page }) => {
    const invoices = new InvoicePage(page);
    const customer = `E2E Over Limit ${Date.now()}`;

    await invoices.open();
    await invoices.signIn('demo.manager', 'ChangeMe123!', 'Manager');

    // Arrange: an invoice just above the Manager limit, submitted for approval.
    await invoices.createInvoice(customer, 50001);
    await invoices.submitForApproval(customer);
    const invoiceId = await invoices.invoiceIdFor(customer);

    // Act: the Manager tries to approve it.
    await invoices.openApprovals();
    await invoices.approve(invoiceId);

    // Assert: a clear reason is shown to the user.
    const rejection = invoices.decisionError();
    await expect(rejection).toBeVisible();
    await expect(rejection).toContainText(/exceeds the Manager limit of 50,000/);
    await expect(rejection).toContainText('50,001');

    // Assert: the invoice is still waiting for approval (nothing changed).
    await expect(invoices.approveButton(invoiceId)).toBeVisible();
    await invoices.openInvoices();
    await expect(invoices.invoiceRow(customer)).toContainText('Pending Approval');

    // Assert: the server state is unchanged too, not just the screen.
    await page.reload();
    await expect(invoices.invoiceRow(customer)).toContainText('Pending Approval');
  });
});
