import { data } from "react-router";

// Catch-all route ("*"): throwing a 404 shows the root ErrorBoundary with the "not found" text.
export function clientLoader() {
  throw data(null, { status: 404 });
}

export default function NotFound() {
  return null;
}
