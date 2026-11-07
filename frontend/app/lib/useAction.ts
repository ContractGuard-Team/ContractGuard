import { useState } from "react";

// State of one button-triggered request: is it running, and how did it fail.
export function useAction() {
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<unknown>(null);

  async function run(action: () => Promise<void>) {
    setPending(true);
    setError(null);
    try {
      await action();
    } catch (caught) {
      setError(caught);
    } finally {
      setPending(false);
    }
  }

  return { pending, error, run, clearError: () => setError(null) };
}
