import { useEffect, useId, useRef, type ReactNode } from "react";

import { Button } from "./Button";

// A yes/no question in the app's own modal, instead of the browser's window.confirm.
// Open while `open` is true; Escape and "Hủy" call onCancel.
export function ConfirmDialog({
  open,
  title,
  children,
  confirmLabel,
  danger = false,
  onConfirm,
  onCancel,
}: {
  open: boolean;
  title: string;
  children: ReactNode;
  confirmLabel: string;
  danger?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  useEffect(() => {
    const dialog = ref.current;
    if (open && !dialog?.open) {
      dialog?.showModal();
    } else if (!open && dialog?.open) {
      dialog.close();
    }
  }, [open]);

  return (
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      onClose={(e) => e.target === e.currentTarget && open && onCancel()}
      className="m-auto w-[min(30rem,calc(100vw-2rem))] rounded-md border border-rule bg-sheet p-6 text-ink backdrop:bg-ink/30"
    >
      <h2 id={titleId} className="mb-2 text-title">
        {title}
      </h2>
      <div className="mb-4 max-w-prose">{children}</div>
      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={onCancel}>
          Hủy
        </Button>
        <Button variant={danger ? "danger" : "primary"} onClick={onConfirm}>
          {confirmLabel}
        </Button>
      </div>
    </dialog>
  );
}
