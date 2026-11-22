import { ApiError } from "./api";

// Vietnamese text for the ERR-* codes of SRS §14.1. The API's own text is English and for logs only.
const messages: Record<string, string> = {
  "ERR-AUTH-001": "Email này đã được đăng ký. Hãy đăng nhập hoặc đặt lại mật khẩu.",
  "ERR-AUTH-002": "Mật khẩu cần ít nhất 8 ký tự, có cả chữ và số.",
  "ERR-AUTH-003": "Email hoặc mật khẩu không đúng.",
  "ERR-AUTH-004": "Liên kết không hợp lệ hoặc đã hết hạn.",
  "ERR-AUTH-005": "Email này tạm bị khóa đăng nhập vì nhập sai nhiều lần. Hãy thử lại sau 15 phút hoặc đặt lại mật khẩu.",
  "ERR-AUTH-006": "Bạn thao tác quá nhanh. Hãy thử lại sau 1 phút.",
  "ERR-UPL-001": "Chỉ nhận file PDF hoặc DOCX.",
  "ERR-UPL-002": "Nội dung file không khớp với phần mở rộng của tên file.",
  "ERR-UPL-003": "File lớn hơn giới hạn 20 MB.",
  "ERR-UPL-004": "File có nhiều hơn giới hạn 100 trang.",
  "ERR-UPL-005": "Phát hiện mã độc trong file, nên file đã bị từ chối.",
  "ERR-SEC-001": "Bạn không có quyền thực hiện thao tác này.",
  "ERR-ORG-002": "Liên kết mời không còn hiệu lực.",
  "ERR-ORG-003": "Tổ chức phải luôn còn ít nhất một Owner.",
  "ERR-SHARE-001": "Hợp đồng của tổ chức chỉ chia sẻ được cho thành viên của tổ chức đó.",
  "ERR-SHARE-002": "Email này chưa có tài khoản ContractGuard.",
  "ERR-SHARE-003": "Người này là quản trị viên của tổ chức, đã có toàn quyền trên hợp đồng.",
  "ERR-CTR-001": "Không thể chuyển sang trạng thái này.",
  "ERR-CTR-002": "Hãy điền đủ các trường bắt buộc.",
  "ERR-CTR-003": "Hợp đồng đang được phân tích. Hãy chờ phân tích xong rồi thử lại.",
  "ERR-ANA-001": "Chưa bắt đầu được phân tích. Vui lòng thử lại.",
  "ERR-EXP-001": "Tạo báo cáo thất bại. Vui lòng thử lại.",
  "ERR-CHAT-001": "Trợ lý AI chưa trả lời được. Vui lòng thử lại.",
  "ERR-INTG-001": "Email chưa gửi được. Vui lòng gửi lại.",
  "ERR-GEN-VALIDATION": "Thông tin gửi lên chưa hợp lệ.",
  "ERR-GEN-NOT-FOUND": "Không tìm thấy dữ liệu này.",
  "ERR-GEN-FILE-MISSING": "Không còn file của phiên bản này. Hãy tải file lên lại dưới dạng phiên bản mới.",
  "ERR-GEN-UNAUTHENTICATED": "Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.",
  "ERR-GEN-EMAIL-UNCONFIRMED": "Email chưa được xác thực. Hãy mở liên kết trong email xác thực đã gửi cho bạn.",
};

const fallback = "Đã có lỗi xảy ra. Vui lòng thử lại.";

export function errorMessage(error: unknown): string {
  return (error instanceof ApiError && error.code && messages[error.code]) || fallback;
}

// A failed job's reason is stored as written by the backend or the worker (FR-ANA-004).
// Known forms get readable text; schema details stay in the logs, found by job_id.
export function failureMessage(reason: string | null): string {
  if (!reason) {
    return "Không rõ lý do.";
  }

  const code = /^(ERR-[A-Z]+-\d+):/.exec(reason)?.[1];
  if (code && messages[code]) {
    return messages[code];
  }

  if (reason.startsWith("The analysis result was invalid")) {
    return "Kết quả phân tích không hợp lệ nên đã bị loại bỏ.";
  }

  if (reason.startsWith("No answer from the analysis worker")) {
    return "Hệ thống phân tích không phản hồi trong 20 phút.";
  }

  if (reason.startsWith("The analysis could not be started")) {
    return "Chưa bắt đầu được phân tích.";
  }

  return reason;
}
