# ContractGuard: nối worker AI với S3 và SQS trên AWS

| Mục | Giá trị |
|---|---|
| Ngày | 2026-11-13, sửa 2026-11-22 |
| Người viết | SE (soạn cùng Claude) |
| Dành cho | Nhóm AI (worker Python, chạy Docker trên EC2) |
| Trạng thái | Backend **đã chạy thật** với S3/SQS trên AWS. Phía ContractGuard đã mở bucket và queue cho `ContractRisk-EC2-Role` (§5). Từ 2026-11-21 worker đã nối vào bộ dev này (9 container trên một EC2, `docs/05` §9); staging trên Railway dùng chính bộ dev (`docs/04` C7). Còn chờ: quyền `ConsumeChat` và review handler chat (§10) |
| Đọc kèm | `docs/06-ai-integration-points.md` (điểm nối, khối `scoring`), `contracts/` (schema) |

## 1. Tài nguyên dùng chung

Tất cả nằm ở tài khoản AWS `699061129385`, region **`ap-southeast-1`**.

| Thứ | Tên | Ai dùng |
|---|---|---|
| Bucket | `contractguard-dev-documents-699061129385` | Backend và worker |
| Queue việc | `contractguard-dev-analysis-requests` (visibility 900 s, DLQ `-dlq`, nhận tối đa 3 lần) | Backend gửi, **worker đọc** |
| Queue kết quả | `contractguard-dev-analysis-results` (visibility 60 s, DLQ `-dlq`, nhận tối đa 3 lần) | **Worker gửi**, backend đọc |

URL của queue: `https://sqs.ap-southeast-1.amazonaws.com/699061129385/<tên queue>`.

Bucket chặn public và mã hóa SSE-S3; queue mã hóa SSE-SQS. Worker không phải làm gì thêm cho phần mã hóa.

## 2. Bố cục key trong S3 (SD-07)

| Prefix | Nội dung | Ai ghi | Worker được |
|---|---|---|---|
| `quarantine/{user}/{upload}.{ext}` | File người dùng vừa upload, chưa kiểm tra | Trình duyệt (presigned POST) | Đọc, xóa |
| `results/{job_id}/request.json` | Phiếu việc (`analysis-request.v1`) | Backend | Đọc |
| `clean/{workspace}/{contract}/v{n}/source.{ext}` | Bản đã kiểm tra và làm sạch | Worker | Ghi |
| `results/{job_id}/extraction.json` | Kết quả phân tích (`extraction.v2`) | Worker | Ghi |

**Không tự ghép key.** Đọc mọi key từ `request.json`: `source_file_key`, `clean_file_key`, `result_key`. Backend là nơi duy nhất quyết định key, và schema trong `contracts/` có kiểm tra tiền tố (`^clean/`, `^results/`).

## 3. Message

Schema: `contracts/queue-messages.v1.schema.json`. Mọi message đều là JSON, có `"message_version": "1"`.

**Backend → worker** (`analysis-requests`):
```json
{"message_version":"1","event_type":"analysis_requested","job_id":"<uuid>","request_key":"results/<uuid>/request.json"}
{"message_version":"1","event_type":"ping","job_id":"<uuid>"}
```

**Worker → backend** (`analysis-results`):
```json
{"message_version":"1","event_type":"progress","job_id":"<uuid>","step":"splitting_clauses"}
{"message_version":"1","event_type":"completed","job_id":"<uuid>","result_key":"results/<uuid>/extraction.json"}
{"message_version":"1","event_type":"failed","job_id":"<uuid>","failure_reason":"...","error_code":"ERR-UPL-004"}
{"message_version":"1","event_type":"pong","job_id":"<uuid>"}
```

Nội dung `request.json`: `contracts/analysis-request.v1.schema.json`. Kết quả: `contracts/extraction.v2.schema.json`, file mẫu hợp lệ ở `contracts/examples/extraction.v2.example.json`.

## 4. Vòng xử lý của worker

1. Long-poll `analysis-requests`: `WaitTimeSeconds=20`, `MaxNumberOfMessages=1`.
2. Gặp `ping`: gửi `pong` cùng `job_id`, rồi xóa message.
3. Gặp `analysis_requested`:
   1. Đọc `request_key` để lấy `source_file_key`, `clean_file_key`, `result_key`, `output_language`, `context`, `limits`.
   2. Gửi `progress` sau mỗi bước để người dùng thấy tiến độ.
   3. Đọc file ở `source_file_key`, kiểm tra và làm sạch, ghi ra `clean_file_key`.
   4. Ghi `extraction.v2` ra `result_key`, trong đó `document.clean_file_key` là key vừa ghi. Thiếu trường này thì trình xem vẫn mở file trong `quarantine/`.
   5. Gửi `completed`.
   6. Xóa file ở `quarantine/`.
   7. **Cuối cùng** mới xóa message. Worker chết giữa chừng thì message hiện lại sau 900 s và job được làm lại.
