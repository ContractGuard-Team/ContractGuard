import type { Config } from "@react-router/dev/config";

export default {
  // SPA mode (DEC-GEN-027): the build is static files in build/client, served by S3 + CloudFront
  // on AWS (SD-03). Data is loaded in the browser with clientLoader, never on a Node server.
  ssr: false,
} satisfies Config;
