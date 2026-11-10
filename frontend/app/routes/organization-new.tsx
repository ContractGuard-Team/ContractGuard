import { useState, type FormEvent } from "react";
import { useNavigate, useRevalidator } from "react-router";

import { Button } from "~/components/Button";
import { TextField } from "~/components/Field";
import { Notice } from "~/components/Notice";
import { apiPost, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { useAction } from "~/lib/useAction";
import { setWorkspace } from "~/lib/workspace";

import type { Route } from "./+types/organization-new";

export function meta(_: Route.MetaArgs) {
  return [{ title: "Tạo tổ chức · ContractGuard" }];
}

// FR-ORG-001: the creator becomes its Owner; the app then switches into the new organization.
export default function OrganizationNew() {
  const [name, setName] = useState("");
  const create = useAction();
  const navigate = useNavigate();
  const { revalidate } = useRevalidator();

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    void create.run(async () => {
      const created = await apiPost<{ organizationId: string }>("/organizations", { name: name.trim() } satisfies Schemas["OrganizationNameRequest"]);
      setWorkspace(created.organizationId);
      await navigate("/organization");
      await revalidate();
    });
  }

  return (
    <section aria-labelledby="new-org-title" className="flex max-w-md flex-col gap-4">
      <h1 id="new-org-title" className="text-headline">
        Tạo tổ chức
      </h1>
      <p className="text-ink-secondary">
        Hợp đồng trong tổ chức thuộc về tổ chức. Bạn là Owner và có thể mời đồng nghiệp tham gia. Không gian cá nhân của bạn vẫn giữ nguyên.
      </p>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        {create.error !== null && <Notice tone="error">{errorMessage(create.error)}</Notice>}
        <TextField label="Tên tổ chức" required maxLength={200} value={name} onChange={(e) => setName(e.target.value)} />
        <Button type="submit" loading={create.pending} disabled={!name.trim()} className="self-start">
          Tạo tổ chức
        </Button>
      </form>
    </section>
  );
}
