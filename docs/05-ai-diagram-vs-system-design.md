# Hướng đi của system design và nhóm AI: lệch ở đâu, đã chốt gì

| | |
|---|---|
| Cập nhật | 2026-11-06 |
| Người đọc | SE (Approver), nhóm AI, IA |
| Trạng thái | Đã chốt chấm điểm mức B (§4) và X1: nhóm AI chỉ làm worker, backend làm phần còn lại (§5). X2, X3 và schema chờ trả lời |
| Nguồn | `docs/02`, `docs/04`, SRS baseline, `contracts/`, `security/docs/`, diagram `Quản lý rủi ro.svg`, repo AI ở commit `39c3defb` |

> [!note] Đổi ngày 2026-11-17
> - Model AI tự host bằng Docker trên EC2, **không còn dùng Bedrock** (CR-GEN-003, DEC-GEN-029). Các chỗ nhắc Bedrock bên dưới là bối cảnh lúc viết.
> - Nền tảng chạy worker (EC2 hay ECS, §5–§6) **do nhóm AI quyết**; SE chỉ giữ ranh giới SQS, S3, không chạm DB (`docs/04` mục C2).
> - Database trên AWS là Supabase thay RDS (`docs/04` mục C1).

Hướng của nhóm AI trong file này được suy ra từ diagram, tài liệu và code của họ, **chưa được nhóm AI xác nhận**. Mục đầu tiên của cuộc họp nên là kiểm tra lại phần này.

## 1. Tóm tắt

- **Khác biệt gốc:** với SD, C# là hệ thống và Python là bộ xử lý AI. Với thiết kế của nhóm AI, Python gần như là cả hệ thống.
- **Phần lớn xung đột vẫn nằm trên kế hoạch:** các thư mục backend Python trong repo AI đều là file rỗng, nên đổi hướng lúc này không mất code đã viết.
- **Đã chốt:** chấm điểm theo mức B: model sinh finding và severity, code Python trong worker tính điểm (§4).
- **Đã chốt với nhóm AI:** không làm FastAPI và API Gateway phía Python. Nhóm AI chỉ làm worker nhận job từ SQS và trả kết quả qua SQS; backend C# làm phần còn lại (§5, X1).
- **Chờ nhóm AI trả lời:** tri thức pháp luật lưu ở đâu, triển khai thế nào (§5).
- **Việc gấp nhất không phải xung đột:** `worker/` chưa có gì, và worker của repo AI chưa đọc được SQS hay file. Phase 1 của nhóm AI không đụng điểm tranh cãi nào, nên có thể bắt đầu ngay.

## 2. Hai bên đang đi theo hướng nào

| | SD (SE) | Nhóm AI |
|---|---|---|
| **Mô hình** | C# giữ API, đăng nhập, DB, trạng thái job. Worker nhận job, trả file kết quả | Python có API riêng, tự ghi DB, tự tìm căn cứ, tự đánh giá rủi ro |
| **Đã có code** | Phase 0 đã merge. Phase 1 (auth, upload, UI) đang ở nhánh `feat/phase1-be-fe` | 3 agent chạy nối tiếp với Bedrock giả, memory, cache, Langfuse |
| **Mới là kế hoạch** | Cơ chế đưa tri thức cho worker: **chưa có cả kế hoạch** | FastAPI, PostgreSQL, Qdrant, MCP, data pipeline, worker SQS |
| **Điểm nối** | `contracts/` và queue, còn DRAFT | Code và tài liệu chưa nhắc tới `contracts/` hay tên queue |

### Repo AI đang chứa những gì

Repo AI không chỉ có phần AI. Tài liệu `docs/folder-responsibilities.md` của họ tự chia repo cho cả nhóm Backend, Data, RAG, DevOps và QA.

