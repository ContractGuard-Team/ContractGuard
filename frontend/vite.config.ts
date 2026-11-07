import { createRequire } from "node:module";
import path from "node:path";

import { reactRouter } from "@react-router/dev/vite";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig, normalizePath } from "vite";
import { viteStaticCopy } from "vite-plugin-static-copy";

const pdfjsDist = path.dirname(createRequire(import.meta.url).resolve("pdfjs-dist/package.json"));

export default defineConfig({
  plugins: [
    tailwindcss(),
    reactRouter(),
    // Files PDF.js asks for on demand: character maps, fonts a PDF does not embed, image decoders of scans.
    viteStaticCopy({
      targets: ["cmaps", "standard_fonts", "wasm"].map((folder) => ({
        src: normalizePath(path.join(pdfjsDist, folder, "*")),
        dest: folder,
        rename: { stripBase: true },
      })),
    }),
  ],
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
