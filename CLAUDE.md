# ContractGuard

Web app AI thẩm định rủi ro hợp đồng. Nhóm thực tập: 1 SE (C#/.NET + React), 3 AI (Python), 1 IA (bảo mật). Demo đầu 12/2026. Trả lời bằng tiếng Việt.

## Trạng thái (cập nhật khi đổi giai đoạn)

- PRD v1.0 và SRS v1.0 đã **baseline** (Gate S5, 2026-10-27). Sửa spec chỉ qua change request (`RDK/Agent/03-SRS/06-Change Request Workflow.md`).
- Đang ở: system design (`docs/02-system-design.md`, mới xong bước 1) rồi code theo `docs/03-implementation-phases.md`.
- Frontend: React Router framework mode, `ssr: false`, build ra file tĩnh (DEC-GEN-027).
- Chưa có code backend. `frontend/` là prototype dữ liệu giả (xem `frontend/DEMO-CLEANUP.md`).

## Đọc gì, theo thứ tự

1. `docs/spec/00-Project-State.md`: quyết định (DEC-*), vị trí hiện tại. Đọc trước.
2. `docs/03-implementation-phases.md`: phase và điểm cần chốt (§4).
3. `docs/02-system-design.md`: ràng buộc, ước lượng.
4. `docs/architecture/*.html`: sơ đồ tổng quan, luồng phân tích.
5. `frontend/design.md`: style giao diện (token, font, component). Đọc trước khi làm UI.

**Không đọc cả `docs/spec/03-SRS.md` (6000+ dòng) hay `02-PRD.md`.** Tra theo ID:
- `grep -n "FR-XXX-001" docs/spec/03-SRS.md`, rồi `Read` với offset và limit.
- Tìm yêu cầu của một feature: RTM ở SRS §16.
- Mục lục SRS ở đầu file (`RDK/Context/SRS-TOC.md` là mẫu).
- `docs/01-product-spec.md`, `Idea/`: nguồn cũ, chỉ tra khi spec dẫn tới.

## Quy tắc

- **System design: coach, đừng viết hộ.** Người dùng tự nháp từng bước; Claude giải thích khái niệm, đưa câu hỏi, review, rồi mới ghi kết quả đã chốt.
- **Không bao giờ tự đặt `APPROVED`.** Chỉ Approver (người dùng) quyết định.
- **Không sửa** `RDK/` (Requirements Documentation Kit, cài bằng `npx req-doc-kit`, bị gitignore). Quy tắc làm spec nằm ở `RDK/Agent/AGENTS.md`.
- AI chạy qua Amazon Bedrock; model chưa chốt, do eval quyết định (DEC-GEN-010, DEC-GEN-016).
- Service C# là nơi duy nhất ghi DB; worker Python không chạm DB; `/contracts` chứa JSON Schema dùng chung.

## Kiểm tra spec

Script kiểm tra (`validate.py`, `check_spec.py`, `regen.py`...) hiện nằm ở thư mục tạm của phiên, chưa vào repo.

## Repo nhóm

- `ContractGuard-Team/ContractGuard`; làm việc qua nhánh và pull request, không đẩy thẳng `main` (xem `CONTRIBUTING.md`).
- Thư mục theo nhóm: `backend/`, `frontend/` (Dev/SE), `worker/` (AI), `security/` (IA), `contracts/` (chung, đổi schema cần cả ba bên duyệt).
- Không commit `.env`, khóa, hợp đồng thật.
