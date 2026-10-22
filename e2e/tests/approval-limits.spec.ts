import { test } from '@playwright/test';
import { InvoiceApprovalsPage } from '../pages/invoice-approvals-page';

test('Manager over-limit approval is rejected and leaves invoice pending', async ({ page }) => {
  const invoicePage = new InvoiceApprovalsPage(page);
  const customerName = `Manager limit ${Date.now()}`;

  await page.goto('/');
  await invoicePage.signIn('demo.manager', 'ChangeMe123!');
  await invoicePage.createAndSubmitInvoice(customerName, 50_001);
  await invoicePage.attemptApproval(customerName);
  await invoicePage.reloadAndExpectPendingInvoice(customerName, '$50,001.00');
});