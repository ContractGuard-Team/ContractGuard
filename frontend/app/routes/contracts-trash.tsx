import { useState } from "react";
import { Link, useRevalidator } from "react-router";

import { Button } from "~/components/Button";
import { ConfirmDialog } from "~/components/ConfirmDialog";
import { Notice } from "~/components/Notice";
import { apiFetch, apiLoad, apiPost, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";

import type { Route } from "./+types/contracts-trash";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Thùng rác · ContractGuard" }];
}

export async function clientLoader() {
  return apiLoad<Schemas["TrashItem"][]>("/contracts/trash");
}

const dateFormat = new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" });
const day = 24 * 60 * 60 * 1000;

function daysLeft(purgeAt: string) {
  return Math.max(0, Math.ceil((new Date(purgeAt).getTime() - Date.now()) / day));
}

// CR-CTR-001: contracts deleted in the current workspace, for their owner and organization owners.
// Restore puts one back in the library; "Xóa vĩnh viễn" does not wait for the 30 days.
export default function ContractsTrash({ loaderData: items }: Route.ComponentProps) {
  const { revalidate } = useRevalidator();
  const action = useAction();
  const [done, setDone] = useState<string | null>(null);
  const [purging, setPurging] = useState<Schemas["TrashItem"] | null>(null);

  function run(work: () => Promise<unknown>, message: string) {
    setDone(null);
    void action.run(async () => {
      await work();
      await revalidate();
      setDone(message);
    });
  }

  return (
    <section aria-labelledby="trash-title" className="flex max-w-5xl flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 id="trash-title" className="text-headline">
          Thùng rác
        </h1>
        <p className="max-w-prose text-ink-secondary">Hợp đồng đã xóa nằm ở đây 30 ngày, sau đó bị xóa vĩnh viễn cùng mọi phiên bản, kết quả phân tích và lịch sử chat.</p>
      </div>

      {action.error !== null && <Notice tone="error">{errorMessage(action.error)}</Notice>}
      {done !== null && <Notice tone="info">{done}</Notice>}

      {items.length === 0 ? (
        <p className="max-w-prose text-ink-secondary">
          Thùng rác trống.{" "}
          <Link to="/contracts" className="text-review-ink underline underline-offset-2">
            Về thư viện hợp đồng
          </Link>
        </p>
      ) : (
        // A list, not a table: on a phone the actions wrap under the name instead of scrolling off screen.
        <ul aria-label={`${items.length} hợp đồng trong thùng rác`} className="divide-y divide-rule rounded-md border border-rule bg-sheet">
          {items.map((item) => (
            <li key={item.contractId} className="flex flex-wrap items-center gap-x-6 gap-y-3 px-4 py-3">
              <div className="min-w-0 flex-1 basis-64">
                <p className="font-medium break-words text-ink">{item.contractName}</p>
                <p className="text-caption break-words text-ink-secondary">
                  {item.originalFileName} · Xóa lúc {dateFormat.format(new Date(item.deletedAt))} · Còn {daysLeft(item.purgeAt)} ngày trước khi xóa vĩnh viễn
                </p>
              </div>
              <div className="flex gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  className="whitespace-nowrap"
                  disabled={action.pending}
                  onClick={() => run(() => apiPost(`/contracts/${item.contractId}/restore`), `Đã khôi phục “${item.contractName}” về thư viện.`)}
                >
                  Khôi phục
                </Button>
                <Button variant="danger" size="sm" className="whitespace-nowrap" disabled={action.pending} onClick={() => setPurging(item)}>
                  Xóa vĩnh viễn
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}

      <ConfirmDialog
        open={purging !== null}
        title="Xóa vĩnh viễn hợp đồng?"
        confirmLabel="Xóa vĩnh viễn"
        danger
        onCancel={() => setPurging(null)}
        onConfirm={() => {
          const item = purging!;
          setPurging(null);
          run(() => apiFetch(`/contracts/${item.contractId}/permanent`, { method: "DELETE" }), `Đã xóa vĩnh viễn “${item.contractName}”.`);
        }}
      >
        File gốc, mọi phiên bản, kết quả phân tích, finding, lịch sử chat và nhật ký của “{purging?.contractName}” sẽ bị xóa và không khôi phục được.
      </ConfirmDialog>
    </section>
  );
}
