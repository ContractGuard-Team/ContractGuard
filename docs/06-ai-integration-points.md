# ContractGuard: điểm nối với nhóm AI

| Mục | Giá trị |
|---|---|
| Ngày | 2026-11-10, sửa 2026-11-22 |
| Người viết | SE (code bởi Claude, chờ review) |
| Trạng thái | SE đã làm xong phần không cần AI của Phase 2–6. Worker AI đã chạy trên EC2 và nối vào bộ dev (`docs/04` C7, `docs/05` §9); handler chat nằm trên nhánh `feat/chatbotAI` của repo AI, chờ nhóm AI review. Các điểm ở §3 **chờ AI + IA + SE thống nhất** |
| Tìm trong code | `grep -rn "AI-HOOKUP" backend/` cho ra đúng các chỗ ở §2 |

SE dừng ở phần **gửi và nhận**: backend gửi việc, nhận kết quả, kiểm tra, lưu và hiển thị. Phần cần model do worker AI làm. Mọi chức năng không cần AI đều đã chạy thật, có test và e2e.

Hiện có hai thứ giả lập để test, cả hai đều **không phải AI** và đã được ghi rõ:

| Giả lập | Dùng để | Bật bằng | Không được dùng khi |
|---|---|---|---|
| `contracts/examples/extraction.v2.example.json` và `contracts/examples/sample-contract.pdf` | Kết quả mẫu khớp với một PDF tổng hợp. Dùng cho seed, integration test, e2e và `scripts/dev/send-analysis-result.*` | Có sẵn | Không áp dụng |
| `FakeChatResponder` | Câu trả lời cố định, có ghi "[Giả lập, chưa có AI]", trích điều khoản trùng nhiều từ nhất với câu hỏi | `Chat__Responder=fake` (compose: `CHAT_RESPONDER=fake`) | Production (API từ chối khởi động), buổi demo |

## 1. Việc nhóm AI cần làm để nối vào

Tên bucket, queue, quyền IAM trên AWS thật và cách chạy worker trên EC2: `docs/07-worker-aws-integration.md`.

| # | Việc | Nối vào đâu | Backend đã có |
|---|---|---|---|
| 1 | Worker đọc `analysis-requests`, ghi `extraction.v2` lên S3, gửi `progress`/`completed`/`failed` lên `analysis-results` | `contracts/queue-messages.v1.schema.json`, `contracts/analysis-request.v1.schema.json`, `contracts/extraction.v2.schema.json` | Gửi job (`AnalysisStarter`), nhận và kiểm tra (`AnalysisResultHandler`, `ExtractionConsistencyValidator`), lưu (`AnalysisResultProcessor`) |
| 2 | Gửi khối `scoring` (mới, xem §3.1) | `extraction.v2.schema.json` → `scoring` | Kiểm tra luật điểm, lưu, hiển thị thước rủi ro, lọc và sắp xếp thư viện theo điểm, đưa vào báo cáo |
| 3 | Chatbot: worker trả lời qua SQS (`docs/04` C6, đổi SD-14 A) | Đọc `chat-requests`, trả `chat_answered` (`docs/07` §10, `contracts/chat-request.v1.schema.json`) | Endpoint SSE, lưu lịch sử chung, lọc trích dẫn sai, UI stream và nhảy tới điều khoản; `WorkerChatResponder` gửi câu hỏi và chờ câu trả lời |
| 4 | Worker xóa bản trong `quarantine/` sau khi ghi `clean/` | SD-07 | Khi xóa hợp đồng, backend chỉ biết key mà version đang trỏ tới, cùng các key `clean/` và bản chép cho lần chạy lại tính được (`ContractPurger.PurgeAsync`, khi xóa vĩnh viễn từ thùng rác hoặc sau 30 ngày) |

Không còn việc embedding: từ 2026-11-21 `extraction.v2` bỏ trường `embeddings` vì chat đi qua worker (`docs/07` §13). Worker gửi trường này thì backend từ chối cả kết quả.

## 2. Các chỗ `AI-HOOKUP` trong code

