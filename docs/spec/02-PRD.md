---
title: "ContractGuard - Product Requirements Document"
aliases:
  - "ContractGuard PRD"
document_type: prd
prd_schema_version: "1.0"
project: "ContractGuard"
project_profile: TEAM
prd_variant: Standard
prd_scope: Whole Product
output_language: vi
role_in_layer: Primary
upstream: []
version: "1.4"
status: REVIEW
authors:
  - "Agent"
reviewers:
  - "Thành viên AI"
  - "Thành viên IA"
approvers:
  - "SE của nhóm"
created: 2026-10-24
last_updated: 2026-11-17
tags:
  - prd
  - requirements
---

# Product Requirements Document - ContractGuard

PRD này theo thực tiễn quản lý sản phẩm (SVPG, Aha!, Atlassian), nội dung đặc tả chung theo ISO/IEC/IEEE 15289 và đặc tính chất lượng theo ISO/IEC 25010:2023. Không có tiêu chuẩn ISO hay IEEE nào định nghĩa PRD, nên tài liệu này không tuyên bố tuân thủ ISO.

## Table Of Contents

- [0. Document Control And Change History](#0-document-control-and-change-history)
- [1. Overview](#1-overview)
- [2. Problem Statement](#2-problem-statement)
- [3. Goals And Success Metrics](#3-goals-and-success-metrics)
- [4. Non-Goals And Out Of Scope](#4-non-goals-and-out-of-scope)
- [5. Target Users And Personas](#5-target-users-and-personas)
- [6. Key Scenarios](#6-key-scenarios)
- [7. Features](#7-features)
- [8. User Stories](#8-user-stories)
- [9. Product Quality Requirements](#9-product-quality-requirements)
- [10. UX And Design References](#10-ux-and-design-references)
- [11. Assumptions, Constraints And Dependencies](#11-assumptions-constraints-and-dependencies)
- [12. Release Criteria](#12-release-criteria)
- [13. Milestones](#13-milestones)
- [14. Risks, Open Questions And Decisions](#14-risks-open-questions-and-decisions)
- [15. Approval](#15-approval)

## 0. Document Control And Change History

| Field | Value |
|---|---|
| Product Name | ContractGuard |
| Document Name | Product Requirements Document |
| Document ID | CG-PRD |
| Version | `1.3` |
| Variant | Standard |
| PRD Scope | Whole Product |
| Role In Layer | Primary (không có StRS) |
| Product Owner | SE của nhóm |
| Status | `REVIEW` |
| Approver | SE của nhóm |

| Version | Date | Author | Changed Sections | Change Description | Status |
|---|---|---|---|---|---|
| 0.1 | 2026-10-24 | Agent | Initial document | Bản nháp đầu tiên, chuyển từ `docs/01-product-spec.md` v1.2 sang mẫu PRD của kit, bổ sung mục tiêu, release criteria và user stories từ Discovery Interview 2026-10-24 | REVIEW |
| 0.2 | 2026-10-24 | Agent | §1, §3, §5, §6, §7, §8, §11, §14 | Áp dụng trả lời OQ: thêm BG-GEN-004 và SC-GEN-006, gắn feature quản lý vào BG-GEN-004; ngày demo đầu tháng 12/2026; Owner tổ chức xem được mọi hợp đồng; giới hạn upload; region Singapore; thông báo khi tạo báo cáo thất bại; biện pháp chống prompt injection; mức rủi ro | REVIEW |
| 0.3 | 2026-10-26 | Agent | §0, §3, §5, §7, §8, §9, §14 | Áp dụng phiếu trả lời OQ (`OQ-Answers.md`), đóng 7 OQ của PRD: chỉ tiêu BG-GEN-004; persona; giãn mục tiêu hiệu năng (DEC-GEN-022); Owner tổ chức có toàn quyền trên mọi hợp đồng (DEC-PRD-017); bỏ chia sẻ toàn tổ chức, US-SHARE-002 DEPRECATED (DEC-PRD-018); nghĩa vụ dữ liệu cá nhân ngoài phạm vi bản demo (DEC-PRD-019); MFA bằng OTP qua email; đánh giá RISK-GEN-006, RISK-GEN-008 | REVIEW |
| 0.4 | 2026-10-27 | Agent | §3, §4, §5, §7, §8, §9, §14 | Mô hình không gian làm việc kiểu GitHub: thêm F-ORG-002 (không gian cá nhân, nhiều tổ chức), US-ORG-004, US-ORG-005, US-SHARE-005, F-CTR-009 (Won't); chia sẻ hợp đồng cá nhân cho mọi tài khoản; DEC-PRD-020, DEC-PRD-021 | REVIEW |
| 1.0 | 2026-10-27 | Approver | Toàn bộ | Baseline tại Gate S5: Approver thông qua PRD (DEC-GEN-026); mọi mục REVIEW chuyển APPROVED | APPROVED |
| 1.1 | 2026-10-27 | Agent | §7, §8, §14 | Bỏ MFA khỏi MVP. F-AUTH-003 Should → Won't, US-AUTH-004 DEPRECATED; thêm DEC-PRD-022 | REVIEW |
| 1.2 | 2026-11-17 | Agent | §6, §7, §8, §10, §11, §14 | CR-GEN-001: bỏ đọc bản scan và OCR, chỉ nhận PDF có lớp chữ và DOCX. US-ANA-003, STR-QUAL-002, DEP-GEN-002 DEPRECATED; F-CTR-001, F-ANA-002, US-CTR-001 sửa và về REVIEW; thêm DEC-PRD-023 | REVIEW |
| 1.3 | 2026-11-17 | Agent | §11, §12 | CR-GEN-003 (DEC-GEN-029): model AI tự host bằng Docker trên Amazon EC2 thay Amazon Bedrock. CON-PRJ-003, DEP-GEN-001, RISK-GEN-001, RISK-GEN-002 sửa và về REVIEW | REVIEW |

### 0.1 Tài liệu nguồn

| REF ID | Tài liệu | Phiên bản | Mức nguồn | Viết tắt dùng trong tài liệu |
|---|---|---|---|---|
| REF-GEN-001 | `docs/01-product-spec.md` | 1.2 (2026-10-21) | P3. Riêng các quyết định ở Phụ lục A do Approver đưa ra trực tiếp nên là P1 (xem §14.2). | PS |
| REF-GEN-002 | `docs/02-system-design.md` | 0.2 (2026-10-21) | P3 | SD |
| REF-GEN-003 | `docs/PRODUCT.md` | 2026-10-24 | P3 | PD |
| REF-GEN-004 | Thư mục `Idea/` | Không có phiên bản | P4 (đã được PS thay thế) | Idea |
| REF-GEN-005 | Discovery Interview, các session 2026-10-20, 2026-10-21, 2026-10-24 | - | P1 | DI |

Thứ tự ưu tiên nguồn được chốt tại DEC-GEN-012 (`00-Project-State.md`).

## 1. Overview

**Last Updated**: 2026-10-24
**Status**: APPROVED
**Author**: Agent

ContractGuard là ứng dụng web giúp doanh nghiệp SME Việt Nam đọc hiểu hợp đồng B2B do đối tác gửi sang (PS §1, §3.1). Hệ thống có ba trụ cột:

1. **Phân tích rủi ro:** phát hiện, giải thích, chấm điểm rủi ro trên 7 nhóm và gợi ý hướng xử lý bằng lời.
2. **Chatbot theo hợp đồng:** chatbot nhắn tin bằng văn bản, trả lời câu hỏi về hợp đồng đang xem, luôn kèm trích dẫn điều khoản.
3. **Quản lý hợp đồng:** thư viện hợp đồng của tổ chức, phiên bản, trạng thái duyệt, chia sẻ quyền theo email.

| Thuộc tính | Giá trị |
|---|---|
| Tình trạng hiện tại | Giai đoạn đặc tả, chưa có mã nguồn |
| Nhóm | 5 người: 1 SE, 3 AI, 1 IA (SD §1) |
| Bối cảnh | Sản phẩm demo cho chương trình thực tập FCAJ (DI 2026-10-21) |
| Lần phát hành mục tiêu | MVP cho buổi demo cuối kỳ thực tập. Ngày demo: đầu tháng 12/2026 (OQ-GEN-003) |
| Phạm vi tài liệu | Toàn bộ sản phẩm. MVP gồm các feature `Must` và `Should`; `Could` làm nếu còn thời gian; `Won't` nằm ngoài lần phát hành này (PS §6, DEC-GEN-009) |

## 2. Problem Statement

**Last Updated**: 2026-10-24
**Status**: APPROVED
**Author**: Agent

**Ai gặp vấn đề:** doanh nghiệp SME Việt Nam, đặc biệt các doanh nghiệp không có bộ phận pháp chế riêng. Người trực tiếp xử lý hợp đồng thường là giám đốc, chủ doanh nghiệp hoặc nhân viên sales, mua hàng (PS §3.1, PD Users).

**Cách xử lý hiện nay** (PS §2.1):

1. Đối tác gửi hợp đồng mẫu (PDF, Word hoặc bản scan) qua email hoặc Zalo. Hợp đồng nằm rải rác, không có nơi lưu tập trung.
2. Nhân viên hoặc giám đốc tự đọc 20–50 trang. Do không có chuyên môn luật, họ thường chỉ xem giá trị hợp đồng và lịch thanh toán, bỏ sót các điều khoản về phạt vi phạm, đơn phương chấm dứt, bồi thường và bảo mật dữ liệu.
3. Người đọc tự gạch xóa trong Word theo cảm tính, không có căn cứ để thuyết phục đối tác khi đàm phán.
4. Giám đốc ký trong tâm thế không chắc chắn hợp đồng có "bẫy" pháp lý hay không.
5. Doanh nghiệp chỉ phát hiện mình ở thế bất lợi khi sự cố đã xảy ra.

**Vì sao vấn đề quan trọng:**
- Hậu quả là thiệt hại tài chính, mất dữ liệu hoặc bị kẹt trong hợp đồng (PS §2.1).
- Riêng các điều khoản về an ninh mạng, dữ liệu và bên thứ ba ngày càng quan trọng với mọi hợp đồng có dữ liệu đi qua đối tác, nhưng gần như không được rà soát (PS §3.1).

## 3. Goals And Success Metrics

**Last Updated**: 2026-10-27
**Status**: APPROVED
**Author**: Agent

PRD sở hữu `BG` và `SC` vì bộ tài liệu không có BRS hay BRD (DEC-GEN-005).

| Goal ID | Goal | Metric | Target | Evaluation Time | Source | Status |
|---|---|---|---|---|---|---|
| BG-GEN-001 | Phát hiện đúng các rủi ro trong hợp đồng trước khi ký | SC-GEN-001, SC-GEN-002 | Recall ≥ 85%, precision ≥ 70% | Mỗi tuần trong 6 tuần chạy eval; lần đánh giá cuối trước buổi demo đầu tháng 12/2026 (OQ-GEN-003) | DI 2026-10-24, Q-PRD-001; PS §8; DI 2026-10-21 (eval 20 hợp đồng × 6 tuần) | APPROVED |
| BG-GEN-002 | Rút ngắn thời gian thẩm định hợp đồng từ việc đọc tay hàng giờ xuống vài phút | SC-GEN-003, SC-GEN-004 | p50 ≤ 2 phút, p95 ≤ 5 phút | Tại môi trường staging trước buổi demo đầu tháng 12/2026 (OQ-GEN-003) | DI 2026-10-24, Q-PRD-001, Q-PRD-007; DEC-GEN-022 | APPROVED |
| BG-GEN-003 | Demo FCAJ trọn vẹn | SC-GEN-005 | Kịch bản demo chạy trọn, không lỗi | Buổi demo cuối kỳ thực tập đầu tháng 12/2026 (OQ-GEN-003) | DI 2026-10-24, Q-PRD-001 | APPROVED |
| BG-GEN-004 | Quản lý hợp đồng tập trung tại một nơi cho cá nhân và tổ chức: tài khoản, không gian cá nhân, tổ chức, chia sẻ quyền, thư viện, trạng thái duyệt, phiên bản, xóa và nhật ký | SC-GEN-006 | 100% user story Must của các feature quản lý đạt tiêu chí nghiệm thu (OQ-GEN-010) | Trước buổi demo đầu tháng 12/2026 (OQ-GEN-003) | DI 2026-10-24, OQ-GEN-005 (Approver: các feature này thuộc chức năng quản lý; tạo mục tiêu mới vì chưa có mục tiêu về quản lý) | APPROVED |

| SC ID | Success Criterion | Measure | Target | Parent | Source | Status |
|---|---|---|---|---|---|---|
| SC-GEN-001 | Hệ thống tìm ra phần lớn rủi ro nghiêm trọng | Recall của finding mức High và Critical trên bộ eval 20 hợp đồng có nhãn | ≥ 85% | BG-GEN-001 | PS §8; DI 2026-10-24, Q-PRD-001 | APPROVED |
| SC-GEN-002 | Hệ thống ít báo nhầm | Precision của finding trên cùng bộ eval | ≥ 70% | BG-GEN-001 | PS §8; DI 2026-10-24, Q-PRD-001 | APPROVED |
| SC-GEN-003 | Người dùng nhận kết quả nhanh trong trường hợp thường gặp | Thời gian từ khi bắt đầu phân tích tới khi có kết quả, hợp đồng có lớp text 20 trang, worker đã chạy sẵn, phân vị 50 | ≤ 2 phút | BG-GEN-002 | DI 2026-10-24, Q-PRD-007; DEC-GEN-022 | APPROVED |
| SC-GEN-004 | Người dùng không phải chờ quá lâu kể cả trường hợp chậm | Cùng phép đo, phân vị 95 | ≤ 5 phút | BG-GEN-002 | DI 2026-10-24, Q-PRD-007; DEC-GEN-022 | APPROVED |
| SC-GEN-005 | Toàn bộ luồng demo hoạt động | Kịch bản demo (OPS-GEN-001, OPS-GEN-002, OPS-GEN-005, dùng hợp đồng Cloud Service Agreement mẫu) chạy từ upload tới xuất báo cáo mà không lỗi | 1 lần chạy trọn tại buổi demo | BG-GEN-003 | DI 2026-10-24, Q-PRD-001; PS §10 | APPROVED |
| SC-GEN-006 | Các chức năng quản lý hợp đồng hoạt động | Tỷ lệ user story Must của các feature quản lý (tài khoản, tổ chức, chia sẻ, thư viện, trạng thái, phiên bản, xóa) đạt tiêu chí nghiệm thu | 100% trước buổi demo | BG-GEN-004 | DI 2026-10-24, OQ-GEN-005, OQ-GEN-010 | APPROVED |

## 4. Non-Goals And Out Of Scope

**Last Updated**: 2026-10-27
**Status**: APPROVED
**Author**: Agent

Sản phẩm sẽ **không** làm những việc sau:

- **Không sửa hợp đồng.** Hệ thống không viết lại điều khoản, không soạn câu chữ thay thế, không tạo bản redline. Hệ thống chỉ chỉ ra rủi ro, giải thích và gợi ý hướng xử lý bằng lời (DEC-PRD-011). Các feature liên quan là `Won't`: F-CHAT-005, F-EXP-003.
- **Không đưa ra kết luận pháp lý.** Hệ thống không kết luận một điều khoản là "trái pháp luật" và không thay thế tư vấn pháp lý. Kết luận luôn ở dạng "rủi ro tiềm ẩn, cần xem xét", kèm căn cứ tham khảo nếu có (DEC-PRD-003, PS §4, §12).
- **Không phải chatbot đa năng và không có giọng nói.** Chatbot chỉ trả lời trong phạm vi hợp đồng và chỉ nhận văn bản (DEC-PRD-012).
- **Không gắn với một model AI cụ thể.** Việc chọn model được hoãn và sẽ quyết định dựa trên eval (DEC-GEN-010).
- **Không phân quyền chi tiết hơn Owner và Member** (DEC-PRD-010).
- **Không yêu cầu high availability trong MVP.** Chấp nhận downtime khi bảo trì (PS §8).
- **Ngoài phạm vi sản phẩm** (PS §12): ký điện tử, soạn hợp đồng từ đầu, tích hợp ERP hay hệ thống quản lý vòng đời hợp đồng (CLM), ứng dụng mobile native, thanh toán và gói cước.
- **Ngoài lần phát hành này** (`Won't`, DEC-GEN-009): ánh xạ framework (F-RISK-004), tự động rà soát lại (F-ANA-003), redline DOCX (F-EXP-003), chat trên toàn thư viện (F-CHAT-004), đề xuất câu chữ sửa đổi (F-CHAT-005), chia sẻ hợp đồng của tổ chức ra ngoài tổ chức (F-SHARE-002), SSO doanh nghiệp (F-AUTH-004), gắn tag hoặc thư mục (F-CTR-007), nhắc hạn hợp đồng (F-CTR-008), chuyển hợp đồng giữa các không gian (F-CTR-009).

## 5. Target Users And Personas

**Last Updated**: 2026-10-27
**Status**: APPROVED
**Author**: Agent

PRD sở hữu `UCL`, `STR` và `OPS` vì không có StRS.

| Persona ID | Persona | Description | Goals | Pain Points | Context Of Use | Technical Experience | Source |
|---|---|---|---|---|---|---|---|
| UCL-GEN-001 | Giám đốc / Chủ doanh nghiệp | Người ký hợp đồng | Quyết định nhanh có nên ký không | Ký trong tâm thế không chắc chắn, sợ có "bẫy" pháp lý bị bỏ sót | Văn phòng, ban ngày, laptop 14–15 inch, đọc kỹ và tập trung | Trung bình | PS §2.1, §3.2; PD Users; OQ-GEN-004 |
| UCL-GEN-002 | Nhân viên Sales / Mua hàng / Vận hành | Người nhận hợp đồng từ đối tác và chuẩn bị nội dung đàm phán | Chuẩn bị nội dung đàm phán với đối tác | Phải tự đọc 20–50 trang khi không có chuyên môn luật; dễ bỏ sót điều khoản bất lợi; sửa theo cảm tính, thiếu căn cứ | Văn phòng, ban ngày, laptop 14–15 inch, đọc kỹ và tập trung | Trung bình | PS §2.1, §3.2; PD Users; OQ-GEN-004 |
| UCL-GEN-003 | Người review chuyên môn | Pháp chế hoặc IT/Security, có thể thuê ngoài | Kiểm tra kỹ từng điểm rủi ro | Phải đọc toàn văn để tìm đúng điều khoản; thiếu bằng chứng tập trung | Làm việc trên laptop, thường review khi có yêu cầu | Cao | PS §3.2; PD Users; OQ-GEN-004 |
| UCL-GEN-004 | Owner tổ chức | Người quản lý thành viên và quyền truy cập của tổ chức | Quản lý thành viên và quyền truy cập | Không biết ai đang giữ hợp đồng nào | Dùng thỉnh thoảng, khi có người mới vào hoặc người nghỉ việc | Trung bình | PS §3.2, §6.1; PD Users; OQ-GEN-004 |

**Quan hệ giữa persona và vai trò trong hệ thống:**
- Hệ thống có hai cấp vai trò, mỗi cấp có Owner và Member (PS §6.1, DEC-PRD-010). Một người thuộc UCL-GEN-001, UCL-GEN-002 hoặc UCL-GEN-003 thường là Member tổ chức, hoặc chỉ dùng không gian cá nhân mà không thuộc tổ chức nào (DEC-PRD-020).
- Người đó trở thành Owner hợp đồng với hợp đồng mình tạo, và Member hợp đồng với hợp đồng được chia sẻ.
- UCL-GEN-004 là người giữ vai trò Owner tổ chức, có mọi quyền của Owner hợp đồng trên mọi hợp đồng của tổ chức (DEC-PRD-017).

**Key needs:**

| Need ID | Statement | Persona | Parent | Priority | Source | Status |
|---|---|---|---|---|---|---|
| STR-RISK-001 | Nhân viên nhận hợp đồng phải có thể biết hợp đồng do đối tác gửi có những rủi ro nào, kể cả các điều khoản bị thiếu, mà không cần chuyên môn luật. | UCL-GEN-002 | BG-GEN-001 | Must | PS §2.1, §3.2, §6.3 | APPROVED |
| STR-RISK-002 | Giám đốc phải có thể biết trong vài phút hợp đồng có rủi ro nghiêm trọng nào và mức rủi ro tổng thể, để quyết định có ký hay không. | UCL-GEN-001 | BG-GEN-002 | Must | PS §3.2; PD Product Purpose | APPROVED |
| STR-RISK-003 | Người review chuyên môn phải có thể kiểm chứng từng rủi ro qua trích dẫn nguyên văn, vị trí trong file và căn cứ tham khảo. | UCL-GEN-003 | BG-GEN-001 | Must | PS §3.2, §4 (nguyên tắc 2) | APPROVED |
| STR-RISK-004 | Nhân viên nhận hợp đồng phải có thể hiểu vì sao một điều khoản là rủi ro và nên đàm phán lại theo hướng nào. | UCL-GEN-002 | BG-GEN-001 | Must | PS §3.2, §7.5 | APPROVED |
| STR-RISK-005 | Người review chuyên môn phải có thể xem riêng mức đáp ứng các yêu cầu Cybersecurity, Data & Privacy và Third-Party của hợp đồng. | UCL-GEN-003 | BG-GEN-001 | Must | PS §3.1, §3.2, §7.3 | APPROVED |
| STR-CHAT-001 | Giám đốc và nhân viên phải có thể hỏi bằng ngôn ngữ tự nhiên về hợp đồng đang xem và nhận câu trả lời có trích dẫn điều khoản. | UCL-GEN-001, UCL-GEN-002 | BG-GEN-002 | Must | PS §1, §6.5 | APPROVED |
| STR-CTR-001 | Nhân viên phải có thể lưu hợp đồng và các phiên bản sau đàm phán ở một nơi tập trung của tổ chức, và theo dõi trạng thái duyệt của chúng. | UCL-GEN-002 | BG-GEN-004 | Must | PS §2.1, §6.2 | APPROVED |
| STR-SHARE-001 | Owner tổ chức phải có thể kiểm soát ai trong tổ chức được truy cập từng hợp đồng. | UCL-GEN-004 | BG-GEN-004 | Must | PS §3.2, §6.1 | APPROVED |
| STR-SEC-001 | Owner tổ chức phải có thể đảm bảo hợp đồng của tổ chức không bị người ngoài tổ chức truy cập. | UCL-GEN-004 | BG-GEN-004 | Must | PS §4 (nguyên tắc 6) | APPROVED |
| STR-EXP-001 | Nhân viên phải có thể mang kết quả phân tích ra ngoài hệ thống dưới dạng báo cáo để gửi người duyệt hoặc đối tác. | UCL-GEN-002 | BG-GEN-003 | Must | PS §2.2, §6.6 | APPROVED |

## 6. Key Scenarios

**Last Updated**: 2026-10-24
**Status**: APPROVED
**Author**: Agent

| Scenario ID | Scenario | Persona | Trigger | Steps Summary | Outcome | Related Needs |
|---|---|---|---|---|---|---|
| OPS-GEN-001 | Thẩm định một hợp đồng mới | UCL-GEN-002, UCL-GEN-001 | Đối tác gửi hợp đồng cần ký | Upload → khai báo ngữ cảnh → phân tích tự động → xem tổng quan và finding | Biết hợp đồng có rủi ro gì, ở đâu, vì sao | STR-RISK-001, STR-RISK-002, STR-RISK-003, STR-RISK-004, STR-RISK-005 |
| OPS-GEN-002 | Hỏi đáp về hợp đồng | UCL-GEN-001, UCL-GEN-002 | Có thắc mắc cụ thể khi đọc kết quả | Mở chatbot → hỏi → đọc câu trả lời → bấm trích dẫn để xem điều khoản | Có câu trả lời có căn cứ trong hợp đồng | STR-CHAT-001 |
| OPS-GEN-003 | Chia sẻ và duyệt hợp đồng | UCL-GEN-002, UCL-GEN-003, UCL-GEN-001 | Cần người khác review hoặc duyệt | Chia sẻ theo email → người review đánh dấu finding → chuyển trạng thái duyệt | Hợp đồng được duyệt hoặc từ chối, có lịch sử | STR-SHARE-001, STR-CTR-001, STR-RISK-003 |
| OPS-GEN-004 | Đàm phán và phân tích phiên bản mới | UCL-GEN-002 | Đối tác gửi bản sửa sau đàm phán | Upload phiên bản mới → phân tích → so kết quả với phiên bản trước | Biết phiên bản mới còn rủi ro gì | STR-CTR-001, STR-RISK-001 |
| OPS-GEN-005 | Xuất báo cáo | UCL-GEN-002, UCL-GEN-001 | Cần gửi kết quả cho người duyệt hoặc đối tác | Chọn xuất PDF hoặc DOCX → tải file | Có báo cáo để gửi đi | STR-EXP-001 |
| OPS-GEN-006 | Quản lý tổ chức | UCL-GEN-004 | Có người mới cần dùng hệ thống | Mời qua email → người được mời tham gia → cấp hoặc thu hồi quyền Owner khi cần | Thành viên có đúng quyền | STR-SHARE-001, STR-SEC-001 |

### 6.1 Chi tiết kịch bản

**OPS-GEN-001 Thẩm định một hợp đồng mới** (PS §2.2, §10)

1. Nhân viên kéo thả file PDF có lớp chữ hoặc DOCX vào hệ thống.
2. Nhân viên khai báo ngữ cảnh: bên mình là bên nào, đối tác, loại hợp đồng, đối tác cung cấp gì, có xử lý dữ liệu cá nhân hay không. Hệ thống điền sẵn những gì trích xuất được.
3. Hệ thống phân tích tự động và hiển thị tiến trình theo bước.
4. Nhân viên và giám đốc xem điểm tổng, điểm từng nhóm, danh sách finding và các điều khoản được tô màu trên văn bản.
- **Ngoại lệ A:** file sai định dạng, quá giới hạn hoặc chứa mã độc. Hệ thống từ chối ngay ở bước upload và báo lý do.
- **Ngoại lệ B:** phân tích thất bại sau khi đã thử lại. Hệ thống báo lỗi kèm lý do và không hiển thị kết quả dở dang.
- **Ngoại lệ C:** hợp đồng chứa nội dung có dấu hiệu prompt injection. Người dùng thấy cảnh báo.

**OPS-GEN-002 Hỏi đáp về hợp đồng** (PS §6.5, §10 bước 6)

1. Người dùng mở chatbot cạnh văn bản hợp đồng.
2. Người dùng đặt câu hỏi bằng tiếng Việt hoặc tiếng Anh.
3. Chatbot trả lời kèm trích dẫn điều khoản. Người dùng bấm trích dẫn để nhảy tới vị trí đó trong văn bản.
- **Ngoại lệ:** hợp đồng không đề cập tới điều được hỏi. Chatbot nói rõ là hợp đồng không có, thay vì suy đoán.

**OPS-GEN-003 Chia sẻ và duyệt hợp đồng** (PS §2.2 bước 5–6, §6.1, §6.2)

1. Owner hợp đồng chia sẻ cho email của người review hoặc giám đốc trong tổ chức.
2. Người review đánh dấu từng finding: chấp nhận rủi ro, cần đàm phán, không đúng hoặc đã xử lý, kèm ghi chú.
3. Người có quyền chuyển trạng thái hợp đồng theo luồng `Mới` → `Đang phân tích` → `Chờ review` → `Đang đàm phán` → `Đã duyệt` hoặc `Từ chối`.
- **Ngoại lệ:** Owner hợp đồng thu hồi quyền chia sẻ. Người bị thu hồi mất quyền truy cập ngay lập tức.

**OPS-GEN-004 Đàm phán và phân tích phiên bản mới** (PS §2.2 bước 6, §6.2)

1. Nhân viên upload phiên bản mới cho cùng hợp đồng.
2. Hệ thống phân tích phiên bản mới. Mỗi phiên bản có kết quả riêng.
3. Nhân viên xem lịch sử phiên bản và kết quả của từng phiên bản.
- **Ngoại lệ:** phiên bản mới phân tích thất bại. Kết quả của các phiên bản trước vẫn giữ nguyên.

**OPS-GEN-005 Xuất báo cáo** (PS §6.6, §10 bước 8)

1. Người dùng chọn xuất báo cáo dạng PDF hoặc DOCX.
2. Hệ thống tạo báo cáo gồm thông tin hợp đồng, điểm tổng, điểm theo nhóm, toàn bộ finding, checklist cyber và dòng miễn trừ trách nhiệm.
- **Ngoại lệ:** tạo báo cáo thất bại. Hệ thống hiển thị thông báo tạo báo cáo thất bại (OQ-EXP-001).

**OPS-GEN-006 Quản lý tổ chức** (PS §6.1)

1. Owner tổ chức mời thành viên qua email.
2. Người được mời nhận link và tham gia tổ chức.
3. Owner tổ chức cấp hoặc thu hồi quyền Owner, hoặc xóa thành viên.
- **Ngoại lệ:** thao tác làm tổ chức không còn Owner nào bị hệ thống từ chối.

## 7. Features

**Last Updated**: 2026-10-27
**Status**: APPROVED
**Author**: Agent

### 7.1 Feature List

Target Release:
- `MVP`: feature `Must` và `Should`.
- `MVP (nếu còn thời gian)`: feature `Could`.
- `Sau MVP`: feature `Won't` (PS §6, DEC-GEN-009).

| Feature ID | Feature | Description | Personas | Goals | Needs (STR) | Priority | Target Release | Status |
|---|---|---|---|---|---|---|---|---|
| F-AUTH-001 | Tài khoản email | Đăng ký, đăng nhập bằng email và mật khẩu; xác thực email; quên mật khẩu | UCL-GEN-001, UCL-GEN-002, UCL-GEN-003, UCL-GEN-004 | BG-GEN-004 | STR-SEC-001 | Must | MVP | APPROVED |
| F-AUTH-002 | Đăng nhập Google | Đăng nhập bằng tài khoản Google (OIDC) | UCL-GEN-001, UCL-GEN-002, UCL-GEN-003, UCL-GEN-004 | BG-GEN-004 | STR-SEC-001 | Should | MVP | APPROVED |
| F-AUTH-003 | Xác thực đa yếu tố | MFA cho tài khoản. Sau MVP, ưu tiên Should: mỗi lần đăng nhập đều bắt buộc MFA bằng mã gửi qua email; không dùng ứng dụng xác thực bên thứ ba | UCL-GEN-001, UCL-GEN-002, UCL-GEN-003, UCL-GEN-004 | - | - | Won't | Sau MVP | REVIEW |
| F-ORG-001 | Tổ chức và thành viên | Tạo tổ chức, mời thành viên qua email, vai trò Owner/Member cấp tổ chức | UCL-GEN-004 | BG-GEN-004 | STR-SHARE-001, STR-SEC-001 | Must | MVP | APPROVED |
| F-ORG-002 | Không gian cá nhân và nhiều tổ chức | Không gian cá nhân có sẵn khi đăng ký; tạo hoặc tham gia nhiều tổ chức; chuyển giữa các không gian | UCL-GEN-001, UCL-GEN-002, UCL-GEN-004 | BG-GEN-004 | STR-CTR-001, STR-SEC-001 | Must | MVP | APPROVED |
| F-SHARE-001 | Chia sẻ hợp đồng | Chia sẻ hợp đồng theo email (thành viên tổ chức với hợp đồng của tổ chức; mọi tài khoản với hợp đồng cá nhân), vai trò Owner/Member cấp hợp đồng, thu hồi quyền | UCL-GEN-002, UCL-GEN-004 | BG-GEN-004 | STR-SHARE-001 | Must | MVP | APPROVED |
| F-CTR-001 | Upload và khai báo ngữ cảnh | Upload file hợp đồng; khai báo bên mình, đối tác, loại hợp đồng, dịch vụ, dữ liệu, mức quan trọng | UCL-GEN-002 | BG-GEN-001, BG-GEN-003 | STR-RISK-001 | Must | MVP | REVIEW |
| F-CTR-002 | Metadata tự trích xuất | Tự trích các bên, loại, giá trị, đồng tiền, ngày hiệu lực, ngày hết hạn, luật áp dụng; người dùng sửa được | UCL-GEN-002 | BG-GEN-002 | STR-RISK-002 | Must | MVP | APPROVED |
| F-CTR-003 | Thư viện hợp đồng | Danh sách, tìm kiếm, lọc, sắp xếp hợp đồng | UCL-GEN-002, UCL-GEN-001 | BG-GEN-004 | STR-CTR-001 | Must | MVP | APPROVED |
| F-CTR-004 | Trạng thái duyệt | Luồng trạng thái từ `Mới` tới `Đã duyệt` hoặc `Từ chối` | UCL-GEN-002, UCL-GEN-001 | BG-GEN-004 | STR-CTR-001 | Must | MVP | APPROVED |
| F-CTR-005 | Phiên bản hợp đồng | Upload phiên bản mới, kết quả riêng cho từng phiên bản, lịch sử phiên bản | UCL-GEN-002 | BG-GEN-004 | STR-CTR-001 | Must | MVP | APPROVED |
| F-CTR-006 | Xóa vĩnh viễn hợp đồng | Xóa file gốc, dữ liệu trích xuất, embedding, finding và lịch sử chat | UCL-GEN-002 | BG-GEN-004 | STR-SEC-001 | Must | MVP | APPROVED |
| F-AUD-001 | Nhật ký hoạt động | Ghi ai upload, xem, chia sẻ, xuất, đổi trạng thái trên mỗi hợp đồng | UCL-GEN-001, UCL-GEN-003 | BG-GEN-004 | STR-CTR-001 | Should | MVP | APPROVED |
| F-ANA-001 | Phân tích tự động | Tự động chạy sau khi xác nhận ngữ cảnh, hiển thị tiến trình, báo lỗi, chạy lại | UCL-GEN-002 | BG-GEN-001, BG-GEN-002 | STR-RISK-001, STR-RISK-002 | Must | MVP | APPROVED |
| F-ANA-002 | Đọc và tách điều khoản | Trích xuất văn bản từ PDF có lớp chữ và DOCX, giữ cấu trúc và vị trí, tách và phân loại điều khoản vào 21 loại cộng "Khác" | UCL-GEN-002, UCL-GEN-003 | BG-GEN-001 | STR-RISK-001, STR-RISK-003 | Must | MVP | REVIEW |
| F-RISK-001 | Đánh giá rủi ro 7 nhóm | Sinh finding theo cấu trúc chuẩn, mỗi finding một nhóm chính; phát hiện điều khoản thiếu; căn cứ tham khảo; nhóm N/A | UCL-GEN-002, UCL-GEN-003 | BG-GEN-001 | STR-RISK-001, STR-RISK-003, STR-RISK-004 | Must | MVP | APPROVED |
| F-RISK-002 | Checklist Cyber/Privacy/Third-Party | Chạy 20 check, trả trạng thái PASS/PARTIAL/MISSING/N/A, hiển thị kèm liên kết tới điều khoản | UCL-GEN-003 | BG-GEN-001 | STR-RISK-005 | Must | MVP | APPROVED |
| F-RISK-003 | Chấm điểm rủi ro | Điểm 0–100 theo từng nhóm và cho cả hợp đồng, có điều chỉnh theo ngữ cảnh | UCL-GEN-001 | BG-GEN-001, BG-GEN-002 | STR-RISK-002 | Must | MVP | APPROVED |
| F-SEC-001 | Bảo vệ đầu vào | Kiểm tra file, quét mã độc, làm sạch tài liệu; phát hiện và cảnh báo prompt injection | UCL-GEN-002 | BG-GEN-001 | STR-RISK-001, STR-SEC-001 | Must | MVP | APPROVED |
| F-VIEW-001 | Dashboard rủi ro | Tổng quan điểm, số finding theo severity, khuyến nghị tổng; danh sách và chi tiết finding | UCL-GEN-001, UCL-GEN-002 | BG-GEN-002, BG-GEN-003 | STR-RISK-002, STR-RISK-004 | Must | MVP | APPROVED |
| F-VIEW-002 | Document viewer | Hiển thị hợp đồng gốc, tô màu điều khoản theo severity, liên kết hai chiều với finding | UCL-GEN-003, UCL-GEN-002 | BG-GEN-002, BG-GEN-003 | STR-RISK-003 | Must | MVP | APPROVED |
| F-VIEW-003 | Xử lý finding | Đánh dấu trạng thái xử lý từng finding, kèm ghi chú | UCL-GEN-003, UCL-GEN-002 | BG-GEN-002 | STR-RISK-004 | Should | MVP | APPROVED |
| F-CHAT-001 | Chatbot theo hợp đồng | Hỏi đáp Việt/Anh về hợp đồng đang mở, trả lời có trích dẫn, nói rõ khi hợp đồng không đề cập | UCL-GEN-001, UCL-GEN-002 | BG-GEN-002, BG-GEN-003 | STR-CHAT-001 | Must | MVP | APPROVED |
| F-CHAT-002 | Gợi ý câu hỏi | Gợi ý các câu hỏi nhanh | UCL-GEN-001, UCL-GEN-002 | BG-GEN-002 | STR-CHAT-001 | Should | MVP | APPROVED |
| F-CHAT-003 | Lịch sử chat | Lưu lịch sử hội thoại theo hợp đồng, người được chia sẻ xem được | UCL-GEN-001, UCL-GEN-002, UCL-GEN-003 | BG-GEN-002 | STR-CHAT-001 | Should | MVP | APPROVED |
| F-EXP-001 | Xuất báo cáo PDF và DOCX | Báo cáo rủi ro dạng PDF và DOCX | UCL-GEN-002, UCL-GEN-001 | BG-GEN-003 | STR-EXP-001 | Must | MVP | APPROVED |
| F-EXP-002 | Tùy chọn báo cáo | Chọn ngôn ngữ (Việt/Anh) và phạm vi finding của báo cáo | UCL-GEN-002 | BG-GEN-003 | STR-EXP-001 | Should | MVP | APPROVED |
| F-CMP-001 | So sánh phiên bản | So sánh hai phiên bản, chỉ ra điểm bị yếu đi và mức thay đổi điểm rủi ro | UCL-GEN-002 | BG-GEN-002 | STR-CTR-001 | Could | MVP (nếu còn thời gian) | APPROVED |
| F-POL-001 | Company Policy | Tổ chức upload chính sách nội bộ, hệ thống chấm hợp đồng theo chính sách | UCL-GEN-004, UCL-GEN-003 | BG-GEN-001 | STR-RISK-005 | Could | MVP (nếu còn thời gian) | APPROVED |
| F-RISK-004 | Ánh xạ framework | Ánh xạ finding sang NIST CSF 2.0, ISO/IEC 27001, SOC 2 | UCL-GEN-003 | - | - | Won't | Sau MVP | APPROVED |
| F-ANA-003 | Tự động rà soát lại | Chạy lại các hợp đồng khi rule hoặc policy thay đổi và cảnh báo | UCL-GEN-002 | - | - | Won't | Sau MVP | APPROVED |
| F-EXP-003 | Redline DOCX | File hợp đồng có track changes và comment tại vị trí rủi ro | UCL-GEN-002 | - | - | Won't | Sau MVP | APPROVED |
| F-CHAT-004 | Chat trên toàn thư viện | Hỏi đáp trên mọi hợp đồng người dùng có quyền xem | UCL-GEN-001 | - | - | Won't | Sau MVP | APPROVED |
| F-CHAT-005 | Đề xuất câu chữ sửa đổi | Sinh đoạn điều khoản thay thế, viết lại điều khoản qua chat | UCL-GEN-002 | - | - | Won't | Sau MVP | APPROVED |
| F-SHARE-002 | Chia sẻ hợp đồng của tổ chức ra ngoài tổ chức | Chia sẻ hợp đồng của tổ chức cho email ngoài tổ chức; hợp đồng cá nhân đã chia sẻ được cho mọi tài khoản (DEC-PRD-021) | UCL-GEN-002 | - | - | Won't | Sau MVP | APPROVED |
| F-AUTH-004 | SSO doanh nghiệp | Đăng nhập bằng SAML SSO | UCL-GEN-004 | - | - | Won't | Sau MVP | APPROVED |
| F-CTR-007 | Tag và thư mục | Gắn tag hoặc thư mục cho hợp đồng | UCL-GEN-002 | - | - | Won't | Sau MVP | APPROVED |
| F-CTR-008 | Nhắc hạn hợp đồng | Nhắc trước ngày hết hạn hoặc ngày tự gia hạn | UCL-GEN-002 | - | - | Won't | Sau MVP | APPROVED |
| F-CTR-009 | Chuyển hợp đồng giữa không gian | Chuyển hợp đồng từ không gian cá nhân sang tổ chức; chuyển quyền Owner của hợp đồng cá nhân sang người khác | UCL-GEN-002 | - | - | Won't | Sau MVP | APPROVED |

### 7.2 Feature Details

Cột "Nguồn" giữ mã yêu cầu trong `docs/01-product-spec.md` (PS) để truy vết. Business rule của các feature phân tích rủi ro (7 nhóm, nhóm chính, N/A, ranh giới, severity, ngưỡng điểm) được định nghĩa tại SRS §7.

#### F-AUTH-001 Tài khoản email

| Attribute | Value |
|---|---|
| Description | Đăng ký và đăng nhập bằng email và mật khẩu, có xác thực email và quên mật khẩu |
| Personas | UCL-GEN-001, UCL-GEN-002, UCL-GEN-003, UCL-GEN-004 |
| Goals | BG-GEN-004 |
| Needs | STR-SEC-001 |
| User Stories | US-AUTH-001, US-AUTH-002 |
| Business Rules | None |
| Priority | Must |
| Source | PS §6.1 ACC-01 |
| Status | APPROVED |

#### F-AUTH-002 Đăng nhập Google

| Attribute | Value |
|---|---|
| Description | Đăng nhập bằng tài khoản Google (OIDC) |
| Personas | UCL-GEN-001, UCL-GEN-002, UCL-GEN-003, UCL-GEN-004 |
| Goals | BG-GEN-004 |
| Needs | STR-SEC-001 |
| User Stories | US-AUTH-003 |
| Business Rules | None |
| Priority | Should |
| Source | PS §6.1 ACC-02 |
| Status | APPROVED |

#### F-AUTH-003 Xác thực đa yếu tố

| Attribute | Value |
|---|---|
| Description | MFA cho tài khoản. Sau MVP, ưu tiên Should: mỗi lần đăng nhập đều bắt buộc MFA bằng mã gửi qua email; không dùng ứng dụng xác thực bên thứ ba. [CHANGE] 2026-10-27: bỏ MFA khỏi MVP (DEC-PRD-022). Trước đó: Should, yếu tố thứ hai là mã OTP gửi về email (OQ-AUTH-001) |
| Personas | UCL-GEN-001, UCL-GEN-002, UCL-GEN-003, UCL-GEN-004 |
| Goals | BG-GEN-004 |
| Needs | STR-SEC-001 |
| User Stories | US-AUTH-004 (DEPRECATED) |
| Business Rules | None |
| Priority | Won't |
| Source | PS §6.1 ACC-09 |
| Status | REVIEW |

#### F-ORG-001 Tổ chức và thành viên

| Attribute | Value |
|---|---|
| Description | Người dùng tạo tổ chức khi cần làm việc nhóm và có thể thuộc nhiều tổ chức (DEC-PRD-020). Người tạo tổ chức là Owner tổ chức. Owner tổ chức mời thành viên qua email, cấp hoặc thu hồi quyền Owner, xóa thành viên, sửa cài đặt tổ chức, và tự động xem được mọi hợp đồng của tổ chức (DEC-PRD-013). Tổ chức có thể có nhiều Owner và luôn phải còn ít nhất một Owner. |
| Personas | UCL-GEN-004 |
| Goals | BG-GEN-004 |
| Needs | STR-SHARE-001, STR-SEC-001 |
| User Stories | US-ORG-001, US-ORG-002, US-ORG-003 |
| Business Rules | Quy tắc vai trò Owner/Member, định nghĩa tại SRS §7 |
| Priority | Must |
| Source | PS §6.1 ACC-03, ACC-04, ACC-05 và bảng vai trò |
| Status | APPROVED |

#### F-ORG-002 Không gian cá nhân và nhiều tổ chức

| Attribute | Value |
|---|---|
| Description | Mỗi tài khoản có sẵn một không gian cá nhân ngay khi đăng ký, nên dùng được hệ thống mà không cần tổ chức. Muốn làm việc nhóm thì tạo tổ chức. Một người tạo hoặc tham gia được nhiều tổ chức và chuyển qua lại giữa không gian cá nhân và các tổ chức, giống mô hình GitHub. Mỗi hợp đồng thuộc đúng một không gian (DEC-PRD-020). |
| Personas | UCL-GEN-001, UCL-GEN-002, UCL-GEN-004 |
| Goals | BG-GEN-004 |
| Needs | STR-CTR-001, STR-SEC-001 |
| User Stories | US-ORG-004, US-ORG-005 |
| Business Rules | Quy tắc không gian làm việc, định nghĩa tại SRS §7 (BR-ORG-004) |
| Priority | Must |
| Source | Approver 2026-10-27 (DEC-PRD-020) |
| Status | APPROVED |

#### F-SHARE-001 Chia sẻ hợp đồng

| Attribute | Value |
|---|---|
| Description | Hợp đồng mặc định chỉ người tạo và các Owner tổ chức thấy (DEC-PRD-013). Owner hợp đồng chia sẻ cho từng email trong tổ chức, thu hồi chia sẻ (hiệu lực ngay lập tức), chuyển quyền Owner. Owner tổ chức có mọi quyền của Owner hợp đồng trên mọi hợp đồng của tổ chức (DEC-PRD-017). Không có chia sẻ cho toàn tổ chức (DEC-PRD-018). Hợp đồng của tổ chức chỉ chia sẻ cho thành viên tổ chức đó; hợp đồng cá nhân chia sẻ được cho email của bất kỳ tài khoản đã đăng ký (DEC-PRD-021). Member hợp đồng xem kết quả, chat, xuất báo cáo, upload phiên bản mới, chạy lại phân tích, sửa metadata và ngữ cảnh, đổi trạng thái hợp đồng và trạng thái finding. |
| Personas | UCL-GEN-002, UCL-GEN-004 |
| Goals | BG-GEN-004 |
| Needs | STR-SHARE-001 |
| User Stories | US-SHARE-001, US-SHARE-003, US-SHARE-004, US-SHARE-005; US-SHARE-002 đã DEPRECATED (DEC-PRD-018) |
| Business Rules | Quy tắc vai trò Owner/Member cấp hợp đồng, định nghĩa tại SRS §7 |
| Priority | Must |
| Source | PS §6.1 ACC-06, ACC-07, ACC-08 và bảng vai trò |
| Status | APPROVED |

#### F-CTR-001 Upload và khai báo ngữ cảnh

| Attribute | Value |
|---|---|
| Description | Upload hợp đồng bằng kéo thả hoặc chọn file (PDF có lớp text, DOCX). Kiểm tra định dạng và kích thước ngay ở bước upload. Khai báo ngữ cảnh: bên mình là bên nào (bên mua/khách hàng hay bên bán/nhà cung cấp), tên đối tác, loại hợp đồng, đối tác cung cấp gì, có xử lý dữ liệu cá nhân hoặc nhạy cảm không, mức độ quan trọng của dịch vụ. Hệ thống điền sẵn thông tin trích xuất được để người dùng xác nhận. [CHANGE] Changed by CR-GEN-001 (2026-11-17): bỏ PDF scan, JPG, PNG (DEC-PRD-023). |
| Personas | UCL-GEN-002 |
| Goals | BG-GEN-001, BG-GEN-003 |
| Needs | STR-RISK-001 |
| User Stories | US-CTR-001, US-CTR-002 |
| Business Rules | Rủi ro luôn được đánh giá từ góc nhìn bên mình, định nghĩa tại SRS §7 |
| Priority | Must |
| Source | PS §2.2, §5, §6.2 CON-01, CON-02 |
| Status | REVIEW |

#### F-CTR-002 Metadata tự trích xuất

| Attribute | Value |
|---|---|
| Description | Tự trích các bên, loại hợp đồng, giá trị, đồng tiền, ngày hiệu lực, ngày hết hạn, luật áp dụng. Người dùng sửa được. |
| Personas | UCL-GEN-002 |
| Goals | BG-GEN-002 |
| Needs | STR-RISK-002 |
| User Stories | US-CTR-003 |
| Business Rules | None |
| Priority | Must |
| Source | PS §6.2 CON-03 |
| Status | APPROVED |

#### F-CTR-003 Thư viện hợp đồng

| Attribute | Value |
|---|---|
| Description | Danh sách hợp đồng người dùng có quyền xem; tìm theo tên hoặc đối tác; lọc theo mức rủi ro, trạng thái, loại hợp đồng; sắp xếp |
| Personas | UCL-GEN-002, UCL-GEN-001 |
| Goals | BG-GEN-004 |
| Needs | STR-CTR-001 |
| User Stories | US-CTR-004 |
| Business Rules | None |
| Priority | Must |
| Source | PS §6.2 CON-04 |
| Status | APPROVED |

#### F-CTR-004 Trạng thái duyệt

| Attribute | Value |
|---|---|
| Description | Trạng thái duyệt: `Mới` → `Đang phân tích` → `Chờ review` → `Đang đàm phán` → `Đã duyệt` hoặc `Từ chối`. Owner và Member của hợp đồng đều được chuyển trạng thái. |
| Personas | UCL-GEN-002, UCL-GEN-001 |
| Goals | BG-GEN-004 |
| Needs | STR-CTR-001 |
| User Stories | US-CTR-005 |
| Business Rules | Quy tắc chuyển trạng thái, định nghĩa tại SRS §7 và §13 |
| Priority | Must |
| Source | PS §6.2 CON-05 |
| Status | APPROVED |

#### F-CTR-005 Phiên bản hợp đồng

| Attribute | Value |
|---|---|
| Description | Upload phiên bản mới (V2, V3...) cho cùng một hợp đồng. Mỗi phiên bản có kết quả phân tích riêng. Xem được lịch sử phiên bản. |
| Personas | UCL-GEN-002 |
| Goals | BG-GEN-004 |
| Needs | STR-CTR-001 |
| User Stories | US-CTR-006 |
| Business Rules | None |
| Priority | Must |
| Source | PS §6.2 CON-06 |
| Status | APPROVED |

#### F-CTR-006 Xóa vĩnh viễn hợp đồng

| Attribute | Value |
|---|---|
| Description | Owner hợp đồng xóa hợp đồng. Hệ thống xóa vĩnh viễn file gốc, dữ liệu trích xuất, embedding, finding và lịch sử chat. |
| Personas | UCL-GEN-002 |
| Goals | BG-GEN-004 |
| Needs | STR-SEC-001 |
| User Stories | US-CTR-007 |
| Business Rules | None |
| Priority | Must |
| Source | PS §6.1 (bảng vai trò), §6.2 CON-07, §8 (Riêng tư) |
| Status | APPROVED |

#### F-AUD-001 Nhật ký hoạt động

| Attribute | Value |
|---|---|
| Description | Ghi nhật ký trên mỗi hợp đồng: ai upload, xem, chia sẻ, xuất, đổi trạng thái |
| Personas | UCL-GEN-001, UCL-GEN-003 |
| Goals | BG-GEN-004 |
| Needs | STR-CTR-001 |
| User Stories | US-AUD-001 |
| Business Rules | None |
| Priority | Should |
| Source | PS §6.2 CON-08 |
| Status | APPROVED |

#### F-ANA-001 Phân tích tự động

| Attribute | Value |
|---|---|
| Description | Tự động phân tích ngay sau khi upload và xác nhận ngữ cảnh. Hiển thị tiến trình theo bước (đọc file → tách điều khoản → đánh giá → chấm điểm → hoàn tất). Báo lỗi rõ lý do khi thất bại. Cho phép chạy lại phân tích cho một phiên bản. |
| Personas | UCL-GEN-002 |
| Goals | BG-GEN-001, BG-GEN-002 |
| Needs | STR-RISK-001, STR-RISK-002 |
| User Stories | US-ANA-001, US-ANA-002 |
| Business Rules | None |
| Priority | Must |
| Source | PS §6.3 ANA-01, ANA-02, ANA-10; §8 (Độ tin cậy) |
| Status | APPROVED |

#### F-ANA-002 Đọc và tách điều khoản

| Attribute | Value |
|---|---|
| Description | Trích xuất văn bản từ PDF có lớp chữ và DOCX, giữ lại cấu trúc (mục, điều, khoản) và vị trí trên trang. Tách hợp đồng thành từng điều khoản và phân loại vào 21 loại điều khoản (PS §7.2) cộng nhóm "Khác". Hỗ trợ hợp đồng tiếng Việt, tiếng Anh và song ngữ. [CHANGE] Changed by CR-GEN-001 (2026-11-17): bỏ OCR cho bản scan (DEC-PRD-023). |
| Personas | UCL-GEN-002, UCL-GEN-003 |
| Goals | BG-GEN-001 |
| Needs | STR-RISK-001, STR-RISK-003 |
| User Stories | US-ANA-004; US-ANA-003 đã DEPRECATED (DEC-PRD-023) |
| Business Rules | Danh mục 21 loại điều khoản, định nghĩa tại SRS §7 |
| Priority | Must |
| Source | PS §5, §6.3 ANA-03, ANA-04, §7.2 |
| Status | REVIEW |

#### F-RISK-001 Đánh giá rủi ro 7 nhóm

| Attribute | Value |
|---|---|
| Description | Đánh giá từng điều khoản trên 7 nhóm rủi ro (Legal, Financial, Operational, Compliance, Cybersecurity, Data & Privacy, Third-Party). Sinh finding theo cấu trúc chuẩn (PS §7.5), mỗi finding có đúng một nhóm chính. Phát hiện điều khoản bị thiếu. Gắn căn cứ tham khảo khi tìm được trong cơ sở tri thức. Xác định nhóm áp dụng hoặc N/A theo ngữ cảnh, bật lại nhóm N/A khi nội dung mâu thuẫn với ngữ cảnh. Diễn đạt kết luận ở dạng "rủi ro tiềm ẩn, cần xem xét". |
| Personas | UCL-GEN-002, UCL-GEN-003 |
| Goals | BG-GEN-001 |
| Needs | STR-RISK-001, STR-RISK-003, STR-RISK-004 |
| User Stories | US-RISK-001, US-RISK-002, US-RISK-003, US-RISK-004, US-RISK-005 |
| Business Rules | 7 nhóm, 3 quy tắc (nhóm chính, N/A, ranh giới), thang severity, góc nhìn bên mình, cách diễn đạt kết luận; định nghĩa tại SRS §7 |
| Priority | Must |
| Source | PS §4, §6.3 ANA-05, ANA-06, ANA-09, ANA-12, §7.1, §7.4, §7.5, §7.7 |
| Status | APPROVED |

#### F-RISK-002 Checklist Cyber/Privacy/Third-Party

| Attribute | Value |
|---|---|
| Description | Chạy 20 check (CY-01..12, PR-01..05, TP-01..03 trong PS §7.3). Mỗi check trả về PASS, PARTIAL, MISSING hoặc N/A. Check PARTIAL hoặc MISSING sinh finding. Hiển thị bảng check kèm liên kết tới điều khoản liên quan. |
| Personas | UCL-GEN-003 |
| Goals | BG-GEN-001 |
| Needs | STR-RISK-005 |
| User Stories | US-RISK-006 |
| Business Rules | Danh mục 20 check, định nghĩa tại SRS §7 |
| Priority | Must |
| Source | PS §6.3 ANA-07, §6.4 VIEW-04, §7.3 |
| Status | APPROVED |

#### F-RISK-003 Chấm điểm rủi ro

| Attribute | Value |
|---|---|
| Description | Chấm điểm 0–100 (cao hơn là rủi ro hơn) cho từng nhóm và cho cả hợp đồng. Chỉ nhóm chính được tính điểm. Nhóm N/A không có điểm. Không dùng trung bình cộng. Có hệ số ngữ cảnh. Finding Critical đặt điểm sàn. Phân mức Low, Medium, High, Critical theo ngưỡng. |
| Personas | UCL-GEN-001 |
| Goals | BG-GEN-001, BG-GEN-002 |
| Needs | STR-RISK-002 |
| User Stories | US-RISK-007 |
| Business Rules | Nguyên tắc chấm điểm và ngưỡng mức rủi ro, định nghĩa tại SRS §7 |
| Priority | Must |
| Source | PS §6.3 ANA-08, §7.6 |
| Status | APPROVED |

#### F-SEC-001 Bảo vệ đầu vào

| Attribute | Value |
|---|---|
| Description | Kiểm tra loại file thật (magic bytes) và kích thước, quét mã độc, làm sạch tài liệu (bỏ macro, embedded object, script) trước khi trích xuất. Phát hiện nội dung có dấu hiệu prompt injection và cảnh báo người dùng. Nội dung hợp đồng luôn được xử lý như dữ liệu, không được đưa ngược vào phần chỉ dẫn (prompt) của AI, và có thêm cơ chế chống prompt injection (DEC-PRD-016). |
| Personas | UCL-GEN-002 |
| Goals | BG-GEN-001 |
| Needs | STR-RISK-001, STR-SEC-001 |
| User Stories | US-SEC-001, US-SEC-002 |
| Business Rules | None |
| Priority | Must |
| Source | PS §4 (nguyên tắc 5), §6.3 ANA-11, §9.1 |
| Status | APPROVED |

#### F-VIEW-001 Dashboard rủi ro

| Attribute | Value |
|---|---|
| Description | Tổng quan: điểm tổng 0–100 kèm mức rủi ro, điểm từng nhóm (nhóm không áp dụng hiển thị N/A kèm lý do), số finding theo severity, khuyến nghị tổng. Danh sách finding lọc theo nhóm, severity, trạng thái xử lý. Chi tiết finding: Why, Impact, Recommendation (hướng xử lý bằng lời), căn cứ tham khảo. |
| Personas | UCL-GEN-001, UCL-GEN-002 |
| Goals | BG-GEN-002, BG-GEN-003 |
| Needs | STR-RISK-002, STR-RISK-004 |
| User Stories | US-VIEW-001, US-VIEW-002 |
| Business Rules | None |
| Priority | Must |
| Source | PS §6.4 VIEW-01, VIEW-02, VIEW-05 |
| Status | APPROVED |

#### F-VIEW-002 Document viewer

| Attribute | Value |
|---|---|
| Description | Hiển thị hợp đồng gốc, tô màu điều khoản theo severity. Bấm vào điều khoản thì mở finding tương ứng, và ngược lại. |
| Personas | UCL-GEN-003, UCL-GEN-002 |
| Goals | BG-GEN-002, BG-GEN-003 |
| Needs | STR-RISK-003 |
| User Stories | US-VIEW-003 |
| Business Rules | None |
| Priority | Must |
| Source | PS §6.4 VIEW-03 |
| Status | APPROVED |

#### F-VIEW-003 Xử lý finding

| Attribute | Value |
|---|---|
| Description | Đánh dấu từng finding là `Chấp nhận rủi ro`, `Cần đàm phán`, `Không đúng` (false positive) hoặc `Đã xử lý`, kèm ghi chú |
| Personas | UCL-GEN-003, UCL-GEN-002 |
| Goals | BG-GEN-002 |
| Needs | STR-RISK-004 |
| User Stories | US-VIEW-004 |
| Business Rules | None |
| Priority | Should |
| Source | PS §6.4 VIEW-06 |
| Status | APPROVED |

#### F-CHAT-001 Chatbot theo hợp đồng

| Attribute | Value |
|---|---|
| Description | Chatbot nhắn tin bằng văn bản (không có giọng nói), nằm cạnh document viewer. Hỏi đáp Việt/Anh về hợp đồng và phiên bản đang mở. Câu trả lời dựa trên nội dung hợp đồng, kết quả phân tích và cơ sở tri thức, kèm trích dẫn điều khoản có thể bấm để nhảy tới vị trí. Nói rõ khi hợp đồng không đề cập. Phạm vi truy cập bị giới hạn bởi quyền của người hỏi. Không viết lại hợp đồng. |
| Personas | UCL-GEN-001, UCL-GEN-002 |
| Goals | BG-GEN-002, BG-GEN-003 |
| Needs | STR-CHAT-001 |
| User Stories | US-CHAT-001, US-CHAT-002, US-CHAT-003 |
| Business Rules | None |
| Priority | Must |
| Source | PS §1, §6.5 CHAT-01, CHAT-02, CHAT-03 |
| Status | APPROVED |

#### F-CHAT-002 Gợi ý câu hỏi

| Attribute | Value |
|---|---|
| Description | Gợi ý câu hỏi nhanh, ví dụ "Rủi ro lớn nhất là gì?", "Điều khoản chấm dứt có bất lợi cho mình không?" |
| Personas | UCL-GEN-001, UCL-GEN-002 |
| Goals | BG-GEN-002 |
| Needs | STR-CHAT-001 |
| User Stories | US-CHAT-004 |
| Business Rules | None |
| Priority | Should |
| Source | PS §6.5 CHAT-04 |
| Status | APPROVED |

#### F-CHAT-003 Lịch sử chat

| Attribute | Value |
|---|---|
| Description | Lưu lịch sử hội thoại theo hợp đồng. Những người được chia sẻ hợp đồng xem được lịch sử chat. |
| Personas | UCL-GEN-001, UCL-GEN-002, UCL-GEN-003 |
| Goals | BG-GEN-002 |
| Needs | STR-CHAT-001 |
| User Stories | US-CHAT-005 |
| Business Rules | None |
| Priority | Should |
| Source | PS §6.5 CHAT-06 |
| Status | APPROVED |

#### F-EXP-001 Xuất báo cáo PDF và DOCX

| Attribute | Value |
|---|---|
| Description | Xuất báo cáo rủi ro dạng PDF gồm thông tin hợp đồng, điểm tổng, điểm theo nhóm, danh sách finding đầy đủ, checklist cyber và dòng miễn trừ trách nhiệm. Xuất cùng báo cáo đó dạng DOCX để người dùng chỉnh sửa tiếp. |
| Personas | UCL-GEN-002, UCL-GEN-001 |
| Goals | BG-GEN-003 |
| Needs | STR-EXP-001 |
| User Stories | US-EXP-001, US-EXP-002 |
| Business Rules | Mọi báo cáo có dòng miễn trừ trách nhiệm, định nghĩa tại SRS §7 |
| Priority | Must |
| Source | PS §4 (nguyên tắc 1), §6.6 EXP-01, EXP-02 |
| Status | APPROVED |

#### F-EXP-002 Tùy chọn báo cáo

| Attribute | Value |
|---|---|
| Description | Chọn ngôn ngữ báo cáo (Việt/Anh) và phạm vi (tất cả finding hay chỉ High/Critical) |
| Personas | UCL-GEN-002 |
| Goals | BG-GEN-003 |
| Needs | STR-EXP-001 |
| User Stories | US-EXP-003 |
| Business Rules | None |
| Priority | Should |
| Source | PS §6.6 EXP-03 |
| Status | APPROVED |

#### F-CMP-001 So sánh phiên bản

| Attribute | Value |
|---|---|
| Description | So sánh hai phiên bản của một hợp đồng, chỉ ra điểm hợp đồng bị yếu đi (ví dụ thông báo sự cố đổi từ "24 giờ" thành "promptly") và mức thay đổi điểm rủi ro |
| Personas | UCL-GEN-002 |
| Goals | BG-GEN-002 |
| Needs | STR-CTR-001 |
| User Stories | US-CMP-001 |
| Business Rules | None |
| Priority | Could |
| Source | PS §6.7 EXT-01; DEC-GEN-009 |
| Status | APPROVED |

#### F-POL-001 Company Policy

| Attribute | Value |
|---|---|
| Description | Tổ chức upload chính sách nội bộ (ví dụ "nhà cung cấp quan trọng phải báo sự cố ≤ 24h, lưu log ≥ 180 ngày"). Hệ thống chấm hợp đồng theo chính sách đó: Đạt, Một phần hoặc Không đạt. |
| Personas | UCL-GEN-004, UCL-GEN-003 |
| Goals | BG-GEN-001 |
| Needs | STR-RISK-005 |
| User Stories | US-POL-001 |
| Business Rules | None |
| Priority | Could |
| Source | PS §6.7 EXT-02; DEC-GEN-009 |
| Status | APPROVED |

#### Các feature Won't

Các feature này nằm ngoài lần phát hành này. Chúng được ghi lại để truy vết và làm định hướng sau MVP. Không có user story.

| Feature ID | Source | Lý do |
|---|---|---|
| F-RISK-004 | PS §6.7 EXT-03 | DEC-GEN-009 |
| F-ANA-003 | PS §6.7 EXT-04 | DEC-GEN-009 |
| F-EXP-003 | PS §6.6 EXP-04, §6.7 EXT-05 | DEC-GEN-009; phạm vi chỉ giải thích (DEC-PRD-011) |
| F-CHAT-004 | PS §6.5 CHAT-07, §6.7 EXT-06 | DEC-GEN-009 |
| F-CHAT-005 | PS §6.5 CHAT-05, §6.7 EXT-07 | DEC-GEN-009; phạm vi chỉ giải thích (DEC-PRD-011) |
| F-SHARE-002 | PS §6.1 ACC-10 | DEC-GEN-009 |
| F-AUTH-004 | PS §6.1 ACC-11 | DEC-GEN-009 |
| F-CTR-007 | PS §6.2 CON-09 | DEC-GEN-009 |
| F-CTR-008 | PS §6.2 CON-10 | DEC-GEN-009 |
| F-CTR-009 | Approver 2026-10-27 | DEC-PRD-021 |
| F-AUTH-003 | PS §6.1 ACC-09 | DEC-PRD-022. Bản sau MVP: ưu tiên Should, MFA qua email mỗi lần đăng nhập |

## 8. User Stories

**Last Updated**: 2026-10-27
**Status**: APPROVED
**Author**: Agent

Mẫu story và từ khóa tiêu chí nghiệm thu theo DEC-GEN-013. Story chỉ mô tả nhu cầu ở mức sản phẩm. Chi tiết hành vi được làm rõ trong SRS.

### 8.1 Tài khoản

#### US-AUTH-001 Đăng ký và đăng nhập bằng email

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn đăng ký và đăng nhập bằng email và mật khẩu, để dùng hệ thống mà không cần tài khoản bên ngoài.
- **Persona**: UCL-GEN-002
- **Feature**: F-AUTH-001
- **Need**: STR-SEC-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.1 ACC-01
- **Acceptance Criteria**:
  1. Given người dùng chưa có tài khoản, when đăng ký bằng email và mật khẩu, then hệ thống gửi email xác thực tới địa chỉ đã đăng ký.
  2. Given người dùng đã có tài khoản, when đăng nhập đúng email và mật khẩu, then người dùng vào được hệ thống.
- **Notes**: Áp dụng cho mọi persona. Chính sách mật khẩu và hành vi khi chưa xác thực email được làm rõ ở SRS.

#### US-AUTH-002 Quên mật khẩu

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn đặt lại mật khẩu khi quên, để lấy lại quyền truy cập tài khoản.
- **Persona**: UCL-GEN-002
- **Feature**: F-AUTH-001
- **Need**: STR-SEC-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.1 ACC-01
- **Acceptance Criteria**:
  1. Given người dùng quên mật khẩu, when yêu cầu đặt lại bằng email đã đăng ký, then người dùng nhận được hướng dẫn đặt lại mật khẩu qua email đó.
- **Notes**: None

#### US-AUTH-003 Đăng nhập bằng Google

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn đăng nhập bằng tài khoản Google, để không phải nhớ thêm mật khẩu.
- **Persona**: UCL-GEN-002
- **Feature**: F-AUTH-002
- **Need**: STR-SEC-001
- **Priority**: Should
- **Status**: APPROVED
- **Source**: PS §6.1 ACC-02
- **Acceptance Criteria**:
  1. Given người dùng có tài khoản Google, when chọn đăng nhập bằng Google và đồng ý trên trang của Google, then người dùng vào được hệ thống.
- **Notes**: Cách liên kết tài khoản Google với tài khoản email đã có được làm rõ ở SRS.

#### US-AUTH-004 Xác thực đa yếu tố

- **Story**: Là Owner tổ chức, tôi muốn bật xác thực đa yếu tố cho tài khoản, để tài khoản khó bị chiếm hơn.
- **Persona**: UCL-GEN-004
- **Feature**: F-AUTH-003
- **Need**: STR-SEC-001
- **Priority**: Should
- **Status**: DEPRECATED
- **Source**: PS §6.1 ACC-09
- **Acceptance Criteria**:
  1. Given tài khoản đã bật MFA, when người dùng đăng nhập bằng mật khẩu đúng, then hệ thống yêu cầu thêm yếu tố xác thực thứ hai trước khi cho vào.
- **Notes**: [DEPRECATED] Bỏ theo DEC-PRD-022: MFA không thuộc MVP. Giữ lại để lưu vết. Trước đó: yếu tố thứ hai là mã OTP gửi về email (SRS FR-AUTH-013).

### 8.2 Tổ chức và chia sẻ

#### US-ORG-001 Tạo tổ chức

- **Story**: Là Owner tổ chức, tôi muốn tạo tổ chức cho doanh nghiệp mình, để hợp đồng của doanh nghiệp được quản lý tách biệt.
- **Persona**: UCL-GEN-004
- **Feature**: F-ORG-001
- **Need**: STR-SEC-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.1 ACC-03
- **Acceptance Criteria**:
  1. Given người dùng đã đăng nhập, kể cả khi đã thuộc tổ chức khác, when tạo tổ chức, then người dùng trở thành Owner của tổ chức đó.
- **Notes**: Một người dùng thuộc được nhiều tổ chức (DEC-PRD-020).

#### US-ORG-002 Mời thành viên

- **Story**: Là Owner tổ chức, tôi muốn mời đồng nghiệp qua email, để họ cùng làm việc với hợp đồng của tổ chức.
- **Persona**: UCL-GEN-004
- **Feature**: F-ORG-001
- **Need**: STR-SHARE-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.1 ACC-04
- **Acceptance Criteria**:
  1. Given là Owner tổ chức, when mời một địa chỉ email, then người được mời nhận được link tham gia tổ chức.
  2. Given người được mời mở link tham gia, when hoàn tất tham gia, then người đó trở thành Member của tổ chức.
- **Notes**: Thời hạn hiệu lực của link mời được làm rõ ở SRS.

#### US-ORG-003 Quản lý quyền Owner và thành viên

- **Story**: Là Owner tổ chức, tôi muốn cấp hoặc thu hồi quyền Owner và xóa thành viên, để quyền quản trị luôn đúng người.
- **Persona**: UCL-GEN-004
- **Feature**: F-ORG-001
- **Need**: STR-SHARE-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.1 ACC-05 và bảng vai trò
- **Acceptance Criteria**:
  1. Given là Owner tổ chức, when cấp quyền Owner cho một Member, then Member đó có quyền Owner tổ chức.
  2. Given là Owner tổ chức, when xóa một thành viên, then người đó không còn truy cập được dữ liệu của tổ chức.
  3. Given tổ chức chỉ còn một Owner, when có thao tác thu hồi quyền Owner hoặc xóa Owner đó, then hệ thống từ chối thao tác.
- **Notes**: Xử lý hợp đồng do thành viên bị xóa sở hữu được làm rõ ở SRS.

#### US-ORG-004 Dùng không gian cá nhân

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn dùng ngay không gian cá nhân sau khi đăng ký, để phân tích hợp đồng mà không cần tạo hay tham gia tổ chức.
- **Persona**: UCL-GEN-002
- **Feature**: F-ORG-002
- **Need**: STR-CTR-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: Approver 2026-10-27 (DEC-PRD-020)
- **Acceptance Criteria**:
  1. Given người dùng vừa đăng ký và chưa thuộc tổ chức nào, when đăng nhập, then người dùng upload và phân tích được hợp đồng trong không gian cá nhân.
  2. Given người dùng có hợp đồng cá nhân, when một người khác chưa được chia sẻ xem thư viện của mình, then người đó không thấy hợp đồng này.
- **Notes**: None

#### US-ORG-005 Chuyển giữa các không gian

- **Story**: Là Owner tổ chức, tôi muốn chuyển giữa không gian cá nhân và các tổ chức mình tham gia, để làm việc với đúng nhóm hợp đồng.
- **Persona**: UCL-GEN-004
- **Feature**: F-ORG-002
- **Need**: STR-CTR-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: Approver 2026-10-27 (DEC-PRD-020)
- **Acceptance Criteria**:
  1. Given người dùng thuộc hai tổ chức, when mở danh sách không gian, then người dùng thấy không gian cá nhân và cả hai tổ chức kèm vai trò ở mỗi tổ chức.
  2. Given đang ở không gian của một tổ chức, when chọn không gian cá nhân, then thư viện chỉ hiển thị hợp đồng cá nhân của mình và hợp đồng cá nhân người khác chia sẻ cho mình.
- **Notes**: None

#### US-SHARE-001 Chia sẻ cho một email trong tổ chức

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn chia sẻ hợp đồng mình tạo cho một đồng nghiệp, để họ review cùng tôi.
- **Persona**: UCL-GEN-002
- **Feature**: F-SHARE-001
- **Need**: STR-SHARE-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.1 ACC-06, ACC-07
- **Acceptance Criteria**:
  1. Given hợp đồng vừa được tạo, when một Member tổ chức khác xem thư viện, then người đó không thấy hợp đồng này.
  2. Given hợp đồng vừa được tạo và chưa chia sẻ, when một Owner tổ chức xem thư viện, then Owner tổ chức thấy hợp đồng này.
  3. Given là Owner hợp đồng, when chia sẻ cho một email thuộc tổ chức, then người đó thấy hợp đồng trong thư viện với vai trò Member hợp đồng.
- **Notes**: Owner tổ chức tự động xem được mọi hợp đồng của tổ chức (DEC-PRD-013) và có mọi quyền của Owner hợp đồng trên các hợp đồng đó (DEC-PRD-017).

#### US-SHARE-002 Chia sẻ cho toàn tổ chức

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn chia sẻ hợp đồng cho cả tổ chức, để mọi đồng nghiệp đều xem được mà không phải thêm từng người.
- **Persona**: UCL-GEN-002
- **Feature**: F-SHARE-001
- **Need**: STR-SHARE-001
- **Priority**: Must
- **Status**: DEPRECATED
- **Source**: PS §6.1 ACC-06
- **Acceptance Criteria**:
  1. Given là Owner hợp đồng, when chia sẻ hợp đồng cho toàn tổ chức, then mọi thành viên của tổ chức thấy hợp đồng với vai trò Member hợp đồng.
- **Notes**: [DEPRECATED] Bỏ tính năng chia sẻ cho toàn tổ chức (DEC-PRD-018, trả lời OQ-SHARE-003, 2026-10-26). Người khác chỉ truy cập được hợp đồng khi được chia sẻ theo email (US-SHARE-001). Giữ lại để lưu vết.

#### US-SHARE-003 Thu hồi chia sẻ

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn thu hồi quyền đã chia sẻ, để người không còn liên quan không xem được hợp đồng nữa.
- **Persona**: UCL-GEN-002
- **Feature**: F-SHARE-001
- **Need**: STR-SHARE-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.1 ACC-08
- **Acceptance Criteria**:
  1. Given hợp đồng đã được chia sẻ cho một người, when Owner hợp đồng thu hồi quyền của người đó, then người đó không mở được hợp đồng ở bất kỳ lần truy cập nào sau thời điểm thu hồi.
- **Notes**: None

#### US-SHARE-004 Chuyển quyền Owner hợp đồng

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn chuyển quyền Owner hợp đồng cho đồng nghiệp, để người tiếp nhận công việc quản lý được hợp đồng.
- **Persona**: UCL-GEN-002
- **Feature**: F-SHARE-001
- **Need**: STR-SHARE-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.1 bảng vai trò
- **Acceptance Criteria**:
  1. Given là Owner hợp đồng, when chuyển quyền Owner cho một thành viên tổ chức, then người nhận có quyền Owner hợp đồng.
- **Notes**: Vai trò của Owner cũ sau khi chuyển được làm rõ ở SRS.

#### US-SHARE-005 Chia sẻ hợp đồng cá nhân

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn chia sẻ hợp đồng cá nhân cho bất kỳ ai có tài khoản ContractGuard, để nhờ người review bên ngoài mà không cần tạo tổ chức.
- **Persona**: UCL-GEN-002
- **Feature**: F-SHARE-001
- **Need**: STR-SHARE-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: Approver 2026-10-27 (DEC-PRD-021)
- **Acceptance Criteria**:
  1. Given có hợp đồng cá nhân và người nhận đã có tài khoản, when chia sẻ theo email của người nhận, then người nhận thấy hợp đồng với vai trò Member hợp đồng.
  2. Given email chưa có tài khoản, when chia sẻ hợp đồng cá nhân cho email đó, then hệ thống từ chối và nêu lý do.
- **Notes**: Hợp đồng cá nhân không chuyển quyền Owner trong MVP (F-CTR-009).

### 8.3 Quản lý hợp đồng

#### US-CTR-001 Upload hợp đồng

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn kéo thả file hợp đồng đối tác gửi vào hệ thống, để bắt đầu thẩm định mà không cần cài phần mềm.
- **Persona**: UCL-GEN-002
- **Feature**: F-CTR-001
- **Need**: STR-RISK-001
- **Priority**: Must
- **Status**: REVIEW
- **Source**: PS §2.2, §5, §6.2 CON-01
- **Acceptance Criteria**:
  1. Given người dùng ở thư viện hợp đồng, when kéo thả hoặc chọn một file PDF hoặc DOCX hợp lệ, then hệ thống nhận file và chuyển sang bước khai báo ngữ cảnh.
  2. Given file có định dạng khác các định dạng trên, lớn hơn 20 MB hoặc nhiều hơn 100 trang, when người dùng upload, then hệ thống từ chối ngay ở bước upload và hiển thị lý do (DEC-PRD-014).
- **Notes**: [CHANGE] Changed by CR-GEN-001 (2026-11-17): bỏ JPG, PNG (DEC-PRD-023).

#### US-CTR-002 Khai báo ngữ cảnh

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn khai báo bên mình là bên nào và bối cảnh của hợp đồng, để rủi ro được đánh giá đúng góc nhìn của doanh nghiệp mình.
- **Persona**: UCL-GEN-002
- **Feature**: F-CTR-001
- **Need**: STR-RISK-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §2.2, §4 (nguyên tắc 3), §6.2 CON-02
- **Acceptance Criteria**:
  1. Given đã upload file, when mở bước khai báo, then người dùng nhập được: bên mình (bên mua/khách hàng hoặc bên bán/nhà cung cấp), tên đối tác, loại hợp đồng, đối tác cung cấp gì, có xử lý dữ liệu cá nhân hoặc nhạy cảm không, mức độ quan trọng của dịch vụ.
  2. Given hệ thống trích xuất được một số thông tin ngữ cảnh từ file, when mở bước khai báo, then các thông tin đó được điền sẵn để người dùng xác nhận hoặc sửa.
- **Notes**: Trường nào bắt buộc được làm rõ ở SRS.

#### US-CTR-003 Metadata tự trích xuất

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn hệ thống tự lấy các thông tin chính của hợp đồng, để không phải tự đọc và nhập tay.
- **Persona**: UCL-GEN-002
- **Feature**: F-CTR-002
- **Need**: STR-RISK-002
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.2 CON-03
- **Acceptance Criteria**:
  1. Given phân tích đã hoàn tất, when người dùng mở hợp đồng, then hệ thống hiển thị các bên, loại hợp đồng, giá trị, đồng tiền, ngày hiệu lực, ngày hết hạn và luật áp dụng trích xuất được.
  2. Given một trường metadata bị trích sai, when người dùng sửa trường đó, then giá trị mới được lưu và hiển thị.
- **Notes**: Hiển thị trường không trích xuất được làm rõ ở SRS.

#### US-CTR-004 Tìm hợp đồng trong thư viện

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn tìm và lọc hợp đồng trong thư viện, để nhanh chóng mở đúng hợp đồng cần xử lý.
- **Persona**: UCL-GEN-002
- **Feature**: F-CTR-003
- **Need**: STR-CTR-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.2 CON-04
- **Acceptance Criteria**:
  1. Given thư viện có nhiều hợp đồng, when tìm theo tên hợp đồng hoặc tên đối tác, then chỉ các hợp đồng khớp được hiển thị.
  2. Given thư viện có nhiều hợp đồng, when lọc theo mức rủi ro, trạng thái hoặc loại hợp đồng, then chỉ các hợp đồng thỏa điều kiện được hiển thị.
  3. Given danh sách đang hiển thị, when chọn một tiêu chí sắp xếp, then danh sách được sắp xếp theo tiêu chí đó.
- **Notes**: Thư viện chỉ chứa hợp đồng người dùng có quyền xem. Các tiêu chí sắp xếp được làm rõ ở SRS.

#### US-CTR-005 Theo dõi trạng thái duyệt

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn chuyển trạng thái của hợp đồng theo tiến trình duyệt, để cả nhóm biết hợp đồng đang ở bước nào.
- **Persona**: UCL-GEN-002
- **Feature**: F-CTR-004
- **Need**: STR-CTR-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.2 CON-05
- **Acceptance Criteria**:
  1. Given là Owner hoặc Member của hợp đồng, when chuyển trạng thái theo luồng `Mới` → `Đang phân tích` → `Chờ review` → `Đang đàm phán` → `Đã duyệt` hoặc `Từ chối`, then trạng thái mới được lưu và hiển thị cho mọi người có quyền xem.
- **Notes**: Trạng thái nào do hệ thống tự chuyển và trạng thái nào do người dùng chuyển được làm rõ ở SRS §13.

#### US-CTR-006 Phiên bản mới sau đàm phán

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn upload bản hợp đồng mới sau đàm phán vào cùng hợp đồng, để giữ lịch sử và phân tích lại bản mới.
- **Persona**: UCL-GEN-002
- **Feature**: F-CTR-005
- **Need**: STR-CTR-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §2.2 bước 6, §6.2 CON-06
- **Acceptance Criteria**:
  1. Given hợp đồng đã có phiên bản V1, when upload một file mới cho hợp đồng này, then hệ thống tạo phiên bản V2 và phân tích riêng V2.
  2. Given hợp đồng có nhiều phiên bản, when mở lịch sử phiên bản, then người dùng xem được từng phiên bản và kết quả phân tích riêng của nó.
- **Notes**: None

#### US-CTR-007 Xóa vĩnh viễn hợp đồng

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn xóa vĩnh viễn hợp đồng không còn cần, để dữ liệu nhạy cảm không còn nằm trong hệ thống.
- **Persona**: UCL-GEN-002
- **Feature**: F-CTR-006
- **Need**: STR-SEC-001
- **Priority**: Must
- **Status**: REVIEW
- **Source**: PS §6.1 bảng vai trò, §6.2 CON-07; CR-CTR-001
- **Acceptance Criteria**:
  1. Given là Owner hợp đồng, when xóa hợp đồng, then hợp đồng không còn xuất hiện với bất kỳ ai và nằm trong thùng rác; sau 30 ngày, hoặc khi Owner xóa vĩnh viễn từ thùng rác, file gốc, dữ liệu trích xuất, embedding, finding và lịch sử chat của mọi phiên bản bị xóa vĩnh viễn.
  2. Given là Member hợp đồng, when thử xóa hợp đồng, then hệ thống từ chối.
- **Notes**: Thời hạn hoàn tất việc xóa dữ liệu được làm rõ ở SRS. [CHANGE] Changed by CR-CTR-001 (2026-11-22): xóa qua thùng rác 30 ngày, khôi phục được.

#### US-AUD-001 Xem nhật ký hoạt động

- **Story**: Là giám đốc, tôi muốn xem ai đã làm gì trên một hợp đồng, để biết lịch sử xử lý trước khi duyệt.
- **Persona**: UCL-GEN-001
- **Feature**: F-AUD-001
- **Need**: STR-CTR-001
- **Priority**: Should
- **Status**: APPROVED
- **Source**: PS §6.2 CON-08
- **Acceptance Criteria**:
  1. Given đã có người upload, xem, chia sẻ, xuất báo cáo hoặc đổi trạng thái hợp đồng, when mở nhật ký hoạt động của hợp đồng, then mỗi hành động hiển thị người thực hiện, loại hành động và thời điểm.
- **Notes**: Thời gian lưu nhật ký được làm rõ ở SRS.

### 8.4 Phân tích hợp đồng

#### US-ANA-001 Phân tích tự động và theo dõi tiến trình

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn hệ thống tự phân tích ngay sau khi tôi xác nhận ngữ cảnh và cho tôi thấy tiến trình, để biết còn phải chờ bao lâu.
- **Persona**: UCL-GEN-002
- **Feature**: F-ANA-001
- **Need**: STR-RISK-002
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.3 ANA-01, ANA-02; §8 (Độ tin cậy)
- **Acceptance Criteria**:
  1. Given người dùng vừa xác nhận ngữ cảnh, when xác nhận xong, then phân tích bắt đầu mà không cần thao tác thêm.
  2. Given phân tích đang chạy, when người dùng xem hợp đồng, then người dùng thấy bước hiện tại trong chuỗi: đọc file → tách điều khoản → đánh giá → chấm điểm → hoàn tất.
  3. Given phân tích thất bại sau khi hệ thống đã tự thử lại, when người dùng xem hợp đồng, then người dùng thấy thông báo lỗi kèm lý do và không thấy kết quả dở dang.
- **Notes**: Mục tiêu thời gian theo SC-GEN-003, SC-GEN-004.

#### US-ANA-002 Chạy lại phân tích

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn chạy lại phân tích sau khi sửa ngữ cảnh, để kết quả phản ánh thông tin đúng.
- **Persona**: UCL-GEN-002
- **Feature**: F-ANA-001
- **Need**: STR-RISK-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.3 ANA-10
- **Acceptance Criteria**:
  1. Given một phiên bản đã được phân tích, when người dùng sửa ngữ cảnh và chọn chạy lại phân tích, then hệ thống phân tích lại phiên bản đó với ngữ cảnh mới và hiển thị kết quả mới.
- **Notes**: Việc giữ hay thay thế kết quả cũ được làm rõ ở SRS.

#### US-ANA-003 Đọc bản scan

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn hệ thống đọc được cả bản scan hoặc ảnh chụp hợp đồng, để không phải gõ lại văn bản.
- **Persona**: UCL-GEN-002
- **Feature**: F-ANA-002
- **Need**: STR-RISK-001
- **Priority**: Must
- **Status**: DEPRECATED
- **Source**: PS §5, §6.3 ANA-03
- **Acceptance Criteria**:
  1. Given file là PDF scan hoặc ảnh JPG/PNG, when hệ thống phân tích, then văn bản được nhận dạng và các điều khoản giữ đúng cấu trúc mục, điều, khoản cùng vị trí trên trang.
- **Notes**: [DEPRECATED] Bỏ theo CR-GEN-001 (DEC-PRD-023, 2026-11-17): hệ thống không đọc bản scan và ảnh, chỉ nhận PDF có lớp chữ và DOCX. Giữ lại để lưu vết. Trước đó: mục tiêu thời gian cho bản scan theo STR-QUAL-002.

#### US-ANA-004 Tách và phân loại điều khoản

- **Story**: Là người review chuyên môn, tôi muốn hợp đồng được tách thành từng điều khoản có phân loại, để xem rủi ro theo từng loại điều khoản.
- **Persona**: UCL-GEN-003
- **Feature**: F-ANA-002
- **Need**: STR-RISK-003
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.3 ANA-04, §7.2
- **Acceptance Criteria**:
  1. Given phân tích đã hoàn tất, when xem danh sách điều khoản, then mỗi điều khoản thuộc đúng một trong 21 loại điều khoản hoặc loại "Khác".
- **Notes**: Mục tiêu chất lượng phân loại theo STR-QUAL-005.

### 8.5 Đánh giá rủi ro

#### US-RISK-001 Finding theo cấu trúc chuẩn

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn mỗi rủi ro được trình bày đầy đủ lý do, tác động và hướng xử lý, để mang đi đàm phán.
- **Persona**: UCL-GEN-002
- **Feature**: F-RISK-001
- **Need**: STR-RISK-004
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.3 ANA-05, §7.5
- **Acceptance Criteria**:
  1. Given phân tích đã hoàn tất, when mở một finding, then finding có tiêu đề, loại, nhóm chính, nhóm phụ, danh mục, điều khoản (trích nguyên văn và vị trí), severity, Why, Impact, Recommendation và trạng thái xử lý.
  2. Given một finding bất kỳ, when kiểm tra nhóm chính, then finding có đúng một nhóm chính.
- **Notes**: None

#### US-RISK-002 Phát hiện điều khoản bị thiếu

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn biết hợp đồng thiếu những điều khoản quan trọng nào, để yêu cầu đối tác bổ sung.
- **Persona**: UCL-GEN-002
- **Feature**: F-RISK-001
- **Need**: STR-RISK-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §4 (nguyên tắc 2), §6.3 ANA-06
- **Acceptance Criteria**:
  1. Given hợp đồng không có điều khoản về một nội dung cần có (ví dụ quyền kiểm toán), when phân tích hoàn tất, then có một finding loại "Thiếu điều khoản" ghi rõ hệ thống đã tìm những gì mà không thấy.
- **Notes**: None

#### US-RISK-003 Căn cứ tham khảo và cách diễn đạt

- **Story**: Là người review chuyên môn, tôi muốn finding có căn cứ tham khảo và được diễn đạt là rủi ro tiềm ẩn, để tự kiểm chứng mà không bị dẫn tới kết luận pháp lý sai.
- **Persona**: UCL-GEN-003
- **Feature**: F-RISK-001
- **Need**: STR-RISK-003
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §4 (nguyên tắc 1), §6.3 ANA-09, §7.7
- **Acceptance Criteria**:
  1. Given cơ sở tri thức có căn cứ liên quan tới một finding, when mở finding đó, then finding hiển thị căn cứ tham khảo.
  2. Given bất kỳ finding nào, when đọc nội dung, then finding không kết luận điều khoản là "trái pháp luật" mà diễn đạt là rủi ro tiềm ẩn cần xem xét.
- **Notes**: None

#### US-RISK-004 Nhóm không áp dụng

- **Story**: Là giám đốc, tôi muốn các nhóm rủi ro không liên quan được ghi là "Không áp dụng" thay vì điểm 0, để không hiểu nhầm là an toàn.
- **Persona**: UCL-GEN-001
- **Feature**: F-RISK-001
- **Need**: STR-RISK-002
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.3 ANA-12, §7.1 (quy tắc 2)
- **Acceptance Criteria**:
  1. Given ngữ cảnh khai báo đối tác không tiếp nhận dữ liệu và không cung cấp dịch vụ CNTT, when phân tích hoàn tất, then các nhóm không áp dụng hiển thị N/A kèm lý do.
  2. Given một nhóm đang N/A theo ngữ cảnh, when hợp đồng có điều khoản thuộc nhóm đó, then hệ thống bật lại nhóm đó và nhắc người dùng kiểm tra lại ngữ cảnh.
- **Notes**: Điều kiện áp dụng từng nhóm theo PS §7.1 quy tắc 2, định nghĩa ở SRS §7.

#### US-RISK-005 Đánh giá theo góc nhìn bên mình

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn rủi ro được đánh giá theo vị trí của doanh nghiệp mình trong hợp đồng, để không bị cảnh báo sai về điều khoản có lợi cho mình.
- **Persona**: UCL-GEN-002
- **Feature**: F-RISK-001
- **Need**: STR-RISK-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §4 (nguyên tắc 3)
- **Acceptance Criteria**:
  1. Given cùng một hợp đồng có điều khoản trả trước 80%, when phân tích lần lượt với khai báo bên mình là bên mua và là bên bán, then điều khoản đó chỉ được đánh giá là rủi ro trong lần khai báo bên mua.
- **Notes**: Ví dụ lấy từ PS §4 nguyên tắc 3.

#### US-RISK-006 Checklist Cyber/Privacy/Third-Party

- **Story**: Là người review chuyên môn, tôi muốn xem bảng 20 check về Cybersecurity, Privacy và Third-Party, để biết hợp đồng đáp ứng đến đâu.
- **Persona**: UCL-GEN-003
- **Feature**: F-RISK-002
- **Need**: STR-RISK-005
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.3 ANA-07, §6.4 VIEW-04, §7.3
- **Acceptance Criteria**:
  1. Given phân tích đã hoàn tất, when mở checklist, then mỗi check trong 20 check có một trong các trạng thái PASS, PARTIAL, MISSING hoặc N/A, kèm liên kết tới điều khoản liên quan nếu có.
  2. Given một check ở trạng thái PARTIAL hoặc MISSING, when xem danh sách finding, then có finding tương ứng với nhóm chính theo tiền tố mã check.
- **Notes**: None

#### US-RISK-007 Điểm rủi ro

- **Story**: Là giám đốc, tôi muốn thấy điểm rủi ro tổng và theo từng nhóm, để nắm mức rủi ro trong vài giây.
- **Persona**: UCL-GEN-001
- **Feature**: F-RISK-003
- **Need**: STR-RISK-002
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.3 ANA-08, §7.6
- **Acceptance Criteria**:
  1. Given phân tích đã hoàn tất, when xem tổng quan, then có điểm 0–100 cho cả hợp đồng và cho từng nhóm đang áp dụng, kèm mức Low, Medium, High hoặc Critical theo ngưỡng.
  2. Given hợp đồng có ít nhất một finding Critical, when xem điểm tổng, then điểm tổng không thấp hơn điểm sàn quy định cho finding Critical.
- **Notes**: Giá trị điểm sàn và công thức được chốt ở SRS §7 và hiệu chỉnh bằng eval (PS §7.6).

### 8.6 Bảo vệ đầu vào

#### US-SEC-001 Từ chối file nguy hiểm

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn hệ thống chặn file giả dạng hoặc chứa mã độc, để không gây hại cho tổ chức.
- **Persona**: UCL-GEN-002
- **Feature**: F-SEC-001
- **Need**: STR-SEC-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §9.1
- **Acceptance Criteria**:
  1. Given file có loại thật khác với phần mở rộng tên file, when upload, then hệ thống từ chối và hiển thị lý do.
  2. Given file chứa mã độc, when upload, then hệ thống từ chối và hiển thị lý do.
- **Notes**: None

#### US-SEC-002 Cảnh báo prompt injection

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn được cảnh báo khi hợp đồng chứa câu lệnh nhằm đánh lừa AI, để không tin nhầm vào kết quả bị thao túng.
- **Persona**: UCL-GEN-002
- **Feature**: F-SEC-001
- **Need**: STR-RISK-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §4 (nguyên tắc 5), §6.3 ANA-11
- **Acceptance Criteria**:
  1. Given hợp đồng chứa câu có dấu hiệu prompt injection (ví dụ "bỏ qua các điều khoản trách nhiệm"), when phân tích hoàn tất, then người dùng thấy cảnh báo chỉ ra vị trí nội dung đó.
  2. Given hợp đồng chứa câu như trên, when so sánh với cùng hợp đồng đã bỏ câu đó, then các finding về những điều khoản còn lại không bị mất.
- **Notes**: Bộ test prompt injection thuộc RC-GEN-003.

### 8.7 Xem kết quả

#### US-VIEW-001 Tổng quan cho người ký

- **Story**: Là giám đốc, tôi muốn xem một màn hình tổng quan về rủi ro, để quyết định nhanh có nên ký không.
- **Persona**: UCL-GEN-001
- **Feature**: F-VIEW-001
- **Need**: STR-RISK-002
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §3.2, §6.4 VIEW-01
- **Acceptance Criteria**:
  1. Given phân tích đã hoàn tất, when mở tổng quan, then thấy điểm tổng kèm mức rủi ro, điểm từng nhóm (nhóm N/A kèm lý do), số finding theo từng severity và một khuyến nghị tổng.
- **Notes**: Ví dụ khuyến nghị tổng: "Không nên duyệt khi chưa xử lý 8 finding High/Critical" (PS §3.2).

#### US-VIEW-002 Danh sách và chi tiết finding

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn lọc danh sách finding và xem chi tiết từng cái, để tập trung vào những điểm cần đàm phán.
- **Persona**: UCL-GEN-002
- **Feature**: F-VIEW-001
- **Need**: STR-RISK-004
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.4 VIEW-02, VIEW-05
- **Acceptance Criteria**:
  1. Given có nhiều finding, when lọc theo nhóm rủi ro, severity hoặc trạng thái xử lý, then chỉ các finding thỏa điều kiện được hiển thị.
  2. Given chọn một finding, when mở chi tiết, then thấy Why, Impact, Recommendation (hướng xử lý bằng lời) và căn cứ tham khảo nếu có.
- **Notes**: None

#### US-VIEW-003 Xem rủi ro ngay trên văn bản

- **Story**: Là người review chuyên môn, tôi muốn thấy điều khoản rủi ro được tô màu ngay trên văn bản gốc, để kiểm chứng trong ngữ cảnh đầy đủ.
- **Persona**: UCL-GEN-003
- **Feature**: F-VIEW-002
- **Need**: STR-RISK-003
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.4 VIEW-03; PD Design Principles
- **Acceptance Criteria**:
  1. Given phân tích đã hoàn tất, when mở document viewer, then các điều khoản có finding được tô màu theo severity, kèm ký hiệu và chữ chỉ severity.
  2. Given đang xem văn bản, when bấm vào một điều khoản được tô màu, then finding tương ứng được mở.
  3. Given đang xem một finding, when chọn đi tới điều khoản, then viewer cuộn tới đúng vị trí điều khoản trong văn bản.
- **Notes**: "Màu không bao giờ đứng một mình" (PD Design Principles 3, STR-QUAL-013).

#### US-VIEW-004 Đánh dấu xử lý finding

- **Story**: Là người review chuyên môn, tôi muốn đánh dấu từng finding là chấp nhận, cần đàm phán, không đúng hoặc đã xử lý, để cả nhóm biết điểm nào còn tồn đọng.
- **Persona**: UCL-GEN-003
- **Feature**: F-VIEW-003
- **Need**: STR-RISK-004
- **Priority**: Should
- **Status**: APPROVED
- **Source**: PS §2.2 bước 5, §6.4 VIEW-06
- **Acceptance Criteria**:
  1. Given là Owner hoặc Member của hợp đồng, when đặt một finding sang `Chấp nhận rủi ro`, `Cần đàm phán`, `Không đúng` hoặc `Đã xử lý` kèm ghi chú, then trạng thái và ghi chú được lưu và hiển thị cho mọi người có quyền xem.
- **Notes**: None

### 8.8 Chatbot

#### US-CHAT-001 Hỏi đáp có trích dẫn

- **Story**: Là giám đốc, tôi muốn hỏi bằng lời thường về hợp đồng và nhận câu trả lời có trích dẫn, để hiểu nhanh mà vẫn kiểm chứng được.
- **Persona**: UCL-GEN-001
- **Feature**: F-CHAT-001
- **Need**: STR-CHAT-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.5 CHAT-01, CHAT-02; §10 bước 6
- **Acceptance Criteria**:
  1. Given đang mở một hợp đồng đã phân tích, when hỏi bằng tiếng Việt hoặc tiếng Anh về nội dung hợp đồng, then chatbot trả lời kèm ít nhất một trích dẫn điều khoản.
  2. Given câu trả lời có trích dẫn, when bấm vào trích dẫn, then document viewer chuyển tới đúng vị trí điều khoản đó.
- **Notes**: Chatbot chỉ nhận văn bản, không có giọng nói (DEC-PRD-012).

#### US-CHAT-002 Không suy đoán khi hợp đồng không đề cập

- **Story**: Là giám đốc, tôi muốn chatbot nói rõ khi hợp đồng không đề cập điều tôi hỏi, để không bị dẫn dắt bởi câu trả lời bịa.
- **Persona**: UCL-GEN-001
- **Feature**: F-CHAT-001
- **Need**: STR-CHAT-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.5 CHAT-03
- **Acceptance Criteria**:
  1. Given hợp đồng không có điều khoản về nội dung được hỏi, when người dùng hỏi về nội dung đó, then chatbot trả lời rằng hợp đồng không đề cập, thay vì đưa ra câu trả lời suy đoán.
- **Notes**: Ví dụ ở PS §10 bước 6: thời hạn thông báo sự cố không được quy định.

#### US-CHAT-003 Chatbot trong phạm vi quyền

- **Story**: Là Owner tổ chức, tôi muốn chatbot chỉ dùng dữ liệu mà người hỏi được phép xem, để chat không làm lộ hợp đồng khác.
- **Persona**: UCL-GEN-004
- **Feature**: F-CHAT-001
- **Need**: STR-SEC-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §4 (nguyên tắc 6), §6.5
- **Acceptance Criteria**:
  1. Given người dùng đang chat trên một hợp đồng, when hỏi về nội dung của một hợp đồng khác mà họ không có quyền xem, then chatbot không trả lời bằng nội dung của hợp đồng đó.
  2. Given người dùng đang chat trên một phiên bản, when chatbot trả lời, then các trích dẫn chỉ đến từ hợp đồng và phiên bản đang mở hoặc từ cơ sở tri thức.
- **Notes**: None

#### US-CHAT-004 Câu hỏi gợi ý

- **Story**: Là giám đốc, tôi muốn có sẵn vài câu hỏi gợi ý, để bắt đầu hỏi mà không phải nghĩ cách đặt câu.
- **Persona**: UCL-GEN-001
- **Feature**: F-CHAT-002
- **Need**: STR-CHAT-001
- **Priority**: Should
- **Status**: APPROVED
- **Source**: PS §6.5 CHAT-04
- **Acceptance Criteria**:
  1. Given mở chatbot trên một hợp đồng đã phân tích, when chưa nhập câu hỏi, then chatbot hiển thị các câu hỏi gợi ý; bấm vào một câu thì câu đó được gửi đi.
- **Notes**: None

#### US-CHAT-005 Lịch sử chat chung

- **Story**: Là người review chuyên môn, tôi muốn xem các câu hỏi đồng nghiệp đã hỏi trên hợp đồng, để không phải hỏi lại.
- **Persona**: UCL-GEN-003
- **Feature**: F-CHAT-003
- **Need**: STR-CHAT-001
- **Priority**: Should
- **Status**: APPROVED
- **Source**: PS §6.5 CHAT-06
- **Acceptance Criteria**:
  1. Given đồng nghiệp đã chat trên một hợp đồng được chia sẻ cho tôi, when tôi mở chatbot của hợp đồng đó, then tôi thấy lịch sử hội thoại đã lưu.
- **Notes**: Lịch sử chat bị xóa cùng hợp đồng (US-CTR-007).

### 8.9 Xuất báo cáo

#### US-EXP-001 Xuất báo cáo PDF

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn xuất báo cáo rủi ro dạng PDF, để gửi cho giám đốc hoặc đối tác.
- **Persona**: UCL-GEN-002
- **Feature**: F-EXP-001
- **Need**: STR-EXP-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.6 EXP-01
- **Acceptance Criteria**:
  1. Given phân tích đã hoàn tất, when chọn xuất PDF, then người dùng tải được file PDF gồm thông tin hợp đồng, điểm tổng, điểm theo nhóm, danh sách finding đầy đủ, checklist cyber và dòng miễn trừ trách nhiệm.
  2. Given việc tạo báo cáo thất bại, when người dùng chờ file, then hệ thống hiển thị thông báo tạo báo cáo thất bại.
- **Notes**: Hành vi khi thất bại theo OQ-EXP-001.

#### US-EXP-002 Xuất báo cáo DOCX

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn xuất cùng báo cáo đó dạng DOCX, để chỉnh sửa thêm trước khi gửi đối tác.
- **Persona**: UCL-GEN-002
- **Feature**: F-EXP-001
- **Need**: STR-EXP-001
- **Priority**: Must
- **Status**: APPROVED
- **Source**: PS §6.6 EXP-02
- **Acceptance Criteria**:
  1. Given phân tích đã hoàn tất, when chọn xuất DOCX, then người dùng tải được file DOCX có cùng nội dung với báo cáo PDF và mở chỉnh sửa được bằng trình soạn thảo văn bản.
- **Notes**: DOCX là báo cáo, không phải bản hợp đồng redline (F-EXP-003 là Won't).

#### US-EXP-003 Tùy chọn ngôn ngữ và phạm vi báo cáo

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn chọn ngôn ngữ và phạm vi finding của báo cáo, để báo cáo phù hợp người nhận.
- **Persona**: UCL-GEN-002
- **Feature**: F-EXP-002
- **Need**: STR-EXP-001
- **Priority**: Should
- **Status**: APPROVED
- **Source**: PS §6.6 EXP-03
- **Acceptance Criteria**:
  1. Given chọn ngôn ngữ tiếng Anh và phạm vi chỉ High/Critical, when xuất báo cáo, then báo cáo viết bằng tiếng Anh và chỉ chứa các finding mức High và Critical.
- **Notes**: Trích dẫn điều khoản giữ nguyên văn gốc (PS §5).

### 8.10 Feature Could

#### US-CMP-001 So sánh hai phiên bản

- **Story**: Là nhân viên nhận hợp đồng, tôi muốn so sánh phiên bản mới với phiên bản trước, để biết đối tác đã làm yếu đi điều khoản nào.
- **Persona**: UCL-GEN-002
- **Feature**: F-CMP-001
- **Need**: STR-CTR-001
- **Priority**: Could
- **Status**: APPROVED
- **Source**: PS §6.7 EXT-01
- **Acceptance Criteria**:
  1. Given hợp đồng có hai phiên bản đã phân tích, when chọn so sánh, then hệ thống chỉ ra các điều khoản bị yếu đi và mức thay đổi điểm rủi ro giữa hai phiên bản.
- **Notes**: None

#### US-POL-001 Chấm hợp đồng theo chính sách nội bộ

- **Story**: Là Owner tổ chức, tôi muốn tải lên chính sách hợp đồng nội bộ, để mọi hợp đồng được chấm theo tiêu chuẩn của công ty.
- **Persona**: UCL-GEN-004
- **Feature**: F-POL-001
- **Need**: STR-RISK-005
- **Priority**: Could
- **Status**: APPROVED
- **Source**: PS §6.7 EXT-02
- **Acceptance Criteria**:
  1. Given tổ chức đã tải lên chính sách nội bộ, when một hợp đồng được phân tích, then mỗi yêu cầu của chính sách có kết quả Đạt, Một phần hoặc Không đạt.
- **Notes**: None

## 9. Product Quality Requirements

**Last Updated**: 2026-10-27
**Status**: APPROVED
**Author**: Agent

Các yêu cầu dưới đây được SRS §11 tinh chỉnh thành `NFR` đo được.

| ID | ISO/IEC 25010 Characteristic | Expectation | Target | Persona Or Stakeholder | Source | Status |
|---|---|---|---|---|---|---|
| STR-QUAL-001 | Performance Efficiency | Thời gian phân tích hợp đồng có lớp text 20 trang, đo khi worker đã chạy sẵn | p50 ≤ 2 phút; p95 ≤ 5 phút | UCL-GEN-001, UCL-GEN-002 | DI 2026-10-24, Q-PRD-007; DEC-GEN-022 (thay DEC-GEN-008). [CONFLICT] PS §2.2 và §8 (P3) ghi "≤ 45 giây (p50)"; Approver (P1) chốt mục tiêu mới; giải quyết theo P1, Source Priority Rule 3.1. | APPROVED |
| STR-QUAL-002 | Performance Efficiency | Thời gian phân tích hợp đồng scan ≤ 50 trang | ≤ 5 phút (p50) | UCL-GEN-002 | PS §8; DEC-GEN-022. [CONFLICT] PS §8 (P3) ghi "≤ 3 phút"; Approver (P1) chốt ≤ 5 phút; giải quyết theo P1. [DEPRECATED] Bỏ theo DEC-PRD-023 (CR-GEN-001) | DEPRECATED |
| STR-QUAL-003 | Performance Efficiency | Chatbot bắt đầu hiển thị câu trả lời | Chữ đầu tiên xuất hiện ≤ 15 giây (p50) | UCL-GEN-001, UCL-GEN-002 | PS §8; DEC-GEN-022. [CONFLICT] PS §8 (P3) ghi "≤ 3 giây"; Approver (P1) chốt ≤ 15 giây; giải quyết theo P1 | APPROVED |
| STR-QUAL-004 | Functional Suitability | Độ chính xác phát hiện rủi ro trên bộ eval 20 hợp đồng có nhãn; ưu tiên không bỏ sót hơn báo nhầm | Recall High/Critical ≥ 85% (SC-GEN-001); precision ≥ 70% (SC-GEN-002) | UCL-GEN-002, UCL-GEN-003 | PS §8; DI 2026-10-24, Q-PRD-001 | APPROVED |
| STR-QUAL-005 | Functional Suitability | Độ chính xác phân loại điều khoản trên bộ eval | F1 ≥ 0,85 | UCL-GEN-003 | PS §8 | APPROVED |
| STR-QUAL-006 | Security | Dữ liệu được mã hóa khi lưu trữ và khi truyền | Mọi dữ liệu hợp đồng và dữ liệu người dùng | UCL-GEN-004 | PS §8 | APPROVED |
| STR-QUAL-007 | Security | Cách ly dữ liệu giữa các không gian làm việc (tổ chức và không gian cá nhân) ở mọi tầng: database, lưu trữ file, vector store, chat | 0 trường hợp truy cập chéo không gian trong bộ kiểm thử cách ly | UCL-GEN-004 | PS §4 (nguyên tắc 6), §8; RC-GEN-003 | APPROVED |
| STR-QUAL-008 | Security | Phân quyền được kiểm tra ở phía server; quyền hạ tầng tối thiểu; có audit log | Mọi thao tác trên hợp đồng | UCL-GEN-004 | PS §8 | APPROVED |
| STR-QUAL-009 | Security | An toàn AI: chống prompt injection, không lộ dữ liệu giữa tổ chức qua RAG, không dùng dữ liệu khách hàng để huấn luyện model | 0 lỗi Critical trong bộ test prompt injection | UCL-GEN-002, UCL-GEN-004 | PS §8, §9.2; RC-GEN-003 | APPROVED |
| STR-QUAL-010 | Security | Riêng tư: xóa hợp đồng là xóa toàn bộ dữ liệu liên quan; dữ liệu chỉ được xử lý trong phạm vi cung cấp dịch vụ | Mọi dữ liệu liên quan tới hợp đồng bị xóa | UCL-GEN-004 | PS §8 | APPROVED |
| STR-QUAL-011 | Maintainability | Truy vết: mỗi kết quả lưu phiên bản rule, prompt, model và schema; mỗi lần phân tích có một mã tương quan (correlation ID) xuyên suốt | Mọi finding và mọi lần phân tích | Nhóm phát triển | PS §4 (nguyên tắc 4), §8 | APPROVED |
| STR-QUAL-012 | Reliability | Phân tích lỗi thì tự thử lại; lỗi hẳn thì báo lý do và không lưu kết quả dở dang. MVP không yêu cầu high availability, chấp nhận downtime khi bảo trì. | Không có kết quả dở dang được hiển thị | UCL-GEN-002 | PS §8 | APPROVED |
| STR-QUAL-013 | Interaction Capability | Giao diện và kết quả bằng tiếng Việt và tiếng Anh; trích dẫn điều khoản giữ nguyên văn gốc. Ưu tiên desktop, xem được trên tablet. | Người dùng chọn được ngôn ngữ giao diện | UCL-GEN-001, UCL-GEN-002, UCL-GEN-003, UCL-GEN-004 | PS §5, §8; Idea/Project.md (đa ngôn ngữ) | APPROVED |
| STR-QUAL-014 | Interaction Capability | Khả năng tiếp cận: độ tương phản chữ ≥ 4.5:1, focus nhìn thấy được, dùng được hoàn toàn bằng bàn phím, severity không phụ thuộc màu, có biến thể giảm chuyển động, font hỗ trợ đầy đủ dấu tiếng Việt | WCAG 2.2 AA | UCL-GEN-001, UCL-GEN-002, UCL-GEN-003, UCL-GEN-004 | PD Accessibility & Inclusion | APPROVED |
| STR-QUAL-015 | Performance Efficiency | Chi phí LLM trên mỗi hợp đồng được đo ngay từ đầu; ngân sách được đặt sau khi có baseline | Đo chi phí của 100% lần phân tích; MVP không đặt ngân sách cứng, ngưỡng được đặt sau khi chọn model (OQ-GEN-007) | Nhóm phát triển | PS §8; SD §2.4 | APPROVED |

## 10. UX And Design References

**Last Updated**: 2026-10-24
**Status**: APPROVED
**Author**: Agent

| Reference | Type | Related Features | Location |
|---|---|---|---|
| Bối cảnh UX: người dùng, mục đích, tính cách thương hiệu, anti-reference, nguyên tắc thiết kế, khả năng tiếp cận | Other (design context) | F-VIEW-001, F-VIEW-002, F-CHAT-001 | [docs/PRODUCT.md](../PRODUCT.md) |

Hiện chưa có wireframe, mockup hay prototype. Các nguyên tắc thiết kế chính (PD Design Principles):

1. Hợp đồng là nhân vật chính: phân tích bám vào văn bản.
2. Không có bằng chứng thì không hiện.
3. Màu không bao giờ đứng một mình.
4. Giải thích, không phán xét.

## 11. Assumptions, Constraints And Dependencies

**Last Updated**: 2026-10-24
**Status**: APPROVED
**Author**: Agent

Không có giả định nào do Agent tự ghi, vì Assumption Policy là `ask` (DEC-GEN-003).

| ID | Type | Statement | Source | Impact If False Or Unavailable | Status |
|---|---|---|---|---|---|
| CON-PRJ-001 | Constraint | Nhóm gồm 1 SE (fullstack), 3 thành viên AI, 1 thành viên IA | SD §1 | Phạm vi phải cắt nếu thiếu người | APPROVED |
| CON-PRJ-002 | Constraint | Service nền tảng viết bằng C#/.NET; phần AI viết bằng Python | DI 2026-10-21 | Thay đổi ngôn ngữ ảnh hưởng thiết kế và phân chia ranh giới hệ thống | APPROVED |
| CON-PRJ-003 | Constraint | Hạ tầng triển khai trên AWS (chương trình FCAJ), region Singapore (`ap-southeast-1`) | DI 2026-10-21; DI 2026-10-24, OQ-GEN-008 | Loại instance GPU để chạy model AI tự host phải có ở region này. [CHANGE] Changed by CR-GEN-003 (2026-11-17): bỏ cross-region inference của Bedrock | REVIEW |
| CON-PRJ-004 | Constraint | Ngân sách hạ tầng là credit AWS của chương trình FCAJ, khoảng $200 cho mỗi thành viên | DI 2026-10-21 | Vượt credit thì phải trả tiền thật hoặc cắt môi trường | APPROVED |
| CON-PRJ-005 | Constraint | Thời gian tới hạn demo hoặc nộp MVP: hơn 4 tháng | DI 2026-10-21 | - | APPROVED |
| CON-PRJ-006 | Constraint | Ứng dụng web, không cần cài đặt; ưu tiên desktop | PS §2.2, §8 | - | APPROVED |
| DEP-GEN-001 | Dependency | Model AI tự host bằng Docker trên Amazon EC2, do nhóm AI vận hành; model cụ thể chưa chọn (DEC-GEN-010) | SD §2.4; DI 2026-10-21; DEC-GEN-029 | Không có model thì không có phân tích và chatbot. [CHANGE] Changed by CR-GEN-003 (2026-11-17): thay Amazon Bedrock | REVIEW |
| DEP-GEN-002 | Dependency | Dịch vụ nhận dạng ký tự (OCR) cho bản scan | PS §6.3 ANA-03 | Không đọc được bản scan và ảnh. [DEPRECATED] Bỏ theo DEC-PRD-023 (CR-GEN-001): không còn OCR | DEPRECATED |
| DEP-GEN-003 | Dependency | Bộ eval 20 hợp đồng có nhãn do nhóm soạn và gán nhãn | DI 2026-10-21; PS §11 (P0) | Không đo được SC-GEN-001, SC-GEN-002 | APPROVED |
| DEP-GEN-004 | Dependency | Bộ căn cứ pháp lý chọn lọc (luật Việt Nam và chuẩn quốc tế) đã được nhóm xác minh | PS §7.7 | Căn cứ tham khảo sai hoặc hết hiệu lực (OQ-RISK-001) | APPROVED |
| DEP-GEN-005 | Dependency | Dịch vụ gửi email (xác thực email, đặt lại mật khẩu, mời thành viên) | PS §6.1 ACC-01, ACC-04 | Không đăng ký, mời thành viên được | APPROVED |
| DEP-GEN-006 | Dependency | Hợp đồng demo Cloud/SaaS Service Agreement mẫu do nhóm soạn | PS §5, §10 | Không chạy được SC-GEN-005 | APPROVED |

## 12. Release Criteria

**Last Updated**: 2026-10-24
**Status**: APPROVED
**Author**: Agent

| Criterion ID | Criterion | Measure | Category | Source | Status |
|---|---|---|---|---|---|
| RC-GEN-001 | Mọi user story Must được nghiệm thu | 100% user story Must qua toàn bộ tiêu chí nghiệm thu | Functionality | DI 2026-10-24, Q-PRD-010 | APPROVED |
| RC-GEN-002 | Đạt ngưỡng chất lượng AI | SC-GEN-001 và SC-GEN-002 đạt trên bộ eval 20 hợp đồng ở lần chạy eval gần nhất trước phát hành | Quality | DI 2026-10-24, Q-PRD-010 | APPROVED |
| RC-GEN-003 | Kiểm thử bảo mật đạt | 0 lỗi mức Critical còn mở trong các bộ kiểm thử cách ly tổ chức, phân quyền chia sẻ và prompt injection | Compliance | DI 2026-10-24, Q-PRD-010; PS §9.3 | APPROVED |
| RC-GEN-004 | Kịch bản demo chạy trọn | Kịch bản demo (PS §10, SC-GEN-005) chạy từ upload tới xuất báo cáo trên môi trường staging mà không lỗi | Operations | DI 2026-10-24, Q-PRD-010 | APPROVED |

## 13. Milestones

**Last Updated**: 2026-10-24
**Status**: APPROVED
**Author**: Agent

Mục này được bỏ qua vì bộ tài liệu không có release plan (DEC-GEN-006), và mục này là Conditional với PRD Standard. Ngày demo FCAJ được theo dõi tại OQ-GEN-003. Xem lại nếu nhóm cần release plan.

## 14. Risks, Open Questions And Decisions

**Last Updated**: 2026-10-27
**Status**: APPROVED
**Author**: Agent

### 14.1 Open Questions

| Question ID | Question | Context | Affected Sections | Owner | Due Date | Status | Resolution |
|---|---|---|---|---|---|---|---|
| OQ-GEN-003 | Ngày demo cuối kỳ thực tập FCAJ là ngày nào? | Thời điểm đánh giá BG-GEN-001..004 | §1, §3 | Approver | - | CLOSED | DI 2026-10-24: đầu tháng 12/2026 (chưa có ngày cụ thể) |
| OQ-GEN-004 | Mức thành thạo công nghệ của từng persona; khó khăn hiện tại và bối cảnh sử dụng của người review chuyên môn và Owner tổ chức | Nguồn chưa mô tả | §5 | Approver | - | CLOSED | 2026-10-26: mức thành thạo công nghệ Giám đốc, Sales Trung bình; người review chuyên môn Cao; Owner tổ chức Trung bình; khó khăn và bối cảnh của UCL-GEN-003, UCL-GEN-004 ở §5 |
| OQ-GEN-005 | Các feature quản lý (tài khoản, tổ chức, chia sẻ, thư viện, trạng thái, phiên bản, xóa, nhật ký) phục vụ mục tiêu nào? Thêm mục tiêu mới, hay gắn vào mục tiêu hiện có? | PRD check PD4 yêu cầu mọi feature Must truy vết tới ít nhất một mục tiêu | §3, §5, §7 | Approver | - | CLOSED | DI 2026-10-24: các feature này thuộc chức năng quản lý; chưa có mục tiêu về quản lý nên tạo BG-GEN-004 (DEC-PRD-015) |
| OQ-GEN-006 | Xác nhận các mặc định ở PS Phụ lục C: (a) Owner tổ chức không tự động xem được mọi hợp đồng; (b) Member hợp đồng được upload phiên bản mới và đổi trạng thái; (c) hợp đồng được lưu đến khi người dùng xóa; (d) mỗi người chỉ thuộc một tổ chức trong MVP | PS ghi là "câu hỏi còn mở, đã đặt mặc định" | §7.2, §8.2 | Approver | - | CLOSED | DI 2026-10-24: đổi (a) thành **Owner tổ chức tự động xem được mọi hợp đồng**; giữ (b), (c), (d) (DEC-PRD-013) |
| OQ-GEN-007 | Ngân sách chi phí LLM trên mỗi hợp đồng là bao nhiêu? | PS §8 nói đặt sau khi có baseline; model chưa chọn | §9 | Approver | - | CLOSED | 2026-10-26: MVP không đặt ngân sách cứng, chỉ đo và theo dõi; đặt ngưỡng sau khi chọn model |
| OQ-GEN-008 | Triển khai ở region AWS nào? | SD §1 mới ghi giả định `ap-southeast-1` | §11 | Approver | - | CLOSED | DI 2026-10-24: Singapore (`ap-southeast-1`) (DEC-GEN-015) |
| OQ-CTR-001 | Giới hạn kích thước file và số trang khi upload là bao nhiêu? | PS §5 mới ghi "đề xuất ≤ 20 MB, ≤ 100 trang" | §7.2, §8.3 | Approver | - | CLOSED | DI 2026-10-24: theo đề xuất, ≤ 20 MB và ≤ 100 trang (DEC-PRD-014) |
| OQ-EXP-001 | Khi tạo báo cáo thất bại, người dùng thấy gì? | Nguồn chưa mô tả | §6.1 | Approver | - | CLOSED | DI 2026-10-24: hiển thị thông báo tạo báo cáo thất bại |
| OQ-GEN-010 | Thước đo và chỉ tiêu của BG-GEN-004 (quản lý hợp đồng tập trung) là gì? | Mục tiêu mới tạo theo OQ-GEN-005, chưa có số đo | §3 | Approver | - | CLOSED | 2026-10-26: 100% user story Must của các feature quản lý đạt tiêu chí nghiệm thu trước buổi demo |
| OQ-SHARE-001 | Ngoài việc xem, Owner tổ chức được làm gì trên hợp đồng không được chia sẻ cho mình (chat, xuất báo cáo, đổi trạng thái, upload phiên bản, chia sẻ, xóa)? | Phát sinh từ DEC-PRD-013 | §7.2 F-SHARE-001, §8.2 | Approver | - | CLOSED | 2026-10-26: Owner tổ chức có mọi quyền của Owner hợp đồng trên mọi hợp đồng của tổ chức (DEC-PRD-017) |
| OQ-RISK-001 | Bộ căn cứ pháp lý chọn lọc gồm những điều luật nào, và đã được xác minh số điều và hiệu lực chưa? [REVIEW] | PS §7.7 ghi là danh sách ứng viên, cần xác minh | §11 | Approver | - | CLOSED | 2026-10-26: dùng danh sách ứng viên PS §7.7; thành viên IA xác minh số điều và hiệu lực, ghi người và ngày xác minh trước khi nạp |
| OQ-PRIV-001 | Hệ thống xử lý email người dùng và nội dung hợp đồng có thể chứa dữ liệu cá nhân. Sản phẩm phục vụ người dùng tại Việt Nam: quy định bảo vệ dữ liệu cá nhân của Việt Nam có áp dụng cho ContractGuard không, và áp dụng ở mức nào? [REVIEW] | Rà soát quyền riêng tư bắt buộc khi có dữ liệu cá nhân (`08-Project Profiles.md` §6). Agent không kết luận luật nào áp dụng. | §9, SRS §11.8, §11.15 | Approver | - | CLOSED | 2026-10-26: hiện chưa cần vì là dự án demo (DEC-PRD-019); SRS NFR-COMPL-001 DEPRECATED |
| OQ-GEN-009 | Nhóm đánh giá xác suất và mức độ nghiêm trọng của các rủi ro ở §14.3 thế nào? | Agent không tự ước lượng | §14.3 | Approver | - | CLOSED | 2026-10-26: RISK-GEN-006 Medium/High; RISK-GEN-008 Medium/Medium. Phần còn lại đã trả lời 2026-10-24 |

### 14.2 Decisions

Các quyết định cấp dự án (profile, bộ tài liệu, độ trễ, ưu tiên, hoãn chọn model...) nằm ở `00-Project-State.md` §6 (DEC-GEN-001..025). Bảng dưới là các quyết định sản phẩm do Approver đưa ra trực tiếp khi hợp nhất ý tưởng, đã ghi ở PS Phụ lục A.

| Decision ID | Decision | Date | Decision Owner | Rationale | Affected Items | Status |
|---|---|---|---|---|---|---|
| DEC-PRD-001 | Tên sản phẩm là ContractGuard | 2026-10-20 | Approver | Tên dùng ở đa số tài liệu nguồn | Toàn bộ | APPROVED |
| DEC-PRD-002 | Khách hàng chính là SME Việt Nam; hợp đồng tiếng Việt và tiếng Anh; giữ 7 nhóm rủi ro và phân tích sâu Cybersecurity, Privacy, Third-Party | 2026-10-20 | Approver | Kết hợp hai hướng định vị trong Idea | §2, §5, F-RISK-001, F-RISK-002 | APPROVED |
| DEC-PRD-003 | Kết luận ở dạng "rủi ro tiềm ẩn, cần xem xét" kèm căn cứ tham khảo; không kết luận vi phạm pháp luật | 2026-10-20 | Approver | Việc áp dụng luật phụ thuộc quốc gia, loại giao dịch và hoàn cảnh | F-RISK-001, US-RISK-003 | APPROVED |
| DEC-PRD-004 | MVP dùng bộ căn cứ pháp lý nhỏ chọn lọc | 2026-10-20 | Approver | Giảm khối lượng và rủi ro sai | F-RISK-001, DEP-GEN-004 | APPROVED |
| DEC-PRD-005 | Giữ 7 nhóm rủi ro (Privacy tách riêng), kèm 3 quy tắc: một nhóm chính cho mỗi finding, trạng thái N/A, quy ước ranh giới | 2026-10-21 | Approver | Tránh đếm điểm lặp và hiểu nhầm nhóm không liên quan là an toàn | F-RISK-001, F-RISK-003 | APPROVED |
| DEC-PRD-006 | Thang severity 4 mức: Low, Medium, High, Critical | 2026-10-20 | Approver | Thống nhất các thang khác nhau trong Idea | F-RISK-001, F-VIEW-001 | APPROVED |
| DEC-PRD-007 | Hình thức tương tác chính là workspace hợp đồng gồm dashboard, document viewer và chatbot | 2026-10-20 | Approver | Không phải chatbot thuần | F-VIEW-001, F-VIEW-002, F-CHAT-001 | APPROVED |
| DEC-PRD-008 | Báo cáo PDF và DOCX thuộc MVP; redline để sau | 2026-10-20 | Approver | Redline khó với PDF và bản scan | F-EXP-001, F-EXP-003 | APPROVED |
| DEC-PRD-009 | Người dùng khai báo bên mình là bên nào trong hợp đồng | 2026-10-20 | Approver | Rủi ro phụ thuộc góc nhìn của bên ký | F-CTR-001, US-RISK-005 | APPROVED |
| DEC-PRD-010 | Phân quyền hai cấp (tổ chức và hợp đồng), mỗi cấp chỉ có Owner và Member | 2026-10-21 | Approver | Chưa thấy cần phân quyền chi tiết hơn | F-ORG-001, F-SHARE-001 | APPROVED |
| DEC-PRD-011 | MVP chỉ giải thích, không sửa hợp đồng | 2026-10-21 | Approver | Thu hẹp phạm vi MVP | §4, F-CHAT-005, F-EXP-003 | APPROVED |
| DEC-PRD-012 | Chatbot nhắn tin văn bản, không có giọng nói | 2026-10-21 | Approver | Làm rõ hình thức trợ lý | F-CHAT-001 | APPROVED |
| DEC-PRD-013 | Owner tổ chức tự động xem được mọi hợp đồng của tổ chức. Member hợp đồng được upload phiên bản mới và đổi trạng thái. Hợp đồng được lưu đến khi người dùng xóa. Mỗi người chỉ thuộc một tổ chức trong MVP (phần này được thay bởi DEC-PRD-020, 2026-10-27). | 2026-10-24 | Approver | Trả lời OQ-GEN-006; thay mặc định (a) trong PS Phụ lục C | F-ORG-001, F-SHARE-001, F-CTR-005, US-SHARE-001 | APPROVED |
| DEC-PRD-014 | Giới hạn upload: ≤ 20 MB và ≤ 100 trang | 2026-10-24 | Approver | Trả lời OQ-CTR-001, theo đề xuất PS §5 | F-CTR-001, US-CTR-001 | APPROVED |
| DEC-PRD-015 | Tạo BG-GEN-004 cho nhóm chức năng quản lý; mọi feature quản lý (tài khoản, tổ chức, chia sẻ, thư viện, trạng thái, phiên bản, xóa, nhật ký) truy vết tới BG-GEN-004 | 2026-10-24 | Approver | Trả lời OQ-GEN-005 | §3, §5, §7 | APPROVED |
| DEC-PRD-016 | Nội dung hợp đồng không được đưa ngược vào phần chỉ dẫn (prompt) của AI; bổ sung cơ chế chống prompt injection | 2026-10-24 | Approver | Prompt injection là rủi ro cao (RISK-GEN-007) | F-SEC-001, RISK-GEN-007 | APPROVED |
| DEC-PRD-017 | Owner tổ chức có toàn bộ quyền của Owner hợp đồng (chia sẻ, thu hồi, chuyển quyền Owner, xóa và mọi quyền khác) trên mọi hợp đồng của tổ chức; Owner hợp đồng chỉ có các quyền đó trên hợp đồng của mình | 2026-10-26 | Approver | Trả lời OQ-SHARE-001; mở rộng DEC-PRD-013 | F-SHARE-001, US-SHARE-001; SRS BR-ORG-003, FR-SHARE-002, SEC-AUTHZ-005 | APPROVED |
| DEC-PRD-018 | Bỏ tính năng chia sẻ hợp đồng cho toàn tổ chức. Hợp đồng mới chỉ Owner hợp đồng và Owner tổ chức truy cập được; người khác phải được chia sẻ theo email | 2026-10-26 | Approver | Trả lời OQ-SHARE-003; thay phần "toàn tổ chức" của PS §6.1 ACC-06 | F-SHARE-001, US-SHARE-002 (DEPRECATED); SRS FR-SHARE-005 (DEPRECATED) | APPROVED |
| DEC-PRD-019 | Bản demo chưa xử lý nghĩa vụ pháp lý về bảo vệ dữ liệu cá nhân | 2026-10-26 | Approver | Trả lời OQ-PRIV-001: dự án đang là demo sản phẩm | STR-QUAL-010; SRS NFR-COMPL-001 (DEPRECATED), §11.8 | APPROVED |
| DEC-PRD-020 | Mô hình không gian làm việc kiểu GitHub: mỗi tài khoản có sẵn một không gian cá nhân; người dùng tạo hoặc tham gia được nhiều tổ chức; mỗi hợp đồng thuộc đúng một không gian (không gian cá nhân của Owner, hoặc một tổ chức). Thay phần "mỗi người chỉ thuộc một tổ chức" của DEC-PRD-013 | 2026-10-27 | Approver | Người dùng dùng được hệ thống một mình, tạo tổ chức khi cần làm việc nhóm | F-ORG-001, F-ORG-002; SRS BR-ORG-004, BR-ORG-001 (DEPRECATED), FR-ORG-002 (DEPRECATED) | APPROVED |
| DEC-PRD-021 | Chia sẻ theo loại không gian: hợp đồng của tổ chức chỉ chia sẻ cho thành viên tổ chức đó; hợp đồng cá nhân chia sẻ cho email của bất kỳ tài khoản đã đăng ký. Chuyển hợp đồng giữa các không gian và chuyển quyền Owner của hợp đồng cá nhân là Won't (F-CTR-009) | 2026-10-27 | Approver | Hợp đồng cá nhân cần nhờ được người review bên ngoài; giữ phạm vi MVP gọn | F-SHARE-001, F-SHARE-002, F-CTR-009, US-SHARE-005; SRS BR-SHARE-003, FR-SHARE-009, FR-SHARE-010 | APPROVED |
| DEC-PRD-022 | F-AUTH-003 (MFA) không thuộc MVP, chuyển Won't. Lý do: không cần cho buổi demo. Định hướng sau MVP: ưu tiên Should; mỗi lần đăng nhập đều bắt buộc nhập mã gửi qua email; không dùng ứng dụng xác thực bên thứ ba | 2026-10-27 | Approver | F-AUTH-003, US-AUTH-004; SRS FR-AUTH-012, FR-AUTH-013, SEC-AUTH-004 |
| DEC-PRD-023 | Bỏ đọc bản scan và OCR khỏi sản phẩm. Hệ thống chỉ nhận file PDF có lớp chữ và DOCX; PDF không có lớp chữ phân tích thất bại vì không nhận diện được điều khoản (SRS EDGE-ANA-001) | 2026-11-17 | Approver | Hợp đồng thường dài từ chục trang trở lên, chụp từng trang không thiết thực; bớt một dịch vụ ngoài. Áp dụng qua CR-GEN-001 | F-CTR-001, F-ANA-002, US-CTR-001, US-ANA-003, STR-QUAL-002, DEP-GEN-002; SRS FR-ANA-007, NFR-PERF-003, EXT-INTG-002, IF-INTG-002, BR-CTR-001, FR-UPL-002, FR-UPL-003 | REVIEW |

### 14.3 Product Risks

| Risk ID | Risk | Cause | Impact | Probability | Severity | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|---|---|
| RISK-GEN-001 | Credit FCAJ không đủ cho máy EC2 GPU chạy model | EC2 tính tiền theo giờ máy bật, kể cả khi không có job; máy GPU đắt hơn nhiều so với máy thường | Hết credit trước buổi demo, phải trả tiền thật | Medium | Medium | Chỉ bật máy GPU trong giờ test và demo; chọn instance nhỏ nhất đạt eval; AWS Budgets cảnh báo 50%, 80%. [CHANGE] Changed by CR-GEN-003 (2026-11-17): rủi ro của Bedrock đổi sang EC2 GPU | Nhóm dự án | REVIEW |
| RISK-GEN-002 | Quota vCPU cho instance GPU của account mới thấp hoặc bằng 0 | AWS giới hạn số vCPU của họ instance G và VT cho account mới | Không bật được máy GPU, chặn Phase 7 và buổi demo | High | High | Xin tăng quota (Service Quotas) ngay khi nhóm AI chọn loại máy; giới hạn số phân tích đồng thời. [CHANGE] Changed by CR-GEN-003 (2026-11-17): quota Bedrock đổi sang quota GPU | Nhóm dự án | REVIEW |
| RISK-GEN-003 | Chi phí hạ tầng cố định vượt credit | Các thành phần luôn bật (load balancer, database, IPv4) tính tiền theo giờ | Hết credit trước buổi demo | Low | Low | Dev local phần lớn thời gian; chỉ dựng môi trường AWS ở giai đoạn cuối (SD §2.7) | Nhóm dự án | APPROVED |
| RISK-GEN-004 | Chất lượng AI không đạt SC-GEN-001, SC-GEN-002 | Model hoặc prompt chưa đủ tốt với hợp đồng tiếng Việt | Không đạt RC-GEN-002; người dùng nhận kết quả không chính xác | High | High | Chạy eval hằng tuần; chọn model dựa trên eval (DEC-GEN-010) | Nhóm dự án | APPROVED |
| RISK-GEN-005 | Số token của hợp đồng tiếng Việt cao hơn ước lượng | Tiếng Việt có dấu tốn nhiều token hơn tiếng Anh | Chi phí và độ trễ cao hơn dự kiến | Low | Low | Đếm token trên hợp đồng mẫu sớm (SD §2.9) | Nhóm dự án | APPROVED |
| RISK-GEN-006 | Người dùng hiểu kết quả là tư vấn pháp lý | Kết quả trông có thẩm quyền | Người dùng ra quyết định sai và quy trách nhiệm cho sản phẩm | Medium | High | Diễn đạt "rủi ro tiềm ẩn"; dòng miễn trừ trách nhiệm trên mọi báo cáo (DEC-PRD-003) | Nhóm dự án | APPROVED |
| RISK-GEN-007 | Prompt injection trong hợp đồng làm AI bỏ sót rủi ro | Hợp đồng là dữ liệu không tin cậy | Kết quả bị thao túng | High | High | Nội dung hợp đồng không được đưa ngược vào phần chỉ dẫn (prompt) của AI; bổ sung cơ chế chống prompt injection (DEC-PRD-016); F-SEC-001; bộ test prompt injection (RC-GEN-003) | Nhóm dự án | APPROVED |
| RISK-GEN-008 | Căn cứ pháp lý sai hoặc hết hiệu lực | Danh sách điều luật mới là ứng viên | Mất niềm tin của người review chuyên môn | Medium | Medium | Thành viên IA xác minh số điều và hiệu lực trước khi đưa vào hệ thống (OQ-RISK-001) | Nhóm dự án | APPROVED |

## 15. Approval

**Last Updated**: 2026-10-24
**Status**: APPROVED
**Author**: Human

| Role | Name | Decision | Date | Comments |
|---|---|---|---|---|
| Product Owner Or Approver | SE của nhóm | APPROVED (Gate S5, baseline) | 2026-10-27 | Ghi bởi Agent theo chỉ thị của Approver (DEC-GEN-026); MC9, RS3 là known issues |

Chỉ Approver mới được cho phép đặt trạng thái `APPROVED`.
