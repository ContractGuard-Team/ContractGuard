import { describe, expect, it } from "vitest";

import { workerActivityLabel, workerActivityStep } from "./labels";

describe("workerActivityStep", () => {
  it("puts each agent on its step and anything else on the first", () => {
    expect(workerActivityStep("agent_1_classification.chunk")).toBe("splitting_clauses");
    expect(workerActivityStep("agent_2_risk_scoring.tool.web_search")).toBe("evaluating");
    expect(workerActivityStep("agent_3")).toBe("scoring");
    expect(workerActivityStep("saving_result")).toBe("scoring");
    expect(workerActivityStep("downloading")).toBe("reading_file");
    expect(workerActivityStep("tool.partner_document")).toBe("reading_file");
  });
});

// docs/04 C8: stage codes as the AI worker writes them in its job table.
describe("workerActivityLabel", () => {
  it("names the agent and the tool it is using", () => {
    expect(workerActivityLabel("agent_2_risk_scoring.tool.web_search")).toBe("Đang đánh giá rủi ro: tra cứu web");
    expect(workerActivityLabel("agent_3_recommendation.tool.supabase_select")).toBe("Đang soạn đề xuất: tra cứu văn bản pháp luật");
  });

  it("names a whole agent or a fixed stage", () => {
    expect(workerActivityLabel("agent_1")).toBe("Đang tách và phân loại điều khoản");
    expect(workerActivityLabel("processing")).toBe("Đang đọc file");
  });

  it("names a tool used outside an agent", () => {
    expect(workerActivityLabel("tool.partner_document")).toBe("Đang đọc lại hợp đồng");
  });

  it("gives nothing for a code it does not know, or an inherited property name", () => {
    expect(workerActivityLabel("monitoring_persist")).toBeNull();
    expect(workerActivityLabel("constructor")).toBeNull();
  });
});
