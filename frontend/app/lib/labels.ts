import type { Schemas } from "./api";

// Vietnamese names of API values. Clause types: BR-RISK-006. Statuses: SRS §13.1. Steps: FR-ANA-002.
export const clauseTypeLabels: Record<string, string> = {
  C01: "Phạm vi công việc",
  C02: "Thanh toán",
  C03: "Phí và điều chỉnh giá",
  C04: "Trễ hạn và phạt vi phạm",
  C05: "Thời hạn và gia hạn",
  C06: "Chấm dứt hợp đồng",
  C07: "Giới hạn trách nhiệm",
  C08: "Bồi thường thiệt hại",
  C09: "Bảo hành và khước từ",
  C10: "Luật áp dụng và tranh chấp",
  C11: "Cam kết dịch vụ",
  C12: "Nghiệm thu",
  C13: "Bất khả kháng",
  C14: "Bảo mật thông tin",
  C15: "Sở hữu và xử lý dữ liệu",
  C16: "Tiêu chuẩn an toàn thông tin",
  C17: "Thông báo sự cố",
  C18: "Lưu trữ và xóa dữ liệu",
  C19: "Nhà thầu phụ",
  C20: "Quyền kiểm toán đối tác",
  C21: "Tuân thủ, thuế, hóa đơn, giấy phép",
  C99: "Khác",
};

// CR-GEN-002: one name per role, the same on the organization page and in the share dialog.
export const orgRoleLabels: Record<NonNullable<Schemas["OrgRole"]>, string> = { owner: "Quản trị viên", member: "Thành viên" };
export const contractAccessLabels = { owner: "Người phụ trách", administrator: orgRoleLabels.owner, shared: "Được chia sẻ" } as const;

export const approvalStatusLabels:Record<Schemas["ApprovalStatus"], string> = {
  new: "Mới",
  analyzing: "Đang phân tích",
  pending_review: "Chờ review",
  negotiating: "Đang đàm phán",
  approved: "Đã duyệt",
  rejected: "Từ chối",
};

export const analysisSteps: { step: Schemas["AnalysisStep"]; label: string }[] = [
  { step: "reading_file", label: "Đọc file" },
  { step: "splitting_clauses", label: "Tách điều khoản" },
  { step: "evaluating", label: "Đánh giá" },
  { step: "scoring", label: "Chấm điểm" },
  { step: "completed", label: "Hoàn tất" },
];

// docs/04 C8: stage codes of the AI worker's own job table, in words. They are the worker's internal names
// (e.g. "agent_2_risk_scoring.tool.web_search"), so an unknown one gives null and the page shows the percent only.
const workerStages: Record<string, string> = {
  received: "Đang chờ đến lượt",
  downloading: "Đang đọc file",
  dispatching: "Đang đọc file",
  processing: "Đang đọc file",
  saving_result: "Đang lưu kết quả",
};
const workerAgents: [prefix: string, label: string][] = [
  ["agent_1", "Đang tách và phân loại điều khoản"],
  ["agent_2", "Đang đánh giá rủi ro"],
  ["agent_3", "Đang soạn đề xuất"],
];
const workerTools: [prefix: string, label: string][] = [
  ["web_search", "tra cứu web"],
  ["legal_search", "tìm văn bản pháp luật liên quan"],
  ["qdrant", "tìm văn bản pháp luật liên quan"],
  ["supabase", "tra cứu văn bản pháp luật"],
  ["partner_document", "đọc lại hợp đồng"],
  ["s3_list_objects", "tìm tài liệu"],
];

export function workerActivityLabel(stage: string): string | null {
  if (Object.hasOwn(workerStages, stage)) {
    return workerStages[stage] ?? null;
  }
  const agent = workerAgents.find(([prefix]) => stage.startsWith(prefix))?.[1];
  const toolName = stage.split("tool.")[1];
  const tool = toolName === undefined ? undefined : workerTools.find(([prefix]) => toolName.startsWith(prefix))?.[1];
  if (agent && tool) {
    return `${agent}: ${tool}`;
  }
  return agent ?? (tool ? `Đang ${tool}` : null);
}

