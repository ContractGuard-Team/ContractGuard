# SRS Macro Validation Report

**Date**: 2026-10-26
**SRS Version**: 0.9 (PRD 0.3)
**Validator**: Agent (Audit Mode)
**Project Profile / Tier**: TEAM / 2
**Thay cho**: `Validation-Report-2026-10-24.md` (SRS 0.8)

Phạm vi lần chạy này: kiểm tra lại toàn bộ sau khi áp dụng phiếu trả lời OQ (`OQ-Answers.md`).

Phương pháp:
- Kiểm tra máy móc bằng script trên `docs/spec/`: ID, tham chiếu, truy vết, marker, heading, metadata, câu yêu cầu.
- Rà soát thủ công các tiêu chí cần phán đoán.

Các yêu cầu mới và các yêu cầu bị sửa đã qua Micro Validation: mỗi câu yêu cầu có đúng một chữ "phải", không có từ mơ hồ, có tiêu chí Given/when/then.

## Thay đổi so với báo cáo 2026-10-24

- **OQ:** đóng toàn bộ 40 OQ còn mở (33 OQ của SRS, 7 OQ của PRD).
- **Marker `[TBD]`:** SRS từ 186 xuống 0, PRD xuống 0.
- **Chuyển sang `DEPRECATED`** (theo chỉ thị của Approver, giữ lại để lưu vết):
  - US-SHARE-002, FR-SHARE-005, AF-SHARE-001, ATTR-DATA-012 (DEC-PRD-018).
  - NFR-COMPL-001 (DEC-PRD-019).
  - STATE-ORG-003 (OQ-ORG-003).
- **Thêm mới:**
  - Yêu cầu: SEC-AUTH-006, SEC-AUTH-007, FR-AUTH-014, FR-AUTH-015 (khóa đăng nhập theo email, giới hạn quên mật khẩu, DEC-GEN-024).
  - Mã lỗi: ERR-AUTH-005, ERR-AUTH-006, ERR-CTR-003.
  - Chuyển trạng thái: TR-CTR-008, TR-CTR-009.
  - Luồng ngoại lệ: EF-AUTH-003.
  - Quyết định: DEC-PRD-017..019, DEC-GEN-022..024.
- **Mâu thuẫn nguồn mới:** 2 mâu thuẫn, đều giải quyết theo P1 và ghi `[CONFLICT]`. Mục tiêu bản scan và mục tiêu chatbot khác PS §8; ghi ở NFR-PERF-003, NFR-PERF-004, STR-QUAL-002, STR-QUAL-003.

## Summary

| Category | Total Checks | Passed | Failed Critical | Failed Warning |
|---|---:|---:|---:|---:|
| Completeness | 11 | 10 | 0 | 1 |
| Consistency | 9 | 9 | 0 | 0 |
| Traceability | 9 | 9 | 0 | 0 |
| Requirement Set | 5 | 4 | 0 | 1 |
| Structural | 5 | 5 | 0 | 0 |

Không có lỗi Critical. Còn 2 cảnh báo (MC9, RS3), cả hai đã có từ báo cáo trước. Hai cảnh báo MC7 và RS1 đã hết.

## Số liệu

| Chỉ số | Giá trị |
|---|---|
| Yêu cầu chức năng (FR) | 145: 144 đang dùng (126 Must, 15 Should, 3 Could), 1 DEPRECATED |
| Yêu cầu phi chức năng (NFR) | 29: 28 đang dùng (Must), 1 DEPRECATED |
| Use case / Business rule | 20 / 30 |
| Yêu cầu xác thực và phân quyền (SEC) / giao diện (UI) | 15 / 9 |
| Mã lỗi / trường hợp biên / chuyển trạng thái | 23 / 15 / 16 |
| Dòng RTM / dòng phụ thuộc | 174 / 115 |
| Marker `[TBD]` trong PRD và SRS | 0 |
| OQ còn mở | 0 |

## Kết quả từng tiêu chí

