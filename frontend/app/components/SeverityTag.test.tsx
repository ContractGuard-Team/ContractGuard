import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { SeverityTag, type Severity } from "./SeverityTag";

// The Never-Alone Rule (DESIGN §2): severity is shown by shape + word + color, never color alone.
describe("SeverityTag", () => {
  const cases: [Severity, string, string][] = [
    ["critical", "Nghiêm trọng", "diamond"],
    ["high", "Cao", "triangle"],
    ["medium", "Trung bình", "circle"],
    ["low", "Thấp", "ring"],
  ];

  it.each(cases)("%s shows its word and its own shape", (severity, word, shape) => {
    const { container } = render(<SeverityTag severity={severity} />);

    expect(screen.getByText(word)).toBeInTheDocument();
    expect(container.querySelector(`svg[data-shape="${shape}"]`)).not.toBeNull();
  });

  it("hides the shape from screen readers, the word carries the meaning", () => {
    const { container } = render(<SeverityTag severity="high" />);

    expect(container.querySelector("svg")).toHaveAttribute("aria-hidden", "true");
  });
});
