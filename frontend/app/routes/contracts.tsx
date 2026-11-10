import { Form, Link, useNavigation, useRevalidator, useSearchParams } from "react-router";

import { Button } from "~/components/Button";
import { ContractDropzone } from "~/components/ContractDropzone";
import { SeverityTag } from "~/components/SeverityTag";
import { apiLoad, type Schemas } from "~/lib/api";
import { approvalStatusLabels, contractTypeLabels } from "~/lib/labels";

import type { Route } from "./+types/contracts";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Thư viện hợp đồng · ContractGuard" }];
}

const pageSize = 20;
const filterNames = ["search", "riskLevel", "status", "contractType", "sort", "descending", "page"] as const;

// The address holds the filters, so a reload or a shared link shows the same list.
export async function clientLoader({ request }: Route.ClientLoaderArgs) {
  const url = new URL(request.url);
  const query = new URLSearchParams({ pageSize: String(pageSize) });
  for (const name of filterNames) {
    const value = url.searchParams.get(name);
    if (value) {
      query.set(name, value);
    }
  }

  return apiLoad<Schemas["LibraryPage"]>(`/contracts?${query}`);
}

const dateFormat = new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" });
const riskLevels: NonNullable<Schemas["RiskLevel"]>[] = ["critical", "high", "medium", "low"];
const riskNames: Record<NonNullable<Schemas["RiskLevel"]>, string> = { critical: "Nghiêm trọng", high: "Cao", medium: "Trung bình", low: "Thấp" };
const selectClass = "h-10 rounded-md border border-rule-strong bg-sheet px-2 text-body focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none";

// Contract library (F-CTR-003): a drop area right on the page (UI-GEN-003), then the contracts of the
// current workspace with search (FR-CTR-009), filters (FR-CTR-010), sort (FR-CTR-011) and pages.
export default function Contracts({ loaderData: library }: Route.ComponentProps) {
  const { revalidate } = useRevalidator();
  const [params, setParams] = useSearchParams();
  const navigation = useNavigation();
  const filtered = filterNames.some((name) => name !== "sort" && name !== "descending" && name !== "page" && params.get(name));
  const page = Number(library.page);
  const pages = Math.max(1, Math.ceil(Number(library.total) / Number(library.pageSize)));

  function goToPage(next: number) {
    setParams((current) => {
      current.set("page", String(next));
      return current;
    });
  }

  return (
    <section aria-labelledby="library-title" className="flex max-w-5xl flex-col gap-6">
      <h1 id="library-title" className="text-headline">
        Thư viện hợp đồng
      </h1>

      {/* Stays on the library: the new contract appears at the top of the list. */}
      <ContractDropzone onUploaded={revalidate} />

      <Form method="get" role="search" aria-label="Tìm và lọc hợp đồng" className="flex flex-wrap items-end gap-2"
        onChange={(e) => (e.target as HTMLElement).tagName === "SELECT" && e.currentTarget.requestSubmit()}
      >
        <div className="flex min-w-56 flex-1 flex-col gap-1">
          <label htmlFor="library-search" className="text-label">
            Tìm theo tên hoặc đối tác
          </label>
          <input
            id="library-search"
            name="search"
            type="search"
            placeholder="Gõ rồi nhấn Enter"
            defaultValue={params.get("search") ?? ""}
            className="h-10 rounded-md border border-rule-strong bg-sheet px-3 text-body focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none"
          />
        </div>
        <select name="riskLevel" aria-label="Mức rủi ro" defaultValue={params.get("riskLevel") ?? ""} className={selectClass}>
          <option value="">Mọi mức rủi ro</option>
          {riskLevels.map((level) => (
            <option key={level} value={level}>
              {riskNames[level]}
            </option>
          ))}
        </select>
        <select name="status" aria-label="Trạng thái duyệt" defaultValue={params.get("status") ?? ""} className={selectClass}>
          <option value="">Mọi trạng thái</option>
          {Object.entries(approvalStatusLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
        <select name="contractType" aria-label="Loại hợp đồng" defaultValue={params.get("contractType") ?? ""} className={selectClass}>
          <option value="">Mọi loại hợp đồng</option>
          {Object.entries(contractTypeLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
        <select name="sort" aria-label="Sắp xếp theo" defaultValue={params.get("sort") ?? "updated"} className={selectClass}>
          <option value="updated">Ngày cập nhật</option>
          <option value="name">Tên hợp đồng</option>
          <option value="score">Điểm rủi ro</option>
          <option value="status">Trạng thái duyệt</option>
        </select>
        <select name="descending" aria-label="Thứ tự" defaultValue={params.get("descending") ?? "true"} className={selectClass}>
          <option value="true">Giảm dần</option>
          <option value="false">Tăng dần</option>
        </select>
      </Form>

      {library.items.length === 0 ? (
        <p className="max-w-prose text-ink-secondary">
          {filtered ? (
            <>
              Không có hợp đồng nào khớp điều kiện tìm.{" "}
              <Link to="/" className="text-review-ink underline underline-offset-2">
                Bỏ mọi bộ lọc
              </Link>
            </>
          ) : (
            "Chưa có hợp đồng nào. Sau khi bạn tải file lên, hệ thống tách điều khoản và chỉ ra rủi ro tiềm ẩn cần xem xét."
          )}
        </p>
      ) : (
        <div className={`overflow-x-auto rounded-md border border-rule bg-sheet transition-opacity duration-150 ${navigation.state === "loading" ? "opacity-60" : ""}`}>
          <table className="w-full text-left">
            <caption className="sr-only">{library.total} hợp đồng</caption>
            <thead className="border-b border-rule text-label text-ink-secondary">
              <tr>
                <th scope="col" className="px-4 py-2.5 font-medium">
                  Hợp đồng
                </th>
                <th scope="col" className="px-4 py-2.5 font-medium whitespace-nowrap">
                  Rủi ro
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
              {library.items.map((contract) => (
                <tr key={contract.contractId} className="transition-colors duration-150 hover:bg-desk">
                  <td className="px-4 py-3">
                    <Link to={`/contracts/${contract.contractId}`} className="font-medium text-ink underline-offset-2 hover:underline">
                      {contract.contractName}
                    </Link>
                    <span className="block text-caption text-ink-secondary">
                      {[contract.counterpartyName, contract.contractType && contractTypeLabels[contract.contractType], contract.sharedWithMe && "Được chia sẻ với bạn"].filter(Boolean).join(" · ")}
                    </span>
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    {contract.riskLevel ? (
                      <span className="flex items-center gap-2">
                        <span className="tabular-nums">{contract.overallScore}</span>
                        <SeverityTag severity={contract.riskLevel} />
                      </span>
                    ) : (
                      <span className="text-ink-muted">Chưa có điểm</span>
                    )}
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

      {pages > 1 && (
        <nav aria-label="Phân trang" className="flex items-center gap-3">
          <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => goToPage(page - 1)}>
            Trang trước
          </Button>
          <span className="text-label text-ink-secondary tabular-nums">
            Trang {page} / {pages} · {library.total} hợp đồng
          </span>
          <Button variant="secondary" size="sm" disabled={page >= pages} onClick={() => goToPage(page + 1)}>
            Trang sau
          </Button>
        </nav>
      )}
    </section>
  );
}
