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

export const approvalStatusLabels: Record<Schemas["ApprovalStatus"], string> = {
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

export const ownPartyLabels: Record<Schemas["OwnParty"], string> = {
  buyer: "Bên mua / khách hàng",
  seller: "Bên bán / nhà cung cấp",
};

export const contractTypeLabels: Record<Schemas["ContractType"], string> = {
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
