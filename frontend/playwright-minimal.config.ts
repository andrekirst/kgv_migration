import { defineConfig, devices } from '@playwright/test'

/**
 * Minimal Playwright Konfiguration für KGV Bezirke Tests
 * Ohne Global Setup - für schnelle lokale Tests
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,

  // Minimal Reporter
  reporter: 'line',

  use: {
    baseURL: 'http://localhost:3001',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 10000,
    navigationTimeout: 15000,
  },

  // Nur Chrome für schnelle Tests
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Kein Global Setup
  // globalSetup: undefined,
  // globalTeardown: undefined,

  // Dev Server für lokale Tests
  webServer: {
    command: 'echo "Assuming Next.js is already running on localhost:3001"',
    port: 3001,
    reuseExistingServer: true,
  },
})