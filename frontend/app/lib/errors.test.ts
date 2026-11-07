import { describe, expect, it } from "vitest";

import { ApiError } from "./api";
import { errorMessage, failureMessage } from "./errors";

describe("errorMessage", () => {
  it("translates a known code", () => {
    expect(errorMessage(new ApiError(401, { code: "ERR-AUTH-003", detail: "Wrong email or password." }))).toBe(
      "Email hoặc mật khẩu không đúng.",
    );
  });

  it("never shows the English text of an unknown error", () => {
    expect(errorMessage(new ApiError(500, { detail: "NullReferenceException" }))).toBe("Đã có lỗi xảy ra. Vui lòng thử lại.");
    expect(errorMessage(new TypeError("Failed to fetch"))).toBe("Đã có lỗi xảy ra. Vui lòng thử lại.");
  });
});

describe("failureMessage", () => {
  it("translates a reason that starts with an error code", () => {
    expect(failureMessage("ERR-UPL-004: 120 pages")).toBe("File có nhiều hơn giới hạn 100 trang.");
  });

  it("hides schema details of a rejected result", () => {
    const message = failureMessage('The analysis result was invalid: /clauses: Value is "string"');

    expect(message).not.toContain("/clauses");
  });

  it("shows any other reason as written", () => {
    expect(failureMessage("Không nhận diện được điều khoản")).toBe("Không nhận diện được điều khoản");
  });

  it("handles a missing reason", () => {
    expect(failureMessage(null)).toBe("Không rõ lý do.");
  });
});
