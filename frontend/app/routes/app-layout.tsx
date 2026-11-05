import { Outlet, useNavigate } from "react-router";

import { AppShell } from "~/components/AppShell";

// Layout route: every page inside it gets the sidebar. Phase 1 adds the session check here
// (clientLoader: no session -> redirect to /login).
export default function AppLayout() {
  const navigate = useNavigate();
  return (
    <AppShell onUpload={() => navigate("/contracts/new")}>
      <Outlet />
    </AppShell>
  );
}
