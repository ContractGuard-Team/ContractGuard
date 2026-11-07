import { isRouteErrorResponse, Links, Meta, Outlet, Scripts, ScrollRestoration } from "react-router";

import type { Route } from "./+types/root";

// Self-hosted fonts (no request to Google): Be Vietnam Pro with Vietnamese glyphs, and the
// variable Source Serif 4 for contract text.
import "@fontsource/be-vietnam-pro/400.css";
import "@fontsource/be-vietnam-pro/500.css";
import "@fontsource/be-vietnam-pro/600.css";
import "@fontsource-variable/source-serif-4/index.css";
import "./app.css";

export const links: Route.LinksFunction = () => [{ rel: "icon", href: "/favicon.svg", type: "image/svg+xml" }];

export function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="vi">
      <head>
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>ContractGuard</title>
        <Meta />
        <Links />
      </head>
      <body>
        {children}
        <ScrollRestoration />
        <Scripts />
      </body>
    </html>
  );
}

export default function App() {
  return <Outlet />;
}

// SPA mode: shown while the first clientLoader runs. A quiet skeleton, no blocking spinner.
export function HydrateFallback() {
  return (
    <div className="flex min-h-screen">
      <div className="w-sidebar shrink-0 bg-rail" aria-hidden="true" />
      <p className="sr-only" role="status">
        Đang tải…
      </p>
    </div>
  );
}

export function ErrorBoundary({ error }: Route.ErrorBoundaryProps) {
  let title = "Đã có lỗi xảy ra";
  let details = "Vui lòng tải lại trang. Nếu lỗi lặp lại, hãy báo cho nhóm phát triển.";
  let stack: string | undefined;

  if (isRouteErrorResponse(error)) {
    if (error.status === 404) {
      title = "Không tìm thấy trang";
      details = "Đường dẫn không tồn tại hoặc bạn không có quyền xem.";
    }
  } else if (import.meta.env.DEV && error instanceof Error) {
    details = error.message;
    stack = error.stack;
  }

  return (
    <main className="mx-auto max-w-2xl p-8">
      <h1 className="text-headline">{title}</h1>
      <p className="mt-2 text-ink-secondary">{details}</p>
      <a href="/" className="mt-4 inline-block text-review-ink underline underline-offset-2 hover:text-review-ink-hover">
        Về thư viện hợp đồng
      </a>
      {stack && <pre className="mt-4 overflow-x-auto rounded-md bg-sheet p-4 text-caption">{stack}</pre>}
    </main>
  );
}
