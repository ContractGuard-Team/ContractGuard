import { defineConfig } from "vitest/config";

// Separate from vite.config.ts: the React Router plugin builds whole routes and is not meant
// to run inside unit tests. Components and helpers are tested in a simulated DOM (jsdom).
export default defineConfig({
  resolve: {
    tsconfigPaths: true,
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./vitest.setup.ts"],
    include: ["app/**/*.test.{ts,tsx}"],
  },
});