| ID | Kết quả | Ghi chú |
|---|---|---|
| MC1 | PASS | Đủ 23 mục (0–22) theo Tier 2 + TEAM |
| MC2 | PASS | 28/28 feature trong phạm vi có ít nhất một FR đang dùng |
| MC3 | PASS | 5/5 actor xuất hiện trong use case và yêu cầu |
| MC4 | PASS | 20/20 use case có yêu cầu liên quan |
| MC5 | PASS | Đủ các nhóm NFR. Ba mục ghi "Không áp dụng" kèm lý do và quyết định: Backup (§11.14, OQ-BKP-001); đồng ý và quyền của cá nhân ở Privacy (§11.8, DEC-PRD-019); NFR-COMPL-001 (DEPRECATED) |
| MC6 | PASS | 23 mã lỗi (§14.1), 15 trường hợp biên (§14.2); mọi cột xử lý đã có giá trị |
| MC7 | PASS | 0 `[TBD]`, 0 OQ mở |
| MC8 | PASS | 145/145 FR có tiêu chí Given/when/then |
| MC9 | **WARNING** | Như báo cáo trước: danh sách cột chi tiết của bảng để thiết kế hệ thống quyết định (§9.3.3, [REVIEW]) |
| MC10 | PASS | Mọi user story Must đang dùng, mọi STR và mọi feature được hiện thực bằng ít nhất một yêu cầu |
| MC11 | PASS | 15/15 STR-QUAL được nối vào NFR hoặc FR |
| CC1 | PASS | Thuật ngữ theo §1.4 và §18.1. Đã cập nhật Owner tổ chức, Member hợp đồng, MFA |
| CC2 | PASS | Không có ID định nghĩa trùng giữa PRD và SRS |
| CC3 | PASS | 0 tham chiếu tới ID không tồn tại |
| CC4 | PASS | Không có yêu cầu Must phụ thuộc yêu cầu Should hay Could |
| CC5 | PASS | Chỉ dùng DRAFT, REVIEW, APPROVED, DEPRECATED cho yêu cầu; OQ dùng thêm CLOSED |
| CC6 | PASS | 3 mâu thuẫn nguồn với PS §2.2, §8 (thời gian text 20 trang, bản scan, chatbot) đều giải quyết theo P1 (DEC-GEN-022) và ghi `[CONFLICT]` ở PRD STR-QUAL-001..003 và SRS NFR-PERF-001, NFR-PERF-003, NFR-PERF-004 |
| CC7 | PASS | DIA-CTR-001 vẽ lại khớp §13.2 (gồm TR-CTR-008, TR-CTR-009 và phân tích phiên bản mới); không còn mũi tên chờ OQ |
| CC8 | PASS | Không có yêu cầu đang dùng cho feature Won't. Quyền Owner tổ chức (DEC-PRD-017) thống nhất ở BR-ORG-003, SEC-AUTHZ-005, FR-SHARE-002, FR-CTR-019, §3.3 và §12.3 |
| CC9 | PASS | TBL tham chiếu ENT; VAL tham chiếu mã ERR có ở §14.1; ERR-AUTH-005, ERR-CTR-003 được tham chiếu từ EF-AUTH-003, TR-CTR-007, EDGE-ANA-003 |
| TC1 | PASS | 174/174 yêu cầu có Source |
| TC2 | PASS | Mọi FR, NFR có tiêu chí nghiệm thu; AC-GEN-005 cập nhật theo DEC-GEN-022 |
| TC3 | PASS | 0 yêu cầu không có nguồn |
| TC4 | PASS | 0 feature trong phạm vi không có yêu cầu |
| TC5 | PASS | RTM sinh lại từ các khối yêu cầu: 174 dòng khớp 174 yêu cầu, gồm cả 2 yêu cầu DEPRECATED |
| TC6 | PASS | Mọi đích phụ thuộc là ID tồn tại |
| TC7 | PASS | §9.11 nối dữ liệu với nguồn, bảng, feature, yêu cầu, rule kiểm tra, rule lưu giữ |
| TC8 | PASS | 174/174 yêu cầu truy vết lên F, US, STR, BG hoặc SC |
| TC9 | PASS | Không còn `[TBD]`; mọi OQ đóng có Resolution |
| RS1 | PASS | Các chi tiết Must về tài khoản, phân quyền và trạng thái đã có quyết định |
| RS2 | PASS | Không thấy mâu thuẫn hay trùng lặp giữa các yêu cầu đang dùng |
| RS3 | **WARNING** | Như báo cáo trước: khối lượng yêu cầu lớn so với 1 SE và hơn 4 tháng (CON-PRJ-001, CON-PRJ-005) |
| RS4 | PASS | Viết tiếng Việt, theo mẫu kit, có thuật ngữ |
| RS5 | PASS | Release criteria và AC-GEN-001..005 cho phép xác nhận mục tiêu |
| SC1 | PASS | Heading không nhảy cấp |
| SC2 | PASS | Cấu trúc khớp `Context/SRS-TOC.md`; mục lục sinh lại |
| SC3 | PASS | 23/23 mục có ngày, trạng thái, tác giả |
| SC4 | PASS | Không có thẻ HTML hay URL trần (`<table>_id` là ví dụ quy ước trong backtick) |
| SC5 | PASS | Có glossary, sơ đồ, tài liệu hỗ trợ |