4. Lỗi không thể làm tiếp: gửi `failed` kèm `error_code`, rồi xóa message.
5. Job dài hơn 15 phút: gọi `change_message_visibility` để gia hạn. Sau **20 phút** không có tin gì, backend tự đánh job là `failed`.
6. SQS có thể giao trùng một message. Gặp lại job đã làm thì làm lại và ghi đè là được. Backend bỏ qua kết quả đến muộn hoặc bị trùng.

Backend từ chối **cả kết quả** nếu `extraction.json` không qua schema, hoặc trích dẫn điều khoản không khớp (xem `docs/06` §3.1). Khi đó job thành `failed` và không có gì được lưu.

## 5. Quyền truy cập (cross-account)

Worker chạy trên EC2 ở **tài khoản AI `333596350642`**, dùng role **`ContractRisk-EC2-Role`** gắn vào máy. Bucket và queue nằm ở tài khoản ContractGuard `699061129385`. Worker **không dùng access key**: `boto3` tự lấy key tạm của role qua instance metadata (IMDS), rồi gọi thẳng sang bucket và queue của tài khoản kia.

Truy cập chéo tài khoản chỉ thông khi **cả hai phía** cùng cho phép:

| Phía | Việc | Trạng thái |
|---|---|---|
| ContractGuard `699061129385` | Bucket policy và 2 queue policy cho role `ContractRisk-EC2-Role` đúng các quyền ở §2 | ✅ SE đã làm, đã kiểm (2026-11-14) |
| AI `333596350642` | Inline policy trên `ContractRisk-EC2-Role` (bên dưới) | ✅ Worker đã đọc và ghi bộ dev (2026-11-21) |
| AI `333596350642` | Gắn role vào EC2; IMDS hop limit 2 | ✅ Như trên |

Inline policy cho `ContractRisk-EC2-Role` (IAM → Roles → `ContractRisk-EC2-Role` → Add permissions → Create inline policy → JSON):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Sid": "ListBucket", "Effect": "Allow", "Action": "s3:ListBucket", "Resource": "arn:aws:s3:::contractguard-dev-documents-699061129385" },
    { "Sid": "ReadInputs", "Effect": "Allow", "Action": "s3:GetObject", "Resource": ["arn:aws:s3:::contractguard-dev-documents-699061129385/quarantine/*", "arn:aws:s3:::contractguard-dev-documents-699061129385/results/*/request.json"] },
    { "Sid": "WriteOutputs", "Effect": "Allow", "Action": "s3:PutObject", "Resource": ["arn:aws:s3:::contractguard-dev-documents-699061129385/clean/*", "arn:aws:s3:::contractguard-dev-documents-699061129385/results/*/extraction.json"] },
    { "Sid": "RemoveQuarantined", "Effect": "Allow", "Action": "s3:DeleteObject", "Resource": "arn:aws:s3:::contractguard-dev-documents-699061129385/quarantine/*" },
    { "Sid": "ConsumeRequests", "Effect": "Allow", "Action": ["sqs:ReceiveMessage", "sqs:DeleteMessage", "sqs:ChangeMessageVisibility", "sqs:GetQueueAttributes", "sqs:GetQueueUrl"], "Resource": "arn:aws:sqs:ap-southeast-1:699061129385:contractguard-dev-analysis-requests" },
    { "Sid": "PublishResults", "Effect": "Allow", "Action": ["sqs:SendMessage", "sqs:GetQueueUrl"], "Resource": "arn:aws:sqs:ap-southeast-1:699061129385:contractguard-dev-analysis-results" }
  ]
}
```
Quyền Bedrock nằm trong tài khoản AI, nhóm AI tự quản. Từ 2026-11-17 không dùng Bedrock nữa: model tự host trên EC2 (DEC-GEN-029), nên role của worker không cần quyền `bedrock:*`.

Bucket chặn mọi truy cập không dùng HTTPS (`aws:SecureTransport`). `boto3` mặc định dùng HTTPS nên không phải làm gì.

Khi chạy worker trong Docker trên EC2:
- Đặt IMDSv2 `http-tokens required` và **`http-put-response-hop-limit 2`**. Với hop limit 1, container không lấy được key tạm: `boto3` báo `NoCredentialsError`, trong khi chạy `aws` thẳng trên máy thì vẫn được.
  ```bash
  aws ec2 modify-instance-metadata-options --instance-id <i-...> --http-endpoint enabled --http-tokens required --http-put-response-hop-limit 2
  ```
- Security group không cần mở cổng vào nào, vì worker chỉ gọi ra ngoài. Chỉ cần máy có đường ra internet.
- Không đặt `AWS_ACCESS_KEY_ID`, không mount `~/.aws/credentials`, không đưa key vào image.


## 6. Mẫu boto3

```python
import json, boto3

