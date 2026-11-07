import { useNavigate } from "react-router";

import { ContractDropzone } from "~/components/ContractDropzone";

import type { Route } from "./+types/contracts-new";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Tải hợp đồng lên · ContractGuard" }];
}

// UC-CTR-001, step 1. The next step, declaring the context, is in the workspace of the new contract.
export default function NewContract() {
  const navigate = useNavigate();

  return (
    <section aria-labelledby="new-title" className="flex max-w-4xl flex-col gap-6">
      <h1 id="new-title" className="text-headline">
        Tải hợp đồng lên
      </h1>
      <ContractDropzone onUploaded={async (contractId) => navigate(`/contracts/${contractId}`)} />
      <p className="max-w-prose text-ink-secondary">Sau khi tải file lên, trang hợp đồng sẽ mở ra để bạn xem file và khai báo ngữ cảnh.</p>
    </section>
  );
}
