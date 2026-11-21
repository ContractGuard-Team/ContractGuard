import { useEffect, useState } from "react";

import { apiFetch, type Schemas } from "~/lib/api";
import { analysisSteps, workerActivityLabel } from "~/lib/labels";

const pollMs = 3000;

// F-ANA-001, FR-ANA-002: the current step of a running analysis, asked for every 3 seconds (SD-15).
// When the job has ended, or can no longer be read, the parent loads the page data again.
// docs/04 C8: when the API can read the AI worker's own progress, a bar and a line say what it is doing now.
export function AnalysisProgress({ job, onFinished }: { job: Schemas["JobStatus"]; onFinished: () => void }) {
  const [step, setStep] = useState(job.step);
  const [activity, setActivity] = useState(job.activity ?? null);

  useEffect(() => {
    const timer = setInterval(() => {
      apiFetch<Schemas["JobStatus"]>(`/jobs/${job.jobId}`)
        .then((latest) => {
          setStep(latest.step);
          setActivity(latest.activity ?? null);
          if (latest.status !== "running") {
            onFinished();
          }
        })
        .catch(onFinished);
    }, pollMs);
    return () => clearInterval(timer);
  }, [job.jobId, onFinished]);

  const current = analysisSteps.findIndex((s) => s.step === step);
  const currentLabel = analysisSteps[current]?.label ?? "";

  return (
    <section aria-labelledby="progress-title" className="max-w-xl rounded-md border border-rule bg-sheet p-6">
      <h2 id="progress-title" className="text-title">
        Đang phân tích hợp đồng
      </h2>
      <p className="mt-1 text-ink-secondary">Bạn có thể rời trang này. Phân tích vẫn tiếp tục và kết quả sẽ có ở đây khi xong.</p>

      <p role="status" className="sr-only">
        Bước hiện tại: {currentLabel}
      </p>
      {activity && <WorkerActivityBar percent={Number(activity.percent)} stage={activity.stage} />}
      <ol className="mt-4 flex flex-col gap-1">
        {analysisSteps.map(({ step: item, label }, index) => {
          const state = index < current ? "done" : index === current ? "current" : "todo";
          return (
            <li key={item} className="flex h-8 items-center gap-2.5" aria-current={state === "current" ? "step" : undefined}>
              <StepIcon state={state} />
              <span className={state === "todo" ? "text-ink-muted" : state === "current" ? "font-semibold text-ink" : "text-ink-secondary"}>{label}</span>
            </li>
          );
        })}
      </ol>
    </section>
  );
}

// 8px bar like the risk ruler (DESIGN §5). Neutral ink, not review-ink: progress is not an action (Action-Only Rule).
// The line under it is not announced on every poll: the step list above already has role="status".
function WorkerActivityBar({ percent, stage }: { percent: number; stage: string }) {
  const label = workerActivityLabel(stage);
  return (
    <div className="mt-4">
      <div
        role="progressbar"
        aria-label="Tiến độ phân tích"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent}
        className="h-2 overflow-hidden rounded-full bg-rule"
      >
        <div
          className="h-full rounded-full bg-ink-secondary transition-[width] duration-200 ease-[cubic-bezier(0.25,1,0.5,1)] motion-reduce:transition-none"
          style={{ width: `${percent}%` }}
        />
      </div>
      <p className="mt-2 flex justify-between gap-4 text-sm text-ink-secondary">
        <span>{label ?? "AI đang làm việc"}</span>
        <span className="tabular-nums">{percent}%</span>
      </p>
    </div>
  );
}

function StepIcon({ state }: { state: "done" | "current" | "todo" }) {
  if (state === "current") {
    return (
      <svg className="size-4 animate-spin text-review-ink" viewBox="0 0 16 16" aria-hidden="true">
        <circle cx="8" cy="8" r="6" fill="none" stroke="currentColor" strokeOpacity="0.25" strokeWidth="2" />
        <path d="M14 8a6 6 0 0 0-6-6" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
      </svg>
    );
  }

  return state === "done" ? (
    <svg className="size-4 text-ink-secondary" viewBox="0 0 16 16" aria-hidden="true">
      <path d="m3.5 8.5 3 3 6-7" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  ) : (
    <svg className="size-4 text-rule-strong" viewBox="0 0 16 16" aria-hidden="true">
      <circle cx="8" cy="8" r="5.5" fill="none" stroke="currentColor" strokeWidth="1.5" />
    </svg>
  );
}
