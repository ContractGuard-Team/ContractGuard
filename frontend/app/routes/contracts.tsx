import { Link } from "react-router";

import type { Route } from "./+types/contracts";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Thư viện hợp đồng · ContractGuard" }];
}

// Contract library (F-CTR-003). Phase 0: empty state only; Phase 1 loads the list from the API.
export default function Contracts() {
  return (
    <section aria-labelledby="library-title" className="max-w-4xl">
      <h1 id="library-title" className="text-headline">
        Thư viện hợp đồng
      </h1>

      {/* Empty state: say what to do next and offer one button (DESIGN §5 Feedback). */}
      <div className="mt-6 rounded-md border border-rule bg-sheet p-8">
        <h2 className="text-title">Chưa có hợp đồng nào</h2>
        <p className="mt-1 max-w-prose text-ink-secondary">
          Tải lên một hợp đồng PDF, DOCX hoặc ảnh chụp (tối đa 20 MB, 100 trang). Hệ thống tách điều khoản và chỉ ra rủi ro tiềm ẩn cần xem xét.
        </p>
        <Link
          to="/contracts/new"
          className="mt-4 inline-flex h-10 items-center rounded-md bg-review-ink px-4 text-button text-sheet transition-colors duration-150 hover:bg-review-ink-hover"
        >
          Tải hợp đồng đầu tiên
        </Link>
      </div>
    </section>
  );
}
