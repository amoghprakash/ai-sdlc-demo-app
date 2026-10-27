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
    await this.approveInvoice(customerName);

    await this.expectDecisionError('Manager approvals are limited to invoices up to 50,000.');
  }

  async expectDecisionError(message: string): Promise<void> {
    const approvalPanel = this.page.getByRole('tabpanel', { name: 'Approvals' });
    await expect(approvalPanel.getByRole('alert')).toContainText(message);
  }

  async approveInvoice(customerName: string): Promise<void> {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
    const pendingInvoice = this.page.getByRole('listitem').filter({ hasText: customerName });
    await expect(pendingInvoice).toBeVisible();
    await pendingInvoice.getByRole('button', { name: /^Approve invoice / }).click();
  }

  async signOut(): Promise<void> {
    await this.page.getByRole('button', { name: 'Sign out' }).click();
    await expect(this.page.getByRole('button', { name: 'Sign in' })).toBeVisible();
  }

  async expectInvoiceStatus(customerName: string, status: string): Promise<void> {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
    await expect(this.invoiceRow(customerName)).toContainText(status);
  }

  async reloadAndExpectPendingInvoice(customerName: string, amountText: string): Promise<void> {
    await this.page.reload();
    await expect(this.page.getByRole('tab', { name: 'Invoices' })).toBeVisible();

    const invoiceRow = this.invoiceRow(customerName);
    await expect(invoiceRow).toContainText(amountText);
    await expect(invoiceRow).toContainText('Pending Approval');
  }

  async reloadAndExpectSecondApproval(
    customerName: string,
    amountText: string,
    firstApprover: string
  ): Promise<void> {
    const invoicesResponse = this.page.waitForResponse((response) =>
      response.url().endsWith('/api/invoices') && response.request().method() === 'GET'
    );
    await this.page.reload();
    const invoices = await (await invoicesResponse).json();
    const invoice = invoices.find((item: { customerName: string }) => item.customerName === customerName);

    expect(invoice).toMatchObject({
      status: 'PendingSecondApproval',
      firstApprover,
    });
    await expect(this.page.getByRole('tab', { name: 'Invoices' })).toBeVisible();

    const invoiceRow = this.invoiceRow(customerName);
    await expect(invoiceRow).toContainText(amountText);
    await expect(invoiceRow).toContainText('Pending Second Approval');
  }

  private invoiceRow(customerName: string) {
    return this.page.getByRole('row').filter({ hasText: customerName });
  }
}