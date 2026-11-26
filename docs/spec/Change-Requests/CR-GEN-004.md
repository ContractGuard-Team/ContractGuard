# CR-GEN-004 - AI chạy bằng Docker trên EC2; hình thức host và model do nhóm AI quyết

| Field | Value |
|---|---|
| CR-ID | CR-GEN-004 |
| Date Received | 2026-11-26 |
| Source | Direct Prompt (Approver) |
| Description | Sửa cách ghi của DEC-GEN-029: spec chỉ nói AI chạy bằng Docker trên Amazon EC2 (`ap-southeast-1`). Model chạy ngay trên máy hay gọi qua API bên ngoài, và model nào, do nhóm AI quyết. |
| Priority | P3-Medium |
| Status | APPLIED (chờ Approver duyệt các mục REVIEW) |
| Authorized | 2026-11-26, Approver: "sửa tài liệu lại là host model qua ec2 nhưng không cần nói rõ là local, hay cloud hay là model nào (chỉ cần biết là sẽ chạy qua docker ec2, còn hình thức nào thì team AI quyết định)"; duyệt kế hoạch cùng ngày |
| Decision | DEC-GEN-034 (chờ duyệt) |

## Impact Analysis - CR-GEN-004

### Change Description

CR-GEN-003 viết "model AI tự host bằng Docker trên EC2", nên các mục spec hiểu là model phải chạy trên máy GPU của nhóm. Thực tế ngày 2026-11-26 (`docs/05` §9): worker và các agent chạy trong 9 container Docker trên EC2 `m7i-flex.large` (không GPU), gọi model qua Google Gemini API, ra Internet qua Squid proxy. CR này bỏ chữ "tự host" và các rủi ro GPU, để spec chỉ ràng buộc nơi chạy (Docker trên EC2 ở `ap-southeast-1`), còn hình thức và model là việc của nhóm AI.

### Directly Affected Items

| ID | Section | Current State | Required Change |
|---|---|---|---|
| DEC-GEN-029 | Project State §6 | Model tự host trên EC2 | Giữ dòng, thêm ghi chú; DEC-GEN-034 làm rõ |
| DEC-GEN-010 | Project State §6 | Chỉ chọn trong model tự host được | Model do nhóm AI chọn theo eval |
| CON-PRJ-003 | PRD §11 | Instance GPU phải có ở region | EC2 chạy AI phải có ở region |
| DEP-GEN-001 | PRD §11 | Model tự host bằng Docker trên EC2 | AI chạy bằng Docker trên EC2; hình thức và model do nhóm AI chọn |
| RISK-GEN-001, RISK-GEN-002 | PRD §12 | Rủi ro máy GPU | Rủi ro chi phí EC2 và API model; quota GPU chỉ khi nhóm AI chọn GPU |
| CON-GEN-001 | SRS §2.5 | Mọi lời gọi model tới model tự host trong Docker trên EC2 | Mọi phần AI chạy trong Docker trên EC2 `ap-southeast-1`; hình thức gọi model do nhóm AI quyết |
| EXT-INTG-001, IF-INTG-001 | SRS §2.6, §10.2 | Model tự host | Dịch vụ AI chạy bằng Docker trên EC2 |
| RISK-SRS-003 | SRS §17.3 | Thiếu GPU ở region | Chỉ áp dụng khi nhóm AI chọn GPU |
| SRS §1.1, §1.2, §2.1, §2.4, §2.6 | Mô tả | "model tự host" | "AI chạy bằng Docker trên Amazon EC2" |

### Indirectly Affected Items

| ID | Section | Relationship | Potential Impact |
|---|---|---|---|
| NFR-SEC-002, NFR-PRIV-* | SRS §11 | Dữ liệu gửi tới model | Nếu nhóm AI gọi API bên ngoài, nội dung hợp đồng ra khỏi AWS. Kênh vẫn mã hóa (HTTPS qua proxy). IA cần xem điều khoản dữ liệu của nhà cung cấp model |
| `docs/02`, `docs/03`, `docs/04` C3 | System design | "tự host", "máy GPU" | Sửa chữ cho khớp, không đổi quyết định kiến trúc phía SE |
| Code | Backend, frontend | Không gọi model | Không đổi |

### Conflict Assessment

None. CR-GEN-003 vẫn đúng ở ý chính (không dùng Bedrock, chạy trên EC2); CR này chỉ nới hình thức.

### Risk Assessment

| Risk | Likelihood | Impact |
|---|---|---|
| Nội dung hợp đồng gửi tới nhà cung cấp model bên ngoài | High (đang dùng Gemini API) | Medium. Hợp đồng demo là hợp đồng mẫu, không dùng hợp đồng thật (AGENTS.md); IA rà điều khoản dữ liệu trước khi dùng dữ liệu thật |
| Chi phí API model ngoài credit AWS | Medium | Low. Nhóm AI theo dõi; EC2 chỉ bật khi web bật |

## Applied Changes

- PRD v1.6: CON-PRJ-003, DEP-GEN-001, RISK-GEN-001, RISK-GEN-002 có ghi chú `[CHANGE]`, về REVIEW.
- SRS v1.9: CON-GEN-001, EXT-INTG-001, IF-INTG-001, RISK-SRS-003 có ghi chú `[CHANGE]`, về REVIEW; câu mô tả ở §1.1, §1.2, §2.1, §2.4, §2.6, §21 sửa chữ.
- Project State: DEC-GEN-034 (chờ duyệt), ghi chú ở DEC-GEN-010 và DEC-GEN-029, nhật ký phiên.
- Không mục nào được đặt `APPROVED`.

## Còn chờ

- Approver duyệt các mục `REVIEW` và DEC-GEN-034.
- IA xem điều khoản dữ liệu của nhà cung cấp model mà nhóm AI đang dùng.
