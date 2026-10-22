import { expect, Page } from '@playwright/test';

export class InvoiceApprovalsPage {
  constructor(private readonly page: Page) {}

  async signIn(username: string, password: string): Promise<void> {
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
    await expect(this.page.getByRole('tab', { name: 'Invoices' })).toBeVisible();
  }

  async createAndSubmitInvoice(customerName: string, amount: number): Promise<void> {
    await this.page.getByLabel('Customer name').fill(customerName);
    await this.page.getByLabel('Amount').fill(String(amount));
    await this.page.getByRole('button', { name: 'Create invoice' }).click();

    const invoiceRow = this.invoiceRow(customerName);
    await expect(invoiceRow).toContainText('Draft');
    await invoiceRow.getByRole('button', { name: /^Submit invoice .* for approval$/ }).click();
    await expect(invoiceRow).toContainText('Pending Approval');
  }

  async attemptApproval(customerName: string): Promise<void> {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
    const pendingInvoice = this.page.getByRole('listitem').filter({ hasText: customerName });
    await expect(pendingInvoice).toBeVisible();
    await pendingInvoice.getByRole('button', { name: /^Approve invoice / }).click();

    const approvalPanel = this.page.getByRole('tabpanel', { name: 'Approvals' });
    await expect(approvalPanel.getByRole('alert')).toContainText(
      'Manager approvals are limited to invoices up to 50,000.'
    );
  }

  async reloadAndExpectPendingInvoice(customerName: string, amountText: string): Promise<void> {
    await this.page.reload();
    await expect(this.page.getByRole('tab', { name: 'Invoices' })).toBeVisible();

    const invoiceRow = this.invoiceRow(customerName);
    await expect(invoiceRow).toContainText(amountText);
    await expect(invoiceRow).toContainText('Pending Approval');
  }

  private invoiceRow(customerName: string) {
    return this.page.getByRole('row').filter({ hasText: customerName });
  }
}