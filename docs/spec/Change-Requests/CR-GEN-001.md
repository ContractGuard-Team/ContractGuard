# CR-GEN-001 - Bỏ đọc bản scan và OCR

| Field | Value |
|---|---|
| CR-ID | CR-GEN-001 |
| Date Received | 2026-11-17 |
| Source | Direct Prompt (Approver) |
| Description | Bỏ chức năng đọc bản scan và OCR khỏi dự án. Người dùng chỉ upload file PDF có lớp chữ hoặc DOCX; không nhận ảnh JPG, PNG. PDF không có lớp chữ không được hỗ trợ. |
| Priority | P2-High |
| Status | APPLIED |
| Authorized | 2026-11-17, Approver: "Sửa hết tất cả, từ tài liệu tới code"; PDF không có lớp chữ: "Xóa chức năng này đi" |
| Decision | DEC-PRD-023 (PRD §14) |

## Impact Analysis - CR-GEN-001

### Change Description

Hợp đồng thường dài từ chục trang trở lên, nên chụp từng trang để upload không thiết thực. Bỏ OCR cũng bớt một dịch vụ ngoài (Textract). Hệ thống chỉ nhận PDF có lớp chữ và DOCX. Nếu người dùng upload PDF không có lớp chữ (ví dụ bản scan), phân tích thất bại với lý do "không nhận diện được điều khoản" theo EDGE-ANA-001. Không thêm bước phát hiện riêng.

### Directly Affected Items

| ID | Section | Current State | Required Change |
|---|---|---|---|
| US-ANA-003 | PRD §8 | APPROVED: đọc bản scan và ảnh | DEPRECATED |
| STR-QUAL-002 | PRD §10 | APPROVED: bản scan ≤ 50 trang p50 ≤ 5 phút | DEPRECATED |
| DEP-GEN-002 | PRD §11 | APPROVED: dịch vụ OCR | DEPRECATED |
| F-CTR-001, US-CTR-001 | PRD §7, §8 | Nhận PDF scan, JPG, PNG | Chỉ PDF có lớp chữ, DOCX; REVIEW |
| F-ANA-002 | PRD §7; SRS §5 | Trích văn bản kể cả OCR | Trích văn bản từ PDF có lớp chữ và DOCX; REVIEW |
| FR-ANA-007 | SRS §8 | APPROVED: nhận dạng bản scan bằng OCR | DEPRECATED |
| NFR-PERF-003 | SRS §11 | APPROVED: thời gian phân tích bản scan | DEPRECATED |
| EXT-INTG-002, IF-INTG-002 | SRS §3.4, §10 | Dịch vụ OCR | DEPRECATED |
| BR-CTR-001, FR-UPL-002, DATA-DATA-006, VAL-DATA-003 | SRS §7, §8, §9 | PDF, DOCX, JPG, PNG | PDF, DOCX; REVIEW |

### Indirectly Affected Items

| ID | Section | Relationship | Potential Impact |
|---|---|---|---|
| FR-UPL-003 | SRS §8 | Tiêu chí dùng PNG đổi tên thành .pdf | Đổi ví dụ sang DOCX; REVIEW |
| FR-ANA-004 | SRS §8 | Tiêu chí dùng lỗi dịch vụ OCR | Đổi sang lỗi dịch vụ model AI; REVIEW |
| FR-ANA-008, FR-ANA-013 | SRS §8, §16 | Phụ thuộc FR-ANA-007 | Bỏ phụ thuộc; REVIEW |
| UC-ANA-001, UC-CTR-001, PF-GEN-003, §2 ranh giới, DIA-GEN-001, quy trình to-be §4 | SRS §2, §4, §6 | Nhắc OCR, JPG, PNG | Bỏ phần nhắc tới |
| EDGE-UPL-001, EDGE-ANA-001 | SRS §15 | Đếm trang ảnh; bản scan trắng | Bỏ ảnh; nêu PDF không có lớp chữ |
| `contracts/extraction.v2`, `analysis-request.v1` | Ngoài spec | Enum `jpg`, `png`; trường `is_scanned` | Bỏ; cần AI + Backend + Security duyệt |
| Code SE | Ngoài spec | `FileType`, `FileTypeDetector`, upload, viewer ảnh, thông báo lỗi | Bỏ JPG, PNG |
| `docs/02-system-design.md` §2, `docs/03-implementation-phases.md`, `docs/PRODUCT.md`, sơ đồ kiến trúc | Ngoài spec | Nhắc bản scan, Textract | Cập nhật |

### Conflict Assessment

None. DEC-GEN-022 (mục tiêu hiệu năng) vẫn đúng với phần còn lại; mục tiêu bản scan trong đó không còn áp dụng. OQ-GEN-013, OQ-PERF-001 đã CLOSED, giữ nguyên để lưu vết.

### Risk Assessment

| Risk | Likelihood | Impact |
|---|---|---|
| Người dùng chỉ có bản scan nên không dùng được sản phẩm | Medium | Medium. Thông báo lỗi ở upload và EDGE-ANA-001 cần nói rõ phải dùng bản PDF hoặc DOCX gốc |
| Nhóm AI đang làm worker theo `is_scanned` hoặc OCR | Low | Low. Worker chưa có code (`worker/src` trống) |

## Applied Changes

- PRD v1.2, SRS v1.2: các mục ở trên; mục sửa ở trạng thái `REVIEW`, mục bỏ ở `DEPRECATED` kèm ghi chú. Không mục nào được đặt `APPROVED`.
- `contracts/`: bỏ `jpg`, `png` khỏi `file_type` và `declared_file_type`; bỏ `document.is_scanned` (schema DRAFT, chưa tăng version).
- Backend: `FileType` chỉ còn `Pdf`, `Docx`; `FileTypeDetector` bỏ chữ ký và đuôi JPG, PNG; thông báo lỗi; `ExtractedDocumentInfo` bỏ `IsScanned`. DB local chỉ có bản ghi `pdf` nên không cần migration.
- Frontend: `upload.ts`, thông báo ERR-UPL-001, dropzone, trang giới thiệu, bỏ viewer ảnh; `api-types.ts` sửa tay theo enum mới.
- Test: unit 79/79, integration 110/110, frontend typecheck và test liên quan pass (2026-11-17).

## Còn chờ

- Approver duyệt các mục `REVIEW`.
- Nhóm AI và IA duyệt thay đổi `contracts/` theo `contracts/README.md`.
