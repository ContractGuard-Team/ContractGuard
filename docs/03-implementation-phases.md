# ContractGuard — Các phase code

| | |
|---|---|
| Phiên bản | 0.1 |
| Ngày | 2026-10-27 |
| Đầu vào | [PRD v1.0](spec/02-PRD.md), [SRS v1.0](spec/03-SRS.md) (baseline 2026-10-27, DEC-GEN-026); [System design](02-system-design.md); `Idea/Tech stack and flow.pdf` |
| Sơ đồ | [Tổng quan hệ thống](architecture/system-overview.html), [Luồng phân tích một hợp đồng](architecture/analysis-flow.html) |

## 0. Cách đọc

- **Không gắn ngày.** Hai mốc thời gian trong spec chưa khớp nhau:
  - PRD OQ-GEN-003 ghi demo đầu tháng 12/2026.
  - CON-PRJ-005 ghi "hơn 4 tháng".

  Khi chốt lịch, nhóm gắn tuần cho từng phase rồi sửa spec bằng change request.
- **Thứ tự theo luồng demo.** Làm thông luồng demo trước (SC-GEN-005: upload → phân tích → xem → chat → xuất báo cáo), sau đó mới mở rộng phần quản lý. Đây là cách xử lý cảnh báo RS3 (khối lượng lớn so với 1 SE).
- **Ba track làm song song** trong mỗi phase:
  - **SE:** service C#, React, hạ tầng.
  - **AI:** worker Python, prompt, eval.
  - **IA:** bảo mật.
- **Tra yêu cầu chi tiết.** Mỗi phase ghi ID feature của PRD. Muốn xem FR, NFR, SEC chi tiết thì tra RTM ở SRS §16 theo cột Feature.
- **Xong phase** nghĩa là mọi mục "Tiêu chí xong" đều kiểm chứng được trên máy dev hoặc staging. Tiêu chí nghiệm thu Given/when/then của các FR liên quan là test case.
- **Phụ thuộc system design.** Mỗi phase ghi bước system design (SD, theo [02-system-design.md](02-system-design.md) §0) phải chốt trước khi code phần đó.

## 1. Tổng quan

| Phase | Tên | Feature | Ưu tiên | Chốt SD trước |
|---|---|---|---|---|
| 0 | Nền móng | - | Must | Bước 2, 3, 4 |
| 1 | Lát cắt dọc luồng demo | F-AUTH-001 (lõi), F-ORG-002 (không gian cá nhân), F-CTR-001, F-ANA-001, F-ANA-002 | Must | Bước 5, 7 (phần upload) |
| 2 | Lõi phân tích rủi ro | F-RISK-001, F-RISK-002, F-RISK-003, F-CTR-002, F-SEC-001 | Must | Bước 5 |
| 3 | Xem kết quả và báo cáo | F-VIEW-001, F-VIEW-002, F-EXP-001 | Must | - |
| 4 | Chatbot | F-CHAT-001 | Must | Bước 6 |
| 5 | Quản lý và cộng tác | F-ORG-001, F-ORG-002 (nhiều tổ chức), F-SHARE-001, F-CTR-003, F-CTR-004, F-CTR-005, F-CTR-006, F-AUD-001, phần còn lại của F-AUTH-001 | Must (F-AUD-001 là Should) | Bước 7 |
| 6 | Các feature Should | F-AUTH-002, F-VIEW-003, F-CHAT-002, F-CHAT-003, F-EXP-002 | Should | - |
| 7 | Triển khai AWS và chuẩn bị demo | - | Must | Bước 8 |
| 8 | Nếu còn thời gian | F-CMP-001, F-POL-001 | Could | - |

**Đường cắt phạm vi khi thiếu thời gian:**
1. Bỏ phase 8 trước.
2. Sau đó bỏ phase 6.

Phase 0–5 và 7 là phần tối thiểu để đạt SC-GEN-005 (luồng demo) và SC-GEN-006 (chức năng quản lý). Muốn hạ ưu tiên một mục Must thì Approver ra quyết định và ghi vào spec bằng change request.

## 2. Chi tiết từng phase

### Phase 0 — Nền móng

