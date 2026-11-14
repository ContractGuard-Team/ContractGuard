import { type RouteConfig, index, layout, route } from "@react-router/dev/routes";

// Route table. Pages inside layout("routes/app-layout.tsx") need a session and get the sidebar.
// "/" is the public landing page; the library of a signed-in user lives at /contracts.
export default [
  index("routes/landing.tsx"),
  route("login", "routes/login.tsx"),
  route("register", "routes/register.tsx"),
  route("verify-email", "routes/verify-email.tsx"),
  route("forgot-password", "routes/forgot-password.tsx"),
  route("reset-password", "routes/reset-password.tsx"),
  route("invitations/:token", "routes/invitation.tsx"),
  layout("routes/app-layout.tsx", [
    route("contracts", "routes/contracts.tsx"),
    route("contracts/new", "routes/contracts-new.tsx"),
    route("contracts/:contractId", "routes/contract-detail.tsx"),
    route("organization", "routes/organization.tsx"),
    route("organizations/new", "routes/organization-new.tsx"),
  ]),
  route("*", "routes/not-found.tsx"),
] satisfies RouteConfig;
