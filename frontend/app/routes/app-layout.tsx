import { Outlet, useMatches, useNavigate, useRevalidator } from "react-router";

import { AppShell } from "~/components/AppShell";
import { apiLoad, apiPost, type Schemas } from "~/lib/api";
import { currentWorkspace, setWorkspace } from "~/lib/workspace";

import type { Route } from "./+types/app-layout";

// SEC-AUTH-001: every page inside this layout needs a session; without one apiLoad goes to /login.
// A remembered organization the user no longer belongs to falls back to the personal workspace.
export async function clientLoader() {
  const me = await apiLoad<Schemas["MeResponse"]>("/me");
  const chosen = currentWorkspace();
  if (chosen && !me.workspaces.some((w) => w.organizationId === chosen)) {
    setWorkspace(null);
  }

  return { me };
}

export default function AppLayout({ loaderData }: Route.ComponentProps) {
  const { me } = loaderData;
  // Read on every render, not from the loader: a contract link may switch the workspace (contract-detail).
  const workspace = currentWorkspace();
  const navigate = useNavigate();
  const { revalidate } = useRevalidator();
  const inWorkspace = useMatches().some((match) => (match.handle as { workspace?: boolean } | undefined)?.workspace);

  async function logout() {
    await apiPost("/auth/logout");
    setWorkspace(null);
    await navigate("/login");
  }

  // FR-ORG-014: everything reloads in the chosen workspace, starting from its library.
  async function switchWorkspace(organizationId: string | null | "new") {
    if (organizationId === "new") {
      await navigate("/organizations/new");
      return;
    }

    setWorkspace(organizationId);
    await navigate("/contracts");
    await revalidate();
  }

  return (
    <AppShell
      compact={inWorkspace}
      name={me.displayName}
      email={me.email}
      workspaces={me.workspaces.map((w) => ({ organizationId: w.organizationId ?? null, label: w.organizationName ?? "Cá nhân" }))}
      workspace={workspace}
      onSwitchWorkspace={(id) => void switchWorkspace(id)}
      onUpload={() => void navigate("/contracts/new")}
      onLogout={() => void logout()}
    >
      <Outlet />
    </AppShell>
  );
}
