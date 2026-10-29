import { expect, type Locator, type Page } from '@playwright/test';

/** Page object for the Invoice Approvals UI. Role-based locators only. */
export class InvoicePage {
  constructor(private readonly page: Page) {}

  async open(): Promise<void> {
    await this.page.goto('/');
  }

  async signIn(username: string, password: string, role: string): Promise<void> {
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
    await expect(this.page.getByText(`${username} (${role})`)).toBeVisible();
  }

  async createInvoice(customer: string, amount: number): Promise<void> {
    await this.page.getByLabel('Customer name').fill(customer);
    await this.page.getByLabel('Amount', { exact: true }).fill(String(amount));
    await this.page.getByRole('button', { name: 'Create invoice' }).click();
    await expect(this.invoiceRow(customer)).toBeVisible();
  }

  invoiceRow(customer: string): Locator {
    return this.page.getByRole('row', { name: customer });
  }

  async invoiceIdFor(customer: string): Promise<string> {
    const idCell = this.invoiceRow(customer).getByRole('cell').first();
    return (await idCell.innerText()).trim();
  }

  async submitForApproval(customer: string): Promise<void> {
    await this.invoiceRow(customer)
      .getByRole('button', { name: /Submit invoice .* for approval/ })
      .click();
    await expect(this.invoiceRow(customer)).toContainText('Pending Approval');
  }

  async openApprovals(): Promise<void> {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
  }

  async openInvoices(): Promise<void> {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
  }

  approveButton(invoiceId: string): Locator {
    return this.page.getByRole('button', { name: `Approve invoice ${invoiceId}`, exact: true });
  }

  async approve(invoiceId: string): Promise<void> {
    await this.approveButton(invoiceId).click();
  }

  /** The visible rejection message shown under Pending approvals. */
  decisionError(): Locator {
    return this.page.getByRole('alert');
  }
}
