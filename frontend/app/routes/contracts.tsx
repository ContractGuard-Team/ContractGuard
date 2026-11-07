import { Link, useRevalidator } from "react-router";

import { ContractDropzone } from "~/components/ContractDropzone";
import { apiLoad, type Schemas } from "~/lib/api";
import { approvalStatusLabels } from "~/lib/labels";

import type { Route } from "./+types/contracts";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Thư viện hợp đồng · ContractGuard" }];
}

export async function clientLoader() {
  return apiLoad<Schemas["ContractSummary"][]>("/contracts");
}

const dateFormat = new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" });

// Contract library (F-CTR-003): a drop area right on the page (UI-GEN-003), then the contracts.
// Search, filter and sort come in P5-03.
export default function Contracts({ loaderData: contracts }: Route.ComponentProps) {
  const { revalidate } = useRevalidator();

  return (
    <section aria-labelledby="library-title" className="flex max-w-4xl flex-col gap-6">
      <h1 id="library-title" className="text-headline">
        Thư viện hợp đồng
      </h1>

      {/* Stays on the library: the new contract appears at the top of the list. */}
      <ContractDropzone onUploaded={revalidate} />

      {contracts.length === 0 ? (
        <p className="max-w-prose text-ink-secondary">
          Chưa có hợp đồng nào. Sau khi bạn tải file lên, hệ thống tách điều khoản và chỉ ra rủi ro tiềm ẩn cần xem xét.
        </p>
      ) : (
        <div className="overflow-x-auto rounded-md border border-rule bg-sheet">
          <table className="w-full text-left">
            <thead className="border-b border-rule text-label text-ink-secondary">
              <tr>
                <th scope="col" className="px-4 py-2.5 font-medium">
                  Hợp đồng
                </th>
                <th scope="col" className="px-4 py-2.5 font-medium whitespace-nowrap">
                  Trạng thái
                </th>
                <th scope="col" className="px-4 py-2.5 font-medium whitespace-nowrap">
                  Phiên bản
                </th>
                <th scope="col" className="px-4 py-2.5 font-medium whitespace-nowrap">
                  Cập nhật
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-rule">
              {contracts.map((contract) => (
                <tr key={contract.contractId} className="transition-colors duration-150 hover:bg-desk">
                  <td className="px-4 py-3">
                    <Link to={`/contracts/${contract.contractId}`} className="font-medium text-ink underline-offset-2 hover:underline">
                      {contract.contractName}
                    </Link>
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-ink-secondary">{approvalStatusLabels[contract.approvalStatus]}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-ink-secondary">V{contract.latestVersionNumber}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-ink-secondary">{dateFormat.format(new Date(contract.updatedAt))}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
