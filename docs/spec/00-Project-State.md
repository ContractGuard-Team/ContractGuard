---
document_type: project_state
state_schema_version: "1.1"
project: "ContractGuard"
last_updated: 2026-11-17
---

# Project State - ContractGuard

The Agent reads this file first in every session and updates it at the end of every turn. It is the single place that shows where the project is in the kit pipeline and what happens next.

## 1. Project Settings

| Field | Value | Source |
|---|---|---|
| Project Name | `ContractGuard` | Discovery Interview, session 2026-10-20 (tên chốt khi hợp nhất ý tưởng), Q-INT-001 |
| Idea Statement | ContractGuard giúp doanh nghiệp SME Việt Nam đọc hiểu hợp đồng B2B do đối tác gửi sang. Hệ thống phân tích từng điều khoản trên 7 nhóm rủi ro, giải thích vì sao có rủi ro, gợi ý hướng xử lý và chấm điểm rủi ro cho toàn bộ hợp đồng. Hệ thống cũng có chatbot hỏi đáp theo từng hợp đồng và là nơi lưu trữ, chia sẻ hợp đồng của tổ chức. | `docs/01-product-spec.md` v1.2, §1; xác nhận tại Discovery Interview, session 2026-10-24, Q-INT-002 |
| Project Profile | TEAM | Discovery Interview, session 2026-10-24, Q-INT-003 |
| Tier | 2 | Discovery Interview, session 2026-10-24, Q-INT-004 |
| Output Language | `vi` | Discovery Interview, session 2026-10-24, Q-INT-005 |
| Mandatory Keyword | `phải` | `Agent/01-Core/05-Output Rules.md` §12 (cố định cho tiếng Việt) |
| Assumption Policy | `ask` | Discovery Interview, session 2026-10-24, Q-INT-007 |
| Approver | SE của nhóm (người lập spec) | Discovery Interview, session 2026-10-24, Q-INT-006 |
| Reviewers | Các thành viên AI (3 người) và IA (1 người) | Discovery Interview, session 2026-10-24, Q-INT-006 |
| Output Location | `docs/spec/` (nằm trong git repo, không bị `.gitignore` loại trừ; tài liệu sẽ được commit cùng repo) | Discovery Interview, session 2026-10-24, Q-INT-010 |
| Document Set | PRD, SRS | Discovery Interview, session 2026-10-24, Q-INT-009 |
| Primary Per Layer | Business: n/a (không có BRS/BRD; PRD sở hữu `BG`, `SC`). Stakeholder/Product: PRD | Discovery Interview, session 2026-10-24, Q-INT-009 |
| PRD Scope | Whole Product | Discovery Interview, session 2026-10-24, Q-INT-012 |
| PRD Variant | Standard | Discovery Interview, session 2026-10-24, Q-INT-013 (chọn user stories) |
| Delivery Options | Backlog: no; Release Plan: no | Discovery Interview, session 2026-10-24, Q-INT-013 |

### 1.1 Language Conventions (Output Language `vi`)

| Element | Vietnamese Form | Status |
|---|---|---|
| Mandatory keyword | `phải` (không dùng "sẽ") | Cố định theo kit |
| EARS - Ubiquitous | `<Hệ thống> phải ...` | Cố định theo kit |
| EARS - Event-driven | `Khi <sự kiện>, <hệ thống> phải ...` | Cố định theo kit |
| EARS - State-driven | `Trong khi <trạng thái>, <hệ thống> phải ...` | Cố định theo kit |
| EARS - Unwanted behavior | `Nếu <điều kiện>, thì <hệ thống> phải ...` | Cố định theo kit |
| EARS - Optional feature | `Ở nơi <tính năng được bật>, <hệ thống> phải ...` | Cố định theo kit |
| User story template | `Là <persona>, tôi muốn <khả năng>, để <lợi ích>.` | Xác nhận 2026-10-24 (DEC-GEN-013) |
| Acceptance criteria keywords | Giữ từ khóa tiếng Anh: `Given <bối cảnh>, when <hành động>, then <kết quả quan sát được>.` Phần nội dung viết tiếng Việt. | Xác nhận 2026-10-24 (DEC-GEN-013) |
| Data model (SRS §9) | Tên entity, thuộc tính, quan hệ, bảng, cột, data item, mã enum và nhãn ERD viết tiếng Anh; định nghĩa, mô tả, quy tắc viết tiếng Việt | Xác nhận 2026-10-27 (DEC-GEN-025) |

## 2. Document Register

