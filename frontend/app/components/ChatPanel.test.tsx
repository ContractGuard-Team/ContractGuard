import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { ChatPanel } from "./ChatPanel";

describe("ChatPanel", () => {
  it("stays locked and says why while no assistant is connected", () => {
    render(<ChatPanel />);

    expect(screen.getByRole("log")).toHaveTextContent("Trợ lý AI chưa được kết nối");
    expect(screen.getByLabelText("Câu hỏi về hợp đồng")).toBeDisabled();
    expect(screen.getByRole("button", { name: "Gửi" })).toBeDisabled();
  });
});
