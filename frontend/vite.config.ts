import { reactRouter } from "@react-router/dev/vite";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [tailwindcss(), reactRouter()],
  resolve: {
    tsconfigPaths: true,
  },
  server: {
    port: 5173,
    strictPort: true,
    // The browser only talks to the Vite dev server; /api/* is forwarded to the .NET API.
    // Same origin for the browser, so the session cookie (SD-04) works without CORS,
    // exactly like CloudFront forwarding /api/* to the ALB in production (SD-03).
    proxy: {
      "/api": "http://localhost:5080",
    },
  },
});
