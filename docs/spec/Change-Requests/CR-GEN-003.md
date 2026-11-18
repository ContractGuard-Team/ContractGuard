# CR-GEN-003 - Model AI tự host bằng Docker trên EC2, thay Amazon Bedrock

| Field | Value |
|---|---|
| CR-ID | CR-GEN-003 |
| Date Received | 2026-11-17 |
| Source | Direct Prompt (Approver) |
| Description | Mọi lời gọi model AI (phân tích, embedding, chatbot) chuyển từ Amazon Bedrock sang model do nhóm AI tự host trong container Docker trên Amazon EC2, region `ap-southeast-1`. Model cụ thể vẫn do eval chọn (DEC-GEN-010). Nền tảng chạy worker (EC2 hay ECS) do nhóm AI quyết, không thuộc CR này. |
| Priority | P2-High |
| Status | APPLIED |
| Authorized | 2026-11-17, Approver: "Đã chốt tự host trên EC2"; cho phép áp dụng sau khi đọc phân tích ("Cho phép áp dụng") |
| Decision | DEC-GEN-029: thay DEC-GEN-016; DEC-GEN-010 giữ việc hoãn chọn model, chỉ trong các model tự host được |

## Impact Analysis - CR-GEN-003

### Change Description

Drawio kiến trúc (`docs/architecture/contractguard-architecture.drawio`) đã vẽ model AI tự host trên EC2, và Approver chốt hướng này ngày 2026-11-17. Spec đang ràng buộc mọi lời gọi model phải qua Bedrock (CON-GEN-001), nên phải đổi bằng CR. Ranh giới hệ thống giữ nguyên: model vẫn là thành phần bên ngoài service C#, do nhóm AI vận hành. Worker và API gọi nó qua một endpoint HTTP thay vì Bedrock API.

### Directly Affected Items

| ID | Section | Current State | Required Change |
|---|---|---|---|
| CON-GEN-001 | SRS §2.5 | APPROVED: mọi lời gọi model phải qua Amazon Bedrock | Mọi lời gọi model AI phải tới model tự host trong Docker trên Amazon EC2 ở `ap-southeast-1`; REVIEW |
| EXT-INTG-001 | SRS §3.4 | Amazon Bedrock, nhà cung cấp AWS | Dịch vụ model AI tự host (Docker trên EC2), do nhóm AI vận hành; REVIEW |
| IF-INTG-001 | SRS §10 | Giao tiếp với Amazon Bedrock | Giao tiếp HTTP với endpoint model tự host; nội dung dữ liệu giữ nguyên; REVIEW |
| CON-PRJ-003 | PRD §11 | APPROVED: cột ảnh hưởng nhắc cross-region inference của Bedrock | Loại instance GPU phải có ở region; REVIEW |
| DEP-GEN-001 | PRD §11 | Model AI trên Amazon Bedrock | Model AI tự host trên EC2 (Docker); model cụ thể chưa chọn (DEC-GEN-010); REVIEW |
| RISK-GEN-001 | PRD §12 | Credit FCAJ không dùng được cho model bên thứ ba trên Bedrock | Đổi thành: credit FCAJ không đủ cho EC2 GPU chạy nhiều giờ. Giảm thiểu: tắt máy ngoài giờ test, chọn instance nhỏ nhất đạt eval; REVIEW |
| RISK-GEN-002 | PRD §12 | Quota Bedrock của account mới thấp | Đổi thành: quota vCPU cho instance GPU (họ G, VT) của account mới thường bằng 0. Giảm thiểu: xin quota ngay khi chọn loại máy; REVIEW |
| RISK-SRS-003 | SRS §17.3 | Model không có ở region Singapore | Đổi thành: loại instance GPU cần dùng không có hoặc hết capacity ở `ap-southeast-1`; REVIEW |
| DEC-GEN-016 | Project State §6 | AI chạy qua Amazon Bedrock | Bị DEC-GEN-029 thay; giữ để lưu vết |

### Indirectly Affected Items

