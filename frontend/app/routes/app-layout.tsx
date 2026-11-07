import { Outlet, useMatches, useNavigate } from "react-router";

import { AppShell } from "~/components/AppShell";
import { apiLoad, apiPost, type Schemas } from "~/lib/api";

import type { Route } from "./+types/app-layout";

// SEC-AUTH-001: every page inside this layout needs a session; without one apiLoad goes to /login.
export async function clientLoader() {
  return apiLoad<Schemas["MeResponse"]>("/me");
}

export default function AppLayout({ loaderData }: Route.ComponentProps) {
  const navigate = useNavigate();
  const inWorkspace = useMatches().some((match) => (match.handle as { workspace?: boolean } | undefined)?.workspace);

  async function logout() {
    await apiPost("/auth/logout");
    await navigate("/login");
  }

  return (
    <AppShell compact={inWorkspace} name={loaderData.displayName} email={loaderData.email} onUpload={() => void navigate("/contracts/new")} onLogout={() => void logout()}>
      <Outlet />
    </AppShell>
  );
}