**Mục tiêu:** cả nhóm code được trên cùng một cấu trúc repo và môi trường local. Ranh giới giữa C# và Python được chốt bằng schema.

| Track | Việc |
|---|---|
| SE | Chuyển `/frontend` sang React Router framework mode (`ssr: false`, thêm `@react-router/dev` 8.5.0; DEC-GEN-027), giữ nguyên các trang prototype. Repo nhóm đã có khung thư mục:  `/frontend` (prototype đã có), `/backend` (.NET 10), `/worker` (Python 3.12), `/contracts` (JSON Schema dùng chung), `/infra` (CDK), `/eval` |
| SE | Docker Compose: PostgreSQL + pgvector, giả lập S3 và SQS. Bedrock gọi bản thật (SD §2.7) |
| SE | Migration đầu tiên theo SRS §9 (bảng, khóa, `organization_id` cho phép rỗng ở `contract`) |
| SE | CI trên GitHub Actions: build, test, lint cho cả ba phần |
| SE, AI | `extraction.json` v0 trong `/contracts`, có `schema_version`; hai bên cùng validate theo file này |
| AI | Bộ eval 20 hợp đồng có nhãn (DEP-GEN-003); đếm token tiếng Việt trên 3–5 hợp đồng (SD §2.9) |
| AI | Kiểm tra quyền dùng model trên Bedrock ở `ap-southeast-1`, credit FCAJ có áp dụng không, và xin tăng quota **sớm** |
| IA | Threat model cho upload, prompt injection, cách ly không gian làm việc; checklist bảo mật cho code review |

**Tiêu chí xong:**
- Chạy `docker compose up` thì API và worker trao đổi được một message qua SQS giả lập.
- CI chạy xanh.
- Có schema `extraction.json` v0 và một file mẫu hợp lệ.
- Gọi Bedrock thành công từ worker.

### Phase 1 — Lát cắt dọc luồng demo

**Mục tiêu:** một hợp đồng đi được trọn đường, từ upload đến danh sách điều khoản trên UI. Chưa cần đánh giá rủi ro.

| Track | Việc |
|---|---|
| SE | Đăng ký, xác thực email, đăng nhập, đăng xuất, quên mật khẩu bằng email (phần lõi của F-AUTH-001) |
| SE | Không gian cá nhân có sẵn khi đăng ký (FR-ORG-013). Mọi truy vấn lọc theo không gian ngay từ đầu (BR-DATA-001) |
| SE | Upload qua presigned URL thẳng lên S3, khai báo ngữ cảnh, tạo job (F-CTR-001) |
| SE | `BackgroundService` đọc `analysis-results`, validate theo schema, lưu dữ liệu; job sai schema bị từ chối và ghi log theo `job_id` |
| SE | UI: nối các trang Login, Library, NewContract với API thật; hiện tiến trình bằng cách gọi `GET /jobs/{id}` định kỳ (F-ANA-001) |
| AI | Worker đọc `analysis-requests`, parse PDF/DOCX bằng Docling và bản scan bằng Textract, tách điều khoản, giữ vị trí trên trang, phân loại vào 21 loại cộng "Khác" (F-ANA-002) |
| AI | Retry và DLQ: job quá số lần thử thì chuyển `failed` kèm lý do |
| IA | Kiểm tra file ở tầng upload: loại file, kích thước, số trang (giới hạn theo SRS) |

**Tiêu chí xong:**
- Đăng ký tài khoản mới, upload hợp đồng mẫu Cloud Service Agreement, rồi thấy danh sách điều khoản đã phân loại trên UI.
- Job lỗi hiện thông báo thất bại.
- Người dùng A không thấy hợp đồng của người dùng B.

### Phase 2 — Lõi phân tích rủi ro

**Mục tiêu:** có finding, kết quả check và điểm rủi ro đúng theo business rule ở SRS §7. Đo được chất lượng.

