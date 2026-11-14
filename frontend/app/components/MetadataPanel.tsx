import { useState, type FormEvent } from "react";

import { apiPut, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { contractTypeLabels } from "~/lib/labels";
import { useAction } from "~/lib/useAction";

import { Button } from "./Button";
import { SelectField, TextField } from "./Field";
import { Notice } from "./Notice";

type Metadata = Schemas["ContractMetadata"];

const money = new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 2 });
const date = (value: string | null) => (value ? new Intl.DateTimeFormat("vi-VN").format(new Date(`${value}T00:00:00`)) : null);

// F-CTR-002: metadata of the version, edited in place (DESIGN: no modal when it can be done here).
// The user's edit is shown when there is one; the extracted values stay stored next to it.
export function MetadataPanel({
  contractId,
  versionId,
  extracted,
  edited,
  onSaved,
}: {
  contractId: string;
  versionId: string;
  extracted: Metadata | null;
  edited: Metadata | null;
  onSaved: () => void;
}) {
  const current = edited ?? extracted;
  const [editing, setEditing] = useState(false);

  return (
    <section aria-labelledby="metadata-title" className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 id="metadata-title" className="text-title">
          Thông tin hợp đồng
        </h2>
        {!editing && (
          <Button variant="secondary" size="sm" onClick={() => setEditing(true)}>
            Sửa
          </Button>
        )}
      </div>

      {editing ? (
        <MetadataForm
          contractId={contractId}
          versionId={versionId}
          initial={current}
          onDone={(saved) => {
            setEditing(false);
            if (saved) {
              onSaved();
            }
          }}
        />
      ) : (
        <>
          <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-1.5 text-body">
            <Row term="Các bên" value={current?.parties.length ? current.parties.join("; ") : null} />
            <Row term="Loại hợp đồng" value={current?.contractType ? contractTypeLabels[current.contractType] : null} />
            <Row term="Giá trị" value={current?.contractValue != null ? `${money.format(Number(current.contractValue))} ${current.currency ?? ""}`.trim() : null} />
            <Row term="Ngày hiệu lực" value={date(current?.effectiveDate ?? null)} />
            <Row term="Ngày hết hạn" value={date(current?.expiryDate ?? null)} />
            <Row term="Luật áp dụng" value={current?.governingLaw ?? null} />
          </dl>
          <p className="text-caption text-ink-muted">
            {edited ? "Đã được sửa thủ công. Giá trị AI trích xuất vẫn được lưu lại." : extracted ? "Do AI trích xuất từ hợp đồng, nên kiểm tra lại." : "Chưa có thông tin trích xuất. Bạn có thể tự điền."}
          </p>
        </>
      )}
    </section>
  );
}

function Row({ term, value }: { term: string; value: string | null }) {
  return (
    <>
      <dt className="text-ink-secondary">{term}</dt>
      <dd className={value ? "text-ink" : "text-ink-muted"}>{value ?? "Không rõ"}</dd>
    </>
  );
}

function MetadataForm({
  contractId,
  versionId,
  initial,
  onDone,
}: {
  contractId: string;
  versionId: string;
  initial: Metadata | null;
  onDone: (saved: boolean) => void;
}) {
  const [parties, setParties] = useState(initial?.parties.join("\n") ?? "");
  const [contractType, setContractType] = useState<NonNullable<Schemas["ContractType"]> | "">(initial?.contractType ?? "");
  const [value, setValue] = useState(initial?.contractValue != null ? String(initial.contractValue) : "");
  const [currency, setCurrency] = useState(initial?.currency ?? "");
  const [effectiveDate, setEffectiveDate] = useState(initial?.effectiveDate ?? "");
  const [expiryDate, setExpiryDate] = useState(initial?.expiryDate ?? "");
  const [governingLaw, setGoverningLaw] = useState(initial?.governingLaw ?? "");
  const save = useAction();

  const currencyError = currency && !/^[A-Za-z]{3}$/.test(currency.trim()) ? "Mã 3 chữ cái theo ISO 4217, ví dụ VND, USD." : undefined;
  const valueError = value && (Number.isNaN(Number(value)) || Number(value) < 0) ? "Nhập một số không âm." : undefined;

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (currencyError || valueError) {
      return;
    }

    void save.run(async () => {
      await apiPut(`/contracts/${contractId}/versions/${versionId}/metadata`, {
        parties: parties.split("\n").map((p) => p.trim()).filter(Boolean),
        contractType: contractType || null,
        contractValue: value ? Number(value) : null,
        currency: currency.trim() || null,
        effectiveDate: effectiveDate || null,
        expiryDate: expiryDate || null,
        governingLaw: governingLaw.trim() || null,
      } satisfies Schemas["MetadataRequest"]);
      onDone(true);
    });
  }

  return (
    <form onSubmit={onSubmit} className="grid gap-3 sm:grid-cols-2" noValidate>
      <div className="sm:col-span-2">
        <label htmlFor="metadata-parties" className="mb-1 block text-label">
          Các bên
        </label>
        <textarea
          id="metadata-parties"
          rows={2}
          value={parties}
          onChange={(e) => setParties(e.target.value)}
          aria-describedby="metadata-parties-hint"
          className="w-full rounded-md border border-rule-strong bg-sheet px-3 py-2 text-body focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none"
        />
        <p id="metadata-parties-hint" className="mt-1 text-caption text-ink-muted">
          Mỗi bên một dòng.
        </p>
      </div>
      <SelectField label="Loại hợp đồng" value={contractType} onChange={(e) => setContractType(e.target.value as NonNullable<Schemas["ContractType"]>)}>
        <option value="">Không rõ</option>
        {Object.entries(contractTypeLabels).map(([key, label]) => (
          <option key={key} value={key}>
            {label}
          </option>
        ))}
      </SelectField>
      <TextField label="Luật áp dụng" value={governingLaw} maxLength={200} onChange={(e) => setGoverningLaw(e.target.value)} />
      <TextField label="Giá trị" inputMode="decimal" value={value} error={valueError} onChange={(e) => setValue(e.target.value)} />
      <TextField label="Đồng tiền" value={currency} maxLength={3} error={currencyError} hint="Ví dụ VND, USD" onChange={(e) => setCurrency(e.target.value.toUpperCase())} />
      <TextField label="Ngày hiệu lực" type="date" value={effectiveDate} onChange={(e) => setEffectiveDate(e.target.value)} />
      <TextField label="Ngày hết hạn" type="date" value={expiryDate} onChange={(e) => setExpiryDate(e.target.value)} />
      {save.error !== null && (
        <div className="sm:col-span-2">
          <Notice tone="error">{errorMessage(save.error)}</Notice>
        </div>
      )}
      <div className="flex gap-2 sm:col-span-2">
        <Button type="submit" size="sm" loading={save.pending}>
          Lưu thông tin
        </Button>
        <Button variant="ghost" size="sm" onClick={() => onDone(false)}>
          Hủy
        </Button>
      </div>
    </form>
  );
}
