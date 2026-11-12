import { defineConfig, devices } from '@playwright/test';

const port = process.env['E2E_PORT'] ?? '4700';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  reporter: [['html', { outputFolder: 'playwright-report', open: 'never' }]],
  use: {
    baseURL: `http://127.0.0.1:${port}`,
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'node e2e/reset-db.mjs && dotnet run --project ../backend/src/Api',
      url: 'http://127.0.0.1:8080/api/contacts',
      env: { ConnectionStrings__Default: 'Data Source=database/contacthub_e2e.db' },
      reuseExistingServer: !process.env['CI'],
      timeout: 300_000,
    },
    {
      command: `npm run start -- --port ${port}`,
      url: `http://127.0.0.1:${port}`,
      reuseExistingServer: !process.env['CI'],
      timeout: 120_000,
    },
  ],
});
