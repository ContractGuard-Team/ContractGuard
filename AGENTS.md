# ContractGuard: hướng dẫn cho AI agent

File chung cho mọi agent và thành viên, không riêng Claude. Công cụ cần file tên khác thì trỏ về đây.

Web app AI thẩm định rủi ro hợp đồng. Nhóm thực tập: 1 SE (C#/.NET + React), 3 AI (Python), 1 IA (bảo mật). Demo đầu 12/2026. Trả lời bằng tiếng Việt.

## Trạng thái (cập nhật khi đổi giai đoạn)

- PRD v1.0 và SRS v1.0 đã **baseline** (Gate S5, 2026-10-27). Sửa spec chỉ qua change request (`RDK/Agent/03-SRS/06-Change Request Workflow.md`).
- Đang ở: code. System design bước 2–8 có đề xuất trong `docs/04-system-design-decisions.md` (dòng Quyết định trống = đồng ý).
- Frontend: React Router framework mode, `ssr: false`, build ra file tĩnh (DEC-GEN-027).
- Phase 1 và phần SE không cần AI của Phase 2–6 đã merge (PR #9); phần cần AI dừng ở gửi/nhận, xem `docs/06-ai-integration-points.md`. Landing mới ở `/`, thư viện ở `/contracts`.
- Thứ tự tiếp theo (2026-11-17): dọn tồn đọng → Google login (F-AUTH-002, FR-AUTH-011: gộp tài khoản theo email) → Phase 7 trên AWS, database dùng Supabase (cần phiếu đổi SD trong `docs/04` trước).

## Đọc gì, theo thứ tự

1. `docs/spec/00-Project-State.md`: quyết định (DEC-*), vị trí hiện tại. Đọc trước.
2. `docs/03-implementation-phases.md`: phase và điểm cần chốt (§4).
3. `docs/02-system-design.md`: ràng buộc, ước lượng, quyết định đã chốt. `docs/04-system-design-decisions.md`: phiếu quyết định bước 2–8.
4. `docs/architecture/*.html`: sơ đồ tổng quan, luồng phân tích.
5. `docs/DESIGN.md`: hệ thống giao diện (token, font, component, quy tắc). Đọc trước khi làm UI; `docs/PRODUCT.md` là định hướng sản phẩm.
6. `docs/06-ai-integration-points.md`: chỗ nối với nhóm AI (`AI-HOOKUP` trong code), đề xuất khối `scoring`, câu hỏi chờ trả lời. Đọc trước khi đụng phân tích hoặc chat.
7. `docs/05-ai-diagram-vs-system-design.md`: chia việc SE và AI, chỗ lệch với system design, tóm tắt repo nhóm AI (§9). Đọc file này thay vì rà `AI/` (bản clone local, Hermes khoảng 22.000 file).

**Không đọc cả `docs/spec/03-SRS.md` (6000+ dòng) hay `02-PRD.md`.** Tra theo ID:
- `grep -n "FR-XXX-001" docs/spec/03-SRS.md`, rồi `Read` với offset và limit.
- Tìm yêu cầu của một feature: RTM ở SRS §16.
- Mục lục SRS ở đầu file (`RDK/Context/SRS-TOC.md` là mẫu).
- `docs/01-product-spec.md`, `Idea/`: nguồn cũ, chỉ tra khi spec dẫn tới.

## Quy tắc

- **System design:** agent viết đề xuất kèm phương án và trade-off vào `docs/04-system-design-decisions.md`; Approver đọc và quyết định (dòng Quyết định để trống = đồng ý). Chỉ ghi vào `02-system-design.md` sau khi đã chốt.
- **Không bao giờ tự đặt `APPROVED`.** Chỉ Approver (SE của nhóm) quyết định.
- **Không sửa** `RDK/` (Requirements Documentation Kit, cài ở máy local bằng `npx req-doc-kit`, không có trên GitHub). Quy tắc làm spec nằm ở `RDK/Agent/AGENTS.md`.
- AI chạy qua Amazon Bedrock; model chưa chốt, do eval quyết định (DEC-GEN-010, DEC-GEN-016).
- Service C# là nơi duy nhất ghi DB; worker Python không chạm DB; `/contracts` chứa JSON Schema dùng chung.

## Kiểm tra spec

Script kiểm tra (`validate.py`, `check_spec.py`, `regen.py`...) chưa vào repo.

## Repo nhóm

- `ContractGuard-Team/ContractGuard`; làm việc qua nhánh và pull request, không đẩy thẳng `main` (xem `CONTRIBUTING.md`).
- Thư mục theo nhóm: `backend/`, `frontend/` (Dev/SE), `worker/` (AI), `security/` (IA), `contracts/` (chung, đổi schema cần cả ba bên duyệt).
- Không commit `.env`, khóa, hợp đồng thật.

## Dispatch (Claude Code = orchestrator)
Call other agent CLIs directly via Bash (use run_in_background to run several in parallel):
`uv run .claude/delegate.py <agy|codex|kiro|copilot> "<brief>" [--write] [--model <id>]`
It prints only the answer and is read-only unless `--write`. Use it instead of wrapper subagents (~11k tokens of overhead each).

| Agent | CLI | Good for |
|---|---|---|
| `agy` | `agy -p` | architecture review, planning. Name the exact files in the brief: it cannot explore directories headless (shell/MCP calls are denied). On Gemini quota errors the script retries once on `claude-sonnet-4-6` |
| `codex` | `codex exec` | diff review, code |
| `kiro` | `kiro-cli chat --no-interactive` | codebase reading, specs, long tasks |
| `copilot` | `copilot -p -s` | quick lookups, explanations, small tests (`--write` unsupported) |

Rules:
- Small tasks: do them directly. Delegate heavy read/loop work or when an independent opinion is wanted.
- Brief = goal, relevant files, definition of done, "reply in <=15 lines".
- Only one worker may write files at a time, only if the brief says so; then check `git diff`.
- On quota/login error, report it and switch to another worker.
