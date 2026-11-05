import { type RouteConfig, index, layout, route } from "@react-router/dev/routes";

// Route table. Pages inside layout("routes/app-layout.tsx") get the sidebar.
export default [
  route("login", "routes/login.tsx"),
  layout("routes/app-layout.tsx", [index("routes/contracts.tsx")]),
  route("*", "routes/not-found.tsx"),
] satisfies RouteConfig;