// The step of the list a worker stage belongs to, so the list and the line under it never disagree.
// Any other code counts as the first step; the page takes the later of this and the API's own step.
export function workerActivityStep(stage: string): Schemas["AnalysisStep"] {
  const agent = /^agent_([123])/.exec(stage)?.[1];
  if (agent) {
    return (["splitting_clauses", "evaluating", "scoring"] as const)[Number(agent) - 1] ?? "reading_file";
  }
  return stage === "saving_result" ? "scoring" : "reading_file";
}

export const ownPartyLabels: Record<Schemas["OwnParty"], string> = {
  buyer: "Bên mua / khách hàng",
  seller: "Bên bán / nhà cung cấp",
};

export const contractTypeLabels: Record<NonNullable<Schemas["ContractType"]>, string> = {
  service_agreement: "Hợp đồng dịch vụ",
  vendor_agreement: "Hợp đồng với nhà cung cấp",
  purchase_agreement: "Hợp đồng mua bán",
  msa: "Hợp đồng khung (MSA)",
  sow: "Phụ lục công việc (SOW)",
  other: "Khác",
};

export const criticalityLabels: Record<NonNullable<Schemas["ServiceCriticality"]>, string> = {
  low: "Thấp",
  medium: "Trung bình",
  high: "Cao",
};

// BR-RISK-001 (DATA-DATA-008).
export const riskDomainLabels: Record<Schemas["RiskDomain"], string> = {
  legal: "Pháp lý",
  financial: "Tài chính",
  operational: "Vận hành",
  compliance: "Tuân thủ",
  cybersecurity: "An ninh mạng",
  data_privacy: "Dữ liệu và quyền riêng tư",
  third_party: "Bên thứ ba",
};

// BR-RISK-007: shown as the SRS names them.
export const checkStatusLabels: Record<Schemas["CheckStatus"], string> = {
  pass: "PASS",
  partial: "PARTIAL",
  missing: "MISSING",
  not_applicable: "N/A",
};

// DATA-DATA-012.
export const handlingStatusLabels: Record<Schemas["HandlingStatus"], string> = {
  open: "Chưa xử lý",
  risk_accepted: "Chấp nhận rủi ro",
  needs_negotiation: "Cần đàm phán",
  false_positive: "Không đúng",
  resolved: "Đã xử lý",
};

// BR-RISK-007: the 20 checks.
export const checkNames: Record<string, string> = {
  "CY-01": "Yêu cầu bảo mật đo lường được",
  "CY-02": "Mã hóa dữ liệu at-rest và in-transit",
  "CY-03": "Kiểm soát truy cập, least privilege",
  "CY-04": "Xác thực đa yếu tố (MFA)",
  "CY-05": "Ghi log và giám sát an ninh, thời gian lưu log",
  "CY-06": "Quản lý lỗ hổng, SLA vá lỗi",
  "CY-07": "Kiểm thử bảo mật (pentest, đánh giá định kỳ)",
  "CY-08": "Thời hạn thông báo sự cố (24/48/72h)",
  "CY-09": "Quy trình ứng phó và leo thang sự cố",
  "CY-10": "Sao lưu, khôi phục thảm họa, liên tục kinh doanh (RTO/RPO)",
  "CY-11": "Chứng chỉ, chuẩn tuân thủ (ISO 27001, SOC 2)",
  "CY-12": "Trách nhiệm khi xảy ra sự cố bảo mật",
  "PR-01": "Quyền sở hữu dữ liệu thuộc khách hàng",
  "PR-02": "Giới hạn mục đích xử lý dữ liệu",
  "PR-03": "Vị trí lưu trữ, chuyển dữ liệu ra nước ngoài",
  "PR-04": "Thời hạn lưu trữ dữ liệu",
  "PR-05": "Xóa hoặc trả dữ liệu và thu hồi truy cập khi chấm dứt",
  "TP-01": "Kiểm soát nhà thầu phụ",
  "TP-02": "Nghĩa vụ bảo mật áp xuống nhà thầu phụ",
  "TP-03": "Quyền kiểm toán bảo mật của khách hàng",
};

export const severityOrder: Schemas["Severity"][] = ["critical", "high", "medium", "low"];