| Phần | Thư mục | Hiện trạng | So với repo chính |
|---|---|---|---|
| Lõi AI | `agents/`, `hermes_integration/`, `orchestration/` | **Có code**, prompt còn TODO | Đúng việc của nhóm AI |
| Quan sát AI | `monitoring/` (Langfuse) | **Có code**, phần log còn rỗng | Đúng việc của nhóm AI |
| Worker | `workers/`, `input_processing/`, `integrations/aws/s3_client.py`, `sqs_client.py` | Rỗng | Đúng việc của nhóm AI, **đang thiếu** |
| Backend Python | `apps/api/`, `database/`, `integrations/database/` | Rỗng | **Trùng `backend/` C#** |
| Tri thức | `rag/`, `data_pipeline/` | Rỗng | Đúng mảng AI, nhưng cách làm lệch SD và SRS (§5, X2) |
| Hạ tầng | `infrastructure/`, `compose*.yaml` | Rỗng | Trùng `compose.yaml`, `infra/` |

## 3. Một hợp đồng đi qua hai thiết kế

| Bước | SD | Nhóm AI | Kết quả |
|---|---|---|---|
| 1. Đăng nhập | C# Identity | FastAPI | ✅ Đã chốt: C# (X1) |
| 2. Upload | C#: presigned vào `quarantine/`, kiểm tra loại file; quét mã độc | Qua Input Validation rồi vào S3 | ✅ Đã chốt: C# (X1) |
| 3. Tạo job, gửi SQS | C# | FastAPI | ✅ Đã chốt: C# (X1) |
| 4. Đọc file, chia đoạn | Worker | Worker | ✅ Khớp |
| 5. Phân loại điều khoản | Worker | Agent 1 | ✅ Khớp |
| 6. Tìm căn cứ pháp lý | **SD chưa nói** | Qdrant, DB qua MCP, web | ❌ Mở (X2) |
| 7. Đánh giá rủi ro, gán severity | Worker | Agent 2 | ✅ Khớp |
| 8. Tính điểm | ~~C#~~ **Code Python trong worker** | Agent 2, tool `risk_calculator` | ✅ Đã chốt (§4) |
| 9. Đề xuất xử lý | Hướng xử lý bằng lời | Agent 3 viết lại điều khoản | ⚠️ Khác, xem §6 |
| 10. Trả kết quả | File trên S3, báo qua queue | Ghi thẳng vào nơi lưu | ✅ Đã chốt: báo qua SQS (X1) |
| 11. Kiểm tra rồi lưu | C# | Không có bước này | ✅ Đã chốt: C# (X1) |

## 4. Đã chốt: chấm điểm theo mức B

**Quyết định của Approver, 2026-11-06.** Nhóm AI và IA cần xác nhận nhận việc. Thay đổi schema vẫn cần cả ba bên duyệt.

| Mức | Cách làm | Kết luận |
|---|---|---|
| A | Model chỉ chọn severity, C# tính điểm | Không chọn: SE phải làm thêm rule engine |
| **B** | **Model chọn severity, code Python tính điểm theo công thức** | **Đã chọn** |
| C | Model tự đưa ra con số 0–100 | Không chọn: trái SRS, kết quả không ổn định, dễ bị prompt injection tác động |

Phân vai theo mức B:

| Ai | Làm gì |
|---|---|
| **Model (Agent 2)** | Sinh finding, gán severity, viết giải thích |
| **Code Python trong worker** | Tính điểm từng nhóm, điểm tổng, mức rủi ro theo công thức trong file cấu hình có version |
| **IA** | Viết nội dung rule và trọng số (CON-GEN-004) |
| **Nhóm AI** | Viết code tính điểm, hiệu chỉnh ngưỡng bằng eval |
| **C#** | Kiểm tra kết quả, lưu, hiển thị. Không tính điểm |

### Điều kiện bắt buộc

1. **Model không được tự đưa ra con số.** Việc tính điểm là một bước cố định trong pipeline, không phải tool để model tự chọn gọi. Nếu muốn model quyết định con số thì phải làm change request, vì trái CON-GEN-003 và BR-RISK-009.
2. **Đúng đủ luật ở BR-RISK-009, BR-RISK-010, FR-RISK-019..022:** điểm 0–100, mỗi finding chỉ tính vào nhóm chính, nhóm N/A bị loại và trọng số được chia lại, không dùng trung bình cộng, có hệ số ngữ cảnh, có Critical thì điểm tổng không dưới điểm sàn. Mỗi luật có unit test.
3. **Worker tự kiểm tra trích dẫn và loại finding sai trước khi tính điểm.** Code C# hiện từ chối **toàn bộ** kết quả nếu còn một trích dẫn sai (`AnalysisResultHandler`). Không kiểm trước thì một trích dẫn do model viết sai có thể làm hỏng cả job. Nếu C# chỉ loại riêng finding đó, điểm sẽ không còn khớp với danh sách finding. Dòng "finding có trích dẫn sai thì bị loại" ở `docs/04` vì vậy không còn đúng với C#.
4. **Worker ghi `rule_version` đã dùng** vào kết quả để truy vết (FR-ANA-016). Schema hiện ghi trường này do backend thêm, cần sửa.

