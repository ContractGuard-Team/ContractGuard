import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { forgetCsrfToken } from "~/lib/api";

import { ContractDropzone } from "./ContractDropzone";

function setup(onUploaded: (contractId: string) => Promise<void> = async () => {}) {
  render(<ContractDropzone onUploaded={onUploaded} />);
  return screen.getByTestId("contract-file");
}

function fileOfSize(name: string, bytes: number) {
  const file = new File(["x"], name);
  Object.defineProperty(file, "size", { value: bytes });
  return file;
}

describe("ContractDropzone", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    forgetCsrfToken();
  });

  it("refuses a file type that is not allowed without calling the API", async () => {
    const fetchSpy = vi.fn();
    vi.stubGlobal("fetch", fetchSpy);

    fireEvent.change(setup(), { target: { files: [fileOfSize("virus.exe", 10)] } });

    expect(await screen.findByRole("alert")).toHaveTextContent("Chỉ nhận file PDF, DOCX, JPG hoặc PNG.");
    expect(fetchSpy).not.toHaveBeenCalled();
  });

  it("refuses a file above 20 MB without calling the API", async () => {
    const fetchSpy = vi.fn();
    vi.stubGlobal("fetch", fetchSpy);

    fireEvent.change(setup(), { target: { files: [fileOfSize("big.pdf", 20 * 1024 * 1024 + 1)] } });

    expect(await screen.findByRole("alert")).toHaveTextContent("File lớn hơn giới hạn 20 MB.");
    expect(fetchSpy).not.toHaveBeenCalled();
  });

  it("leaves the next step to the page, then says the file arrived", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (url: string) => {
        if (url.endsWith("/auth/csrf")) {
          return Response.json({ token: "t" });
        }

        if (url.endsWith("/uploads")) {
          return Response.json({ uploadId: "u", url: "http://storage.test/bucket", fields: { key: "quarantine/a.pdf" } });
        }

        return url.endsWith("/contracts") ? Response.json({ contractId: "c1", contractVersionId: "v1" }) : new Response(null, { status: 204 });
      }),
    );
    const onUploaded = vi.fn(async () => {});

    fireEvent.change(setup(onUploaded), { target: { files: [fileOfSize("a.pdf", 10)] } });

    expect(await screen.findByText("Đã tải lên a.pdf.")).toBeInTheDocument();
    expect(onUploaded).toHaveBeenCalledWith("c1");
  });

  it("sends the file to storage after the signed fields, and reports a refused upload", async () => {
    let sentToStorage: FormData | undefined;
    vi.stubGlobal(
      "fetch",
      vi.fn(async (url: string, init?: RequestInit) => {
        if (url.endsWith("/auth/csrf")) {
          return Response.json({ token: "t" });
        }

        if (url.endsWith("/uploads")) {
          return Response.json({ uploadId: "u", url: "http://storage.test/bucket", fields: { key: "quarantine/a.pdf", Policy: "p" } });
        }

        sentToStorage = init?.body as FormData;
        return new Response("EntityTooLarge", { status: 400 });
      }),
    );

    fireEvent.change(setup(), { target: { files: [fileOfSize("a.pdf", 10)] } });

    expect(await screen.findByRole("alert")).toHaveTextContent("Chưa tải được file lên.");
    await waitFor(() => expect(sentToStorage).toBeDefined());
    expect([...sentToStorage!.keys()]).toEqual(["key", "Policy", "file"]);
  });
});