| File | Nội dung |
|---|---|
| `backend/src/ContractGuard.Application/Analysis/Extraction/ExtractionDocument.cs` | `ExtractedScoring`: khối điểm do SE đề xuất, đang để tùy chọn |
| `backend/src/ContractGuard.Application/Chat/IChatResponder.cs` | Port của chatbot: request mang `AnalysisRunId`, câu hỏi, 10 lượt gần nhất, ngôn ngữ; trả về stream gồm chữ và trích dẫn |
| `backend/src/ContractGuard.Application/Chat/WorkerChatResponder.cs` | Responder thật: gửi phiếu câu hỏi cho worker qua SQS và chờ `chat_answered` |
| `backend/src/ContractGuard.Infrastructure/DependencyInjection.cs` | Chỗ đăng ký responder (`Chat:Responder` = `worker` hoặc `fake`) |
| `backend/src/ContractGuard.Application/Contracts/ContractCommands.cs` | Xóa file `quarantine/` (việc #4) |
| `backend/src/ContractGuard.Domain/Analysis/AnalysisRun.cs` | `Cancel`: người dùng dừng phân tích (CR-ANA-001); `ContractCommands.CancelAnalysisAsync` gửi `cancel_requested` cho worker (§3.5, `docs/07` §15) |

## 3. Cần thống nhất (AI + IA + SE)

### 3.1 Khối `scoring` trong `extraction.v2` (SE đề xuất)

`docs/05` §4 đã chốt mức B: model chọn severity, còn code Python trong worker tính điểm. Schema chưa có chỗ chứa điểm, nên SE thêm khối tùy chọn sau:

```json
"scoring": {
  "rule_version": "rules-2026-11",
  "overall_score": 72,
  "risk_level": "high",
  "domains": [
    { "domain": "cybersecurity", "applicable": true, "score": 88, "risk_level": "critical", "na_reason": null },
    { "domain": "compliance", "applicable": false, "score": null, "risk_level": null, "na_reason": "…", "re_enabled": false }
  ]
}
```

Backend từ chối **cả kết quả** (job `failed`, không lưu gì, theo VAL-DATA-013) nếu vi phạm một trong các luật sau:

| Luật | Nguồn |
|---|---|
| Có đúng 7 nhóm, mỗi nhóm một lần | REL-DATA-014 |
| Nhóm áp dụng phải có điểm và mức; nhóm N/A không có điểm và phải có lý do | FR-RISK-019, FR-VIEW-003 |
| Mức khớp ngưỡng 0–29 / 30–54 / 55–84 / 85–100, cho điểm tổng và từng nhóm | BR-RISK-010 |
| Có finding Critical thì điểm tổng phải ≥ 55 | FR-RISK-022 |
| Finding không được thuộc nhóm N/A | BR-RISK-003 |
| Finding sinh từ check thì nhóm chính theo tiền tố mã check (CY → cybersecurity, PR → data_privacy, TP → third_party) | BR-RISK-002 |

`trace.rule_version` cũ đã chuyển sang `scoring.rule_version`, vì người tính điểm ghi phiên bản rule (`docs/05` §4 điểm 4).

**Câu hỏi mở:**

1. ~~Khi ngưỡng được hiệu chỉnh bằng eval, ngưỡng nằm ở đâu?~~ **Đã chốt 2026-11-23 (`docs/04` P7-01):** worker tính cả điểm lẫn mức theo policy có phiên bản (`scoring.rule_version`); backend lưu đúng mức worker ghi, không suy lại theo ngưỡng SRS. Scoring hỏng cấu trúc thì backend bỏ khối scoring, vẫn lưu phần còn lại.
2. Khi nào `scoring` thành bắt buộc? Đề xuất: ngay khi worker đầu tiên chạy được.
3. Có giữ `re_enabled` (BR-RISK-004) không, hay worker tự bật lại nhóm rồi chỉ báo `applicable: true`?

### 3.2 Chatbot (`IChatResponder`)

- **Từ 2026-11-20 (`docs/04` C6):** worker AI trả lời qua SQS. `WorkerChatResponder` ghi phiếu `chat-request.v1` (câu hỏi, 10 lượt gần nhất, điều khoản và finding của run) rồi gửi `chat_requested` lên queue `chat-requests`. Worker trả `chat_answered` về `analysis-results`. Bật bằng `Chat:Responder=worker` (môi trường `Cloud`). Việc của worker: `docs/07` §10.
- **Phạm vi:** request chỉ mang id của một lần phân tích mà người hỏi được xem. Adapter **chỉ được đọc dữ liệu của `AnalysisRunId`** (BR-CHAT-001, FR-CHAT-006).
- **Trích dẫn:** trả `ChatChunk.Cite(clauseKey)`. Backend bỏ trích dẫn tới điều khoản không thuộc lần phân tích đó, giống cách kiểm tra extraction.
- **Lỗi giữa chừng:** người dùng nhận event `error` (ERR-CHAT-001). Câu hỏi vẫn được lưu, câu trả lời dở thì không.
- **Câu mở:**
  - Ai viết câu "hợp đồng không đề cập" (FR-CHAT-005) và câu "chỉ giải thích, không viết lại" (FR-CHAT-007): prompt của nhóm AI hay một luật cứng?
  - Model im lặng hơn 60 giây có làm ALB cắt kết nối không? Có `shortcut:` ở `ChatEndpoints.cs` về tín hiệu giữ kết nối.

### 3.3 Ngôn ngữ báo cáo

FR-EXP-005 cho người dùng chọn tiếng Việt hoặc tiếng Anh. Backend dịch phần chữ cố định của báo cáo. Nội dung finding do worker viết theo `output_language` lúc phân tích. Muốn báo cáo dịch luôn finding thì cần một trong hai cách:

- worker trả finding bằng cả hai ngôn ngữ, hoặc
- có thêm một bước dịch.

Chờ nhóm AI chọn.

**Từ 2026-11-23 (CR-AUTH-001), `output_language = "en"` có thể tới worker thật.** Người dùng chọn "Ngôn ngữ kết quả phân tích và trợ lý AI" ở trang hồ sơ; backend gửi lựa chọn đó trong `output_language` của `analysis-request.v1` và `chat-request.v1`, schema không đổi. Trước đây giá trị luôn là `vi`, nên chưa ai thử `en`. Nhóm AI nên thử một lần phân tích và một câu chat bằng `en`.

### 3.4 Câu hỏi gợi ý (F-CHAT-002)

Chưa làm vì cần model. Gợi ý: thêm `suggested_questions` vào `extraction.v2` để worker sinh một lần lúc phân tích. Khi đó backend chỉ cần lưu và hiển thị.

### 3.5 Dừng phân tích (CR-ANA-001, 2026-11-23; nối với worker 2026-11-24)

Người dùng dừng được một lần đang chạy (`POST /api/v1/jobs/{jobId}/cancel`). Backend đánh lần đó `cancelled`, bỏ qua mọi `progress`/`completed`/`failed` đến sau của job đó, rồi gửi `cancel_requested` lên queue riêng `analysis-cancels`. Worker dừng job (hoặc bỏ qua khi job còn trong queue) và trả `failed` + `ERR-AI-003`. Chi tiết, mã lỗi, quyền: `docs/07` §15.

**Trạng thái (2026-11-24):** nhóm AI viết luồng hủy và chọn queue SQS riêng (repo AI `13785369`, `docs/partner_cancel_and_failure.md`); backend đã nối. Queue AWS dev đã tạo (2026-11-24). Còn chờ: nhóm AI cấp quyền và đặt `WORKER_CANCEL_QUEUE_URL`, rồi hai bên chạy nghiệm thu.

Gửi lệnh hủy là best-effort: nếu SQS lỗi, run vẫn `cancelled` và worker chạy nốt job như trước. Vì vậy backend vẫn giữ bản chép riêng `quarantine/{owner}/{jobId}{ext}` cho lần chạy lại sau một lần bị dừng.

Lần gọi model đang chạy dở vẫn tính token, vì không ngắt được một request HTTP đã gửi đi.

## 4. Phần SE đã làm (để đối chiếu)

| Phase | Đã chạy thật | Phụ thuộc AI |
|---|---|---|
| 2 | Lưu finding, 20 check, điểm, metadata, cảnh báo injection, truy vết; sửa metadata | Nội dung finding, check, điểm, metadata |
| 3 | Tổng quan, tờ hợp đồng có ghi chú lề, tô vùng trên PDF, checklist, xuất PDF/DOCX | Không |
| 4 | Lịch sử chat chung, SSE, trích dẫn | Câu trả lời |
| 5 | Tổ chức, lời mời, vai trò, chia sẻ, thư viện, duyệt, phiên bản, xóa vĩnh viễn, nhật ký, rate limit, giới hạn email | Không |
| 6 | Trạng thái xử lý finding, tùy chọn báo cáo, lịch sử chat | Câu hỏi gợi ý (§3.4) |

Ngày 2026-11-10 tạm bỏ Google login (F-AUTH-002), Phase 7 (AWS), Phase 8. Ngày 2026-11-17 mở lại Google login (đã code cùng ngày, không cần AI) và Phase 7 (database dùng Supabase); Phase 8 vẫn bỏ.