## Failed Checks

### MC9 - Data model coverage

- **Severity**: Warning
- **Location**: §9.3.3
- **Finding**: Danh sách cột của từng bảng không liệt kê chi tiết; quy tắc là cột tương ứng 1:1 với thuộc tính §9.4.
- **Recommended Action**: Approver xác nhận mức chi tiết này, hoặc yêu cầu bổ sung bảng cột chi tiết.

### RS3 - Feasible

- **Severity**: Warning
- **Location**: Toàn bộ §8, §11
- **Finding**: Khối lượng yêu cầu lớn so với nhân sự nền tảng (1 SE).
- **Recommended Action**: Khi lập kế hoạch, làm xong các yêu cầu Must của luồng demo trước (SC-GEN-005). Việc hạ ưu tiên một số Must không nằm trên luồng demo là quyết định của Approver.

## Điểm Approver nên xem lại

Các mục sau không phải lỗi kiểm tra, nhưng là hệ quả của quyết định mới:

- **MFA bằng OTP qua email** (FR-AUTH-013) và **đặt lại mật khẩu** (FR-AUTH-009) cùng dựa vào hộp thư email. Ai chiếm được email thì vượt qua được cả hai. Với bản demo thì chấp nhận được; nếu dùng thật nên cân nhắc ứng dụng xác thực.
- **NFR-COMPL-001 DEPRECATED** (DEC-PRD-019): bản demo không xử lý nghĩa vụ pháp lý về dữ liệu cá nhân. Nên tránh dùng hợp đồng thật có dữ liệu cá nhân khi demo.
- **Khóa đăng nhập theo email** (SEC-AUTH-006): người biết email của bạn có thể cố tình nhập sai 5 lần để khóa đăng nhập bằng mật khẩu 15 phút. Đã giảm thiểu: vẫn cho đặt lại mật khẩu trong lúc bị khóa. Rate limit theo IP (SEC-AUTH-007) có ngưỡng cao nên không khóa lây người dùng chung mạng. Khi triển khai sau load balancer, cần đọc IP thật từ header `X-Forwarded-For` (việc của thiết kế hệ thống).

## Kết luận

Không có lỗi Critical. SRS v0.9 và PRD v0.3 đủ điều kiện để Approver quyết định Gate S5. Agent không tự duyệt.

Các lựa chọn của Approver:
- (A) Thông qua Gate S5 (baseline), coi MC9 và RS3 là known issues.
- (B) Yêu cầu chỉnh sửa thêm, rồi chạy lại kiểm tra.
