import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { expect, it } from "vitest";

import Landing from "./landing";

// The sample sheet: one explanation open at a time, opened from its margin note.
it("opens the explanation of the note that is clicked", async () => {
  render(
    <MemoryRouter>
      <Landing />
    </MemoryRouter>,
  );

  expect(screen.getByText(/Điều 301 Luật Thương mại 2005/)).toBeInTheDocument();

  // Each note is in the DOM twice (in the sheet for narrow screens, in the margin for wide ones).
  await userEvent.click(screen.getAllByRole("button", { name: /Chấm dứt ngay/ })[0]!);

  expect(screen.getByText(/không cho Bên B thời gian sửa lỗi/)).toBeInTheDocument();
  expect(screen.queryByText(/Điều 301 Luật Thương mại 2005/)).not.toBeInTheDocument();
});