| Order | Doc Type | File | Role | Depth, Variant, Or Tier | Version | Status | Gate | Gate Decision |
|---:|---|---|---|---|---|---|---|---|
| 0 | Project State | `00-Project-State.md` | - | - | - | - | S0 Intake | Passed 2026-10-24 by Approver |
| 1 | BRS / BRD | - | - | - | - | - | S1 Business | Skipped (not in Document Set) |
| 2 | PRD | `02-PRD.md` | Primary | Standard | `1.5` | REVIEW | S2 Stakeholder & Product | Passed 2026-10-24 by Approver (v0.2); v0.3 áp dụng trả lời OQ 2026-10-26; v0.4 thêm mô hình không gian làm việc 2026-10-27; v1.0 baseline tại Gate S5 2026-10-27; v1.1 bỏ MFA khỏi MVP (DEC-PRD-022) 2026-10-27; v1.2 bỏ bản scan và OCR (CR-GEN-001, DEC-PRD-023) 2026-11-17; chờ Approver duyệt lại; v1.3 CR-GEN-003 model tự host trên EC2 2026-11-17; v1.4 CR-CTR-001 thùng rác 30 ngày 2026-11-22; v1.5 CR-ANA-002 bấm mới chạy phân tích 2026-11-25 |
| 3 | SRS | `03-SRS.md` | - | Tier 2 | `1.8` | REVIEW | S3 Outline, S4 Incremental | S3: Passed 2026-10-24 by Approver; S4: hoàn tất bản nháp đầy đủ 2026-10-24; v0.9 áp dụng trả lời OQ 2026-10-26; v0.10 mô hình không gian và §9 tiếng Anh 2026-10-27; v1.0 baseline 2026-10-27; v1.1 bỏ MFA khỏi MVP 2026-10-27; v1.2 bỏ bản scan và OCR (CR-GEN-001) 2026-11-17; chờ Approver duyệt lại; v1.4 CR-GEN-003 model tự host trên EC2 2026-11-17; v1.5 CR-CTR-001 thùng rác 30 ngày và đổi tên hợp đồng 2026-11-22; v1.6 CR-ANA-001 dừng phân tích 2026-11-23; v1.7 CR-AUTH-001 trang hồ sơ và đổi mật khẩu 2026-11-23; v1.8 CR-ANA-002 lưu ngữ cảnh không tự chạy phân tích 2026-11-25 |
| 3 | Validation Report | `Reports/Validation-Report-2026-10-27.md` (thay các bản trước) | - | - | - | - | S5 Baseline | Passed 2026-10-27 by Approver: 0 Critical, 2 Warning (MC9, RS3) là known issues (DEC-GEN-026) |
| 4 | Backlog / Release Plan | - | - | - | - | - | S6 Delivery | Skipped (not in Document Set) |

## 3. Current Position

| Field | Value |
|---|---|
| Current Stage | S5 hoàn tất (baseline). S6 Delivery không thuộc Document Set |
| Current Run Mode | - |
| Current Focus | PRD v1.0 và SRS v1.0 đã baseline. Kế hoạch code theo phase ở `docs/03-implementation-phases.md`; sơ đồ kiến trúc ở `docs/architecture/contractguard-architecture.drawio` (thay hai sơ đồ HTML từ 2026-11-17) |
| Next Step | Làm tiếp system design (`docs/02-system-design.md` bước 2–8), rồi code theo phase. Mọi thay đổi PRD, SRS từ nay đi qua change request (`Agent/03-SRS/06-Change Request Workflow.md`) |
| Blocked By | Approver duyệt lại các mục `REVIEW` sau khi bỏ MFA (F-AUTH-003, FR-AUTH-006, FR-AUTH-007) và sau CR-GEN-001 bỏ bản scan (`Change-Requests/CR-GEN-001.md`), sau CR-GEN-003 model tự host trên EC2 (`Change-Requests/CR-GEN-003.md`), sau CR-CTR-001 thùng rác và đổi tên (`Change-Requests/CR-CTR-001.md`), sau CR-ANA-001 dừng phân tích (`Change-Requests/CR-ANA-001.md`), sau CR-AUTH-001 trang hồ sơ (`Change-Requests/CR-AUTH-001.md`). Known issue: ngày demo đầu 12/2026 (OQ-GEN-003) chưa khớp CON-PRJ-005 "hơn 4 tháng" |

## 4. Interview Progress

| Stage | Answered Questions | Pending Questions | Skipped (Not Applicable To Profile) |
|---|---|---|---|
| S0 | Q-INT-001, Q-INT-002, Q-INT-003, Q-INT-004, Q-INT-005, Q-INT-006, Q-INT-007, Q-INT-008, Q-INT-009, Q-INT-010, Q-INT-012, Q-INT-013 | None | Q-INT-011 (mỗi tầng chỉ có một tài liệu) |
| S1 | - | - | Toàn bộ (S1 skipped, not in Document Set) |
| S2 | Q-PRD-001 (mục tiêu), Q-PRD-005 (ưu tiên mục "L"), Q-PRD-007 (độ trễ), Q-PRD-010 (release criteria); Q-PRD-002, Q-PRD-003, Q-PRD-004, Q-PRD-008, Q-PRD-009, Q-PRD-012 đã có trả lời trong nguồn P3 | Q-PRD-006 (chi tiết tiêu chí nghiệm thu, hỏi khi phát sinh) | Q-PRD-011 (không có release plan) |
| S6 | - | - | Toàn bộ (not in Document Set) |

## 5. Open Questions Summary

