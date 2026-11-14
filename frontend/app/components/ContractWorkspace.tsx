import { useRef, useState, useSyncExternalStore, type ReactNode } from "react";
import { Group, Panel, Separator, useDefaultLayout, usePanelRef } from "react-resizable-panels";

import { Button } from "./Button";

const wideScreen = "(min-width: 1024px)";
const panelTitle = "Trợ lý AI";

function useIsWideScreen() {
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

// UI-GEN-001, UI-GEN-002: the contract fills the center, the chat sits next to it like the agent panel
// of an IDE (resizable, can be hidden, remembers its width). On a narrow screen it becomes a drawer
// (DESIGN §1). The panel holds only the chat; everything else about the contract goes in the center.
// shortcut: crossing the 1024px line while the window is resized remounts both sides, so a typed chat
// message would be lost. Keep one tree if that ever matters.
export function ContractWorkspace({
  title,
  subtitle,
  actions,
  document,
  chat,
}: {
  title: string;
  subtitle: string;
  actions: ReactNode;
  document: ReactNode;
  chat: ReactNode;
}) {
  const wide = useIsWideScreen();
  const panelRef = usePanelRef();
  const drawer = useRef<HTMLDialogElement>(null);
  const [hidden, setHidden] = useState(false);
  const layout = useDefaultLayout({ id: "contract-workspace" });

  const documentArea = <div className="h-full overflow-y-auto px-4 pb-8 lg:px-6">{document}</div>;

  return (
    <div className="flex h-screen flex-col">
      <header className="flex items-center justify-between gap-4 border-b border-rule px-4 py-2.5 lg:px-6">
        <div className="min-w-0">
          <h1 className="truncate text-headline" title={title}>
            {title}
          </h1>
          <p className="truncate text-caption text-ink-secondary" title={subtitle}>
            {subtitle}
          </p>
        </div>
        <div className="flex shrink-0 flex-wrap items-center justify-end gap-2">
          {actions}
          {wide ? (
            <Button variant="ghost" size="sm" onClick={() => (hidden ? panelRef.current?.expand() : panelRef.current?.collapse())}>
              {hidden ? `Hiện ${panelTitle}` : `Ẩn ${panelTitle}`}
            </Button>
          ) : (
            <Button variant="secondary" size="sm" onClick={() => drawer.current?.showModal()}>
              {panelTitle}
            </Button>
          )}
        </div>
      </header>

      {wide ? (
        <Group className="min-h-0 flex-1" defaultLayout={layout.defaultLayout} onLayoutChanged={layout.onLayoutChanged}>
          <Panel id="document" minSize="420px">
            {documentArea}
          </Panel>
          <Separator
            aria-label="Đổi độ rộng panel"
            className="w-px bg-rule transition-colors duration-150 hover:bg-review-ink focus-visible:bg-review-ink focus-visible:outline-none data-[separator=active]:bg-review-ink"
          />
          <Panel
            id="side"
            panelRef={panelRef}
            collapsible
            defaultSize="400px"
            minSize="320px"
            maxSize="55%"
            onResize={() => setHidden(panelRef.current?.isCollapsed() ?? false)}
          >
            <aside aria-label={panelTitle} className="relative flex h-full flex-col overflow-hidden bg-sheet">
              <h2 className="border-b border-rule px-4 py-2.5 text-title">{panelTitle}</h2>
              {chat}
            </aside>
          </Panel>
        </Group>
      ) : (
        <>
          <div className="min-h-0 flex-1">{documentArea}</div>
          <dialog ref={drawer} aria-label={panelTitle} className="m-0 ml-auto h-full max-h-none w-[min(26rem,90vw)] max-w-none bg-sheet text-ink shadow-2xl backdrop:bg-ink/30">
            <div className="flex h-full flex-col">
              <div className="flex items-center justify-between gap-2 border-b border-rule px-4 py-2">
                <h2 className="text-title">{panelTitle}</h2>
                <Button variant="ghost" size="sm" onClick={() => drawer.current?.close()}>
                  Đóng
                </Button>
              </div>
              {chat}
            </div>
          </dialog>
        </>
      )}
    </div>
  );
}