### C# vẫn kiểm tra tối thiểu

Điểm trong 0–100; nhóm đang áp dụng có đủ điểm, nhóm N/A không có điểm; mức rủi ro khớp ngưỡng; luật điểm sàn Critical được giữ. Cách kiểm tra ngưỡng và điểm sàn khi các giá trị này được hiệu chỉnh bằng eval sẽ chốt lúc sửa schema.

### Được và mất

| Được | Mất, rủi ro |
|---|---|
| SE bớt rule engine ở Phase 2 | **Điểm trở thành dữ liệu không tin cậy.** Worker bị xâm nhập có thể gửi điểm giả mà vẫn hợp lệ; C# chỉ bắt được con số sai luật rõ ràng (liên quan T05) |
| Không phải giữ hai bản logic rule C# và Python (mục 5 của `CONTRACT_FOR_TEAM.md`) | Nhóm AI có thêm việc khi đang thiếu worker |
| Eval có thể chấm lại ngay trong Python | Phải sửa schema DRAFT và các tài liệu bên dưới |

**Đã kiểm tra:** SRS không yêu cầu tính lại điểm mà không chạy lại model. Khi sửa ngữ cảnh, người dùng chọn chạy lại phân tích (FR-ANA-014), nên quyết định này không vướng.

**Rủi ro không đổi so với trước:** câu chèn trong hợp đồng có thể làm model hạ severity và kéo điểm xuống, dù C# hay Python tính điểm. Tiêu chí T07 "malicious contract has no effect on score" cần IA xem lại.

### Phần việc kèm theo