| Question ID | Question | Document | Blocking Stage | Status |
|---|---|---|---|---|
| OQ-GEN-001 | Idea statement ở mục 1 đã đúng chưa? | Project State | S0 | CLOSED 2026-10-24: giữ nguyên |
| OQ-GEN-002 | Có dùng mẫu user story `Là ..., tôi muốn ..., để ...` và từ khóa nghiệm thu `Cho trước / khi / thì` không? | PRD, SRS | S0 | CLOSED 2026-10-24: dùng mẫu story tiếng Việt, giữ Given/When/Then tiếng Anh (DEC-GEN-013) |
| OQ-GEN-003 | Ngày demo cuối kỳ thực tập FCAJ là ngày nào? Dùng làm thời điểm đánh giá BG-GEN-001..004. | PRD | - | CLOSED 2026-10-24: đầu tháng 12/2026 |
| OQ-GEN-004 | Mức thành thạo công nghệ của từng persona; khó khăn và bối cảnh sử dụng của UCL-GEN-003, UCL-GEN-004 | PRD | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem PRD §14.1) |
| OQ-GEN-005 | Các feature quản lý (tài khoản, tổ chức, chia sẻ, thư viện, trạng thái, phiên bản, xóa, nhật ký) phục vụ mục tiêu nào? | PRD | - | CLOSED 2026-10-24: tạo BG-GEN-004 (DEC-PRD-015) |
| OQ-GEN-006 | Xác nhận các mặc định ở Phụ lục C của product spec | PRD, SRS | - | CLOSED 2026-10-24: Owner tổ chức xem được mọi hợp đồng; giữ 3 mặc định còn lại (DEC-PRD-013) |
| OQ-GEN-007 | Ngân sách chi phí LLM trên mỗi hợp đồng | PRD, SRS | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem PRD §14.1) |
| OQ-GEN-008 | Region AWS | PRD, SRS | - | CLOSED 2026-10-24: Singapore (DEC-GEN-015) |
| OQ-GEN-009 | Xác suất và mức độ nghiêm trọng của các rủi ro sản phẩm | PRD | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem PRD §14.1) |
| OQ-GEN-010 | Thước đo và chỉ tiêu của BG-GEN-004 | PRD | Không chặn (PD2 cho phép OQ) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem PRD §14.1) |
| OQ-SHARE-001 | Ngoài việc xem, Owner tổ chức được làm gì trên hợp đồng không được chia sẻ cho mình | PRD, SRS | S4 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem PRD §14.1) |
| OQ-GEN-011 | Tần suất sử dụng dự kiến của từng lớp người dùng (UCL-GEN-001..004) | SRS §2.3 | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-GEN-012 | Danh sách trình duyệt và phiên bản được hỗ trợ | SRS §2.4, §11.11 | S4 (§11) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-GEN-013 | (a) Dịch vụ OCR và dịch vụ gửi email cụ thể (chọn ở thiết kế hệ thống); (b) hành vi khi gửi email thất bại | SRS §3.4, §10.2, §14.1 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-ORG-001 | Member tổ chức có xem được danh sách thành viên của tổ chức không? | SRS §3.3, §12 | S4 (§12) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-ORG-002 | Có chức năng xóa tổ chức không? Nếu có, ai được xóa và dữ liệu xử lý thế nào? | SRS §3.3, §9.9, §12 | S4 (§12) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-SHARE-002 | Member hợp đồng có được sửa metadata và ngữ cảnh của hợp đồng không? | SRS §3.3, §8, §12 | S4 (§8) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-AUD-001 | Ai được xem nhật ký hoạt động của một hợp đồng (Owner hợp đồng, Member hợp đồng, Owner tổ chức)? | SRS §5.2, §8, §12 | S4 (§8) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-CTR-002 | Các chuyển trạng thái `Mới` → `Đang phân tích` → `Chờ review` (và khi upload phiên bản mới) do hệ thống tự chuyển hay người dùng chuyển? | SRS §4.5, §8, §13 | S4 (§13) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-CTR-001 | Giới hạn kích thước file và số trang khi upload | PRD, SRS | - | CLOSED 2026-10-24: ≤ 20 MB, ≤ 100 trang (DEC-PRD-014) |
| OQ-EXP-001 | Hành vi khi tạo báo cáo thất bại | PRD, SRS | - | CLOSED 2026-10-24: hiển thị thông báo tạo báo cáo thất bại |
| OQ-RISK-001 | Bộ căn cứ pháp lý chọn lọc và việc xác minh | PRD, SRS | S4 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem PRD §14.1) |
| OQ-PRIV-001 | Quy định bảo vệ dữ liệu cá nhân của Việt Nam có áp dụng cho ContractGuard không, ở mức nào | PRD, SRS | S4 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem PRD §14.1) |
| OQ-AUTH-001 | Chi tiết xác thực: (a) chính sách mật khẩu; (b) người dùng chưa xác thực email có đăng nhập được không; (c) thời hạn liên kết xác thực email và liên kết đặt lại mật khẩu; (d) cách liên kết tài khoản Google với tài khoản email đã có; (e) loại yếu tố MFA; (f) phản hồi khi đặt lại mật khẩu cho email chưa đăng ký | SRS §6, §8, §12.1 | S4 (§8) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-ORG-003 | Thời hạn hiệu lực của link mời tham gia tổ chức | SRS §6 (UC-ORG-002), §8 | S4 (§8) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-ORG-004 | Khi xóa một thành viên khỏi tổ chức, các hợp đồng người đó đang là Owner được xử lý thế nào? | SRS §6 (UC-ORG-003), §8, §9 | S4 (§8) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-SHARE-003 | Khi hợp đồng được chia sẻ cho toàn tổ chức, thành viên tham gia sau thời điểm chia sẻ có tự động có quyền không? | SRS §6 (UC-SHARE-001), §8 | S4 (§8) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-SHARE-004 | Sau khi chuyển quyền Owner hợp đồng, Owner cũ còn vai trò gì (Member hợp đồng hay mất quyền)? | SRS §6 (UC-SHARE-002), §8 | S4 (§8) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-CTR-003 | Chi tiết quản lý hợp đồng: (a) trường ngữ cảnh nào bắt buộc; (b) các tiêu chí sắp xếp thư viện; (c) phiên bản mới có kế thừa ngữ cảnh của phiên bản trước không | SRS §6, §8 | S4 (§8) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-ANA-001 | Khi chạy lại phân tích một phiên bản, kết quả cũ được giữ lại hay thay thế? | SRS §6 (UC-ANA-002), §8, §9 | S4 (§8) | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-GEN-014 | Nơi lưu kết quả kiểm thử, eval và biên bản demo | SRS §15.2 | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-GEN-015 | Xác suất và mức độ nghiêm trọng của RISK-SRS-001..003 | SRS §17.3 | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-AUTH-002 | Cách lưu mật khẩu, thời hạn phiên, chức năng đăng xuất, ghi log đăng nhập thất bại | SRS §12.1, §14.1 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-ORG-005 | Cài đặt tổ chức gồm những gì, kể cả thông tin khi tạo tổ chức | SRS FR-ORG-001, FR-ORG-012 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-CTR-004 | Chuyển trạng thái ngoài luồng chính; trạng thái cuối | SRS §13 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-ANA-002 | Ngôn ngữ của phần giải thích khi nhiều người xem chọn ngôn ngữ khác nhau; ngôn ngữ câu trả lời chatbot | SRS FR-RISK-013, FR-CHAT-001 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-ANA-003 | Không tách được điều khoản; finding trùng ở hợp đồng song ngữ; phân tích đồng thời; thao tác bị chặn khi đang phân tích | SRS §13.1, §14.2 | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-DATA-001 | Thời hạn hoàn tất xóa dữ liệu hợp đồng | SRS FR-CTR-018, NFR-PRIV-001 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-DATA-002 | Thời gian lưu nhật ký và log; nhật ký của hợp đồng đã xóa; log sự kiện bảo mật | SRS §9.9, §11.13 | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-DATA-003 | Chức năng xóa tài khoản và thời gian lưu dữ liệu tài khoản | SRS §9.9, §12.3 | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-EXP-002 | Báo cáo đã xuất có được lưu lại không | SRS FR-EXP-001 | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-RISK-002 | Giá trị điểm sàn khi có finding Critical | SRS BR-RISK-009, FR-RISK-022 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-PERF-001 | Phân vị đo của mục tiêu thời gian bản scan và chatbot | SRS NFR-PERF-003, NFR-PERF-004 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-SCAL-001 | Số phân tích đồng thời tối thiểu | SRS NFR-SCAL-001 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-BKP-001 | Yêu cầu sao lưu và khôi phục | SRS §11.14 | Không chặn | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-VIEW-001 | Tô màu điều khoản có nhiều finding; lọc theo nhóm có gồm nhóm phụ | SRS FR-VIEW-006, FR-VIEW-009 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-CHAT-001 | Hành vi khi chatbot không tạo được câu trả lời | SRS ERR-CHAT-001 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |
| OQ-UPL-001 | Cách đếm số trang DOCX | SRS FR-UPL-005 | S5 | CLOSED 2026-10-26 (phiếu `OQ-Answers.md`; xem SRS §17.1) |

