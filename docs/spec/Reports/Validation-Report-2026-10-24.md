# SRS Macro Validation Report

**Date**: 2026-10-24
**SRS Version**: 0.8
**Validator**: Agent (Audit Mode)
**Project Profile / Tier**: TEAM / 2

Phương pháp:
- Kiểm tra máy móc bằng script trên `docs/spec/`: ID, tham chiếu, truy vết, marker, heading, metadata, câu yêu cầu.
- Rà soát thủ công các tiêu chí cần phán đoán.

Các lỗi Micro Validation tìm thấy trong lúc viết đã được sửa trước khi lập báo cáo này:
- 3 câu yêu cầu dùng "không phải", tạo ra hai chữ "phải" trong một câu.
- 1 câu dùng "có thể".
- 4 STR-QUAL chưa được nối vào yêu cầu nào.

## Summary

| Category | Total Checks | Passed | Failed Critical | Failed Warning |
|---|---:|---:|---:|---:|
| Completeness | 11 | 9 | 0 | 2 |
| Consistency | 9 | 9 | 0 | 0 |
| Traceability | 9 | 9 | 0 | 0 |
| Requirement Set | 5 | 3 | 0 | 2 |
| Structural | 5 | 5 | 0 | 0 |

Không có lỗi Critical. Có 4 cảnh báo, chi tiết bên dưới.

## Số liệu

| Chỉ số | Giá trị |
|---|---|
| Yêu cầu chức năng (FR) | 143: 126 Must, 14 Should, 3 Could |
| Yêu cầu phi chức năng (NFR) | 29, đều là Must |
| Use case / Business rule | 20 / 30 |
| Yêu cầu xác thực và phân quyền (SEC) / giao diện (UI) | 13 / 9 |
| Thực thể dữ liệu / bảng | 21 / 22 |
| Dòng RTM / dòng phụ thuộc | 172 / 113 |
| Marker `[TBD]` trong SRS | 186; mật độ mỗi mục dưới 5% (ngưỡng halt là 30%) |
| OQ do SRS sở hữu | 33: 23 Must, 7 Should, 3 Could |

## Kết quả từng tiêu chí

| ID | Kết quả | Ghi chú |
|---|---|---|
| MC1 | PASS | Đủ 23 mục (0–22) theo Tier 2 + TEAM; mục cắt gọt có lý do (DEC-GEN-018) |
| MC2 | PASS | 28/28 feature trong phạm vi có ít nhất một FR |
| MC3 | PASS | 5/5 actor xuất hiện trong use case và yêu cầu |
| MC4 | PASS | 20/20 use case có yêu cầu liên quan |
| MC5 | PASS | Đủ các nhóm Performance, Scalability, Security, Privacy, Usability, Accessibility, Compatibility, Maintainability, Compliance; Availability, Reliability, Logging có lý do hoặc tham chiếu FR |
| MC6 | PASS | 20 mã lỗi (§14.1), 15 trường hợp biên (§14.2) |
| MC7 | **WARNING** | Mọi `[TBD]` đều có OQ và mật độ dưới ngưỡng, nhưng 23 OQ đang chặn yêu cầu Must |
| MC8 | PASS | 143/143 FR có tiêu chí Given/when/then |
| MC9 | **WARNING** | Có đủ thực thể, thuộc tính, quan hệ, ràng buộc, sở hữu, vòng đời, lưu giữ, migration (N/A), truy vết dữ liệu; danh sách cột chi tiết của bảng để thiết kế hệ thống quyết định (§9.3.3, [REVIEW]) |
| MC10 | PASS | Mọi user story Must, mọi STR và mọi feature của PRD được hiện thực bằng ít nhất một yêu cầu |
| MC11 | PASS | 15/15 STR-QUAL được nối vào NFR hoặc FR |
| CC1 | PASS | Thuật ngữ theo §1.4 và §18.1 |
| CC2 | PASS | Không có ID định nghĩa trùng giữa PRD và SRS; không có heading định nghĩa trùng |
| CC3 | PASS | 0 tham chiếu tới ID không tồn tại |
| CC4 | PASS | Không có yêu cầu Must phụ thuộc yêu cầu Should hay Could |
| CC5 | PASS | Chỉ dùng DRAFT, REVIEW, APPROVED, DEPRECATED cho yêu cầu; OQ dùng thêm CLOSED (§20) |
| CC6 | PASS | Một mâu thuẫn nguồn (độ trễ 45 giây và 60 giây) đã giải quyết theo P1 và ghi `[CONFLICT]` ở NFR-PERF-001 |
| CC7 | PASS | DIA-CTR-001 khớp bảng §13.2; các mũi tên chưa chốt được đánh dấu |
| CC8 | PASS | Không có yêu cầu cho feature Won't; 3 FR Could gắn với F-CMP-001, F-POL-001 |
| CC9 | PASS | TBL tham chiếu ENT; VAL tham chiếu mã ERR có ở §14.1 |
| TC1 | PASS | 172/172 yêu cầu có Source |
| TC2 | PASS | Mọi FR, NFR có tiêu chí nghiệm thu; AC-GEN-001..005 gắn release criteria |
| TC3 | PASS | 0 yêu cầu không có nguồn |
| TC4 | PASS | 0 feature trong phạm vi không có yêu cầu |
| TC5 | PASS | RTM sinh từ chính các khối yêu cầu: 172 dòng khớp 172 yêu cầu |
| TC6 | PASS | Mọi đích phụ thuộc là ID tồn tại |
| TC7 | PASS | §9.11 nối dữ liệu với nguồn, bảng, feature, yêu cầu, rule kiểm tra, rule lưu giữ |
| TC8 | PASS | 172/172 yêu cầu truy vết lên F, US, STR, BG hoặc SC |
| TC9 | PASS | Mọi `[TBD]` có OQ trên cùng dòng; mọi OQ đã được liệt kê cho Approver |
| RS1 | **WARNING** | Tập yêu cầu bao phủ mọi nhu cầu trong phạm vi, nhưng còn nhiều chi tiết Must chờ OQ, tập trung ở tài khoản, phân quyền và trạng thái |
| RS2 | PASS | Không thấy mâu thuẫn hay trùng lặp |
| RS3 | **WARNING** | 143 FR và 29 NFR là khối lượng lớn với 1 SE và hơn 4 tháng (CON-PRJ-001, CON-PRJ-005). Cần ưu tiên theo MoSCoW khi lập kế hoạch. |
| RS4 | PASS | Viết tiếng Việt, theo mẫu kit, có thuật ngữ |
| RS5 | PASS | Release criteria và AC-GEN-001..005 cho phép xác nhận mục tiêu |
| SC1 | PASS | Heading không nhảy cấp |
| SC2 | PASS | Cấu trúc khớp `Context/SRS-TOC.md` |
| SC3 | PASS | 23/23 mục có ngày, trạng thái, tác giả |
| SC4 | PASS | Không có thẻ HTML hay URL trần (`<table>_id` là ví dụ quy ước đặt tên trong backtick) |
| SC5 | PASS | Có glossary, sơ đồ, tài liệu hỗ trợ; TBD được theo dõi ở §17.1 |