| Việc | Ai | Trạng thái |
|---|---|---|
| `docs/02` SD-10, `docs/03` Phase 2, `docs/04` SD-10, `docs/architecture/data-contract.md` | SE | **Đã cập nhật** |
| `contracts/extraction.v2.schema.json`: thêm điểm, mức rủi ro, `rule_version` | AI + IA + SE | Chưa làm, cần ba bên duyệt |
| `security/docs/CONTRACT_FOR_TEAM.md` mục 4, 5 (đang ghi C# chấm điểm, biên bản chưa ký) | IA | Chưa làm |
| `security/docs/THREAT_MODEL.md` T05, T07 | IA | Chưa làm |
| Đưa rule vào worker. `security/src/rule_validator.py` là điểm khởi đầu, nhưng ghi rõ chỉ là bản tham chiếu offline và chưa tính điểm 0–100 | AI + IA | Chưa làm |
| Đổi vai trò `risk_calculator` thành bước cố định, không phải tool cho model | AI | Chưa làm |
| Backend lưu và kiểm tra điểm, đọc `rule_version` từ kết quả | SE | Làm ở Phase 2 |

## 5. Các xung đột

Thứ tự theo mức ảnh hưởng. X1 đã chốt; dòng Quyết định của X2, X3 để trống tới khi nhóm AI và IA trả lời.

### X1. Ai là trung tâm của hệ thống, ai ghi DB? (đã chốt)

- **SD:** C# là API và là nơi duy nhất ghi DB. Worker chỉ nói chuyện qua S3 và SQS.
- **Nhóm AI:** FastAPI tạo job, worker ghi kết quả vào PostgreSQL.
- **Hậu quả nếu không chốt:** hai bên cùng làm đăng nhập và quản lý job.

**Nếu Python ghi DB:**

| Rủi ro | Chuyện gì xảy ra |
|---|---|
| Lộ dữ liệu khi worker bị tấn công (nặng nhất) | Worker đọc file lạ và output của model, nên dễ bị tấn công nhất. Giữ quyền DB thì có thể chạm tới hợp đồng người khác và bảng tài khoản, tùy quyền được cấp |
| Bỏ qua bước kiểm tra | `ExtractionConsistencyValidator` chặn clause ID, số trang, trích dẫn sai trước khi lưu. Bỏ qua thì dữ liệu sai hiện lên UI, hoặc phải viết lại bằng Python |
| Lưu dở dang | C# lưu một lần, đủ hoặc không có gì (FR-ANA-005). SQS giao trùng, Spot bị thu hồi giữa chừng, Python phải tự xử lý giao dịch và chống ghi trùng |
| Hai bên sửa trạng thái job | SD-13 giao máy trạng thái cho C# |
| Hai nơi phụ thuộc cấu trúc bảng | Migration do EF Core quản lý. C# đổi cột thì Python chỉ hỏng lúc chạy, trừ khi có test chung |
| Cách ly tenant | Bộ lọc EF Core (SD-05) không áp dụng cho Python |
| Vận hành | Thêm secret DB, mở mạng tới RDS, giới hạn kết nối trên `db.t4g.micro` |

**Đề xuất của Approver:** C# làm đăng nhập, upload và là nơi duy nhất ghi DB. C# đã có đường nhận, kiểm tra và lưu kết quả, nên Python ghi DB gần như không tiết kiệm được gì. Nhóm AI bỏ `apps/`, `database/`, `integrations/database/`.

**Upload để C#** vì nhóm AI nên tập trung vào AI, và code Phase 1 đang làm ở C# (`CreateUploadAsync` tạo presigned POST vào `quarantine/`, `FileTypeDetector` kiểm tra loại file thật). Worker chỉ làm phần phải đọc nội dung file: đếm số trang và làm sạch (SD-08).

**Quyết định (đã thống nhất với nhóm AI):** không làm FastAPI và API Gateway phía Python. Nhóm AI chỉ làm worker: nhận job từ `analysis-requests`, xử lý, trả kết quả qua `analysis-results`. Backend C# làm phần còn lại, gồm API, đăng nhập, upload, trạng thái job, kiểm tra kết quả và ghi DB.

Theo SD-11, message SQS chỉ mang ID và đường dẫn, nên worker vẫn đọc `request.json`, file hợp đồng và ghi `extraction.v2` trên S3.

### X2. Tri thức pháp luật lấy từ đâu?

- **SRS:** kho tri thức nhập tay, IA xác minh số điều và hiệu lực, ghi người và ngày (FR-RISK-011, NFR-COMPL-002). Danh sách hệ thống ngoài ở §3.4 không có dịch vụ search.
- **Nhóm AI:** tự thu thập, tìm thêm trên web qua MCP, lưu vào Qdrant. Theo nét vẽ, kết quả web đi thẳng vào context của Agent 3.
- **Lỗ hổng phía SD:** worker phải gắn căn cứ vào finding nhưng không được chạm DB, và SD chưa nói worker lấy tri thức bằng cách nào. **Nhu cầu của nhóm AI là có thật.** Câu hỏi tương tự với bộ rule đã được quyết định ở §4: worker cần nhận rule.

| Phương án lấy tri thức | Ưu | Nhược |
|---|---|---|
| **A. Backend xuất tri thức và rule đã xác minh thành file có version trên S3; `request.json` trỏ tới file; worker tìm trong bộ nhớ** | Giữ nguyên quy tắc DB; không thêm hệ thống; truy vết version đúng CON-GEN-004 | Sửa `analysis-request` và SD-07; kho lớn thì tìm kém hơn |
| B. Worker đề xuất từ khóa, C# khớp căn cứ bằng pgvector sau khi nhận kết quả | Worker đơn giản | Agent không thấy căn cứ khi đánh giá |
| C. Role DB chỉ đọc đúng bảng `knowledge_item` | Rủi ro thấp hơn nhiều so với cho ghi; tìm được bằng pgvector | Thêm secret và đường mạng vào RDS; đổi quy tắc "worker không chạm DB" |
| D. Qdrant | Tìm kiếm mạnh | Thừa với kho nhập tay; thêm từ $20/tháng (`docs/04`); đã loại ở SD-06 |

**Đề xuất của Approver:** nhóm AI phụ trách tìm căn cứ pháp lý và **tự chọn cách tìm**, theo SRS hoặc theo cách họ thấy hợp lý. Bảng trên chỉ là gợi ý; phương án A đơn giản nhất nếu kho nhỏ. Dù chọn cách nào cũng giữ các ràng buộc sau:

- Chỉ dùng căn cứ đã có người và ngày xác minh (NFR-COMPL-002, mức Must). Thu thập tự động không thay được bước IA xác minh.
- Search web không có trong SRS §3.4, nên muốn dùng phải làm change request. Khi đó cũng phải xử lý rủi ro lộ dữ liệu và prompt injection.
- Kết luận chỉ ghi là "rủi ro tiềm ẩn", không ghi "trái pháp luật" (BR-RISK-012).
- Không ghi vào DB chính (X1).
- **Chatbot cũng cần bộ tri thức này**, và SD-14 hiện tạm để C# đọc từ pgvector. Nếu dùng kho riêng như Qdrant, nhóm AI phải nêu cách để hai nơi không bị lệch, kèm chi phí thêm.

**Quyết định:**

### X3. Triển khai thế nào?

- **SD:** một worker trên ECS Fargate, scale 0→3 task (SD-02, SD-12).
- **Nhóm AI:** một EC2 chứa API và 3 container, mỗi agent một container.
- **Lưu ý:** code AI hiện chạy một tiến trình, tức đã gần SD hơn diagram của chính họ. Cache SQLite local sẽ mất khi Spot bị thu hồi.

**Vì sao chọn ECS mà không chọn EC2** (SD-02, SD-12, Q1; SRS không quy định nền tảng này):

1. **Tiết kiệm khi không có job:** Q1 chốt không giữ worker chạy sẵn. ECS đưa worker về 0 task, EC2 thì chạy suốt.
2. **Tăng theo tải:** NFR-SCAL-001 yêu cầu 10 phân tích đồng thời. ECS chạy tối đa 3 task, mỗi task khoảng 4 job, tức 12 job. Một EC2 không scale được.
3. **Một máy hỏng là hỏng hết:** API và worker chung EC2 thì cùng dừng. Worker đọc file lạ và dùng Docling nặng cũng có thể kéo cả API xuống (đây là suy luận, chưa đo).
4. **Ít việc vận hành:** không phải tự vá hệ điều hành và lo TLS, phù hợp khi chỉ có 1 SE.

Giá không phải lý do chính: `docs/04` ghi ECS ~$9/tháng và EC2 ~$12–15/tháng, nhưng dòng ECS có vẻ chưa tính ALB. EC2 đơn giản hơn và giống môi trường local; SD-02 ghi nên chọn EC2 nếu credit gần hết.

**Đề xuất:** giữ SD, một worker chứa cả 3 agent. EC2 + Compose chỉ là phương án dự phòng cho **riêng staging demo** nếu ECS làm chậm tiến độ; khi đó Approver đổi SD rõ ràng và kiểm tra lại NFR-SCAL-001. Xung đột này ít gấp nhất, chốt sau cũng được.

**Quyết định:**

## 6. Những chỗ không phải xung đột

**Quyền của nhóm AI, SD không quy định:** pipeline 3 agent, cách tiền xử lý, khung prompt 8 phần, LangGraph, memory, cache theo job, cấu hình model riêng từng agent.

**Chỉ là chưa vẽ, chưa làm, cần nhận việc:**

| Việc | Căn cứ | Ai |
|---|---|---|
| Phát hiện prompt injection | UC-ANA-001 bước 3, F-SEC-001 | AI, IA |
| Đếm số trang, làm sạch file (loại file thật do C# kiểm tra) | SD-08 | AI, IA |
| Báo tiến trình, trả `failed`, retry | SD-11, SD-13, FR-ANA-002, FR-ANA-003 | AI |
| Ghi output theo `extraction.v2` lên S3 | SD-10, SD-11 | AI |

**Hiểu khác yêu cầu:** Agent 3 viết lại điều khoản (`clause_rewriter`), trong khi BR-RISK-014 chỉ yêu cầu hướng xử lý bằng lời. Nên sửa theo SRS; muốn giữ tính năng này thì làm change request.

**Công cụ trong repo AI:**

| Công cụ | Đề xuất | Lý do |
|---|---|---|
| Langfuse | Dùng khi dev và eval, `CAPTURE_CONTENT=false`, không dùng hợp đồng thật | Có ích để chọn model. Muốn dùng ở staging trở lên cần IA duyệt và change request cho SRS §3.4 |
| Hermes | Không chép vào `worker/` của repo chính; nhóm AI xem có bỏ được không | Gọi trực tiếp 2 module nhưng kéo theo 228 MB source; IA cần xem nếu vẫn giữ |
| Cache SQLite | Giữ logic, nhóm AI quyết định nơi lưu hoặc chấp nhận chạy lại | Không bắt buộc cho demo |

## 7. Đề xuất chia việc gửi nhóm AI và IA

Đã chốt: chấm điểm mức B (§4) và ranh giới C#, worker (X1). Tri thức pháp luật, triển khai và phần việc của IA chờ nhóm AI và IA trả lời.

| Bên | Phụ trách |
|---|---|
| **C# (SE)** | Đăng nhập, upload (tạo link S3, kiểm tra loại file), API cho frontend, trạng thái job. Là nơi duy nhất ghi DB, kiểm tra kết quả của worker rồi mới lưu |
| **Worker Python (AI)** | Nhận job từ SQS, đọc và tách điều khoản, phân loại, đánh giá rủi ro, tính điểm, tìm căn cứ pháp lý, viết hướng xử lý bằng lời. Ghi kết quả theo `extraction.v2` lên S3 rồi báo qua `analysis-results` |
| **IA** | Viết nội dung rule và trọng số, xác minh căn cứ pháp lý, rà soát kiểm tra file và prompt injection |

### Phần việc thêm cho nhóm AI

So với `docs/03` ban đầu, nhóm AI nhận thêm các việc sau:

| Việc thêm | Yêu cầu chính | Phase |
|---|---|---|
| Code tính điểm 0–100 theo nhóm và cả hợp đồng | Bước cố định trong pipeline, model không đưa ra con số; đúng BR-RISK-009, BR-RISK-010, FR-RISK-019..022; mỗi luật có unit test | 2 |
| Đọc rule và trọng số từ file cấu hình có version | Không gắn cứng trong code (CON-GEN-003, CON-GEN-004); IA viết nội dung; ghi `rule_version` vào kết quả | 2 |
| Tự kiểm tra trích dẫn, loại finding sai trước khi tính điểm | C# từ chối toàn bộ kết quả nếu còn một trích dẫn sai | 1, 2 |
| Hiệu chỉnh trọng số, ngưỡng bằng eval | Theo dõi cùng báo cáo eval hằng tuần | 2 |
| Tìm căn cứ pháp lý | Tự chọn cách tìm, giữ các ràng buộc ở X2 | 2 |

**Nhóm AI được bớt:** FastAPI, xác thực, upload, tạo job, models và migration PostgreSQL (`apps/`, `database/`, `integrations/database/`).

**Việc đã có trong `docs/03`, cần làm trước:** worker đọc SQS và trả `pong`, đọc `request.json`, tải file, trích xuất, tách điều khoản, phân loại, ghi `extraction.v2`. Phần này là Phase 1, không đụng xung đột nào, nên không cần chờ họp.

### Câu cần nhóm AI và IA trả lời

1. §2 đã mô tả đúng hướng của nhóm AI chưa, và diagram là thiết kế thật hay bản nháp?
2. Hướng chia việc này có vướng gì với plan hiện tại không? Nhóm AI và IA có nhận phần việc trên không?
3. Nhóm AI định lưu tri thức ở đâu, và kho dự kiến lớn cỡ nào?
4. Nếu cần EC2 hoặc tách container, lý do là gì?
5. Khi nào có worker trả được `pong` để nối thử với backend?

**SE:** tiếp tục Phase 1, không bị ảnh hưởng bởi quyết định ở §4. Commit code và đẩy nhánh sớm để nhóm AI thấy được đường nối thật.

## 8. Phụ lục: toàn bộ điểm lệch trong diagram

Mức độ: **Cao** là trái yêu cầu hoặc ranh giới an toàn; **TB** là tốn thêm chi phí, công sức; **Thấp** là dễ đổi.

| # | Chủ đề | Diagram, repo AI | SD, SRS | Mức | Tình trạng |
|---|---|---|---|---|---|
| L1 | Chấm điểm | Agent 2, `risk_calculator` | SD-10 cũ: C#. BR-RISK-009, CON-GEN-003: công thức có version | Cao | **Đã chốt: code Python** (§4) |
| L2 | Kiểm tra output AI | Không có bước nào | `data-contract.md`, `extraction.v2`: backend kiểm trước khi lưu | Cao | X1 |
| L3 | Ghi DB | Container 3 ghi thẳng vào icon cam (suy đoán là DB); repo có `postgres_writer` | AGENTS.md: chỉ C# ghi. SD-13 | Cao | X1 |
| L4 | Search web | MCP gọi web, kết quả vào Context Agent 3 | SRS §3.4 không có; NFR-COMPL-002 | Cao | X2 |
| L5 | Nguồn tri thức | Tự thu thập qua pipeline | `knowledge_item` Manual Entry; FR-RISK-011 | Cao | X2 |
| L6 | Upload an toàn | Không có `quarantine/`, không quét mã độc | SD-07, SD-08 | Cao | §6 |
| L7 | Prompt injection | Không có bước phát hiện | UC-ANA-001, F-SEC-001 | Cao | §6 |
| L8 | API, đăng nhập | FastAPI | SD-01, SD-04, SD-05 | Cao | **Đã chốt: C#** (X1) |
| L9 | Đường về kết quả | Không có queue kết quả, tiến trình, retry, DLQ | SD-11, SD-13, FR-ANA-002, FR-ANA-003 | Cao | X1, §6 |
| L10 | Hạ tầng | Một EC2 | SD-02, SD-12 | TB | X3 |
| L11 | Container theo agent | 3 container nối trực tiếp | SD-01; code AI chạy một tiến trình | TB | X3 |
| L12 | Vector DB | Qdrant, Redis | SD-06, K4, SD-14 | TB | X2 |
| L13 | Viết lại điều khoản | `clause_rewriter` | BR-RISK-014 | TB | §6 |
| L14 | Cổng vào | API Gateway | SD-03. `docs/04` nêu API Gateway là dự phòng khi thiếu ngân sách | Thấp | **Đã chốt: backend phụ trách**, theo SD-03 (X1) |
| L15 | Lambda | Một Lambda không nhãn đi vào tiền xử lý | `docs/04` đã loại Lambda cho worker | Thấp | Hỏi nhóm AI |

**Nhận xét về diagram:** tách đúng luồng hợp đồng và luồng tri thức, dùng SQS đúng, giữ cấu trúc điều khoản, và khung prompt 8 phần đáng dùng lại. Nhưng nhiều icon không có nhãn, ba khối đều tên "Container", không đánh số bước, chỉ vẽ đường thành công, không có chatbot hay cách ly tenant. Nên vẽ lại khi X1 và X2 đã chốt, rồi đưa vào `docs/architecture/`.

## 9. Phụ lục: tóm tắt repo AI

Phần này để agent và thành viên nắm repo AI mà **không phải rà `AI/`**.

| | |
|---|---|
| Ảnh chụp | Commit `39c3defb` (2026-11-03), nhánh `master`. Khác commit này thì tóm tắt có thể đã cũ: `git -C AI/contract-risk-management log --oneline -n 5` |
| Remote | `ContractGuard-Team/contract-risk-management` (repo riêng) |
| Trên máy SE | `AI/contract-risk-management/` là bản clone local, không thuộc repo ContractGuard. `AGENTS.md` trong đó chỉ áp dụng cho repo ấy |
| Khi tìm kiếm | Nếu `AI/` chưa được ignore, `grep` hoặc `Glob` từ root sẽ quét cả khoảng 22.000 file của Hermes. Hãy giới hạn đường dẫn |

**Vì sao nặng:** code của nhóm chỉ có 210 KB (267 file, trong đó 158 file rỗng). Hermes Agent (Nous Research, MIT) được commit nguyên cả ứng dụng: 228,5 MB, 22.004 file, cộng `.git` 95 MB. Code của nhóm gọi trực tiếp `agent.bedrock_adapter` và `agent.context_engine`, nhưng hai module này còn import `agent/`, `hermes_cli`, `hermes_constants`, `pm`, nên không thể chỉ chép 2 file. Xóa bằng commit mới không làm `.git` nhẹ đi.

**Luồng đã có code** (một tiến trình, thử với Bedrock giả):

```text
ContractRiskPipeline.run_contract(ContractInput, job_id)     orchestration/pipeline.py
  ├─ classify_chunk  Agent 1 lặp từng chunk, nối memory      orchestration/workflow.py
  ├─ aggregate       gom output theo thứ tự
  ├─ evaluate        Agent 2 nhận bản tổng hợp
  └─ recommend       Agent 3 nhận output Agent 2

BaseAgent.run → HermesBedrockProvider → HermesModelAdapter
  → agent.bedrock_adapter → BedrockClient → boto3 converse
```

**Cập nhật 2026-11-21 (từ tài khoản AI và nhánh `feat/chatbotAI`):** worker chạy 9 container Docker trên một EC2 `m7i-flex.large` (không GPU), model gọi qua OpenAI (`MODEL_PROVIDER=openai`). Worker giữ trạng thái job riêng trong DynamoDB: `contract-risk-jobs` (một dòng mỗi hợp đồng, khóa `contract:<contract_id>`, `status`/`stage`/`progress`, khóa chống chạy trùng `lease_until`) và `contract-risk-job-events` (nhật ký chỉ ghi thêm, từng bước và tool của agent). API ContractGuard đọc bảng đầu để hiện tiến độ (`docs/04` C8, `docs/07` §12). Phần dưới là ảnh chụp cũ ngày 2026-11-03.

**Đã có:**

- Cache SQLite theo job, chạy lại thì bỏ qua các bước đã lưu.
- Memory theo job, tóm tắt khi vượt ngân sách token.
- Langfuse: tắt mặc định, khi bật chỉ gửi metadata trừ khi đặt `LANGFUSE_CAPTURE_CONTENT=true`.
- Báo lỗi khi model trả rỗng hoặc bị cắt.
- 3 file unit test; tài liệu của nhóm ghi đạt 38/38 ngày 2026-11-03. Chưa có lần gọi Bedrock thật nào được ghi nhận.

**Chưa có:**

- Prompt, model ID, ngân sách token còn TODO, nên gọi thật sẽ báo `ConfigurationError`.
- Output là chữ tự do trong `AgentResult.text`, chưa theo schema.
- Vòng lặp tool, MCP client, retry cấp workflow. Các tool chỉ là vỏ gọi handler đăng ký thủ công; `plan.md` của 3 agent mô tả hành vi mục tiêu, phần lớn chưa có code.
- Trong code và tài liệu chưa có tham chiếu tới `contracts/` hay tên queue.

**Khi cần mở một file trong `AI/contract-risk-management/`:**

| Cần biết | File |
|---|---|
| Luồng đồ thị, điểm vào pipeline | `orchestration/workflow.py`, `orchestration/pipeline.py` |
| Dữ liệu vào, dữ liệu giữa các agent | `common/schemas/contract.py`, `common/schemas/agent_messages.py` |
| Vòng gọi model, nạp prompt | `agents/base.py`, `hermes_integration/config_loader.py` |
| Memory, cache, phục hồi | `docs/agent_workflow.md` |
| Langfuse | `docs/langfuse.md` |
| Hành vi mục tiêu từng agent | `agents/agent_N_*/plan.md` |
| Kiến trúc mục tiêu của nhóm AI (không phải trạng thái thật) | `docs/folder-responsibilities.md` |

Không mở `third_party/hermes-agent/` trừ khi đang sửa đúng phần nối với Hermes.

## 10. Giới hạn của file này

- Icon không nhãn trong diagram là suy đoán, đặc biệt icon cam được hiểu là DB. L3 phụ thuộc vào suy đoán đó.
- Chưa chạy code AI, chưa đo độ trễ và chi phí. Số tiền lấy từ `docs/04`.
- SD do chính Approver chốt nên dễ thiên về SD. §5 vì vậy ghi riêng lỗ hổng của SD và lập luận dựa trên SRS.
