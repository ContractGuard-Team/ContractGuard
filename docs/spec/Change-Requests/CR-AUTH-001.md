# CR-AUTH-001 - Trang hồ sơ và đổi mật khẩu

| Field | Value |
|---|---|
| CR-ID | CR-AUTH-001 |
| Date Received | 2026-11-23 |
| Source | Direct Prompt (Approver) |
| Description | Người dùng xem và sửa hồ sơ của mình (tên hiển thị, số điện thoại, ngôn ngữ kết quả) và đổi hoặc đặt mật khẩu khi đã đăng nhập. Trước đây chỉ nhập tên lúc đăng ký và chỉ đổi mật khẩu qua email đặt lại. |
| Priority | P3-Medium |
| Status | APPLIED (chờ Approver duyệt các mục REVIEW) |
| Authorized | 2026-11-23, Approver yêu cầu: "Thêm việc xem, sửa profile ... chưa có thể cấu hình profile bản thân được"; chọn tên hiển thị, số điện thoại, đổi mật khẩu, ngôn ngữ; "Viết CR + code" |
| Decision | DEC-GEN-032 (chờ duyệt) |

## Impact Analysis - CR-AUTH-001

### Change Description

Bảng tài khoản đã có tên hiển thị, ngôn ngữ (`ui_language`, FR-VIEW-015) và cột số điện thoại mặc định của ASP.NET Identity, nhưng không có chỗ nào cho người dùng sửa. CR này thêm trang hồ sơ và đổi mật khẩu khi đã đăng nhập. Ngôn ngữ không đổi luồng worker hay schema: backend đã gửi ngôn ngữ của người dùng trong `output_language` của `analysis-request.v1` và `chat-request.v1`, chỉ là trước đây luôn là `vi`.

### Directly Affected Items

| ID | Section | Current State | Required Change |
|---|---|---|---|
| FR-AUTH-016 (mới) | SRS §8.2 | - | Xem và sửa hồ sơ; REVIEW |
| FR-AUTH-017 (mới) | SRS §8.2 | - | Đổi hoặc đặt mật khẩu khi đã đăng nhập; REVIEW |
| ERR-AUTH-007 (mới) | SRS §14 | - | Mật khẩu hiện tại không đúng; REVIEW |
| ATTR-DATA-047, ATTR-DATA-048 (mới) | SRS §9 | `display_name` đã có trong code nhưng chưa có trong spec; `phone_number` chưa dùng | Thêm vào ENT-DATA-002; REVIEW |
| FR-VIEW-015 | SRS §8.2 | APPROVED, chưa làm | Ghi chú `[CHANGE]`: chọn ở trang hồ sơ, hiện chỉ đổi ngôn ngữ kết quả |
| F-AUTH-001 (bảng §6), RTM | SRS | APPROVED | Thêm FR-AUTH-016, FR-AUTH-017; ghi `[CHANGE]` |

### Indirectly Affected Items

| ID | Section | Relationship | Potential Impact |
|---|---|---|---|
| FR-RISK-013 | SRS §8.2 | Ngôn ngữ của phần giải thích | Người dùng chọn `en` thì lần phân tích sau yêu cầu worker trả tiếng Anh. Worker cần xử lý đúng `en` (`docs/06`) |
| FR-AUTH-005, FR-AUTH-009, FR-AUTH-011 | SRS §8.2 | Chính sách mật khẩu, đặt lại, Google | Dùng lại chính sách; tài khoản Google đặt được mật khẩu ngay trong ứng dụng thay vì qua email |
| SEC-AUTH-007 | SRS §12 | Giới hạn theo IP | Áp dụng cho đổi mật khẩu |
| §9.9 lưu giữ dữ liệu | SRS §9 | Dữ liệu tài khoản | Số điện thoại là dữ liệu cá nhân mới, lưu cùng tài khoản và xóa cùng tài khoản |

### Conflict Assessment

- **Riêng tư:** thêm số điện thoại là thu thập thêm dữ liệu cá nhân (không bắt buộc). Cần IA xác nhận và cập nhật trang chính sách riêng tư nếu cần.
- Ngoài điểm trên: none.

### Risk Assessment

| Risk | Likelihood | Impact |
|---|---|---|
| Người dùng chọn tiếng Anh nhưng worker trả tiếng Việt hoặc lỗi | Medium (chưa ai thử `en`) | Low. Nhóm AI thử một lần |
| Người dùng hiểu nhầm lựa chọn ngôn ngữ sẽ dịch giao diện | Medium | Low. Ô chọn có ghi chú "Giao diện vẫn bằng tiếng Việt" |
| Đoán mật khẩu hiện tại qua endpoint đổi mật khẩu | Low | Medium. Cần phiên đăng nhập hợp lệ và bị giới hạn theo IP |

## Applied Changes

- SRS v1.7: thêm FR-AUTH-016, FR-AUTH-017, ERR-AUTH-007, ATTR-DATA-047, ATTR-DATA-048 (`REVIEW`); FR-VIEW-015, bảng F-AUTH-001, RTM có ghi chú `[CHANGE]`. Không mục nào được đặt `APPROVED`.
- PRD: không đổi.
- Project State: DEC-GEN-032 (chờ duyệt), nhật ký phiên.
- Code: `GET /me` thêm `phoneNumber`, `hasPassword`; `PUT /me/profile`; `POST /me/password` (giới hạn như đăng nhập); trang `/profile`, mở từ tên ở chân thanh bên. Không cần migration.

## Còn chờ

- Approver duyệt các mục `REVIEW` và DEC-GEN-032.
- IA xác nhận việc lưu số điện thoại.
- Nhóm AI thử một lần phân tích và chat với `output_language = en`.
