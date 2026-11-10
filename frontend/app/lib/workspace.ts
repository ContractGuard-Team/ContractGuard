// FR-ORG-014: the workspace the user works in, sent as X-Workspace-Id on every API call (SD-05).
// null is the personal workspace. Kept in localStorage so a reload or a new tab stays in the same
// workspace; the API checks membership on every request, so a stale value only costs one 403.

const storageKey = "cg.workspace";

let current: string | null = read();

function read(): string | null {
  try {
    return typeof window === "undefined" ? null : window.localStorage.getItem(storageKey);
  } catch {
    return null;
  }
}

export function currentWorkspace(): string | null {
  return current;
}

export function setWorkspace(organizationId: string | null) {
  current = organizationId;
  try {
    if (organizationId) {
      window.localStorage.setItem(storageKey, organizationId);
    } else {
      window.localStorage.removeItem(storageKey);
    }
  } catch {
    // Private mode: the choice lasts until the tab closes, which is fine.
  }
}

// Only same-app paths, so ?next= cannot send the user to another site after login.
export function safeNext(next: string | null): string {
  return next && next.startsWith("/") && !next.startsWith("//") ? next : "/";
}
