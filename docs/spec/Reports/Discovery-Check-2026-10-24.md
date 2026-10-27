# Discovery Check - PRD v0.1 - 2026-10-24

Kiểm tra `02-PRD.md` v0.1 theo PRD Checks PD1-PD10 (`Agent/02-Discovery/05-PRD Output Contract.md` §6) trước Gate S2.

Phương pháp: đọc lại toàn văn, kết hợp script đếm ID, tham chiếu, marker `[TBD]` và tiêu chí nghiệm thu.

## Kết quả

| ID | Check | Severity | Result | Ghi chú |
|---|---|---|---|---|
| PD1 | Problem statement mô tả vấn đề, không mô tả giải pháp | Critical | PASS | §2 chỉ mô tả người gặp vấn đề, cách xử lý hiện nay và hậu quả |
| PD2 | Mỗi mục tiêu có số đo, hoặc có OQ hỏi số đo | Critical | PASS | BG-GEN-001..003 có SC-GEN-001..005 kèm target. Thời điểm đánh giá là `[TBD]`, theo dõi tại OQ-GEN-003 |
| PD3 | Có nêu non-goals và phần ngoài phạm vi | Critical | PASS | §4 |
| PD4 | Mọi feature `Must` truy vết tới ít nhất một mục tiêu và một persona | Critical | **FAIL** | 7 feature Must chưa có mục tiêu cha: F-AUTH-001, F-ORG-001, F-SHARE-001, F-CTR-003, F-CTR-004, F-CTR-005, F-CTR-006. Đang chờ OQ-GEN-005. Feature Should F-AUTH-002, F-AUTH-003, F-AUD-001 cũng chờ cùng OQ. Mọi feature Must đều đã có persona. |
| PD5 | Mọi feature `Must` có ít nhất một story; story đúng mẫu, qua INVEST, có tiêu chí Given/when/then | Critical | PASS | 19/19 feature Must có story. Tổng 46 story, 100% có Given/when/then. Ghi chú INVEST: US-RISK-001 và US-VIEW-003 có nhiều tiêu chí nhưng vẫn kiểm thử được; đề xuất giữ nguyên. |
| PD6 | Release criteria đo được | Warning | PASS | RC-GEN-001..004 đều có thước đo |
| PD7 | Không có ngày, ước lượng, giá hay năng lực mà người dùng chưa cung cấp | Critical | PASS | Ngân sách, thời gian và độ trễ đều do Approver cung cấp hoặc lấy từ nguồn. Xác suất và mức độ rủi ro để `[TBD]` (OQ-GEN-009). |
| PD8 | Không định nghĩa ID hai lần; mọi tham chiếu đều giải được | Critical | PASS | 0 tham chiếu treo. Mã F xuất hiện ở cả bảng 7.1 và mục chi tiết 7.2 theo cấu trúc template, không phải định nghĩa trùng. OQ ở `00-Project-State.md` chỉ là bảng tóm tắt. |
| PD9 | Mọi mục bắt buộc của biến thể đều có, hoặc theo Skipped Section Protocol | Critical | PASS | Mục 0-15 đều có; §13 Milestones bỏ qua theo protocol (Conditional, không có release plan) |
| PD10 | Mọi `[TBD]` có OQ; không có `[ASSUMPTION]` khi người dùng chưa đồng ý | Critical | PASS | 65 marker `[TBD]`, 100% có OQ trên cùng dòng; 0 marker `[ASSUMPTION]` |

## Mật độ TBD theo mục

| Mục | Ước lượng tỷ lệ ô `[TBD]` | Ngưỡng halt (30%) |
|---|---|---|
| §5 Personas | 8/32 ≈ 25% | Dưới ngưỡng |
| §14.3 Product Risks | 16/72 ≈ 22% | Dưới ngưỡng |
| Các mục khác | < 10% | Dưới ngưỡng |

## Kết luận (lần chạy 1, PRD v0.1)

Gate S2 **chưa thể thông qua** vì PD4 (Critical) FAIL. Cần Approver trả lời OQ-GEN-005, rồi cập nhật cột Goals của các feature liên quan và chạy lại PD4.

## Lần chạy 2 (PRD v0.2)

PRD v0.2 áp dụng các trả lời OQ của Approver ngày 2026-10-24 (OQ-GEN-003, 005, 006, 008, 009, CTR-001, EXP-001).

| ID | Result | Ghi chú |
|---|---|---|
| PD1 | PASS | Không đổi |
| PD2 | PASS | BG-GEN-004 mới có SC-GEN-006 nhưng thước đo và chỉ tiêu còn `[TBD]`; đã có OQ-GEN-010 hỏi số đo, đúng điều kiện của PD2 |
| PD3 | PASS | Không đổi |
| PD4 | PASS | 19/19 feature Must truy vết tới ít nhất một BG và một UCL. Các feature quản lý truy vết tới BG-GEN-004 (DEC-PRD-015). |
| PD5 | PASS | 46 story, 100% có Given/when/then; US-SHARE-001 và US-EXP-001 được thêm tiêu chí |
| PD6 | PASS | Không đổi |
| PD7 | PASS | Ngày demo "đầu tháng 12/2026" và mức rủi ro đều do Approver cung cấp |
| PD8 | PASS | 0 tham chiếu treo |
| PD9 | PASS | Không đổi |
| PD10 | PASS | 24 marker `[TBD]`, 100% có OQ; 0 marker `[ASSUMPTION]` |

**Kết luận:** không còn lỗi Critical. PRD v0.2 sẵn sàng cho Gate S2. Các OQ còn mở không chặn gate: OQ-GEN-004, OQ-GEN-007, OQ-GEN-009 (một phần), OQ-GEN-010, OQ-SHARE-001, OQ-RISK-001, OQ-PRIV-001.
