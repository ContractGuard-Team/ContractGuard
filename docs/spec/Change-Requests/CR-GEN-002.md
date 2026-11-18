# CR-GEN-002 - Một vai trò hiệu lực cho mỗi người trên hợp đồng

| Field | Value |
|---|---|
| CR-ID | CR-GEN-002 |
| Date Received | 2026-11-17 |
| Source | Direct Prompt (Approver) |
| Description | Bốn vai trò (Owner/Member tổ chức, Owner/Member hợp đồng) chồng lên nhau và làm người dùng rối. Ví dụ: C (Member tổ chức) tạo hợp đồng rồi chia sẻ cho A, B (Owner tổ chức). Danh sách chia sẻ ghi A, B là Member hợp đồng, nhưng họ có toàn quyền vì là Owner tổ chức (DEC-PRD-017). Giữ quyền giám sát của Owner tổ chức, nhưng mỗi người chỉ có một vai trò hiệu lực trên một hợp đồng. |
| Priority | P2-High |
| Status | APPLIED |
| Authorized | 2026-11-17, Approver chọn "Hướng 1": giữ DEC-PRD-017, hiển thị vai trò hiệu lực; "khi bấm chia sẻ thì sẽ thấy những người đang coi được và vai trò của người đó"; "thêm modal để xác nhận khi thao tác phân quyền" |
| Decision | Chưa đánh số DEC; Approver ghi khi duyệt |

## Impact Analysis - CR-GEN-002

### Change Description

Quyền thật không đổi: Owner tổ chức vẫn có mọi quyền của Owner hợp đồng trên mọi hợp đồng của tổ chức. Thay đổi gồm:

1. **Vai trò hiệu lực.** Trên một hợp đồng, mỗi người có đúng một vai trò, lấy mức cao nhất: Owner hợp đồng, rồi Owner tổ chức (chỉ với hợp đồng của tổ chức đó), rồi Member hợp đồng.
2. **Không chia sẻ cho Owner tổ chức.** Chia sẻ hợp đồng của tổ chức cho Owner tổ chức bị từ chối (ERR-SHARE-003). "Chia sẻ cho toàn tổ chức" bỏ qua Owner tổ chức.
3. **Danh sách truy cập liệt kê mọi người xem được hợp đồng**, kể cả các Owner tổ chức, mỗi người kèm vai trò hiệu lực. Owner hợp đồng đồng thời là Owner tổ chức (ví dụ được chuyển quyền) hiện cả hai: "Quản trị viên / Người phụ trách". Chỉ Member hợp đồng có nút thu hồi. Bản ghi chia sẻ có sẵn của người vừa lên Owner tổ chức vẫn giữ, để có hiệu lực lại nếu người đó bị thu hồi quyền Owner tổ chức.
4. **Tên hiển thị** (ID và mã enum giữ nguyên): Owner tổ chức là "Quản trị viên", Member tổ chức là "Thành viên", Owner hợp đồng là "Người phụ trách", Member hợp đồng là "Được chia sẻ". Trang tổ chức và hộp thoại Chia sẻ dùng chung một bộ nhãn.
5. **Xác nhận** trước khi cấp hoặc thu hồi quyền quản trị và trước khi thu hồi chia sẻ (chỉ thay đổi giao diện).

### Directly Affected Items

| ID | Section | Current State | Required Change |
|---|---|---|---|
| BR-SHARE-002 | SRS §7 | APPROVED: hai vai trò hợp đồng, Owner tổ chức là ngoại lệ | Thêm vai trò hiệu lực, không chia sẻ cho Owner tổ chức; REVIEW |
| FR-SHARE-006 | SRS §8 | APPROVED: danh sách người được chia sẻ | Danh sách mọi người truy cập được kèm vai trò hiệu lực, thêm 2 tiêu chí; REVIEW |
| VAL-DATA-016 | SRS §9 | Chưa có | Thêm; REVIEW |
| ERR-SHARE-003 | SRS §14.1 | Chưa có | Thêm |
| §1.4 Thuật ngữ | SRS §1.4 | Chưa có "Vai trò hiệu lực" | Thêm, kèm tên hiển thị |
| §12.3 | SRS §12.3 | "hợp các quyền" | Thêm câu về vai trò hiệu lực trên giao diện |

### Indirectly Affected Items

| ID | Section | Relationship | Potential Impact |
|---|---|---|---|
| FR-SHARE-004, FR-SHARE-005 | SRS §8 | Kiểm tra email khi chia sẻ | Thêm điều kiện VAL-DATA-016; tiêu chí cũ vẫn đúng |
| FR-ORG-007, FR-ORG-008 | SRS §8 | Cấp, thu hồi Owner tổ chức | Thêm bước xác nhận trên giao diện; tiêu chí cũ vẫn đúng |
| DEC-PRD-017 | PRD | Quyền của Owner tổ chức | Giữ nguyên |
| Code SE | Ngoài spec | `ShareService`, `ContractAccessView` (thêm `administrators`), `ErrorCodes`, `ShareDialog`, trang tổ chức, e2e | Đã sửa |

### Conflict Assessment

None. Không đổi ma trận quyền §12.3; chỉ thêm ràng buộc khi chia sẻ và cách hiển thị.

### Risk Assessment

| Risk | Likelihood | Impact |
|---|---|---|
| Người dùng muốn "chia sẻ" cho quản trị viên để báo cho họ biết | Low | Low. Quản trị viên đã thấy hợp đồng trong thư viện; thông báo lỗi nói rõ lý do |
| Lộ danh sách quản trị viên cho Owner hợp đồng | Low | Low. Member tổ chức vốn đã xem được email và vai trò mọi thành viên (FR-ORG-006) |

## Applied Changes

- SRS v1.3: các mục ở trên. Mục sửa ở trạng thái `REVIEW`; không mục nào được đặt `APPROVED`.
- Code: backend `ShareService` và test `Organization_owners_show_as_administrators_not_shares`; frontend `ShareDialog`, `organization.tsx`, `labels.ts`, `errors.ts`; e2e `org-roles`, `collaboration`.
