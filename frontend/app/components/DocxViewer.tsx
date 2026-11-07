import { renderAsync } from "docx-preview";
import { useRef, useState } from "react";

import { ViewerFailed, ViewerSkeleton } from "./ViewerParts";

// docx-preview does not clean what a file contains, so it renders into a frame where nothing can run:
// no scripts or javascript: links, and no popups, which the base target turns every link into.
// Keep "allow-scripts" out of the sandbox: together with allow-same-origin it would undo all of this.
// The library empties the containers it is given, so it gets its own two instead of head and body.
const frameDocument = `<!doctype html><html><head><base target="_blank"><style>
  body { margin: 0; }
  .docx-wrapper { background: transparent !important; padding: 2px 0 24px !important; }
  .docx-wrapper > section.docx { margin-bottom: 16px !important; box-shadow: 0 1px 3px rgb(0 0 0 / 0.2) !important; }
</style></head><body><div id="styles"></div><div id="content"></div></body></html>`;

export default function DocxViewer({ url }: { url: string }) {
  const frame = useRef<HTMLIFrameElement>(null);
  const [state, setState] = useState<"loading" | "ready" | "failed">("loading");

  async function render() {
    const document = frame.current?.contentDocument;
    const content = document?.getElementById("content");
    const styles = document?.getElementById("styles");
    if (!document || !content || !styles) {
      return;
    }

    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`The file could not be read: ${response.status}`);
    }

    await renderAsync(await response.blob(), content, styles, { renderAltChunks: false, renderChanges: false });
    if (frame.current) {
      frame.current.style.height = `${document.documentElement.scrollHeight}px`;
      setState("ready");
    }
  }

  return (
    <div>
      {state === "loading" && <ViewerSkeleton />}
      {state === "failed" && <ViewerFailed />}
      <iframe
        ref={frame}
        title="Nội dung file DOCX"
        sandbox="allow-same-origin"
        srcDoc={frameDocument}
        onLoad={() => void render().catch(() => setState("failed"))}
        className={state === "ready" ? "block w-full border-0" : "block h-0 w-full border-0"}
      />
    </div>
  );
}