| Track | Việc |
|---|---|
| AI | Đánh giá 7 nhóm rủi ro chạy song song, sinh finding theo cấu trúc chuẩn, phát hiện điều khoản thiếu, nhóm N/A (F-RISK-001) |
| AI | 20 check Cyber/Privacy/Third-party với trạng thái PASS/PARTIAL/MISSING/N/A (F-RISK-002) |
| AI | Trích metadata: các bên, giá trị, ngày, luật áp dụng (F-CTR-002) |
| AI | Chạy eval hằng tuần, theo dõi recall, precision, chi phí và độ trễ (SC-GEN-001, SC-GEN-002) |
| AI, IA | Code Python trong worker tính điểm 0–100 theo nhóm và theo cả hợp đồng từ cấu hình có version, có điều chỉnh theo ngữ cảnh; IA viết nội dung rule và trọng số (F-RISK-003, SD-10) |
| SE | Kiểm tra giới hạn của điểm và mức rủi ro, lưu và hiển thị |
| SE | Lưu truy vết của từng finding: clause ID, vị trí, `schema_version`, phiên bản prompt, model, phiên bản rule |
| SE | Người dùng sửa được metadata trên UI |
| IA | Quét mã độc, làm sạch tài liệu, phát hiện và cảnh báo prompt injection (F-SEC-001) |

**Tiêu chí xong:**
- Hợp đồng mẫu có finding, kết quả check và điểm.
- Báo cáo eval tuần đầu có số recall và precision.
- File chứa câu lệnh chèn vào sinh ra cảnh báo.

### Phase 3 — Xem kết quả và báo cáo

**Mục tiêu:** người dùng đọc và dùng được kết quả. Luồng demo chạy trọn, trừ phần chat.

| Track | Việc |
|---|---|
| SE | Dashboard: tổng quan điểm, số finding theo severity, khuyến nghị tổng, danh sách và chi tiết finding (F-VIEW-001); nối trang Overview, Checklist. Finding hiện ở ghi chú lề cạnh văn bản, không đặt trong panel phải (panel chỉ có chat). Khi làm P3-02 phải chốt cách hiện ghi chú lề lúc vùng giữa không đủ rộng cho cột 244px |
| SE | Document viewer: xem và tải file gốc đã có từ Phase 1 (PDF.js qua `react-pdf`, `docx-preview`); còn tô điều khoản theo severity, liên kết hai chiều giữa điều khoản và finding (F-VIEW-002); nối trang DocumentView |
| SE | Xuất báo cáo PDF và DOCX; báo lỗi khi tạo báo cáo thất bại (F-EXP-001) |

**Tiêu chí xong:** chạy OPS-GEN-001 và OPS-GEN-005 trên hợp đồng mẫu mà không lỗi.

### Phase 4 — Chatbot

**Mục tiêu:** hỏi đáp tiếng Việt và tiếng Anh về hợp đồng đang mở, có trích dẫn. Chữ đầu tiên hiện ra trong 15 giây (p50).

| Track | Việc |
|---|---|
| AI | Embedding điều khoản và lưu vào pgvector; RAG trong phạm vi một hợp đồng; trả lời kèm trích dẫn; nói rõ khi hợp đồng không đề cập (F-CHAT-001) |
| SE | Endpoint chat có streaming, nối trang ChatPanel; bấm trích dẫn thì nhảy tới điều khoản |
| IA | Chống prompt injection qua chat; kiểm thử không lộ dữ liệu của không gian khác (NFR-SEC-003) |

**Tiêu chí xong:**
- Chạy OPS-GEN-002 trên hợp đồng mẫu.
- Hỏi về nội dung không có trong hợp đồng thì nhận câu "không đề cập".
- Đo được thời gian ra chữ đầu tiên.

**Sau phase này** luồng demo SC-GEN-005 chạy trọn trên máy dev.

### Phase 5 — Quản lý và cộng tác

**Mục tiêu:** đủ các chức năng quản lý Must (SC-GEN-006) và các lớp chống dò mật khẩu.

