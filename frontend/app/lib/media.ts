import { useSyncExternalStore } from "react";

// The lg breakpoint of Tailwind: side panels sit next to the content above it, over the content below it.
const wideScreen = "(min-width: 1024px)";

export function useIsWideScreen() {
  return useSyncExternalStore(
    (onChange) => {
      const query = window.matchMedia(wideScreen);
      query.addEventListener("change", onChange);
      return () => query.removeEventListener("change", onChange);
    },
    () => window.matchMedia(wideScreen).matches,
    () => true,
  );
}
