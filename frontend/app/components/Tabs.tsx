import { useRef, type KeyboardEvent } from "react";

// DESIGN §5 Tabs: 44px, open tab underlined in review-ink, arrows/Home/End move between tabs (WAI-ARIA).
// Only the tab list: the page renders the panel with id `${id}-panel` and aria-labelledby `${id}-tab-${value}`.
export function Tabs<T extends string>({
  id,
  label,
  tabs,
  value,
  onChange,
}: {
  id: string;
  label: string;
  tabs: { value: T; label: string }[];
  value: T;
  onChange: (value: T) => void;
}) {
  const list = useRef<HTMLDivElement>(null);

  function onKeyDown(event: KeyboardEvent) {
    const index = tabs.findIndex((tab) => tab.value === value);
    const next = { ArrowRight: index + 1, ArrowLeft: index - 1, Home: 0, End: tabs.length - 1 }[event.key];
    if (next === undefined) {
      return;
    }

    event.preventDefault();
    const target = tabs[(next + tabs.length) % tabs.length]!;
    onChange(target.value);
    list.current?.querySelector<HTMLButtonElement>(`#${id}-tab-${target.value}`)?.focus();
  }

  return (
    <div ref={list} role="tablist" aria-label={label} onKeyDown={onKeyDown} className="flex gap-1 overflow-x-auto shadow-[inset_0_-1px_0_var(--color-rule)]">
      {tabs.map((tab) => {
        const selected = tab.value === value;
        return (
          <button
            key={tab.value}
            id={`${id}-tab-${tab.value}`}
            type="button"
            role="tab"
            aria-selected={selected}
            aria-controls={`${id}-panel`}
            tabIndex={selected ? 0 : -1}
            onClick={() => onChange(tab.value)}
            className={`h-11 shrink-0 border-b-2 px-3 text-button transition-colors duration-150 ${selected ? "border-review-ink text-ink" : "border-transparent text-ink-muted hover:text-ink"}`}
          >
            {tab.label}
          </button>
        );
      })}
    </div>
  );
}
