import type { Route } from "./+types/login";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Đăng nhập · ContractGuard" }];
}

// Placeholder. Phase 1 (F-AUTH-001) adds the form and the call to POST /api/v1/auth/login.
export default function Login() {
  return (
    <main className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-sm rounded-md border border-rule bg-sheet p-6">
        <h1 className="text-headline">Đăng nhập</h1>
        <p className="mt-2 text-ink-secondary">Trang đăng nhập sẽ có ở Phase 1.</p>
      </div>
    </main>
  );
}