## 6. Decisions Log

| Decision ID | Decision | Date | Decided By | Affected Items |
|---|---|---|---|---|
| DEC-GEN-001 | Project Profile = TEAM, Tier = 2 | 2026-10-24 | Approver | Toàn bộ tài liệu |
| DEC-GEN-002 | Output Language = `vi`, mandatory keyword = `phải` | 2026-10-24 | Approver | Toàn bộ tài liệu |
| DEC-GEN-003 | Assumption Policy = `ask` | 2026-10-24 | Approver | Toàn bộ tài liệu |
| DEC-GEN-004 | Approver = SE của nhóm; Reviewers = thành viên AI và IA | 2026-10-24 | Approver | Các gate, mục Approval |
| DEC-GEN-005 | Document Set = PRD + SRS. Bỏ qua S1 vì không có tài liệu tầng kinh doanh; PRD sở hữu `BG` và `SC`. | 2026-10-24 | Approver | `02-PRD.md`, `03-SRS.md` |
| DEC-GEN-006 | PRD Scope = Whole Product; PRD Variant = Standard (có user stories); không có Backlog, Release Plan | 2026-10-24 | Approver | `02-PRD.md` |
| DEC-GEN-007 | Output Location = `docs/spec/`, chấp nhận được commit vào git | 2026-10-24 | Approver | Toàn bộ tài liệu |
| DEC-GEN-008 | Mục tiêu độ trễ phân tích hợp đồng 20 trang: p50 ≤ 60 giây, p95 ≤ 2 phút, đo khi worker đã chạy sẵn. Thay mức p50 ≤ 45 giây trong `docs/01-product-spec.md` §8. Được thay bởi DEC-GEN-022 (2026-10-26). | 2026-10-24 | Approver | PRD §9, SRS §11 |
| DEC-GEN-009 | Ánh xạ mức "L" (làm sau) trong product spec sang MoSCoW: So sánh phiên bản (EXT-01) và Company Policy (EXT-02) là `Could`; các mục "L" còn lại là `Won't`. | 2026-10-24 | Approver | PRD §7 |
| DEC-GEN-010 | Việc chọn model AI (open-weight trên Bedrock, Custom Model Import, hoặc Claude) được hoãn; spec không gắn với model cụ thể. Từ DEC-GEN-029 (2026-11-17), lựa chọn chỉ còn các model tự host được trên EC2. | 2026-10-21 | Approver | PRD §11, SRS §2.5, §10.2 |
| DEC-GEN-011 | Tài liệu không chứa phần phân công thành viên. | 2026-10-20 | Approver | Toàn bộ tài liệu |
| DEC-GEN-012 | Nguồn ưu tiên: quyết định trực tiếp của Approver (P1) > `docs/01-product-spec.md` v1.2, `docs/02-system-design.md` v0.2, `docs/PRODUCT.md` (P3, đang REVIEW) > thư mục `Idea/` (P4, đã được product spec thay thế). Các quyết định sản phẩm ở Phụ lục A của product spec do Approver đưa ra trực tiếp nên được tính là P1. | 2026-10-24 | Approver (xác nhận tại Gate S0) | Toàn bộ tài liệu |
| DEC-GEN-013 | Mẫu user story tiếng Việt `Là <persona>, tôi muốn <khả năng>, để <lợi ích>.`; tiêu chí nghiệm thu giữ từ khóa tiếng Anh `Given / when / then`, nội dung viết tiếng Việt. | 2026-10-24 | Approver | PRD §8, SRS §8, §15 |
| DEC-GEN-014 | Gate S0 thông qua | 2026-10-24 | Approver | `00-Project-State.md` |
| DEC-GEN-015 | Region AWS: Singapore (`ap-southeast-1`) | 2026-10-24 | Approver | PRD CON-PRJ-003, SRS §2.4, §2.5 |
| DEC-GEN-016 | Với các mục liên quan tới AI: viết theo hướng AI hoạt động ổn định và chạy qua Amazon Bedrock. Các mục Approver không trả lời được coi là chưa có quyết định và giữ `[TBD]`. Phần "chạy qua Amazon Bedrock" được thay bởi DEC-GEN-029 (2026-11-17, CR-GEN-003). | 2026-10-24 | Approver | PRD, SRS (các yêu cầu về phân tích AI và chatbot) |
| DEC-GEN-017 | Gate S2 thông qua (PRD v0.2) | 2026-10-24 | Approver | `02-PRD.md` |
| DEC-GEN-018 | Gate S3 thông qua. Cấu trúc SRS theo Tier 2 + TEAM; giữ §13 State Models; §21, §22 bản gọn; §9.10, §10.3, §11.16 ghi "Không áp dụng" kèm lý do; bộ mã domain `AUTH`, `ORG`, `SHARE`, `CTR`, `UPL`, `ANA`, `RISK`, `VIEW`, `CHAT`, `EXP`, `AUD`, `DATA` và mã danh mục NFR | 2026-10-24 | Approver | `03-SRS.md` |
| DEC-GEN-019 | S4 chạy theo Incremental Mode: viết từng mục, dừng để Approver review trước khi viết mục tiếp theo | 2026-10-24 | Approver | `03-SRS.md` |
| DEC-GEN-021 | Từ §7, S4 chuyển sang Full Generation Mode: viết hết các mục còn lại, kiểm tra sau mỗi mục lớn, Approver review một lần ở cuối. Thay DEC-GEN-019 cho phần còn lại. | 2026-10-24 | Approver | `03-SRS.md` §7-§22 |
| DEC-GEN-020 | Mã danh mục NFR theo kit (`Agent/03-SRS/03-Requirement Writing Rules.md` §10): `PERF`, `SCAL`, `AVAIL`, `REL`, `BKP`, `SEC`, `PRIV`, `LOG`, `USAB`, `ACC`, `COMP` (compatibility), `MAINT`, `PORT`, `SAFE`, `COMPL` (compliance). Thay phần mã NFR trong DEC-GEN-018. | 2026-10-24 | Approver | SRS §11 |
| DEC-GEN-022 | Giãn mục tiêu hiệu năng (trả lời OQ-PERF-001): hợp đồng có lớp text 20 trang p50 ≤ 2 phút, p95 ≤ 5 phút; bản scan ≤ 50 trang p50 ≤ 5 phút; chatbot hiển thị chữ đầu tiên p50 ≤ 15 giây. Mục tiêu chính là hệ thống chạy đúng tiêu chí đánh giá. Thay DEC-GEN-008. | 2026-10-26 | Approver | PRD BG-GEN-002, SC-GEN-003, SC-GEN-004, STR-QUAL-001..003; SRS NFR-PERF-001..004, AC-GEN-005 |
| DEC-GEN-023 | Quy ước của phiếu trả lời OQ (`OQ-Answers.md`): OQ để trống dòng Quyết định nghĩa là đồng ý với đề xuất; Approver có thể sửa thẳng nội dung đề xuất. Agent so phiếu với bản gốc trước khi áp dụng. | 2026-10-26 | Approver | `OQ-Answers.md`; mọi OQ đóng ngày 2026-10-26 |
| DEC-GEN-024 | Chống dò mật khẩu và spam email theo mô hình nhiều lớp, thay cách khóa theo IP ở OQ-AUTH-002 (d). (1) Đăng nhập: sai 5 lần liên tiếp với cùng email thì khóa đăng nhập bằng mật khẩu của email đó 15 phút, đếm ở server; vẫn cho đặt lại mật khẩu. (2) Quên mật khẩu: mỗi tài khoản chờ 3 phút giữa hai lần gửi, tối đa 5 email/giờ, luôn trả cùng thông báo; giao diện có đồng hồ đếm ngược 3 phút. (3) Rate limit theo IP: 30 request/phút cho đăng nhập, 10 request/phút cho quên mật khẩu. Email cảnh báo đăng nhập sai nhiều lần: Won't trong MVP. Lý do: khóa theo IP làm khóa lây mọi người dùng chung một mạng (NAT). | 2026-10-26 | Approver | SRS SEC-AUTH-006, SEC-AUTH-007, FR-AUTH-014, FR-AUTH-015, ERR-AUTH-005, ERR-AUTH-006, EF-AUTH-003 |
| DEC-GEN-025 | Ngoại lệ ngôn ngữ cho SRS §9 (Data Requirements And Data Model): tên entity, thuộc tính, quan hệ, bảng, cột, data item, mã giá trị enum và nhãn trong ERD viết tiếng Anh; cột định nghĩa, mô tả, quy tắc giữ tiếng Việt. Mã enum viết thường (ví dụ `pending_review`), nhãn tiếng Việt hiển thị trên giao diện. | 2026-10-27 | Approver | SRS §9, §1.1 Language Conventions |
| DEC-GEN-026 | Gate S5 thông qua: Approver baseline PRD v1.0 và SRS v1.0 ("SRS đã ổn"); mọi mục `REVIEW` chuyển `APPROVED`, mục `DEPRECATED` và OQ `CLOSED` giữ nguyên. MC9 (chưa có bảng cột chi tiết) và RS3 (khối lượng lớn so với 1 SE) là known issues. Kế hoạch code chia phase, không gắn ngày. | 2026-10-27 | Approver | `02-PRD.md`, `03-SRS.md`, `Reports/Validation-Report-2026-10-27.md`, `docs/03-implementation-phases.md` |
| DEC-GEN-027 | Frontend dùng React Router framework mode với `ssr: false` (SPA, build ra file tĩnh đặt trên S3 + CloudFront), không dùng Next.js và không thêm server Node.js. Lý do: ứng dụng nằm sau đăng nhập, landing và about không cần SEO. `@react-router/dev` 8.5.0 cùng phiên bản `react-router` 8.5.0 của prototype, hỗ trợ Vite 8 và TypeScript 6. Quyết định kỹ thuật, không đổi PRD hay SRS. | 2026-10-27 | Approver | `docs/03-implementation-phases.md`, `docs/architecture/system-overview.html` (đã xóa 2026-11-17, thay bằng `contractguard-architecture.drawio`) |
| DEC-GEN-028 | Xác thực tự quản bằng ASP.NET Core Identity trong service C# (system design SD-04 = A). Không dùng Amazon Cognito. MFA không thuộc MVP (DEC-PRD-022). | 2026-10-27 | Approver | `docs/02-system-design.md` §4 |
| DEC-GEN-029 | Model AI (phân tích, embedding, chatbot) do nhóm AI tự host bằng Docker trên Amazon EC2 ở `ap-southeast-1`, không dùng Amazon Bedrock. Thay DEC-GEN-016. DEC-GEN-010 vẫn áp dụng: model cụ thể do eval chọn, nhưng chỉ trong các model tự host được. Nền tảng chạy worker (EC2 hay ECS) do nhóm AI quyết. Áp dụng qua CR-GEN-003 | 2026-11-17 | Approver | PRD CON-PRJ-003, DEP-GEN-001, RISK-GEN-001, RISK-GEN-002; SRS CON-GEN-001, EXT-INTG-001, IF-INTG-001, RISK-SRS-003 |
| DEC-GEN-030 | Xóa hợp đồng chuyển vào thùng rác 30 ngày (khôi phục được), sau đó xóa vĩnh viễn; Owner xóa vĩnh viễn sớm được từ thùng rác. Thêm đổi tên hợp đồng (Owner hợp đồng, Owner tổ chức). Áp dụng qua CR-CTR-001. **Chờ Approver duyệt** | 2026-11-22 | Approver yêu cầu; chờ duyệt | PRD US-CTR-007; SRS FR-CTR-018, FR-CTR-021, FR-CTR-022, NFR-PRIV-001, UC-CTR-005, §9.9, OQ-DATA-001 |
| DEC-GEN-031 | Owner hoặc Member hợp đồng dừng được một lần phân tích đang chạy; lần đó sang `Đã dừng`, hợp đồng về trạng thái trước khi phân tích, chạy lại được ngay. Worker AI chưa được báo dừng, kết quả đến sau bị bỏ qua. Áp dụng qua CR-ANA-001. **Chờ Approver duyệt** | 2026-11-23 | Approver yêu cầu; chờ duyệt | SRS FR-ANA-018, STATE-ANA-004, TR-ANA-004, TR-CTR-003 |
| DEC-GEN-032 | Người dùng xem và sửa hồ sơ (tên hiển thị, số điện thoại không bắt buộc, ngôn ngữ kết quả vi/en) và đổi hoặc đặt mật khẩu khi đã đăng nhập; email không đổi được. Ngôn ngữ chỉ đổi kết quả phân tích và chat, giao diện vẫn tiếng Việt. Áp dụng qua CR-AUTH-001. **Chờ Approver duyệt** | 2026-11-23 | Approver yêu cầu; chờ duyệt | SRS FR-AUTH-016, FR-AUTH-017, ERR-AUTH-007, ATTR-DATA-047, ATTR-DATA-048, FR-VIEW-015 |
| DEC-GEN-033 | Lưu ngữ cảnh chỉ lưu, không tự chạy phân tích; người dùng bấm "Bắt đầu phân tích" (chưa có ngữ cảnh thì báo ERR-CTR-002). Hủy trên hộp ngữ cảnh không lưu gì. Lần phân tích đã dừng không hiện trên trang hợp đồng: trang về như trước khi bấm chạy. Áp dụng qua CR-ANA-002. **Chờ Approver duyệt** | 2026-11-25 | Approver yêu cầu; chờ duyệt | PRD US-ANA-001; SRS FR-ANA-001, FR-CTR-005, FR-ANA-018 |