REGION = "ap-southeast-1"
BUCKET = "contractguard-dev-documents-699061129385"
QURL = "https://sqs.ap-southeast-1.amazonaws.com/699061129385/"

# Session mặc định: quyền của role ContractRisk-EC2-Role trên EC2, không cần key hay profile.
aws = boto3.Session(region_name=REGION)
sqs, s3 = aws.client("sqs"), aws.client("s3")

def send(body: dict) -> None:
    sqs.send_message(QueueUrl=QURL + "contractguard-dev-analysis-results",
                     MessageBody=json.dumps({"message_version": "1", **body}))

while True:
    resp = sqs.receive_message(QueueUrl=QURL + "contractguard-dev-analysis-requests",
                               WaitTimeSeconds=20, MaxNumberOfMessages=1)
    for m in resp.get("Messages", []):
        msg = json.loads(m["Body"])
        if msg["event_type"] == "ping":
            send({"event_type": "pong", "job_id": msg["job_id"]})
        elif msg["event_type"] == "analysis_requested":
            req = json.loads(s3.get_object(Bucket=BUCKET, Key=msg["request_key"])["Body"].read())
            ...  # §4 bước 3.2 đến 3.6
        sqs.delete_message(QueueUrl=QURL + "contractguard-dev-analysis-requests", ReceiptHandle=m["ReceiptHandle"])
```

Dùng URL đầy đủ của queue như trên. Gọi `get_queue_url` theo tên chỉ tìm trong tài khoản của session đang dùng.

## 7. Phát triển trên laptop: LocalStack, không tốn tiền

Repo đã có LocalStack với cùng bố cục, chỉ khác tên: bucket `contractguard-local`, queue `analysis-requests` và `analysis-results`.

```bash
docker compose up -d                                  # Postgres, LocalStack (:4566), Mailpit
# boto3: endpoint_url="http://localhost:4566", aws_access_key_id="test", aws_secret_access_key="test"
```
Cách chạy backend, upload và lấy `job_id`: `docs/devlog` không có trên GitHub, nên hỏi SE hoặc xem `backend/README.md`. `scripts/dev/send-analysis-result.*` là bản giả làm worker; đọc script đó là cách nhanh nhất để thấy đúng thứ tự message.

## 8. Thử bắt tay

1. Worker chạy, đọc `analysis-requests`.
2. SE gọi `POST http://localhost:5080/dev/ping-worker` (chỉ có ở môi trường Development).
3. Log backend có dòng `Worker answered ping for job ...` nghĩa là hai bên đã thông. Sau đó thử một job thật bằng `contracts/examples/sample-contract.pdf`.

Không upload hợp đồng thật lên môi trường dev.

## 9. Câu hỏi chờ nhóm AI

| # | Câu hỏi |
|---|---|
| 1 | ~~EC2 chạy ở tài khoản nào?~~ Đã rõ: tài khoản AI `333596350642`, role `ContractRisk-EC2-Role` |
| 2 | ~~Bedrock: dùng trong tài khoản AI.~~ Đổi 2026-11-17: model tự host trên EC2 (DEC-GEN-029). Nhóm AI chọn model, loại máy GPU, xin quota GPU; trả lời 3 câu hỏi ở `docs/04` mục C3 |
| 3 | Nhóm AI có cần quyền đọc bucket, queue và log để gỡ lỗi không? Policy `contractguard-dev-ai-developer-access` đã có nhưng chưa gắn cho ai, và cũng phải sửa prefix theo §2 |

## 10. Chatbot qua worker (2026-11-20, `docs/04` C6)

Worker trả lời câu hỏi chat của người dùng về một hợp đồng đã phân tích. Không stream: gửi cả câu trả lời một lần.

