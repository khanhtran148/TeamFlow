import { defineConfig, devices } from "@playwright/test";
import path from "node:path";

const AUTH_STATE_PATH = path.join(__dirname, ".auth", "user.json");

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false, // Run serially to avoid rate limit exhaustion
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1, // Single worker to stay within auth rate limit (10 req/15 min)
  reporter: process.env.CI ? "github" : "html",
  use: {
    baseURL: process.env.BASE_URL ?? "http://localhost:3000",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  projects: [
    {
      name: "setup",
      testMatch: /global-setup\.ts/,
    },
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"],
        storageState: AUTH_STATE_PATH,
      },
      dependencies: ["setup"],
    },
  ],
  webServer: {
    command: "npm run dev",
    url: "http://localhost:3000",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
