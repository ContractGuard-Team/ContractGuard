# CR-VIEW-001 - Giao diện tiếng Việt; ngôn ngữ chọn được chỉ áp dụng cho kết quả AI

| Field | Value |
|---|---|
| CR-ID | CR-VIEW-001 |
| Date Received | 2026-11-26 |
| Source | Direct Prompt (Approver) |
| Description | FR-VIEW-015 (Must) đòi đổi toàn bộ giao diện sang tiếng Anh, trong khi CR-AUTH-001 đã chốt giao diện giữ tiếng Việt, ngôn ngữ chọn ở hồ sơ chỉ đổi kết quả phân tích và câu trả lời trợ lý. Approver chọn giữ giao diện tiếng Việt. |
| Priority | P3-Medium |
| Status | APPLIED (chờ Approver duyệt các mục REVIEW) |
| Authorized | 2026-11-26, Approver chọn "Giữ UI tiếng Việt" khi được hỏi giữa làm i18n toàn bộ giao diện và thu hẹp FR-VIEW-015 |
| Decision | DEC-GEN-035 (chờ duyệt) |

## Impact Analysis - CR-VIEW-001

### Change Description

FR-VIEW-015, NFR-USAB-001 và UI-GEN-008 yêu cầu mọi nhãn và thông báo có bản tiếng Anh. Giao diện hiện chỉ có tiếng Việt; làm song ngữ cần dịch toàn bộ chuỗi trên frontend. CR này thu hẹp FR-VIEW-015 về đúng điều đã làm theo CR-AUTH-001: người dùng chọn tiếng Việt hoặc tiếng Anh cho kết quả phân tích và câu trả lời trợ lý; giao diện giữ tiếng Việt. Giao diện song ngữ để sau MVP.

### Directly Affected Items

| ID | Section | Current State | Required Change |
|---|---|---|---|
| FR-VIEW-015 | SRS §8.2 | APPROVED, AC1 chưa đạt | Chọn ngôn ngữ kết quả AI; AC1 sửa. Về REVIEW |
| NFR-USAB-001 | SRS §11 | APPROVED, Must | DEPRECATED, thay bằng FR-VIEW-015 đã sửa; song ngữ giao diện để sau MVP |
| UI-GEN-008 | SRS §10.1 | Giao diện tiếng Việt và tiếng Anh | Giao diện tiếng Việt; kết quả AI theo ngôn ngữ đã chọn. Về REVIEW |
| STR-QUAL-013 | PRD §10 | Giao diện và kết quả song ngữ | Ghi chú `[CHANGE]`: giao diện tiếng Việt trong MVP. Về REVIEW |

### Indirectly Affected Items

| ID | Section | Relationship | Potential Impact |
|---|---|---|---|
| FR-RISK-013 | SRS §8.2 | Ngôn ngữ finding | Không đổi: đã theo ngôn ngữ chọn |
| FR-AUTH-016 | SRS §8.2 | Trang hồ sơ | Không đổi |
| FR-EXP-005 | SRS §8.2 | Ngôn ngữ báo cáo | Không đổi: phần chữ cố định của báo cáo vẫn có vi/en |
| Code | Frontend | `<html lang="vi">`, chuỗi tiếng Việt | Không đổi |

### Conflict Assessment

None sau CR này; trước CR, FR-VIEW-015 mâu thuẫn với DEC-GEN-032.

### Risk Assessment

| Risk | Likelihood | Impact |
|---|---|---|
| Giám khảo hoặc người dùng không đọc tiếng Việt | Low | Low. Kết quả phân tích, trợ lý và báo cáo vẫn có tiếng Anh |

## Applied Changes

- SRS v1.9: FR-VIEW-015, UI-GEN-008 sửa và về REVIEW; NFR-USAB-001 DEPRECATED.
- PRD v1.6: STR-QUAL-013 ghi chú `[CHANGE]`, về REVIEW.
- Project State: DEC-GEN-035 (chờ duyệt).
- Không mục nào được đặt `APPROVED`.

## Còn chờ

- Approver duyệt các mục `REVIEW` và DEC-GEN-035.