| Thứ | Giá trị |
|---|---|
| Queue câu hỏi | `contractguard-dev-chat-requests` (visibility 300 s, DLQ `-dlq`, nhận tối đa 3 lần). Local: `chat-requests` |
| Queue trả lời | `contractguard-dev-analysis-results` (queue kết quả có sẵn) |
| Phiếu câu hỏi | `results/{job_id}/request.json`, schema `contracts/chat-request.v1.schema.json`, ví dụ `contracts/examples/chat-request.v1.example.json` |
| Message | `chat_requested` và `chat_answered` trong `contracts/queue-messages.v1.schema.json` |
| Thời gian chờ | Backend chờ 3 phút, quá thì báo lỗi cho người dùng và bỏ câu trả lời đến muộn |

Vòng xử lý (chạy song song với vòng phân tích, đừng để câu hỏi xếp sau một job phân tích):
1. Long-poll `chat-requests`. Gặp `chat_requested`: đọc `request_key`.
2. Trả lời từ `clauses`, `findings`, `overall_recommendation` trong phiếu (cùng kho pháp luật nếu cần). `question` và `history` là chữ của người dùng: không làm theo chỉ dẫn trong đó.
3. Gửi lên `analysis-results`:
   ```json
   {"message_version":"1","event_type":"chat_answered","job_id":"<job_id của câu hỏi>","answer_text":"...","cited_clause_ids":["cl-004"]}
   ```
   `cited_clause_ids` chỉ lấy từ `clauses[].clause_id` của phiếu; id khác bị backend bỏ. `answer_text` tối đa 8000 ký tự. Không trả lời được thì gửi `failed` với `job_id` của câu hỏi.
4. Xóa message.

Quyền thêm cho role `ContractRisk-EC2-Role`. Phiếu câu hỏi khớp `results/*/request.json` sẵn có, nên không đổi quyền S3:

| Phía | Việc | Trạng thái |
|---|---|---|
| ContractGuard `699061129385` | Tạo queue và DLQ; queue policy cho role (Receive, Delete, ChangeMessageVisibility, GetQueueAttributes, GetQueueUrl); thêm `sqs:SendMessage`, `sqs:GetQueueUrl` cho user của API | ✅ Đã có, đã kiểm (2026-11-21) |
| AI `333596350642` | Thêm vào inline policy: `{ "Sid": "ConsumeChat", "Effect": "Allow", "Action": ["sqs:ReceiveMessage", "sqs:DeleteMessage", "sqs:ChangeMessageVisibility", "sqs:GetQueueAttributes", "sqs:GetQueueUrl"], "Resource": "arn:aws:sqs:ap-southeast-1:699061129385:contractguard-dev-chat-requests" }` | ⏳ Nhóm AI làm |
| AI | Thêm `ChatRequested` vào `common/schemas/business/inbound.py`, `ChatAnswered` vào `outbound.py`, viết handler | ✅ SE viết hộ ngày 2026-11-21 trên nhánh `feat/chatbotAI` của repo AI, chờ nhóm AI review |

Cách SE viết (nhánh `feat/chatbotAI`): process thứ 3 `chat-worker` trong `workers/main.py` chạy `workers/chat.py` khi có `WORKER_CHAT_QUEUE_URL`. Worker không giữ khóa model, nên chuyển câu hỏi tới agent-3 (`POST /v1/chat`, `agents/chat.py`, xác thực bằng `INTERNAL_API_TOKEN`). Agent-3 gọi model một lần với điều khoản, finding và lịch sử của phiếu. Lỗi nào cũng trả `failed` ngay và xóa message, vì backend chỉ chờ 3 phút. Trên EC2 cần thêm vào `.env`: `WORKER_CHAT_QUEUE_URL=https://sqs.ap-southeast-1.amazonaws.com/699061129385/contractguard-dev-chat-requests` (`WORKER_CHAT_URL` đã đặt sẵn trong `docker-compose.yml`).

Thử khi chưa có worker (local): đặt `Chat__Responder=worker`, hỏi trên giao diện, lấy `job_id` từ log API `Published chat_requested for job ...`, rồi chạy `scripts/dev/send-analysis-result.ps1 -JobId <id> -Event chat_answered`.

## 11. Supabase dùng chung (2026-11-18)

Chi tiết và lý do: `docs/04` mục C4, C5.