## 7. Session Log

| Date | Stage | Summary Of Work | Next Step |
|---|---|---|---|
| 2026-10-24 | S0 | Hỏi và ghi nhận thiết lập dự án (Q-INT), các câu trả lời S2 sớm (mục tiêu, ưu tiên, độ trễ, release criteria). Tạo file trạng thái. Gate S0 thông qua. | Viết `02-PRD.md` |
| 2026-10-24 | S2 | Viết `02-PRD.md` v0.1 (37 feature, 46 user story, 15 STR-QUAL, 4 RC). Chạy PD1-PD10: PD4 FAIL do thiếu mục tiêu cha cho các feature quản lý. | Hỏi OQ-GEN-005 và đợt OQ đầu, sửa PRD, Gate S2 |
| 2026-10-24 | S2 | Áp dụng trả lời OQ (PRD v0.2), PD1-PD10 PASS. Gate S2 thông qua. | Khung SRS (S3) |
| 2026-10-24 | S3 | Dựng khung `03-SRS.md` (mục lục, §0, §1.1, §1.2). Gate S3 thông qua; chọn Incremental Mode cho S4. | Viết SRS từng mục |
| 2026-10-24 | S4 | Viết SRS §1 (review xong), §2 (review xong), §3 (chờ review). Thêm OQ-GEN-011..013, OQ-ORG-001, OQ-ORG-002, OQ-SHARE-002. | Review §3, viết §4 |
| 2026-10-24 | S4 | Approver chọn viết hết rồi review một lần (DEC-GEN-021). Viết §7–§22, điền liên kết, sửa theo Micro Validation. SRS v0.8: 143 FR, 29 NFR, 20 UC, 30 BR. | S5 |
| 2026-10-24 | S5 | Chạy Macro Validation: 0 Critical, 4 Warning (`Reports/Validation-Report-2026-10-24.md`). | Approver review và quyết định Gate S5 |
| 2026-10-26 | S5 | Tạo phiếu `OQ-Answers.md` (40 OQ, kèm trích dẫn và đề xuất). Approver điền phiếu; trả lời thêm 4 câu (mục tiêu text 20 trang, cách đo chatbot, hiệu lực OTP, tài khoản Google đặt mật khẩu). Áp dụng vào PRD v0.3, SRS v0.9; đóng mọi OQ; thêm DEC-PRD-017..019, DEC-GEN-022, DEC-GEN-023; US-SHARE-002, FR-SHARE-005, NFR-COMPL-001 chuyển DEPRECATED. Chạy lại kiểm tra: 0 Critical, 2 Warning (`Reports/Validation-Report-2026-10-26.md`). | Approver review thay đổi và quyết định Gate S5 |
| 2026-10-26 | S5 | Approver đổi cách chống dò mật khẩu: khóa theo email thay cho theo IP, thêm giới hạn quên mật khẩu theo mô hình nhiều lớp (DEC-GEN-024). Thêm SEC-AUTH-007, FR-AUTH-014, FR-AUTH-015, ERR-AUTH-006; chạy lại kiểm tra. | Approver quyết định Gate S5 |
| 2026-10-27 | S5 | Approver yêu cầu §9 dùng tên tiếng Anh (DEC-GEN-025) và mô hình không gian kiểu GitHub: không gian cá nhân, nhiều tổ chức, chia sẻ hợp đồng cá nhân cho mọi tài khoản, không chuyển hợp đồng giữa các không gian (DEC-PRD-020, DEC-PRD-021). PRD v0.4, SRS v0.10; chạy lại kiểm tra. | Approver review và quyết định Gate S5 |
| 2026-10-27 | S5 | Approver thông qua Gate S5 (DEC-GEN-026): PRD v1.0, SRS v1.0 chuyển `APPROVED`. Tạo kế hoạch code theo phase `docs/03-implementation-phases.md` và sơ đồ tổng quan `docs/architecture/` (theo `Idea/Tech stack and flow.pdf`). | System design bước 2, rồi code phase 0 |
| 2026-10-27 | Sau S5 | System design: Approver chốt bước 1 và SD-01..SD-20 (`docs/02-system-design.md` §4); SD-04 = ASP.NET Core Identity, không dùng Cognito (DEC-GEN-028). Bỏ MFA khỏi MVP, sau MVP ưu tiên Should với MFA qua email mỗi lần đăng nhập (DEC-PRD-022): PRD v1.1, SRS v1.1. | Approver duyệt các mục REVIEW (F-AUTH-003, FR-AUTH-006, FR-AUTH-007) |
| 2026-11-17 | Sau S5 | CR-GEN-001 (`Change-Requests/CR-GEN-001.md`): Approver bỏ đọc bản scan và OCR, chỉ nhận PDF có lớp chữ và DOCX (DEC-PRD-023). PRD v1.2, SRS v1.2: US-ANA-003, STR-QUAL-002, DEP-GEN-002, FR-ANA-007, NFR-PERF-003, EXT-INTG-002, IF-INTG-002 DEPRECATED; các mục sửa về REVIEW. Code, `contracts/` (bỏ `jpg`, `png`, `is_scanned`), system design và sơ đồ cập nhật theo. | Approver duyệt các mục REVIEW; nhóm AI và IA duyệt thay đổi `contracts/` |
| 2026-11-17 | Sau S5 | CR-GEN-003 (`Change-Requests/CR-GEN-003.md`): Approver chốt model AI tự host bằng Docker trên EC2 thay Bedrock (DEC-GEN-029). PRD v1.3, SRS v1.4; các mục sửa về REVIEW. Cùng ngày: database chuyển sang Supabase (system design, `docs/04`), nền tảng worker để nhóm AI quyết | Approver duyệt các mục REVIEW; nhóm AI chọn model và loại máy GPU, xin quota; IA sửa policy IAM trong `security/` |
| 2026-11-22 | Sau S5 | CR-CTR-001 (`Change-Requests/CR-CTR-001.md`): Approver yêu cầu thùng rác 30 ngày và đổi tên hợp đồng (DEC-GEN-030, chờ duyệt). PRD v1.4, SRS v1.5: FR-CTR-018, NFR-PRIV-001, UC-CTR-005, US-CTR-007 sửa; thêm FR-CTR-021, FR-CTR-022; các mục về REVIEW. Code đã làm cùng lúc (nhánh `feat/trash-rename-retry`) | Approver duyệt các mục REVIEW |
| 2026-11-23 | Sau S5 | CR-ANA-001 (`Change-Requests/CR-ANA-001.md`): Approver yêu cầu dừng được phân tích đang chạy rồi chạy lại (DEC-GEN-031, chờ duyệt). SRS v1.6: thêm FR-ANA-018, STATE-ANA-004, TR-ANA-004; STATE-ANA-001, STATE-CTR-002, TR-CTR-003 có ghi chú `[CHANGE]`. Code làm cùng lúc | Approver duyệt các mục REVIEW; nhóm AI xem đề xuất ở `docs/06` |
| 2026-11-23 | Sau S5 | CR-AUTH-001 (`Change-Requests/CR-AUTH-001.md`): Approver yêu cầu trang hồ sơ (tên, số điện thoại, ngôn ngữ kết quả) và đổi mật khẩu khi đã đăng nhập (DEC-GEN-032, chờ duyệt). SRS v1.7: thêm FR-AUTH-016, FR-AUTH-017, ERR-AUTH-007, ATTR-DATA-047, ATTR-DATA-048; FR-VIEW-015 có ghi chú `[CHANGE]`. Code làm cùng lúc | Approver duyệt các mục REVIEW; IA xác nhận lưu số điện thoại |
| 2026-11-25 | Sau S5 | CR-ANA-002 (`Change-Requests/CR-ANA-002.md`): Approver yêu cầu lưu ngữ cảnh không tự chạy phân tích, bấm mới chạy, dừng thì trang về như trước (DEC-GEN-033, chờ duyệt). PRD v1.5: US-ANA-001 sửa. SRS v1.8: FR-ANA-001 sửa và về REVIEW; FR-CTR-005, FR-ANA-018 có ghi chú `[CHANGE]`. Code làm cùng lúc (nhánh `feat/manualAnalysisStart`) | Approver duyệt các mục REVIEW |
