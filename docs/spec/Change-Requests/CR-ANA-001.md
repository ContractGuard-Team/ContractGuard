# CR-ANA-001 - Dừng phân tích đang chạy

| Field | Value |
|---|---|
| CR-ID | CR-ANA-001 |
| Date Received | 2026-11-23 |
| Source | Direct Prompt (Approver) |
| Description | Người dùng dừng (hủy) được một lần phân tích hợp đồng đang chạy, rồi chạy lại được ngay sau đó. |
| Priority | P3-Medium |
| Status | APPLIED (chờ Approver duyệt các mục REVIEW) |
| Authorized | 2026-11-23, Approver yêu cầu: "thêm tính năng hủy chạy phân tích ... có thể chạy lại được sau khi hủy"; chọn "Viết CR + code", quyền dừng "Owner và Member" |
| Decision | DEC-GEN-031 (chờ duyệt) |

## Impact Analysis - CR-ANA-001

### Change Description

SRS baseline chỉ có ba trạng thái của một lần phân tích: Đang chạy, Hoàn tất, Thất bại (STATE-ANA-001..003). Muốn bỏ một lần đang chạy, người dùng phải chờ nó xong hoặc chờ 20 phút để hệ thống tự đánh thất bại (SD-13). CR này thêm trạng thái `Đã dừng`: Owner hoặc Member hợp đồng dừng lần đang chạy, hợp đồng về trạng thái trước khi phân tích, và chạy lại được ngay (FR-ANA-014).

### Directly Affected Items

| ID | Section | Current State | Required Change |
|---|---|---|---|
| FR-ANA-018 (mới) | SRS §8.2 | - | Dừng phân tích; REVIEW |
| STATE-ANA-004 (mới) | SRS §13.1 | - | Trạng thái `Đã dừng`, kết thúc; REVIEW |
| TR-ANA-004 (mới) | SRS §13.2 | - | Đang chạy → Đã dừng khi người dùng dừng; REVIEW |
| STATE-ANA-001, STATE-CTR-002 | SRS §13.1 | APPROVED | Thêm lối ra "đã dừng"; ghi `[CHANGE]` |
| TR-CTR-003 | SRS §13.2 | APPROVED: hợp đồng về trạng thái cũ khi thất bại | Áp dụng cả khi dừng; ghi `[CHANGE]` |
| F-ANA-001 (bảng §6), UC-ANA-002, RTM | SRS | APPROVED | Thêm FR-ANA-018; ghi `[CHANGE]` |

### Indirectly Affected Items

| ID | Section | Relationship | Potential Impact |
|---|---|---|---|
| FR-ANA-014 | SRS §8.2 | Chạy lại phân tích | Không đổi; chạy lại được ngay sau khi dừng |
| FR-AUD-001 | SRS §8.2 | Nhật ký hoạt động | Thêm hành động "dừng phân tích" (như CR-CTR-001 thêm hành động mà không sửa câu yêu cầu) |
| ENT-DATA-010 `AnalysisRun` | SRS §9 | Cột `run_status` | Thêm giá trị `cancelled`. Cột là chuỗi, không cần migration |
| SD-13 (sweeper 20 phút) | `docs/02-system-design.md` | Chỉ đánh thất bại lần đang chạy | Không đổi |
| Worker AI | `docs/06`, `docs/07` | Worker chỉ báo sự kiện (SD-13) | Worker chưa được báo dừng, chạy nốt; kết quả đến sau bị bỏ qua |

### Conflict Assessment

None.

### Risk Assessment

| Risk | Likelihood | Impact |
|---|---|---|
| Worker vẫn chạy nốt lần đã dừng, tốn tài nguyên và làm lần chạy lại phải xếp hàng sau | High (worker xử lý từng job một) | Low–Medium. Lần chạy lại chậm hơn; nếu lần đã dừng chạy lâu, lần chạy lại có thể chạm mốc 20 phút của SD-13. Đề xuất cho nhóm AI ở `docs/06` |
| Worker xóa file `quarantine/` của lần đã dừng khi chạy xong, trong lúc lần chạy lại cần file đó | High nếu không xử lý | Đã xử lý: sau khi có lần bị dừng, mỗi lần chạy nhận bản chép riêng `quarantine/{owner}/{jobId}{ext}` |
| Member dừng phân tích do Owner bắt đầu | Medium | Low. Ghi nhật ký; ai cũng chạy lại được |

## Applied Changes

- SRS v1.6: thêm FR-ANA-018, STATE-ANA-004, TR-ANA-004 (`REVIEW`); STATE-ANA-001, STATE-CTR-002, TR-CTR-003, bảng F-ANA-001, UC-ANA-002, RTM có ghi chú `[CHANGE]`. Không mục nào được đặt `APPROVED`.
- PRD: không đổi (F-ANA-001 đã gồm "hiển thị tiến trình, báo lỗi, chạy lại"; dừng là chi tiết của luồng đó).
- Project State: DEC-GEN-031 (chờ duyệt), nhật ký phiên.
- Code: `POST /api/v1/jobs/{jobId}/cancel` (204; lần đã kết thúc thì không làm gì); `RunStatus.Cancelled`, `AuditAction.AnalysisCancelled`; nút "Dừng phân tích" kèm hộp xác nhận trên khung tiến độ; thông báo "Đã dừng phân tích" trên trang hợp đồng.

## Còn chờ

- Approver duyệt các mục `REVIEW` và DEC-GEN-031.
- ~~Nhóm AI: viết luồng hủy phía worker, chọn cách báo dừng.~~ Xong 2026-11-24: queue SQS riêng `analysis-cancels`, message `cancel_requested`, worker trả `failed` + `ERR-AI-003`. Backend đã nối (`docs/07` §15).
- Queue AWS dev đã tạo (2026-11-24). Nhóm AI cấp quyền và đặt `WORKER_CANCEL_QUEUE_URL`, hai bên chạy nghiệm thu. Thêm `cancel_requested` vào `contracts/queue-messages.v1` cần cả ba bên duyệt.