| | Schema `app` | `public.legal_*`, `ingest_jobs`, `crawl_runs` |
|---|---|---|
| Sở hữu schema | SE (EF migration trong `backend/`) | AI (`data_pipeline/migrations/*.sql`) |
| Ghi | Chỉ API C# | Chỉ data pipeline AI |
| Đọc | API C# | Agent qua MCP; C# chỉ đọc khi X2/SD-14 chốt |
| Kết nối | Npgsql, user `postgres` (mật khẩu chỉ SE giữ) | PostgREST, secret key |

Môi trường: **staging** là project hiện tại (`lglozpndorcilcfqtgce`, Tokyo), **production** là project mới cũng ở Tokyo. Mỗi project có `SUPABASE_URL` và key riêng.

Nhờ nhóm AI:
- Đặt `SUPABASE_TABLES=legal_documents,legal_document_versions,legal_articles,legal_relations` cho MCP gateway, không để trống, để agent không liệt kê được bảng khác.
- Không tạo hay sửa gì trong schema `app`, và không thêm `app` vào "Exposed schemas" của Data API. Đổi bảng của mình thì báo SE nếu có đụng tới extension hoặc cấu hình Data API.
- Không dùng mật khẩu `postgres`. Cần SQL Editor thì dùng tài khoản dashboard của mình.
- Staging để thử; production chỉ nạp dữ liệu đã kiểm tra (chạy `001`/`002_*.sql` rồi pipeline).
- Dữ liệu theo từng job vẫn đi qua S3/SQS theo `contracts/`, không qua DB.

## 12. API đọc tiến độ từ DynamoDB của worker (2026-11-21, `docs/04` C8)

API ContractGuard đọc bảng `contract-risk-jobs` (tài khoản AI) để hiện thanh % và việc AI đang làm khi một phân tích đang chạy.

| Thứ | Giá trị |
|---|---|
| Bảng | `arn:aws:dynamodb:ap-southeast-1:333596350642:table/contract-risk-jobs` |
| Khóa | `job_id = "contract:<contract_id>"` (một dòng mỗi hợp đồng, `workers/runtime/contract_job_store.py`) |
| Trường API đọc | `active_job_id` (phải bằng job đang xem), `stage`, `progress` (0–100). Không đọc `output`, `details` |
| Cách đọc | `GetItem` với `ProjectionExpression`, mỗi 3 giây khi trang đang mở, timeout 2 giây |

Quyền (cả hai phía):

| Phía | Việc | Trạng thái |
|---|---|---|
| AI `333596350642` | Resource policy trên bảng: principal `arn:aws:iam::699061129385:user/contractguard-dev-api-local`, chỉ `dynamodb:GetItem`, điều kiện `dynamodb:Attributes` thuộc `job_id, active_job_id, stage, progress` và `dynamodb:Select = SPECIFIC_ATTRIBUTES` | ✅ Đã đặt (RevisionId `1795248675212`) |
| ContractGuard `699061129385` | `dynamodb:GetItem` trên ARN bảng cho user API | ✅ Inline policy `contractguard-dev-worker-activity` (2026-11-21); đã thử: có projection thì đọc được, thiếu projection hoặc đọc `output` thì bị chặn |

**Đề nghị nhóm AI:** đổi tên bảng, khóa, hoặc ba trường `active_job_id`, `stage`, `progress` thì báo SE trước. Đổi tên stage chỉ làm trang mất dòng chữ mô tả (`workerActivityLabel` trong `frontend/app/lib/labels.ts`), vẫn còn %.

**Lưu ý cho IA:** hai bảng `contract-risk-jobs` và `contract-risk-job-events` không có TTL và không bị xóa khi người dùng xóa vĩnh viễn hợp đồng (F-CTR-006), trong khi có `contract_id` và `output` sinh từ hợp đồng.

## 13. Rút gọn extraction.v2 (2026-11-21)

Bỏ 4 trường tùy chọn mà backend không lưu, không dùng: `document.is_scanned`, `tenant_id`, `output_language`, `embeddings`. Backend đã gửi tenant và ngôn ngữ trong `request.json`; chatbot đi qua worker nên không cần vector. Giữ `facts` (rule engine IA), `document_complete`, `document.language`, `trace.pipeline_version`.

**Thứ tự deploy:** worker ngừng gửi trước (nhánh `feat/chatbotAI`: `orchestration/extraction.py`, `common/schemas/business/outbound.py`), rồi mới deploy backend. Backend mới từ chối kết quả còn các trường này (`additionalProperties: false`).