| ID | Section | Relationship | Potential Impact |
|---|---|---|---|
| SRS §1.1, §1.2, §2.1, sơ đồ ngữ cảnh, bảng môi trường (AI Platform), §2.6, §17 | SRS | Nhắc Bedrock trong mô tả | Sửa câu, không đổi yêu cầu |
| NFR-PERF-001, NFR-PERF-004 | SRS §11 | p50 ≤ 2 phút cho 20 trang; chữ đầu tiên chat ≤ 15 giây | Không đổi chỉ tiêu. Tốc độ giờ phụ thuộc loại GPU nhóm AI chọn; phải đo trên staging (Phase 7) |
| NFR-SCAL-001 | SRS §11 | 10 phân tích đồng thời | Không đổi chỉ tiêu. Một máy GPU có giới hạn số request song song; nhóm AI phải chứng minh đạt |
| DEC-GEN-010 | Project State §6 | Liệt kê lựa chọn open-weight trên Bedrock, Custom Model Import, Claude | Giữ việc hoãn chọn model; bỏ các lựa chọn gắn với Bedrock |
| `docs/02-system-design.md` (§2 chi phí và quota, K3, SD-13, SD-14, SD-17), `docs/04` (SD-12, SD-14, SD-17, bảng việc nhóm AI), `docs/03` (Phase 0, Phase 7, D3), `docs/05`, `docs/06`, `docs/07`, `AGENTS.md` | Ngoài spec | Nhắc Bedrock | Cập nhật |
| `docs/architecture/contractguard-architecture.drawio` | Ngoài spec | Đang ghi "pending decision" | Bỏ chữ pending ở phần model |
| `backend/.../Chat/IChatResponder.cs` | Code SE | Chú thích nhắc Bedrock | Sửa chú thích; port không đổi |
| `security/aws/iam/worker-task-policy.TEMPLATE.json`, `security/docs/*` | Thư mục IA | Quyền `bedrock:*` cho worker | **IA tự sửa**; SE không sửa thư mục `security/` |
| Phần đã có của nhóm AI (`HermesBedrockProvider`, `BedrockClient`) | Repo AI | Gọi Bedrock | Nhóm AI thay provider |

### Conflict Assessment

- **CON-PRJ-004 (ngân sách credit FCAJ):** EC2 GPU tính tiền theo giờ máy bật, kể cả khi không có job; Bedrock tính theo token. Không mâu thuẫn trực tiếp, nhưng chi phí phải theo dõi bằng lịch bật/tắt (SD-20).
- **SD-12 (worker scale về 0) và Q1:** không còn áp dụng cho máy model. Máy GPU phải bật trước giờ demo và giờ test, có thời gian khởi động model. Ghi ở phiếu SD.
- Ngoài hai điểm trên: none.

### Risk Assessment

| Risk | Likelihood | Impact |
|---|---|---|
| Quota GPU bằng 0 làm trễ Phase 7 | High (account mới) | High. Xin quota ngay khi nhóm AI chọn loại máy |
| Model open-weight tự host yếu tiếng Việt hơn model trên Bedrock, không đạt SC-GEN-001 (recall, precision) | Medium | High. Eval quyết định; nếu không đạt thì Approver xem lại CR này |
| Chi phí GPU vượt credit | Medium | Medium. Tắt máy ngoài giờ, Budgets cảnh báo 50%/80% |
| Thêm việc vận hành (cập nhật model, giám sát máy) cho nhóm AI | High | Medium |

## Applied Changes

- PRD v1.3: CON-PRJ-003, DEP-GEN-001, RISK-GEN-001, RISK-GEN-002 sửa và về `REVIEW`, có ghi chú `[CHANGE]`.
- SRS v1.4: CON-GEN-001, EXT-INTG-001, IF-INTG-001, RISK-SRS-003 sửa và về `REVIEW`; mô tả ở §1, §2, sơ đồ ngữ cảnh DIA-GEN-001, bảng môi trường, §17 sửa câu. Không mục nào được đặt `APPROVED`.
- Project State: thêm DEC-GEN-029; DEC-GEN-016 ghi bị thay; DEC-GEN-010 thu hẹp lựa chọn; nhật ký phiên.
- Ngoài spec: `docs/02`, `docs/03`, `docs/04`, `docs/05`, `docs/06`, `docs/07`, `AGENTS.md`, drawio, chú thích `IChatResponder.cs`.

## Còn chờ

- Approver duyệt các mục `REVIEW`.
- Nhóm AI: chọn model, loại instance GPU, serving stack; IA: sửa policy IAM trong `security/`.
