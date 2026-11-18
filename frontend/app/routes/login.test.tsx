import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { expect, it } from "vitest";

import Login from "./login";

function renderAt(url: string) {
  render(
    <MemoryRouter initialEntries={[url]}>
      <Login />
    </MemoryRouter>,
  );
}

// F-AUTH-002: the Google link keeps where the user was going, e.g. an invitation.
it("sends the Google sign-in back to the page the user came from", () => {
  renderAt("/login?next=/invitations/abc");

  expect(screen.getByRole("link", { name: /Tiếp tục với Google/ })).toHaveAttribute(
    "href",
    "/api/v1/auth/google?next=%2Finvitations%2Fabc",
  );
});

it("never sends the Google sign-in to another site", () => {
  renderAt("/login?next=//evil.example");

  expect(screen.getByRole("link", { name: /Tiếp tục với Google/ })).toHaveAttribute("href", "/api/v1/auth/google?next=%2Fcontracts");
});

it("explains why the API sent the user back from Google", () => {
  renderAt("/login?google=cancelled");

  expect(screen.getByText("Bạn đã hủy đăng nhập bằng Google.")).toBeInTheDocument();
});
