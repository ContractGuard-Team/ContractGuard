import { useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes } from "react";

interface FieldProps {
  label: string;
  hint?: string;
  error?: string | undefined;
}

// DESIGN §5 Inputs: 40px high, 6px radius, label above, hint or error below. The error replaces the
// hint and is linked with aria-describedby, so a screen reader reads it with the field.
const controlClass =
  "h-10 w-full rounded-md border bg-sheet px-3 text-body text-ink transition-[border-color,box-shadow] duration-150 ease-out-quart placeholder:text-ink-muted focus:outline-none focus:ring-3 disabled:bg-desk disabled:text-ink-muted";
const normalClass = "border-rule-strong focus:border-review-ink focus:ring-review-ink/15";
const errorClass = "border-severity-critical focus:border-severity-critical focus:ring-severity-critical/15";

function Shell({ id, label, hint, error, children }: FieldProps & { id: string; children: ReactNode }) {
  const message = error ?? hint;
  return (
    <div>
      <label htmlFor={id} className="mb-1 block text-label text-ink">
        {label}
      </label>
      {children}
      {message && (
        <p id={`${id}-message`} className={`mt-1 text-caption ${error ? "text-severity-critical" : "text-ink-muted"}`}>
          {message}
        </p>
      )}
    </div>
  );
}

function describe(id: string, { hint, error }: FieldProps) {
  return {
    "aria-invalid": error ? true : undefined,
    "aria-describedby": error || hint ? `${id}-message` : undefined,
    className: `${controlClass} ${error ? errorClass : normalClass}`,
  };
}

export function TextField({ label, hint, error, ...input }: FieldProps & InputHTMLAttributes<HTMLInputElement>) {
  const id = useId();
  return (
    <Shell id={id} label={label} hint={hint} error={error}>
      <input id={id} {...input} {...describe(id, { label, hint, error })} />
    </Shell>
  );
}

export function SelectField({ label, hint, error, children, ...select }: FieldProps & SelectHTMLAttributes<HTMLSelectElement>) {
  const id = useId();
  return (
    <Shell id={id} label={label} hint={hint} error={error}>
      <select id={id} {...select} {...describe(id, { label, hint, error })}>
        {children}
      </select>
    </Shell>
  );
}
