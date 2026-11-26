import { renderAsync } from "docx-preview";
import { useEffect, useRef, useState } from "react";

import { severityLabels, type Severity } from "./SeverityTag";
import { ViewerFailed, ViewerSkeleton } from "./ViewerParts";

// One clause of the run, for washing its paragraphs in the DOCX (FR-VIEW-009). Every clause is passed, also
// those without findings (severity null), so a paragraph is matched to the clause it really belongs to.
export interface DocxClause {
  clauseKey: string;
  text: string;
  severity: Severity | null;
  label: string;
}

// docx-preview does not clean what a file contains, so it renders into a frame where nothing can run:
// no scripts or javascript: links, and no popups, which the base target turns every link into.
// Keep "allow-scripts" out of the sandbox: together with allow-same-origin it would undo all of this.
// The library empties the containers it is given, so it gets its own two instead of head and body.
const frameDocument = `<!doctype html><html><head><base target="_blank"><style>
  body { margin: 0; }
  .docx-wrapper { background: transparent !important; padding: 2px 0 24px !important; }
  .docx-wrapper > section.docx { margin-bottom: 16px !important; box-shadow: 0 1px 3px rgb(0 0 0 / 0.2) !important; }
  [data-clause] { cursor: pointer; }
</style></head><body><div id="styles"></div><div id="content"></div></body></html>`;

const normalize = (text: string) => text.replace(/\s+/g, " ").trim().toLowerCase();

// Shorter paragraphs ("1.", "a)") appear in many clauses and would be guessed, not matched.
const minimumParagraph = 8;

// For each paragraph, the clause whose verbatim text contains it. Clauses run in document order, so the
// search starts at the last matched clause and only then looks at the rest.
export function matchParagraphs(paragraphs: string[], clauses: Pick<DocxClause, "clauseKey" | "text">[]): (string | null)[] {
  const texts = clauses.map((c) => normalize(c.text));
  let cursor = 0;
  return paragraphs.map((paragraph) => {
    const text = normalize(paragraph);
    if (text.length < minimumParagraph) {
      return null;
    }

    const order = [...texts.keys()].slice(cursor).concat([...texts.keys()].slice(0, cursor));
    const found = order.find((i) => texts[i]!.includes(text));
    if (found === undefined) {
      return null;
    }

    cursor = found;
    return clauses[found]!.clauseKey;
  });
}

export default function DocxViewer({ url, clauses = [], onSelectClause }: { url: string; clauses?: DocxClause[]; onSelectClause?: (clauseKey: string) => void }) {
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

  // The frame runs no script, so the page styles its paragraphs and listens on them from outside
  // (same origin through allow-same-origin). Colours are read from the page's tokens: the frame has none.
  useEffect(() => {
    const document = frame.current?.contentDocument;
    if (state !== "ready" || !document) {
      return;
    }

    const paragraphs = [...document.querySelectorAll<HTMLParagraphElement>("#content p")];
    const keys = matchParagraphs(paragraphs.map((p) => p.textContent ?? ""), clauses);
    const tokens = getComputedStyle(window.document.documentElement);
    const cleanups: (() => void)[] = [];
    paragraphs.forEach((paragraph, index) => {
      const clause = clauses.find((c) => c.clauseKey === keys[index]);
      if (!clause?.severity) {
        return;
      }

      const open = () => onSelectClause?.(clause.clauseKey);
      const onKey = (event: KeyboardEvent) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          open();
        }
      };
      const label = `${severityLabels[clause.severity]}: ${clause.label}`;
      paragraph.dataset.clause = clause.clauseKey;
      paragraph.style.backgroundColor = tokens.getPropertyValue(`--color-severity-${clause.severity}-wash`);
      paragraph.style.boxShadow = `inset 3px 0 0 ${tokens.getPropertyValue(`--color-severity-${clause.severity}`)}`;
      paragraph.title = label;
      paragraph.tabIndex = 0;
      paragraph.setAttribute("role", "button");
      paragraph.setAttribute("aria-label", label);
      paragraph.addEventListener("click", open);
      paragraph.addEventListener("keydown", onKey);
      cleanups.push(() => {
        paragraph.removeEventListener("click", open);
        paragraph.removeEventListener("keydown", onKey);
        delete paragraph.dataset.clause;
        paragraph.style.backgroundColor = "";
        paragraph.style.boxShadow = "";
        paragraph.removeAttribute("title");
        paragraph.removeAttribute("tabindex");
        paragraph.removeAttribute("role");
        paragraph.removeAttribute("aria-label");
      });
    });
    return () => cleanups.forEach((cleanup) => cleanup());
  }, [state, clauses, onSelectClause]);

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