## Failed Checks

### MC7 - TBD tracking

- **Severity**: Warning
- **Location**: §17.1 (23 OQ Priority Must)
- **Finding**: Các marker đều được theo dõi và dưới ngưỡng, nhưng nhiều yêu cầu Must chưa đủ chi tiết để cài đặt và kiểm thử. Ví dụ: chính sách mật khẩu, thời hạn link, quyền của Owner tổ chức trên hợp đồng không được chia sẻ, chuyển trạng thái tự động hay thủ công.
- **Recommended Action**: Approver trả lời các OQ Must trước khi baseline, hoặc baseline kèm danh sách OQ là known issues.

### MC9 - Data model coverage

- **Severity**: Warning
- **Location**: §9.3.3
- **Finding**: Danh sách cột của từng bảng không liệt kê chi tiết; quy tắc là cột tương ứng 1:1 với thuộc tính §9.4.
- **Recommended Action**: Approver xác nhận mức chi tiết này, hoặc yêu cầu bổ sung bảng cột chi tiết.

### RS1 - Complete

- **Severity**: Warning
- **Location**: domain `AUTH`, `ORG`, `SHARE`, `CTR`; §12.3, §13.2
- **Finding**: Như MC7.
- **Recommended Action**: Như MC7.

### RS3 - Feasible

- **Severity**: Warning
- **Location**: Toàn bộ §8, §11
- **Finding**: Khối lượng yêu cầu lớn so với nhân sự nền tảng (1 SE).
- **Recommended Action**: Khi lập kế hoạch, làm xong các yêu cầu Must của luồng demo trước (SC-GEN-005); cân nhắc hạ ưu tiên một số Must không nằm trên luồng demo. Việc hạ ưu tiên là quyết định của Approver.

## Kết luận

Không có lỗi Critical; SRS v0.8 đủ điều kiện để Approver quyết định Gate S5. Agent không tự duyệt.

Các lựa chọn của Approver:
- (A) Trả lời các OQ Must, Agent cập nhật SRS rồi chạy lại báo cáo.
- (B) Baseline ngay, coi các OQ còn mở là known issues.
- (C) Yêu cầu chỉnh sửa khác.