| Track | Việc |
|---|---|
| SE | Tạo tổ chức, mời thành viên qua email, vai trò Owner/Member, chuyển giữa các không gian (F-ORG-001, F-ORG-002); nối trang Members |
| SE | Chia sẻ theo email và thu hồi quyền (F-SHARE-001): hợp đồng của tổ chức chỉ chia sẻ cho thành viên tổ chức; hợp đồng cá nhân chia sẻ cho mọi tài khoản. Nối ShareDialog |
| SE | Thư viện có tìm kiếm, lọc, sắp xếp (F-CTR-003); luồng trạng thái duyệt (F-CTR-004) |
| SE | Phiên bản hợp đồng, mỗi phiên bản có kết quả riêng (F-CTR-005); nối trang History |
| SE | Xóa vĩnh viễn: file gốc, dữ liệu trích xuất, embedding, finding, lịch sử chat (F-CTR-006) |
| SE | Nhật ký hoạt động (F-AUD-001) |
| SE | Khóa đăng nhập theo email, rate limit theo IP, giới hạn gửi email quên mật khẩu kèm đồng hồ đếm ngược (SEC-AUTH-006, SEC-AUTH-007, FR-AUTH-014, FR-AUTH-015) |
| IA | Kiểm thử phân quyền theo ma trận SRS §12.3 và cách ly không gian làm việc |

**Tiêu chí xong:**
- Mọi user story Must của các feature quản lý đạt tiêu chí nghiệm thu.
- Test cách ly không gian làm việc chạy xanh.

### Phase 6 — Các feature Should

| Track | Việc |
|---|---|
| SE | Đăng nhập Google (F-AUTH-002). Tài khoản mới lấy tên từ Google, không có thì lấy phần trước `@`; tài khoản đã có giữ tên cũ |
| SE | Đánh dấu trạng thái xử lý finding kèm ghi chú (F-VIEW-003) |
| SE | Tùy chọn báo cáo: ngôn ngữ và phạm vi finding (F-EXP-002) |
| AI, SE | Gợi ý câu hỏi (F-CHAT-002), lịch sử chat (F-CHAT-003) |

**Tiêu chí xong:** các user story Should đạt tiêu chí nghiệm thu.

### Phase 7 — Triển khai AWS và chuẩn bị demo

| Track | Việc |
|---|---|
| SE | Dựng staging ở `ap-southeast-1` bằng CDK, deploy bằng GitHub Actions và ECR |
| SE | Chính sách bật/tắt để tiết kiệm credit (SD §2.7); quyết định worker chạy sẵn hay scale từ 0 (SD Q1) |
| Cả nhóm | Trước khi dựng AWS: mọi thành viên cấu hình billing và cảnh báo ngân sách trên tài khoản của mình (SD-20) |
| SE | OpenTelemetry gửi sang CloudWatch và X-Ray, dùng `job_id` làm correlation ID |
| SE | Đọc IP thật qua `X-Forwarded-For` sau ALB để rate limit hoạt động đúng |
| SE | Security group chỉ cho ALB gọi API, vì API tin header `X-Forwarded-Proto` từ mọi nguồn; CloudFront gọi ALB bằng HTTPS, nếu không API ở Production trả 500 (SD-03) |
| SE | ~~Dọn code demo của frontend~~ Đã xong: trang `/demo` bỏ cùng landing mới (2026-11-17) |
| SE, AI | Đo NFR hiệu năng trên staging: p50 ≤ 2 phút, p95 ≤ 5 phút với hợp đồng text 20 trang |
| AI | Chạy eval lần cuối, chốt model cho từng bước (DEC-GEN-010) |
| IA | Kiểm thử bảo mật trên staging: upload, injection, phân quyền, quét container |
| Cả nhóm | Diễn tập kịch bản demo ít nhất hai lần trên staging |

**Tiêu chí xong:** kịch bản demo chạy trọn trên staging, không lỗi, hai lần liên tiếp.

### Phase 8 — Nếu còn thời gian (Could)

- So sánh hai phiên bản, chỉ ra điểm bị yếu đi và mức thay đổi điểm rủi ro (F-CMP-001).
- Company Policy: tổ chức upload chính sách nội bộ (F-POL-001). Chỉ có ở tổ chức.

## 3. Bảng phủ feature

Bảng gồm mọi feature Must, Should, Could đang dùng trong PRD §7.1. Feature Won't nằm ngoài kế hoạch.

