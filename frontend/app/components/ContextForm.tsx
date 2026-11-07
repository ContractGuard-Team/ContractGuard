import { useState, type FormEvent } from "react";

import type { Schemas } from "~/lib/api";
import { apiPut } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { contractTypeLabels, criticalityLabels, ownPartyLabels } from "~/lib/labels";
import { useAction } from "~/lib/useAction";

import { Button } from "./Button";
import { SelectField, TextField } from "./Field";
import { Notice } from "./Notice";

type Errors = Partial<Record<"ownParty" | "contractType" | "hasPersonalData", string>>;

// FR-CTR-005: confirm or correct what the contract is about. When an analysis service is deployed,
// confirming also starts the analysis (FR-ANA-001).
export function ContextForm({
  contractId,
  versionId,
  initial,
  analysisAvailable,
  onSaved,
  onCancel,
}: {
  contractId: string;
  versionId: string;
  initial: Schemas["ContextDetails"] | null;
  analysisAvailable: boolean;
  onSaved: () => void;
  onCancel: () => void;
}) {
  const [ownParty, setOwnParty] = useState(initial?.ownParty ?? "");
  const [contractType, setContractType] = useState(initial?.contractType ?? "");
  const [hasPersonalData, setHasPersonalData] = useState(initial === null ? "" : String(initial.hasPersonalData));
  const [counterpartyName, setCounterpartyName] = useState(initial?.counterpartyName ?? "");
  const [counterpartyOffering, setCounterpartyOffering] = useState(initial?.counterpartyOffering ?? "");
  const [serviceCriticality, setServiceCriticality] = useState(initial?.serviceCriticality ?? "");
  const [errors, setErrors] = useState<Errors>({});
  const submit = useAction();

  function onSubmit(event: FormEvent) {
    event.preventDefault();

    // VAL-DATA-010: required fields, named one by one.
    const missing: Errors = {
      ...(ownParty ? {} : { ownParty: "Hãy chọn vị trí của bạn trong hợp đồng." }),
      ...(contractType ? {} : { contractType: "Hãy chọn loại hợp đồng." }),
      ...(hasPersonalData ? {} : { hasPersonalData: "Hãy chọn có hoặc không." }),
    };
    setErrors(missing);
    if (Object.keys(missing).length > 0) {
      return;
    }

    void submit.run(async () => {
      await apiPut(`/contracts/${contractId}/versions/${versionId}/context`, {
        ownParty,
        contractType,
        hasPersonalData: hasPersonalData === "true",
        counterpartyName: counterpartyName || null,
        counterpartyOffering: counterpartyOffering || null,
        serviceCriticality: serviceCriticality || null,
      });
      onSaved();
    });
  }

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
      <p className="text-ink-secondary">Rủi ro được đánh giá theo vị trí của bạn trong hợp đồng, nên hãy kiểm tra kỹ phần này.</p>

      <RadioGroup
        legend="Bạn là bên nào trong hợp đồng?"
        name="ownParty"
        value={ownParty}
        onChange={setOwnParty}
        error={errors.ownParty}
        options={Object.entries(ownPartyLabels).map(([value, label]) => ({ value, label }))}
      />

      <SelectField label="Loại hợp đồng" value={contractType} onChange={(e) => setContractType(e.target.value)} error={errors.contractType}>
        <option value="">Chọn loại hợp đồng</option>
        {Object.entries(contractTypeLabels).map(([value, label]) => (
          <option key={value} value={value}>
            {label}
          </option>
        ))}
      </SelectField>

      <RadioGroup
        legend="Hợp đồng có xử lý dữ liệu cá nhân hoặc dữ liệu nhạy cảm không?"
        name="hasPersonalData"
        value={hasPersonalData}
        onChange={setHasPersonalData}
        error={errors.hasPersonalData}
        options={[
          { value: "true", label: "Có" },
          { value: "false", label: "Không" },
        ]}
      />

      <TextField
        label="Tên đối tác (không bắt buộc)"
        value={counterpartyName}
        maxLength={300}
        onChange={(e) => setCounterpartyName(e.target.value)}
      />

      <TextField
        label="Đối tác cung cấp gì? (không bắt buộc)"
        hint="Ví dụ: dịch vụ lưu trữ đám mây, phần mềm kế toán"
        value={counterpartyOffering}
        maxLength={2000}
        onChange={(e) => setCounterpartyOffering(e.target.value)}
      />

      <SelectField label="Mức độ quan trọng của dịch vụ (không bắt buộc)" value={serviceCriticality} onChange={(e) => setServiceCriticality(e.target.value)}>
        <option value="">Chưa xác định</option>
        {Object.entries(criticalityLabels).map(([value, label]) => (
          <option key={value} value={value}>
            {label}
          </option>
        ))}
      </SelectField>

      {submit.error !== null && <Notice tone="error">{errorMessage(submit.error)}</Notice>}

      <div className="flex flex-wrap gap-2">
        <Button type="submit" loading={submit.pending}>
          {analysisAvailable ? "Xác nhận và bắt đầu phân tích" : "Lưu ngữ cảnh"}
        </Button>
        <Button variant="ghost" onClick={onCancel}>
          Đóng
        </Button>
      </div>
    </form>
  );
}

function RadioGroup({
  legend,
  name,
  value,
  onChange,
  error,
  options,
}: {
  legend: string;
  name: string;
  value: string;
  onChange: (value: string) => void;
  error: string | undefined;
  options: { value: string; label: string }[];
}) {
  const messageId = `${name}-message`;
  return (
    <fieldset aria-describedby={error ? messageId : undefined}>
      <legend className="mb-1 text-label text-ink">{legend}</legend>
      <div className="flex flex-wrap gap-x-6">
        {options.map((option) => (
          <label key={option.value} className="flex h-10 cursor-pointer items-center gap-2">
            <input
              type="radio"
              name={name}
              value={option.value}
              checked={value === option.value}
              onChange={() => onChange(option.value)}
              className="size-4 accent-review-ink"
            />
            {option.label}
          </label>
        ))}
      </div>
      {error && (
        <p id={messageId} className="text-caption text-severity-critical">
          {error}
        </p>
      )}
    </fieldset>
  );
}
