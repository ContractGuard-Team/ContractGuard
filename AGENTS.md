# ContractGuard: hướng dẫn cho AI agent

File chung cho mọi agent và thành viên, không riêng Claude. Công cụ cần file tên khác thì trỏ về đây.

Web app AI thẩm định rủi ro hợp đồng. Nhóm thực tập: 1 SE (C#/.NET + React), 3 AI (Python), 1 IA (bảo mật). Demo đầu 12/2026. Trả lời bằng tiếng Việt.

## Trạng thái (cập nhật khi đổi giai đoạn)

- PRD v1.0 và SRS v1.0 đã **baseline** (Gate S5, 2026-10-27). Sửa spec chỉ qua change request (`RDK/Agent/03-SRS/06-Change Request Workflow.md`).
- Đang ở: code. System design bước 2–8 có đề xuất trong `docs/04-system-design-decisions.md` (dòng Quyết định trống = đồng ý).
- Frontend: React Router framework mode, `ssr: false`, build ra file tĩnh (DEC-GEN-027).
- Phase 1 và phần SE không cần AI của Phase 2–6 đã merge (PR #9); phần cần AI dừng ở gửi/nhận, xem `docs/06-ai-integration-points.md`. Landing mới ở `/`, thư viện ở `/contracts`.
- 2026-11-17: dọn tồn đọng xong; Google login (F-AUTH-002) đã code, chờ review, phiếu P6-01 trong `docs/04`. Tiếp theo: Phase 7 trên AWS, database dùng Supabase (cần phiếu đổi SD trong `docs/04` trước).
- 2026-11-18: Supabase staging đã migrate, bảng app ở schema `app`, dùng chung DB với kho pháp luật của AI (`docs/04` C4). Staging/production và email SES gửi từ Gmail: `docs/04` C5.
- 2026-11-21: staging chạy trên Railway (`https://contractguardweb.up.railway.app`), dùng S3/SQS dev; production trên AWS (`infra/cdk/`) chỉ bật ~3 ngày quanh showcase, cần domain trước (`docs/04` C7).
- 2026-11-23: staging AWS (demo) chạy bằng CDK, stack `ContractGuard-StagingAws` (`docs/04` C9): không domain (`*.cloudfront.net`), SES sandbox, dùng lại S3/SQS bộ dev và Supabase staging, khóa phiên trong DB; khi nó chạy phải tắt API trên Railway. **Không gọi là production**: production thật (domain, SES production access, DB riêng) chưa làm. Checklist ở `infra/cdk/README.md`.
- 2026-11-21: chatbot qua worker (`docs/04` C6): handler phía AI viết trên nhánh `feat/chatbotAI` của repo AI, chờ nhóm AI review. Tiến độ chi tiết đọc từ DynamoDB của worker (C8). `extraction.v2` bỏ 4 trường thừa (`docs/07` §13).

## Đọc gì, theo thứ tự

1. `docs/spec/00-Project-State.md`: quyết định (DEC-*), vị trí hiện tại. Đọc trước.
2. `docs/03-implementation-phases.md`: phase và điểm cần chốt (§4).
3. `docs/02-system-design.md`: ràng buộc, ước lượng, quyết định đã chốt. `docs/04-system-design-decisions.md`: phiếu quyết định bước 2–8.
4. `docs/architecture/contractguard-architecture.drawio`: sơ đồ kiến trúc AWS và software stack (nguồn chính duy nhất; mở bằng draw.io).
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
- Model AI do nhóm AI tự host bằng Docker trên EC2, không dùng Bedrock (DEC-GEN-029, CR-GEN-003); model cụ thể do eval quyết định (DEC-GEN-010). Nền tảng chạy worker do nhóm AI chọn.
- Database trên AWS là Supabase (PostgreSQL + pgvector), một project ở Tokyo dùng chung cho staging Railway và staging AWS (`docs/04` C9); local vẫn dùng Postgres trong Docker (`docs/04` mục C1, C5).
- Service C# là nơi duy nhất ghi dữ liệu nghiệp vụ (schema `app`); data pipeline AI chỉ ghi kho pháp luật (`public.legal_*`) qua PostgREST (`docs/04` C4); worker Python không chạm DB; `/contracts` chứa JSON Schema dùng chung.

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
