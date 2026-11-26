import type { Schemas } from "./api";
import { checkNames, checkStatusLabels, severityOrder } from "./labels";

type Result = Pick<Schemas["AnalysisResultView"], "score" | "findings" | "checks">;
type Severity = Schemas["Severity"];

export interface ScoreChange {
  domain: Schemas["RiskDomain"] | null; // null = overall score
  before: number | null;
  after: number | null;
}

export interface Change {
  key: string;
  title: string;
  detail: string;
  severity: Severity | null;
}

export interface Comparison {
  scores: ScoreChange[];
  weakened: Change[];
  improved: Change[];
}

const checkRank: Partial<Record<Schemas["CheckStatus"], number>> = { pass: 0, partial: 1, missing: 2 };
const worse = (a: Severity, b: Severity) => severityOrder.indexOf(a) < severityOrder.indexOf(b);
const severityName: Record<Severity, string> = { critical: "Nghiêm trọng", high: "Cao", medium: "Trung bình", low: "Thấp" };

// A finding is the same across versions when it comes from the same check, otherwise when it has the
// same domain and category: clause keys and finding keys are new in every run.
const findingKey = (f: Schemas["FindingItem"]) => f.checkCode ?? `${f.primaryDomain}|${f.category ?? f.title}`;

// F-CMP-001, FR-CTR-020: what got weaker from one analysed version to the next, and how the score moved.
// Built from the two stored results, so it needs no model: a check that got worse, a finding that is new
// or more severe. The reverse is listed as improved.
export function compareResults(before: Result, after: Result): Comparison {
  // The API types int32 as number | string (OpenAPI), as for page regions.
  const num = (value: number | string | null | undefined) => (value === null || value === undefined ? null : Number(value));
  const scores: ScoreChange[] = [
    { domain: null, before: num(before.score?.overallScore), after: num(after.score?.overallScore) },
    ...(after.score?.domains ?? before.score?.domains ?? []).map((d) => ({
      domain: d.domain,
      before: num(before.score?.domains.find((x) => x.domain === d.domain)?.score),
      after: num(after.score?.domains.find((x) => x.domain === d.domain)?.score),
    })),
  ];

  const weakened: Change[] = [];
  const improved: Change[] = [];

  for (const check of after.checks) {
    const old = before.checks.find((c) => c.checkCode === check.checkCode);
    const from = old && checkRank[old.status];
    const to = checkRank[check.status];
    if (from === undefined || to === undefined || from === to) {
      continue;
    }

    const change: Change = {
      key: `check-${check.checkCode}`,
      title: `${check.checkCode} · ${checkNames[check.checkCode] ?? check.checkCode}`,
      detail: `${checkStatusLabels[old!.status]} → ${checkStatusLabels[check.status]}`,
      severity: null,
    };
    (to > from ? weakened : improved).push(change);
  }

  // Several findings can share a key (two clauses of one category), so they are compared as groups: the worst
  // severity of each group, then how many there are.
  const group = (findings: Schemas["FindingItem"][]) => {
    const groups = new Map<string, Schemas["FindingItem"][]>();
    for (const f of findings) {
      groups.set(findingKey(f), [...(groups.get(findingKey(f)) ?? []), f]);
    }

    return groups;
  };
  const worst = (findings: Schemas["FindingItem"][]) =>
    findings.reduce((w, f) => (worse(f.severity, w.severity) ? f : w));
  const oldGroups = group(before.findings);
  const newGroups = group(after.findings);
  for (const key of new Set([...oldGroups.keys(), ...newGroups.keys()])) {
    const olds = oldGroups.get(key) ?? [];
    const news = newGroups.get(key) ?? [];
    const id = `finding-${key}`;
    if (olds.length === 0) {
      const f = worst(news);
      weakened.push({ key: id, title: f.title, detail: news.length > 1 ? `Phát sinh ở phiên bản sau (${news.length} finding)` : "Phát sinh ở phiên bản sau", severity: f.severity });
    } else if (news.length === 0) {
      const f = worst(olds);
      improved.push({ key: id, title: f.title, detail: "Không còn ở phiên bản sau", severity: f.severity });
    } else {
      const from = worst(olds);
      const to = worst(news);
      if (worse(to.severity, from.severity)) {
        weakened.push({ key: id, title: to.title, detail: `${severityName[from.severity]} → ${severityName[to.severity]}`, severity: to.severity });
      } else if (worse(from.severity, to.severity)) {
        improved.push({ key: id, title: to.title, detail: `${severityName[from.severity]} → ${severityName[to.severity]}`, severity: to.severity });
      } else if (news.length > olds.length) {
        weakened.push({ key: id, title: to.title, detail: `${olds.length} → ${news.length} finding`, severity: to.severity });
      } else if (news.length < olds.length) {
        improved.push({ key: id, title: to.title, detail: `${olds.length} → ${news.length} finding`, severity: to.severity });
      }
    }
  }

  const bySeverity = (a: Change, b: Change) => severityOrder.indexOf(a.severity ?? "low") - severityOrder.indexOf(b.severity ?? "low");
  return { scores, weakened: weakened.sort(bySeverity), improved: improved.sort(bySeverity) };
}
