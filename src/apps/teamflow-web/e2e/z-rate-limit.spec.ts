import { test, expect } from "@playwright/test";

const API_URL = process.env.API_URL ?? "http://localhost:5210/api/v1";

test.describe("AC8: Auth endpoint rate limit → 429 with Retry-After", () => {
  test("rate limit triggers after exhausting auth quota", async ({
    request,
  }) => {
    // Send login requests until we get a 429 (some quota may already be used by prior tests)
    let got429 = false;
    let retryAfterHeader: string | null = null;
    // Dev environment has 10x limits (300), production has 30
    const maxAttempts = 305;

    for (let i = 0; i < maxAttempts; i++) {
      const response = await request.post(`${API_URL}/auth/login`, {
        data: {
          email: `ratelimit-${Date.now()}-${i}@test.com`,
          password: "wrong",
        },
      });

      if (response.status() === 429) {
        got429 = true;
        retryAfterHeader = response.headers()["retry-after"];
        break;
      }
    }

    expect(got429).toBe(true);
    expect(retryAfterHeader).toBeTruthy();
  });
});
