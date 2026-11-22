# CR-CTR-001 - Thùng rác 30 ngày và đổi tên hợp đồng

| Field | Value |
|---|---|
| CR-ID | CR-CTR-001 |
| Date Received | 2026-11-22 |
| Source | Direct Prompt (Approver) |
| Description | (1) Xóa hợp đồng không xóa hẳn ngay mà chuyển vào thùng rác; hợp đồng ở đó 30 ngày, khôi phục được, rồi tự bị xóa vĩnh viễn. Owner có thể xóa vĩnh viễn sớm từ thùng rác. (2) Owner hợp đồng và Owner tổ chức đổi được tên hợp đồng. |
| Priority | P2-High |
| Status | APPLIED (chờ Approver duyệt các mục REVIEW) |
| Authorized | 2026-11-22, Approver yêu cầu: "Có thể xóa hợp đồng, xóa kiểu soft delete vào thùng rác, sau 30 ngày thì xóa vĩnh viễn. Có thể thay đổi tên hợp đồng"; chọn "Viết CR + code", đổi tên "Chỉ Owner" |
| Decision | DEC-GEN-030 (chờ duyệt) |

## Impact Analysis - CR-CTR-001

### Change Description

FR-CTR-018 và NFR-PRIV-001 (baseline) yêu cầu xóa hẳn mọi dữ liệu trong vòng 24 giờ sau khi Owner xóa (OQ-DATA-001). CR này thêm một bước ở giữa: xóa → thùng rác (ẩn với mọi người ngay) → 30 ngày → xóa vĩnh viễn. Mốc 24 giờ của NFR-PRIV-001 nay tính từ lúc xóa vĩnh viễn. Đổi tên hợp đồng là yêu cầu mới; trước đây tên lấy từ tên file và không sửa được.

### Directly Affected Items

| ID | Section | Current State | Required Change |
|---|---|---|---|
| FR-CTR-018 | SRS §8.2 | APPROVED: xóa vĩnh viễn ngay | Xóa vào thùng rác; xóa vĩnh viễn sau 30 ngày hoặc khi Owner chọn; REVIEW |
| NFR-PRIV-001 | SRS §11 | APPROVED: 24 giờ kể từ khi xóa | 24 giờ kể từ khi xóa vĩnh viễn; REVIEW |
| UC-CTR-005 | SRS §5 | APPROVED | Luồng chính qua thùng rác, thêm AF-CTR-005 khôi phục; REVIEW |
| US-CTR-007 | PRD | APPROVED | AC1 qua thùng rác; REVIEW |
| FR-CTR-021 (mới) | SRS §8.2 | - | Khôi phục từ thùng rác; REVIEW |
| FR-CTR-022 (mới) | SRS §8.2 | - | Đổi tên hợp đồng; REVIEW |
| §9.9 (dòng Contract data), OQ-DATA-001 | SRS | Hard Delete trong 24 giờ | Soft Delete 30 ngày rồi Hard Delete; ghi `[CHANGE]` |

### Indirectly Affected Items

| ID | Section | Relationship | Potential Impact |
|---|---|---|---|
| FR-CTR-019 | SRS §8.2 | Chỉ Owner được xóa | Không đổi; áp dụng cả cho khôi phục và xóa vĩnh viễn |
| FR-SHARE-* | SRS §8.2 | Người được chia sẻ | Mất quyền xem khi hợp đồng ở thùng rác, có lại khi khôi phục |
| FR-ORG-011 | SRS §8.2 | Chuyển hợp đồng của thành viên bị xóa | Hợp đồng trong thùng rác cũng được chuyển (code dùng truy vấn bỏ qua bộ lọc) |
| ENT-DATA-005 `Contract` | SRS §9 | Bảng `contract` | Thêm `deleted_at`, `deleted_by_user_account_id` (migration `AddContractTrash`) |
| FR-AUD-001 | SRS §8.2 | Nhật ký hoạt động | Thêm hành động: đổi tên, chuyển vào thùng rác, khôi phục |

### Conflict Assessment

- **STR-QUAL-010 / riêng tư:** dữ liệu của hợp đồng đã xóa nằm thêm tối đa 30 ngày. Chấp nhận được vì người dùng chủ động xóa và có thể xóa vĩnh viễn ngay từ thùng rác. Cần IA xác nhận.
- Ngoài điểm trên: none.

### Risk Assessment

| Risk | Likelihood | Impact |
|---|---|---|
| Job dọn thùng rác không chạy (API tắt lâu), dữ liệu nằm quá 30 ngày | Low | Medium. Job chạy khi API khởi động và mỗi giờ; production chỉ bật ~3 ngày nên có thể không tới mốc 30 ngày |
| Người được chia sẻ bất ngờ mất hợp đồng | Medium | Low. Owner khôi phục được trong 30 ngày |

## Applied Changes

- PRD v1.4: US-CTR-007 sửa, về `REVIEW`.
- SRS v1.5: FR-CTR-018, NFR-PRIV-001, UC-CTR-005 sửa và về `REVIEW`; thêm FR-CTR-021, FR-CTR-022 (`REVIEW`); bảng F-CTR-003, F-CTR-006, RTM, §9.9, OQ-DATA-001 có ghi chú `[CHANGE]`. Không mục nào được đặt `APPROVED`.
- Project State: DEC-GEN-030 (chờ duyệt), nhật ký phiên.
- Code (nhánh `feat/trash-rename-retry`): `DELETE /contracts/{id}` chuyển vào thùng rác; `GET /contracts/trash`, `POST /contracts/{id}/restore`, `DELETE /contracts/{id}/permanent`, `PUT /contracts/{id}/name`; job `TrashPurgeService` mỗi giờ; trang `/contracts/trash`.

## Còn chờ

- Approver duyệt các mục `REVIEW` và DEC-GEN-030.
- IA xác nhận việc giữ dữ liệu thêm 30 ngày không trái chính sách riêng tư.