| Feature | Ưu tiên | Phase |
|---|---|---|
| F-AUTH-001 Tài khoản email | Must | 1 (lõi), 5 (khóa đăng nhập, rate limit) |
| F-AUTH-002 Đăng nhập Google | Should | 6 |
| F-ORG-001 Tổ chức và thành viên | Must | 5 |
| F-ORG-002 Không gian cá nhân và nhiều tổ chức | Must | 1 (không gian cá nhân), 5 (nhiều tổ chức) |
| F-SHARE-001 Chia sẻ hợp đồng | Must | 5 |
| F-CTR-001 Upload và khai báo ngữ cảnh | Must | 1 |
| F-CTR-002 Metadata tự trích xuất | Must | 2 |
| F-CTR-003 Thư viện hợp đồng | Must | 1 (danh sách), 5 (tìm, lọc, sắp xếp) |
| F-CTR-004 Trạng thái duyệt | Must | 5 |
| F-CTR-005 Phiên bản hợp đồng | Must | 5 |
| F-CTR-006 Xóa vĩnh viễn hợp đồng | Must | 5 |
| F-AUD-001 Nhật ký hoạt động | Should | 5 |
| F-ANA-001 Phân tích tự động | Must | 1 |
| F-ANA-002 Đọc và tách điều khoản | Must | 1 |
| F-RISK-001 Đánh giá rủi ro 7 nhóm | Must | 2 |
| F-RISK-002 Checklist Cyber/Privacy/Third-Party | Must | 2 |
| F-RISK-003 Chấm điểm rủi ro | Must | 2 |
| F-SEC-001 Bảo vệ đầu vào | Must | 2 |
| F-VIEW-001 Dashboard rủi ro | Must | 3 |
| F-VIEW-002 Document viewer | Must | 3 |
| F-VIEW-003 Xử lý finding | Should | 6 |
| F-CHAT-001 Chatbot theo hợp đồng | Must | 4 |
| F-CHAT-002 Gợi ý câu hỏi | Should | 6 |
| F-CHAT-003 Lịch sử chat | Should | 6 |
| F-EXP-001 Xuất báo cáo PDF và DOCX | Must | 3 |
| F-EXP-002 Tùy chọn báo cáo | Should | 6 |
| F-CMP-001 So sánh phiên bản | Could | 8 |
| F-POL-001 Company Policy | Could | 8 |

## 4. Điểm cần chốt ở system design trước khi code

Đề xuất và trade-off cho từng điểm nằm ở [04-system-design-decisions.md](04-system-design-decisions.md): D1 → SD-04, D2 → SD-16, D3 → SD-14, D4 → SD-15, D5 → SD-12, D6 → SD-08.

Đây là những chỗ file Tech stack và SRS chưa khớp nhau, hoặc chưa có quyết định. Nhóm tự chốt khi làm system design. Sơ đồ ở `docs/architecture/` đánh dấu các chỗ này là "dự kiến".

| # | Câu hỏi | Vì sao cần chốt | Trước phase | Bước SD |
|---|---|---|---|---|
| D1 | ~~Tự viết auth hay Cognito?~~ **Đã chốt: ASP.NET Core Identity** (SD-04, DEC-GEN-028). Không có MFA trong MVP (DEC-PRD-022) | - | 1 | 7 |
| D2 | Dịch vụ gửi email: Amazon SES hay dịch vụ khác? Ra khỏi sandbox thế nào? | Tài khoản SES mới ở chế độ sandbox, chỉ gửi được tới địa chỉ đã xác minh | 1 | 8 |
| D3 | Ai gọi Bedrock cho chatbot và ai đọc pgvector? | File Tech stack quy định worker không chạm database, nhưng chatbot cần tìm điều khoản trong pgvector | 4 | 6 |
| D4 | Chat streaming bằng gì: SSE, WebSocket (SignalR) hay polling? | Mục tiêu chữ đầu tiên ≤ 15 giây; file Tech stack ghi MVP chưa cần WebSocket | 4 | 6 |
| D5 | Worker chạy sẵn hay scale từ 0? | Đánh đổi giữa chi phí và độ trễ (SD Q1) | 7 | 5, 8 |
| D6 | Quét mã độc bằng gì, chạy ở đâu? | F-SEC-001; ảnh "Mẫu stack" gợi ý ClamAV | 2 | 7 |
