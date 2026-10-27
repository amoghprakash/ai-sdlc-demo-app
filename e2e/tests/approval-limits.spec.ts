import { expect, test } from '@playwright/test';
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

test('Manager and a different Director complete high-value approval through the UI', async ({ page }) => {
  const invoicePage = new InvoiceApprovalsPage(page);
  const customerName = `High value ${Date.now()}`;

  await page.goto('/');
  await invoicePage.signIn('demo.manager', 'ChangeMe123!');
  await invoicePage.createAndSubmitInvoice(customerName, 600_001);

  const firstApprovalResponse = page.waitForResponse((response) =>
    response.url().includes('/api/approvals/') &&
    response.url().endsWith('/approve') &&
    response.request().method() === 'POST'
  );
  await invoicePage.approveInvoice(customerName);
  const firstApproval = await (await firstApprovalResponse).json();

  expect(firstApproval).toMatchObject({
    status: 'PendingSecondApproval',
    firstApprover: 'demo.manager',
    secondApprover: null,
  });
  await invoicePage.expectInvoiceStatus(customerName, 'Pending Second Approval');

  await invoicePage.signOut();
  await invoicePage.signIn('demo.director', 'ChangeMe123!');

  const secondApprovalResponse = page.waitForResponse((response) =>
    response.url().includes('/api/approvals/') &&
    response.url().endsWith('/approve') &&
    response.request().method() === 'POST'
  );
  await invoicePage.approveInvoice(customerName);
  const secondApproval = await (await secondApprovalResponse).json();

  expect(secondApproval).toMatchObject({
    status: 'Approved',
    firstApprover: 'demo.manager',
    secondApprover: 'demo.director',
    decidedBy: 'demo.director',
  });
  await invoicePage.expectInvoiceStatus(customerName, 'Approved');
});

test('duplicate second approval is rejected and preserves the first approver', async ({ page }) => {
  const invoicePage = new InvoiceApprovalsPage(page);
  const customerName = `Duplicate second approver ${Date.now()}`;

  await page.goto('/');
  await invoicePage.signIn('demo.manager', 'ChangeMe123!');
  await invoicePage.createAndSubmitInvoice(customerName, 600_001);
  await invoicePage.approveInvoice(customerName);
  await invoicePage.expectInvoiceStatus(customerName, 'Pending Second Approval');

  const invalidApprovalResponse = page.waitForResponse((response) =>
    response.url().includes('/api/approvals/') &&
    response.url().endsWith('/approve') &&
    response.request().method() === 'POST'
  );
  await invoicePage.approveInvoice(customerName);
  const response = await invalidApprovalResponse;
  expect(response.status()).toBe(409);
  await invoicePage.expectDecisionError('same user');

  await invoicePage.reloadAndExpectSecondApproval(customerName, '$600,001.00', 'demo.manager');
});