# CR-ANA-002 - Lưu ngữ cảnh không tự chạy phân tích; dừng thì trang về như trước

| Field | Value |
|---|---|
| CR-ID | CR-ANA-002 |
| Date Received | 2026-11-25 |
| Source | Direct Prompt (Approver) |
| Description | Upload xong, hộp Ngữ cảnh chỉ lưu (hoặc Hủy: không lưu gì, hợp đồng vẫn còn). Phân tích chỉ chạy khi người dùng bấm; chưa có ngữ cảnh thì báo cần khai báo. Dừng phân tích thì trang về như trước khi bấm chạy, không hiện "Tổng quan" và các tab kết quả. |
| Priority | P3-Medium |
| Status | APPLIED (chờ Approver duyệt các mục REVIEW) |
| Authorized | 2026-11-25, Approver: "khi upload hợp đồng lên không bắt buộc phải chạy phân tích ... bấm hủy thì không thay đổi gì giao diện (kiểu như rollback) ... Nếu bấm chạy mà chưa điền ngữ cảnh, báo cần phải điền ngữ cảnh"; duyệt kế hoạch cùng ngày |
| Decision | DEC-GEN-033 (chờ duyệt) |

## Impact Analysis - CR-ANA-002

### Change Description

FR-ANA-001 baseline: xác nhận ngữ cảnh là bắt đầu phân tích luôn. Người dùng muốn upload và khai ngữ cảnh trước, đọc hợp đồng, rồi mới chạy. CR này tách hai việc: `PUT .../context` chỉ lưu, `POST .../versions/{versionId}/analyses` bắt đầu phân tích (lần đầu và chạy lại, FR-ANA-014). Lần phân tích đã dừng (CR-ANA-001) không còn hiện trên trang hợp đồng: trang về đúng như trước khi bấm chạy.

### Directly Affected Items

| ID | Section | Current State | Required Change |
|---|---|---|---|
| FR-ANA-001 | SRS §8.2 | APPROVED: tự bắt đầu khi xác nhận ngữ cảnh | Bắt đầu khi người dùng chọn; cần ngữ cảnh đã lưu. Về REVIEW |
| US-ANA-001 | PRD | APPROVED | AC1 đổi theo; về REVIEW |
| FR-CTR-005 | SRS §8.2 | APPROVED | Không đổi câu yêu cầu; ghi chú `[CHANGE]`: lưu không chạy phân tích |
| FR-ANA-018 | SRS §8.2 | REVIEW (CR-ANA-001) | Thêm AC4: dừng thì trang về như trước lần chạy |

### Indirectly Affected Items

| ID | Section | Relationship | Potential Impact |
|---|---|---|---|
| UC-CTR-001 | SRS §6 | Upload → ngữ cảnh → phân tích | Thêm một bước bấm chạy; luồng chính khác ở thao tác, không khác kết quả |
| ERR-CTR-002 | SRS §14 | Ngữ cảnh chưa đủ | Dùng thêm khi bấm chạy mà chưa có ngữ cảnh |
| FR-AUD-001 | SRS §8.2 | Nhật ký | Không đổi: vẫn ghi "xác nhận ngữ cảnh" và "dừng phân tích" |
| API | `PUT /contracts/{id}/versions/{v}/context` | Trả 202 + jobId hoặc 204 | Luôn 204. Endpoint mới `POST .../analyses` trả 202 + jobId |
| Worker AI | `docs/07` | Message queue | Không đổi |

### Conflict Assessment

None.

### Risk Assessment

| Risk | Likelihood | Impact |
|---|---|---|
| Người dùng lưu ngữ cảnh rồi quên bấm chạy | Medium | Low. Nút "Bắt đầu phân tích" nổi bật (primary) khi đã có ngữ cảnh mà chưa có kết quả |
| Client cũ còn chờ PUT context trả jobId | Low (frontend deploy cùng backend) | Low. Deploy backend và frontend cùng lúc |

## Applied Changes

- SRS v1.8: FR-ANA-001 sửa và về REVIEW; FR-CTR-005, FR-ANA-018 có ghi chú `[CHANGE]`, FR-ANA-018 thêm AC4. PRD v1.5: US-ANA-001 sửa và về REVIEW. Không mục nào được đặt `APPROVED`.
- Project State: DEC-GEN-033 (chờ duyệt), nhật ký phiên.
- Code: `AnalysisStarter.SaveContextAsync` / `StartAsync`; `POST /api/v1/contracts/{contractId}/versions/{versionId}/analyses`; `ContractQueries` bỏ run `cancelled` khỏi `latestJob` và trạng thái phiên bản. Frontend: hộp ngữ cảnh "Lưu ngữ cảnh" / "Hủy" (Hủy bỏ những gì đã gõ), nút "Bắt đầu phân tích" luôn có khi dịch vụ phân tích sẵn sàng, báo cần ngữ cảnh khi bấm mà chưa có; bỏ thông báo "Đã dừng phân tích".

## Còn chờ

- Approver duyệt các mục `REVIEW` và DEC-GEN-033.
