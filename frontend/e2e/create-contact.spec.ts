import { expect, test } from '@playwright/test';

test('creates a contact and shows it in the list', async ({ page }) => {
  const surname = `Tester${Date.now()}`;

  await page.goto('/contacts/new');
  await page.getByLabel('First name').fill('Ada');
  await page.getByLabel('Surname').fill(surname);
  await page.getByLabel('Date of birth').pressSequentially('1990-05-17');
  await page.keyboard.press('Escape');
  await page.getByLabel('Phone number').fill('+359888123456');
  await page.getByLabel('IBAN').fill('GB82WEST12345698765432');
  await page.getByLabel('Street').fill('1 Main Street');
  await page.getByLabel('Postal code').fill('1000');
  await page.getByLabel('City').fill('Sofia');
  await page.getByLabel('Country').fill('Bulgaria');
  await page.getByRole('button', { name: 'Save contact' }).click();

  await expect(page).toHaveURL(/\/contacts$/);
  const row = page.getByRole('row').filter({ hasText: surname });
  await expect(row.getByRole('link', { name: `Ada ${surname}` })).toBeVisible();
  await expect(row.getByRole('cell', { name: 'GB82 WEST 1234 5698 7654 32' })).toBeVisible();
});
